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
- `Content/presets/<mundo>/`: `world`, `regions` (com `activities`), `classes`, `abilities`, `items`, `creatures`, `population` (`.json`) + `scenario.json` (só do runner).
- JSON é o formato de troca; o save SQLite guarda o conteúdo da campanha; a execução usa memória (D-008).

## Regras

- Seed e tick fixo (100 ms); ordem estável; comandos registrados. Ordem no tick: comandos → eventos agendados → sistemas contínuos.
- Domínio só com inteiros (permille, cobre, centavos, ticks). Nunca `float`/`double` no estado autoritativo, `DateTime.Now`, `System.Random` ou `string.GetHashCode`.
- Coleções que afetam resultados: ordenação ordinal explícita (`StringComparer.Ordinal`).
- Toda regra nova de simulação tem teste em `Simulation.Tests`. Estado novo entra no `StateHasher`.
- Eventos relevantes registram causas (`EventCause`).
- Abrir ou fechar a visão 3D não muda o resultado autoritativo.
- Interface consistente com os tokens e fluxos da seção 12 do plano. Textos ao jogador em pt-BR acentuado; comentários de código em pt-BR sem acentos.
- Arquivos em UTF-8 **sem BOM**: o Godot rejeita BOM, e o PowerShell 5.1 grava BOM com `-Encoding utf8`.
- O criador não usa o editor do Godot: cenas, temas e recursos são escritos como texto e validados por linha de comando.
- Não alegar testes, desempenho ou revisão visual não executados. Dados de demonstração sempre identificados.

## Ambiente

- .NET SDK 10.0.403 em `C:\Program Files\dotnet`. O PATH do sistema aponta primeiro para o dotnet x86 sem SDK; em PowerShell use `$env:Path = "C:\Program Files\dotnet;" + $env:Path`.
- Godot: `C:\Users\Pichau\Tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`.
- Se a extensão do VS Code definir `ELECTRON_RUN_AS_NODE`, remova-a antes de chamar apps Electron.

## Verificação

```bash
dotnet test
dotnet run --project Simulation.Runner -- --world Content/presets/inicial --days 30 --seed 42 --out reports/teste01
dotnet run --project Simulation.Runner -- --days 1 --no-skip   # verificação tick a tick
```

## Marco atual

- M0 concluído. Próximo: teste de viabilidade do Godot (projeto criado só pelo terminal, mapa com população se movendo) e depois M1 (HUD e mapa). Ver `docs/backlog.md`.
