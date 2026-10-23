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

## Referências de alcance (shellingProps.range)
Morteiro 81 mm 10 · morteiro 120 mm 18 · foguetes 130/132 mm 20 · obuseiro 105 mm 30 · 152/155 mm 60 · maior visto (patch de mod) 80.
