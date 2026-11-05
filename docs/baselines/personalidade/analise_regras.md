# P4a — Agressivo e deslocamento

Validação padrão: 5 seeds × 1.000 pessoas × 90 dias. **P0** = personalidade classificada, sem regras; **P1** = com regras (preset). P0 e P1 têm as mesmas pessoas.

## 1. Agressivo: PvP de 8,7% para 31%

A pergunta do criador: ele ainda responde às outras preferências, aos objetivos e ao risco, ou ficou preso à emboscada?

**Distribuição por pessoa** (parcela do tempo em patrulha e emboscada; n = 350):

| | p10 | p25 | mediana | p75 | p90 | máx. | acima de 50% | zero |
|---|---|---|---|---|---|---|---|---|
| P0 | 0,0 | 0,0 | 0,8 | 8,0 | 19,1 | 49,9 | 0,0% | 40,0% |
| P1 | 0,0 | 3,5 | 17,7 | 37,1 | 55,0 | 84,4 | 13,4% | 16,3% |

- **Patrulha** 15,5% e **emboscada** 7,4%, na média por pessoa: não há concentração na emboscada.

**PvP pela preferência de PvP** (quartis):

| Quartil | P0 | P1 |
|---|---|---|
| Q1 (até 237) | 1,2 | 8,7 |
| Q2 | 2,2 | 16,0 |
| Q3 | 4,6 | 23,2 |
| Q4 (acima de 682) | 16,4 | 43,8 |

**PvP pela aversão a risco** (quartis; valores da criação, refeita com as mesmas seeds):

| Quartil | P0 | P1 |
|---|---|---|
| Q1 (pouca aversão) | 10,2 | 31,9 |
| Q2 | 7,3 | 27,7 |
| Q3 | 4,3 | 19,5 |
| Q4 (muita aversão) | 2,6 | 12,7 |

- Correlação de postos (Spearman) entre aversão a risco e PvP: −0,28 em P0 e −0,36 em P1. Nos demais (sem Agressivo e Pacífico), −0,20.

**As outras preferências continuam valendo** (Spearman entre preferência e tempo no tema):

| Tema | Agressivo P0 | Agressivo P1 | Demais P1 |
|---|---|---|---|
| PvE | +0,84 | +0,83 | +0,81 |
| Coleta | +0,76 | +0,75 | +0,74 |
| Exploração | +0,65 | +0,63 | +0,63 |
| Social | +0,76 | +0,75 | +0,74 |
| Comércio | +0,80 | +0,74 | +0,74 |

- Quem gosta de coletar continua coletando: Agressivos acima da mediana de coleta passam de 20,7% para 18,1% de coleta.

**Metas e progressão:**
- **Nível mediano:** de 27 para 28.
- **Horas até o nível 10** (mediana): de 14,9 h para 14,5 h.
- **Metas de desbloqueio cumpridas:** de 93% para 94%.

**PvP pelo nível ao fim:**
- 1 a 10: 0%;
- 11 a 20: 12,6%;
- 21 a 30: 26,5%.

O conteúdo de PvP continua dependendo do nível.

**Conclusão:** o efeito é forte, mas o Agressivo continua respondendo à preferência de PvP, ao risco, às outras preferências e às metas. Ele não ficou preso a uma atividade. O bônus de 150 fica como valor candidato.

## 2. "Andando ou esperando": de 19,4% para 20,3% (limite provisório de 20%)

Deslocamento e espera, em % do tempo conectado, ponderado pelo tempo:

| Personalidade | P0 | P1 | Contribuição ao aumento geral (pontos) |
|---|---|---|---|
| Inquieto | 21,1 | 31,9 | +0,83 |
| Paciente | 18,6 | 23,9 | +0,41 |
| Versátil | 21,2 | 25,8 | +0,35 |
| Independente | 18,9 | 20,2 | +0,10 |
| Agressivo | 19,7 | 16,9 | −0,22 |
| Pacífico | 19,4 | 16,8 | −0,20 |
| Metódico | 17,4 | 14,6 | −0,19 |
| Demais | — | — | entre −0,07 e +0,01 |

A espera sem opção viável (idle) é 0,0% nas duas versões: o aumento é todo de deslocamento.

**Por que essas três andam mais:**
- **Inquieto:** depois de 2 ciclos seguidos na mesma atividade, troca de lugar, e a troca costuma exigir deslocamento.
  - A distribuição inteira se desloca: a mediana por pessoa vai de 19,8% para 30,9%, o p25 de 14,8% para 27,2%.
  - Não é um ciclo vazio de poucos casos, como o do Independente antes da correção. Nível e horas não mudam.
- **Paciente:** fecha as etapas de missão (+100), que muitas vezes ficam em outra zona. O tempo em missões vai de 1,9% para 2,7%.
- **Versátil:** começa a sessão por uma atividade diferente da sessão anterior.

**Decisão do criador:** 20,3% contra um limite provisório de 20% não justifica, sozinho, enfraquecer personalidades. As regras ficam como estão, e a divergência fica registrada em `validacao.md`.

**Observação para uma calibração futura:** a repetição do Inquieto é contada em ciclos de decisão, e os ciclos têm durações diferentes por atividade. Um ciclo de comércio dura 15 min; um de caça, 25 a 30 min. Contar em minutos deixaria a regra mais uniforme. Não foi aplicado.
