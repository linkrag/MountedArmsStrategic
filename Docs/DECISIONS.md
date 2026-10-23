# Decisões

## Do usuário (Felipe)
- **Mod separado** do Mounted Arms (out/2026): escopo estratégico, conteúdo só-CE no início.
- Fazer **os dois**: lançador de míssil balístico tático e silo de ICBM.
- Ogivas do ICBM: convencional **e** nuclear (perguntou "tem como?"). Depois apontou que o **Dubs Rimatomics** já tem armas nucleares → decisão sobre a nuclear pendente.

## Técnicas
- Defs CE nativos (o mod exige CE), herdando das bases do CE.
- Projétil de artilharia: speed 0 + gravityFactor 5 (lição da bateria do Mounted Arms).
- Lançador tático sem raio de colocação e `ai_combatDangerous` false (alcance 700).
- `circularError` 3 e `indirectFirePenalty` 0,05: míssil guiado, mais preciso que o morteiro do CE (3 / 0,3).
- Dano no mapa-mundo 1,5: ~3 acertos num assentamento industrial (cálculo em `CE_WORLD_SHELLING.md`).

## Dubs Rimatomics (verificado nos defs, workshop 1127530465)
- Tem o silo "Liberator" (`Rimatomics.MissileSilo`, 9x5, 5 mísseis, precisa de SACS), ICBM de fissão como WorldObject próprio (`WorldObject_ICBMfission`), mísseis táticos HE/químico/nuclear e os GameConditions `NuclearFallout`/`RadioactiveFallout`.
- Usa código próprio, não o sistema de mapa-mundo do CE. O CE tem patches de compatibilidade para o Rimatomics (apparel, dano, torretas), nada dos mísseis.
