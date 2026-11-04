# MMORPG Tycoon (nome provisório)

Jogo de gestão single-player e offline em que você cria e administra um MMORPG e a empresa que o opera. Cada jogador desse MMORPG é uma pessoa simulada individualmente, com rotina, personalidade, memória, inventário e relações.

**Estado:** M0 (fundação da simulação) concluído. Cliente gráfico em Godot 4.7 (.NET) a seguir.

## Requisitos

- .NET SDK 10.0.403 ou superior da linha 10.0
- Godot 4.7.2 edição .NET (a partir do cliente gráfico)

## Executar

```bash
dotnet test
dotnet run --project Simulation.Runner -- --world Content/presets/inicial --days 30 --seed 42 --out reports/teste01
```

O runner gera em `--out`: `summary.md` (resumo legível), `daily.csv`, `events.csv`, `commands.csv`, `population.csv` e `metadata.json`. A mesma seed, o mesmo conteúdo e os mesmos comandos produzem o mesmo hash de estado.

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `Simulation.Core/` | Domínio e simulação em C# puro, sem engine |
| `Simulation.Infrastructure/` | Carregamento de conteúdo JSON, cenários e relatórios |
| `Simulation.Runner/` | CLI headless |
| `Simulation.Tests/` | Testes automatizados (xUnit) |
| `Content/presets/inicial/` | Mundo inicial em JSON |
| `docs/` | Plano de design, decisões e backlog |
