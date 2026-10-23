# Decisões

## Do usuário (Felipe)
- **Mod separado** do Mounted Arms (out/2026): escopo estratégico, conteúdo só-CE no início.
- Fazer **os dois**: lançador de míssil balístico tático e silo de ICBM.
- Ogivas do ICBM: convencional **e** nuclear (perguntou "tem como?"). Depois apontou que o **Dubs Rimatomics** já tem armas nucleares → decidiu: **nuclear integrada ao Rimatomics** (a ogiva nuclear só existe com o Rimatomics ativo e usa o fallout dele, `NuclearFallout`). Sem Rimatomics, o ICBM só tem ogivas convencionais.

- **Defesa antimíssil** e **IA retaliando com mísseis por nível** (Spacer: tático; Ultra/Archotech: tático + ICBM HE; sem nuclear para a IA). A bateria também intercepta drop pods.

- Lote 3 pedido pelo usuário ("pode fazer todos"): salva (rajada + seleção múltipla), MLRS, míssil de cruzeiro, ogivas cluster/termobárica/EMP, M777.

- Nomes reais nos labels (M270/ATACMS, HIMARS/GMLRS, Mk 41/Tomahawk, Minuteman III, Patriot/PAC-3, M777); defNames mantidos.
- M270 (ATACMS) precisa de **energia** além do operador.
- Mísseis guiados devem errar pouco: o erro fica só no `circularError` (CEP), sem penalidades de luz/fumaça/clima/estimativa de distância do CE.
- Lançadores guiados atacam mapa carregado sem marcador; o M777 (não guiado) continua exigindo.

## Técnicas
- Defs CE nativos (o mod exige CE), herdando das bases do CE.
- Projétil de artilharia: speed 0 + gravityFactor 5 (lição da bateria do Mounted Arms).
- Lançador tático sem raio de colocação e `ai_combatDangerous` false (alcance 700).
- `circularError` 3 e `indirectFirePenalty` 0,05: míssil guiado, mais preciso que o morteiro do CE (3 / 0,3).
- `Verb_ShootGuidedCE` (nosso, deriva de `Verb_ShootMortarCE`) para M270, HIMARS, Mk 41 e Minuteman; explicação em `CE_WORLD_SHELLING.md`.
- Dano no mapa-mundo 1,5: ~3 acertos num assentamento industrial (cálculo em `CE_WORLD_SHELLING.md`).

## Dubs Rimatomics (verificado nos defs, workshop 1127530465)
- Tem o silo "Liberator" (`Rimatomics.MissileSilo`, 9x5, 5 mísseis, precisa de SACS), ICBM de fissão como WorldObject próprio (`WorldObject_ICBMfission`), mísseis táticos HE/químico/nuclear e os GameConditions `NuclearFallout`/`RadioactiveFallout`.
- Usa código próprio, não o sistema de mapa-mundo do CE. O CE tem patches de compatibilidade para o Rimatomics (apparel, dano, torretas), nada dos mísseis.
