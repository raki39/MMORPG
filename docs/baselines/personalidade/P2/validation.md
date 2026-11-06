# Validação da simulação

> Gerado pelo `Simulation.Runner` com várias seeds. As faixas vêm de `targets.json` e são **hipóteses de design ajustáveis**, não regras do mundo. Uma meta só conta como "dentro" quando todas as seeds caem na faixa.

| Campo | Valor |
|---|---|
| Conteúdo | `reports/p4a/mundos/P2` (`85241d1c5d244194`) |
| Metas | `reports/p4a/mundos/P2/targets.json` (`d8f348f9966ab923`) |
| Simulador | 0.1.0+6adfb4bb6dc2412137c388d3f34d61d2aea44287 |
| Execuções | 5 seeds (1, 2, 3, 4, 5) · 1.000 pessoas · 90 dias |
| Janela das medidas semanais | a partir do dia 7 (exclui a semana de lançamento) |
| Tempo total | 313,6 s |

## Metas

As metas têm três classes (D-039a); o total "x de y" sozinho não diz se dá para avançar:

- **Regras do cenário:** rotina calibrada, medida numa rodada controlada com o humor neutralizado. Falhar aqui indica erro.
- **Provisórias:** balanceamento que será recalibrado (progressão, divisão do tempo, missões).
- **Dependem de sistemas futuros:** só acompanhamento, não reprovam (retenção, missões e coleta dependem de combate, economia e social).

### Regras do cenário: 7 de 9 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Dias de jogo por semana — casual | 2,0–4,0  | 3,1 | 3,0–3,2 |  |
| ✅ dentro | Dias de jogo por semana — hardcore | 5,0–7,0  | 6,1 | 5,9–6,2 |  |
| ✅ dentro | Sessões por dia de jogo — casual | 1,1–2,0  | 1,5 | 1,5–1,5 |  |
| ✅ dentro | Sessões por dia de jogo — hardcore | 1,2–2,5  | 1,5 | 1,5–1,5 |  |
| ❌ acima | Horas por semana — casual | 3,0–9,0 h | 9,8 | 9,4–10,3 |  |
| ✅ dentro | Horas por semana — hardcore | 15,0–45,0 h | 36,7 | 35,1–37,7 |  |
| ✅ dentro | Jogadores ativos em dia útil / cadastrados | 30,0–55,0 % | 53,2 | 52,5–53,6 | 58 dias |
| ⚠️ instável | Ativos no fim de semana ÷ ativos em dia útil | 1,3–2,0 × | 1,3 | 1,3–1,3 |  |
| ✅ dentro | Pico online do dia / cadastrados | 15,0–35,0 % | 28,3 | 27,7–29,6 |  |

### Provisórias: 8 de 12 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Horas conectadas até o nível 5 (mediana) | 1,0–4,0 h | 2,6 | 2,6–2,6 | 100% chegaram |
| ✅ dentro | Horas conectadas até o nível 10 (mediana) | 8,0–20,0 h | 15,1 | 14,9–15,1 | 98–99% chegaram |
| ✅ dentro | Horas conectadas até o nível 15 (mediana) | 22,0–50,0 h | 39,2 | 38,4–40,0 | 88–89% chegaram |
| ❌ abaixo | Horas conectadas até o nível 20 (mediana) | 80,0–110 h | 77,9 | 75,7–80,1 | 74–77% chegaram |
| ✅ dentro | Horas conectadas até o nível 30 (mediana) | 170–230 h | 198 | 193–204 | 33–35% chegaram (mediana só de quem chegou: subestima o tempo real) |
| ❌ acima | Casuais no nível 10 ou mais ao fim | 60,0–95,0 % | 96,2 | 95,0–97,8 |  |
| ✅ dentro | XP vindo de quests | 15,0–35,0 % | 17,7 | 17,4–18,0 |  |
| ✅ dentro | Quests concluídas por personagem (média) | 4,0–30,0  | 26,3 | 26,0–26,7 |  |
| ❌ abaixo | Tempo em atividades de combate (abstratas: caça, chefe, patrulha, emboscada; sem luta simulada até o M3) | 35,0–55,0 % | 33,6 | 32,4–35,1 |  |
| ✅ dentro | Tempo explorando | 5,0–15,0 % | 9,0 | 8,1–9,6 |  |
| ✅ dentro | Tempo na cidade (comércio, taverna, treino, conserto) | 10,0–25,0 % | 23,4 | 22,4–24,5 |  |
| ❌ acima | Tempo andando ou esperando | 5,0–20,0 % | 20,2 | 20,0–20,4 |  |

### Dependem de sistemas futuros (acompanhamento): 2 de 5 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ❌ abaixo | Tempo em missões | 10,0–25,0 % | 1,8 | 1,8–1,8 |  |
| ✅ dentro | Tempo em coleta | 8,0–20,0 % | 12,0 | 11,4–12,9 |  |
| ❌ abaixo | Desistentes no fim / cadastrados | 8,0–30,0 % | 0,8 | 0,5–1,0 |  |
| ❌ abaixo | Inativos no fim / cadastrados | 2,0–15,0 % | 1,6 | 1,1–1,9 |  |
| ✅ dentro | Quem saiu e voltou (inativos e desistentes) | 20,0–70,0 % | 54,3 | 45,7–61,7 | 46 saíram; 51 saíram; 47 saíram; 48 saíram |


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
| Casual | 3,1 | 1,51 | 4,6 | 9,8 |
| Devoto | 7,0 | 1,51 | 10,5 | 57,6 |
| Hardcore | 6,1 | 1,50 | 9,1 | 36,7 |
| Regular | 4,1 | 1,55 | 6,4 | 17,5 |

## Perfis na campanha completa (média das seeds)

| Perfil | Pessoas | Dias de jogo/semana | Sessões por dia de jogo | Horas/semana | Nível mediano no fim | Nível p90 | Sessões em dia útil iniciadas entre 18h e 23h |
|---|---:|---:|---:|---:|---:|---:|---:|
| Casual | 353 | 3,2 | 1,52 | 10,3 | 21,0 | 30,0 | 50,7% |
| Devoto | 19,4 | 6,8 | 1,51 | 56,1 | 30,0 | 30,0 | 38,9% |
| Hardcore | 101 | 6,1 | 1,50 | 36,7 | 30,0 | 30,0 | 50,1% |
| Regular | 527 | 4,2 | 1,56 | 18,2 | 27,0 | 30,0 | 50,7% |

## Evolução dos atributos do jogador (média das seeds)

Variação média desde o início, em **pontos da escala 1–20** (1 ponto ≈ 52,6 unidades do Core), ao fim de 90 dias. Valores de `population.json` (`dynamics`).

| Perfil | Mecânica | Conhecimento | PvE | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | +0,20 | +1,10 | +0,20 | +0,11 | +0,01 | -0,02 | 15,4 | 3,1 |
| Devoto | +0,86 | +2,15 | +0,45 | +0,18 | +0,05 | -0,14 | 41,8 | 6,6 |
| Hardcore | +0,70 | +1,70 | +0,42 | +0,17 | +0,03 | -0,10 | 32,6 | 6,6 |
| Regular | +0,40 | +1,36 | +0,29 | +0,15 | +0,02 | -0,05 | 21,9 | 4,9 |

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
| Aposentado | 20,2 | 8,8 | 26,0 | — | 15,3 h | 0,0% | R$ 3.426 | R$ 36,9 |
| Autônomo / freelancer | 42,4 | 8,2 | 19,9 | 8,9 h | 15,2 h | 0,0% | R$ 4.269 | R$ 55,1 |
| Trabalho em horário comercial | 229 | 5,2 | 15,0 | 4,2 h | 14,8 h | 0,0% | R$ 4.421 | R$ 29,8 |
| Cuidando da casa e da família | 30,0 | 7,6 | 16,7 | 8,4 h | — | 0,0% | R$ 0,0 | R$ 2.322 |
| Comércio e serviços em escala 5x2 | 82,4 | 4,7 | 12,8 | 3,3 h | 14,9 h | 0,0% | R$ 2.604 | R$ 31,8 |
| Comércio em escala 6x1 | 37,0 | 4,6 | 12,0 | 4,8 h | 15,0 h | 0,0% | R$ 2.719 | R$ 28,1 |
| Estudante (manhã) | 106 | 7,2 | 21,2 | 8,4 h | 14,7 h | 0,0% | R$ 0,0 | R$ 739 |
| Estudante (tarde) | 45,4 | 7,6 | 21,4 | 8,7 h | 15,2 h | 0,0% | R$ 0,0 | R$ 729 |
| Home office (tecnologia) | 80,4 | 7,1 | 18,0 | 6,4 h | 15,1 h | 0,0% | R$ 8.226 | R$ 17,5 |
| Meio período (manhã) | 30,0 | 7,4 | 21,6 | 9,5 h | 14,8 h | 0,0% | R$ 1.971 | R$ 294 |
| Meio período (tarde) | 25,2 | 7,5 | 19,7 | 9,3 h | 15,1 h | 0,0% | R$ 1.914 | R$ 286 |
| Sem ocupação | 82,0 | 9,3 | 30,0 | — | 15,3 h | 0,0% | R$ 0,0 | R$ 811 |
| Trabalha de dia e estuda à noite | 55,8 | 2,4 | 7,2 | 1,0 h | 12,9 h | 53,0% | R$ 3.384 | R$ 53,2 |
| Turno noturno | 31,6 | 5,4 | 15,2 | 7,1 h | 11,5 h | 0,0% | R$ 3.614 | R$ 19,8 |
| Universitário (diurno) | 103 | 6,5 | 20,1 | 7,1 h | 14,8 h | 0,0% | R$ 527 | R$ 1.374 |

Como terminaram os dias da agenda:

| Dia | Parcela |
|---|---:|
| Jogou | 56,9% |
| Tinha tempo, preferiu outro lazer | 38,5% |
| Sem tempo livre | 3,0% |
| Inativo | 1,2% |
| Cansado (dormiu pouco) | 0,4% |
| Sem vontade (engajamento baixo) | 0,1% |

## Sono e dificuldade de parar (média das seeds, campanha completa)

Importância do jogo, tendência a prolongar e autocontrole são traços separados. A extensão tem limites explícitos (vezes por sessão, minutos, sono mínimo e teto do déficit); só corta sono no fim do dia, nunca uma obrigação. Sono cortado vira déficit; manhã livre e noites normais recuperam.

- Extensões na janela: 405 (129 gostando da sessão, 277 perto de concluir a meta); 405 avançaram sobre a hora de dormir.
- Noites encurtadas pelo jogo: 369, 63,4 min a menos em média.

| Perfil | Extensões/semana | Sono cortado/semana | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|
| Casual | 0,0 | 0,6 min | 0,2 h | 2,0% |
| Devoto | 0,1 | 7,7 min | 0,0 h | 0,0% |
| Hardcore | 0,1 | 6,5 min | 0,4 h | 2,8% |
| Regular | 0,0 | 1,8 min | 0,3 h | 2,1% |

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
| Meio período (tarde) | 7,9 h | 7,9 h | 7,9 h | 0,0 h | 0,0% |
| Sem ocupação | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Trabalha de dia e estuda à noite | 7,9 h | 6,9 h | 9,7 h | 4,6 h | 38,4% |
| Turno noturno | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Universitário (diurno) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |

## Jogar no horário de trabalho (média das seeds)

Só no trabalho remoto. Pausa: jogar no intervalo permitido. Escapada: jogar no expediente, rara, pela flexibilidade do trabalho, importância do jogo, falta de autocontrole e folga do dia; sessão curta e interrompível. Deslocamento, trabalho presencial e estudo ficam protegidos. Sem consequência profissional por enquanto.

- Sessões na pausa: 1.128 (1,5% das sessões); escapadas: 220 (0,3%).
- Chamados do trabalho durante a escapada: 76,6 (8,2 terminaram a atividade, 68,4 largaram e saíram).

| Ocupação | Pausa/semana | Escapadas/semana | Dias de trabalho com escapada |
|---|---:|---:|---:|
| Autônomo / freelancer | 0,8 | 0,2 | 2,8% |
| Home office (tecnologia) | 0,8 | 0,1 | 3,0% |

## Distribuições (R3)

Medição da campanha completa (com humor), seeds somadas, na janela de medição. A referência antes da R4 (modelo de vontade) está em `docs/baselines/pre-R4/`, com o mesmo relatório e o cenário controlado (pessoas iguais por célula perfil × ocupação), que separa o efeito das regras da mistura da população.

**Duração das sessões (minutos, sessões normais).** Inclui extensões e o fim real; pausa e escapada do trabalho à parte.

| Perfil | Sessões | p10 | p25 | Mediana | p75 | p90 | Máx. | Média |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 100.142 | 64,0 | 87,0 | 122 | 164 | 201 | 519 | 128 |
| Devoto | 11.728 | 122 | 229 | 327 | 432 | 511 | 894 | 327 |
| Hardcore | 53.171 | 115 | 190 | 246 | 306 | 369 | 806 | 246 |
| Regular | 202.620 | 84,0 | 118 | 161 | 211 | 258 | 640 | 167 |
| Pausa no trabalho | 5.641 | 38,0 | 52,0 | 55,0 | 55,0 | 55,0 | 55,0 | 50,9 |
| Escapada no expediente | 1.100 | 9,0 | 18,0 | 23,0 | 26,0 | 29,0 | 34,0 | 21,3 |

**Horas por semana, perfil × ocupação** (por pessoa registrada; "—" com menos de 10 pessoas na soma das seeds).

| Ocupação | Casual | Devoto | Hardcore | Regular |
|---|---:|---:|---:|---:|
| Aposentado | 17,3 | — | 54,6 | 27,3 |
| Autônomo / freelancer | 11,8 | — | 43,3 | 22,1 |
| Trabalho em horário comercial | 8,5 | — | 32,2 | 15,5 |
| Cuidando da casa e da família | 9,1 | — | 36,4 | 17,1 |
| Comércio e serviços em escala 5x2 | 7,4 | — | 30,0 | 13,8 |
| Comércio em escala 6x1 | 8,1 | — | 27,5 | 12,1 |
| Estudante (manhã) | 12,4 | 53,5 | 43,6 | 21,9 |
| Estudante (tarde) | 11,5 | 48,8 | 37,3 | 20,9 |
| Home office (tecnologia) | 9,8 | — | 38,5 | 18,6 |
| Meio período (manhã) | 11,7 | — | 47,5 | 23,5 |
| Meio período (tarde) | 11,4 | — | 44,0 | 21,2 |
| Sem ocupação | 17,6 | 63,0 | 51,7 | 27,2 |
| Trabalha de dia e estuda à noite | 4,4 | — | 14,3 | 7,5 |
| Turno noturno | 9,7 | — | 35,8 | 15,2 |
| Universitário (diurno) | 10,8 | 49,2 | 39,6 | 19,4 |

**Sessões por dia de jogo:** 58,7% com uma, 30,6% com duas, 10,7% com três ou mais. Intervalo entre sessões do mesmo dia: mediana 81,4 min, p90 193 min.
**Voltas depois de uma pausa (v3):** em 39,1% dos dias com jogo. Sessões normais por dia com jogo: uma 59,4%, duas 30,4%, três ou mais 10,3%. Voltas consideradas e recusadas: 35.333. Sessão da noite deixada de lado depois de jogar no trabalho: 42 dias.

**Por que as sessões começaram e terminaram** (sessões normais, seeds somadas).

| Início | Parcela | | Fim | Parcela |
|---|---:|---|---|---:|
| Antes do horário, por vontade | 45,9% | | Decidiu parar (saciedade, cansaço, outras opções) | 73,3% |
| Voltou depois de uma pausa | 34,5% | | Hora de dormir | 20,8% |
| No horário de costume | 19,6% | | Fim do tempo livre (obrigação, preparo, compromisso) | 5,2% |
|  |  | | Depois de ficar acordado jogando | 0,5% |
|  |  | | Fim planejado (sem decisão) | 0,2% |
|  |  | | Frustração | 0,0% |

**Sessões mais longas e por quê** (as 8 maiores sessões normais, seeds somadas). Resto: terminar a atividade ou a viagem.

| Perfil | Ocupação | Duração | Ao começar | Continuou | Ficou acordado | Resto | Disposição | Tempo livre do dia |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Devoto | Sem ocupação | 894 min | 327 min | 387 min | 180 min | 0 min | +0% | 15,1 h |
| Devoto | Sem ocupação | 828 min | 430 min | 390 min | 0 min | 8 min | −1% | 15,3 h |
| Hardcore | Trabalho em horário comercial | 806 min | 286 min | 520 min | 0 min | 0 min | +14% | 15,3 h |
| Hardcore | Turno noturno | 803 min | 378 min | 420 min | 0 min | 5 min | +5% | 14,1 h |
| Devoto | Sem ocupação | 796 min | 430 min | 360 min | 0 min | 6 min | +5% | 15,3 h |
| Devoto | Universitário (diurno) | 793 min | 574 min | 210 min | 0 min | 9 min | +4% | 15,0 h |
| Devoto | Universitário (diurno) | 789 min | 549 min | 60 min | 180 min | 0 min | −7% | 10,2 h |
| Devoto | Sem ocupação | 783 min | 430 min | 349 min | 0 min | 4 min | +9% | 15,3 h |


**Quanto do tempo livre vira jogo** (minutos jogados ÷ minutos livres da agenda; o jogo no trabalho e as extensões entram no jogado).

| Perfil | Uso |
|---|---:|
| Casual | 14,8% |
| Devoto | 61,6% |
| Hardcore | 53,3% |
| Regular | 26,1% |

| Ocupação | Uso |
|---|---:|
| Aposentado | 22,6% |
| Autônomo / freelancer | 24,6% |
| Trabalho em horário comercial | 26,0% |
| Cuidando da casa e da família | 26,6% |
| Comércio e serviços em escala 5x2 | 25,0% |
| Comércio em escala 6x1 | 25,7% |
| Estudante (manhã) | 26,8% |
| Estudante (tarde) | 26,2% |
| Home office (tecnologia) | 25,9% |
| Meio período (manhã) | 25,7% |
| Meio período (tarde) | 23,9% |
| Sem ocupação | 26,0% |
| Trabalha de dia e estuda à noite | 18,7% |
| Turno noturno | 23,5% |
| Universitário (diurno) | 28,2% |

**Composição do tempo jogado:** 1.066.303 h no total; extensões 0,2%; pausa e escapada do trabalho 0,5%; sono cortado pelo jogo 1.948 h (0,2% do tempo jogado).

**Dias sem sessão principal** (43,1% dos dias da agenda):

| Motivo | Parcela dos dias sem sessão |
|---|---:|
| Sem oportunidade (nenhuma janela comporta uma sessão) | 6,9% |
| Escolha: tinha tempo, preferiu outro lazer | 87,8% |
| Cansaço | 0,9% |
| Desânimo (engajamento baixo) | 0,2% |
| Inativo | 2,7% |
| Jogou só no horário de trabalho | 1,5% |

**Efeito do tempo livre, separado** (dias da agenda por faixa de tempo livre): a frequência (chance de jogar) e a duração (minutos planejados quando joga) medidas à parte.

| Tempo livre no dia | Dias | Jogou | Duração planejada quando joga | Jogado por dia |
|---|---:|---:|---:|---:|
| até 2 h | 4.438 | 0,0% | 64,0 min | 0,0 min |
| 2–4 h | 20.020 | 44,3% | 152 min | 74,8 min |
| 4–6 h | 24.092 | 45,0% | 160 min | 85,9 min |
| 6–10 h | 42.774 | 56,7% | 152 min | 146 min |
| 10 h ou mais | 58.676 | 70,5% | 163 min | 203 min |

**Corte de sono (observações do criador, R2/R3).**

- **Onde pode cortar:** só a janela que termina **por causa do sono** avança sobre ele. Janela que termina por obrigação, preparo, deslocamento ou compromisso não avança (teste).
- **Teto compartilhado:** os 180 minutos por período de sono valem para a soma de todas as sessões que afetam aquele período (teste).
- **Medido:** 1.843 períodos de sono encurtados pelo jogo; corte por período: mediana 58,0 min, p90 109 min, máximo 180 min.

## Localidades (média das seeds)

Cada pessoa joga no horário e nos feriados de onde mora; o relógio do jogo é o do servidor. O início médio é uma média circular das horas (a meia-noite não distorce), então local e servidor diferem exatamente pelo fuso.

| Localidade | Fuso | Pessoas | Estrangeiros | Sessões | Início médio (local) | Início médio (servidor) |
|---|---|---:|---:|---:|---:|---:|
| Argentina | UTC−3 | 85,6 | 2,2 | 6.446 | 16h24 | 16h24 |
| Brasil | UTC−3 | 758 | 19,0 | 57.008 | 16h26 | 16h26 |
| China | UTC+8 | 75,4 | 0,8 | 5.585 | 16h29 | 05h29 |
| Estados Unidos | UTC−5 | 81,4 | 12,6 | 5.902 | 16h32 | 18h32 |

## Retenção (média das seeds, no fim)

| Perfil | Desistiram | Inativos | Voltaram alguma vez |
|---|---:|---:|---:|
| Casual | 1,5% | 2,2% | 4,8% |
| Devoto | 1,8% | 0,0% | 0,0% |
| Hardcore | 0,2% | 0,6% | 0,8% |
| Regular | 0,4% | 1,4% | 1,7% |

Desistências: dia mediano 68,0, nível mediano 8,0. Sessões estendidas por estarem indo bem: 0,5%; encerradas por frustração: 0,0%.

Motivo principal das desistências (o que mais incomodava; "long_inactivity/" = sumiu por muito tempo):

| Motivo | Desistências |
|---|---:|
| long_inactivity/long_inactivity | 80,5% |
| long_inactivity/boredom | 9,8% |
| boredom | 7,3% |
| long_inactivity/fatigue | 2,4% |

## Humor e memória (média das seeds, no fim)

Satisfação, frustração, novidade e engajamento em permille (0–1000); progresso em permille do XP de referência do nível.

| Perfil | Satisfação | Insatisfeitos (< 400) | Frustração | Novidade | Engajamento | Intenção de continuar | Expectativa | Progresso da última sessão | Memórias |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 614 | 0,0% | 2,8 | 969 | 726 | 672 | 582 | 475 | 9,4 |
| Devoto | 483 | 8,7% | 6,6 | 302 | 567 | 541 | 570 | 348 | 6,1 |
| Hardcore | 542 | 1,4% | 5,9 | 661 | 644 | 602 | 594 | 387 | 8,5 |
| Regular | 587 | 0,2% | 5,1 | 913 | 701 | 647 | 603 | 450 | 9,8 |

Sessões ótimas: 9,9% · ruins: 0,0% (após a semana de lançamento).

O que mais incomoda cada pessoa (motivo mais negativo acumulado):

| Motivo | Pessoas |
|---|---:|
| boredom | 96,0% |
| progress | 2,2% |
| fatigue | 0,8% |
| nenhum | 0,8% |
| fun | 0,1% |
| fit | 0,0% |

## Uso do tempo conectado (todas as seeds)

Caça, chefe, patrulha e emboscada ainda são atividades abstratas (tempo e XP, sem luta simulada) até o M3; aí o tempo se divide em viagem, procura, luta, recuperação e recompensa.

| Tipo | Tempo | Escolhas |
|---|---:|---:|
| Caça | 28,6% | 29,4% |
| Deslocamento | 20,2% | — |
| Socializar | 16,7% | 16,1% |
| Coleta | 12,0% | 17,1% |
| Exploração | 9,0% | 8,9% |
| Comércio | 5,6% | 10,8% |
| Patrulha | 3,9% | 3,9% |
| Missão | 1,8% | 8,4% |
| Emboscada | 1,2% | 1,7% |
| Treino | 1,0% | 3,3% |
| Conserto | 0,1% | 0,4% |

## Continuidade das sessões (média das seeds)

| Medida | Valor |
|---|---:|
| Escolhas por sessão | 6,0 |
| Continuou na mesma atividade (% das escolhas após a primeira) | 76,1% |
| Trocas de atividade por hora conectada | 0,4 |
| Trocas de zona por sessão | 0,5 |
| Ciclos seguidos na mesma atividade (média) | 2,7 |

Motivos das trocas de plano (fator principal):

| Motivo | Trocas |
|---|---:|
| quest_step | 22,4% |
| quest_completed | 17,4% |
| time_budget | 10,6% |
| boredom | 10,4% |
| level_up | 8,1% |
| focus_unavailable | 7,3% |
| personality | 5,9% |
| goal_completed | 5,3% |
| affinity | 4,1% |
| nothing_new | 2,6% |
| goal | 1,2% |
| zone_discovered | 1,1% |
| quest_deliver | 1,0% |
| quest_offer | 0,8% |
| company | 0,6% |
| level_fit | 0,4% |
| crowding | 0,4% |
| risk | 0,1% |
| memory | 0,0% |
| noise | 0,0% |
| challenge | 0,0% |
| familiarity | 0,0% |
| interruption | 0,0% |
| early_progression | 0,0% |

## Atividades pouco usadas (menos de 1% das escolhas)

| Zona | Atividade | Tipo | Escolhas | Diagnóstico |
|---|---|---|---:|---|
| Cidadela de Cinzaforte | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Covil do Rei Goblin | Enfrentar o Rei Goblin | `boss` | 0,0% | exige grupo de 5; grupos chegam no M6, então hoje ninguém pode fazer. |
| Fortaleza Sombria | Enfrentar o Senhor das Cinzas | `boss` | 0,0% | exige grupo de 10; grupos chegam no M6, então hoje ninguém pode fazer; nível mínimo 28: só 43% dos personagens chegaram até o fim. |
| Forte do Vau | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Observar a forja vulcânica | `quest` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Vila Aurora | Consertar equipamento | `repair` | 0,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Explorar trilhas da floresta | `explore` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Consertar equipamento | `repair` | 0,1% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Fortaleza Sombria | Ler as inscrições da cripta | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Investigar o covil do yeti | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Planalto dos Ventos | Espiar o acampamento orc | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Explorar as terras queimadas | `explore` | 0,1% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Caçar mortos-vivos | `hunt` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Examinar o altar profanado | `quest` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Coletar cristais sombrios | `gather` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Explorar galerias profundas | `explore` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Patrulhar as ruínas | `patrol` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ravina Escarlate | Emboscar viajantes | `ambush` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mata dos Espinhos | Seguir os rastros dos trolls | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Procurar o estandarte perdido | `quest` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Brejo das Brumas | Procurar o ninho das serpentes | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Investigar as galerias abandonadas | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Examinar os ninhos das harpias | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Falar com o lenhador | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar aranhas tecelãs | `hunt` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Cidadela de Cinzaforte | Consertar equipamento | `repair` | 0,3% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Ravina Escarlate | Patrulhar a ravina | `patrol` | 0,5% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Forte do Vau | Treinar com o mestre de armas | `train` | 0,5% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Caçar salamandras de fogo | `hunt` | 0,6% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Terras Queimadas | Enfrentar golens de magma | `hunt` | 0,6% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ravina Escarlate | Caçar esqueletos da ravina | `hunt` | 0,6% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Caçar lobos das colinas | `hunt` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Caçar morcegos gigantes | `hunt` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Socializar na taverna | `socialize` | 0,8% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Mina Velha | Caçar goblins | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Purgar a cripta | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Explorar a fortaleza | `explore` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Explorar as colinas | `explore` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Caçar ursos pardos | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar javalis | `hunt` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Enfrentar gárgulas | `hunt` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |

## Execuções

| Seed | Hash final | Simulação |
|---:|---|---:|
| 1 | `8655c1feb987d06a` | 228,3 s |
| 2 | `acf2dac22c801cc8` | 228,6 s |
| 3 | `13ca4769708cc12c` | 228,4 s |
| 4 | `b15df913ede786ba` | 221,6 s |
| 5 | `02cc424957034163` | 226,3 s |

Medições de tempo desta máquina, não benchmark.
