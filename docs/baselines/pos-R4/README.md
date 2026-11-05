# Medição depois da R4 (modelo de vontade, D-050)

Mesma configuração e população da referência pré-R4 (`../pre-R4/`), para comparar as mesmas distribuições antes e depois. Nada foi recalibrado; a calibração é a R5.

| Arquivo | O que é |
|---|---|
| `validation.md`, `validation.csv` | Validação completa: 5 seeds (1–5) × 1.000 pessoas × 90 dias. Regras do cenário 7 de 7; provisórias 8 de 12; futuras 1 de 5. |
| `controlled.md`, `controlled.csv` | Cenário controlado: 36 células perfil × ocupação, 50 pessoas iguais por célula, 5 seeds × 60 dias, humor neutro. |
| `comparacao-controlada.md` | Cada célula antes → depois (pré-R4 → R4). |

Comandos:

```bash
dotnet run --project Simulation.Runner -- --days 90 --population 1000 --seeds 1-5 --out reports/r4/validacao
dotnet run --project Simulation.Runner -- --controlled "casual,social,comerciante,explorador,competitivo,hardcore:sem_ocupacao,escala_5x2,meio_periodo_manha,home_office,turno_noturno,trabalho_estudo" --seeds 1-5 --days 60 --population 50 --out reports/r4/controlled-pos
```
