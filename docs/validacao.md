# Validação da simulação

Este documento registra o que foi medido na simulação, contra quais metas e o que os números mostram. Os relatórios completos ficam em `reports/` (fora do Git). Aqui ficam só as conclusões.

## Como rodar

```bash
dotnet run --project Simulation.Runner -- --days 90 --population 1000 --seeds 1-5 --out reports/validacao
```

- As seeds rodam em paralelo.
- O comando gera `validation.md` (metas, perfis, uso do tempo e atividades pouco usadas, com diagnóstico) e `validation.csv` (valor de cada meta em cada seed).
- Uma execução simples (`--seed`) também gera `validation.md` quando o mundo tem metas.

## Metas

As metas ficam em [`Content/presets/inicial/targets.json`](../Content/presets/inicial/targets.json).

- **São hipóteses de design, não regras do mundo.** O Claude propôs as faixas em 04/11/2026, antes de rodar as execuções grandes, para não ajustar a meta ao resultado. O criador pode e deve ajustá-las.
- **Uma meta só conta como "dentro" quando todas as seeds caem na faixa.**
  - Se a média está na faixa mas alguma seed escapa, a meta fica "instável".
- **Medidas semanais ignoram a semana de lançamento** (`warmupDays`).

## Resultado de 04/11/2026

Rodada de 5 seeds × 1.000 pessoas × 90 dias. **9 de 16 metas ficaram dentro da faixa.** A variação entre seeds é pequena (menos de 2 pontos na maioria das medidas), então os desvios são do modelo, não do acaso.

### O que está validado: rotina e calendário

| Medida | Meta | Resultado |
|---|---|---|
| Sessões por semana, casual | 2–4 | 3,1 |
| Sessões por semana, hardcore | 5–7 | 5,4 |
| Horas por semana, casual | 2–6 h | 4,3 h |
| Horas por semana, hardcore | 15–35 h | 25,1 h |
| Ativos em dia útil ÷ cadastrados | 30–55% | 52% |
| Ativos no fim de semana ÷ dia útil | 1,3–2,0× | 1,6× |
| Pico online ÷ cadastrados | 15–35% | 29% |

**Teste estatístico do "trabalhador 19h–23h"** (plano 19.4, `ValidationTests`):
- **Configuração:** um perfil com hora preferida 21 ± 2 e variação de ± 75 min.
- **Proporção esperada:** a proporção exata de inícios de sessão entre 19h e 23h, calculada a partir dessa regra, é **96,08%**.
- **Resultado observado:** **96,07%**, em 1.000 pessoas e 8.798 sessões de dia útil (5 seeds).
- **Tolerância:** 4 erros-padrão contados por pessoa, ou seja, ±2,45 pontos.
  - Conta por pessoa e não por sessão porque a hora preferida é fixa para cada pessoa.

Conclusão: quem joga, quando e por quanto tempo segue o que os perfis configuram.

### O que está fora, e por quê

**1. A progressão está cerca de 3 vezes mais lenta que a meta.**
- **Números:** nível 5 com 9,9 h jogadas (meta 2–6 h) e nível 10 com 65 h (meta 10–25 h). Só 27% dos casuais chegam ao nível 10 em 90 dias (meta 40–80%).
- **Causa principal: só 57% do tempo conectado dá XP.**
  - deslocamento: 27%;
  - taverna e comércio, que não dão XP: 15%.
- **Causa secundária: a curva de XP** (60n² + 140n).
- Não é bug; são as duas alavancas para a conferência da fórmula.

**2. Deslocamento ocupa 27% do tempo** (meta até 20%).
- **Zonas grandes:** têm raio de 1,2 km. Ir de uma zona à vizinha leva cerca de 15 minutos a pé (9 km/h).
- **Atividades curtas:** duram de 15 a 30 minutos.
- **Troca frequente:** a penalidade de repetição empurra a pessoa para outra atividade, muitas vezes em outra zona.
- **Alavancas:** atividades mais longas na mesma área, penalidade de repetição menor dentro da mesma zona, ou montaria e velocidade maior (decisão de design).

**3. O quadro de missões da cidade domina as escolhas** (35% das escolhas, 16% do tempo).
- A cidade tem faixa de nível 1–60. Por isso, a missão sempre tem "adequação ao nível" máxima, não exige viagem e não tem risco.
- **Exemplo:** acima do nível 8, caçar na Floresta rende menos XP por minuto (8,4) que pegar missão na cidade (10).
- É falha da fórmula: a missão deveria ter nível, como as zonas.

**4. Pouco tempo em combate** (21%, meta 35–55%).
- Caça contra criaturas: 11%.
- **PvP sem combate de verdade:** emboscada e patrulha somam 10% do tempo, mas ainda não existe combate entre jogadores. Hoje são "XP com risco declarado" sem nenhuma consequência.
- Isso só fica realista com o combate (M3).

**5. A exploração não cansa** (15%, no limite da faixa).
- Explorar continua atraente depois que a pessoa já conhece o mapa inteiro.
- Falta um termo de novidade na nota.

**6. Pouca coleta** (4,5%, meta 8–20%).
- Coleta dá pouco XP (5 por minuto) e o recurso coletado ainda não vale nada, porque a economia só chega no M5.

### Atividades pouco usadas

| Atividade | Escolhas | Diagnóstico |
|---|---|---|
| Enfrentar o Rei Goblin (Covil) | 0% | Exige grupo de 5 e nível 15. Grupos só existem no M6, então hoje ninguém pode fazer. **Não é bug.** |
| Recrutar para grupo (Vila) | 0% | Depende do sistema de grupos (M6). |
| Coletar cristais sombrios (Ruínas) | 0% | Exige nível 10, que só 48% alcançam, numa zona de PvP com perda total. |
| Consertar equipamento (Vila) | 0,3% | Ainda não existe desgaste de equipamento nem economia, então não há motivo para consertar. |

### Bug encontrado durante a validação

A primeira rodada mostrou 10,5% do tempo em "espera sem opção viável". Investigando, ninguém ficava de fato sem opção. O problema era a contagem nova de tempo: quando a atividade terminava junto com a sessão, o tempo era somado duas vezes.

Foi corrigido, e um teste (`TempoJogado_FechaComAsSessoesEAsAtividades`) agora confere que a soma fecha com as sessões.

## Próximos passos

1. **Conferir a fórmula de decisão** com o criador (itens 1, 2, 3, 5 e 6 acima) e registrar os desvios em relação ao plano 4.4.
2. **Combate (M3)**, que dá consequência real à caça, ao PvP, à morte e ao saque (item 4).
3. **Reavaliar as metas de progressão.** Depende de decidir quanto tempo o jogo deve levar até o nível máximo do conteúdo atual (18).
