# Validação da simulação

> Gerado pelo `Simulation.Runner` com várias seeds. As faixas vêm de `targets.json` e são **hipóteses de design ajustáveis**, não regras do mundo. Uma meta só conta como "dentro" quando todas as seeds caem na faixa.

| Campo | Valor |
|---|---|
| Conteúdo | `reports/exp-ded/mundos/B` (`7ce9043dbeedddf5`) |
| Metas | `reports/exp-ded/mundos/B/targets.json` (`d8f348f9966ab923`) |
| Simulador | 0.1.0+ebf4e31c53eede8349491bbc1daecf818cb137e9 |
| Execuções | 5 seeds (1, 2, 3, 4, 5) · 1.000 pessoas · 90 dias |
| Janela das medidas semanais | a partir do dia 7 (exclui a semana de lançamento) |
| Tempo total | 343,4 s |

## Metas

As metas têm três classes (D-039a); o total "x de y" sozinho não diz se dá para avançar:

- **Regras do cenário:** rotina calibrada, medida numa rodada controlada com o humor neutralizado. Falhar aqui indica erro.
- **Provisórias:** balanceamento que será recalibrado (progressão, divisão do tempo, missões).
- **Dependem de sistemas futuros:** só acompanhamento, não reprovam (retenção, missões e coleta dependem de combate, economia e social).

### Regras do cenário: 9 de 9 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Dias de jogo por semana — casual | 2,0–4,0  | 2,8 | 2,8–2,8 |  |
| ✅ dentro | Dias de jogo por semana — hardcore | 5,0–7,0  | 6,2 | 5,9–6,5 |  |
| ✅ dentro | Sessões por dia de jogo — casual | 1,1–2,0  | 1,5 | 1,5–1,5 |  |
| ✅ dentro | Sessões por dia de jogo — hardcore | 1,2–2,5  | 1,5 | 1,5–1,6 |  |
| ✅ dentro | Horas por semana — casual | 3,0–9,0 h | 6,8 | 6,7–7,0 |  |
| ✅ dentro | Horas por semana — hardcore | 15,0–45,0 h | 36,2 | 35,3–38,0 |  |
| ✅ dentro | Jogadores ativos em dia útil / cadastrados | 30,0–55,0 % | 52,3 | 51,6–53,0 | 58 dias |
| ✅ dentro | Ativos no fim de semana ÷ ativos em dia útil | 1,3–2,0 × | 1,3 | 1,3–1,3 |  |
| ✅ dentro | Pico online do dia / cadastrados | 15,0–35,0 % | 24,9 | 24,0–25,8 |  |

### Provisórias: 9 de 12 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Horas conectadas até o nível 5 (mediana) | 1,0–4,0 h | 2,7 | 2,7–2,7 | 100% chegaram |
| ✅ dentro | Horas conectadas até o nível 10 (mediana) | 8,0–20,0 h | 15,4 | 15,2–15,5 | 98–99% chegaram |
| ✅ dentro | Horas conectadas até o nível 15 (mediana) | 22,0–50,0 h | 36,6 | 36,4–36,9 | 80–82% chegaram |
| ❌ abaixo | Horas conectadas até o nível 20 (mediana) | 80,0–110 h | 69,4 | 68,8–70,0 | 60–62% chegaram |
| ✅ dentro | Horas conectadas até o nível 30 (mediana) | 170–230 h | 173 | 170–175 | 27–30% chegaram (mediana só de quem chegou: subestima o tempo real) |
| ❌ acima | Casuais no nível 10 ou mais ao fim | 60,0–95,0 % | 98,9 | 98,5–99,4 |  |
| ✅ dentro | XP vindo de quests | 15,0–35,0 % | 20,6 | 20,2–21,1 |  |
| ✅ dentro | Quests concluídas por personagem (média) | 4,0–30,0  | 28,5 | 27,9–29,3 |  |
| ✅ dentro | Tempo em atividades de combate (abstratas: caça, chefe, patrulha, emboscada; sem luta simulada até o M3) | 35,0–55,0 % | 41,8 | 41,0–42,4 |  |
| ✅ dentro | Tempo explorando | 5,0–15,0 % | 6,9 | 6,2–7,5 |  |
| ✅ dentro | Tempo na cidade (comércio, taverna, treino, conserto) | 10,0–25,0 % | 22,4 | 21,9–23,2 |  |
| ❌ acima | Tempo andando ou esperando | 5,0–20,0 % | 21,4 | 21,3–21,6 |  |

### Dependem de sistemas futuros (acompanhamento): 1 de 5 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ❌ abaixo | Tempo em missões | 10,0–25,0 % | 2,2 | 2,2–2,2 |  |
| ❌ abaixo | Tempo em coleta | 8,0–20,0 % | 5,2 | 5,0–5,3 |  |
| ❌ abaixo | Desistentes no fim / cadastrados | 8,0–30,0 % | 0,7 | 0,4–0,9 |  |
| ❌ abaixo | Inativos no fim / cadastrados | 2,0–15,0 % | 1,1 | 0,8–1,3 |  |
| ✅ dentro | Quem saiu e voltou (inativos e desistentes) | 20,0–70,0 % | 59,1 | 51,4–68,4 | 35 saíram; 40 saíram; 44 saíram; 38 saíram; 39 saíram |


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
| Casual | 2,8 | 1,49 | 4,2 | 6,8 |
| Comerciante | 4,4 | 1,71 | 7,5 | 14,4 |
| Competitivo | 4,7 | 1,57 | 7,5 | 20,7 |
| Devoto | 7,0 | 1,53 | 10,7 | 58,1 |
| Explorador | 4,0 | 1,66 | 6,7 | 15,3 |
| Hardcore | 6,2 | 1,52 | 9,4 | 36,2 |
| Social | 3,7 | 1,55 | 5,7 | 12,3 |

## Perfis na campanha completa (média das seeds)

| Perfil | Pessoas | Dias de jogo/semana | Sessões por dia de jogo | Horas/semana | Nível mediano no fim | Nível p90 | Sessões em dia útil iniciadas entre 18h e 23h |
|---|---:|---:|---:|---:|---:|---:|---:|
| Casual | 346 | 2,9 | 1,51 | 7,2 | 20,4 | 25,6 | 52,1% |
| Comerciante | 97,4 | 4,4 | 1,71 | 14,6 | 17,0 | 22,0 | 50,7% |
| Competitivo | 145 | 4,9 | 1,58 | 21,5 | 30,0 | 30,0 | 52,9% |
| Devoto | 18,6 | 6,9 | 1,52 | 57,3 | 30,0 | 30,0 | 37,4% |
| Explorador | 98,0 | 4,4 | 1,70 | 17,2 | 29,2 | 30,0 | 46,3% |
| Hardcore | 97,2 | 6,2 | 1,52 | 36,7 | 30,0 | 30,0 | 51,8% |
| Social | 197 | 3,8 | 1,57 | 13,0 | 13,0 | 17,0 | 52,7% |

## Evolução dos atributos do jogador (média das seeds)

Variação média desde o início, em **pontos da escala 1–20** (1 ponto ≈ 52,6 unidades do Core), ao fim de 90 dias. Valores de `population.json` (`dynamics`).

| Perfil | Mecânica | Conhecimento | PvE | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | +0,14 | +1,32 | +0,21 | +0,11 | +0,01 | 0,00 | 14,1 | 3,2 |
| Comerciante | +0,03 | +0,99 | +0,10 | +0,06 | +0,33 | 0,00 | 22,5 | 7,7 |
| Competitivo | +0,84 | +1,26 | +0,29 | +0,06 | +0,03 | -0,25 | 19,2 | 15,0 |
| Devoto | +1,16 | +0,98 | +1,02 | +0,05 | +0,01 | -0,17 | 43,6 | 3,2 |
| Explorador | +0,29 | +1,47 | +0,17 | +0,41 | +0,02 | -0,05 | 20,9 | 7,8 |
| Hardcore | +1,04 | +0,97 | +0,85 | +0,05 | 0,00 | -0,10 | 35,8 | 4,8 |
| Social | +0,03 | +1,10 | +0,07 | +0,04 | 0,00 | 0,00 | 18,0 | 7,9 |

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
| Aposentado | 20,0 | 9,0 | 21,6 | — | 15,3 h | 0,0% | R$ 3.438 | R$ 37,2 |
| Autônomo / freelancer | 42,2 | 8,4 | 18,1 | 8,9 h | 15,2 h | 0,0% | R$ 4.312 | R$ 41,2 |
| Trabalho em horário comercial | 227 | 5,2 | 13,0 | 4,2 h | 14,8 h | 0,0% | R$ 4.417 | R$ 30,0 |
| Cuidando da casa e da família | 30,2 | 7,7 | 15,9 | 8,4 h | — | 0,0% | R$ 0,0 | R$ 2.264 |
| Comércio e serviços em escala 5x2 | 82,4 | 4,9 | 11,7 | 3,3 h | 14,9 h | 0,0% | R$ 2.595 | R$ 31,2 |
| Comércio em escala 6x1 | 37,0 | 4,6 | 10,7 | 4,8 h | 15,1 h | 0,0% | R$ 2.721 | R$ 27,8 |
| Estudante (manhã) | 108 | 7,4 | 19,3 | 8,4 h | 14,7 h | 0,0% | R$ 0,0 | R$ 732 |
| Estudante (tarde) | 46,2 | 7,7 | 19,1 | 8,7 h | 15,2 h | 0,0% | R$ 0,0 | R$ 749 |
| Home office (tecnologia) | 80,2 | 7,1 | 15,0 | 6,4 h | 15,1 h | 0,0% | R$ 8.225 | R$ 16,7 |
| Meio período (manhã) | 30,0 | 7,7 | 19,1 | 9,5 h | 14,9 h | 0,0% | R$ 1.964 | R$ 291 |
| Meio período (tarde) | 25,6 | 7,9 | 18,5 | 9,3 h | 15,1 h | 0,0% | R$ 1.962 | R$ 290 |
| Sem ocupação | 80,8 | 9,6 | 26,9 | — | 15,3 h | 0,0% | R$ 0,0 | R$ 832 |
| Trabalha de dia e estuda à noite | 56,2 | 2,5 | 6,3 | 1,0 h | 12,9 h | 53,2% | R$ 3.388 | R$ 52,4 |
| Turno noturno | 31,8 | 5,7 | 14,1 | 7,1 h | 11,4 h | 0,0% | R$ 3.611 | R$ 19,7 |
| Universitário (diurno) | 102 | 6,6 | 16,9 | 7,1 h | 14,8 h | 0,0% | R$ 522 | R$ 1.366 |

Como terminaram os dias da agenda:

| Dia | Parcela |
|---|---:|
| Jogou | 56,7% |
| Tinha tempo, preferiu outro lazer | 39,0% |
| Sem tempo livre | 3,0% |
| Inativo | 0,9% |
| Cansado (dormiu pouco) | 0,4% |
| Sem vontade (engajamento baixo) | 0,0% |

## Sono e dificuldade de parar (média das seeds, campanha completa)

Importância do jogo, tendência a prolongar e autocontrole são traços separados. A extensão tem limites explícitos (vezes por sessão, minutos, sono mínimo e teto do déficit); só corta sono no fim do dia, nunca uma obrigação. Sono cortado vira déficit; manhã livre e noites normais recuperam.

- Extensões na janela: 423 (134 gostando da sessão, 289 perto de concluir a meta); 423 avançaram sobre a hora de dormir.
- Noites encurtadas pelo jogo: 383, 70,1 min a menos em média.

| Perfil | Extensões/semana | Sono cortado/semana | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|
| Casual | 0,0 | 0,2 min | 0,2 h | 2,0% |
| Comerciante | 0,0 | 0,4 min | 0,4 h | 2,8% |
| Competitivo | 0,0 | 2,4 min | 0,3 h | 2,2% |
| Devoto | 0,1 | 11,9 min | 0,1 h | 0,0% |
| Explorador | 0,0 | 1,0 min | 0,2 h | 1,6% |
| Hardcore | 0,2 | 14,0 min | 0,4 h | 3,4% |
| Social | 0,0 | 0,8 min | 0,2 h | 1,9% |

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
| Meio período (tarde) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Sem ocupação | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Trabalha de dia e estuda à noite | 7,9 h | 6,9 h | 9,7 h | 4,5 h | 38,4% |
| Turno noturno | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Universitário (diurno) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |

## Jogar no horário de trabalho (média das seeds)

Só no trabalho remoto. Pausa: jogar no intervalo permitido. Escapada: jogar no expediente, rara, pela flexibilidade do trabalho, importância do jogo, falta de autocontrole e folga do dia; sessão curta e interrompível. Deslocamento, trabalho presencial e estudo ficam protegidos. Sem consequência profissional por enquanto.

- Sessões na pausa: 1.104 (1,4% das sessões); escapadas: 219 (0,3%).
- Chamados do trabalho durante a escapada: 77,2 (9,6 terminaram a atividade, 67,6 largaram e saíram).

| Ocupação | Pausa/semana | Escapadas/semana | Dias de trabalho com escapada |
|---|---:|---:|---:|
| Autônomo / freelancer | 0,8 | 0,2 | 3,1% |
| Home office (tecnologia) | 0,8 | 0,1 | 2,7% |

## Distribuições (R3)

Medição da campanha completa (com humor), seeds somadas, na janela de medição. A referência antes da R4 (modelo de vontade) está em `docs/baselines/pre-R4/`, com o mesmo relatório e o cenário controlado (pessoas iguais por célula perfil × ocupação), que separa o efeito das regras da mistura da população.

**Duração das sessões (minutos, sessões normais).** Inclui extensões e o fim real; pausa e escapada do trabalho à parte.

| Perfil | Sessões | p10 | p25 | Mediana | p75 | p90 | Máx. | Média |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 89.467 | 65,0 | 78,0 | 94,0 | 114 | 139 | 382 | 98,7 |
| Comerciante | 43.176 | 72,0 | 89,0 | 109 | 136 | 170 | 470 | 116 |
| Competitivo | 65.172 | 103 | 137 | 167 | 201 | 237 | 573 | 169 |
| Devoto | 11.695 | 112 | 241 | 344 | 418 | 479 | 797 | 324 |
| Explorador | 42.798 | 90,0 | 111 | 135 | 162 | 195 | 502 | 139 |
| Hardcore | 53.889 | 115 | 193 | 238 | 283 | 333 | 785 | 234 |
| Social | 69.047 | 83,0 | 104 | 126 | 153 | 185 | 548 | 131 |
| Pausa no trabalho | 5.522 | 36,0 | 42,0 | 54,0 | 55,0 | 55,0 | 55,0 | 48,4 |
| Escapada no expediente | 1.094 | 9,0 | 17,0 | 23,0 | 26,0 | 29,0 | 34,0 | 21,2 |

**Horas por semana, perfil × ocupação** (por pessoa registrada; "—" com menos de 10 pessoas na soma das seeds).

| Ocupação | Casual | Comerciante | Competitivo | Devoto | Explorador | Hardcore | Social |
|---|---:|---:|---:|---:|---:|---:|---:|
| Aposentado | 12,1 | 21,9 | 32,8 | — | 26,4 | — | 20,4 |
| Autônomo / freelancer | 8,5 | 17,8 | 27,0 | — | 19,3 | 43,9 | 16,2 |
| Trabalho em horário comercial | 5,9 | 12,0 | 18,4 | — | 14,2 | 33,3 | 10,6 |
| Cuidando da casa e da família | 5,8 | 13,1 | 19,9 | — | 15,3 | 36,9 | 12,3 |
| Comércio e serviços em escala 5x2 | 5,1 | 11,4 | 17,5 | — | 13,1 | 28,7 | 9,4 |
| Comércio em escala 6x1 | 4,9 | 10,6 | 14,7 | — | 11,8 | 28,8 | 8,1 |
| Estudante (manhã) | 8,6 | 17,1 | 25,6 | 54,3 | 20,4 | 43,4 | 15,8 |
| Estudante (tarde) | 8,3 | 15,0 | 22,9 | 50,4 | 19,9 | 43,1 | 14,7 |
| Home office (tecnologia) | 7,2 | 15,3 | 22,4 | — | 15,8 | 38,1 | 13,5 |
| Meio período (manhã) | 8,9 | 18,5 | 28,7 | — | 21,2 | 48,1 | 17,4 |
| Meio período (tarde) | 8,5 | 17,2 | 24,7 | — | 22,9 | 43,9 | 15,5 |
| Sem ocupação | 12,4 | 22,1 | 31,3 | 67,5 | 26,7 | 50,3 | 20,8 |
| Trabalha de dia e estuda à noite | 2,9 | 5,6 | 8,8 | — | 7,0 | 14,7 | 5,4 |
| Turno noturno | 6,2 | 12,7 | 18,9 | — | 13,6 | 34,4 | 10,9 |
| Universitário (diurno) | 7,8 | 15,6 | 23,6 | 50,6 | 17,8 | 40,5 | 13,3 |

**Sessões por dia de jogo:** 57,5% com uma, 30,2% com duas, 12,3% com três ou mais. Intervalo entre sessões do mesmo dia: mediana 80,5 min, p90 180 min.
**Voltas depois de uma pausa (v3):** em 40,6% dos dias com jogo. Sessões normais por dia com jogo: uma 57,9%, duas 30,3%, três ou mais 11,8%. Voltas consideradas e recusadas: 39.387. Sessão da noite deixada de lado depois de jogar no trabalho: 37 dias.

**Por que as sessões começaram e terminaram** (sessões normais, seeds somadas).

| Início | Parcela | | Fim | Parcela |
|---|---:|---|---|---:|
| Antes do horário, por vontade | 45,8% | | Decidiu parar (saciedade, cansaço, outras opções) | 77,9% |
| Voltou depois de uma pausa | 36,1% | | Hora de dormir | 16,7% |
| No horário de costume | 18,1% | | Fim do tempo livre (obrigação, preparo, compromisso) | 4,7% |
|  |  | | Depois de ficar acordado jogando | 0,5% |
|  |  | | Fim planejado (sem decisão) | 0,2% |

**Sessões mais longas e por quê** (as 8 maiores sessões normais, seeds somadas). Resto: terminar a atividade ou a viagem.

| Perfil | Ocupação | Duração | Ao começar | Continuou | Ficou acordado | Resto | Disposição | Tempo livre do dia |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Devoto | Sem ocupação | 797 min | 372 min | 420 min | 0 min | 5 min | +3% | 15,7 h |
| Devoto | Sem ocupação | 794 min | 367 min | 420 min | 0 min | 7 min | +3% | 15,3 h |
| Devoto | Estudante (manhã) | 786 min | 423 min | 360 min | 0 min | 3 min | +6% | 15,4 h |
| Hardcore | Trabalho em horário comercial | 785 min | 284 min | 321 min | 180 min | 0 min | −1% | 15,8 h |
| Devoto | Estudante (manhã) | 772 min | 383 min | 217 min | 172 min | 0 min | +12% | 15,3 h |
| Hardcore | Estudante (manhã) | 760 min | 255 min | 325 min | 180 min | 0 min | +2% | 14,3 h |
| Devoto | Universitário (diurno) | 753 min | 389 min | 360 min | 0 min | 4 min | +3% | 14,6 h |
| Hardcore | Trabalha de dia e estuda à noite | 753 min | 311 min | 436 min | 0 min | 6 min | +8% | 14,3 h |


**Quanto do tempo livre vira jogo** (minutos jogados ÷ minutos livres da agenda; o jogo no trabalho e as extensões entram no jogado).

| Perfil | Uso |
|---|---:|
| Casual | 10,3% |
| Comerciante | 21,1% |
| Competitivo | 31,4% |
| Devoto | 64,5% |
| Explorador | 24,2% |
| Hardcore | 53,2% |
| Social | 18,7% |

| Ocupação | Uso |
|---|---:|
| Aposentado | 18,9% |
| Autônomo / freelancer | 22,4% |
| Trabalho em horário comercial | 22,7% |
| Cuidando da casa e da família | 25,0% |
| Comércio e serviços em escala 5x2 | 22,9% |
| Comércio em escala 6x1 | 22,9% |
| Estudante (manhã) | 24,5% |
| Estudante (tarde) | 23,6% |
| Home office (tecnologia) | 21,6% |
| Meio período (manhã) | 22,4% |
| Meio período (tarde) | 22,2% |
| Sem ocupação | 23,3% |
| Trabalha de dia e estuda à noite | 16,7% |
| Turno noturno | 22,3% |
| Universitário (diurno) | 23,8% |

**Composição do tempo jogado:** 941.691 h no total; extensões 0,2%; pausa e escapada do trabalho 0,5%; sono cortado pelo jogo 2.237 h (0,2% do tempo jogado).

**Dias sem sessão principal** (43,3% dos dias da agenda):

| Motivo | Parcela dos dias sem sessão |
|---|---:|
| Sem oportunidade (nenhuma janela comporta uma sessão) | 6,9% |
| Escolha: tinha tempo, preferiu outro lazer | 88,6% |
| Cansaço | 0,8% |
| Desânimo (engajamento baixo) | 0,0% |
| Inativo | 2,1% |
| Jogou só no horário de trabalho | 1,6% |

**Efeito do tempo livre, separado** (dias da agenda por faixa de tempo livre): a frequência (chance de jogar) e a duração (minutos planejados quando joga) medidas à parte.

| Tempo livre no dia | Dias | Jogou | Duração planejada quando joga | Jogado por dia |
|---|---:|---:|---:|---:|
| até 2 h | 4.482 | 0,0% | — | 0,0 min |
| 2–4 h | 19.940 | 44,0% | 128 min | 65,6 min |
| 4–6 h | 23.988 | 44,3% | 134 min | 75,5 min |
| 6–10 h | 43.094 | 56,3% | 127 min | 131 min |
| 10 h ou mais | 58.496 | 70,8% | 132 min | 179 min |

**Corte de sono (observações do criador, R2/R3).**

- **Onde pode cortar:** só a janela que termina **por causa do sono** avança sobre ele. Janela que termina por obrigação, preparo, deslocamento ou compromisso não avança (teste).
- **Teto compartilhado:** os 180 minutos por período de sono valem para a soma de todas as sessões que afetam aquele período (teste).
- **Medido:** 1.915 períodos de sono encurtados pelo jogo; corte por período: mediana 62,0 min, p90 122 min, máximo 180 min.

## Localidades (média das seeds)

Cada pessoa joga no horário e nos feriados de onde mora; o relógio do jogo é o do servidor. O início médio é uma média circular das horas (a meia-noite não distorce), então local e servidor diferem exatamente pelo fuso.

| Localidade | Fuso | Pessoas | Estrangeiros | Sessões | Início médio (local) | Início médio (servidor) |
|---|---|---:|---:|---:|---:|---:|
| Argentina | UTC−3 | 85,4 | 2,2 | 6.676 | 16h25 | 16h25 |
| Brasil | UTC−3 | 758 | 19,0 | 58.179 | 16h30 | 16h30 |
| China | UTC+8 | 75,4 | 0,8 | 5.716 | 16h36 | 05h36 |
| Estados Unidos | UTC−5 | 81,4 | 12,6 | 5.846 | 16h40 | 18h40 |

## Retenção (média das seeds, no fim)

| Perfil | Desistiram | Inativos | Voltaram alguma vez |
|---|---:|---:|---:|
| Casual | 1,5% | 1,8% | 4,9% |
| Comerciante | 0,2% | 0,7% | 0,8% |
| Competitivo | 0,4% | 0,3% | 0,7% |
| Devoto | 0,0% | 0,0% | 0,0% |
| Explorador | 0,2% | 1,2% | 1,0% |
| Hardcore | 0,0% | 0,0% | 0,4% |
| Social | 0,4% | 1,0% | 1,5% |

Desistências: dia mediano 64,0, nível mediano 7,0. Sessões estendidas por estarem indo bem: 0,6%; encerradas por frustração: 0,0%.

Motivo principal das desistências (o que mais incomodava; "long_inactivity/" = sumiu por muito tempo):

| Motivo | Desistências |
|---|---:|
| long_inactivity/long_inactivity | 97,1% |
| long_inactivity/boredom | 2,9% |

## Humor e memória (média das seeds, no fim)

Satisfação, frustração, novidade e engajamento em permille (0–1000); progresso em permille do XP de referência do nível.

| Perfil | Satisfação | Insatisfeitos (< 400) | Frustração | Novidade | Engajamento | Intenção de continuar | Expectativa | Progresso da última sessão | Memórias |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 608 | 0,0% | 4,5 | 991 | 724 | 654 | 622 | 597 | 10,8 |
| Comerciante | 567 | 0,0% | 8,3 | 942 | 690 | 603 | 456 | 180 | 10,0 |
| Competitivo | 580 | 0,1% | 10,1 | 894 | 694 | 625 | 723 | 533 | 10,0 |
| Devoto | 506 | 0,9% | 0,1 | 97,9 | 563 | 616 | 870 | 521 | 3,0 |
| Explorador | 652 | 0,0% | 3,7 | 950 | 750 | 701 | 601 | 524 | 10,4 |
| Hardcore | 564 | 0,0% | 0,6 | 583 | 653 | 653 | 870 | 592 | 7,2 |
| Social | 603 | 0,0% | 8,9 | 898 | 710 | 689 | 454 | 57,5 | 8,5 |

Sessões ótimas: 9,8% · ruins: 0,0% (após a semana de lançamento).

O que mais incomoda cada pessoa (motivo mais negativo acumulado):

| Motivo | Pessoas |
|---|---:|
| boredom | 98,3% |
| nenhum | 0,8% |
| fatigue | 0,7% |
| progress | 0,1% |

## Uso do tempo conectado (todas as seeds)

Caça, chefe, patrulha e emboscada ainda são atividades abstratas (tempo e XP, sem luta simulada) até o M3; aí o tempo se divide em viagem, procura, luta, recuperação e recompensa.

| Tipo | Tempo | Escolhas |
|---|---:|---:|
| Caça | 32,3% | 32,6% |
| Deslocamento | 21,4% | — |
| Socializar | 14,7% | 14,2% |
| Exploração | 6,9% | 6,9% |
| Comércio | 6,6% | 12,6% |
| Patrulha | 5,4% | 5,3% |
| Coleta | 5,2% | 7,7% |
| Emboscada | 4,2% | 6,1% |
| Missão | 2,2% | 10,7% |
| Treino | 1,1% | 3,5% |
| Conserto | 0,0% | 0,4% |

## Continuidade das sessões (média das seeds)

| Medida | Valor |
|---|---:|
| Escolhas por sessão | 5,3 |
| Continuou na mesma atividade (% das escolhas após a primeira) | 74,8% |
| Trocas de atividade por hora conectada | 0,4 |
| Trocas de zona por sessão | 0,5 |
| Ciclos seguidos na mesma atividade (média) | 2,5 |

Motivos das trocas de plano (fator principal):

| Motivo | Trocas |
|---|---:|
| quest_step | 27,0% |
| quest_completed | 21,9% |
| time_budget | 10,5% |
| focus_unavailable | 9,8% |
| boredom | 9,6% |
| level_up | 8,3% |
| goal_completed | 5,7% |
| affinity | 2,9% |
| zone_discovered | 1,2% |
| quest_deliver | 0,8% |
| nothing_new | 0,7% |
| goal | 0,6% |
| quest_offer | 0,6% |
| level_fit | 0,2% |
| crowding | 0,1% |
| company | 0,1% |
| challenge | 0,0% |
| risk | 0,0% |
| interruption | 0,0% |
| memory | 0,0% |
| familiarity | 0,0% |

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
| Forte do Vau | Consertar equipamento | `repair` | 0,1% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Fortaleza Sombria | Ler as inscrições da cripta | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Explorar trilhas da floresta | `explore` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Investigar o covil do yeti | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Patrulhar as ruínas | `patrol` | 0,1% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Terras Queimadas | Explorar as terras queimadas | `explore` | 0,2% | nível mínimo 22: só 50% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Planalto dos Ventos | Espiar o acampamento orc | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Caçar mortos-vivos | `hunt` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Explorar galerias profundas | `explore` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Examinar o altar profanado | `quest` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Terras Queimadas | Minerar obsidiana | `gather` | 0,2% | nível mínimo 23: só 46% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Caçar aranhas tecelãs | `hunt` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Cidadela de Cinzaforte | Consertar equipamento | `repair` | 0,3% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Mata dos Espinhos | Seguir os rastros dos trolls | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Procurar o estandarte perdido | `quest` | 0,3% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Brejo das Brumas | Procurar o ninho das serpentes | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Investigar as galerias abandonadas | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Examinar os ninhos das harpias | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Falar com o lenhador | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Colher ervas das encostas | `gather` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Caçar lobos das colinas | `hunt` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Socializar na taverna | `socialize` | 0,6% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Picos Gelados | Coletar cristais de gelo | `gather` | 0,6% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Patrulhar a ravina | `patrol` | 0,7% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Coletar ervas curativas | `gather` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Emboscar viajantes | `ambush` | 0,7% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Fortaleza Sombria | Explorar a fortaleza | `explore` | 0,7% | nível mínimo 25: só 40% dos personagens chegaram até o fim. |
| Mina Velha | Minerar ferro e prata | `gather` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Caçar esqueletos da ravina | `hunt` | 0,8% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Caçar morcegos gigantes | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar javalis | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar lobos | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Explorar as colinas | `explore` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Treinar com o mestre de armas | `train` | 0,9% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Colinas do Vale | Caçar ursos pardos | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Caçar goblins | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Brejo das Brumas | Explorar o brejo | `explore` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Caçar salamandras de fogo | `hunt` | 1,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |

## Execuções

| Seed | Hash final | Simulação |
|---:|---|---:|
| 1 | `8d9d1eb729fd6131` | 264,1 s |
| 2 | `4d8be990dca01e6f` | 262,9 s |
| 3 | `097b2bd696591d6b` | 267,9 s |
| 4 | `de2093281b3b773f` | 263,9 s |
| 5 | `6e7677b7a9747587` | 256,6 s |

Medições de tempo desta máquina, não benchmark.
