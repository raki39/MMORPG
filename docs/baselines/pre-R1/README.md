# Referência pré-R1

Estado da simulação antes da revisão da rotina (R1–R5), guardado para separar o efeito das correções de contabilidade (R1, R2) do efeito do comportamento novo (R4).

| Campo | Valor |
|---|---|
| Commit | `52d8bef` (Rotina coerente D-045 e compromissos D-046) |
| Simulador | 0.1.0+52d8befed79145a4f1f86ca134e87d3ecf1c3735 |
| Conteúdo | `Content/presets/inicial` (hash `ae3b04a052743364`) |
| Metas | `targets.json` (hash `322f4c445d8c8479`) |
| Validação | 5 seeds (1–5) × 1.000 pessoas × 90 dias; semana de lançamento excluída |
| Resultado | Regras do cenário 7 de 7; provisórias 8 de 12; futuras 1 de 5 |
| Verificação tick a tick | seed 42, 100 pessoas, 1 dia: hash `2034f72d5308abc0` (sem a identificação do conteúdo: `b8dfce4165c21162`) |

Para reproduzir:

```bash
git checkout 52d8bef
dotnet run --project Simulation.Runner -- --days 90 --population 1000 --seeds 1-5 --out reports/baseline-pre-R1
```

Arquivos: `validation.md` (relatório completo) e `validation.csv` (valores por seed).
