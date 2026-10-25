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
| Zona | Faixa de R | Sobrepressão | Prédios (HP direto) | Pawns (dano Bomb total / golpes) | Itens | Plantas | Fogo |
|---|---|---|---|---|---|---|---|
| Núcleo | 0–0,25 | ≥ ~22 psi | destruídos | 400–500 / 5 | destruídos | destruídas | 30% |
| Severa A | 0,25–0,37 | 22–10 psi | −900 (plastaço 840 cai) | 150–250 / 4 | 90% destruídos | destruídas | 10% |
| Severa B | 0,37–0,50 | 10–5,8 psi | −600 (plastaço sobrevive) | 80–150 / 3 | 70% destruídos | destruídas | 10% |
| Pesada | 0,50–0,75 | 5,8–3 psi | −250 (madeira 195 cai; pedra trinca) | 30–60 / 2 | 25% destruídos | 50% | 0 |
| Moderada | 0,75–1,0 | 3–2 psi | −100; portas −75% do HP | 10–25 / 1 | −10–20% HP | 10% | 0 |

Paredes vanilla (HP base 300 × fator do material): madeira ~195, aço 300, pedra 360–510, plastaço 840.

## Termobárico
- Mesmo núcleo; anéis até 0,55R e 0,82R (pico de sobrepressão ao ar livre só ~1,0–1,4× TNT; raio cresce no máximo 1,4^(1/3) ≈ 1,12×).
- **Confinamento**: células com teto antes da explosão levam 1,5× dano (prédios e pawns) nas zonas 1–4 — escolha de jogo; as fontes dizem que o efeito é muito maior em espaços fechados, mas não dão número.
- Fogo 100/60/60/25/5% por zona, até 200 focos. Pawns levam dano `Thermobaric`.

## Regras de desempenho (a primeira versão travou o jogo)
- Todo teto não-espesso do raio é arrancado antes (regra de jogo: uma laje real de concreto resistiria a 2 psi).
- Prédios/itens/plantas perdem HP direto (também evita o multiplicador de prédios do dano Bomb).
- Rocha natural intocada; munição do CE na área some (sem cook-off); cada coisa processada uma vez; cratera só se couber.

## Mísseis com este modelo
Minuteman III HE (R 24,9) e termobárico (24,9), ATACMS HE (11,9) e termobárico (12,9), GMLRS (7,4), Tomahawk (17,9). Não usam: cluster (submunições), EMP (fórmula do CE no mapa-mundo), nuclear (Rimatomics).

## Fontes
- Swisdak 1994, *Simplified Kingery Airblast Calculations*: https://apps.dtic.mil/sti/pdfs/ADA526744.pdf
- Implementação de Kingery-Bulmash: https://pypi.org/project/kingery-bulmash/ · https://github.com/fcento100/kingery-bulmash
- NIOSH/CDC, *Explosions and Refuge Chambers* (tabela de Glasstone & Dolan, Sartori, TM 5-1300): https://Cdc.gov/niosh/docket/archive/pdfs/NIOSH-125/125-ExplosionsandRefugeChambers.pdf
- OTA, *The Effects of Nuclear War*, efeitos de blast: https://atomicarchive.com/resources/documents/effects/ota/chapter-2-2-2.html
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
