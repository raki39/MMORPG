# R5 com o perfil Devoto (D-052)

Mesma configuração das referências anteriores. O perfil Devoto (cerca de 2%) entrou no preset, e o sorteio de perfil passou a dividir pelos novos pesos. A mesma seed gera pessoas diferentes das de `../pos-R5/` na campanha; no cenário controlado, as pessoas de cada célula são iguais.

| Arquivo | O que é |
|---|---|
| `validation.md`, `validation.csv` | Validação completa: 5 seeds × 1.000 pessoas × 90 dias. Regras do cenário 9 de 9; provisórias 9 de 12; futuras 1 de 5. |
| `controlled.md`, `controlled.csv` | As mesmas 36 células da R5: resultado idêntico ao de `../pos-R5/controlled.csv` (os outros perfis não mudaram). |
| `devoto-x-hardcore.md`, `.csv` | Cenário controlado: devoto e hardcore nas quatro ocupações do devoto, 50 pessoas iguais por célula, 5 seeds × 60 dias. |

Medição por pessoa (população de 6.000, seed 23, 60 dias, 110 devotos):
- horas por semana p10 47, mediana 55, p90 69, máximo 76;
- 6,8 dias de jogo por semana e 1,59 sessão por dia de jogo;
- 9h29 por dia livre e 6h12 por dia de obrigação;
- ocupações: sem ocupação 44% e estudantes 56%;
- 0% de dias cansado.
