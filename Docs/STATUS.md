# Status (atualize sempre que o usuário testar algo)

> **RimWorld 1.6 e 1.7 (25/out/2026):** conteúdo comum em `1.6/`; DLL por versão em `Bin/1.6` e `Bin/1.7`, as duas com o código atual (lote 11), compiladas juntas pelo `build.ps1` (SDK .NET 10). A 1.7 compila contra o CE 1.6 com `tools/unity_facades.ps1`. Auditoria, riscos e toolchain: `MountedArms/Docs/MIGRATION_1_7.md`. Contexto anterior: `MountedArmsStrategic/Docs/HANDOFF.md`. **Confirmado em jogo (26/out): carrega e funciona no 1.7 com o CE 16.7.4.**


Ambiente: RimWorld 1.6 ou 1.7, Harmony, Combat Extended 16.7.4, Dubs Rimatomics (opcional). DLL: `build.ps1` **com o jogo fechado** (o jogo trava a DLL; `-Out <pasta>` só para validar a compilação). Gera `Bin/1.6/Assemblies` e `Bin/1.7/Assemblies` de uma vez (`-Version` para só uma).

## Confirmado em jogo (out/2026)
- Carregamento sem erros, aba de pesquisa, construção, receitas no injector do CE.
- **M270 (ATACMS)** `MAS_Turret_TacticalLauncher`: tiro local e no mapa-mundo; salva de 2 com dispersão só no 2º míssil (`recoilAmount` 1,2; o recuo do CE é 0 no 1º tiro da rajada). Esse era o comportamento antes do lote 11; desde 25/out, nas duas versões, cada míssil cai num ponto próprio dentro do `circularError` 3.
- **Minuteman III (nuclear, com Rimatomics)**: explosão nuclear e fogo do próprio Rimatomics funcionando (o fallout só existe desde o lote 12, ainda não testado).
- **Retaliação do CE**: a facção atacada revida com morteiros no mapa do jogador.
- **Lote 5**: botão Descarregar (troca de ogiva pelo menu de recarga do CE + Descarregar), nomes reais, ataque a mapa carregado **sem marcador** nos lançadores guiados, raios de explosão em escala.

## Lote 11 (testado na 1.7 em 26/out: itens 1, 2, 3 e 5 confirmados; 4 pendente; 1.6 não testada)
Relato do usuário depois do lote 10: paredes com 1 de HP sobreviviam a vários mísseis; a salva da HIMARS caía muito junta; a remoção direta nos anéis parecia "apertar Delete".
- **Paredes com 1 HP**: causa no nosso código, não no CE (vale igual na 1.6). Toda parede, coluna ou porta a até 6,9 células de teto de montanha ficava com no mínimo 1 de HP para sempre. Agora (escolha do usuário: desabamento real) o golpe letal fica guardado e elas caem juntas no fim da onda, e o desabamento vanilla roda uma vez no tick seguinte. A cratera não sai sob teto de montanha.
- **Dano real fora do núcleo** (decisão do usuário): o núcleo continua sendo vaporização (remoção direta + cratera); nos anéis, prédios/itens/plantas levam `TakeDamage` com o dano novo `MAS_BlastWave` (fatores 1, sem som, sem sujeira, sem fragmentos de parede do CE), prédios ±20%. O teto continua arrancado à frente da onda.
- **Munição e explosivos** (inclusive o boomshroom do Odyssey, que explode ao morrer) só podem explodir nos últimos 3–8% do raio. Mais para dentro, itens somem, prédios explosivos viram destroços sem explodir (`destroyedThroughDetonation`) e boomshrooms morrem sem explodir. Detalhes em `Docs/BLAST.md`.
- **Salva da HIMARS**: o CE trava a mira da rajada inteira no 1º tiro (estimativa de distância e erro circular), e só o `ShotSpread` 0,6° variava por foguete, então a salva caía num grupo apertado cujo centro variava. Fonte: `Verb_LaunchProjectileCE.ShiftTarget`/`LockRotationAndAngle`. Agora `Verb_ShootGuidedCE.ShiftTarget` manda cada míssil num alvo local para um ponto próprio, uniforme dentro do `circularError` do ponto mirado (sem spread, recuo nem erro de distância), limitado ao alcance da carga. O lead do CE em pawns em movimento continua. HIMARS `circularError` 2 → 7 (o raio da explosão do GMLRS é 7,4). Vale nas duas versões desde 25/out (o `Bin/1.6` foi recompilado com o SDK; a `1.6/Legacy1.6`, que mantinha 2 na 1.6, foi apagada). Vale também para M270 (3), Mk 41 (1) e Minuteman (4); o `recoilAmount`/`ShotSpread` deles não contam mais. O tiro no mapa-mundo não muda.
- **Build (25/out)**: `build.ps1` na raiz compila as duas versões com o SDK .NET 10 (`Source/MountedArmsStrategic.csproj`, alvos `net472`/`net10.0`, `Krafs.Rimworld.Ref`). Na 1.7 gera antes as facades `UnityEngine.*` → `UnityStubs` (`tools/unity_facades.ps1`, em `obj/`) para o compilador aceitar o CE 1.6. Pelo dump de metadados, o 1.7 novo é equivalente ao que o csc gerava, e o 1.6 tem a mesma superfície do 1.7. A DLL de 1.7 referencia `UnityStubs` e só roda na 1.7.
- Revisão independente (3 revisores + verificador por achado; 4 de 5 confirmados, todos corrigidos): boomshroom explodindo nos anéis; paredes de montanha do núcleo deixavam destroços no núcleo (agora caem antes da varredura final); `circularError` 7 piorava a 1.6; o lead em alvo móvel tinha se perdido.
- Teste na 1.7 com o CE 16.7.4 (usuário, 26/out):
  1. Carrega sem erros: **confirmado**.
  2. Base comum (anéis com dano visível, prédios danificados em vez de sumirem, núcleo vaporizado com cratera): **confirmado**.
  3. Base escavada em montanha (paredes caem, a montanha desaba uma vez no fim da onda, sem travar): **confirmado**.
  4. Paiol no raio só explode na borda: **pendente** (o usuário vai validar).
  5. Salva da HIMARS espalhada até ~7 células: **confirmado**.

## Lote 12 (implementado, NÃO testado): fallout do ICBM nuclear
- Relato do usuário (26/out, mapa de superfície na 1.7): a explosão do Minuteman nuclear acontece, mas a condição `NuclearFallout` não começa.
- **Causa** (lida no IL do Rimatomics 1.6 e 1.7, que são iguais nesse ponto): o `NuclearStrike.StartStrike` só registra o `NuclearFallout`, e só faz a explosão completa em long events (tela "Detonating"), com `Yield > 150`. Com o nosso `Yield 55` (o da ogiva solta, `Item_FissionWarhead`) ele faz só o `GoBang`, sem fallout. Isso valia também na 1.6, ou seja, nunca foi problema da 1.7. O silo do próprio Rimatomics usa 250.
- **Correção** (escolha do usuário: manter 55, sem tela, com fallout realista): `NuclearFallout.Start` registra o `NuclearFallout` no impacto e anota o `uniqueID` dele num `MapComponent_NuclearFallout`. Dois patches Harmony (por nome, ignorados sem Rimatomics) trocam o `GameConditionTick`/`DoCellSteadyEffects` **só do nosso fallout**; o do silo do Rimatomics fica como é.
  - Dura 2 dias. Taxa de dose pela regra 7-10 do fallout real: cheia na 1ª hora, depois t^-1,2 (10% em H+7, 1% em H+48).
  - Radiação pelo `DubUtils.applyRads` do Rimatomics (respeita a opção `RadiationStrength` dele), a cada 250 ticks, em pawns de carne em célula ao ar livre. Pico de 0,45 de radiation sickness por hora: cerca de 3 h fora no começo chega a 0,9, onde o Rimatomics passa a radiação fatal; 1 h fora dá ~0,45; ficar fora do dia 1 ao dia 2 dá só ~0,15.
  - Plantas morrendo e comida apodrecendo como no Rimatomics, escalados pela taxa. Céu escuro e cinza caindo iguais aos dele.
  - Sem toxic buildup: o do Rimatomics multiplica pela `ToxicEnvironmentResistance` (e não por 1 − ela), então já era 0 para quase todos.
  - Outra bomba sobre o nosso fallout volta a taxa ao máximo por mais 2 dias. Se o fallout ativo for do Rimatomics (10–30 dias, constante), não mexemos. Como ele, encerra o `RadioactiveFallout` ativo.
- Compilado nas duas versões e instalado em `Bin/1.6` e `Bin/1.7` (26/out).
- Teste pendente:
  1. Minuteman nuclear num mapa de superfície: fallout começa, sem tela "Detonating".
  2. Colono fora nas primeiras horas: radiation sickness sobe rápido (contador Geiger do Rimatomics); dentro de casa, nada.
  3. Depois de ~1 dia o ganho fica pequeno; a condição acaba em 2 dias.
  4. Salvar e carregar durante o fallout: continua decaindo de onde estava.

## Lote 10 (implementado, NÃO testado)
- Teste do lote 9: zonas OK, mas impacto travava ~1 s, sem efeito visual, e tudo aparecia destruído de uma vez (tudo era feito num tick). Agora a explosão é uma **onda de choque em ticks** (`BlastWave`, `HeavyBlastWaves`): frente de 0 a R em 6–25 ticks, até 150 coisas por tick, teto arrancado 7 células à frente, efeito de explosão vanilla em cada célula + fumaça/poeira. Ver `Docs/BLAST.md`.
- Revisão independente (2 revisores + verificador; 2 de 7 achados confirmados): paredes/colunas que seguram teto de montanha ficam com ≥ 1 HP (antes desabava a montanha); pawns que andam para uma célula já processada levam o dano no fim da onda.

## Lote 9 (testado: zonas/cook-off/rocha OK; travamento e falta de efeito → lote 10)
- **Explosões por zonas de sobrepressão reais** (`Docs/BLAST.md`): núcleo 0,25R destruído; anéis 0,37/0,50/0,75/1,0R com prédios −900/−600/−250/−100 HP (portas −75% na borda), dano em pawns por zona, itens/plantas por chance, fogo por zona; termobárico com anéis maiores (0,55/0,82R), 1,5× dentro de lugares cobertos e fogo 100→5%. R = contorno de 2 psi. Aplicado em Minuteman HE/termobárico, ATACMS HE/termobárico, GMLRS e Tomahawk (antes só o Minuteman usava a explosão própria; os outros usavam a do CE com fragmentos).
- `HeavyBlastExtension` agora só tem `radius`, `thermobaric`, `damageDef`, `fxRadius`, `crater` (o dano por zona está no código, com as tabelas da pesquisa).
- Correções da revisão independente (2 revisores + verificador; todos os achados confirmados): rocha alisada/desmoronada/minério também intocados (seguravam teto de montanha); itens que explodem ao morrer somem sem explodir; cratera não apaga rocha nem edifícios e exige terreno construível; prédios danificados entram na lista de reparo; 3ª passada no núcleo (cadáveres e equipamento); fogo embaralhado antes do limite; som do próprio projétil; **cook-off de mísseis estocados** (o CE fazia a explosão gigante do XML direto, que travava o jogo) agora é uma explosão pequena via patch em `AmmoThing.TryDetonate`.

## Lote 8 (implementado, NÃO testado)
- **Jogo travou no Minuteman III convencional** (log: dezenas de "Tried to destroy already-destroyed thing" em paredes/móveis/Sandstone e "Tried to place thing ChunkSandstone… in a null map", sem pilha). Causa provável: a explosão destruía paredes sob teto construído (desabamento em cascata esmagando o que já estava sendo destruído) e matava rocha natural (pedaços em células já desocupadas, teto de montanha caindo). `HeavyBlast` agora: passada 1 arranca todo teto não-espesso do raio; passada 2 destrói; rocha natural intocada; prédios/itens/plantas perdem HP direto (sem o pipeline de dano do CE); só pawns tomam `TakeDamage`.
- **Chama no meio do míssil**: o `CompProjectileFleck` do CE posiciona a chama em `DrawPos − deslocamento do último tick × originOffset` (depende da velocidade; na fase lenta fica no centro do sprite). Novo `CompProperties_Exhaust` (`Source/Exhaust.cs`) põe chama/fumaça a `nozzleDistance` (≈ meio comprimento do sprite) atrás da posição desenhada. Substitui o do CE em Minuteman (1,9), ATACMS (1,4), GMLRS (0,9), Tomahawk (1,3; chama só 40 ticks = booster) e PAC-3 (0,55).
- **Correções da revisão independente** (workflow com 3 revisores + verificador por achado; 8 confirmados, 2 descartados):
  - espoleta de proximidade: estado agora marcado com o `FlightTicks` da última atualização; recomeça se não for o tick seguinte (antes sobrevivia a reload do save e podia explodir interceptadores cedo);
  - inclinação do sprite na fase lenta: postfix em `ProjectileCE.DrawRotation` recalcula pela posição desenhada suavizada;
  - explosão pesada: cada coisa recebe dano uma vez (prédios de várias células levavam N vezes); cratera só se couber no mapa e não cobrir nada indestrutível; tremor de câmera só no mapa que está na tela.

## Lote 7 (implementado, NÃO testado)
- **Lançamento realista** (`Docs/LAUNCH.md`): `BoostPhaseTrajectoryWorker` + `LaunchProfileExtension` — sobe devagar e acelera (mesmo caminho/tempo/impacto), fumaça e brilho na saída; Tomahawk com lançamento a frio. Minuteman 0,15 · ATACMS 0,05 · GMLRS 0,03 · Tomahawk 0,10; PAC-3 só efeitos.
- **Explosão pesada sem a tela "Detonando"** (roda na hora) e **munição estocada na área some sem cook-off** (a cadeia de cook-off do CE é o que gera milhares de fragmentos numa base com munição; paredes não têm fragmentos no CE). Minuteman HE sem comp de fragmentos (a detonação própria não usa).
- **Menos fragmentos** nos mísseis que usam a explosão do CE: ATACMS HE 20/30 → 8/12, GMLRS 10/20 → 4/8, Tomahawk 20/40 → 8/16.
- Texto das configurações sem a frase "That is why early on...".

## Confirmado (lote 6)
- Configurações do mod, retaliação ajustável e espoleta de proximidade dos interceptadores: funcionando. Minuteman nuclear (Rimatomics): sem tela e sem queda de frames; destruiu uma base 36x36 inteira (efeito do `NuclearStrike` do Rimatomics com Yield 55).

## Lote 6 (implementado)
- **Detonação estilo Rimatomics** (`ProjectileCE_HeavyBlast` + `HeavyBlastExtension`, `Source/HeavyBlast.cs`) no Minuteman HE (r24,9, 8000) e termobárico (r24,9, 5000, dano Thermobaric, fogo 30%): roda em `LongEventHandler.QueueLongEvent` ("Detonando"), destrói direto o núcleo (35% do raio; rocha natural fica), uma passada de `TakeDamage` com queda até 15% na borda, retira teto fino no núcleo, cratera, explosão pequena só para efeito/som, tremor de câmera. Sem `ExplosionCE` nem fragmentos. Mundo inalterado (damage manual).
- **Configurações do mod** (Opções → Configurações de mods → "Mounted Arms: Strategic", `Source/Settings.cs`): texto explicando a retaliação; barra do orçamento 0–500% (Harmony prefix em `HostilitySheller.TryStartShelling`, multiplica `points`; vale para todos os projéteis, morteiros do CE incluídos; 0% desliga o bombardeio de retaliação, não os raides); caixa "facções podem retaliar com Minuteman nuclear" (só com Rimatomics; adiciona/remove o nuclear em CE_ShellingPreset_Spacer/Ultra/Archotech, 6000 pts, peso 0,05; o CE lê a lista ao vivo).
- **Espoleta de proximidade nos interceptadores** (Harmony postfix em `ProjectileCE_CIWS.Tick`, todos os projéteis de interceptação do CE, inclusive os do Mounted Arms): se a distância ao alvo cresce por 3 ticks após a aproximação máxima, o interceptador se destrói e libera o alvo. Motivo: o CE não deixa outra torreta mirar um projétil que tenha interceptador em voo; um PAC-3 que errou seguia voando e o Phalanx ficava sem tempo (relato do usuário).
- A DLL agora usa Harmony (já carregado pelo CE; `loadAfter brrainz.harmony` no About) e referencia `Unity.Mathematics.dll` no build.

## Ajustes desta rodada (NÃO testados)
- **Minuteman convencional derrubou o FPS no impacto** (raio 24,9, ~1950 células; centenas de paredes destruídas → recálculo de regiões/teto). Raio limitado a **19,5** (= maior explosão convencional do próprio CE, o Tomahawk), dano 8000, fragmentos 20/30; termobárico 19,5 / 5000. O nuclear é do Rimatomics e não muda.
- Log "Tried to destroy already-destroyed thing Wall..." no impacto do Minuteman e da salva do HIMARS: pilha só com código do jogo/CE (`ExplosionCE.Tick` → `ExplosionDamageThing` → `Kill`), nada nosso. Hipótese: duas explosões/fragmentos no mesmo tick atingem a mesma parede. Inofensivo; não corrigível sem patch no CE.
- **Patriot interceptou o ATACMS e o Phalanx não**: comportamento do CE (`VerbCIWS.TryFindNewTarget`): uma torreta ignora projéteis que outra CIWS já mira ou que já têm interceptador a caminho. O Patriot (alcance 119) engaja antes do Phalanx (90). Com vários projéteis, cada torreta pega um.
- **M270 consome energia** (300 W) além do operador.
- **Minuteman errava ~30+ células**. Causa (código do CE, `Verb_LaunchProjectileCE.ShiftVecReportFor(GlobalTargetInfo)` e `Verb_ShootMortarCE`): no tiro pelo mapa-mundo o CE trata a distância como ≥ 44% do alcance máximo (Minuteman: ≥ 1100 "células"), soma iluminação fixa 1 e fumaça fixa 10 e um erro de estimativa de distância proporcional à precisão; se o desvio sai do mapa, sorteia célula aleatória. Correção: verbo próprio `MountedArmsStrategic.Verb_ShootGuidedCE` (`Source/GuidedVerb.cs`) que zera luz/fumaça/clima e usa AimingAccuracy 1,5 no tiro global (sem erro de distância). Sobra só o `circularError` (≈ CEP). Aplicado em M270, HIMARS, Mk 41 e Minuteman; Minuteman `circularError` 6 → 4. M777 continua com a física de artilharia comum.

## Implementado, NÃO testado
- **M142 HIMARS** `MAS_Turret_MLRS`: tripulado 3x3, minificável, pod de 6 (troca o pod inteiro, 30 s), rajada 6 × 25 ticks, circularError 7 (era 2 até o lote 11; o ShotSpread 0,6 continua no XML, mas não conta mais), giro 20°/s. `MAS_Ammo_GMLRS` (M31): Bomb 900 r 7,4; mundo 40 tiles, 0,5 tiles/tick, damage 0,4.
- **Mk 41 VLS (Tomahawk)** `MAS_Turret_CruiseVLS`: 2x2, 300 W, sem operador, `ManualFireOnlyExtension` + `FixedTopExtension` (topo não gira), mag 4, circularError 1. `MAS_Ammo_CruiseMissile`: Bomb 5600 r 17,9; mundo 400 tiles, 0,15 tiles/tick, damage 3.
- **Minuteman III (convencional)** `MAS_Turret_ICBMSilo`: 3x3, 1000 W, sem operador, só "atacar tile do mundo", mag 1, recarga 60 s. HE: detonação própria r 24,9 / 8000; mundo 500 tiles, damage 5.
- **M777** `MAS_Turret_M777_Howitzer`: tripulado 3x3, minificável, `AmmoSet_155mmHowitzerShell` do CE (mundo 60 tiles), giro 10°/s, exige marcador em mapa carregado.
- **Ogivas especiais** (`MAS_Research_SpecialWarheads`), ATACMS / Minuteman:
  - Cluster (`ProjectileCE_Cluster`): 40 bombas em raio 12 / 100 em raio 26; mundo 1,2 / 3,5.
  - Termobárica (dano `Thermobaric` do CE): 1500 r 12,9 / detonação própria 5000 r 24,9 com fogo; mundo 1,8 / 6.
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
- **RimWorld 1.7**: testar em jogo seguindo `../MountedArms/Docs/MIGRATION_1_7.md` ("Teste a fazer"). Quando sair um CE 1.7: `build.ps1 -CE17 <CombatExtended.dll>` e rever os alvos do CE listados lá. Decidir se o `PatchAll()` de `Settings.cs` ganha try/catch por classe.
- Vanilla: sistema próprio de tiro no mapa-mundo.
- Remote do git.
