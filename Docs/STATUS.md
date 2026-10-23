# Status (atualize sempre que o usuário testar algo)

## Testado em jogo (out/2026): carrega, pesquisa, construção, tiro local e no mapa-mundo funcionaram

## Detalhes
- **Lançador de mísseis táticos** `MAS_Turret_TacticalLauncher` / gun `MAS_Gun_TacticalLauncher`
  - 3x3, tripulado (`TurretMannedBuildableBase` do CE), interação ao sul; minificável (exigido pelas thingCategories da base do CE).
  - `Verb_ShootMortarCE`, `isMortar`, range 700 (min 60), warmup 10 s, burst 1, cooldown 30 s, mag 2 (recarga 30 s, um por vez), `circularError` 3, `indirectFirePenalty` 0,05.
  - Custo: aço 800, plasteel 120, CompInd 12, CompSp 2; WorkToBuild 40000; construção 8.
  - Pesquisa `MAS_Research_TacticalMissiles` (Mortars + MicroelectronicsBasics, 3000).
- **Míssil** `MAS_Ammo_TacticalMissile_HE` → projétil `MAS_Bullet_TacticalMissile_HE`
  - speed 0, gravityFactor 5, Bomb 1500, raio 9,9, CraterLarge, fragmentos 20/30.
  - `shellingProps`: range 120 tiles, tilesPerTick 0,4, damage 1,5.
  - Receita `MakeMAS_Ammo_TacticalMissile_HE` (bancada de usinagem, via injector do CE): aço 300, plasteel 20, FSX 120, CompInd 6; trabalho 60000; Crafting 8.

## Sprite por munição (implementado, NÃO testado)
- `Building_MissileLauncherCE` (subclasse de `Building_TurretGunCE`) sobrescreve `TurretTopMaterial` (o `TurretTop.DrawTurret` do jogo desenha esse material; conferido no IL) e escolhe a textura pela `CompAmmo.CurMagCount`, via `LoadedTopsExtension` (Top0/Top1/Top2).

## Correções após 1º carregamento
- "not minifiable yet has thing categories" → `minifiedDef MinifiedThing`.
- "ammo injector found no recipe named MakeMAS_Ammo_TacticalMissile_HE" → receita renomeada.

- Giro do erguedor: `NonSnapTurretExtension` 0,2°/tick (12°/s), NÃO testado. Silo e bateria antimíssil sem giro (tubo fixo / lançador do Patriot aponta para um setor e o míssil manobra).

## O que testar
1. Pesquisa aparece na aba "Strategic Missiles"; prédio no menu Security; míssil na bancada de usinagem.
2. Construir, carregar 2 mísseis, atirar no mapa local (> 60 células).
3. Botão "atacar tile do mundo": aparece? alcance de 120 tiles? o míssil viaja e destrói/danifica o assentamento? retaliação?
4. Log sem erros.

## Silo de ICBM (implementado, NÃO testado)
- `MAS_Turret_ICBMSilo` / gun `MAS_Gun_ICBMSilo`: 3x3, `BuildingBase` + campos da TurretBase do CE (sem thingCategories → não precisa ser minificável), 1000 W, sem operador, `ManualFireOnlyExtension` (não mira sozinho; o CE já não dá "forçar ataque" a torreta sem operador). Só "atacar tile do mundo".
  - Verbo: `Verb_ShootMortarCE`, range 700, **minRange 0** (a célula de saída na borda nunca fica "perto demais"), warmup 20 s, mag 1, recarga 60 s, cooldown 60 s, `circularError` 6.
  - Custo: aço 1500, plasteel 300, CompInd 20, CompSp 6; WorkToBuild 120000; construção 10. Não pode ficar sob teto.
  - Pesquisa `MAS_Research_ICBM` (tático + AdvancedFabrication, 6000, Spacer).
- `MAS_Ammo_ICBM_HE` → `MAS_Bullet_ICBM_HE`: Bomb 3000, raio 14,9, cratera; shellingProps range 500, tilesPerTick 0,6, damage 5. Receita `MakeMAS_Ammo_ICBM_HE` na bancada de fabricação.
- **Nuclear (só com Rimatomics)**, `1.6/Rimatomics`: `MAS_Ammo_ICBM_Nuclear` → `MAS_Bullet_ICBM_Nuclear` (classe `ProjectileCE_NuclearStrike`). No impacto gera o `ICBMStrike` do Rimatomics (`Rimatomics.NuclearStrike`) com `Yield` 55 por reflexão; o próprio NuclearStrike faz clarão, onda de choque e `NuclearFallout` (lido no IL: `StartStrike` → `GameConditionMaker.MakeCondition`/`RegisterCondition`, `GoBang`). Mapa-mundo: damage 20 (destrói qualquer nível tecnológico). Receita usa 1 `FissionWarhead` do Rimatomics. Cook-off usa o projétil HE.

## O que testar no silo
1. Construção (sem teto), energia; não atira em inimigos sozinho; não tem "forçar ataque".
2. Fabricar ICBM HE; carregar; "atacar tile do mundo" até 500 tiles; assentamento destruído em 1–2 acertos.
3. Com Rimatomics: receita nuclear aparece; disparo em mapa carregado → explosão nuclear + fallout do Rimatomics; em assentamento não carregado → destruído.
4. Sprite do silo: tubo vazio / com míssil.

## Bateria antimíssil (implementado, NÃO testado)
- `MAS_Turret_InterceptorBattery` (classe `Building_MissileInterceptorCE`, subclasse do `Building_CIWS_CE` do CE com sprite por munição) / gun `MAS_Gun_InterceptorBattery`: só verbos de interceptação (projéteis e drop pods), alcance 119, rajada 2, mag 4 (recarga 8 s, um por vez). 3x3, 400 W, minificável. Aço 600, plasteel 100, CompInd 10, CompSp 3. Pesquisa `MAS_Research_MissileDefense` (tático, 4000).
- Interceptador `MAS_Ammo_Interceptor` → `MAS_Bullet_Interceptor` (speed 180, `impactChance` 0,85); receita `MakeMAS_Ammo_Interceptor` (bancada de usinagem).
- Depende da opção do CE "Enable CIWS" (Controller.settings.EnableCIWS).

## Retaliação da IA com mísseis (implementado, NÃO testado)
- `1.6/Patches/ShellingResponse.xml`: Spacer + tático (1500 pts, peso 0,25); Ultra e Archotech + tático (1500, 0,4) e ICBM HE (4000, 0,15). Sem nuclear para a IA.

## O que testar (defesa/IA)
1. Bateria abate granadas de morteiro inimigas e mísseis da retaliação; botões de "segurar fogo" (projéteis/drop pods) aparecem; sprite 0–4.
2. Atacar assentamento espacial e ver se a retaliação usa mísseis táticos.

## Próximos passos
- Vanilla: sistema próprio de tiro no mapa-mundo.
- Remote do git.
