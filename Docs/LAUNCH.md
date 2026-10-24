# Lançamento realista dos mísseis

## Como funciona na vida real (pesquisa, out/2026)
- **Minuteman III (silo, "hot launch")**: a porta do silo abre e o 1º estágio acende segundos depois (relato de um teste em Vandenberg: ~6 s), o míssil sobe devagar e acelera; 1º estágio queima ~60 s. Lança muita fumaça (o Minuteman "sopra anéis de fumaça" na saída do silo).
- **Tomahawk (Mk 41 VLS, "cold launch")**: um gerador de gás ejeta o míssil 9–15 m acima da célula; só então o booster sólido acende (protege o navio); depois abrem asas/aletas/entrada de ar, o booster cai e o turbofan assume (cruzeiro ~Mach 0,7).
- **Stinger ("soft launch")**: motor de ejeção tira o míssil do tubo; ele plana ~8,5 m antes do motor de voo acender; só manobra após ~200 m.
- **PAC-3 MSE**: sai do contêiner com aceleração muito alta do motor sólido (fase de boost), depois fase de sustentação em alta velocidade.
- **ATACMS / GMLRS**: lançamento quente direto do pod do M270/HIMARS, aceleração quase imediata.

Fontes: [Minuteman III test launch (relato)](https://discourse.weareopen.coop/news/minuteman-iii-a-look-at) · [Minuteman III MIRV path (Wikimedia)](https://www.cosmos-indirekt.de/Physik-Schule/Datei:Minuteman_III_MIRV_path.svg) · [Minuteman smoke rings (Air & Space)](https://www.airspacemag.com/daily-planet/minuteman-missile-blows-smoke-rings-180958003) · [How the Navy launches Tomahawk](https://list25.com/how-the-navy-launches-tomahawk-missiles-1000-mile-strikes/) · [Tomahawk (designation-systems)](https://designation-systems.net/dusrm/m-109.html) · [Soft launch (Wikipedia)](https://en.wikipedia.org/wiki/Soft_launch_(missile)) · [FIM-92 Stinger (Wikipedia)](https://en.wikipedia.org/wiki/FIM-92_Stinger) · [PAC-3 MSE overview (Lockheed Martin)](https://sustainability.lockheedmartin.com/content/dam/lockheed-martin/mfc/documents/pac-3/23-07402-IAMD-PAC-3-MSE-Overview-R2.pdf)

## Como está no mod
- `BoostPhaseTrajectoryWorker` (`Source/LaunchProfile.cs`): trajetória "lerped" do CE com o **tempo** suavizado — começa devagar e acelera, com **o mesmo caminho, o mesmo tempo total e o mesmo ponto de impacto** (a mira não muda; `PredictPositions` usa a mesma curva, então CIWS inimigos preveem certo). Chegadas do mapa-mundo (nascem a altura 200) não são suavizadas.
- `LaunchProfileExtension` no projétil: `boostFraction` (fração do voo na fase lenta), `launchSmoke`/`launchSmokeSize` (fumaça no lançador), `hotLaunch` (brilho de fogo na saída) ou lançamento a frio (puff de fumaça, ignição em `ignitionAt` da trajetória).

| Míssil | boostFraction | Lançamento | Fumaça |
|---|---|---|---|
| Minuteman III (todas as ogivas, inclusive nuclear) | 0,15 | quente | 16 × 3,5 |
| ATACMS (todas as ogivas) | 0,05 | quente | 8 × 2,5 |
| GMLRS | 0,03 | quente | 3 × 1,5 |
| Tomahawk | 0,10 | **frio** (ignição a 3% do voo) | 5 × 2,0 |
| PAC-3 MSE | — (voo linear) | quente | 4 × 1,5 |

- Interceptadores (PAC-3, Stinger, rajadas de CIWS) **não** recebem a curva: o cálculo de interceptação do CE (`CanReachPos`/previsão) assume voo linear. O PAC-3 recebe só a fumaça/brilho (pelo patch `ProjectileCE_CIWS.Tick`). O Stinger fica no Mounted Arms, que não depende do Strategic.
- A trilha de chama/fumaça do próprio projétil (`CompProperties_ProjectileFleck`) continua; na fase lenta ela se acumula perto do lançador, formando a nuvem de saída.
