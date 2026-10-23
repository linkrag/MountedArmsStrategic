# Status (atualize sempre que o usuário testar algo)

Ambiente: RimWorld 1.6, Harmony, Combat Extended 16.7.4, Dubs Rimatomics (opcional). DLL: `Source/build.ps1` **com o jogo fechado** (o jogo trava a DLL; `-Out <pasta>` só para validar a compilação).

## Confirmado em jogo (out/2026)
- Carregamento sem erros, aba de pesquisa, construção, receitas no injector do CE.
- **M270 (ATACMS)** `MAS_Turret_TacticalLauncher`: tiro local e no mapa-mundo; salva de 2 com dispersão só no 2º míssil (`recoilAmount` 1,2; o recuo do CE é 0 no 1º tiro da rajada).
- **Minuteman III (nuclear, com Rimatomics)**: explosão nuclear e fogo do próprio Rimatomics funcionando.
- **Retaliação do CE**: a facção atacada revida com morteiros no mapa do jogador.
- **Lote 5**: botão Descarregar (troca de ogiva pelo menu de recarga do CE + Descarregar), nomes reais, ataque a mapa carregado **sem marcador** nos lançadores guiados, raios de explosão em escala.

## Ajustes desta rodada (NÃO testados)
- **M270 consome energia** (300 W) além do operador.
- **Minuteman errava ~30+ células**. Causa (código do CE, `Verb_LaunchProjectileCE.ShiftVecReportFor(GlobalTargetInfo)` e `Verb_ShootMortarCE`): no tiro pelo mapa-mundo o CE trata a distância como ≥ 44% do alcance máximo (Minuteman: ≥ 1100 "células"), soma iluminação fixa 1 e fumaça fixa 10 e um erro de estimativa de distância proporcional à precisão; se o desvio sai do mapa, sorteia célula aleatória. Correção: verbo próprio `MountedArmsStrategic.Verb_ShootGuidedCE` (`Source/GuidedVerb.cs`) que zera luz/fumaça/clima e usa AimingAccuracy 1,5 no tiro global (sem erro de distância). Sobra só o `circularError` (≈ CEP). Aplicado em M270, HIMARS, Mk 41 e Minuteman; Minuteman `circularError` 6 → 4. M777 continua com a física de artilharia comum.

## Implementado, NÃO testado
- **M142 HIMARS** `MAS_Turret_MLRS`: tripulado 3x3, minificável, pod de 6 (troca o pod inteiro, 30 s), rajada 6 × 25 ticks, ShotSpread 0,6, circularError 2, giro 20°/s. `MAS_Ammo_GMLRS` (M31): Bomb 900 r 7,4; mundo 40 tiles, 0,5 tiles/tick, damage 0,4.
- **Mk 41 VLS (Tomahawk)** `MAS_Turret_CruiseVLS`: 2x2, 300 W, sem operador, `ManualFireOnlyExtension` + `FixedTopExtension` (topo não gira), mag 4, circularError 1. `MAS_Ammo_CruiseMissile`: Bomb 5600 r 17,9; mundo 400 tiles, 0,15 tiles/tick, damage 3.
- **Minuteman III (convencional)** `MAS_Turret_ICBMSilo`: 3x3, 1000 W, sem operador, só "atacar tile do mundo", mag 1, recarga 60 s. HE: Bomb 10000 r 24,9; mundo 500 tiles, damage 5.
- **M777** `MAS_Turret_M777_Howitzer`: tripulado 3x3, minificável, `AmmoSet_155mmHowitzerShell` do CE (mundo 60 tiles), giro 10°/s, exige marcador em mapa carregado.
- **Ogivas especiais** (`MAS_Research_SpecialWarheads`), ATACMS / Minuteman:
  - Cluster (`ProjectileCE_Cluster`): 40 bombas em raio 12 / 100 em raio 26; mundo 1,2 / 3,5.
  - Termobárica (dano `Thermobaric` do CE): 1500 r 12,9 / 6000 r 24,9; mundo 1,8 / 6.
  - EMP: 400 r 6,9 / 600 r 7,9, **sem** `shellingProps.damage` (o CE usa o modificador EMP: 1,0 contra mecanoides, 0,03–0,18 contra humanos).
- **MIM-104 Patriot** `MAS_Turret_InterceptorBattery` (`Building_MissileInterceptorCE`): só interceptação (projéteis e drop pods), alcance 119, 2 por ameaça, mag 4, 400 W. PAC-3 MSE: speed 180, `impactChance` 0,85. Precisa da opção do CE "Enable CIWS" (a interceptação do CIWS do Mounted Arms foi confirmada com ela ligada).
- **Retaliação com mísseis** (`1.6/Patches/ShellingResponse.xml`): Spacer GMLRS (400 pts) + ATACMS (1500); Ultra/Archotech ATACMS + Minuteman HE (4000). Sem nuclear para a IA.
- Sprites por munição (M270 0–2, HIMARS 0–6, Mk 41 0–4, silo 0–1, Patriot 0–4).

## O que testar
1. Minuteman/ATACMS/HIMARS/Tomahawk acertando perto do alvo no mapa-mundo (erro ≤ circularError).
2. M270 sem energia não dispara.
3. HIMARS (salva, sprite), Mk 41 (topo fixo), M777 (com marcador), ogivas cluster/termobárica/EMP (colmeia mecanoide e assentamento humano).
4. Patriot interceptando a retaliação; retaliação com mísseis de facção espacial.

## Próximos passos
- Vanilla: sistema próprio de tiro no mapa-mundo.
- Remote do git.
