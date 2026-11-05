# Validação da simulação

> Gerado pelo `Simulation.Runner` com várias seeds. As faixas vêm de `targets.json` e são **hipóteses de design ajustáveis**, não regras do mundo. Uma meta só conta como "dentro" quando todas as seeds caem na faixa.

| Campo | Valor |
|---|---|
| Conteúdo | `Content/presets/inicial` (`bbf1b1fb730d219c`) |
| Metas | `Content/presets/inicial/targets.json` (`34fe840d48d60e76`) |
| Simulador | 0.1.0+71ba584217d0429bbd68423cb9b2cc263928ae79 |
| Execuções | 5 seeds (1, 2, 3, 4, 5) · 1.000 pessoas · 90 dias |
| Janela das medidas semanais | a partir do dia 7 (exclui a semana de lançamento) |
| Tempo total | 386,9 s |

## Metas

As metas têm três classes (D-039a); o total "x de y" sozinho não diz se dá para avançar:

- **Regras do cenário:** rotina calibrada, medida numa rodada controlada com o humor neutralizado. Falhar aqui indica erro.
- **Provisórias:** balanceamento que será recalibrado (progressão, divisão do tempo, missões).
- **Dependem de sistemas futuros:** só acompanhamento, não reprovam (retenção, missões e coleta dependem de combate, economia e social).

### Regras do cenário: 9 de 9 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Dias de jogo por semana — casual | 2,0–4,0  | 2,8 | 2,8–2,8 |  |
| ✅ dentro | Dias de jogo por semana — hardcore | 5,0–7,0  | 6,2 | 6,0–6,4 |  |
| ✅ dentro | Sessões por dia de jogo — casual | 1,1–2,0  | 1,5 | 1,5–1,5 |  |
| ✅ dentro | Sessões por dia de jogo — hardcore | 1,2–2,5  | 1,5 | 1,5–1,6 |  |
| ✅ dentro | Horas por semana — casual | 3,0–9,0 h | 6,7 | 6,6–6,8 |  |
| ✅ dentro | Horas por semana — hardcore | 15,0–45,0 h | 36,5 | 35,6–37,7 |  |
| ✅ dentro | Jogadores ativos em dia útil / cadastrados | 30,0–55,0 % | 51,4 | 50,2–52,4 | 58 dias |
| ✅ dentro | Ativos no fim de semana ÷ ativos em dia útil | 1,3–2,0 × | 1,3 | 1,3–1,4 |  |
| ✅ dentro | Pico online do dia / cadastrados | 15,0–35,0 % | 23,9 | 22,7–24,4 |  |

### Provisórias: 9 de 12 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Horas conectadas até o nível 5 (mediana) | 1,0–4,0 h | 2,7 | 2,7–2,8 | 100% chegaram |
| ✅ dentro | Horas conectadas até o nível 10 (mediana) | 8,0–20,0 h | 15,5 | 15,2–15,7 | 98–99% chegaram |
| ✅ dentro | Horas conectadas até o nível 15 (mediana) | 22,0–50,0 h | 36,8 | 36,2–37,2 | 78–81% chegaram |
| ❌ abaixo | Horas conectadas até o nível 20 (mediana) | 80,0–110 h | 69,7 | 68,6–70,9 | 57–61% chegaram |
| ✅ dentro | Horas conectadas até o nível 30 (mediana) | 170–230 h | 175 | 174–176 | 26–29% chegaram (mediana só de quem chegou: subestima o tempo real) |
| ❌ acima | Casuais no nível 10 ou mais ao fim | 60,0–95,0 % | 98,9 | 98,3–99,7 |  |
| ✅ dentro | XP vindo de quests | 15,0–35,0 % | 20,4 | 19,5–21,2 |  |
| ✅ dentro | Quests concluídas por personagem (média) | 4,0–30,0  | 27,0 | 26,3–27,8 |  |
| ✅ dentro | Tempo em atividades de combate (abstratas: caça, chefe, patrulha, emboscada; sem luta simulada até o M3) | 35,0–55,0 % | 40,4 | 39,9–41,1 |  |
| ✅ dentro | Tempo explorando | 5,0–15,0 % | 7,4 | 6,9–7,7 |  |
| ✅ dentro | Tempo na cidade (comércio, taverna, treino, conserto) | 10,0–25,0 % | 23,7 | 23,3–24,0 |  |
| ❌ acima | Tempo andando ou esperando | 5,0–20,0 % | 21,0 | 20,8–21,2 |  |

### Dependem de sistemas futuros (acompanhamento): 1 de 5 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ❌ abaixo | Tempo em missões | 10,0–25,0 % | 2,2 | 2,2–2,3 |  |
| ❌ abaixo | Tempo em coleta | 8,0–20,0 % | 5,4 | 4,9–5,6 |  |
| ❌ abaixo | Desistentes no fim / cadastrados | 8,0–30,0 % | 0,8 | 0,4–1,2 |  |
| ❌ abaixo | Inativos no fim / cadastrados | 2,0–15,0 % | 1,2 | 0,7–1,7 |  |
| ✅ dentro | Quem saiu e voltou (inativos e desistentes) | 20,0–70,0 % | 60,2 | 54,8–69,4 | 51 saíram; 49 saíram; 41 saíram; 36 saíram; 42 saíram |


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
- **Horas por semana — casual:** Cerca de 3 dias de jogo; no dia livre, 1 ou 2 sessões de 1h30 com pausa (D-051, comportamento aprovado pelo criador em 05/11/2026). A faixa antiga (2–6 h, '3 sessões de 70 minutos') supunha uma sessão por dia (R5, D-052).
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
| Comerciante | 4,4 | 1,72 | 7,5 | 14,3 |
| Competitivo | 4,8 | 1,57 | 7,5 | 20,7 |
| Explorador | 4,1 | 1,67 | 6,8 | 15,6 |
| Hardcore | 6,2 | 1,52 | 9,4 | 36,5 |
| Social | 3,7 | 1,56 | 5,7 | 12,2 |

## Perfis na campanha completa (média das seeds)

| Perfil | Pessoas | Dias de jogo/semana | Sessões por dia de jogo | Horas/semana | Nível mediano no fim | Nível p90 | Sessões em dia útil iniciadas entre 18h e 23h |
|---|---:|---:|---:|---:|---:|---:|---:|
| Casual | 353 | 2,9 | 1,51 | 7,1 | 20,0 | 25,6 | 52,1% |
| Comerciante | 99,4 | 4,4 | 1,71 | 14,4 | 16,8 | 21,8 | 50,5% |
| Competitivo | 149 | 4,9 | 1,58 | 21,5 | 30,0 | 30,0 | 53,2% |
| Explorador | 99,4 | 4,5 | 1,70 | 17,5 | 29,4 | 30,0 | 46,1% |
| Hardcore | 98,0 | 6,3 | 1,52 | 36,9 | 30,0 | 30,0 | 51,8% |
| Social | 202 | 3,8 | 1,57 | 12,9 | 13,0 | 16,6 | 52,8% |

## Evolução dos atributos do jogador (média das seeds)

Variação média desde o início, em **pontos da escala 1–20** (1 ponto ≈ 52,6 unidades do Core), ao fim de 90 dias. Valores de `population.json` (`dynamics`).

| Perfil | Mecânica | Conhecimento | PvE | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | +0,13 | +1,30 | +0,20 | +0,11 | +0,01 | 0,00 | 13,9 | 3,1 |
| Comerciante | +0,03 | +0,99 | +0,10 | +0,06 | +0,32 | 0,00 | 22,1 | 7,6 |
| Competitivo | +0,84 | +1,25 | +0,30 | +0,06 | +0,03 | -0,24 | 19,8 | 14,6 |
| Explorador | +0,30 | +1,44 | +0,17 | +0,42 | +0,02 | -0,05 | 21,3 | 7,8 |
| Hardcore | +1,04 | +0,97 | +0,85 | +0,05 | 0,00 | -0,10 | 36,1 | 4,4 |
| Social | +0,03 | +1,08 | +0,07 | +0,04 | 0,00 | 0,00 | 18,2 | 7,8 |

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
| Aposentado | 20,4 | 8,9 | 21,9 | — | 15,3 h | 0,0% | R$ 3.427 | R$ 36,9 |
| Autônomo / freelancer | 43,4 | 8,5 | 18,4 | 8,9 h | 15,1 h | 0,0% | R$ 4.323 | R$ 53,7 |
| Trabalho em horário comercial | 231 | 5,2 | 12,8 | 4,2 h | 14,8 h | 0,0% | R$ 4.410 | R$ 29,9 |
| Cuidando da casa e da família | 30,4 | 7,6 | 15,6 | 8,4 h | — | 0,0% | R$ 0,0 | R$ 2.320 |
| Comércio e serviços em escala 5x2 | 84,0 | 4,8 | 11,6 | 3,3 h | 14,9 h | 0,0% | R$ 2.605 | R$ 31,2 |
| Comércio em escala 6x1 | 37,8 | 4,6 | 10,5 | 4,8 h | 15,0 h | 0,0% | R$ 2.706 | R$ 27,3 |
| Estudante (manhã) | 106 | 7,2 | 17,6 | 8,4 h | 14,7 h | 0,0% | R$ 0,0 | R$ 730 |
| Estudante (tarde) | 44,0 | 7,5 | 17,0 | 8,7 h | 15,2 h | 0,0% | R$ 0,0 | R$ 737 |
| Home office (tecnologia) | 81,8 | 7,0 | 14,8 | 6,4 h | 15,1 h | 0,0% | R$ 8.195 | R$ 17,0 |
| Meio período (manhã) | 30,4 | 7,8 | 19,0 | 9,5 h | 14,9 h | 0,0% | R$ 1.961 | R$ 291 |
| Meio período (tarde) | 26,0 | 7,9 | 18,2 | 9,3 h | 15,1 h | 0,0% | R$ 1.948 | R$ 282 |
| Sem ocupação | 76,2 | 9,4 | 23,4 | — | 15,3 h | 0,0% | R$ 0,0 | R$ 839 |
| Trabalha de dia e estuda à noite | 56,8 | 2,5 | 6,2 | 1,0 h | 12,9 h | 53,1% | R$ 3.374 | R$ 53,2 |
| Turno noturno | 32,6 | 5,7 | 14,1 | 7,1 h | 11,4 h | 0,0% | R$ 3.601 | R$ 19,7 |
| Universitário (diurno) | 99,2 | 6,4 | 15,3 | 7,1 h | 14,8 h | 0,0% | R$ 526 | R$ 1.376 |

Como terminaram os dias da agenda:

| Dia | Parcela |
|---|---:|
| Jogou | 55,8% |
| Tinha tempo, preferiu outro lazer | 39,8% |
| Sem tempo livre | 3,0% |
| Inativo | 0,9% |
| Cansado (dormiu pouco) | 0,4% |
| Sem vontade (engajamento baixo) | 0,0% |

## Sono e dificuldade de parar (média das seeds, campanha completa)

Importância do jogo, tendência a prolongar e autocontrole são traços separados. A extensão tem limites explícitos (vezes por sessão, minutos, sono mínimo e teto do déficit); só corta sono no fim do dia, nunca uma obrigação. Sono cortado vira déficit; manhã livre e noites normais recuperam.

- Extensões na janela: 414 (141 gostando da sessão, 273 perto de concluir a meta); 414 avançaram sobre a hora de dormir.
- Noites encurtadas pelo jogo: 369, 68,4 min a menos em média.

| Perfil | Extensões/semana | Sono cortado/semana | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|
| Casual | 0,0 | 0,1 min | 0,2 h | 2,0% |
| Comerciante | 0,0 | 0,4 min | 0,4 h | 2,9% |
| Competitivo | 0,0 | 2,7 min | 0,3 h | 2,0% |
| Explorador | 0,0 | 1,2 min | 0,2 h | 1,8% |
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
| Meio período (tarde) | 7,9 h | 7,8 h | 7,9 h | 0,0 h | 0,0% |
| Sem ocupação | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Trabalha de dia e estuda à noite | 7,9 h | 6,9 h | 9,7 h | 4,6 h | 38,8% |
| Turno noturno | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Universitário (diurno) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |

## Jogar no horário de trabalho (média das seeds)

Só no trabalho remoto. Pausa: jogar no intervalo permitido. Escapada: jogar no expediente, rara, pela flexibilidade do trabalho, importância do jogo, falta de autocontrole e folga do dia; sessão curta e interrompível. Deslocamento, trabalho presencial e estudo ficam protegidos. Sem consequência profissional por enquanto.

- Sessões na pausa: 1.126 (1,5% das sessões); escapadas: 226 (0,3%).
- Chamados do trabalho durante a escapada: 79,2 (8,4 terminaram a atividade, 70,8 largaram e saíram).

| Ocupação | Pausa/semana | Escapadas/semana | Dias de trabalho com escapada |
|---|---:|---:|---:|
| Autônomo / freelancer | 0,8 | 0,2 | 3,1% |
| Home office (tecnologia) | 0,7 | 0,1 | 2,8% |

## Distribuições (R3)

Medição da campanha completa (com humor), seeds somadas, na janela de medição. A referência antes da R4 (modelo de vontade) está em `docs/baselines/pre-R4/`, com o mesmo relatório e o cenário controlado (pessoas iguais por célula perfil × ocupação), que separa o efeito das regras da mistura da população.

**Duração das sessões (minutos, sessões normais).** Inclui extensões e o fim real; pausa e escapada do trabalho à parte.

| Perfil | Sessões | p10 | p25 | Mediana | p75 | p90 | Máx. | Média |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 90.554 | 64,0 | 77,0 | 92,0 | 113 | 138 | 342 | 97,4 |
| Comerciante | 43.907 | 73,0 | 89,0 | 108 | 135 | 168 | 417 | 115 |
| Competitivo | 67.084 | 99,0 | 136 | 167 | 200 | 237 | 596 | 168 |
| Explorador | 43.976 | 90,0 | 112 | 134 | 163 | 196 | 488 | 139 |
| Hardcore | 54.312 | 115 | 195 | 241 | 285 | 336 | 711 | 236 |
| Social | 70.541 | 83,0 | 103 | 126 | 152 | 184 | 529 | 130 |
| Pausa no trabalho | 5.628 | 36,0 | 43,0 | 54,0 | 55,0 | 55,0 | 55,0 | 48,5 |
| Escapada no expediente | 1.132 | 10,0 | 17,0 | 23,0 | 26,0 | 29,0 | 34,0 | 21,3 |

**Horas por semana, perfil × ocupação** (por pessoa registrada; "—" com menos de 10 pessoas na soma das seeds).

| Ocupação | Casual | Comerciante | Competitivo | Explorador | Hardcore | Social |
|---|---:|---:|---:|---:|---:|---:|
| Aposentado | 12,0 | 21,6 | 33,3 | 24,6 | — | 20,0 |
| Autônomo / freelancer | 8,6 | 18,6 | 26,0 | 21,6 | 44,9 | 16,0 |
| Trabalho em horário comercial | 5,7 | 11,7 | 18,3 | 14,6 | 32,8 | 10,6 |
| Cuidando da casa e da família | 6,1 | 12,9 | 19,5 | 16,3 | 36,7 | 11,8 |
| Comércio e serviços em escala 5x2 | 5,0 | 10,7 | 17,5 | 12,6 | 28,9 | 9,6 |
| Comércio em escala 6x1 | 4,5 | 10,5 | 15,4 | 11,2 | 28,2 | 8,2 |
| Estudante (manhã) | 8,5 | 16,9 | 26,0 | 20,3 | 43,9 | 15,2 |
| Estudante (tarde) | 8,1 | 15,7 | 23,3 | 19,3 | 44,3 | 14,0 |
| Home office (tecnologia) | 7,1 | 15,0 | 21,9 | 16,1 | 37,7 | 13,3 |
| Meio período (manhã) | 9,3 | 18,6 | 28,0 | 22,2 | 46,0 | 16,8 |
| Meio período (tarde) | 8,9 | 16,9 | 25,1 | 21,0 | 44,9 | 15,3 |
| Sem ocupação | 12,2 | 21,3 | 30,5 | 27,8 | 51,5 | 20,7 |
| Trabalha de dia e estuda à noite | 2,8 | 5,7 | 8,7 | 7,1 | 15,1 | 5,5 |
| Turno noturno | 6,0 | 13,2 | 19,5 | 14,4 | 34,6 | 11,0 |
| Universitário (diurno) | 7,4 | 15,2 | 23,3 | 17,8 | 39,8 | 13,7 |

**Sessões por dia de jogo:** 57,7% com uma, 29,6% com duas, 12,7% com três ou mais. Intervalo entre sessões do mesmo dia: mediana 80,4 min, p90 180 min.
**Voltas depois de uma pausa (v3):** em 40,4% dos dias com jogo. Sessões normais por dia com jogo: uma 58,0%, duas 29,7%, três ou mais 12,2%. Voltas consideradas e recusadas: 39.631. Sessão da noite deixada de lado depois de jogar no trabalho: 34 dias.

**Por que as sessões começaram e terminaram** (sessões normais, seeds somadas).

| Início | Parcela | | Fim | Parcela |
|---|---:|---|---|---:|
| Antes do horário, por vontade | 45,8% | | Decidiu parar (saciedade, cansaço, outras opções) | 78,7% |
| Voltou depois de uma pausa | 36,2% | | Hora de dormir | 15,9% |
| No horário de costume | 18,0% | | Fim do tempo livre (obrigação, preparo, compromisso) | 4,7% |
|  |  | | Depois de ficar acordado jogando | 0,5% |
|  |  | | Fim planejado (sem decisão) | 0,2% |

**Sessões mais longas e por quê** (as 8 maiores sessões normais, seeds somadas). Resto: terminar a atividade ou a viagem.

| Perfil | Ocupação | Duração | Ao começar | Continuou | Ficou acordado | Resto | Disposição | Tempo livre do dia |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Hardcore | Comércio e serviços em escala 5x2 | 711 min | 263 min | 442 min | 0 min | 6 min | +7% | 15,1 h |
| Hardcore | Universitário (diurno) | 697 min | 250 min | 267 min | 180 min | 0 min | +6% | 8,6 h |
| Hardcore | Sem ocupação | 686 min | 260 min | 420 min | 0 min | 6 min | −1% | 15,7 h |
| Hardcore | Universitário (diurno) | 675 min | 272 min | 390 min | 0 min | 13 min | +2% | 15,1 h |
| Hardcore | Home office (tecnologia) | 674 min | 282 min | 390 min | 0 min | 2 min | +12% | 15,7 h |
| Hardcore | Aposentado | 667 min | 289 min | 374 min | 0 min | 4 min | +4% | 15,6 h |
| Hardcore | Sem ocupação | 659 min | 290 min | 360 min | 0 min | 9 min | +3% | 15,2 h |
| Hardcore | Universitário (diurno) | 658 min | 290 min | 360 min | 0 min | 8 min | −0% | 16,0 h |


**Quanto do tempo livre vira jogo** (minutos jogados ÷ minutos livres da agenda; o jogo no trabalho e as extensões entram no jogado).

| Perfil | Uso |
|---|---:|
| Casual | 10,2% |
| Comerciante | 20,8% |
| Competitivo | 31,2% |
| Explorador | 24,5% |
| Hardcore | 53,2% |
| Social | 18,6% |

| Ocupação | Uso |
|---|---:|
| Aposentado | 19,0% |
| Autônomo / freelancer | 22,8% |
| Trabalho em horário comercial | 22,5% |
| Cuidando da casa e da família | 24,3% |
| Comércio e serviços em escala 5x2 | 22,9% |
| Comércio em escala 6x1 | 22,6% |
| Estudante (manhã) | 22,3% |
| Estudante (tarde) | 21,2% |
| Home office (tecnologia) | 21,4% |
| Meio período (manhã) | 22,5% |
| Meio período (tarde) | 21,7% |
| Sem ocupação | 20,1% |
| Trabalha de dia e estuda à noite | 16,3% |
| Turno noturno | 22,1% |
| Universitário (diurno) | 21,3% |

**Composição do tempo jogado:** 891.938 h no total; extensões 0,2%; pausa e escapada do trabalho 0,6%; sono cortado pelo jogo 2.102 h (0,2% do tempo jogado).

**Dias sem sessão principal** (44,2% dos dias da agenda):

| Motivo | Parcela dos dias sem sessão |
|---|---:|
| Sem oportunidade (nenhuma janela comporta uma sessão) | 6,8% |
| Escolha: tinha tempo, preferiu outro lazer | 88,7% |
| Cansaço | 0,9% |
| Desânimo (engajamento baixo) | 0,0% |
| Inativo | 2,0% |
| Jogou só no horário de trabalho | 1,6% |

**Efeito do tempo livre, separado** (dias da agenda por faixa de tempo livre): a frequência (chance de jogar) e a duração (minutos planejados quando joga) medidas à parte.

| Tempo livre no dia | Dias | Jogou | Duração planejada quando joga | Jogado por dia |
|---|---:|---:|---:|---:|
| até 2 h | 4.532 | 0,1% | 108 min | 0,2 min |
| 2–4 h | 20.162 | 43,4% | 126 min | 64,1 min |
| 4–6 h | 24.423 | 44,2% | 131 min | 74,7 min |
| 6–10 h | 42.866 | 55,2% | 119 min | 124 min |
| 10 h ou mais | 58.017 | 69,9% | 123 min | 169 min |

**Corte de sono (observações do criador, R2/R3).**

- **Onde pode cortar:** só a janela que termina **por causa do sono** avança sobre ele. Janela que termina por obrigação, preparo, deslocamento ou compromisso não avança (teste).
- **Teto compartilhado:** os 180 minutos por período de sono valem para a soma de todas as sessões que afetam aquele período (teste).
- **Medido:** 1.843 períodos de sono encurtados pelo jogo; corte por período: mediana 59,0 min, p90 123 min, máximo 180 min.

## Localidades (média das seeds)

Cada pessoa joga no horário e nos feriados de onde mora; o relógio do jogo é o do servidor. O início médio é uma média circular das horas (a meia-noite não distorce), então local e servidor diferem exatamente pelo fuso.

| Localidade | Fuso | Pessoas | Estrangeiros | Sessões | Início médio (local) | Início médio (servidor) |
|---|---|---:|---:|---:|---:|---:|
| Argentina | UTC−3 | 85,4 | 2,2 | 6.617 | 16h26 | 16h26 |
| Brasil | UTC−3 | 758 | 19,0 | 57.510 | 16h33 | 16h33 |
| China | UTC+8 | 75,4 | 0,8 | 5.591 | 16h40 | 05h40 |
| Estados Unidos | UTC−5 | 81,4 | 12,6 | 5.754 | 16h42 | 18h42 |

## Retenção (média das seeds, no fim)

| Perfil | Desistiram | Inativos | Voltaram alguma vez |
|---|---:|---:|---:|
| Casual | 1,5% | 2,1% | 5,4% |
| Comerciante | 0,2% | 0,6% | 0,8% |
| Competitivo | 0,7% | 0,4% | 0,8% |
| Explorador | 0,2% | 1,4% | 1,0% |
| Hardcore | 0,0% | 0,0% | 0,4% |
| Social | 0,4% | 1,2% | 1,8% |

Desistências: dia mediano 68,0, nível mediano 8,0. Sessões estendidas por estarem indo bem: 0,5%; encerradas por frustração: 0,0%.

Motivo principal das desistências (o que mais incomodava; "long_inactivity/" = sumiu por muito tempo):

| Motivo | Desistências |
|---|---:|
| long_inactivity/long_inactivity | 89,5% |
| long_inactivity/boredom | 5,3% |
| boredom | 2,6% |
| progress | 2,6% |

## Humor e memória (média das seeds, no fim)

Satisfação, frustração, novidade e engajamento em permille (0–1000); progresso em permille do XP de referência do nível.

| Perfil | Satisfação | Insatisfeitos (< 400) | Frustração | Novidade | Engajamento | Intenção de continuar | Expectativa | Progresso da última sessão | Memórias |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 608 | 0,0% | 5,3 | 991 | 723 | 654 | 618 | 593 | 10,7 |
| Comerciante | 563 | 0,0% | 10,1 | 938 | 687 | 597 | 454 | 154 | 10,0 |
| Competitivo | 581 | 0,3% | 7,0 | 899 | 695 | 627 | 721 | 524 | 10,0 |
| Explorador | 652 | 0,0% | 2,0 | 949 | 751 | 702 | 603 | 518 | 10,2 |
| Hardcore | 565 | 0,0% | 0,1 | 571 | 652 | 654 | 874 | 580 | 7,0 |
| Social | 602 | 0,0% | 10,0 | 893 | 709 | 687 | 454 | 51,0 | 8,6 |

Sessões ótimas: 10,0% · ruins: 0,0% (após a semana de lançamento).

O que mais incomoda cada pessoa (motivo mais negativo acumulado):

| Motivo | Pessoas |
|---|---:|
| boredom | 98,0% |
| fatigue | 0,9% |
| nenhum | 0,8% |
| progress | 0,3% |

## Uso do tempo conectado (todas as seeds)

Caça, chefe, patrulha e emboscada ainda são atividades abstratas (tempo e XP, sem luta simulada) até o M3; aí o tempo se divide em viagem, procura, luta, recuperação e recompensa.

| Tipo | Tempo | Escolhas |
|---|---:|---:|
| Caça | 30,6% | 30,7% |
| Deslocamento | 21,0% | — |
| Socializar | 15,4% | 14,8% |
| Exploração | 7,3% | 7,3% |
| Comércio | 7,0% | 13,3% |
| Coleta | 5,4% | 7,9% |
| Patrulha | 5,3% | 5,2% |
| Emboscada | 4,4% | 6,4% |
| Missão | 2,2% | 10,5% |
| Treino | 1,2% | 3,7% |
| Conserto | 0,0% | 0,4% |

## Continuidade das sessões (média das seeds)

| Medida | Valor |
|---|---:|
| Escolhas por sessão | 5,1 |
| Continuou na mesma atividade (% das escolhas após a primeira) | 75,0% |
| Trocas de atividade por hora conectada | 0,4 |
| Trocas de zona por sessão | 0,4 |
| Ciclos seguidos na mesma atividade (média) | 2,5 |

Motivos das trocas de plano (fator principal):

| Motivo | Trocas |
|---|---:|
| quest_step | 26,6% |
| quest_completed | 21,4% |
| time_budget | 11,0% |
| boredom | 10,2% |
| focus_unavailable | 9,1% |
| level_up | 8,8% |
| goal_completed | 5,8% |
| affinity | 2,9% |
| zone_discovered | 1,2% |
| quest_deliver | 0,8% |
| nothing_new | 0,7% |
| goal | 0,7% |
| quest_offer | 0,5% |
| level_fit | 0,2% |
| crowding | 0,0% |
| company | 0,0% |
| challenge | 0,0% |
| risk | 0,0% |
| noise | 0,0% |
| interruption | 0,0% |

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
| Floresta Sussurrante | Explorar trilhas da floresta | `explore` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Consertar equipamento | `repair` | 0,1% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Fortaleza Sombria | Ler as inscrições da cripta | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Investigar o covil do yeti | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Patrulhar as ruínas | `patrol` | 0,1% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Terras Queimadas | Explorar as terras queimadas | `explore` | 0,2% | nível mínimo 22: só 48% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Planalto dos Ventos | Espiar o acampamento orc | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Caçar mortos-vivos | `hunt` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Caçar aranhas tecelãs | `hunt` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Explorar galerias profundas | `explore` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Examinar o altar profanado | `quest` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Terras Queimadas | Minerar obsidiana | `gather` | 0,2% | nível mínimo 23: só 44% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Cidadela de Cinzaforte | Consertar equipamento | `repair` | 0,2% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Mata dos Espinhos | Seguir os rastros dos trolls | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Procurar o estandarte perdido | `quest` | 0,3% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Brejo das Brumas | Procurar o ninho das serpentes | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Investigar as galerias abandonadas | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Examinar os ninhos das harpias | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Falar com o lenhador | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Caçar lobos das colinas | `hunt` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Colher ervas das encostas | `gather` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Coletar cristais de gelo | `gather` | 0,6% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Socializar na taverna | `socialize` | 0,7% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Coletar ervas curativas | `gather` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Patrulhar a ravina | `patrol` | 0,7% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Caçar javalis | `hunt` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Emboscar viajantes | `ambush` | 0,7% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Caçar morcegos gigantes | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Minerar ferro e prata | `gather` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Caçar esqueletos da ravina | `hunt` | 0,8% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Caçar lobos | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Explorar a fortaleza | `explore` | 0,8% | nível mínimo 25: só 38% dos personagens chegaram até o fim. |
| Colinas do Vale | Caçar ursos pardos | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Caçar salamandras de fogo | `hunt` | 0,9% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Explorar as colinas | `explore` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Enfrentar golens de magma | `hunt` | 0,9% | nível mínimo 24: só 42% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Forte do Vau | Treinar com o mestre de armas | `train` | 0,9% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Mina Velha | Caçar goblins | `hunt` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Purgar a cripta | `hunt` | 1,0% | nível mínimo 26: só 36% dos personagens chegaram até o fim. |

## Execuções

| Seed | Hash final | Simulação |
|---:|---|---:|
| 1 | `636770e474c855b7` | 257,0 s |
| 2 | `babda38e311d2652` | 260,0 s |
| 3 | `c499ca8792fca29b` | 270,7 s |
| 4 | `a6f3e5a2d70318a4` | 266,3 s |
| 5 | `c9555bcc1b9f34de` | 260,4 s |

Medições de tempo desta máquina, não benchmark.
