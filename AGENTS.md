# AGENTS.md — Mounted Arms: Strategic Missiles (guia para agentes de código)

Leia este arquivo inteiro antes de mexer em qualquer coisa. Detalhes em `Docs/`.

## O que é o projeto
Mod de RimWorld **1.6** (`packageId: linkrag.mountedarms.strategic`, autor Felipe), companheiro do **Mounted Arms** (`../MountedArms`, mas **sem depender dele**). Adiciona lançadores de mísseis balísticos que atacam o mapa local e o **mapa-mundo**, usando o sistema de tiro no mapa-mundo do **Combat Extended**. **Requer CE**; vanilla fica para depois, com sistema próprio (ver `Docs/DECISIONS.md`).

## Quem é o usuário e como trabalhar com ele
- Felipe, brasileiro. **Responda em português (BR)**, conciso e direto. Engenheiro Java/Spring/Kafka, modder de RimWorld; testa no jogo e envia logs.
- Ele **verifica e corrige** as respostas. Separe o verificado (código, log, fonte) da hipótese; diga "não testei". Não invente comportamento do jogo/CE: leia o código (CE no GitHub: `CombatExtended-Continued/CombatExtended`) e os defs (CE local: `F:\Steam\steamapps\workshop\content\294100\2890901044`).
- Você não roda o jogo. Entregue pronto para teste e peça o que ele observou. Log: `C:\Users\Felipe Pinto\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`.

## Estrutura
```
About/About.xml          metadados (depende de CE)
LoadFolders.xml          / e 1.6; 1.6/Rimatomics só com Dubwise.Rimatomics
1.6/Defs/                Research, Ammo_*, Weapons, Buildings (defs CE nativos, sem patch)
1.6/Textures/MountedArmsStrategic/   geradas por tools/gen_tex.py
tools/gen_tex.py         gerador de texturas (PIL)
Source/                  C# (DLL só-CE, usa Harmony): Settings (MASMod, retaliação, espoleta dos interceptadores), HeavyBlast, Verb_ShootGuidedCE, AmmoUnload, ProjectileCE_Cluster, Building_MissileLauncherCE / Building_MissileInterceptorCE (+ LoadedTopsExtension, ManualFireOnlyExtension), ProjectileCE_NuclearStrike (+ NuclearStrikeExtension); build.ps1
1.6/Patches/             ShellingResponse.xml (retaliação da IA com nossos mísseis)
1.6/Rimatomics/          ICBM nuclear (defs + patch no AmmoSet), sem referência de compilação ao Rimatomics
1.6/Assemblies/MountedArmsStrategic.dll  compilado (NÃO versionado)
Docs/                    status, decisões, notas do sistema de mapa-mundo do CE
```

## Regras de ouro
1. Prefixo `MAS_` em todos os defs. **defName de ThingDef nunca termina em dígito** (já quebrou com `M777`: use `_M777_Howitzer`). Rode `python tools/check_defnames.py` antes de entregar.
2. Como o mod exige CE, os defs usam classes do CE direto (`TurretMannedBuildableBase`, `BaseTurretGun`, `VerbPropertiesCE`, `AmmoDef`...), não `PatchOperationMakeGunCECompatible`.
3. **Projétil de artilharia CE: `speed` 0 e `gravityFactor` 5** com cargas 30/50/70/90. `ProjectileCE.Launch` voa a `Max(carga, def.speed)`; speed > carga ignora a carga e o tiro sai do mapa (bug da bateria do Mounted Arms).
4. O botão "atacar tile do mundo" do CE só aparece se o prédio é artilharia (verbo `isMortar`/projétil `flyOverhead`), está ativo, é do jogador e o projétil tem `shellingProps`.
5. `shellingProps.damage` é dividido pelo nível tecnológico da facção alvo e multiplicado por 0,4–1,5 (ver `Docs/CE_WORLD_SHELLING.md`).
6. Alcance grande: sem `PlaceWorker_ShowTurretRadius` e com `ai_combatDangerous` false.
7. Receita de munição do CE: defName = `Make` + defName da munição (o injector procura por esse nome). Prédio com `thingCategories` (herdado das bases do CE) precisa ser minificável.
8. **Compilar:** `powershell -File Source/build.ps1` (csc do .NET Framework, C# 5, contra o mscorlib do jogo com `/noconfig /nostdlib`). Não há SDK .NET nem mono na máquina. Sem recursos de C# 6+ (sem `=>` em membros, sem `?.`, sem interpolação `$""`).
9. **Interceptação (CIWS do CE):** `Building_CIWS_CE` + verbos `VerbProperties_CIWSProjectile`/`CIWSSkyfaller` + comp `CompVerbDisabler`; alvo = `ProjectileCE` hostil com `flyOverhead` e `speed` < 80 (os nossos mísseis entram). Alcance ≤ 119 (anéis de alcance quebram em 120). Sem `PlaceWorker_ShowTurretRadius` em torreta sem verbo de chão. Detalhes em `../MountedArms/Docs/CE_NOTES.md`.
10. **Lançador guiado** (míssil/foguete com GPS/inércia): `verbClass` `MountedArmsStrategic.Verb_ShootGuidedCE` e `CombatExtended.OrbitalTurretExtension` (`isMarkMandatory` false) na gun; precisão só pelo `circularError`. Artilharia não guiada fica no `Verb_ShootMortarCE`.
11. Compile a DLL com o jogo **fechado** (o jogo trava o arquivo).
12. Replace em massa: confira a linha certa; valide o XML depois.

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
| `Docs/CE_WORLD_SHELLING.md` | como o CE faz tiro no mapa-mundo (com fonte) |
