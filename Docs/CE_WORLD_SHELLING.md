# Tiro no mapa-mundo do CE (lido no código, CE 16.7.x)

Fonte: `CombatExtended-Continued/CombatExtended`, arquivos `Things/Building_TurretGunCE.cs`, `Verbs/Verb_ShootMortarCE.cs`, `WorldObjects/TravelingShell.cs`, `WorldObjects/HealthComp.cs`, `WorldObjects/WorldObjectDamageWorker.cs`, `Projectiles/ProjectileCE.cs`.

## Botão
`Building_TurretGunCE.GetGizmos` mostra `Command_ArtilleryTarget` se `IsMortar && Active && Faction.IsPlayerSafe() && ProjectileProps?.shellingProps != null`. Alcance máximo do seletor: `shellingProps.range` (tiles).

## Disparo e viagem
- `Verb_ShootMortarCE.TryStartShelling` → mira na célula de saída do mapa na direção do alvo; o projétil sai do mapa e vira `TravelingShell` (WorldObject), que anda `tilesPerTick`.
- Chegada (`TravelingShell.Arrived`):
  - mapa do alvo carregado → projétil nasce perto da borda (75% da distância), altura 200, velocidade 55, e cai na célula mirada (ou aleatória);
  - não carregado → `HealthComp.ApplyDamage` no assentamento/site; `HostilityComp.TryHostilityResponse` (a facção pode retaliar).

## Dano no assentamento
- `health` 0..1; cura `techLevel/4 × 0,15` por dia.
- Dano por tiro = `shellingProps.damage` (se > 0; senão calculado de explosão/fragmentos ÷ 3500) × aleatório 0,4–1,5 ÷ `ArmorDamageMultiplier`.
- `ArmorDamageMultiplier` ≈ nível tecnológico da facção (Neolithic 2, Medieval 3, Industrial 4, Spacer 5, Ultra 6), × modificadores de facção/site.
- Exemplos: damage 1,5 vs Industrial = 0,15–0,56 por tiro (média ~0,36 → ~3 acertos). Para destruir sempre com 1 tiro um Ultra: damage ≥ 6 / 0,4 = 15.
- Alguns sites/facções podem ter `destroyedInstantly` ou chance de negar dano (`WorldObjectHealthExtension`).

## Marcador de artilharia (mapa carregado)
`Command_ArtilleryTarget`: se o mapa do alvo está carregado (há colonos lá), exige `ArtilleryMarker` na célula quando a opção global `RequireArtilleryMarkerForOffMapArtillery` está ligada **e** `turret.OrbitalTurretExtension?.isMarkMandatory ?? true`. A extensão fica na **gun** (`Gun.def`), padrão `isMarkMandatory = false`; ela também habilita `CanShootOtherLayers`.

## Precisão do tiro global (por que o Minuteman errava 30+ células)
- `Verb_LaunchProjectileCE.ShiftVecReportFor(GlobalTargetInfo)`: `shotDist` = tiles × 5, `lightingShift` = 1 e `smokeDensity` = 10 **fixos**; `Verb_ShootMortarCE` recalcula `weatherShift` e o `circularMissRadius` (`circularError` × 0,5–1).
- `ShiftGlobalTarget`: `shotDist` ≥ `maxRange × 0,444` sem marcador (0,111 com marcador); desvio = erro de distância (`distShift` = shotDist² / maxRange × min(accuracyFactor·0,5; 0,8), accuracyFactor = (1,5 − AimingAccuracy) / SightsEfficiency) + círculo de `visibilityShift + circularMissRadius` (+ spread). `visibilityShift` cresce com luz/fumaça e com shotDist / 50.
- Célula fora do mapa na chegada → `TravelingShell` sorteia uma célula aleatória.
- Nosso `Verb_ShootGuidedCE`: zera luz/fumaça/clima (tiro local e global) e põe AimingAccuracy 1,5 no global (accuracyFactor 0 → sem erro de distância). Resta o círculo de `circularError`.

## Salva
- `Command_ArtilleryTarget` agrupa (`GroupsWith`/`MergeWith`): várias torretas selecionadas recebem a mesma ordem.
- `Verb_ShootMortarCE.TryCastShot` usa `TryCastGlobalShot` em cada tiro da rajada; `ShiftGlobalTarget` sorteia dispersão por tiro.

## Dano EMP no mapa-mundo
`damage` manual (> 0) substitui o cálculo. Sem ele: `EMPPotentialDamage = base·m + Σ(i=1..raio-1) m·dano(i)·2^i`, ÷ 3500; `m` = 1 contra facções só de mecanoides, senão `3,3^tech / 1800 / (tech-2)` (Industrial ≈ 0,033, Spacer ≈ 0,072, Ultra ≈ 0,18). O termo 2^raio domina: raio grande = dano explosivo também contra humanos.

## Referências de alcance (shellingProps.range)
Morteiro 81 mm 10 · morteiro 120 mm 18 · foguetes 130/132 mm 20 · obuseiro 105 mm 30 · 152/155 mm 60 · maior visto (patch de mod) 80.
