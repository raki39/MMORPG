# Validação da simulação

> Gerado pelo `Simulation.Runner` com várias seeds. As faixas vêm de `targets.json` e são **hipóteses de design ajustáveis**, não regras do mundo. Uma meta só conta como "dentro" quando todas as seeds caem na faixa.

| Campo | Valor |
|---|---|
| Conteúdo | `Content/presets/inicial` (`d44dcda4c1092a6c`) |
| Metas | `Content/presets/inicial/targets.json` (`322f4c445d8c8479`) |
| Simulador | 0.1.0+c5a4530e0952f1cdde9290672af2f93df9801bf9 |
| Execuções | 5 seeds (1, 2, 3, 4, 5) · 1.000 pessoas · 90 dias |
| Janela das medidas semanais | a partir do dia 7 (exclui a semana de lançamento) |
| Tempo total | 414,1 s |

## Metas

As metas têm três classes (D-039a); o total "x de y" sozinho não diz se dá para avançar:

- **Regras do cenário:** rotina calibrada, medida numa rodada controlada com o humor neutralizado. Falhar aqui indica erro.
- **Provisórias:** balanceamento que será recalibrado (progressão, divisão do tempo, missões).
- **Dependem de sistemas futuros:** só acompanhamento, não reprovam (retenção, missões e coleta dependem de combate, economia e social).

### Regras do cenário: 4 de 7 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ❌ acima | Sessões por semana — casual | 2,0–4,0  | 4,2 | 4,2–4,3 |  |
| ❌ acima | Sessões por semana — hardcore | 5,0–7,0  | 9,5 | 9,0–10,0 |  |
| ❌ acima | Horas por semana — casual | 2,0–6,0 h | 6,8 | 6,7–6,9 |  |
| ✅ dentro | Horas por semana — hardcore | 15,0–45,0 h | 36,8 | 35,8–38,0 |  |
| ✅ dentro | Jogadores ativos em dia útil / cadastrados | 30,0–55,0 % | 51,4 | 50,3–52,4 | 58 dias |
| ✅ dentro | Ativos no fim de semana ÷ ativos em dia útil | 1,3–2,0 × | 1,3 | 1,3–1,4 |  |
| ✅ dentro | Pico online do dia / cadastrados | 15,0–35,0 % | 24,1 | 22,8–24,7 |  |

### Provisórias: 9 de 12 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Horas conectadas até o nível 5 (mediana) | 1,0–4,0 h | 2,7 | 2,7–2,7 | 100% chegaram |
| ✅ dentro | Horas conectadas até o nível 10 (mediana) | 8,0–20,0 h | 15,5 | 15,3–15,7 | 98–99% chegaram |
| ✅ dentro | Horas conectadas até o nível 15 (mediana) | 22,0–50,0 h | 36,8 | 36,5–37,1 | 80% chegaram |
| ❌ abaixo | Horas conectadas até o nível 20 (mediana) | 80,0–110 h | 70,1 | 69,4–71,4 | 59–62% chegaram |
| ✅ dentro | Horas conectadas até o nível 30 (mediana) | 170–230 h | 176 | 174–179 | 26–29% chegaram (mediana só de quem chegou: subestima o tempo real) |
| ❌ acima | Casuais no nível 10 ou mais ao fim | 60,0–95,0 % | 98,9 | 98,3–99,7 |  |
| ✅ dentro | XP vindo de quests | 15,0–35,0 % | 20,4 | 19,7–21,0 |  |
| ✅ dentro | Quests concluídas por personagem (média) | 4,0–30,0  | 27,0 | 26,6–27,7 |  |
| ✅ dentro | Tempo em atividades de combate (abstratas: caça, chefe, patrulha, emboscada; sem luta simulada até o M3) | 35,0–55,0 % | 40,4 | 40,0–40,9 |  |
| ✅ dentro | Tempo explorando | 5,0–15,0 % | 7,4 | 7,0–7,8 |  |
| ✅ dentro | Tempo na cidade (comércio, taverna, treino, conserto) | 10,0–25,0 % | 23,8 | 23,5–24,3 |  |
| ❌ acima | Tempo andando ou esperando | 5,0–20,0 % | 20,8 | 20,6–21,0 |  |

### Dependem de sistemas futuros (acompanhamento): 1 de 5 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ❌ abaixo | Tempo em missões | 10,0–25,0 % | 2,2 | 2,2–2,2 |  |
| ❌ abaixo | Tempo em coleta | 8,0–20,0 % | 5,4 | 5,0–5,7 |  |
| ❌ abaixo | Desistentes no fim / cadastrados | 8,0–30,0 % | 0,8 | 0,3–1,1 |  |
| ❌ abaixo | Inativos no fim / cadastrados | 2,0–15,0 % | 1,2 | 0,7–1,7 |  |
| ✅ dentro | Quem saiu e voltou (inativos e desistentes) | 20,0–70,0 % | 59,2 | 52,9–69,7 | 51 saíram; 47 saíram; 44 saíram; 33 saíram |


Por que cada faixa:

- **Horas conectadas até o nível 5 (mediana):** Ritmo A (D-027): o começo apresenta o jogo; a prévia nominal dá cerca de 2 h. Referência de perfil, não prazo para todos.
- **Horas conectadas até o nível 10 (mediana):** Ritmo A (D-027): prévia nominal de cerca de 11 h, contando viagem e cidade. A desaceleração forte fica para as faixas altas.
- **Horas conectadas até o nível 15 (mediana):** Ritmo A (D-027): prévia nominal de cerca de 31 h. O conteúdo atual termina perto do 15–18 até a redistribuição das áreas.
- **Horas conectadas até o nível 20 (mediana):** Referência do criador: cerca de 95 h até o 20. Recalibrar depois das quests estruturadas (passo 3 do D-025).
- **Horas conectadas até o nível 30 (mediana):** Referência do criador: cerca de 200 h até o teto. Recalibrar depois das quests estruturadas (passo 3 do D-025).
- **Casuais no nível 10 ou mais ao fim:** Em 90 dias um casual joga cerca de 50 h; a maioria deve passar do 10, sem ser regra para todos.
- **XP vindo de quests:** D-025: quests guiam e recompensam, mas evoluir não pode depender delas nem das diárias; a caça e o mundo continuam sendo a maior parte.
- **Quests concluídas por personagem (média):** Em 90 dias, um personagem típico deve concluir várias quests (principais, secundárias e algumas diárias).
- **Sessões por semana — casual:** O perfil casual está configurado com média de 3 dias por semana; a rotina simulada deve respeitar isso.
- **Sessões por semana — hardcore:** O perfil hardcore está configurado com média de 6 dias por semana.
- **Horas por semana — casual:** Cerca de 3 sessões de 70 minutos, um pouco mais no fim de semana.
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

| Perfil | Sessões/semana | Horas/semana |
|---|---:|---:|
| Casual | 4,2 | 6,8 |
| Comerciante | 7,6 | 14,6 |
| Competitivo | 7,6 | 20,9 |
| Explorador | 6,9 | 15,8 |
| Hardcore | 9,5 | 36,8 |
| Social | 5,8 | 12,4 |

## Perfis na campanha completa (média das seeds)

| Perfil | Pessoas | Sessões/semana | Horas/semana | Nível mediano no fim | Nível p90 | Sessões em dia útil iniciadas entre 18h e 23h |
|---|---:|---:|---:|---:|---:|---:|
| Casual | 353 | 4,5 | 7,2 | 20,2 | 25,8 | 52,1% |
| Comerciante | 99,4 | 7,7 | 14,6 | 16,4 | 22,0 | 50,8% |
| Competitivo | 149 | 7,8 | 21,7 | 30,0 | 30,0 | 53,5% |
| Explorador | 99,4 | 7,7 | 17,7 | 29,4 | 30,0 | 46,2% |
| Hardcore | 98,0 | 9,5 | 37,1 | 30,0 | 30,0 | 51,7% |
| Social | 202 | 6,1 | 13,1 | 12,8 | 16,8 | 52,9% |

## Evolução dos atributos do jogador (média das seeds)

Variação média desde o início, em **pontos da escala 1–20** (1 ponto ≈ 52,6 unidades do Core), ao fim de 90 dias. Valores de `population.json` (`dynamics`).

| Perfil | Mecânica | Conhecimento | PvE | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | +0,14 | +1,31 | +0,21 | +0,11 | +0,01 | 0,00 | 14,2 | 3,2 |
| Comerciante | +0,03 | +0,99 | +0,10 | +0,06 | +0,33 | 0,00 | 22,5 | 8,0 |
| Competitivo | +0,85 | +1,26 | +0,30 | +0,06 | +0,03 | -0,25 | 19,6 | 15,1 |
| Explorador | +0,30 | +1,45 | +0,17 | +0,42 | +0,02 | -0,05 | 21,6 | 7,8 |
| Hardcore | +1,05 | +0,97 | +0,86 | +0,05 | 0,00 | -0,10 | 36,2 | 4,5 |
| Social | +0,03 | +1,09 | +0,07 | +0,04 | 0,00 | 0,00 | 18,3 | 8,0 |

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
| Aposentado | 20,4 | 9,0 | 22,3 | — | 15,3 h | 0,0% | R$ 3.427 | R$ 36,9 |
| Autônomo / freelancer | 43,4 | 8,5 | 18,5 | 8,9 h | 15,1 h | 0,0% | R$ 4.323 | R$ 53,7 |
| Trabalho em horário comercial | 231 | 5,3 | 13,0 | 4,2 h | 14,8 h | 0,0% | R$ 4.410 | R$ 29,9 |
| Cuidando da casa e da família | 30,4 | 7,6 | 15,7 | 8,4 h | — | 0,0% | R$ 0,0 | R$ 2.320 |
| Comércio e serviços em escala 5x2 | 84,0 | 4,9 | 11,7 | 3,3 h | 14,9 h | 0,0% | R$ 2.605 | R$ 31,2 |
| Comércio em escala 6x1 | 37,8 | 4,6 | 10,5 | 4,8 h | 15,0 h | 0,0% | R$ 2.706 | R$ 27,3 |
| Estudante (manhã) | 106 | 7,3 | 17,9 | 8,4 h | 14,7 h | 0,0% | R$ 0,0 | R$ 730 |
| Estudante (tarde) | 44,0 | 7,5 | 17,0 | 8,7 h | 15,2 h | 0,0% | R$ 0,0 | R$ 737 |
| Home office (tecnologia) | 81,8 | 7,1 | 15,0 | 6,4 h | 15,1 h | 0,0% | R$ 8.195 | R$ 17,0 |
| Meio período (manhã) | 30,4 | 7,9 | 19,4 | 9,5 h | 14,8 h | 0,0% | R$ 1.961 | R$ 291 |
| Meio período (tarde) | 26,0 | 7,9 | 18,1 | 9,3 h | 15,1 h | 0,0% | R$ 1.948 | R$ 282 |
| Sem ocupação | 76,2 | 9,6 | 23,9 | — | 15,3 h | 0,0% | R$ 0,0 | R$ 839 |
| Trabalha de dia e estuda à noite | 56,8 | 2,5 | 6,3 | 1,0 h | 12,9 h | 53,1% | R$ 3.374 | R$ 53,2 |
| Turno noturno | 32,6 | 5,7 | 14,2 | 7,1 h | 11,4 h | 0,0% | R$ 3.601 | R$ 19,7 |
| Universitário (diurno) | 99,2 | 6,5 | 15,5 | 7,1 h | 14,8 h | 0,0% | R$ 526 | R$ 1.376 |

Como terminaram os dias da agenda:

| Dia | Parcela |
|---|---:|
| Jogou | 55,8% |
| Tinha tempo, preferiu outro lazer | 39,8% |
| Sem tempo livre | 3,0% |
| Inativo | 1,0% |
| Cansado (dormiu pouco) | 0,4% |
| Sem vontade (engajamento baixo) | 0,0% |

## Sono e dificuldade de parar (média das seeds, campanha completa)

Importância do jogo, tendência a prolongar e autocontrole são traços separados. A extensão tem limites explícitos (vezes por sessão, minutos, sono mínimo e teto do déficit); só corta sono no fim do dia, nunca uma obrigação. Sono cortado vira déficit; manhã livre e noites normais recuperam.

- Extensões na janela: 402 (131 gostando da sessão, 271 perto de concluir a meta); 402 avançaram sobre a hora de dormir.
- Noites encurtadas pelo jogo: 366, 66,6 min a menos em média.

| Perfil | Extensões/semana | Sono cortado/semana | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|
| Casual | 0,0 | 0,1 min | 0,2 h | 2,0% |
| Comerciante | 0,0 | 0,5 min | 0,4 h | 2,9% |
| Competitivo | 0,0 | 2,4 min | 0,3 h | 2,2% |
| Explorador | 0,0 | 1,3 min | 0,2 h | 1,8% |
| Hardcore | 0,2 | 13,5 min | 0,5 h | 3,4% |
| Social | 0,0 | 0,8 min | 0,2 h | 1,9% |

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
| Meio período (tarde) | 7,9 h | 7,8 h | 7,9 h | 0,0 h | 0,0% |
| Sem ocupação | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Trabalha de dia e estuda à noite | 7,9 h | 6,9 h | 9,7 h | 4,6 h | 38,8% |
| Turno noturno | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Universitário (diurno) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |

## Jogar no horário de trabalho (média das seeds)

Só no trabalho remoto. Pausa: jogar no intervalo permitido. Escapada: jogar no expediente, rara, pela flexibilidade do trabalho, importância do jogo, falta de autocontrole e folga do dia; sessão curta e interrompível. Deslocamento, trabalho presencial e estudo ficam protegidos. Sem consequência profissional por enquanto.

- Sessões na pausa: 1.126 (1,5% das sessões); escapadas: 227 (0,3%).
- Chamados do trabalho durante a escapada: 79,0 (8,8 terminaram a atividade, 70,2 largaram e saíram).

| Ocupação | Pausa/semana | Escapadas/semana | Dias de trabalho com escapada |
|---|---:|---:|---:|
| Autônomo / freelancer | 0,8 | 0,2 | 3,1% |
| Home office (tecnologia) | 0,7 | 0,1 | 2,8% |

## Distribuições (R3)

Medição da campanha completa (com humor), seeds somadas, na janela de medição. A referência antes da R4 (modelo de vontade) está em `docs/baselines/pre-R4/`, com o mesmo relatório e o cenário controlado (pessoas iguais por célula perfil × ocupação), que separa o efeito das regras da mistura da população.

**Duração das sessões (minutos, sessões normais).** Inclui extensões e o fim real; pausa e escapada do trabalho à parte.

| Perfil | Sessões | p10 | p25 | Mediana | p75 | p90 | Máx. | Média |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 91.441 | 64,0 | 77,0 | 93,0 | 114 | 139 | 445 | 98,1 |
| Comerciante | 44.293 | 72,0 | 89,0 | 109 | 136 | 171 | 444 | 116 |
| Competitivo | 67.445 | 99,0 | 136 | 168 | 202 | 239 | 599 | 169 |
| Explorador | 44.360 | 90,0 | 113 | 135 | 164 | 199 | 481 | 140 |
| Hardcore | 54.367 | 114 | 195 | 241 | 286 | 338 | 745 | 237 |
| Social | 71.158 | 83,0 | 103 | 126 | 153 | 186 | 501 | 130 |
| Pausa no trabalho | 5.631 | 36,0 | 43,0 | 54,0 | 55,0 | 55,0 | 55,0 | 48,5 |
| Escapada no expediente | 1.134 | 10,0 | 18,0 | 23,0 | 26,0 | 29,0 | 34,0 | 21,3 |

**Horas por semana, perfil × ocupação** (por pessoa registrada; "—" com menos de 10 pessoas na soma das seeds).

| Ocupação | Casual | Comerciante | Competitivo | Explorador | Hardcore | Social |
|---|---:|---:|---:|---:|---:|---:|
| Aposentado | 12,2 | 21,9 | 33,4 | 25,3 | — | 20,9 |
| Autônomo / freelancer | 8,7 | 18,6 | 26,0 | 21,7 | 45,1 | 16,1 |
| Trabalho em horário comercial | 5,7 | 12,0 | 18,5 | 14,8 | 33,0 | 10,8 |
| Cuidando da casa e da família | 6,1 | 12,8 | 19,8 | 16,5 | 36,6 | 12,0 |
| Comércio e serviços em escala 5x2 | 5,1 | 10,9 | 17,5 | 12,9 | 29,0 | 9,8 |
| Comércio em escala 6x1 | 4,6 | 10,7 | 15,2 | 11,4 | 28,0 | 8,2 |
| Estudante (manhã) | 8,7 | 17,4 | 26,4 | 20,7 | 44,2 | 15,5 |
| Estudante (tarde) | 8,2 | 15,6 | 23,6 | 19,3 | 44,5 | 13,8 |
| Home office (tecnologia) | 7,2 | 15,2 | 22,4 | 16,2 | 37,8 | 13,5 |
| Meio período (manhã) | 9,6 | 19,1 | 28,3 | 22,7 | 46,7 | 17,0 |
| Meio período (tarde) | 9,1 | 16,9 | 25,0 | 20,9 | 44,5 | 15,2 |
| Sem ocupação | 12,4 | 21,9 | 31,0 | 28,5 | 51,7 | 21,3 |
| Trabalha de dia e estuda à noite | 2,9 | 5,8 | 8,9 | 7,2 | 15,2 | 5,5 |
| Turno noturno | 6,0 | 13,2 | 19,6 | 14,3 | 35,0 | 11,1 |
| Universitário (diurno) | 7,6 | 15,4 | 23,5 | 18,0 | 40,0 | 13,9 |

**Sessões por dia de jogo:** 57,1% com uma, 29,8% com duas, 13,1% com três ou mais. Intervalo entre sessões do mesmo dia: mediana 80,0 min, p90 180 min.
**Voltas depois de uma pausa (v3):** em 40,9% dos dias com jogo. Sessões normais por dia com jogo: uma 57,4%, duas 29,9%, três ou mais 12,6%. Voltas consideradas e recusadas: 38.749. Sessão da noite deixada de lado depois de jogar no trabalho: 49 dias.

**Por que as sessões começaram e terminaram** (sessões normais, seeds somadas).

| Início | Parcela | | Fim | Parcela |
|---|---:|---|---|---:|
| Antes do horário, por vontade | 45,4% | | Decidiu parar (saciedade, cansaço, outras opções) | 78,4% |
| Voltou depois de uma pausa | 36,8% | | Hora de dormir | 16,2% |
| No horário de costume | 17,8% | | Fim do tempo livre (obrigação, preparo, compromisso) | 4,6% |
|  |  | | Depois de ficar acordado jogando | 0,5% |
|  |  | | Fim planejado (sem decisão) | 0,2% |
|  |  | | Frustração | 0,0% |

**Sessões mais longas e por quê** (as 8 maiores sessões normais, seeds somadas). Resto: terminar a atividade ou a viagem.

| Perfil | Ocupação | Duração | Ao começar | Continuou | Ficou acordado | Resto | Disposição | Tempo livre do dia |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Hardcore | Comércio e serviços em escala 5x2 | 745 min | 225 min | 510 min | 0 min | 10 min | +1% | 15,1 h |
| Hardcore | Sem ocupação | 708 min | 308 min | 400 min | 0 min | 0 min | +12% | 15,3 h |
| Hardcore | Turno noturno | 699 min | 246 min | 450 min | 0 min | 3 min | +0% | 15,8 h |
| Hardcore | Sem ocupação | 693 min | 240 min | 450 min | 0 min | 3 min | +11% | 15,6 h |
| Hardcore | Trabalho em horário comercial | 692 min | 262 min | 420 min | 0 min | 10 min | +8% | 14,2 h |
| Hardcore | Trabalho em horário comercial | 688 min | 294 min | 390 min | 0 min | 4 min | +6% | 15,6 h |
| Hardcore | Trabalha de dia e estuda à noite | 671 min | 275 min | 387 min | 0 min | 9 min | −0% | 13,5 h |
| Hardcore | Autônomo / freelancer | 665 min | 289 min | 196 min | 180 min | 0 min | +4% | 8,8 h |


**Quanto do tempo livre vira jogo** (minutos jogados ÷ minutos livres da agenda; o jogo no trabalho e as extensões entram no jogado).

| Perfil | Uso |
|---|---:|
| Casual | 10,3% |
| Comerciante | 21,2% |
| Competitivo | 31,6% |
| Explorador | 24,9% |
| Hardcore | 53,4% |
| Social | 18,9% |

| Ocupação | Uso |
|---|---:|
| Aposentado | 19,6% |
| Autônomo / freelancer | 22,8% |
| Trabalho em horário comercial | 22,8% |
| Cuidando da casa e da família | 24,6% |
| Comércio e serviços em escala 5x2 | 23,0% |
| Comércio em escala 6x1 | 22,8% |
| Estudante (manhã) | 22,6% |
| Estudante (tarde) | 21,3% |
| Home office (tecnologia) | 21,7% |
| Meio período (manhã) | 23,0% |
| Meio período (tarde) | 21,6% |
| Sem ocupação | 20,5% |
| Trabalha de dia e estuda à noite | 16,4% |
| Turno noturno | 22,4% |
| Universitário (diurno) | 21,6% |

**Composição do tempo jogado:** 902.662 h no total; extensões 0,2%; pausa e escapada do trabalho 0,5%; sono cortado pelo jogo 2.033 h (0,2% do tempo jogado).

**Dias sem sessão principal** (44,2% dos dias da agenda):

| Motivo | Parcela dos dias sem sessão |
|---|---:|
| Sem oportunidade (nenhuma janela comporta uma sessão) | 6,8% |
| Escolha: tinha tempo, preferiu outro lazer | 88,5% |
| Cansaço | 0,9% |
| Desânimo (engajamento baixo) | 0,0% |
| Inativo | 2,2% |
| Jogou só no horário de trabalho | 1,6% |

**Efeito do tempo livre, separado** (dias da agenda por faixa de tempo livre): a frequência (chance de jogar) e a duração (minutos planejados quando joga) medidas à parte.

| Tempo livre no dia | Dias | Jogou | Duração planejada quando joga | Jogado por dia |
|---|---:|---:|---:|---:|
| até 2 h | 4.529 | 0,0% | 103 min | 0,1 min |
| 2–4 h | 20.159 | 43,5% | 126 min | 64,5 min |
| 4–6 h | 24.438 | 44,2% | 131 min | 75,2 min |
| 6–10 h | 42.862 | 55,2% | 119 min | 124 min |
| 10 h ou mais | 58.012 | 69,9% | 123 min | 172 min |

**Corte de sono (observações do criador, R2/R3).**

- **Onde pode cortar:** só a janela que termina **por causa do sono** avança sobre ele. Janela que termina por obrigação, preparo, deslocamento ou compromisso não avança (teste).
- **Teto compartilhado:** os 180 minutos por período de sono valem para a soma de todas as sessões que afetam aquele período (teste).
- **Medido:** 1.831 períodos de sono encurtados pelo jogo; corte por período: mediana 57,0 min, p90 120 min, máximo 180 min.

## Localidades (média das seeds)

Cada pessoa joga no horário e nos feriados de onde mora; o relógio do jogo é o do servidor. O início médio é uma média circular das horas (a meia-noite não distorce), então local e servidor diferem exatamente pelo fuso.

| Localidade | Fuso | Pessoas | Estrangeiros | Sessões | Início médio (local) | Início médio (servidor) |
|---|---|---:|---:|---:|---:|---:|
| Argentina | UTC−3 | 85,4 | 2,2 | 6.654 | 16h29 | 16h29 |
| Brasil | UTC−3 | 758 | 19,0 | 57.927 | 16h34 | 16h34 |
| China | UTC+8 | 75,4 | 0,8 | 5.634 | 16h41 | 05h41 |
| Estados Unidos | UTC−5 | 81,4 | 12,6 | 5.795 | 16h44 | 18h44 |

## Retenção (média das seeds, no fim)

| Perfil | Desistiram | Inativos | Voltaram alguma vez |
|---|---:|---:|---:|
| Casual | 1,5% | 2,2% | 5,6% |
| Comerciante | 0,2% | 0,6% | 0,8% |
| Competitivo | 0,7% | 0,3% | 0,7% |
| Explorador | 0,2% | 1,4% | 0,8% |
| Hardcore | 0,0% | 0,0% | 0,4% |
| Social | 0,5% | 1,1% | 1,4% |

Desistências: dia mediano 68,0, nível mediano 9,0. Sessões estendidas por estarem indo bem: 0,5%; encerradas por frustração: 0,0%.

Motivo principal das desistências (o que mais incomodava; "long_inactivity/" = sumiu por muito tempo):

| Motivo | Desistências |
|---|---:|
| long_inactivity/long_inactivity | 79,5% |
| long_inactivity/boredom | 12,8% |
| boredom | 5,1% |
| long_inactivity/fatigue | 2,6% |

## Humor e memória (média das seeds, no fim)

Satisfação, frustração, novidade e engajamento em permille (0–1000); progresso em permille do XP de referência do nível.

| Perfil | Satisfação | Insatisfeitos (< 400) | Frustração | Novidade | Engajamento | Intenção de continuar | Expectativa | Progresso da última sessão | Memórias |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 607 | 0,0% | 4,2 | 990 | 723 | 654 | 620 | 609 | 10,7 |
| Comerciante | 564 | 0,2% | 9,6 | 939 | 687 | 598 | 456 | 168 | 9,9 |
| Competitivo | 581 | 0,1% | 10,7 | 894 | 694 | 625 | 723 | 525 | 10,0 |
| Explorador | 652 | 0,0% | 2,3 | 944 | 750 | 702 | 590 | 496 | 10,1 |
| Hardcore | 565 | 0,0% | 0,0 | 570 | 652 | 654 | 878 | 589 | 6,8 |
| Social | 600 | 0,0% | 9,9 | 891 | 708 | 685 | 453 | 44,0 | 8,5 |

Sessões ótimas: 9,9% · ruins: 0,0% (após a semana de lançamento).

O que mais incomoda cada pessoa (motivo mais negativo acumulado):

| Motivo | Pessoas |
|---|---:|
| boredom | 98,1% |
| fatigue | 0,9% |
| nenhum | 0,8% |
| progress | 0,2% |

## Uso do tempo conectado (todas as seeds)

Caça, chefe, patrulha e emboscada ainda são atividades abstratas (tempo e XP, sem luta simulada) até o M3; aí o tempo se divide em viagem, procura, luta, recuperação e recompensa.

| Tipo | Tempo | Escolhas |
|---|---:|---:|
| Caça | 30,5% | 30,5% |
| Deslocamento | 20,8% | — |
| Socializar | 15,5% | 14,7% |
| Exploração | 7,4% | 7,3% |
| Comércio | 7,1% | 13,4% |
| Coleta | 5,4% | 7,8% |
| Patrulha | 5,4% | 5,2% |
| Emboscada | 4,6% | 6,5% |
| Missão | 2,2% | 10,3% |
| Treino | 1,2% | 3,8% |
| Conserto | 0,1% | 0,4% |

## Continuidade das sessões (média das seeds)

| Medida | Valor |
|---|---:|
| Escolhas por sessão | 5,1 |
| Continuou na mesma atividade (% das escolhas após a primeira) | 75,2% |
| Trocas de atividade por hora conectada | 0,4 |
| Trocas de zona por sessão | 0,4 |
| Ciclos seguidos na mesma atividade (média) | 2,5 |

Motivos das trocas de plano (fator principal):

| Motivo | Trocas |
|---|---:|
| quest_step | 26,4% |
| quest_completed | 21,3% |
| time_budget | 11,1% |
| boredom | 10,4% |
| focus_unavailable | 9,0% |
| level_up | 8,7% |
| goal_completed | 5,8% |
| affinity | 3,0% |
| zone_discovered | 1,2% |
| quest_deliver | 0,8% |
| nothing_new | 0,7% |
| goal | 0,7% |
| quest_offer | 0,5% |
| level_fit | 0,2% |
| company | 0,0% |
| crowding | 0,0% |
| challenge | 0,0% |
| risk | 0,0% |
| memory | 0,0% |
| familiarity | 0,0% |

## Atividades pouco usadas (menos de 1% das escolhas)

| Zona | Atividade | Tipo | Escolhas | Diagnóstico |
|---|---|---|---:|---|
| Cidadela de Cinzaforte | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Covil do Rei Goblin | Enfrentar o Rei Goblin | `boss` | 0,0% | exige grupo de 5; grupos chegam no M6, então hoje ninguém pode fazer. |
| Fortaleza Sombria | Enfrentar o Senhor das Cinzas | `boss` | 0,0% | exige grupo de 10; grupos chegam no M6, então hoje ninguém pode fazer; nível mínimo 28: só 32% dos personagens chegaram até o fim. |
| Forte do Vau | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Observar a forja vulcânica | `quest` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Ruínas Sombrias | Coletar cristais sombrios | `gather` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Consertar equipamento | `repair` | 0,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Explorar trilhas da floresta | `explore` | 0,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Consertar equipamento | `repair` | 0,1% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Fortaleza Sombria | Ler as inscrições da cripta | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Investigar o covil do yeti | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Patrulhar as ruínas | `patrol` | 0,1% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Caçar mortos-vivos | `hunt` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Planalto dos Ventos | Espiar o acampamento orc | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Explorar galerias profundas | `explore` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Explorar as terras queimadas | `explore` | 0,2% | nível mínimo 22: só 48% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Caçar aranhas tecelãs | `hunt` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Examinar o altar profanado | `quest` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Terras Queimadas | Minerar obsidiana | `gather` | 0,2% | nível mínimo 23: só 45% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mata dos Espinhos | Seguir os rastros dos trolls | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Cidadela de Cinzaforte | Consertar equipamento | `repair` | 0,3% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Ravina Escarlate | Procurar o estandarte perdido | `quest` | 0,3% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Brejo das Brumas | Procurar o ninho das serpentes | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Investigar as galerias abandonadas | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Examinar os ninhos das harpias | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Falar com o lenhador | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Caçar lobos das colinas | `hunt` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Colher ervas das encostas | `gather` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Coletar cristais de gelo | `gather` | 0,6% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Socializar na taverna | `socialize` | 0,6% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Coletar ervas curativas | `gather` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Patrulhar a ravina | `patrol` | 0,7% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ravina Escarlate | Emboscar viajantes | `ambush` | 0,7% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Minerar ferro e prata | `gather` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar javalis | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Caçar esqueletos da ravina | `hunt` | 0,8% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Fortaleza Sombria | Explorar a fortaleza | `explore` | 0,8% | nível mínimo 25: só 39% dos personagens chegaram até o fim. |
| Mina Velha | Caçar morcegos gigantes | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar lobos | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Caçar ursos pardos | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Caçar salamandras de fogo | `hunt` | 0,9% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Forte do Vau | Treinar com o mestre de armas | `train` | 0,9% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Enfrentar golens de magma | `hunt` | 0,9% | nível mínimo 24: só 42% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Caçar goblins | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Purgar a cripta | `hunt` | 1,0% | nível mínimo 26: só 36% dos personagens chegaram até o fim. |
| Colinas do Vale | Explorar as colinas | `explore` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Brejo das Brumas | Explorar o brejo | `explore` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |

## Execuções

| Seed | Hash final | Simulação |
|---:|---|---:|
| 1 | `c751fa4cd6c91c0e` | 286,1 s |
| 2 | `83aa3a0089891aec` | 285,0 s |
| 3 | `33a99a6fb72686ab` | 295,7 s |
| 4 | `c9b58fe7055409f4` | 291,1 s |
| 5 | `6fac8936f1ccd5a8` | 281,9 s |

Medições de tempo desta máquina, não benchmark.
