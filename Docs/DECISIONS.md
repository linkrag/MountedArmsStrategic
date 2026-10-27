# Decisões

## Do usuário (Felipe)
- **Mod separado** do Mounted Arms (out/2026): escopo estratégico, conteúdo só-CE no início.
- Fazer **os dois**: lançador de míssil balístico tático e silo de ICBM.
- Ogivas do ICBM: convencional **e** nuclear (perguntou "tem como?"). Depois apontou que o **Dubs Rimatomics** já tem armas nucleares → decidiu: **nuclear integrada ao Rimatomics** (a ogiva nuclear só existe com o Rimatomics ativo, é feita da ogiva de fissão dele e usa a condição `NuclearFallout` dele). Sem Rimatomics, o ICBM só tem ogivas convencionais.

- **Defesa antimíssil** e **IA retaliando com mísseis por nível** (Spacer: tático; Ultra/Archotech: tático + ICBM HE; nuclear só com a opção nas configurações). A bateria também intercepta drop pods.

- Lote 3 pedido pelo usuário ("pode fazer todos"): salva (rajada + seleção múltipla), MLRS, míssil de cruzeiro, ogivas cluster/termobárica/EMP, M777.

- Nomes reais nos labels (M270/ATACMS, HIMARS/GMLRS, Mk 41/Tomahawk, Minuteman III, Patriot/PAC-3, M777); defNames mantidos.
- M270 (ATACMS) precisa de **energia** além do operador.
- Mísseis guiados devem errar pouco: o erro fica só no `circularError` (CEP), sem penalidades de luz/fumaça/clima/estimativa de distância do CE.
- Lançadores guiados atacam mapa carregado sem marcador; o M777 (não guiado) continua exigindo.

- Retaliação ajustável pelo jogador numa tela de configurações (barra do orçamento + retaliação nuclear opcional), em vez de custos fixos.
- Interceptação em camadas: em vez de várias torretas no mesmo alvo, o interceptador que errou se autodestrói e libera o alvo para a próxima camada.

- Sem tela "Detonating" em nenhuma explosão (nem nas nucleares); manter os specs e reduzir fragmentos. Lançamentos mais realistas (pesquisa em `LAUNCH.md`). Avenger intercepta também projéteis.
- MDs atualizados após toda edição (regra do usuário).
- RimWorld 1.6 e 1.7 (25/out/2026): manter as duas versões; SDK .NET 10 instalado para compilar.

- Explosões grandes seguem zonas reais de sobrepressão (pesquisa em `BLAST.md`), mantendo as regras de desempenho (teto arrancado no raio todo).

- Explosão grande (out/2026, lote 11): núcleo = vaporização (remoção direta + cratera); anéis com dano real (`TakeDamage`); munição e explosivos só reagem nos últimos 3–8% do raio. Paredes que seguram teto de montanha caem e a montanha desaba de verdade (antes ficavam com 1 de HP para sempre).
- Salva de foguetes guiados espalhada: cada míssil cai num ponto próprio dentro do `circularError` (HIMARS 7).

## Técnicas
- Defs CE nativos (o mod exige CE), herdando das bases do CE.
- Projétil de artilharia: speed 0 + gravityFactor 5 (lição da bateria do Mounted Arms).
- Lançador tático sem raio de colocação e `ai_combatDangerous` false (alcance 700).
- `circularError` 3 e `indirectFirePenalty` 0,05: míssil guiado, mais preciso que o morteiro do CE (3 / 0,3).
- `Verb_ShootGuidedCE` (nosso, deriva de `Verb_ShootMortarCE`) para M270, HIMARS, Mk 41 e Minuteman; explicação em `CE_WORLD_SHELLING.md`.
- Explosão convencional acima de ~r20: usar `ProjectileCE_HeavyBlast` (hoje uma onda de choque espalhada em ticks; ver `BLAST.md`), não a `ExplosionCE` do CE, que em r24,9 derrubava o FPS.
- Dano da explosão grande em prédios/itens/plantas com um DamageDef próprio (`MAS_BlastWave`, fatores 1) em vez do Bomb (×4 em prédios e plantas, som de impacto, sujeira e fragmentos de parede do CE).
- DLL por versão em `Bin/<versão>/Assemblies`, conteúdo comum em `1.6/`; na 1.7, compilar contra o CE 1.6 com facades `UnityEngine.*` → `UnityStubs`. Build único das duas versões com o SDK .NET 10 (`build.ps1`, alvos `net472`/`net10.0`, referências `Krafs.Rimworld.Ref`), o mesmo esquema do Mounted Arms.
- Dano no mapa-mundo 1,5: ~3 acertos num assentamento industrial (cálculo em `CE_WORLD_SHELLING.md`).

- Fallout do ICBM nuclear (26/out, escolha do usuário): registrar o `NuclearFallout` nós mesmos, com formato realista, conferido com fontes: chegada em ~30 min, 3 dias, taxa de dose t^-1,2 (regra 7-10), letal ao ar livre nas primeiras horas e proteção por abrigo (cômodo 1/5, sob montanha quase total). Só o fallout do nosso míssil decai; o do silo do Rimatomics fica como é.

- Ogivas maiores e MIRV (27/out, escolhas do usuário): Minuteman W62 e W78 (yield 151 e 250 do Rimatomics), com o nosso fallout escalado; RS-28 Sarmat com 10 ogivas pequenas espalhadas pelo mapa todo e abatíveis uma a uma, em silo próprio.
- Explosão nuclear (27/out, lote 14): a nossa (`HeavyBlast` por zonas) em todas as nucleares, com R = o yield do Rimatomics (raio onde o dano dele zerava), em vez do `NuclearStrike` do Rimatomics (erros no log, remoção direta, tela "Detonating" acima de 150). Do Rimatomics ficam o cogumelo, o som e a condição de fallout. A IA usa as quatro armas nucleares com custos crescentes, as maiores só em facções ultra/archotech.

## Dubs Rimatomics (verificado nos defs, workshop 1127530465)
- Tem o silo "Liberator" (`Rimatomics.MissileSilo`, 9x5, 5 mísseis, precisa de SACS), ICBM de fissão como WorldObject próprio (`WorldObject_ICBMfission`), mísseis táticos HE/químico/nuclear e os GameConditions `NuclearFallout`/`RadioactiveFallout`.
- Usa código próprio, não o sistema de mapa-mundo do CE. O CE tem patches de compatibilidade para o Rimatomics (apparel, dano, torretas), nada dos mísseis.
