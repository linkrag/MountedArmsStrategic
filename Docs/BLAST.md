# Explosões grandes por zonas de sobrepressão

Pesquisa feita em out/2026 com workflow (4 trilhas de pesquisa + síntese + verificador cético; o verificador aprovou com correções, já incorporadas). Implementação: `Source/HeavyBlast.cs` (`ProjectileCE_HeavyBlast` + `HeavyBlastExtension`).

## Física usada
- **Escala de Hopkinson-Cranz**: distância escalada Z = R / W^(1/3) (m/kg^(1/3)), W = massa equivalente de TNT. Sobrepressão incidente de pico pela curva de **Kingery-Bulmash** (explosão hemisférica de TNT na superfície; coeficientes do relatório simplificado de Swisdak 1994).
- Z em que a sobrepressão atinge: 100 psi 1,36 · 50 psi 1,84 · 20 psi 2,76 · 10 psi 3,88 · 5 psi 5,71 · 3 psi 7,93 · 2 psi 10,6 · 1 psi 18,1.
- Pela escala de raiz cúbica, **as razões entre esses raios não dependem do tamanho da ogiva** → o mesmo modelo de zonas serve para todos os mísseis, mudando só R.
- **R do jogo = contorno de 2 psi.** Conferido: GMLRS (~23 kg) 30 m / R 7,4, ATACMS (~100 kg) 49 m / R 11,9, carga de 1 t 106 m / R 24,9 → ~4,1–4,25 m por célula em todos. Tomahawk (R 17,9) fica 20–40% generoso (opcional reduzir para ~14–15).

## Critérios de dano (Glasstone & Dolan, TM 5-1300, OTA, FM 8-9, NIOSH)
- ~2 psi: portas e janelas arrancadas, telhados muito danificados, feridos por vidro/destroços.
- ~3 psi: casas desabam, paredes de prédios de estrutura de aço arrancadas, feridos graves comuns, algumas mortes.
- ~5 psi: a maioria dos prédios desaba; concreto armado com dano moderado a grave; mortes espalhadas.
- ~10 psi: concreto armado gravemente danificado ou demolido; a maioria das pessoas morre.
- ≥ ~20 psi: concreto armado pesado arrasado.

## Modelo no jogo (HE)
| Zona | Faixa de R | Sobrepressão | Prédios (dano `MAS_BlastWave`, ±20%) | Pawns (dano Bomb total / golpes) | Itens | Plantas | Fogo |
|---|---|---|---|---|---|---|---|
| Núcleo (vaporização) | 0–0,25 | ≥ ~22 psi | removidos direto | 400–500 / 5 | removidos direto | removidas direto | 30% |
| Severa A | 0,25–0,37 | 22–10 psi | −900 (plastaço 840 cai) | 150–250 / 4 | 90% destruídos | destruídas | 10% |
| Severa B | 0,37–0,50 | 10–5,8 psi | −600 (plastaço sobrevive) | 80–150 / 3 | 70% destruídos | destruídas | 10% |
| Pesada | 0,50–0,75 | 5,8–3 psi | −250 (madeira 195 cai; pedra trinca) | 30–60 / 2 | 25% destruídos | 50% | 0 |
| Moderada | 0,75–1,0 | 3–2 psi | −100; portas −75% do HP | 10–25 / 1 | −10–20% HP | 10% | 0 |

Paredes vanilla (HP base 300 × fator do material): madeira ~195, aço 300, pedra 360–510, plastaço 840.

- **Núcleo = zona de vaporização** (pedido do usuário): tudo menos rocha é removido direto (`Destroy`), e a cratera fica no centro.
- **Fora do núcleo, dano real** (pedido do usuário, out/2026: a remoção direta parecia "apertar Delete"): prédios, itens e plantas recebem `TakeDamage` com o dano próprio `MAS_BlastWave` (`1.6/Defs/DamageDefs.xml`). Todos os fatores dele ficam em 1 (o Bomb é ×4 em prédios e plantas), então o HP perdido é exatamente o da tabela. Não tem som de impacto, não gera sujeira e não solta fragmentos de parede do CE (`canCauseWallFragmentation` false). Por isso os prédios entram na lista de reparo e a facção dona é avisada normalmente. O dano em prédios varia ±20% por coisa, para o anel não sair cortado em degrau. "Destruído" nos itens e plantas = dano igual ao HP (planta deixa toco).
- Pawns continuam levando Bomb (ou o `damageDef` da ogiva).

## Termobárico
- Mesmo núcleo; anéis até 0,55R e 0,82R (pico de sobrepressão ao ar livre só ~1,0–1,4× TNT; raio cresce no máximo 1,4^(1/3) ≈ 1,12×).
- **Confinamento**: células com teto antes da explosão levam 1,5× dano (prédios e pawns) nas zonas 1–4 — escolha de jogo; as fontes dizem que o efeito é muito maior em espaços fechados, mas não dão número.
- Fogo 100/60/60/25/5% por zona, até 200 focos. Pawns levam dano `Thermobaric`.

## Regras de desempenho (a primeira versão travou o jogo)
- **Onda de choque espalhada no tempo** (`BlastWave` + `HeavyBlastWaves`, um MapComponent): o clarão e o som saem no impacto; a frente cresce do centro até R em 6–25 ticks (~0,1–0,4 s) e cada tick processa as células que ela passou, no máximo 150 coisas e 600 células por tick (base densa ou raio nuclear deixam a frente mais lenta, não o quadro; as células vêm de um retângulo recortado pelo mapa, porque o `GenRadial` vai só até ~r120). Cada célula gera o efeito de explosão vanilla do tipo de dano (`explosionCellFleck`, cor do centro para a borda), mais fumaça/poeira sorteadas e brilho de fogo no termobárico. Antes, tudo num tick só: ~1 s de travamento, sem efeito visual, e tudo aparecia destruído de uma vez.
- A onda não é salva: um save feito no meio dela perde o resto (dura menos de meio segundo).
- Pawns no raio no momento do impacto são registrados; quem entrar numa célula já processada leva o dano da zona onde estava, no fim da onda.
- Paredes, colunas e portas construídas que seguram teto de montanha (teto espesso a até 6,9 células): um golpe que não mata é aplicado na hora. Um golpe letal (sempre no núcleo) fica guardado, e elas caem todas juntas no fim da onda, antes da varredura final do núcleo (que limpa os restos delas). O desabamento vanilla roda uma vez, no tick seguinte (`MapPreTick`): rocha desmoronada cai e esmaga o que estiver embaixo, com uma carta só. Antes elas ficavam com no mínimo 1 de HP e sobreviviam a qualquer número de mísseis (relato do usuário, out/2026).
- Todo teto não-espesso do raio é arrancado, sempre 7 células à frente da onda (a sustentação de teto vanilla alcança 6,9), então nenhuma parede cai sob teto que ainda existe (regra de jogo: uma laje real de concreto resistiria a 2 psi).
- Rocha intocada: natural, alisada, desmoronada e minério (`mineable`/`isNaturalRock`/`isResourceRock`), porque seguram o teto de montanha, que não é arrancado.
- **Munição e explosivos** (munição do CE e tudo que explode ao morrer: químico, torretas, IEDs, geradores, `CompExplosive`/`CompExplosiveCE`, também dentro de caixotes de minificado, e o boomshroom do Odyssey, cujo `Kill` solta chamas) só podem explodir nos últimos 3–8% do raio (sorteado por coisa). Ali levam o dano normal da zona e podem ou não reagir: cook-off do CE, pavio do vanilla. Mais para dentro, sem reação em cadeia: itens somem (consumidos); prédios perdem HP direto (o `TakeDamage` acenderia o pavio) e, se o golpe for letal, viram destroços sem explodir (`destroyedThroughDetonation`); boomshrooms perdem HP direto e morrem por `Destroy`, que não chama o `Kill`.
- Cada coisa é processada uma vez; uma 3ª passada no núcleo pega o que surgiu durante a 2ª (cadáveres, equipamento caído, conteúdo ejetado).
- Fogo: as células sorteadas são embaralhadas antes do limite de 200, para os anéis externos também queimarem.
- Prédios explosivos que perdem HP direto entram na lista de reparo à mão (`listerBuildingsRepairable`).
- Cratera só se couber: dentro do mapa, em terreno que suporte construção, sem nenhum edifício que tenha sobrado nem rocha embaixo, e sem teto de montanha em nenhuma célula (o desabamento cairia em cima dela).
- **Cook-off**: o `AmmoThing.TryDetonate` do CE usa direto o `explosionRadius`/dano do projétil (não passa por esta classe). Um patch troca isso, nestes mísseis, por uma explosão pequena (`fxRadius`, 60 de Bomb), com a mesma chance do CE (quanto mais danificada a pilha, mais provável). O `explosionRadius` do XML fica, porque o CE o usa para o círculo de mira.
- Som: usa o `soundExplode` do projétil (o GMLRS toca o som de morteiro).
- Fora do núcleo o dano em prédios passa pelo aviso normal à facção, mas **sem instigador**, para uma onda não gerar centenas de perdas de goodwill e mensagens. Uma perda única de goodwill por facção atingida ficou como ideia.

## Mísseis com este modelo
Minuteman III HE (R 24,9) e termobárico (24,9), ATACMS HE (11,9) e termobárico (12,9), GMLRS (7,4), Tomahawk (17,9); nucleares (`ProjectileCE_NuclearBlast`): Minuteman (fissão) 55, W62 151, W78 250 (R = o yield do Rimatomics, raio onde o dano dele `Yield × ln(Yield / d)` zerava) e ogivas do Sarmat 29,9. Não usam: cluster (submunições), EMP (fórmula do CE no mapa-mundo).

## Nucleares: escala, pulso térmico e radiação inicial
**Escala.** Com ~4,1 m por célula e o contorno de 2 psi nuclear em ~0,74 km × Y^(1/3) (Y em kt, explosão aérea; 20 kt: 5 psi 1,2 km, 1 psi 3 km), o R de cada ogiva equivale a:

| Ogiva | R (2 psi) | Yield equivalente no mapa | Yield real | 2 psi real |
|---|---|---|---|---|
| Ogiva do Sarmat | 29,9 (123 m) | ~5 t | Sarmat: ~10 × 300–750 kt (estimado) | ~1 000+ células |
| Minuteman (fissão) | 55 (225 m) | ~0,03 kt (classe W54/Davy Crockett) | não existe ogiva de fissão pura no Minuteman | — |
| W62 | 151 (620 m) | ~0,6 kt | 170 kt | ~4 km ≈ 950 células |
| W78 | 250 (1 025 m) | ~2,7 kt | ~335 kt | ~5 km ≈ 1 200 células |

Ou seja: as nucleares são comprimidas **~6× em distância (~250× em energia)** para caber num mapa de 250–400 células. Com o yield real, só o anel de ≥ 20 psi (concreto arrasado) da W62 já teria ~1,1 km ≈ 260 células: o mapa inteiro seria núcleo. Escolha de jogo; o resto do modelo (proporções entre zonas, pulso térmico, radiação, fallout) segue a física **do yield equivalente**, então fica coerente dentro do mapa.

**Pulso térmico** (`NuclearPulse`, no instante da explosão, antes da onda arrancar os tetos): ~35% da energia sai como radiação térmica; a exposição cai com 1/d² (Q ≈ 2,8·10⁶ · Y · T / d², cal/cm², d em m, transmitância T ~0,9). Queimadura de 3º grau e ignição de combustível fino (capim seco, folhas, papel) em ~5–6 cal/cm² nesses yields pequenos. `thermalRadius` = esse contorno; q = (thermalRadius / d)²:
- pawn ao ar livre (sem teto) e com linha de visão até a explosão: q ≥ 4 carbonização (120–180 de Burn em 5 golpes), q ≥ 1 3º grau (50–80 em 4), q ≥ 0,5 2º grau (15–30 em 3). Roupa protege pelo armor de calor do CE; qualquer teto ou parede faz sombra;
- fogo: células ao ar livre sorteadas até q = 0,5 (1,41 × thermalRadius), chance 50% com q ≥ 1 e 15% abaixo, só onde há algo inflamável (`FireUtility.TryStartFireIn`), até `maxFires`;
- a fração do raio cresce com o yield (∝ Y^(1/6)): é por isso que o fogo pesa pouco na fissão e cobre o mapa na W78, como na realidade (acima de ~1 kt o térmico passa o raio de 5 psi).

**Radiação inicial** (gama e nêutrons do instante): dose ∝ e^(−d/λ) / d², λ ≈ 250 m de ar (61 células), calibrada em ~500 rem a 1,2–1,3 km em 15–20 kt. `radiationRadius` = onde a dose sem proteção chega à DL50 (~450 rem = 0,9 de radiation sickness do Rimatomics, que vira `FatalRad`); teto comum ×0,5 (fator de proteção de casa ~2), teto de montanha ×0,001; máximo 2,0 por pawn. Em yields pequenos ela é a maior assassina (passa o raio de 2 psi: a W54 matava por radiação a ~400 m); acima de alguns kt fica dentro da zona de blast letal.

| Ogiva | thermalRadius | radiationRadius | maxFires |
|---|---|---|---|
| Sarmat (por ogiva) | 12 (0,39R) | 44 (1,47R) | 40 |
| Minuteman (fissão) | 29 (0,53R) | 80 (1,46R) | 150 |
| W62 | 126 (0,83R) | 171 (1,13R) | 400 |
| W78 | 257 (1,03R) | 229 (0,92R) | 600 |

O que **não** é modelado: bola de fogo separada (na escala equivalente ela teria 4–15 células, dentro do núcleo de 0,25R), tempestade de fogo (fica com o fogo vanilla se espalhando), pulso eletromagnético, cegueira por flash, efeitos sobre rocha e montanha (rocha natural fica intocada por regra de jogo).

## Fontes
- Swisdak 1994, *Simplified Kingery Airblast Calculations*: https://apps.dtic.mil/sti/pdfs/ADA526744.pdf
- Implementação de Kingery-Bulmash: https://pypi.org/project/kingery-bulmash/ · https://github.com/fcento100/kingery-bulmash
- NIOSH/CDC, *Explosions and Refuge Chambers* (tabela de Glasstone & Dolan, Sartori, TM 5-1300): https://Cdc.gov/niosh/docket/archive/pdfs/NIOSH-125/125-ExplosionsandRefugeChambers.pdf
- OTA, *The Effects of Nuclear War*, efeitos de blast: https://atomicarchive.com/resources/documents/effects/ota/chapter-2-2-2.html
- Glasstone & Dolan, *The Effects of Nuclear Weapons* (1977), cap. 7 (radiação térmica, exposições para queimaduras e ignição) e cap. 8 (radiação inicial): https://www.fourmilab.ch/etexts/www/effects/
- Ogivas: W62 https://en.wikipedia.org/wiki/W62 · W78 https://en.wikipedia.org/wiki/W78 · W54 https://en.wikipedia.org/wiki/W54 · RS-28 https://en.wikipedia.org/wiki/RS-28_Sarmat
- US Army FM 8-9, Parte I, cap. 4: https://nuke.fas.org/guide/usa/doctrine/dod/fm8-9/1ch4.htm
- HYDESim (dados de Glasstone & Dolan): https://mail.meyerweb.com/eric/tools/gmap/hydesim.html
- CDC, lesões por explosão: https://stacks.cdc.gov/view/cdc/28987
- DoD DESR 6055.09: https://www.denix.osd.mil/ddes/denix-files/sites/32/2021/08/DESR-6055.09-Edition1.pdf
- Ogiva unitária GMLRS (GD-OTS): https://www.gd-ots.com/wp-content/uploads/2021/08/GMLRS-Unitary-Warhead-202007.pdf
- ATACMS: https://en.wikipedia.org/wiki/MGM-140_ATACMS · WDU-18 (Harpoon): https://designation-systems.net/dusrm/m-84.html
- Tomahawk: https://en.wikipedia.org/wiki/Tomahawk_missile
- Conventional Prompt Global Strike (CRS RL33067): https://www.everycrsreport.com/reports/RL33067.html
- Variabilidade de equivalência TNT: https://pmc.ncbi.nlm.nih.gov/articles/PMC12050314 · https://www.msiac.nato.int/publication/o-193-how-to-determine-tnt-equivalency-for-energetic-materials-and-munitions/
- Termobárico: Zhao et al. 2016 https://bzycj.cn/en/article/doi/10.11883/1001-1455(2016)01-0038-05 · HRW 2000 https://www.hrw.org/report/2000/02/01/backgrounder-russian-fuel-air-explosives-vacuum-bombs · Andrew 2003 https://jmvh.org/wp-content/uploads/2022/10/Volume-14-No-2-David-Andrew-Thermobaric-Munitions.pdf · https://en.wikipedia.org/wiki/Thermobaric_weapon
