# Status (atualize sempre que o usuário testar algo)

## Implementado, NÃO testado em jogo
- **Lançador de mísseis táticos** `MAS_Turret_TacticalLauncher` / gun `MAS_Gun_TacticalLauncher`
  - 3x3, tripulado (`TurretMannedBuildableBase` do CE), interação ao sul; minificável (exigido pelas thingCategories da base do CE).
  - `Verb_ShootMortarCE`, `isMortar`, range 700 (min 60), warmup 10 s, burst 1, cooldown 30 s, mag 2 (recarga 30 s, um por vez), `circularError` 3, `indirectFirePenalty` 0,05.
  - Custo: aço 800, plasteel 120, CompInd 12, CompSp 2; WorkToBuild 40000; construção 8.
  - Pesquisa `MAS_Research_TacticalMissiles` (Mortars + MicroelectronicsBasics, 3000).
- **Míssil** `MAS_Ammo_TacticalMissile_HE` → projétil `MAS_Bullet_TacticalMissile_HE`
  - speed 0, gravityFactor 5, Bomb 1500, raio 9,9, CraterLarge, fragmentos 20/30.
  - `shellingProps`: range 120 tiles, tilesPerTick 0,4, damage 1,5.
  - Receita `MakeMAS_Ammo_TacticalMissile_HE` (bancada de usinagem, via injector do CE): aço 300, plasteel 20, FSX 120, CompInd 6; trabalho 60000; Crafting 8.

## Correções após 1º carregamento
- "not minifiable yet has thing categories" → `minifiedDef MinifiedThing`.
- "ammo injector found no recipe named MakeMAS_Ammo_TacticalMissile_HE" → receita renomeada.

## O que testar
1. Pesquisa aparece na aba "Strategic Missiles"; prédio no menu Security; míssil na bancada de usinagem.
2. Construir, carregar 2 mísseis, atirar no mapa local (> 60 células).
3. Botão "atacar tile do mundo": aparece? alcance de 120 tiles? o míssil viaja e destrói/danifica o assentamento? retaliação?
4. Log sem erros.

## Próximos passos
- Silo de ICBM (convencional), depois de testar o lançador tático.
- Ogiva nuclear integrada ao Dubs Rimatomics (`Dubwise.Rimatomics`): defs só com o mod ativo (LoadFolders `IfModActive` ou `MayRequire`), DLL nossa referenciando CE para aplicar `NuclearFallout` no impacto. Verificar a API/GameCondition do Rimatomics antes.
- Vanilla: sistema próprio de tiro no mapa-mundo.
- Remote do git.
