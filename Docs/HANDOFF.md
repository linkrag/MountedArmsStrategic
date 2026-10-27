# Onde paramos (27/out/2026)

Vale para os dois mods (**MountedArms** e **MountedArmsStrategic**). Leia este arquivo primeiro e depois o `Docs/STATUS.md` de cada mod.

## Estado
- Os dois mods rodam em RimWorld 1.6 e 1.7 (decisão do usuário: manter as duas). DLL por versão em `Bin/1.6` e `Bin/1.7`, compiladas com o SDK .NET 10 pelo `build.ps1` de cada mod; a 1.7 contra o CE 1.6 (`tools/unity_facades.ps1`). Confirmado em jogo na 1.7 com o CE 16.7.4 (26/out). Detalhes e riscos: `../MountedArms/Docs/MIGRATION_1_7.md`.
- Strategic: lote 14 (explosão nuclear própria) implementado e não testado; o que já foi confirmado e o que falta testar estão no `Docs/STATUS.md`.
- Sem remote no git.

## Regras do projeto (resumo)
Docs atualizados no mesmo commit de cada mudança (`MountedArms/Docs/RULES.md` §0). Nada de novas dependências no About sem pedir. defName de ThingDef nunca termina em dígito (`tools/check_defnames.py`). Jogo fechado para compilar.

## Onde está cada coisa
- Strategic:
  - `Docs/STATUS.md` (estado, testes, resumo de cada sistema), `DECISIONS.md`, `BLAST.md` (física das explosões + fontes), `LAUNCH.md` (lançamento realista), `CE_WORLD_SHELLING.md` (tiro no mapa-mundo do CE)
  - `AGENTS.md` (regras técnicas)
- MountedArms:
  - `Docs/STATUS.md`, `TURRETS.md`, `DECISIONS.md`, `CE_NOTES.md` (comportamentos internos do CE descobertos), `HISTORY.md` (erros e lições dos dois mods), `TOOLING.md` (build), `RULES.md`, `MIGRATION_1_7.md` (migração 1.7, riscos, testes; vale para os dois mods), `TEMPLATE_NOVA_TORRETA.md`
  - `AGENTS.md`
