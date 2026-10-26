# Onde paramos (25/out/2026), antes da migração para o RimWorld 1.7

Vale para os dois mods (**MountedArms** e **MountedArmsStrategic**). Leia este arquivo primeiro. Depois leia o `Docs/STATUS.md` de cada mod.

## Estado
- **Lote 11 (Strategic, 25/out/2026, não testado)**: paredes com 1 HP corrigidas (desabamento real da montanha), dano real fora do núcleo, munição e explosivos só na borda, salva da HIMARS espalhada, DLL própria da 1.7 em `Bin/1.7`. Ver `Docs/STATUS.md`.
- Até o lote 10 tudo compilou para RimWorld 1.6 + Harmony + CE 16.7.4 (+ Rimatomics opcional). Desde 25/out o `build.ps1` (SDK .NET 10) gera `Bin/1.6` e `Bin/1.7` juntos, as duas com o lote 11. Não há remote no git.
- **Lote 10 (Strategic) implementado e NÃO testado**: explosão grande como onda de choque espalhada em ticks (`Source/HeavyBlast.cs`: `BlastWave` + MapComponent `HeavyBlastWaves`). Teste do lote 9: zonas, cook-off e rocha OK. O impacto travava ~1 s e não tinha efeito visual (tudo num tick), e isso motivou o lote 10. O usuário parou as correções aqui.
- Lotes 7–8 (Strategic) e "Ajustes desta rodada" (MountedArms): não ficou registrada confirmação explícita em jogo. Itens: chama no bocal, efeitos de lançamento, espoleta de proximidade, inclinação na fase lenta. Teste quando voltar.

## Testes pendentes (lote 10; substituídos pelo lote 11, ver a lista "Testar" do `Docs/STATUS.md`)
1. Minuteman III HE sobre base densa: a onda se expande (~0,4 s) com o efeito vanilla por célula, sem travar. Se engasgar, anote o momento: impacto, onda ou fim (fogo/cratera).
2. ~~Paredes sob teto de montanha ficam com ≥ 1 HP~~: mudou no lote 11. Agora as atingidas caem e a montanha desaba uma vez, no fim da onda.
3. GMLRS/ATACMS: onda curta e proporcional.
4. Log: `heavy blast wave stopped` indica erro dentro da onda.

## Ideias em aberto (nada decidido)
- Tomahawk R 17,9 é 20–40% generoso pela física; poderia ser ~14–15.
- Prédios de outras facções: o dano passa pelo aviso à facção, mas sem instigador (sem goodwill). Ideia: uma perda única de goodwill por facção atingida.
- Se a explosão ainda pesar: orçamento ponderado por tick (pawn ~15, prédio destruído ~3, resto 1), e fogo/cratera do `Finish` também em ticks. A revisão estimou isso como pequeno; não foi medido.
- Na 1.7: ver se a engine nova permite algo melhor (paralelismo, efeitos nativos de explosão grande, desempenho de destruição em massa).
- Vanilla sem CE: tiro no mapa-mundo próprio. Remote do git.

## Migração para 1.7
**Iniciada em 25/out/2026**: About/LoadFolders aceitam 1.7. Os dois mods têm DLLs por versão (`Bin/1.6`, `Bin/1.7`), compiladas com o SDK .NET 10 (`build.ps1` na raiz de cada mod; a 1.7 contra o CE 1.6 com `tools/unity_facades.ps1`). O usuário decidiu manter 1.6 e 1.7. Auditoria completa e pendências em `../MountedArms/Docs/MIGRATION_1_7.md` (substitui a lista abaixo onde divergirem).

### O que se previa que fosse precisar
Observado na pasta do jogo 1.7.4985: sem `RimWorldWin64_Data`/Unity, runtime .NET 10 (CoreCLR), Vulkan, bibliotecas `Brutal.*` (RocketWerkz). **Não verificado**: API, nome das DLLs de referência, Harmony e CE para 1.7.
- **DLLs a portar (4)** (caminhos e scripts antigos, removidos em 25/out; hoje: `Bin/<versão>` e `build.ps1`):
  - `MountedArms/1.6/Assemblies/MountedArms.dll` (`Source/`, `build.sh`)
  - `MountedArmsFx.dll` (`SourceFx/build_fx.ps1`)
  - `MountedArms/1.6/CE/Assemblies/MountedArmsCE.dll` (`SourceCE/build_ce.ps1`, depende do CE)
  - `MountedArmsStrategic/1.6/Assemblies/MountedArmsStrategic.dll` (`Source/build.ps1`, depende do CE, Harmony e Unity.Mathematics)
- **Toolchain de então** (substituída em 25/out pelo SDK .NET 10 e `build.ps1`): csc do .NET Framework, só C# 5, `/noconfig /nostdlib` contra `RimWorldWin64_Data/Managed`. Na 1.7 vai mudar (provavelmente SDK .NET moderno, C# atual), e os scripts de build terão de ser refeitos.
- **Dependências externas**: CE (vários tipos usados: `ProjectileCE`, `ProjectileCE_Explosive`, `Verb_ShootMortarCE`, `Building_CIWS_CE`, `AmmoThing.TryDetonate`, `HostilitySheller`, `ShellingResponseDef`...), Harmony e Dubs Rimatomics. Esperar as versões 1.7 deles.
- **Uso de Unity**: `UnityEngine` (Mathf, Vector3, Color, `Find.CameraDriver.shaker`, flecks) em todos os fontes. Se a 1.7 trocou a engine, isso muda.
- **Pastas de versão**: criar `1.7/` (e `LoadFolders.xml`, `supportedVersions` no About), mantendo `1.6/` se quiser compatibilidade.
- **Defs XML**: provavelmente reaproveitáveis. Conferir campos renomeados no log de carregamento.

## Regras do projeto (resumo)
Docs atualizados no mesmo commit de cada mudança (`MountedArms/Docs/RULES.md` §0). Nada de novas dependências no About sem pedir. defName de ThingDef nunca termina em dígito (`tools/check_defnames.py`). Commits com `git -c user.name=Felipe -c user.email=linkrag.felipe@gmail.com`. Jogo fechado para compilar.

## Onde está cada coisa
- Strategic:
  - `Docs/STATUS.md` (lotes, testes), `DECISIONS.md`, `BLAST.md` (física das explosões + fontes), `LAUNCH.md` (lançamento realista), `CE_WORLD_SHELLING.md` (tiro no mapa-mundo do CE)
  - `AGENTS.md` (regras técnicas)
- MountedArms:
  - `Docs/STATUS.md`, `TURRETS.md`, `DECISIONS.md`, `CE_NOTES.md` (comportamentos internos do CE descobertos), `HISTORY.md` (erros e lições), `TOOLING.md` (build), `RULES.md`, `MIGRATION_1_7.md` (migração 1.7, riscos, testes; vale para os dois mods), `TEMPLATE_NOVA_TORRETA.md`
  - `AGENTS.md`
