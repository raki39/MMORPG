# Validação da simulação

> Gerado pelo `Simulation.Runner` com várias seeds. As faixas vêm de `targets.json` e são **hipóteses de design ajustáveis**, não regras do mundo. Uma meta só conta como "dentro" quando todas as seeds caem na faixa.

| Campo | Valor |
|---|---|
| Conteúdo | `Content/presets/inicial` (`f16808f95448478c`) |
| Metas | `Content/presets/inicial/targets.json` (`d8f348f9966ab923`) |
| Simulador | 0.1.0+49ec1ba56269775ea70415323d5336c4d33a8082 |
| Execuções | 5 seeds (1, 2, 3, 4, 5) · 1.000 pessoas · 90 dias |
| Janela das medidas semanais | a partir do dia 7 (exclui a semana de lançamento) |
| Tempo total | 333,9 s |

## Metas

As metas têm três classes (D-039a); o total "x de y" sozinho não diz se dá para avançar:

- **Regras do cenário:** rotina calibrada, medida numa rodada controlada com o humor neutralizado. Falhar aqui indica erro.
- **Provisórias:** balanceamento que será recalibrado (progressão, divisão do tempo, missões).
- **Dependem de sistemas futuros:** só acompanhamento, não reprovam (retenção, missões e coleta dependem de combate, economia e social).

### Regras do cenário: 8 de 9 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Dias de jogo por semana — casual | 2,0–4,0  | 3,1 | 3,1–3,2 |  |
| ✅ dentro | Dias de jogo por semana — hardcore | 5,0–7,0  | 6,1 | 5,9–6,2 |  |
| ✅ dentro | Sessões por dia de jogo — casual | 1,1–2,0  | 1,5 | 1,5–1,5 |  |
| ✅ dentro | Sessões por dia de jogo — hardcore | 1,2–2,5  | 1,5 | 1,5–1,6 |  |
| ❌ acima | Horas por semana — casual | 3,0–9,0 h | 10,0 | 9,6–10,5 |  |
| ✅ dentro | Horas por semana — hardcore | 15,0–45,0 h | 37,1 | 35,6–38,1 |  |
| ✅ dentro | Jogadores ativos em dia útil / cadastrados | 30,0–55,0 % | 54,1 | 53,5–54,7 | 58 dias |
| ✅ dentro | Ativos no fim de semana ÷ ativos em dia útil | 1,3–2,0 × | 1,3 | 1,3–1,3 |  |
| ✅ dentro | Pico online do dia / cadastrados | 15,0–35,0 % | 28,1 | 27,4–29,5 |  |

### Provisórias: 8 de 12 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Horas conectadas até o nível 5 (mediana) | 1,0–4,0 h | 2,6 | 2,6–2,6 | 100% chegaram |
| ✅ dentro | Horas conectadas até o nível 10 (mediana) | 8,0–20,0 h | 15,1 | 15,1–15,2 | 98–99% chegaram |
| ✅ dentro | Horas conectadas até o nível 15 (mediana) | 22,0–50,0 h | 39,3 | 38,7–40,2 | 90–92% chegaram |
| ❌ abaixo | Horas conectadas até o nível 20 (mediana) | 80,0–110 h | 77,9 | 76,2–79,3 | 77–79% chegaram |
| ✅ dentro | Horas conectadas até o nível 30 (mediana) | 170–230 h | 201 | 198–205 | 35–37% chegaram (mediana só de quem chegou: subestima o tempo real) |
| ❌ acima | Casuais no nível 10 ou mais ao fim | 60,0–95,0 % | 97,1 | 96,2–98,1 |  |
| ✅ dentro | XP vindo de quests | 15,0–35,0 % | 16,4 | 16,1–16,9 |  |
| ✅ dentro | Quests concluídas por personagem (média) | 4,0–30,0  | 25,8 | 25,2–26,4 |  |
| ❌ abaixo | Tempo em atividades de combate (abstratas: caça, chefe, patrulha, emboscada; sem luta simulada até o M3) | 35,0–55,0 % | 34,3 | 33,0–35,7 |  |
| ✅ dentro | Tempo explorando | 5,0–15,0 % | 9,0 | 8,1–9,7 |  |
| ✅ dentro | Tempo na cidade (comércio, taverna, treino, conserto) | 10,0–25,0 % | 22,1 | 21,2–23,6 |  |
| ❌ acima | Tempo andando ou esperando | 5,0–20,0 % | 20,6 | 20,4–20,9 |  |

### Dependem de sistemas futuros (acompanhamento): 2 de 5 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ❌ abaixo | Tempo em missões | 10,0–25,0 % | 1,7 | 1,7–1,8 |  |
| ✅ dentro | Tempo em coleta | 8,0–20,0 % | 12,1 | 11,6–12,9 |  |
| ❌ abaixo | Desistentes no fim / cadastrados | 8,0–30,0 % | 0,8 | 0,6–0,9 |  |
| ❌ abaixo | Inativos no fim / cadastrados | 2,0–15,0 % | 1,5 | 1,0–1,7 |  |
| ✅ dentro | Quem saiu e voltou (inativos e desistentes) | 20,0–70,0 % | 55,6 | 46,8–61,7 | 47 saíram; 50 saíram; 46 saíram; 44 saíram |


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
| Casual | 3,1 | 1,51 | 4,7 | 10,0 |
| Devoto | 7,0 | 1,53 | 10,6 | 58,6 |
| Hardcore | 6,1 | 1,52 | 9,2 | 37,1 |
| Regular | 4,1 | 1,56 | 6,5 | 17,8 |

## Perfis na campanha completa (média das seeds)

| Perfil | Pessoas | Dias de jogo/semana | Sessões por dia de jogo | Horas/semana | Nível mediano no fim | Nível p90 | Sessões em dia útil iniciadas entre 18h e 23h |
|---|---:|---:|---:|---:|---:|---:|---:|
| Casual | 353 | 3,2 | 1,53 | 10,5 | 21,4 | 30,0 | 48,8% |
| Devoto | 19,4 | 6,9 | 1,52 | 57,6 | 30,0 | 30,0 | 37,7% |
| Hardcore | 101 | 6,1 | 1,52 | 37,1 | 30,0 | 30,0 | 48,8% |
| Regular | 527 | 4,3 | 1,57 | 18,5 | 27,6 | 30,0 | 49,1% |

## Evolução dos atributos do jogador (média das seeds)

Variação média desde o início, em **pontos da escala 1–20** (1 ponto ≈ 52,6 unidades do Core), ao fim de 90 dias. Valores de `population.json` (`dynamics`).

| Perfil | Mecânica | Conhecimento | PvE | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | +0,20 | +1,12 | +0,21 | +0,11 | +0,01 | -0,02 | 15,6 | 3,0 |
| Devoto | +0,89 | +2,18 | +0,48 | +0,19 | +0,05 | -0,14 | 41,8 | 5,7 |
| Hardcore | +0,72 | +1,72 | +0,43 | +0,18 | +0,03 | -0,10 | 32,9 | 6,5 |
| Regular | +0,42 | +1,39 | +0,30 | +0,15 | +0,02 | -0,05 | 22,1 | 4,8 |

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
| Aposentado | 20,2 | 8,9 | 26,3 | — | 15,3 h | 0,0% | R$ 3.426 | R$ 36,9 |
| Autônomo / freelancer | 42,4 | 8,2 | 20,5 | 9,3 h | 14,3 h | 0,0% | R$ 4.269 | R$ 55,1 |
| Trabalho em horário comercial | 229 | 5,2 | 15,0 | 4,3 h | 14,3 h | 0,0% | R$ 4.421 | R$ 29,8 |
| Cuidando da casa e da família | 30,0 | 7,7 | 16,8 | 8,4 h | — | 0,0% | R$ 0,0 | R$ 2.322 |
| Comércio e serviços em escala 5x2 | 82,4 | 4,9 | 13,0 | 3,6 h | 14,4 h | 0,0% | R$ 2.604 | R$ 31,8 |
| Comércio em escala 6x1 | 37,0 | 4,7 | 12,4 | 5,0 h | 13,9 h | 0,0% | R$ 2.719 | R$ 28,1 |
| Estudante (manhã) | 106 | 7,2 | 21,3 | 8,6 h | 14,3 h | 0,0% | R$ 0,0 | R$ 739 |
| Estudante (tarde) | 45,4 | 7,9 | 22,7 | 8,9 h | 14,7 h | 0,0% | R$ 0,0 | R$ 729 |
| Home office (tecnologia) | 80,4 | 7,2 | 18,3 | 6,7 h | 14,5 h | 0,0% | R$ 8.226 | R$ 17,5 |
| Meio período (manhã) | 30,0 | 7,4 | 21,5 | 9,7 h | 14,5 h | 0,0% | R$ 1.971 | R$ 294 |
| Meio período (tarde) | 25,2 | 7,7 | 20,2 | 9,5 h | 14,7 h | 0,0% | R$ 1.914 | R$ 286 |
| Sem ocupação | 82,0 | 9,5 | 30,5 | — | 15,3 h | 0,0% | R$ 0,0 | R$ 811 |
| Trabalha de dia e estuda à noite | 55,8 | 2,5 | 7,4 | 1,2 h | 12,5 h | 52,2% | R$ 3.384 | R$ 53,2 |
| Turno noturno | 31,6 | 6,4 | 16,7 | 6,3 h | 13,4 h | 0,0% | R$ 3.614 | R$ 19,8 |
| Universitário (diurno) | 103 | 6,5 | 20,1 | 7,3 h | 14,3 h | 0,0% | R$ 527 | R$ 1.374 |

Como terminaram os dias da agenda:

| Dia | Parcela |
|---|---:|
| Jogou | 57,1% |
| Tinha tempo, preferiu outro lazer | 38,4% |
| Sem tempo livre | 2,9% |
| Inativo | 1,1% |
| Cansado (dormiu pouco) | 0,4% |
| Sem vontade (engajamento baixo) | 0,1% |
| procrastinated | 0,0% |

## Sono e dificuldade de parar (média das seeds, campanha completa)

Importância do jogo, tendência a prolongar e autocontrole são traços separados. A extensão tem limites explícitos (vezes por sessão, minutos, sono mínimo e teto do déficit); só corta sono no fim do dia, nunca uma obrigação. Sono cortado vira déficit; manhã livre e noites normais recuperam.

- Extensões na janela: 407 (121 gostando da sessão, 286 perto de concluir a meta); 407 avançaram sobre a hora de dormir.
- Noites encurtadas pelo jogo: 371, 61,6 min a menos em média.

| Perfil | Extensões/semana | Sono cortado/semana | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|
| Casual | 0,0 | 0,6 min | 0,2 h | 1,8% |
| Devoto | 0,1 | 7,2 min | 0,0 h | 0,0% |
| Hardcore | 0,1 | 5,6 min | 0,4 h | 2,8% |
| Regular | 0,0 | 1,9 min | 0,3 h | 2,1% |

Sono da noite anterior pela agenda (o sono cortado pelo jogo vai direto para o déficit). Em dia livre a pessoa dorme mais para recuperar. O déficit no fim depende do dia da semana em que a campanha termina.

| Ocupação | Precisa | Sono em dia com obrigação | Sono em dia livre | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|---:|
| Aposentado | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Autônomo / freelancer | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Trabalho em horário comercial | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Cuidando da casa e da família | 7,8 h | 7,8 h | — | 0,1 h | 0,0% |
| Comércio e serviços em escala 5x2 | 7,8 h | 7,8 h | 7,9 h | 0,0 h | 0,0% |
| Comércio em escala 6x1 | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Estudante (manhã) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Estudante (tarde) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Home office (tecnologia) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Meio período (manhã) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Meio período (tarde) | 7,9 h | 7,9 h | 7,9 h | 0,0 h | 0,0% |
| Sem ocupação | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Trabalha de dia e estuda à noite | 7,9 h | 6,9 h | 9,7 h | 4,5 h | 36,7% |
| Turno noturno | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Universitário (diurno) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |

## Jogar no horário de trabalho (média das seeds)

Só no trabalho remoto. Pausa: jogar no intervalo permitido. Escapada: jogar no expediente, rara, pela flexibilidade do trabalho, importância do jogo, falta de autocontrole e folga do dia; sessão curta e interrompível. Deslocamento, trabalho presencial e estudo ficam protegidos. Sem consequência profissional por enquanto.

- Sessões na pausa: 1.128 (1,5% das sessões); escapadas: 220 (0,3%).
- Chamados do trabalho durante a escapada: 76,8 (9,0 terminaram a atividade, 67,8 largaram e saíram).

| Ocupação | Pausa/semana | Escapadas/semana | Dias de trabalho com escapada |
|---|---:|---:|---:|
| Autônomo / freelancer | 0,8 | 0,2 | 2,8% |
| Home office (tecnologia) | 0,8 | 0,1 | 3,0% |

## Distribuições (R3)

Medição da campanha completa (com humor), seeds somadas, na janela de medição. A referência antes da R4 (modelo de vontade) está em `docs/baselines/pre-R4/`, com o mesmo relatório e o cenário controlado (pessoas iguais por célula perfil × ocupação), que separa o efeito das regras da mistura da população.

**Duração das sessões (minutos, sessões normais).** Inclui extensões e o fim real; pausa e escapada do trabalho à parte.

| Perfil | Sessões | p10 | p25 | Mediana | p75 | p90 | Máx. | Média |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 101.809 | 64,0 | 87,0 | 122 | 163 | 200 | 493 | 128 |
| Devoto | 11.932 | 118 | 230 | 338 | 435 | 511 | 887 | 330 |
| Hardcore | 54.288 | 101 | 184 | 242 | 308 | 372 | 741 | 244 |
| Regular | 206.199 | 84,0 | 118 | 160 | 210 | 258 | 660 | 167 |
| Pausa no trabalho | 5.640 | 38,0 | 52,0 | 55,0 | 55,0 | 55,0 | 55,0 | 50,9 |
| Escapada no expediente | 1.099 | 9,0 | 18,0 | 23,0 | 26,0 | 29,0 | 34,0 | 21,3 |

**Horas por semana, perfil × ocupação** (por pessoa registrada; "—" com menos de 10 pessoas na soma das seeds).

| Ocupação | Casual | Devoto | Hardcore | Regular |
|---|---:|---:|---:|---:|
| Aposentado | 17,3 | — | 55,7 | 27,7 |
| Autônomo / freelancer | 12,2 | — | 43,8 | 22,8 |
| Trabalho em horário comercial | 8,7 | — | 31,9 | 15,6 |
| Cuidando da casa e da família | 9,2 | — | 35,4 | 17,2 |
| Comércio e serviços em escala 5x2 | 7,6 | — | 30,9 | 13,9 |
| Comércio em escala 6x1 | 8,5 | — | 27,7 | 12,4 |
| Estudante (manhã) | 12,4 | 54,2 | 43,3 | 22,1 |
| Estudante (tarde) | 12,1 | 53,2 | 39,3 | 21,8 |
| Home office (tecnologia) | 9,9 | — | 38,9 | 19,1 |
| Meio período (manhã) | 11,9 | — | 46,9 | 23,4 |
| Meio período (tarde) | 11,3 | — | 45,7 | 21,8 |
| Sem ocupação | 17,7 | 65,2 | 53,3 | 27,6 |
| Trabalha de dia e estuda à noite | 4,5 | — | 15,0 | 7,7 |
| Turno noturno | 10,8 | — | 38,2 | 16,8 |
| Universitário (diurno) | 10,9 | 49,0 | 39,4 | 19,3 |

**Sessões por dia de jogo:** 57,9% com uma, 31,0% com duas, 11,1% com três ou mais. Intervalo entre sessões do mesmo dia: mediana 87,0 min, p90 386 min.
**Voltas depois de uma pausa (v3):** em 40,5% dos dias com jogo. Sessões normais por dia com jogo: uma 57,9%, duas 31,1%, três ou mais 11,0%. Voltas consideradas e recusadas: 37.059. Sessão da noite deixada de lado depois de jogar no trabalho: 42 dias.

**Por que as sessões começaram e terminaram** (sessões normais, seeds somadas).

| Início | Parcela | | Fim | Parcela |
|---|---:|---|---|---:|
| Antes do horário, por vontade | 42,1% | | Decidiu parar (saciedade, cansaço, outras opções) | 73,3% |
| Voltou depois de uma pausa | 35,5% | | Hora de dormir | 20,3% |
| No horário de costume | 22,5% | | Fim do tempo livre (obrigação, preparo, compromisso) | 5,7% |
| Primeira (cadastro ou volta ao jogo) | 0,0% | | Depois de ficar acordado jogando | 0,5% |
|  |  | | Fim planejado (sem decisão) | 0,2% |
|  |  | | Frustração | 0,0% |

**Sessões mais longas e por quê** (as 8 maiores sessões normais, seeds somadas). Resto: terminar a atividade ou a viagem.

| Perfil | Ocupação | Duração | Ao começar | Continuou | Ficou acordado | Resto | Disposição | Tempo livre do dia |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Devoto | Sem ocupação | 887 min | 501 min | 386 min | 0 min | 0 min | +7% | 15,8 h |
| Devoto | Sem ocupação | 857 min | 437 min | 240 min | 180 min | 0 min | +0% | 15,6 h |
| Devoto | Universitário (diurno) | 849 min | 574 min | 270 min | 0 min | 5 min | −12% | 15,4 h |
| Devoto | Sem ocupação | 822 min | 351 min | 471 min | 0 min | 0 min | −1% | 15,5 h |
| Devoto | Sem ocupação | 820 min | 367 min | 450 min | 0 min | 3 min | −12% | 15,5 h |
| Devoto | Estudante (manhã) | 802 min | 556 min | 240 min | 0 min | 6 min | −3% | 15,0 h |
| Devoto | Sem ocupação | 791 min | 541 min | 249 min | 0 min | 1 min | +1% | 15,3 h |
| Devoto | Sem ocupação | 781 min | 501 min | 270 min | 0 min | 10 min | −6% | 15,8 h |


**Quanto do tempo livre vira jogo** (minutos jogados ÷ minutos livres da agenda; o jogo no trabalho e as extensões entram no jogado).

| Perfil | Uso |
|---|---:|
| Casual | 15,1% |
| Devoto | 62,1% |
| Hardcore | 53,6% |
| Regular | 26,5% |

| Ocupação | Uso |
|---|---:|
| Aposentado | 23,0% |
| Autônomo / freelancer | 24,9% |
| Trabalho em horário comercial | 26,1% |
| Cuidando da casa e da família | 26,6% |
| Comércio e serviços em escala 5x2 | 25,5% |
| Comércio em escala 6x1 | 26,5% |
| Estudante (manhã) | 27,1% |
| Estudante (tarde) | 27,4% |
| Home office (tecnologia) | 26,3% |
| Meio período (manhã) | 25,8% |
| Meio período (tarde) | 24,4% |
| Sem ocupação | 26,2% |
| Trabalha de dia e estuda à noite | 18,9% |
| Turno noturno | 25,8% |
| Universitário (diurno) | 28,2% |

**Composição do tempo jogado:** 1.082.338 h no total; extensões 0,2%; pausa e escapada do trabalho 0,5%; sono cortado pelo jogo 1.904 h (0,2% do tempo jogado).

**Dias sem sessão principal** (42,9% dos dias da agenda):

| Motivo | Parcela dos dias sem sessão |
|---|---:|
| Sem oportunidade (nenhuma janela comporta uma sessão) | 6,8% |
| Escolha: tinha tempo, preferiu outro lazer | 88,0% |
| Cansaço | 0,9% |
| Desânimo (engajamento baixo) | 0,3% |
| Inativo | 2,7% |
| Jogou só no horário de trabalho | 1,5% |

**Efeito do tempo livre, separado** (dias da agenda por faixa de tempo livre): a frequência (chance de jogar) e a duração (minutos planejados quando joga) medidas à parte.

| Tempo livre no dia | Dias | Jogou | Duração planejada quando joga | Jogado por dia |
|---|---:|---:|---:|---:|
| até 2 h | 4.435 | 0,5% | 53,9 min | 0,2 min |
| 2–4 h | 19.337 | 44,3% | 149 min | 73,8 min |
| 4–6 h | 21.710 | 45,5% | 157 min | 87,2 min |
| 6–10 h | 42.812 | 55,8% | 154 min | 144 min |
| 10 h ou mais | 61.706 | 70,1% | 163 min | 203 min |

**Corte de sono (observações do criador, R2/R3).**

- **Onde pode cortar:** só a janela que termina **por causa do sono** avança sobre ele. Janela que termina por obrigação, preparo, deslocamento ou compromisso não avança (teste).
- **Teto compartilhado:** os 180 minutos por período de sono valem para a soma de todas as sessões que afetam aquele período (teste).
- **Medido:** 1.854 períodos de sono encurtados pelo jogo; corte por período: mediana 54,0 min, p90 112 min, máximo 180 min.

## Localidades (média das seeds)

Cada pessoa joga no horário e nos feriados de onde mora; o relógio do jogo é o do servidor. O início médio é uma média circular das horas (a meia-noite não distorce), então local e servidor diferem exatamente pelo fuso.

| Localidade | Fuso | Pessoas | Estrangeiros | Sessões | Início médio (local) | Início médio (servidor) |
|---|---|---:|---:|---:|---:|---:|
| Argentina | UTC−3 | 85,6 | 2,2 | 6.576 | 16h38 | 16h38 |
| Brasil | UTC−3 | 758 | 19,0 | 58.128 | 16h41 | 16h41 |
| China | UTC+8 | 75,4 | 0,8 | 5.657 | 16h46 | 05h46 |
| Estados Unidos | UTC−5 | 81,4 | 12,6 | 5.959 | 16h48 | 18h48 |

## Retenção (média das seeds, no fim)

| Perfil | Desistiram | Inativos | Voltaram alguma vez |
|---|---:|---:|---:|
| Casual | 1,5% | 2,3% | 4,8% |
| Devoto | 1,1% | 0,0% | 0,0% |
| Hardcore | 0,2% | 0,4% | 0,6% |
| Regular | 0,5% | 1,1% | 1,6% |

Desistências: dia mediano 67,0, nível mediano 9,0. Sessões estendidas por estarem indo bem: 0,5%; encerradas por frustração: 0,0%.

Motivo principal das desistências (o que mais incomodava; "long_inactivity/" = sumiu por muito tempo):

| Motivo | Desistências |
|---|---:|
| long_inactivity/long_inactivity | 78,0% |
| long_inactivity/boredom | 14,6% |
| boredom | 4,9% |
| progress | 2,4% |

## Humor e memória (média das seeds, no fim)

Satisfação, frustração, novidade e engajamento em permille (0–1000); progresso em permille do XP de referência do nível.

| Perfil | Satisfação | Insatisfeitos (< 400) | Frustração | Novidade | Engajamento | Intenção de continuar | Expectativa | Progresso da última sessão | Memórias |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 614 | 0,1% | 3,7 | 971 | 726 | 673 | 587 | 478 | 9,7 |
| Devoto | 488 | 12,7% | 16,1 | 311 | 570 | 542 | 568 | 325 | 7,1 |
| Hardcore | 541 | 1,0% | 6,6 | 659 | 644 | 601 | 596 | 369 | 9,0 |
| Regular | 586 | 0,3% | 5,3 | 915 | 701 | 646 | 605 | 450 | 10,1 |

Sessões ótimas: 9,7% · ruins: 0,0% (após a semana de lançamento).

O que mais incomoda cada pessoa (motivo mais negativo acumulado):

| Motivo | Pessoas |
|---|---:|
| boredom | 96,6% |
| progress | 1,8% |
| nenhum | 0,9% |
| fatigue | 0,6% |
| fun | 0,0% |
| fit | 0,0% |

## Uso do tempo conectado (todas as seeds)

Caça, chefe, patrulha e emboscada ainda são atividades abstratas (tempo e XP, sem luta simulada) até o M3; aí o tempo se divide em viagem, procura, luta, recuperação e recompensa.

| Tipo | Tempo | Escolhas |
|---|---:|---:|
| Caça | 29,0% | 30,2% |
| Deslocamento | 20,4% | — |
| Socializar | 15,8% | 15,4% |
| Coleta | 12,1% | 17,4% |
| Exploração | 9,0% | 8,8% |
| Comércio | 5,2% | 10,2% |
| Patrulha | 4,0% | 4,0% |
| Missão | 1,7% | 8,3% |
| Emboscada | 1,2% | 1,8% |
| Treino | 1,0% | 3,4% |
| Espera (sem opção viável) | 0,3% | — |
| Conserto | 0,1% | 0,5% |

## Continuidade das sessões (média das seeds)

| Medida | Valor |
|---|---:|
| Escolhas por sessão | 6,0 |
| Continuou na mesma atividade (% das escolhas após a primeira) | 75,5% |
| Trocas de atividade por hora conectada | 0,4 |
| Trocas de zona por sessão | 0,5 |
| Ciclos seguidos na mesma atividade (média) | 2,7 |

Motivos das trocas de plano (fator principal):

| Motivo | Trocas |
|---|---:|
| quest_step | 21,4% |
| quest_completed | 16,7% |
| boredom | 10,7% |
| time_budget | 10,1% |
| level_up | 8,1% |
| focus_unavailable | 6,9% |
| personality | 5,6% |
| goal_completed | 5,1% |
| affinity | 4,3% |
| nothing_new | 2,7% |
| quirk_impulsivo | 1,8% |
| goal | 1,2% |
| zone_discovered | 1,1% |
| quest_deliver | 1,0% |
| quest_offer | 1,0% |
| company | 0,6% |
| level_fit | 0,5% |
| crowding | 0,4% |
| quirk_colecionador | 0,4% |
| risk | 0,2% |
| noise | 0,0% |
| memory | 0,0% |
| challenge | 0,0% |
| familiarity | 0,0% |
| early_progression | 0,0% |
| interruption | 0,0% |

## Atividades pouco usadas (menos de 1% das escolhas)

| Zona | Atividade | Tipo | Escolhas | Diagnóstico |
|---|---|---|---:|---|
| Cidadela de Cinzaforte | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Covil do Rei Goblin | Enfrentar o Rei Goblin | `boss` | 0,0% | exige grupo de 5; grupos chegam no M6, então hoje ninguém pode fazer. |
| Fortaleza Sombria | Enfrentar o Senhor das Cinzas | `boss` | 0,0% | exige grupo de 10; grupos chegam no M6, então hoje ninguém pode fazer; nível mínimo 28: só 44% dos personagens chegaram até o fim. |
| Forte do Vau | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Observar a forja vulcânica | `quest` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Vila Aurora | Consertar equipamento | `repair` | 0,1% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Explorar trilhas da floresta | `explore` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Ler as inscrições da cripta | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Consertar equipamento | `repair` | 0,1% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Picos Gelados | Investigar o covil do yeti | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Planalto dos Ventos | Espiar o acampamento orc | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Coletar cristais sombrios | `gather` | 0,1% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Examinar o altar profanado | `quest` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Terras Queimadas | Explorar as terras queimadas | `explore` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Caçar mortos-vivos | `hunt` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ravina Escarlate | Emboscar viajantes | `ambush` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mata dos Espinhos | Seguir os rastros dos trolls | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Explorar galerias profundas | `explore` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Procurar o estandarte perdido | `quest` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Patrulhar as ruínas | `patrol` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Brejo das Brumas | Procurar o ninho das serpentes | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Investigar as galerias abandonadas | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Examinar os ninhos das harpias | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Falar com o lenhador | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar aranhas tecelãs | `hunt` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Cidadela de Cinzaforte | Consertar equipamento | `repair` | 0,3% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Ravina Escarlate | Patrulhar a ravina | `patrol` | 0,5% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Forte do Vau | Treinar com o mestre de armas | `train` | 0,6% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Caçar salamandras de fogo | `hunt` | 0,6% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Terras Queimadas | Enfrentar golens de magma | `hunt` | 0,6% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ravina Escarlate | Caçar esqueletos da ravina | `hunt` | 0,6% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Caçar lobos das colinas | `hunt` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Caçar morcegos gigantes | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Explorar as colinas | `explore` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Purgar a cripta | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Caçar goblins | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Explorar a fortaleza | `explore` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Caçar ursos pardos | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar javalis | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Coletar ervas curativas | `gather` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Vila Aurora | Treinar habilidades | `train` | 1,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Caçar lobos | `hunt` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |

## Execuções

| Seed | Hash final | Simulação |
|---:|---|---:|
| 1 | `9aef9dfd3175782d` | 248,8 s |
| 2 | `421aab2dc4bbc505` | 250,6 s |
| 3 | `9e2b2cb2b015f447` | 248,9 s |
| 4 | `931724afdcc78114` | 242,7 s |
| 5 | `2aa0bc5153700647` | 244,8 s |

Medições de tempo desta máquina, não benchmark.
