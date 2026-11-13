# AGENTS.md — Mounted Arms: Strategic Missiles (guia para agentes de código)

Leia este arquivo inteiro antes de mexer em qualquer coisa. Detalhes em `Docs/`.

## O que é o projeto
Mod de RimWorld **1.6 e 1.7** (conteúdo comum em `1.6/`, DLL por versão em `Bin/1.6` e `Bin/1.7`; ver `../MountedArms/Docs/MIGRATION_1_7.md`) (`packageId: linkrag.mountedarms.strategic`, autor Felipe), companheiro do **Mounted Arms** (`../MountedArms`, mas **sem depender dele**). Adiciona lançadores de mísseis balísticos que atacam o mapa local e o **mapa-mundo**, usando o sistema de tiro no mapa-mundo do **Combat Extended**. **Requer CE**; vanilla fica para depois, com sistema próprio (ver `Docs/DECISIONS.md`).

## Quem é o usuário e como trabalhar com ele
- Felipe, brasileiro. **Responda em português (BR)**, conciso e direto. Engenheiro Java/Spring/Kafka, modder de RimWorld; testa no jogo e envia logs.
- Ele **verifica e corrige** as respostas. Separe o verificado (código, log, fonte) da hipótese; diga "não testei". Não invente comportamento do jogo/CE: leia o código (CE no GitHub: `CombatExtended-Continued/CombatExtended`) e os defs (CE local: `F:\Steam\steamapps\workshop\content\294100\2890901044`).
- Você não roda o jogo. Entregue pronto para teste e peça o que ele observou. Log: `C:\Users\Felipe Pinto\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log` (mesmo local na 1.6 e na 1.7).

## Estrutura
```
About/About.xml          metadados (depende de CE)
LoadFolders.xml          v1.6: /, 1.6, Bin/1.6; v1.7: /, 1.6, Bin/1.7; 1.6/Rimatomics só com Dubwise.Rimatomics; 1.6/MountedArms só com linkrag.mountedarms (aba Architect "Militar")
1.6/Defs/                Research, Ammo_*, Weapons, Buildings, DamageDefs (defs CE nativos, sem patch)
1.6/Textures/MountedArmsStrategic/   geradas por tools/gen_tex.py
tools/gen_tex.py         gerador de texturas (PIL)
tools/unity_facades.ps1  facades UnityEngine.* → UnityStubs para compilar na 1.7 contra o CE 1.6 (saída em obj/, não versionada)
Source/                  C# (DLL só-CE, usa Harmony): Settings (MASMod, retaliação, espoleta dos interceptadores), HeavyBlast, Verb_ShootGuidedCE, AmmoUnload, ProjectileCE_Cluster, Building_MissileLauncherCE / Building_MissileInterceptorCE (+ LoadedTopsExtension, ManualFireOnlyExtension), Nuclear (ProjectileCE_NuclearBlast, NuclearFallout + patches do fallout do Rimatomics por nome), Mirv (ProjectileCE_Mirv); MountedArmsStrategic.csproj
1.6/Patches/             ShellingResponse.xml (retaliação da IA com nossos mísseis)
1.6/Rimatomics/          nucleares (Minuteman 55/W62/W78, Sarmat e silo, pesquisas, patch no AmmoSet), sem referência de compilação ao Rimatomics
Bin/1.6/Assemblies/, Bin/1.7/Assemblies/  MountedArmsStrategic.dll compilada por versão (NÃO versionada; build.ps1)
build.ps1, Directory.Build.props   build das duas versões (SDK .NET 10; ver ../MountedArms/Docs/TOOLING.md)
Docs/                    status, decisões, notas do sistema de mapa-mundo do CE
tools/deploy.ps1         publica a cópia limpa em RimWorld/Mods/<mod> (build.ps1 chama)
tools/gen_preview.py     gera About/Preview.png (thumb da Oficina e do menu de mods)
Docs/WORKSHOP_DESCRIPTION.txt   descrição da Oficina (BBCode, inglês)
```
**Fonte x publicado:** esta pasta (`RimWorld/_dev/src/MountedArmsStrategic`) é o fonte. `RimWorld/Mods/MountedArmsStrategic` é só a cópia que o jogo carrega e a Oficina recebe (About, LoadFolders.xml, 1.6, Bin), gerada por `tools/deploy.ps1` (o `build.ps1` chama no fim). Nunca edite em `Mods/`. Mudou só XML/textura/Keyed: rode `powershell -File tools/deploy.ps1`. Oficina: `Docs/WORKSHOP_DESCRIPTION.txt` (colar à mão na página) e `tools/gen_preview.py` (thumb). Ver `../MountedArms/Docs/TOOLING.md`.


## Regras de ouro
1. Prefixo `MAS_` em todos os defs. **defName de ThingDef nunca termina em dígito** (já quebrou com `M777`: use `_M777_Howitzer`). Rode `python tools/check_defnames.py` antes de entregar.
2. Como o mod exige CE, os defs usam classes do CE direto (`TurretMannedBuildableBase`, `BaseTurretGun`, `VerbPropertiesCE`, `AmmoDef`...), não `PatchOperationMakeGunCECompatible`.
3. **Projétil de artilharia CE: `speed` 0 e `gravityFactor` 5** com cargas 30/50/70/90. `ProjectileCE.Launch` voa a `Max(carga, def.speed)`; speed > carga ignora a carga e o tiro sai do mapa (bug da bateria do Mounted Arms).
4. O botão "atacar tile do mundo" do CE só aparece se o prédio é artilharia (verbo `isMortar`/projétil `flyOverhead`), está ativo, é do jogador e o projétil tem `shellingProps`.
5. `shellingProps.damage` é dividido pelo nível tecnológico da facção alvo e multiplicado por 0,4–1,5 (ver `Docs/CE_WORLD_SHELLING.md`).
6. Alcance grande: sem `PlaceWorker_ShowTurretRadius` e com `ai_combatDangerous` false.
7. Receita de munição do CE: defName = `Make` + defName da munição (o injector procura por esse nome). Prédio com `thingCategories` (herdado das bases do CE) precisa ser minificável.
8. **Compilar**: `powershell -File build.ps1`, com o SDK .NET 10. Gera `Bin/1.6` (`net472`) e `Bin/1.7` (`net10.0`) juntos, com referências do NuGet `Krafs.Rimworld.Ref`, então a 1.6 compila sem o jogo 1.6 instalado. Detalhes em `../MountedArms/Docs/TOOLING.md`.
   - Cada versão carrega só a sua pasta `Bin/<versão>`. A DLL da 1.7 referencia `UnityStubs` e não roda na 1.6.
   - A 1.7 compila contra o CE 1.6 através de `tools/unity_facades.ps1`, que precisa do jogo 1.7 instalado.
   - C# atual, mas o código tem de compilar nos dois alvos (nada de API só do .NET 10).
   - O XML em `1.6/` é carregado pelas duas DLLs: compile sempre as duas.
9. **Interceptação (CIWS do CE):** `Building_CIWS_CE` + verbos `VerbProperties_CIWSProjectile`/`CIWSSkyfaller` + comp `CompVerbDisabler`; alvo = `ProjectileCE` hostil com `flyOverhead` e `speed` < 80 (os nossos mísseis entram). Alcance ≤ 119 (anéis de alcance quebram em 120). Sem `PlaceWorker_ShowTurretRadius` em torreta sem verbo de chão. Detalhes em `../MountedArms/Docs/CE_NOTES.md`.
10. **Lançador guiado** (míssil/foguete com GPS/inércia): `verbClass` `MountedArmsStrategic.Verb_ShootGuidedCE` e `CombatExtended.OrbitalTurretExtension` (`isMarkMandatory` false) na gun; precisão só pelo `circularError`: no mapa local cada míssil cai num ponto próprio, uniforme dentro dele (o CE travaria a rajada na mira do 1º tiro); `ShotSpread`/`recoilAmount` não contam. Artilharia não guiada fica no `Verb_ShootMortarCE`.
11. Compile a DLL com o jogo **fechado** (o jogo trava o arquivo).
12. **Explosões grandes**: nunca matar rocha (natural, alisada, desmoronada, minério) nem derrubar paredes sob teto em massa (travou o jogo): arranque o teto antes (ver `HeavyBlast`). Núcleo = remoção direta; anéis = `TakeDamage` com `MAS_BlastWave` (fatores 1). Paredes sob teto de montanha só caem no fim da onda (um desabamento). Munição/explosivos só reagem nos últimos 3–8% do raio. Trabalho de explosão grande vai em ticks (`BlastWave`), nunca num tick só; as nucleares também usam a `HeavyBlast` (nunca o `NuclearStrike` do Rimatomics). `GenRadial` só vai até ~r120. O cook-off do CE (`AmmoThing.TryDetonate`) usa o `explosionRadius` do projétil direto: munição de ogiva grande precisa do patch de cook-off. **Chama de foguete**: use `CompProperties_Exhaust`, não o `CompProperties_ProjectileFleck` do CE.
13. **Docs sempre atualizados (regra obrigatória):** toda implementação ou modificação atualiza os MDs afetados (STATUS, DECISIONS, CE_WORLD_SHELLING, LAUNCH, BLAST, HANDOFF, README, este AGENTS e o CLAUDE.md se o fluxo mudar) no mesmo commit. Mudança de build atualiza também `../MountedArms/Docs/TOOLING.md`/`MIGRATION_1_7.md`, commitados no repositório do Mounted Arms.
14. Replace em massa: confira a linha certa; valide o XML depois.
15. **Aba do Architect:** prédio novo do Strategic entra na lista de `1.6/MountedArms/Patches/MilitaryCategory.xml` (troca `designationCategory` para `MA_Military`, definida no Mounted Arms; só carrega com `linkrag.mountedarms`). Sem o Mounted Arms o prédio fica em Security.

## Convenções
- Git: branch `main`, commits curtos no imperativo, identidade `Felipe <linkrag.felipe@gmail.com>` (o global da máquina é outro: use `git -c user.name=... -c user.email=...`). Sem remote ainda.
- Código/XML em inglês; docs e conversa em português.
- Não adicione dependências ao About sem pedir.

## Docs
| Arquivo | Conteúdo |
|---|---|
| `README.md` | visão geral |
| `Docs/STATUS.md` | o que existe, testado ou não, próximos passos |
| `Docs/DECISIONS.md` | decisões do usuário e técnicas |
| `Docs/CHANGELOG.md` | versões para jogador (inglês), espelhadas na descrição da Oficina |
| `Docs/CE_WORLD_SHELLING.md` | como o CE faz tiro no mapa-mundo (com fonte) |
| `Docs/LAUNCH.md` | lançamento realista (pesquisa + perfis por míssil) |
| `Docs/HANDOFF.md` | **onde paramos** (ler primeiro) |
| `Docs/BLAST.md` | explosões por zonas de sobrepressão (pesquisa, modelo, fontes) |
| `../MountedArms/Docs/TOOLING.md` | build das DLLs (SDK .NET 10, `build.ps1`; vale para os dois mods) |
| `../MountedArms/Docs/MIGRATION_1_7.md` | migração para 1.7: auditoria, riscos, testes (vale para os dois mods) |
