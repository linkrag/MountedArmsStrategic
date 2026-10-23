# Mounted Arms: Strategic Missiles

Mod de RimWorld 1.6 com lançadores de mísseis balísticos que atacam o mapa local e assentamentos no **mapa-mundo**, usando o sistema de tiro no mapa-mundo do **Combat Extended** (requisito). Companheiro do Mounted Arms, sem depender dele.

- packageId: `linkrag.mountedarms.strategic` · autor: Felipe
- Aba de pesquisa própria: "Strategic Missiles"

## Conteúdo
| Item | Resumo |
|---|---|
| Lançador de mísseis táticos (3x3, tripulado) | Estilo ATACMS/Iskander. 2 mísseis, mapa local (60–700 células) ou mapa-mundo até 120 tiles |
| Míssil balístico tático (HE) | Explosão raio 9,9, cratera; ~3 acertos destroem um assentamento industrial |
| Silo de ICBM (3x3, energia) | Só dispara por ordem no mapa-mundo, até 500 tiles; 1 míssil |
| ICBM (HE) | Explosão raio 14,9; dano 5 no mapa-mundo (1–2 acertos) |
| Lançador múltiplo de foguetes (3x3, tripulado, estilo HIMARS) | Salva de 6 foguetes guiados; mapa local ou até 40 tiles |
| Lançador de mísseis de cruzeiro (2x2, energia, VLS) | Só por ordem; até 400 tiles; lento e muito preciso |
| Obuseiro M777 155 mm (3x3, tripulado) | Munição 155 mm do CE; até 60 tiles |
| Ogivas especiais (tático e ICBM) | Cluster, termobárica, EMP (contra colmeias mecanoides) |
| Bateria antimíssil (3x3, energia, estilo Patriot PAC-3) | Intercepta granadas de artilharia, mísseis e drop pods hostis até 119 células; 4 interceptadores, 2 por ameaça |
| ICBM (nuclear), só com Dubs Rimatomics | Usa a ogiva de fissão e a explosão/fallout do próprio Rimatomics; destrói qualquer assentamento |

Retaliação da IA (CE): facções espaciais podem responder com mísseis táticos; ultra/archotech também com ICBM HE (nunca nuclear).

## Desenvolvimento
Guia para agentes: `AGENTS.md`. Texturas: `python tools/gen_tex.py 1.6/Textures/MountedArmsStrategic`.
