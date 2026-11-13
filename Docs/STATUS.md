# Status (atualize sempre que o usuário testar algo)

Ambiente: RimWorld 1.6 e 1.7, Harmony, Combat Extended 16.7.4, Dubs Rimatomics (opcional; todo o conteúdo nuclear só carrega com ele). Conteúdo comum em `1.6/`; DLL por versão em `Bin/1.6` e `Bin/1.7`, compiladas juntas pelo `build.ps1` (SDK .NET 10) **com o jogo fechado** (o jogo trava a DLL; `-Out <pasta>` só valida a compilação). A 1.7 compila contra o CE 1.6 com `tools/unity_facades.ps1`. Migração, riscos e toolchain: `../MountedArms/Docs/MIGRATION_1_7.md`.

## Versão e changelog (13/nov/2026) - versão atual **1.0.1**, sem efeito no jogo além do número
- `<modVersion>1.0.1</modVersion>` no `About/About.xml` (o campo existe no `ModMetaData` da 1.6 e da 1.7; o menu de mods mostra a versão).
- `Docs/CHANGELOG.md` (novo, inglês, para jogador): uma versão por dia com mudança visível desde a publicação (1.0.0 = 7/nov). Montado por workflow de agentes a partir de STATUS/HISTORY/git e conferido item a item por um revisor cético.
- `Docs/WORKSHOP_DESCRIPTION.txt`: linha "Mod version" ao lado de "Versions" e seção [h2]Changelog[/h2] no fim (a 1.0.0 vira uma linha só, porque os recursos já estão descritos acima; o limite da Steam é 8000 caracteres). **Colar na página da Oficina: pendente com o usuário.**
- Nada testado no jogo (só o número no menu de mods mudaria).

## Passo de "Até X" dos mísseis (13/nov/2026) - deploy feito, **NÃO testado no jogo**
- As 15 receitas de míssil (cruzeiro, ICBM HE/Cluster/Termobárico/EMP, GMLRS, tático x4, interceptador, ICBM nucleares x3 e Sarmat no `1.6/Rimatomics`) herdavam `targetCountAdjustment` 20 do `AmmoRecipeBase` do CE, mas fazem 1 por construção: agora cada uma declara `<targetCountAdjustment>1</targetCountAdjustment>` (o -/+ de "Até X" anda de 1). Achado pela auditoria do Colony Organizer, que também corrige em runtime; aqui vale mesmo sem ele. Regra para receitas novas sobre `AmmoRecipeBase`/`MAS_TacticalRecipeBase`: declarar `targetCountAdjustment` = contagem do produto.

## Oficina Steam e fonte fora de `Mods` (7/nov/2026)
- Publicado na Oficina pelo usuário (ID 3835655252). O 1º upload levou a pasta inteira (Docs, Source, .git...): o fonte foi para `RimWorld/_dev/src/MountedArmsStrategic` e `Mods/MountedArmsStrategic` virou cópia limpa gerada por `tools/deploy.ps1` (chamado pelo `build.ps1`). Build 1.6 + 1.7 e deploy rodados com sucesso; a cópia tem só `About/`, `LoadFolders.xml`, `1.6/`, `Bin/`. **Falta:** "Atualizar na Oficina Steam" no jogo (troca conteúdo e thumb) e confirmar que o jogo carrega a cópia nova sem erro (**não testado no jogo**).
- Novo `About/Preview.png` (1280x720, `tools/gen_preview.py`) e descrição da Oficina em `Docs/WORKSHOP_DESCRIPTION.txt` (BBCode; colar à mão na página, o jogo não reenvia descrição). Detalhes: `../MountedArms/Docs/TOOLING.md` (seção "Fonte x cópia publicada").
- Achado da checagem: o `About.xml` não lista Harmony em `modDependencies` (a DLL usa Harmony; o CE já exige Harmony, então funciona). Não alterado: dependência nova no About só com pedido.

Testes do usuário nesta fase: na 1.7 com o CE 16.7.4. A 1.6 não é testada desde o lote 10 (compila igual).

## Pendente de teste (2/nov/2026)
- **Aba "Militar" do Architect** compartilhada com o Mounted Arms: `1.6/MountedArms/Patches/MilitaryCategory.xml`, carregado por `LoadFolders IfModActive="linkrag.mountedarms"`. Troca `designationCategory` de TacticalLauncher, ICBMSilo, MLRS, CruiseVLS, M777, InterceptorBattery e Sarmat (esta só com Rimatomics) para `MA_Military`, definida no Mounted Arms. Sem o Mounted Arms nada muda (Security). NÃO testado no jogo.

## Confirmado em jogo
- Carregamento sem erros (1.6 e 1.7), aba de pesquisa, construção, receitas no injector do CE.
- Tiro local e no mapa-mundo dos lançadores guiados; ataque a mapa carregado sem marcador; botão Descarregar e troca de ogiva; nomes reais.
- Retaliação do CE no mapa do jogador; configurações do mod; espoleta de proximidade dos interceptadores.
- Explosão grande por zonas (lote 11): anéis com dano visível, núcleo vaporizado com cratera, base escavada em montanha desaba uma vez sem travar. Salva da HIMARS espalhada até ~7 células.
- Fallout (lote 12; a força ×1,6 / ×2,1 da W62 / W78 também não foi conferida): começa no impacto; residência e base sob montanha reduzem a radiação.
- Nucleares (lote 13): pesquisas e receitas novas; Sarmat (silo 4×4, divisão no ar em 10 ogivas espalhadas pelo mapa todo, todas alcançam o alvo, desempenho normal, sem tela).
- Explosão nuclear própria (lotes 14–16): fissão, W62 e W78 sem tela e sem erros no log; a W78 arrasa a base e a onda termina rápido; nomes reais no silo e na pesquisa; pulso térmico (queimaduras, incêndios por boa parte do mapa na W62/W78 sem queda forte de frames) e radiação inicial, com proteção por teto e montanha; colonos dentro de base em montanha sobrevivem à onda.

## Testes pendentes
- Paiol no raio de uma explosão grande só explode na borda (lote 11).
- Fallout (lote 12; a força ×1,6 / ×2,1 da W62 / W78 também não foi conferida): nos primeiros 30 min nada; colono ao virar "Fatal Radiation Exposure" (~3 h fora) deixa de receber dose e morre em ~2 dias, não em horas; depois de ~1 dia o ganho fica pequeno; a condição acaba em 3 dias; salvar e carregar no meio continua o decaimento.
- Defesa antimíssil abatendo ogivas individuais do Sarmat (a área delas fica intacta) e o ônibus antes da divisão (difícil de testar).
- IA com a retaliação nuclear ligada contra colônia grande (ou orçamento de retaliação alto): usa W62/W78/Sarmat raramente.
- Precisão no mapa-mundo (erro ≤ `circularError`); M270 sem energia não dispara; Mk 41 com topo fixo; M777 com marcador; cluster/termobárica/EMP (colmeia mecanoide e assentamento humano); Patriot interceptando retaliação; efeitos de lançamento e chama no bocal.

## Como cada sistema funciona (resumo; detalhes nos docs)
- **Explosões grandes** (`Source/HeavyBlast.cs`, `Docs/BLAST.md`): zonas reais de sobrepressão por frações de R (R = contorno de 2 psi): núcleo 0,25R vaporizado + cratera; anéis 0,37/0,50/0,75/1,0R com dano real (`MAS_BlastWave`, fatores 1); termobárico com anéis maiores, confinamento ×1,5 e mais fogo. Onda de choque espalhada em ticks; todo teto não-espesso no raio arrancado à frente; rocha natural intocada; munição e explosivos só reagem nos últimos 3–8% do raio; sob teto de montanha é abrigo (fora do núcleo nada é tocado; no núcleo, só o dano do anel externo); sustentadores de teto de montanha caem juntos no fim e a montanha desaba uma vez. Usada por Minuteman HE/termobárico, ATACMS HE/termobárico, GMLRS, Tomahawk e todas as nucleares.
- **Mira** (`Source/GuidedVerb.cs`, `Docs/CE_WORLD_SHELLING.md`): `Verb_ShootGuidedCE` zera luz/fumaça/clima/erro de distância do CE; sobra só o `circularError`. Cada míssil de uma salva local cai num ponto próprio dentro dele (o CE travava a mira da rajada no 1º tiro).
- **Lançamento** (`Source/LaunchProfile.cs`, `Docs/LAUNCH.md`): fase de impulso lenta e depois acelerada no mesmo caminho; chama e fumaça no bocal (`Source/Exhaust.cs`).
- **Defesa antimíssil**: Patriot sobre o CIWS do CE (exige a opção "Enable CIWS" do CE); interceptador que errou se autodestrói e libera o alvo para a próxima camada.
- **Retaliação da IA** (`1.6/Patches/ShellingResponse.xml`, `Source/Settings.cs`): espacial GMLRS (400) + ATACMS (1500); ultra/archotech ATACMS + Minuteman HE (4000). Com a opção nuclear: Minuteman (fissão) 6000/peso 0,05 (espacial em diante), W62 9000/0,03, W78 12000/0,02 e Sarmat 20000/0,01 (só ultra/archotech). O CE só escolhe um projétil enquanto o orçamento (ameaça × 3,5 × força × barra das configurações) paga por ele: com força 1, W62 a partir de ~2600 pontos de ameaça, W78 ~3400, Sarmat ~5700.
- **Nucleares** (só com Rimatomics, `1.6/Rimatomics/`):
  - Minuteman (fissão, R 55), W62 (R 151) e W78 (R 250); mapa-mundo 20 / 25 / 30 (arrasa qualquer nível tecnológico). Escala: ~6× menor em distância que as ogivas reais (yield equivalente no mapa ~0,03 / 0,6 / 2,7 kt contra 170 e 335 kt reais; `Docs/BLAST.md`).
  - Sarmat (`Source/Mirv.cs`): silo 4×4 próprio (2500 HP, 1500 W, alcance 700 tiles, recarga 90 s). Sobre um mapa carregado o ônibus libera 10 ogivas a 20% do voo de reentrada (o CE faz as chegadas a altura 200): uma no ponto mirado e as outras espalhadas pelo mapa todo (amostragem "best candidate", margem 10 da borda). Cada ogiva é um `ProjectileCE` próprio (lançado como o `TravelingShell` do CE), abatível pela defesa; se o ônibus cair sem se dividir, explode como uma ogiva. Mapa-mundo 40.
  - Pesquisas (bancada de alta tecnologia): W62 thermonuclear warhead 8000 (pré: ICBM 6000) → W62; W78 thermonuclear warhead 10000 → W78; RS-28 Sarmat (MIRV) 12000 (pré: W62) → Sarmat.
  - Pulso no instante da explosão (`NuclearPulse`): térmico (queimaduras ao ar livre com linha de visão, fogo ao ar livre) e radiação inicial (teto ×0,5, montanha ×0,001). Raios térmico / radiação: fissão 29 / 80, W62 126 / 171, W78 257 / 229, ogiva do Sarmat 12 / 44.
  - Receitas: fissão = ogiva de fissão do Rimatomics + 1000 aço, 200 plasteel, 200 FSX, 16/4 componentes. W62 = o mesmo + 150 urânio empobrecido + 60 plutônio, 6 componentes espaciais, Crafting 13. W78 = + 300 urânio empobrecido + 120 plutônio, 8 componentes espaciais, Crafting 14. Sarmat = 4 ogivas de fissão, 3000 aço, 600 plasteel, 1500 chemfuel, 400 FSX, 40/15 componentes, Crafting 15.
- **Fallout** (`Source/Nuclear.cs`): a condição `NuclearFallout` do Rimatomics, registrada e controlada por nós (dois patches Harmony por nome, só no nosso fallout; o do silo do próprio Rimatomics, constante por 10–30 dias, fica como é). Formato conferido com fontes:
  - chegada em ~30 min, taxa de H+1 na 1ª hora, depois t^-1,2 (regra 7-10: 10% em H+7, 1% em H+48); dura 3 dias (os 2–3 primeiros são os perigosos; no fim do 3º a dose empata com a recuperação do Rimatomics, −0,08/dia);
  - radiação pelo `DubUtils.applyRads` do Rimatomics a cada 250 ticks em pawns de carne; pico de 0,7 de radiation sickness por hora ao ar livre no Minuteman (~350 R/h; 0,9 vira radiação fatal, ~DL50 4–5 Gy): ~0,86 em 3 h fora;
  - proteção 1/PF: ao ar livre 1, teto sem paredes 0,5, cômodo fechado 0,2 (térreo de casa, PF 2–5), sob montanha 0,001 (PF > 1000);
  - quem já tem `FatalRad` não recebe mais dose (o `applyRads` jogava toda dose seguinte nele e matava em horas; a morte real com 7–14 Gy vem em semanas; fica o curso do Rimatomics, ~2,25 dias);
  - escala por ogiva (`NuclearFalloutExtension.scale`): 1 / 1,6 / 2,1, Sarmat 0,25 por ogiva; explosões a menos de 1 h uma da outra somam, uma posterior recomeça a curva com o que restava; teto 3×;
  - sem toxic buildup (o do Rimatomics multiplica pela resistência e dá 0 para quase todos); plantas e comida como no Rimatomics, escalados pela taxa; encerra o `RadioactiveFallout` ativo;
  - fontes: [manual de defesa civil, ORAU](https://orau.org/health-physics-museum/files/library/civil-defense/victor727.pdf) (regra 7-10, 2–3 dias); [relatório de teste desclassificado](https://minddfiles.nuclearsecrecy.com/0404656.pdf) (chegada ~30 min); [CDC](https://www.cdc.gov/nceh/radiation/emergencies/arsphysicianfactsheet.htm) (DL50 2,5–5 Gy); [WA DAHP](https://dahp.wa.gov/sites/default/files/Fallout%20Protection%20Homes%20with%20Basements.pdf) (PF de casas). A taxa de pico é escolha de jogo dentro da faixa real.

## Próximos passos
- Quando sair um CE 1.7: `build.ps1 -CE17 <CombatExtended.dll>` e rever os alvos do CE listados em `../MountedArms/Docs/MIGRATION_1_7.md`. Decidir se o `PatchAll()` de `Settings.cs` ganha try/catch por classe.
- Ideias em aberto (nada decidido): modo "yield real" para W62/W78 (mapa inteiro no núcleo); Tomahawk R 17,9 é 20–40% generoso pela física (~14–15); perda única de goodwill por facção atingida; tiro no mapa-mundo sem CE; remote do git.
