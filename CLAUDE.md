# MMORPG Tycoon (nome provisório)

Jogo de gestão single-player e offline: o jogador cria e administra um MMORPG e a empresa que o opera. Cada usuário desse MMORPG é uma pessoa simulada individualmente.

- Design: `docs/Plano_MMO_Tycoon_Godot_Claude.md` (arquivo do criador; nunca apagar nem mover). `docs/design.md` é só um apontador.
- O plano é **referência e base, não regra rígida**: use julgamento técnico quando houver opção melhor e registre o desvio e o motivo em `docs/decisoes.md`.
- Decisões: `docs/decisoes.md` · Backlog: `docs/backlog.md`

## Arquitetura

- `Simulation.Core`: domínio e simulação em C# puro (.NET 10). Proibido usar a API do Godot (Node, GD, RNG ou vetores da engine) e pacotes externos.
- `Simulation.Infrastructure`: JSON de conteúdo, cenários e relatórios (M0); SQLite e saves depois. O Core não depende dela.
- `Simulation.Runner`: CLI headless que compõe Core e Infrastructure.
- `Simulation.Tests`: xUnit.
- `GodotClient/`: projeto Godot 4.7.2 .NET; visualização, interface e comandos para o Core via `ProjectReference`. Nada de um nó por jogador no mapa; desenho em lote.
- `Content/presets/<mundo>/`: `world`, `localities` (fuso, idiomas, feriados), `occupations` (obrigações, renda), `regions` (com `activities`), `classes`, `abilities`, `items`, `creatures`, `population`, `progression`, `quests` (`.json`; o jogador, como desenvolvedor do MMORPG, editará esse conteúdo no jogo) + `scenario.json` e `targets.json` (só do runner: cenário e metas de validação).
- JSON é o formato de troca; o save SQLite guarda o conteúdo da campanha; a execução usa memória (D-008).

## Regras

- Seed e tick fixo (100 ms); ordem estável; comandos registrados. Ordem no tick: comandos → eventos agendados → sistemas contínuos.
- Domínio só com inteiros (permille, cobre, centavos, ticks). Nunca `float`/`double` no estado autoritativo, `DateTime.Now`, `System.Random` ou `string.GetHashCode`.
- Coleções que afetam resultados: ordenação ordinal explícita (`StringComparer.Ordinal`).
- Toda regra nova de simulação tem teste em `Simulation.Tests`. Estado novo entra no `StateHasher`.
- Eventos relevantes registram causas (`EventCause`).
- Abrir ou fechar a visão 3D não muda o resultado autoritativo.
- Interface: kit visual v3 (D-034, `docs/ui-kit.md`), estilo dossiê de jogo de gestão (FM): escuro, denso, Inter (11 a 28 px, rótulos claros), cards na grade de 12 colunas com bordas alinhadas entre linhas, ícones Lucide, gráficos analíticos, tooltips explicando. Só o mapa passa o tempo; páginas pausam. Navegação pelo menu lateral (D-035). Nada de visual de MMORPG mobile nem card vazio na visão geral. Cinzel só nos nomes de zona do mapa.
- Arquivos em UTF-8 **sem BOM**: o Godot rejeita BOM, e o PowerShell 5.1 grava BOM com `-Encoding utf8`.
- O criador não usa o editor do Godot: cenas, temas e recursos são escritos como texto e validados por linha de comando.
- Não alegar testes, desempenho ou revisão visual não executados.
- Desempenho desde já (D-032): nada de redesenhar o mapa inteiro a cada quadro; desenho estático em cache, preenchimentos em lote; rodar `tools/perf.ps1` após mudanças no cliente. Dados de demonstração sempre identificados.

## Ambiente

- .NET SDK 10.0.403 em `C:\Program Files\dotnet`. O PATH do sistema aponta primeiro para o dotnet x86 sem SDK; em PowerShell use `$env:Path = "C:\Program Files\dotnet;" + $env:Path`.
- Godot: `C:\Users\Pichau\Tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`.
- Se a extensão do VS Code definir `ELECTRON_RUN_AS_NODE`, remova-a antes de chamar apps Electron.

## Verificação

```bash
dotnet test
dotnet run --project Simulation.Runner -- --world Content/presets/inicial --days 30 --seed 42 --out reports/teste01
dotnet run --project Simulation.Runner -- --days 1 --no-skip   # verificação tick a tick
dotnet run --project Simulation.Runner -- --days 90 --population 1000 --seeds 1-5 --out reports/validacao   # metas (docs/validacao.md)
dotnet run --project Simulation.Runner -- --progression   # prévia nominal da curva de XP (progression.json)
dotnet run --project Simulation.Runner -- --controlled "casual,hardcore:sem_ocupacao,escala_5x2" --seeds 1-5 --days 60 --population 50 --out reports/controlado   # pessoas iguais por célula, humor neutro
tools/perf.ps1 -Quick   # por etapa: abre o jogo uma vez (~15 s); a completa (sem -Quick) só se o rápido acusar algo, D-032
```

## Marco atual

- M0 e teste de viabilidade concluídos. M1 quase concluído (falta a revisão em 1366×768).
- M2 fechado (D-039): plano da sessão, humor, memória, abandono e retorno (`docs/comportamento.md`); atributos do jogador em `docs/atributos.md`.
- Roteiro por etapas (backlog):
  0. Pessoa (`docs/plano-pessoa.md`): P0 a P3 feitas (D-041 a D-044: parâmetros, localidade e fuso, obrigações e tempo livre, sono e jogo no trabalho remoto); revisão da rotina R1 a R4 feitas (D-047 a D-051; a vontade de jogar da R4 foi corrigida na v3, D-051) → R5 (recalibrar e reescrever `docs/rotina.md`) → P4 traits. Reputação com a Etapa 3; finanças no M7.
  1. Etapa 1 — personagem (classes, atributos do personagem, itens iniciais, inventário e ouro).
  2. Etapa 2 — combate PvE (M3; recalibrar a curva a/b).
  3. Etapa 3 — PvP e chefe.
  4. Depois: ritmo de gestão → M5 → M6 → M7 → M8. A visão 3D (M4) fica depois dos sistemas básicos.
- Uma etapa por vez, fechada por inteiro, com resumo ao criador ao final de cada uma.
- Após cada entrega: `tools/godot.ps1 export` (o atalho "MMORPG Tycoon" na Área de Trabalho abre `build/MMOTycoon.exe`).
