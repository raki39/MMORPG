# Validação da simulação

> Gerado pelo `Simulation.Runner` com várias seeds. As faixas vêm de `targets.json` e são **hipóteses de design ajustáveis**, não regras do mundo. Uma meta só conta como "dentro" quando todas as seeds caem na faixa.

| Campo | Valor |
|---|---|
| Conteúdo | `Content/presets/inicial` (`ae3b04a052743364`) |
| Metas | `Content/presets/inicial/targets.json` (`322f4c445d8c8479`) |
| Simulador | 0.1.0+52d8befed79145a4f1f86ca134e87d3ecf1c3735 |
| Execuções | 5 seeds (1, 2, 3, 4, 5) · 1.000 pessoas · 90 dias |
| Janela das medidas semanais | a partir do dia 7 (exclui a semana de lançamento) |
| Tempo total | 282,4 s |

## Metas

As metas têm três classes (D-039a); o total "x de y" sozinho não diz se dá para avançar:

- **Regras do cenário:** rotina calibrada, medida numa rodada controlada com o humor neutralizado. Falhar aqui indica erro.
- **Provisórias:** balanceamento que será recalibrado (progressão, divisão do tempo, missões).
- **Dependem de sistemas futuros:** só acompanhamento, não reprovam (retenção, missões e coleta dependem de combate, economia e social).

### Regras do cenário: 7 de 7 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Sessões por semana — casual | 2,0–4,0  | 2,9 | 2,8–2,9 |  |
| ✅ dentro | Sessões por semana — hardcore | 5,0–7,0  | 6,5 | 6,3–6,7 |  |
| ✅ dentro | Horas por semana — casual | 2,0–6,0 h | 5,6 | 5,6–5,7 |  |
| ✅ dentro | Horas por semana — hardcore | 15,0–45,0 h | 37,7 | 36,1–39,3 |  |
| ✅ dentro | Jogadores ativos em dia útil / cadastrados | 30,0–55,0 % | 51,8 | 50,7–52,9 | 58 dias |
| ✅ dentro | Ativos no fim de semana ÷ ativos em dia útil | 1,3–2,0 × | 1,3 | 1,3–1,3 |  |
| ✅ dentro | Pico online do dia / cadastrados | 15,0–35,0 % | 25,7 | 24,1–26,4 |  |

### Provisórias: 8 de 12 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Horas conectadas até o nível 5 (mediana) | 1,0–4,0 h | 2,7 | 2,7–2,7 | 100% chegaram |
| ✅ dentro | Horas conectadas até o nível 10 (mediana) | 8,0–20,0 h | 15,5 | 15,5–15,5 | 98–99% chegaram |
| ✅ dentro | Horas conectadas até o nível 15 (mediana) | 22,0–50,0 h | 35,8 | 35,6–36,0 | 81–85% chegaram |
| ❌ abaixo | Horas conectadas até o nível 20 (mediana) | 80,0–110 h | 67,8 | 67,2–68,3 | 56–58% chegaram |
| ✅ dentro | Horas conectadas até o nível 30 (mediana) | 170–230 h | 172 | 170–173 | 24–27% chegaram (mediana só de quem chegou: subestima o tempo real) |
| ❌ acima | Casuais no nível 10 ou mais ao fim | 60,0–95,0 % | 98,8 | 97,1–99,4 |  |
| ✅ dentro | XP vindo de quests | 15,0–35,0 % | 24,1 | 23,7–24,6 |  |
| ❌ acima | Quests concluídas por personagem (média) | 4,0–30,0  | 30,9 | 30,4–31,9 |  |
| ✅ dentro | Tempo em atividades de combate (abstratas: caça, chefe, patrulha, emboscada; sem luta simulada até o M3) | 35,0–55,0 % | 40,4 | 39,7–41,2 |  |
| ✅ dentro | Tempo explorando | 5,0–15,0 % | 6,3 | 5,7–6,9 |  |
| ✅ dentro | Tempo na cidade (comércio, taverna, treino, conserto) | 10,0–25,0 % | 18,4 | 17,6–18,9 |  |
| ❌ acima | Tempo andando ou esperando | 5,0–20,0 % | 26,9 | 26,6–27,0 |  |

### Dependem de sistemas futuros (acompanhamento): 1 de 5 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ❌ abaixo | Tempo em missões | 10,0–25,0 % | 2,7 | 2,7–2,8 |  |
| ❌ abaixo | Tempo em coleta | 8,0–20,0 % | 5,3 | 5,1–5,5 |  |
| ❌ abaixo | Desistentes no fim / cadastrados | 8,0–30,0 % | 0,7 | 0,4–1,3 |  |
| ❌ abaixo | Inativos no fim / cadastrados | 2,0–15,0 % | 1,3 | 0,9–1,7 |  |
| ✅ dentro | Quem saiu e voltou (inativos e desistentes) | 20,0–70,0 % | 58,8 | 55,3–62,8 | 43 saíram; 38 saíram; 46 saíram; 36 saíram; 47 saíram |


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
| Casual | 2,9 | 5,6 |
| Comerciante | 4,5 | 10,9 |
| Competitivo | 4,9 | 19,0 |
| Explorador | 4,2 | 12,7 |
| Hardcore | 6,5 | 37,7 |
| Social | 3,7 | 10,5 |

## Perfis na campanha completa (média das seeds)

| Perfil | Pessoas | Sessões/semana | Horas/semana | Nível mediano no fim | Nível p90 | Sessões em dia útil iniciadas entre 18h e 23h |
|---|---:|---:|---:|---:|---:|---:|
| Casual | 353 | 3,1 | 6,0 | 20,0 | 24,0 | 57,7% |
| Comerciante | 99,4 | 4,6 | 11,3 | 16,8 | 21,0 | 46,8% |
| Competitivo | 149 | 5,2 | 20,3 | 30,0 | 30,0 | 49,2% |
| Explorador | 99,4 | 4,7 | 14,6 | 27,8 | 30,0 | 40,4% |
| Hardcore | 98,0 | 6,5 | 37,9 | 30,0 | 30,0 | 42,2% |
| Social | 202 | 3,9 | 11,2 | 13,2 | 18,8 | 52,5% |

## Evolução dos atributos do jogador (média das seeds)

Variação média desde o início, em **pontos da escala 1–20** (1 ponto ≈ 52,6 unidades do Core), ao fim de 90 dias. Valores de `population.json` (`dynamics`).

| Perfil | Mecânica | Conhecimento | PvE | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | +0,10 | +1,28 | +0,17 | +0,10 | 0,00 | 0,00 | 11,4 | 1,7 |
| Comerciante | +0,03 | +0,98 | +0,09 | +0,05 | +0,24 | 0,00 | 17,6 | 3,3 |
| Competitivo | +0,79 | +1,26 | +0,25 | +0,05 | +0,03 | -0,23 | 15,8 | 9,3 |
| Explorador | +0,27 | +1,42 | +0,16 | +0,31 | +0,01 | -0,04 | 15,7 | 5,2 |
| Hardcore | +1,02 | +0,96 | +0,71 | +0,05 | +0,01 | -0,12 | 27,5 | 2,9 |
| Social | +0,04 | +1,13 | +0,07 | +0,04 | 0,00 | 0,00 | 15,3 | 3,8 |

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
| Aposentado | 21,0 | 5,3 | 18,7 | — | 15,3 h | 0,0% | R$ 3.414 | R$ 35,5 |
| Autônomo / freelancer | 46,6 | 5,4 | 16,8 | 8,2 h | 15,2 h | 0,0% | R$ 4.305 | R$ 54,3 |
| Trabalho em horário comercial | 245 | 3,8 | 12,2 | 4,2 h | 14,8 h | 0,0% | R$ 4.399 | R$ 29,2 |
| Cuidando da casa e da família | 28,6 | 4,1 | 9,9 | 6,2 h | — | 0,0% | R$ 0,0 | R$ 2.199 |
| Comércio em escala 6x1 | 72,4 | 3,8 | 11,9 | 4,8 h | 15,1 h | 0,0% | R$ 2.637 | R$ 26,5 |
| Estudante (manhã) | 116 | 4,4 | 15,9 | 8,4 h | 14,7 h | 0,0% | R$ 0,0 | R$ 728 |
| Estudante (tarde) | 45,6 | 4,5 | 14,2 | 8,7 h | 15,2 h | 0,0% | R$ 0,0 | R$ 790 |
| Home office (tecnologia) | 84,0 | 4,9 | 13,4 | 5,6 h | 15,1 h | 0,0% | R$ 7.737 | R$ 5,4 |
| Meio período (manhã) | 33,6 | 4,6 | 16,0 | 9,5 h | 14,8 h | 0,0% | R$ 2.075 | R$ 339 |
| Meio período (tarde) | 28,4 | 4,8 | 16,0 | 9,4 h | 15,1 h | 0,0% | R$ 1.929 | R$ 287 |
| Sem ocupação | 79,6 | 5,4 | 19,9 | — | 15,3 h | 0,0% | R$ 0,0 | R$ 838 |
| Trabalha de dia e estuda à noite | 58,8 | 1,5 | 5,0 | 1,0 h | 13,0 h | 53,1% | R$ 3.325 | R$ 53,4 |
| Turno noturno | 37,4 | 3,8 | 13,2 | 7,2 h | 15,1 h | 0,0% | R$ 3.645 | R$ 15,7 |
| Universitário (diurno) | 103 | 4,3 | 13,7 | 7,6 h | 14,8 h | 0,0% | R$ 526 | R$ 1.374 |

Como terminaram os dias da agenda:

| Dia | Parcela |
|---|---:|
| Jogou | 57,2% |
| Tinha tempo, preferiu outro lazer | 38,5% |
| Sem tempo livre | 3,1% |
| Inativo | 0,9% |
| Cansado (dormiu pouco) | 0,4% |
| Sem vontade (engajamento baixo) | 0,0% |

## Sono e dificuldade de parar (média das seeds, campanha completa)

Importância do jogo, tendência a prolongar e autocontrole são traços separados. A extensão tem limites explícitos (vezes por sessão, minutos, sono mínimo e teto do déficit); só corta sono no fim do dia, nunca uma obrigação. Sono cortado vira déficit; manhã livre e noites normais recuperam.

- Extensões na janela: 1.507 (718 gostando da sessão, 790 perto de concluir a meta); 637 avançaram sobre a hora de dormir.
- Noites encurtadas pelo jogo: 651, 86,7 min a menos em média.

| Perfil | Extensões/semana | Sono cortado/semana | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|
| Casual | 0,1 | 0,3 min | 0,2 h | 1,4% |
| Comerciante | 0,1 | 0,3 min | 0,2 h | 2,1% |
| Competitivo | 0,2 | 7,0 min | 0,2 h | 1,9% |
| Explorador | 0,2 | 2,6 min | 0,2 h | 1,4% |
| Hardcore | 0,3 | 29,9 min | 0,3 h | 2,3% |
| Social | 0,1 | 1,9 min | 0,2 h | 1,4% |

Sono da noite anterior pela agenda (o sono cortado pelo jogo vai direto para o déficit). Em dia livre a pessoa dorme mais para recuperar. O déficit no fim depende do dia da semana em que a campanha termina.

| Ocupação | Precisa | Sono em dia com obrigação | Sono em dia livre | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|---:|
| Aposentado | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Autônomo / freelancer | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Trabalho em horário comercial | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Cuidando da casa e da família | 7,8 h | 7,8 h | — | 0,0 h | 0,0% |
| Comércio em escala 6x1 | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Estudante (manhã) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Estudante (tarde) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Home office (tecnologia) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Meio período (manhã) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Meio período (tarde) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Sem ocupação | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Trabalha de dia e estuda à noite | 7,8 h | 7,0 h | 9,6 h | 3,2 h | 27,8% |
| Turno noturno | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Universitário (diurno) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |

## Jogar no horário de trabalho (média das seeds)

Só no trabalho remoto. Pausa: jogar no intervalo permitido. Escapada: jogar no expediente, rara, pela flexibilidade do trabalho, importância do jogo, falta de autocontrole e folga do dia; sessão curta e interrompível. Deslocamento, trabalho presencial e estudo ficam protegidos. Sem consequência profissional por enquanto.

- Sessões na pausa: 1.080 (2,2% das sessões); escapadas: 205 (0,4%).
- Chamados do trabalho durante a escapada: 72,8 (30,8 terminaram a atividade, 42,0 largaram e saíram).

| Ocupação | Pausa/semana | Escapadas/semana | Dias de trabalho com escapada |
|---|---:|---:|---:|
| Autônomo / freelancer | 0,6 | 0,1 | 3,0% |
| Home office (tecnologia) | 0,7 | 0,1 | 2,7% |

## Localidades (média das seeds)

Cada pessoa joga no horário e nos feriados de onde mora; o relógio do jogo é o do servidor. O início médio é uma média circular das horas (a meia-noite não distorce), então local e servidor diferem exatamente pelo fuso.

| Localidade | Fuso | Pessoas | Estrangeiros | Sessões | Início médio (local) | Início médio (servidor) |
|---|---|---:|---:|---:|---:|---:|
| Argentina | UTC−3 | 85,4 | 2,2 | 4.372 | 17h20 | 17h20 |
| Brasil | UTC−3 | 758 | 19,0 | 38.078 | 17h15 | 17h15 |
| China | UTC+8 | 75,4 | 0,8 | 3.669 | 17h25 | 06h25 |
| Estados Unidos | UTC−5 | 81,4 | 12,6 | 3.909 | 17h35 | 19h35 |

## Retenção (média das seeds, no fim)

| Perfil | Desistiram | Inativos | Voltaram alguma vez |
|---|---:|---:|---:|
| Casual | 1,3% | 2,4% | 4,8% |
| Comerciante | 0,2% | 1,7% | 1,6% |
| Competitivo | 0,1% | 0,3% | 0,9% |
| Explorador | 0,0% | 0,4% | 1,2% |
| Hardcore | 0,2% | 0,8% | 0,4% |
| Social | 0,8% | 0,6% | 1,6% |

Desistências: dia mediano 65,0, nível mediano 7,0. Sessões estendidas por estarem indo bem: 3,0%; encerradas por frustração: 0,0%.

Motivo principal das desistências (o que mais incomodava; "long_inactivity/" = sumiu por muito tempo):

| Motivo | Desistências |
|---|---:|
| long_inactivity/long_inactivity | 93,9% |
| long_inactivity/boredom | 6,1% |

## Humor e memória (média das seeds, no fim)

Satisfação, frustração, novidade e engajamento em permille (0–1000); progresso em permille do XP de referência do nível.

| Perfil | Satisfação | Insatisfeitos (< 400) | Frustração | Novidade | Engajamento | Intenção de continuar | Expectativa | Progresso da última sessão | Memórias |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 631 | 0,0% | 2,4 | 992 | 740 | 679 | 643 | 657 | 10,8 |
| Comerciante | 576 | 0,0% | 5,5 | 971 | 700 | 613 | 460 | 212 | 10,4 |
| Competitivo | 606 | 0,0% | 2,9 | 891 | 713 | 654 | 742 | 516 | 9,7 |
| Explorador | 683 | 0,0% | 2,0 | 969 | 774 | 733 | 635 | 549 | 10,3 |
| Hardcore | 569 | 0,0% | 0,6 | 492 | 647 | 658 | 844 | 542 | 5,7 |
| Social | 613 | 0,0% | 4,0 | 954 | 724 | 703 | 457 | 116 | 9,7 |

Sessões ótimas: 15,0% · ruins: 0,0% (após a semana de lançamento).

O que mais incomoda cada pessoa (motivo mais negativo acumulado):

| Motivo | Pessoas |
|---|---:|
| boredom | 98,0% |
| fatigue | 0,9% |
| nenhum | 0,9% |
| progress | 0,1% |
| crowding | 0,0% |

## Uso do tempo conectado (todas as seeds)

Caça, chefe, patrulha e emboscada ainda são atividades abstratas (tempo e XP, sem luta simulada) até o M3; aí o tempo se divide em viagem, procura, luta, recuperação e recompensa.

| Tipo | Tempo | Escolhas |
|---|---:|---:|
| Caça | 29,6% | 31,9% |
| Deslocamento | 26,9% | — |
| Socializar | 13,5% | 14,0% |
| Patrulha | 6,4% | 6,8% |
| Exploração | 6,3% | 6,8% |
| Coleta | 5,3% | 8,4% |
| Comércio | 4,6% | 9,5% |
| Emboscada | 4,4% | 6,9% |
| Missão | 2,7% | 14,1% |
| Treino | 0,3% | 1,4% |
| Conserto | 0,0% | 0,1% |

## Continuidade das sessões (média das seeds)

| Medida | Valor |
|---|---:|
| Escolhas por sessão | 6,3 |
| Continuou na mesma atividade (% das escolhas após a primeira) | 67,9% |
| Trocas de atividade por hora conectada | 0,5 |
| Trocas de zona por sessão | 1,0 |
| Ciclos seguidos na mesma atividade (média) | 2,3 |

Motivos das trocas de plano (fator principal):

| Motivo | Trocas |
|---|---:|
| quest_step | 29,7% |
| quest_completed | 23,3% |
| boredom | 10,3% |
| focus_unavailable | 10,1% |
| level_up | 9,2% |
| time_budget | 5,8% |
| goal_completed | 5,0% |
| affinity | 1,6% |
| zone_discovered | 1,2% |
| quest_offer | 0,9% |
| quest_deliver | 0,9% |
| nothing_new | 0,8% |
| goal | 0,6% |
| level_fit | 0,4% |
| crowding | 0,1% |
| company | 0,1% |
| challenge | 0,0% |
| risk | 0,0% |
| familiarity | 0,0% |
| memory | 0,0% |
| noise | 0,0% |

## Atividades pouco usadas (menos de 1% das escolhas)

| Zona | Atividade | Tipo | Escolhas | Diagnóstico |
|---|---|---|---:|---|
| Cidadela de Cinzaforte | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Covil do Rei Goblin | Enfrentar o Rei Goblin | `boss` | 0,0% | exige grupo de 5; grupos chegam no M6, então hoje ninguém pode fazer. |
| Fortaleza Sombria | Enfrentar o Senhor das Cinzas | `boss` | 0,0% | exige grupo de 10; grupos chegam no M6, então hoje ninguém pode fazer; nível mínimo 28: só 29% dos personagens chegaram até o fim. |
| Forte do Vau | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Observar a forja vulcânica | `quest` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Ruínas Sombrias | Coletar cristais sombrios | `gather` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Consertar equipamento | `repair` | 0,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Explorar trilhas da floresta | `explore` | 0,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Consertar equipamento | `repair` | 0,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Cidadela de Cinzaforte | Consertar equipamento | `repair` | 0,1% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Explorar as terras queimadas | `explore` | 0,1% | nível mínimo 22: só 45% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Fortaleza Sombria | Ler as inscrições da cripta | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Patrulhar as ruínas | `patrol` | 0,1% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Caçar aranhas tecelãs | `hunt` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Caçar mortos-vivos | `hunt` | 0,1% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Explorar galerias profundas | `explore` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Investigar o covil do yeti | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Planalto dos Ventos | Espiar o acampamento orc | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Minerar obsidiana | `gather` | 0,2% | nível mínimo 23: só 41% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Examinar o altar profanado | `quest` | 0,3% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Caçar lobos das colinas | `hunt` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mata dos Espinhos | Seguir os rastros dos trolls | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Cidadela de Cinzaforte | Treinar com o mestre de armas | `train` | 0,3% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Ravina Escarlate | Procurar o estandarte perdido | `quest` | 0,3% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Forte do Vau | Treinar com o mestre de armas | `train` | 0,4% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Brejo das Brumas | Procurar o ninho das serpentes | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Investigar as galerias abandonadas | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Falar com o lenhador | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Examinar os ninhos das harpias | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Colher ervas das encostas | `gather` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Explorar a fortaleza | `explore` | 0,6% | nível mínimo 25: só 35% dos personagens chegaram até o fim. |
| Floresta Sussurrante | Coletar ervas curativas | `gather` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Coletar cristais de gelo | `gather` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Vila Aurora | Treinar habilidades | `train` | 0,7% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Caçar javalis | `hunt` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Caçar esqueletos da ravina | `hunt` | 0,7% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Caçar lobos | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Caçar morcegos gigantes | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Minerar ferro e prata | `gather` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Patrulhar a ravina | `patrol` | 0,8% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ravina Escarlate | Emboscar viajantes | `ambush` | 0,8% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Caçar ursos pardos | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Explorar os picos | `explore` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Caçar salamandras de fogo | `hunt` | 0,9% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Explorar as colinas | `explore` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Caçar goblins | `hunt` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Enfrentar golens de magma | `hunt` | 1,0% | nível mínimo 24: só 38% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |

## Execuções

| Seed | Hash final | Simulação |
|---:|---|---:|
| 1 | `e3abc0fba81ae203` | 192,6 s |
| 2 | `c5035fdcd3fd4692` | 194,8 s |
| 3 | `720310ba6127f87a` | 203,3 s |
| 4 | `fc0f8bf7782534e7` | 201,2 s |
| 5 | `57806bd4b8493fcc` | 197,2 s |

Medições de tempo desta máquina, não benchmark.
