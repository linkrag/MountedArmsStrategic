# Mounted Arms: Strategic Missiles

Mod de RimWorld 1.6 e 1.7 com lançadores de mísseis balísticos que atacam o mapa local e assentamentos no **mapa-mundo**, usando o sistema de tiro no mapa-mundo do **Combat Extended** (requisito). Companheiro do Mounted Arms, sem depender dele.

- packageId: `linkrag.mountedarms.strategic` · autor: Felipe
- Aba de pesquisa própria: "Strategic Missiles"

## Conteúdo
| Item | Resumo |
|---|---|
| M270 (ATACMS) (3x3, tripulado, 300 W) | 2 mísseis, mapa local (60–700 células) ou mapa-mundo até 120 tiles |
| MGM-140 ATACMS (HE) | Explosão por zonas reais, raio 11,9; ~3 acertos destroem um assentamento industrial |
| Minuteman III silo (3x3, energia) | Só dispara por ordem no mapa-mundo, até 500 tiles; 1 míssil |
| LGM-30G Minuteman III (HE) | Explosão raio 24,9 (detonação própria, sem queda de frames); dano 5 no mapa-mundo (1–2 acertos) |
| Lançador múltiplo de foguetes (3x3, tripulado, estilo HIMARS) | Salva de 6 foguetes guiados espalhados até ~7 células do ponto; mapa local ou até 40 tiles |
| Lançador de mísseis de cruzeiro (2x2, energia, VLS) | Só por ordem; até 400 tiles; lento e muito preciso |
| Obuseiro M777 155 mm (3x3, tripulado) | Munição 155 mm do CE; até 60 tiles |
| Ogivas especiais (tático e ICBM) | Cluster, termobárica, EMP (contra colmeias mecanoides) |
| Bateria antimíssil (3x3, energia, estilo Patriot PAC-3) | Intercepta granadas de artilharia, mísseis e drop pods hostis até 119 células; 4 interceptadores, 2 por ameaça |
| Minuteman III (fissão), só com Dubs Rimatomics | Feito da ogiva de fissão do Rimatomics; explosão por zonas raio 55, pulso térmico, radiação inicial, cogumelo do Rimatomics, fallout realista de 3 dias (letal ao ar livre nas primeiras horas, decai pela regra 7-10, abrigo protege); destrói qualquer assentamento |
| Minuteman III W62 / W78, só com Dubs Rimatomics | Ogivas termonucleares: explosão por zonas raio 151 e 250 (boa parte do mapa ou o mapa todo), pulso térmico que incendeia boa parte do mapa, sem tela de carregamento; fallout mais forte |
| RS-28 Sarmat (MIRV), só com Dubs Rimatomics | Silo próprio 4×4; sobre o alvo se divide em 10 ogivas nucleares pequenas espalhadas pelo mapa todo, cada uma abatível pela defesa antimíssil |

Configurações do mod: orçamento da retaliação da IA (0–500%) e retaliação nuclear opcional (com Rimatomics).

Retaliação da IA (CE): facções espaciais podem responder com mísseis táticos; ultra/archotech também com ICBM HE. Com a opção de retaliação nuclear: Minuteman (fissão) (espacial em diante) e, só ultra/archotech, W62, W78 e Sarmat, cada vez mais caros e raros.

## Desenvolvimento
Guia para agentes: `AGENTS.md`. DLL: `powershell -File build.ps1` (SDK .NET 10, jogo fechado; gera `Bin/1.6` e `Bin/1.7`). Texturas: `python tools/gen_tex.py 1.6/Textures/MountedArmsStrategic`.
