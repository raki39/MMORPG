# Validação da simulação

> Gerado pelo `Simulation.Runner` com várias seeds. As faixas vêm de `targets.json` e são **hipóteses de design ajustáveis**, não regras do mundo. Uma meta só conta como "dentro" quando todas as seeds caem na faixa.

| Campo | Valor |
|---|---|
| Conteúdo | `reports/exp-ded/mundos/B0` (`4a4bedc5916b379f`) |
| Metas | `reports/exp-ded/mundos/B0/targets.json` (`d8f348f9966ab923`) |
| Simulador | 0.1.0+ebf4e31c53eede8349491bbc1daecf818cb137e9 |
| Execuções | 5 seeds (1, 2, 3, 4, 5) · 1.000 pessoas · 90 dias |
| Janela das medidas semanais | a partir do dia 7 (exclui a semana de lançamento) |
| Tempo total | 374,2 s |

## Metas

As metas têm três classes (D-039a); o total "x de y" sozinho não diz se dá para avançar:

- **Regras do cenário:** rotina calibrada, medida numa rodada controlada com o humor neutralizado. Falhar aqui indica erro.
- **Provisórias:** balanceamento que será recalibrado (progressão, divisão do tempo, missões).
- **Dependem de sistemas futuros:** só acompanhamento, não reprovam (retenção, missões e coleta dependem de combate, economia e social).

### Regras do cenário: 9 de 9 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Dias de jogo por semana — casual | 2,0–4,0  | 2,8 | 2,8–2,8 |  |
| ✅ dentro | Dias de jogo por semana — hardcore | 5,0–7,0  | 6,2 | 5,9–6,4 |  |
| ✅ dentro | Sessões por dia de jogo — casual | 1,1–2,0  | 1,5 | 1,5–1,5 |  |
| ✅ dentro | Sessões por dia de jogo — hardcore | 1,2–2,5  | 1,5 | 1,5–1,6 |  |
| ✅ dentro | Horas por semana — casual | 3,0–9,0 h | 6,7 | 6,7–6,8 |  |
| ✅ dentro | Horas por semana — hardcore | 15,0–45,0 h | 36,0 | 35,0–37,6 |  |
| ✅ dentro | Jogadores ativos em dia útil / cadastrados | 30,0–55,0 % | 52,3 | 51,4–53,3 | 58 dias |
| ✅ dentro | Ativos no fim de semana ÷ ativos em dia útil | 1,3–2,0 × | 1,3 | 1,3–1,3 |  |
| ✅ dentro | Pico online do dia / cadastrados | 15,0–35,0 % | 24,8 | 23,6–25,9 |  |

### Provisórias: 9 de 12 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Horas conectadas até o nível 5 (mediana) | 1,0–4,0 h | 2,7 | 2,7–2,7 | 100% chegaram |
| ✅ dentro | Horas conectadas até o nível 10 (mediana) | 8,0–20,0 h | 15,5 | 15,4–15,7 | 98–99% chegaram |
| ✅ dentro | Horas conectadas até o nível 15 (mediana) | 22,0–50,0 h | 36,7 | 36,5–36,8 | 79–82% chegaram |
| ❌ abaixo | Horas conectadas até o nível 20 (mediana) | 80,0–110 h | 69,8 | 69,3–70,3 | 60–62% chegaram |
| ✅ dentro | Horas conectadas até o nível 30 (mediana) | 170–230 h | 173 | 171–175 | 28–30% chegaram (mediana só de quem chegou: subestima o tempo real) |
| ❌ acima | Casuais no nível 10 ou mais ao fim | 60,0–95,0 % | 98,7 | 97,9–99,4 |  |
| ✅ dentro | XP vindo de quests | 15,0–35,0 % | 20,7 | 19,9–21,3 |  |
| ✅ dentro | Quests concluídas por personagem (média) | 4,0–30,0  | 28,5 | 27,8–29,0 |  |
| ✅ dentro | Tempo em atividades de combate (abstratas: caça, chefe, patrulha, emboscada; sem luta simulada até o M3) | 35,0–55,0 % | 41,7 | 41,2–42,1 |  |
| ✅ dentro | Tempo explorando | 5,0–15,0 % | 6,9 | 6,5–7,6 |  |
| ✅ dentro | Tempo na cidade (comércio, taverna, treino, conserto) | 10,0–25,0 % | 22,5 | 22,2–22,9 |  |
| ❌ acima | Tempo andando ou esperando | 5,0–20,0 % | 21,4 | 21,3–21,6 |  |

### Dependem de sistemas futuros (acompanhamento): 1 de 5 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ❌ abaixo | Tempo em missões | 10,0–25,0 % | 2,2 | 2,2–2,3 |  |
| ❌ abaixo | Tempo em coleta | 8,0–20,0 % | 5,2 | 4,9–5,4 |  |
| ❌ abaixo | Desistentes no fim / cadastrados | 8,0–30,0 % | 0,8 | 0,4–1,0 |  |
| ❌ abaixo | Inativos no fim / cadastrados | 2,0–15,0 % | 1,1 | 0,8–1,5 |  |
| ✅ dentro | Quem saiu e voltou (inativos e desistentes) | 20,0–70,0 % | 56,9 | 47,4–67,6 | 38 saíram; 42 saíram; 37 saíram; 40 saíram |


Por que cada faixa:

- **Horas conectadas até o nível 5 (mediana):** Ritmo A (D-027): o começo apresenta o jogo; a prévia nominal dá cerca de 2 h. Referência de perfil, não prazo para todos.
- **Horas conectadas até o nível 10 (mediana):** Ritmo A (D-027): prévia nominal de cerca de 11 h, contando viagem e cidade. A desaceleração forte fica para as faixas altas.
- **Horas conectadas até o nível 15 (mediana):** Ritmo A (D-027): prévia nominal de cerca de 31 h. O conteúdo atual termina perto do 15–18 até a redistribuição das áreas.
- **Horas conectadas até o nível 20 (mediana):** Referência do criador: cerca de 95 h até o 20. Recalibrar depois das quests estruturadas (passo 3 do D-025).
- **Horas conectadas até o nível 30 (mediana):** Referência do criador: cerca de 200 h até o teto. Recalibrar depois das quests estruturadas (passo 3 do D-025).
- **Casuais no nível 10 ou mais ao fim:** Em 90 dias um casual joga cerca de 50 h; a maioria deve passar do 10, sem ser regra para todos.
- **XP vindo de quests:** D-025: quests guiam e recompensam, mas evoluir não pode depender delas nem das diárias; a caça e o mundo continuam sendo a maior parte.
- **Quests concluídas por personagem (média):** Em 90 dias, um personagem típico deve concluir várias quests (principais, secundárias e algumas diárias).
- **Dias de jogo por semana — casual:** Tendência do perfil: o casual joga em cerca de 3 dias por semana. Mesma faixa da antiga meta de sessões por semana, que na prática media dias quando havia uma sessão por dia (R5, D-052).
- **Dias de jogo por semana — hardcore:** Tendência do perfil: o hardcore joga em cerca de 6 dias por semana. Mesma faixa da antiga meta de sessões por semana (R5, D-052).
- **Sessões por dia de jogo — casual:** Tendência (D-051): na maioria dos dias o casual faz uma sessão, e voltar depois de uma pausa é comum, sobretudo no dia livre. Abaixo de 1,1 a volta quase não existiria; acima de 2 jogaria em várias sessões quase todo dia.
- **Sessões por dia de jogo — hardcore:** Tendência (D-051): quem prioriza o jogo volta com mais frequência quando há tempo, mas a sessão normal longa limita quantas cabem no dia.
- **Horas por semana — casual:** Cerca de 3 dias de jogo; no dia livre, 1 ou 2 sessões de 1h30 com pausa (D-051: comportamento da v3, considerado coerente pelo criador em 05/11/2026, ainda sem avaliação jogável). A faixa antiga (2–6 h, '3 sessões de 70 minutos') supunha uma sessão por dia (R5, D-052).
- **Horas por semana — hardcore:** Cerca de 6 sessões de 4 a 6 horas; quem tem muito tempo livre e prioriza o jogo chega a 35–45 h (direção do criador, D-045: o hardcore ainda estava abaixo do real).
- **Jogadores ativos em dia útil / cadastrados:** Regra do cenário: medida com o humor neutralizado (D-039a). Na campanha completa, faltas por desânimo são resultado legítimo.
- **Ativos no fim de semana ÷ ativos em dia útil:** O fim de semana deve ser claramente mais cheio, sem dobrar a base inteira.
- **Pico online do dia / cadastrados:** Concentração no horário nobre: parte relevante dos ativos do dia ao mesmo tempo.
- **Tempo em atividades de combate (abstratas: caça, chefe, patrulha, emboscada; sem luta simulada até o M3):** Caça é o núcleo do PvE de um MMORPG de fantasia, mas não pode ser tudo.
- **Tempo em missões:** Missões guiam o início, mas não substituem a caça.
- **Tempo em coleta:** Coleta alimenta a economia futura (M5); precisa existir desde já.
- **Tempo explorando:** Explorar revela o mapa e é a motivação do perfil explorador, mas cansa depois de conhecer tudo.
- **Tempo na cidade (comércio, taverna, treino, conserto):** A cidade é o ponto de encontro e de serviços; sem ela o mundo vira só caça.
- **Tempo andando ou esperando:** Caminhar faz parte do mundo, mas acima de 20% da sessão vira perda de tempo.
- **Desistentes no fim / cadastrados:** Hipótese do Claude (D-039): em 90 dias parte da base desiste, sobretudo quem esgota o conteúdo ou não acha o que gosta. Sem mortes, economia e social, as causas ainda são poucas; a faixa sobe quando esses sistemas existirem.
- **Inativos no fim / cadastrados:** Hipótese do Claude (D-039): parte dos irregulares fica mais de 14 dias sem jogar; a maioria volta ou desiste.
- **Quem saiu e voltou (inativos e desistentes):** Hipótese do Claude (D-039): inativos voltam pela rotina; desistentes, pela saudade (atualizações e convites no M6/M7).

## Rotina pura (rodada controlada, humor neutralizado)

| Perfil | Dias de jogo/semana | Sessões por dia de jogo | Sessões/semana | Horas/semana |
|---|---:|---:|---:|---:|
| Casual | 2,8 | 1,49 | 4,2 | 6,7 |
| Comerciante | 4,4 | 1,71 | 7,5 | 14,3 |
| Competitivo | 4,7 | 1,58 | 7,5 | 20,6 |
| Devoto | 7,0 | 1,53 | 10,7 | 57,9 |
| Explorador | 4,1 | 1,67 | 6,8 | 15,7 |
| Hardcore | 6,2 | 1,53 | 9,4 | 36,0 |
| Social | 3,7 | 1,56 | 5,7 | 12,4 |

## Perfis na campanha completa (média das seeds)

| Perfil | Pessoas | Dias de jogo/semana | Sessões por dia de jogo | Horas/semana | Nível mediano no fim | Nível p90 | Sessões em dia útil iniciadas entre 18h e 23h |
|---|---:|---:|---:|---:|---:|---:|---:|
| Casual | 346 | 2,9 | 1,51 | 7,1 | 20,0 | 25,6 | 51,9% |
| Comerciante | 97,4 | 4,4 | 1,71 | 14,5 | 16,8 | 21,4 | 50,5% |
| Competitivo | 145 | 4,9 | 1,58 | 21,5 | 30,0 | 30,0 | 52,9% |
| Devoto | 18,6 | 6,9 | 1,53 | 57,2 | 30,0 | 30,0 | 37,1% |
| Explorador | 98,0 | 4,5 | 1,70 | 17,5 | 28,8 | 30,0 | 46,5% |
| Hardcore | 97,2 | 6,2 | 1,53 | 36,4 | 30,0 | 30,0 | 51,7% |
| Social | 197 | 3,9 | 1,57 | 13,0 | 12,8 | 17,2 | 52,6% |

## Evolução dos atributos do jogador (média das seeds)

Variação média desde o início, em **pontos da escala 1–20** (1 ponto ≈ 52,6 unidades do Core), ao fim de 90 dias. Valores de `population.json` (`dynamics`).

| Perfil | Mecânica | Conhecimento | PvE | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | +0,13 | +1,30 | +0,20 | +0,11 | +0,01 | 0,00 | 14,0 | 3,1 |
| Comerciante | +0,03 | +0,99 | +0,10 | +0,06 | +0,32 | 0,00 | 22,3 | 7,7 |
| Competitivo | +0,84 | +1,26 | +0,29 | +0,06 | +0,03 | -0,24 | 19,5 | 14,8 |
| Devoto | +1,17 | +0,98 | +1,02 | +0,05 | +0,01 | -0,17 | 43,8 | 3,1 |
| Explorador | +0,29 | +1,48 | +0,17 | +0,42 | +0,02 | -0,05 | 21,6 | 7,5 |
| Hardcore | +1,04 | +0,97 | +0,85 | +0,05 | 0,00 | -0,10 | 35,8 | 4,9 |
| Social | +0,03 | +1,09 | +0,07 | +0,04 | 0,00 | 0,00 | 18,3 | 7,9 |

Inatividade (queda, sem modificadores):

| Dias parado | Mecânica | Conhecimento |
|---:|---:|---:|
| 7 | 0,00 | 0,00 |
| 14 | -0,13 | 0,00 |
| 30 | -0,44 | -0,30 |
| 60 | -1,01 | -0,87 |
| 90 | -1,58 | -1,44 |

## Rotina por ocupação (média das seeds, campanha completa)

Obrigações e sono definem o tempo livre; a vontade de jogar decide se e quanto. Tempo livre medido na agenda dos últimos dias. Renda e apoio em reais por mês (sem efeito até o M7).

| Ocupação | Pessoas | Sessões/semana | Horas/semana | Livre em dia com obrigação | Livre em dia livre | Dias sem tempo | Renda | Apoio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Aposentado | 20,0 | 8,9 | 21,5 | — | 15,3 h | 0,0% | R$ 3.438 | R$ 37,2 |
| Autônomo / freelancer | 42,2 | 8,4 | 18,0 | 8,9 h | 15,2 h | 0,0% | R$ 4.312 | R$ 41,2 |
| Trabalho em horário comercial | 227 | 5,2 | 12,8 | 4,2 h | 14,8 h | 0,0% | R$ 4.417 | R$ 30,0 |
| Cuidando da casa e da família | 30,2 | 7,8 | 16,1 | 8,4 h | — | 0,0% | R$ 0,0 | R$ 2.264 |
| Comércio e serviços em escala 5x2 | 82,4 | 4,8 | 11,6 | 3,3 h | 14,9 h | 0,0% | R$ 2.595 | R$ 31,2 |
| Comércio em escala 6x1 | 37,0 | 4,7 | 10,7 | 4,8 h | 15,0 h | 0,0% | R$ 2.721 | R$ 27,8 |
| Estudante (manhã) | 108 | 7,4 | 19,4 | 8,4 h | 14,7 h | 0,0% | R$ 0,0 | R$ 732 |
| Estudante (tarde) | 46,2 | 7,7 | 19,0 | 8,7 h | 15,2 h | 0,0% | R$ 0,0 | R$ 749 |
| Home office (tecnologia) | 80,2 | 7,1 | 15,0 | 6,4 h | 15,1 h | 0,0% | R$ 8.225 | R$ 16,7 |
| Meio período (manhã) | 30,0 | 7,7 | 19,3 | 9,5 h | 14,8 h | 0,0% | R$ 1.964 | R$ 291 |
| Meio período (tarde) | 25,6 | 7,9 | 18,2 | 9,3 h | 15,1 h | 0,0% | R$ 1.962 | R$ 290 |
| Sem ocupação | 80,8 | 9,6 | 27,0 | — | 15,3 h | 0,0% | R$ 0,0 | R$ 832 |
| Trabalha de dia e estuda à noite | 56,2 | 2,5 | 6,2 | 1,0 h | 12,9 h | 53,2% | R$ 3.388 | R$ 52,4 |
| Turno noturno | 31,8 | 5,7 | 14,2 | 7,1 h | 11,4 h | 0,0% | R$ 3.611 | R$ 19,7 |
| Universitário (diurno) | 102 | 6,6 | 16,8 | 7,1 h | 14,8 h | 0,0% | R$ 522 | R$ 1.366 |

Como terminaram os dias da agenda:

| Dia | Parcela |
|---|---:|
| Jogou | 56,7% |
| Tinha tempo, preferiu outro lazer | 39,0% |
| Sem tempo livre | 3,0% |
| Inativo | 1,0% |
| Cansado (dormiu pouco) | 0,4% |
| Sem vontade (engajamento baixo) | 0,0% |

## Sono e dificuldade de parar (média das seeds, campanha completa)

Importância do jogo, tendência a prolongar e autocontrole são traços separados. A extensão tem limites explícitos (vezes por sessão, minutos, sono mínimo e teto do déficit); só corta sono no fim do dia, nunca uma obrigação. Sono cortado vira déficit; manhã livre e noites normais recuperam.

- Extensões na janela: 439 (158 gostando da sessão, 281 perto de concluir a meta); 439 avançaram sobre a hora de dormir.
- Noites encurtadas pelo jogo: 395, 70,2 min a menos em média.

| Perfil | Extensões/semana | Sono cortado/semana | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|
| Casual | 0,0 | 0,1 min | 0,2 h | 2,0% |
| Comerciante | 0,0 | 0,4 min | 0,4 h | 2,8% |
| Competitivo | 0,0 | 2,4 min | 0,3 h | 1,9% |
| Devoto | 0,1 | 10,4 min | 0,0 h | 0,0% |
| Explorador | 0,0 | 1,2 min | 0,2 h | 1,6% |
| Hardcore | 0,2 | 14,3 min | 0,4 h | 3,4% |
| Social | 0,0 | 1,0 min | 0,2 h | 1,8% |

Sono da noite anterior pela agenda (o sono cortado pelo jogo vai direto para o déficit). Em dia livre a pessoa dorme mais para recuperar. O déficit no fim depende do dia da semana em que a campanha termina.

| Ocupação | Precisa | Sono em dia com obrigação | Sono em dia livre | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|---:|
| Aposentado | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Autônomo / freelancer | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Trabalho em horário comercial | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Cuidando da casa e da família | 7,8 h | 7,8 h | — | 0,0 h | 0,0% |
| Comércio e serviços em escala 5x2 | 7,8 h | 7,8 h | 7,9 h | 0,0 h | 0,0% |
| Comércio em escala 6x1 | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Estudante (manhã) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Estudante (tarde) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Home office (tecnologia) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Meio período (manhã) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Meio período (tarde) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Sem ocupação | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Trabalha de dia e estuda à noite | 7,9 h | 6,9 h | 9,7 h | 4,5 h | 37,3% |
| Turno noturno | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Universitário (diurno) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |

## Jogar no horário de trabalho (média das seeds)

Só no trabalho remoto. Pausa: jogar no intervalo permitido. Escapada: jogar no expediente, rara, pela flexibilidade do trabalho, importância do jogo, falta de autocontrole e folga do dia; sessão curta e interrompível. Deslocamento, trabalho presencial e estudo ficam protegidos. Sem consequência profissional por enquanto.

- Sessões na pausa: 1.106 (1,4% das sessões); escapadas: 220 (0,3%).
- Chamados do trabalho durante a escapada: 77,4 (7,4 terminaram a atividade, 70,0 largaram e saíram).

| Ocupação | Pausa/semana | Escapadas/semana | Dias de trabalho com escapada |
|---|---:|---:|---:|
| Autônomo / freelancer | 0,8 | 0,2 | 3,1% |
| Home office (tecnologia) | 0,8 | 0,1 | 2,7% |

## Distribuições (R3)

Medição da campanha completa (com humor), seeds somadas, na janela de medição. A referência antes da R4 (modelo de vontade) está em `docs/baselines/pre-R4/`, com o mesmo relatório e o cenário controlado (pessoas iguais por célula perfil × ocupação), que separa o efeito das regras da mistura da população.

**Duração das sessões (minutos, sessões normais).** Inclui extensões e o fim real; pausa e escapada do trabalho à parte.

| Perfil | Sessões | p10 | p25 | Mediana | p75 | p90 | Máx. | Média |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 89.064 | 63,0 | 77,0 | 93,0 | 112 | 137 | 370 | 97,2 |
| Comerciante | 43.017 | 72,0 | 89,0 | 109 | 136 | 169 | 514 | 116 |
| Competitivo | 65.389 | 101 | 135 | 167 | 200 | 236 | 553 | 168 |
| Devoto | 11.743 | 109 | 240 | 345 | 415 | 475 | 818 | 322 |
| Explorador | 43.322 | 90,0 | 113 | 136 | 163 | 196 | 467 | 140 |
| Hardcore | 53.936 | 113 | 192 | 237 | 281 | 331 | 756 | 233 |
| Social | 69.548 | 83,0 | 103 | 126 | 153 | 185 | 530 | 130 |
| Pausa no trabalho | 5.529 | 35,0 | 41,0 | 54,0 | 55,0 | 55,0 | 55,0 | 48,2 |
| Escapada no expediente | 1.101 | 9,0 | 17,0 | 23,0 | 26,0 | 29,0 | 34,0 | 21,2 |

**Horas por semana, perfil × ocupação** (por pessoa registrada; "—" com menos de 10 pessoas na soma das seeds).

| Ocupação | Casual | Comerciante | Competitivo | Devoto | Explorador | Hardcore | Social |
|---|---:|---:|---:|---:|---:|---:|---:|
| Aposentado | 11,9 | 21,8 | 32,3 | — | 27,5 | — | 20,1 |
| Autônomo / freelancer | 8,3 | 18,1 | 26,6 | — | 20,3 | 43,8 | 16,3 |
| Trabalho em horário comercial | 5,7 | 11,8 | 18,1 | — | 14,3 | 32,8 | 10,7 |
| Cuidando da casa e da família | 6,0 | 12,7 | 20,5 | — | 16,3 | 36,9 | 12,6 |
| Comércio e serviços em escala 5x2 | 4,9 | 11,0 | 17,6 | — | 12,9 | 28,5 | 9,6 |
| Comércio em escala 6x1 | 4,6 | 10,7 | 15,6 | — | 11,9 | 28,6 | 8,4 |
| Estudante (manhã) | 8,5 | 17,2 | 26,1 | 54,5 | 20,7 | 43,3 | 15,5 |
| Estudante (tarde) | 8,1 | 15,6 | 22,6 | 50,6 | 20,0 | 43,0 | 14,3 |
| Home office (tecnologia) | 7,0 | 15,7 | 22,2 | — | 16,3 | 37,7 | 13,6 |
| Meio período (manhã) | 9,2 | 18,3 | 28,8 | — | 22,2 | 47,7 | 17,2 |
| Meio período (tarde) | 8,7 | 16,9 | 24,1 | — | 21,2 | 43,1 | 15,3 |
| Sem ocupação | 12,2 | 21,7 | 31,2 | 66,8 | 28,0 | 51,2 | 21,2 |
| Trabalha de dia e estuda à noite | 2,8 | 5,6 | 8,7 | — | 7,0 | 15,0 | 5,2 |
| Turno noturno | 6,1 | 13,1 | 19,7 | — | 14,1 | 33,8 | 11,1 |
| Universitário (diurno) | 7,6 | 15,2 | 23,1 | 50,4 | 18,0 | 39,5 | 13,6 |

**Sessões por dia de jogo:** 57,2% com uma, 30,3% com duas, 12,5% com três ou mais. Intervalo entre sessões do mesmo dia: mediana 80,8 min, p90 180 min.
**Voltas depois de uma pausa (v3):** em 40,7% dos dias com jogo. Sessões normais por dia com jogo: uma 57,7%, duas 30,3%, três ou mais 12,1%. Voltas consideradas e recusadas: 39.441. Sessão da noite deixada de lado depois de jogar no trabalho: 36 dias.

**Por que as sessões começaram e terminaram** (sessões normais, seeds somadas).

| Início | Parcela | | Fim | Parcela |
|---|---:|---|---|---:|
| Antes do horário, por vontade | 45,6% | | Decidiu parar (saciedade, cansaço, outras opções) | 77,9% |
| Voltou depois de uma pausa | 36,3% | | Hora de dormir | 16,7% |
| No horário de costume | 18,1% | | Fim do tempo livre (obrigação, preparo, compromisso) | 4,7% |
|  |  | | Depois de ficar acordado jogando | 0,5% |
|  |  | | Fim planejado (sem decisão) | 0,2% |

**Sessões mais longas e por quê** (as 8 maiores sessões normais, seeds somadas). Resto: terminar a atividade ou a viagem.

| Perfil | Ocupação | Duração | Ao começar | Continuou | Ficou acordado | Resto | Disposição | Tempo livre do dia |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Devoto | Sem ocupação | 818 min | 367 min | 449 min | 0 min | 2 min | +8% | 15,8 h |
| Devoto | Sem ocupação | 799 min | 315 min | 477 min | 0 min | 7 min | +9% | 15,4 h |
| Devoto | Estudante (manhã) | 784 min | 404 min | 380 min | 0 min | 0 min | +6% | 15,4 h |
| Devoto | Sem ocupação | 768 min | 408 min | 360 min | 0 min | 0 min | +6% | 16,0 h |
| Devoto | Sem ocupação | 757 min | 337 min | 420 min | 0 min | 0 min | +2% | 15,4 h |
| Hardcore | Sem ocupação | 756 min | 199 min | 377 min | 180 min | 0 min | +2% | 15,6 h |
| Devoto | Universitário (diurno) | 753 min | 393 min | 360 min | 0 min | 0 min | +2% | 15,4 h |
| Devoto | Sem ocupação | 749 min | 449 min | 300 min | 0 min | 0 min | +0% | 15,3 h |


**Quanto do tempo livre vira jogo** (minutos jogados ÷ minutos livres da agenda; o jogo no trabalho e as extensões entram no jogado).

| Perfil | Uso |
|---|---:|
| Casual | 10,2% |
| Comerciante | 21,0% |
| Competitivo | 31,2% |
| Devoto | 64,1% |
| Explorador | 24,7% |
| Hardcore | 52,8% |
| Social | 18,8% |

| Ocupação | Uso |
|---|---:|
| Aposentado | 18,9% |
| Autônomo / freelancer | 22,2% |
| Trabalho em horário comercial | 22,4% |
| Cuidando da casa e da família | 25,3% |
| Comércio e serviços em escala 5x2 | 22,8% |
| Comércio em escala 6x1 | 23,0% |
| Estudante (manhã) | 24,4% |
| Estudante (tarde) | 23,4% |
| Home office (tecnologia) | 21,6% |
| Meio período (manhã) | 22,8% |
| Meio período (tarde) | 21,7% |
| Sem ocupação | 23,4% |
| Trabalha de dia e estuda à noite | 16,7% |
| Turno noturno | 22,4% |
| Universitário (diurno) | 23,6% |

**Composição do tempo jogado:** 939.276 h no total; extensões 0,2%; pausa e escapada do trabalho 0,5%; sono cortado pelo jogo 2.311 h (0,2% do tempo jogado).

**Dias sem sessão principal** (43,3% dos dias da agenda):

| Motivo | Parcela dos dias sem sessão |
|---|---:|
| Sem oportunidade (nenhuma janela comporta uma sessão) | 6,9% |
| Escolha: tinha tempo, preferiu outro lazer | 88,4% |
| Cansaço | 0,8% |
| Desânimo (engajamento baixo) | 0,0% |
| Inativo | 2,2% |
| Jogou só no horário de trabalho | 1,6% |

**Efeito do tempo livre, separado** (dias da agenda por faixa de tempo livre): a frequência (chance de jogar) e a duração (minutos planejados quando joga) medidas à parte.

| Tempo livre no dia | Dias | Jogou | Duração planejada quando joga | Jogado por dia |
|---|---:|---:|---:|---:|
| até 2 h | 4.484 | 0,0% | 91,5 min | 0,0 min |
| 2–4 h | 19.941 | 43,7% | 128 min | 64,8 min |
| 4–6 h | 23.995 | 44,5% | 132 min | 75,4 min |
| 6–10 h | 43.086 | 56,3% | 126 min | 130 min |
| 10 h ou mais | 58.494 | 70,9% | 132 min | 179 min |

**Corte de sono (observações do criador, R2/R3).**

- **Onde pode cortar:** só a janela que termina **por causa do sono** avança sobre ele. Janela que termina por obrigação, preparo, deslocamento ou compromisso não avança (teste).
- **Teto compartilhado:** os 180 minutos por período de sono valem para a soma de todas as sessões que afetam aquele período (teste).
- **Medido:** 1.975 períodos de sono encurtados pelo jogo; corte por período: mediana 62,0 min, p90 125 min, máximo 180 min.

## Localidades (média das seeds)

Cada pessoa joga no horário e nos feriados de onde mora; o relógio do jogo é o do servidor. O início médio é uma média circular das horas (a meia-noite não distorce), então local e servidor diferem exatamente pelo fuso.

| Localidade | Fuso | Pessoas | Estrangeiros | Sessões | Início médio (local) | Início médio (servidor) |
|---|---|---:|---:|---:|---:|---:|
| Argentina | UTC−3 | 85,4 | 2,2 | 6.712 | 16h24 | 16h24 |
| Brasil | UTC−3 | 758 | 19,0 | 58.292 | 16h31 | 16h31 |
| China | UTC+8 | 75,4 | 0,8 | 5.728 | 16h37 | 05h37 |
| Estados Unidos | UTC−5 | 81,4 | 12,6 | 5.845 | 16h41 | 18h41 |

## Retenção (média das seeds, no fim)

| Perfil | Desistiram | Inativos | Voltaram alguma vez |
|---|---:|---:|---:|
| Casual | 1,6% | 2,0% | 4,9% |
| Comerciante | 0,2% | 0,7% | 0,8% |
| Competitivo | 0,3% | 0,6% | 0,8% |
| Devoto | 0,0% | 0,0% | 0,0% |
| Explorador | 0,2% | 1,2% | 1,1% |
| Hardcore | 0,0% | 0,0% | 0,4% |
| Social | 0,6% | 0,8% | 1,2% |

Desistências: dia mediano 64,0, nível mediano 7,0. Sessões estendidas por estarem indo bem: 0,6%; encerradas por frustração: 0,0%.

Motivo principal das desistências (o que mais incomodava; "long_inactivity/" = sumiu por muito tempo):

| Motivo | Desistências |
|---|---:|
| long_inactivity/long_inactivity | 84,2% |
| long_inactivity/boredom | 15,8% |

## Humor e memória (média das seeds, no fim)

Satisfação, frustração, novidade e engajamento em permille (0–1000); progresso em permille do XP de referência do nível.

| Perfil | Satisfação | Insatisfeitos (< 400) | Frustração | Novidade | Engajamento | Intenção de continuar | Expectativa | Progresso da última sessão | Memórias |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 609 | 0,0% | 4,6 | 991 | 724 | 655 | 620 | 593 | 10,8 |
| Comerciante | 562 | 0,0% | 9,3 | 939 | 686 | 597 | 455 | 145 | 10,1 |
| Competitivo | 580 | 0,0% | 9,0 | 900 | 694 | 625 | 720 | 519 | 10,2 |
| Devoto | 506 | 0,0% | 0,0 | 97,7 | 563 | 616 | 867 | 504 | 2,9 |
| Explorador | 655 | 0,0% | 2,6 | 946 | 752 | 705 | 601 | 542 | 10,3 |
| Hardcore | 566 | 0,0% | 0,2 | 584 | 654 | 655 | 871 | 579 | 7,1 |
| Social | 604 | 0,0% | 7,7 | 896 | 711 | 690 | 455 | 52,2 | 8,6 |

Sessões ótimas: 9,9% · ruins: 0,0% (após a semana de lançamento).

O que mais incomoda cada pessoa (motivo mais negativo acumulado):

| Motivo | Pessoas |
|---|---:|
| boredom | 98,2% |
| nenhum | 0,9% |
| fatigue | 0,8% |
| progress | 0,1% |

## Uso do tempo conectado (todas as seeds)

Caça, chefe, patrulha e emboscada ainda são atividades abstratas (tempo e XP, sem luta simulada) até o M3; aí o tempo se divide em viagem, procura, luta, recuperação e recompensa.

| Tipo | Tempo | Escolhas |
|---|---:|---:|
| Caça | 32,2% | 32,5% |
| Deslocamento | 21,4% | — |
| Socializar | 14,7% | 14,3% |
| Exploração | 6,9% | 6,9% |
| Comércio | 6,6% | 12,7% |
| Patrulha | 5,4% | 5,3% |
| Coleta | 5,2% | 7,6% |
| Emboscada | 4,1% | 6,0% |
| Missão | 2,2% | 10,7% |
| Treino | 1,1% | 3,6% |
| Conserto | 0,0% | 0,4% |

## Continuidade das sessões (média das seeds)

| Medida | Valor |
|---|---:|
| Escolhas por sessão | 5,2 |
| Continuou na mesma atividade (% das escolhas após a primeira) | 74,8% |
| Trocas de atividade por hora conectada | 0,4 |
| Trocas de zona por sessão | 0,5 |
| Ciclos seguidos na mesma atividade (média) | 2,5 |

Motivos das trocas de plano (fator principal):

| Motivo | Trocas |
|---|---:|
| quest_step | 27,0% |
| quest_completed | 22,0% |
| time_budget | 10,5% |
| focus_unavailable | 9,8% |
| boredom | 9,6% |
| level_up | 8,4% |
| goal_completed | 5,7% |
| affinity | 2,9% |
| zone_discovered | 1,2% |
| quest_deliver | 0,8% |
| nothing_new | 0,6% |
| goal | 0,6% |
| quest_offer | 0,6% |
| level_fit | 0,2% |
| crowding | 0,1% |
| company | 0,0% |
| challenge | 0,0% |
| risk | 0,0% |
| familiarity | 0,0% |
| memory | 0,0% |
| interruption | 0,0% |

## Atividades pouco usadas (menos de 1% das escolhas)

| Zona | Atividade | Tipo | Escolhas | Diagnóstico |
|---|---|---|---:|---|
| Cidadela de Cinzaforte | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Covil do Rei Goblin | Enfrentar o Rei Goblin | `boss` | 0,0% | exige grupo de 5; grupos chegam no M6, então hoje ninguém pode fazer. |
| Fortaleza Sombria | Enfrentar o Senhor das Cinzas | `boss` | 0,0% | exige grupo de 10; grupos chegam no M6, então hoje ninguém pode fazer; nível mínimo 28: só 33% dos personagens chegaram até o fim. |
| Forte do Vau | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Observar a forja vulcânica | `quest` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Ruínas Sombrias | Coletar cristais sombrios | `gather` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Consertar equipamento | `repair` | 0,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Explorar trilhas da floresta | `explore` | 0,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Consertar equipamento | `repair` | 0,1% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Fortaleza Sombria | Ler as inscrições da cripta | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Investigar o covil do yeti | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Explorar as terras queimadas | `explore` | 0,1% | nível mínimo 22: só 49% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Patrulhar as ruínas | `patrol` | 0,1% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Planalto dos Ventos | Espiar o acampamento orc | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Caçar mortos-vivos | `hunt` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Explorar galerias profundas | `explore` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar aranhas tecelãs | `hunt` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Minerar obsidiana | `gather` | 0,2% | nível mínimo 23: só 46% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Examinar o altar profanado | `quest` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Cidadela de Cinzaforte | Consertar equipamento | `repair` | 0,3% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Mata dos Espinhos | Seguir os rastros dos trolls | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Procurar o estandarte perdido | `quest` | 0,3% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Brejo das Brumas | Procurar o ninho das serpentes | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Investigar as galerias abandonadas | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Examinar os ninhos das harpias | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Falar com o lenhador | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Colher ervas das encostas | `gather` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Caçar lobos das colinas | `hunt` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Coletar cristais de gelo | `gather` | 0,6% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Socializar na taverna | `socialize` | 0,6% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Ravina Escarlate | Patrulhar a ravina | `patrol` | 0,6% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ravina Escarlate | Emboscar viajantes | `ambush` | 0,7% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Coletar ervas curativas | `gather` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Minerar ferro e prata | `gather` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar javalis | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Caçar esqueletos da ravina | `hunt` | 0,8% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Fortaleza Sombria | Explorar a fortaleza | `explore` | 0,8% | nível mínimo 25: só 39% dos personagens chegaram até o fim. |
| Mina Velha | Caçar morcegos gigantes | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar lobos | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Treinar com o mestre de armas | `train` | 0,9% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Colinas do Vale | Caçar ursos pardos | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Explorar as colinas | `explore` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Brejo das Brumas | Explorar o brejo | `explore` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Caçar goblins | `hunt` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Caçar salamandras de fogo | `hunt` | 1,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Cidadela de Cinzaforte | Treinar com o mestre de armas | `train` | 1,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |

## Execuções

| Seed | Hash final | Simulação |
|---:|---|---:|
| 1 | `59fce2d3c2bf9db7` | 293,4 s |
| 2 | `74c73eddac362c8f` | 290,6 s |
| 3 | `3dfe73df6418a020` | 299,0 s |
| 4 | `fc0eb9573555a2e4` | 297,1 s |
| 5 | `9cd005f7e889c027` | 281,4 s |

Medições de tempo desta máquina, não benchmark.
