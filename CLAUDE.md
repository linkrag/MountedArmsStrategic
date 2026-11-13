@AGENTS.md

## Regra obrigatória: manter a documentação atualizada
Toda **implementação ou modificação** (código, XML, textura, build, configuração, decisão ou pedido do usuário, correção de bug) só está terminada depois de atualizar, **no mesmo passo/commit**, os documentos afetados deste mod:
- `Docs/STATUS.md` (o que mudou; marque **testado x não testado** no jogo) e, conforme o caso, `DECISIONS`, `HISTORY` (erros e lições), `TURRETS`/`CE_NOTES` etc.;
- o `README.md`, se a funcionalidade visível mudou;
- o `AGENTS.md` (estrutura, regras de ouro, estado atual) e este `CLAUDE.md`, se a estrutura, as regras ou o fluxo mudaram;
- o `CLAUDE.md` do workspace (`RimWorld/_dev/src/`, um nível acima; tabela de mods e "Estado"), se mudou algo que vale para o workspace.
Mudança que afeta outro mod do workspace atualiza também os MDs dele. Sem essa atualização o trabalho está inacabado: diga explicitamente quais docs foram atualizados na resposta final.
