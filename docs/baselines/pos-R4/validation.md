# Validação da simulação

> Gerado pelo `Simulation.Runner` com várias seeds. As faixas vêm de `targets.json` e são **hipóteses de design ajustáveis**, não regras do mundo. Uma meta só conta como "dentro" quando todas as seeds caem na faixa.

| Campo | Valor |
|---|---|
| Conteúdo | `Content/presets/inicial` (`ab3a506c153ddf8f`) |
| Metas | `Content/presets/inicial/targets.json` (`322f4c445d8c8479`) |
| Simulador | 0.1.0+753b9186d2182e960d6aec31a76cecb5286dded9 |
| Execuções | 5 seeds (1, 2, 3, 4, 5) · 1.000 pessoas · 90 dias |
| Janela das medidas semanais | a partir do dia 7 (exclui a semana de lançamento) |
| Tempo total | 321,5 s |

## Metas

As metas têm três classes (D-039a); o total "x de y" sozinho não diz se dá para avançar:

- **Regras do cenário:** rotina calibrada, medida numa rodada controlada com o humor neutralizado. Falhar aqui indica erro.
- **Provisórias:** balanceamento que será recalibrado (progressão, divisão do tempo, missões).
- **Dependem de sistemas futuros:** só acompanhamento, não reprovam (retenção, missões e coleta dependem de combate, economia e social).

### Regras do cenário: 7 de 7 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Sessões por semana — casual | 2,0–4,0  | 2,8 | 2,8–2,8 |  |
| ✅ dentro | Sessões por semana — hardcore | 5,0–7,0  | 6,6 | 6,2–6,9 |  |
| ✅ dentro | Horas por semana — casual | 2,0–6,0 h | 5,4 | 5,4–5,5 |  |
| ✅ dentro | Horas por semana — hardcore | 15,0–45,0 h | 31,9 | 30,7–33,0 |  |
| ✅ dentro | Jogadores ativos em dia útil / cadastrados | 30,0–55,0 % | 51,3 | 50,1–52,3 | 58 dias |
| ✅ dentro | Ativos no fim de semana ÷ ativos em dia útil | 1,3–2,0 × | 1,3 | 1,3–1,4 |  |
| ✅ dentro | Pico online do dia / cadastrados | 15,0–35,0 % | 25,0 | 24,3–25,7 |  |

### Provisórias: 8 de 12 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Horas conectadas até o nível 5 (mediana) | 1,0–4,0 h | 2,8 | 2,7–2,8 | 100% chegaram |
| ✅ dentro | Horas conectadas até o nível 10 (mediana) | 8,0–20,0 h | 15,7 | 15,5–15,7 | 98% chegaram |
| ✅ dentro | Horas conectadas até o nível 15 (mediana) | 22,0–50,0 h | 35,7 | 35,6–35,8 | 79–83% chegaram |
| ❌ abaixo | Horas conectadas até o nível 20 (mediana) | 80,0–110 h | 68,1 | 67,9–68,4 | 54–56% chegaram |
| ⚠️ instável | Horas conectadas até o nível 30 (mediana) | 170–230 h | 171 | 169–173 | 22–24% chegaram (mediana só de quem chegou: subestima o tempo real) |
| ❌ acima | Casuais no nível 10 ou mais ao fim | 60,0–95,0 % | 98,5 | 98,1–98,9 |  |
| ✅ dentro | XP vindo de quests | 15,0–35,0 % | 23,1 | 22,8–23,3 |  |
| ✅ dentro | Quests concluídas por personagem (média) | 4,0–30,0  | 28,4 | 27,7–29,3 |  |
| ✅ dentro | Tempo em atividades de combate (abstratas: caça, chefe, patrulha, emboscada; sem luta simulada até o M3) | 35,0–55,0 % | 41,1 | 40,4–41,8 |  |
| ✅ dentro | Tempo explorando | 5,0–15,0 % | 6,7 | 6,2–7,2 |  |
| ✅ dentro | Tempo na cidade (comércio, taverna, treino, conserto) | 10,0–25,0 % | 17,8 | 17,2–18,0 |  |
| ❌ acima | Tempo andando ou esperando | 5,0–20,0 % | 26,3 | 26,1–26,5 |  |

### Dependem de sistemas futuros (acompanhamento): 1 de 5 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ❌ abaixo | Tempo em missões | 10,0–25,0 % | 2,8 | 2,7–2,8 |  |
| ❌ abaixo | Tempo em coleta | 8,0–20,0 % | 5,3 | 5,0–5,5 |  |
| ❌ abaixo | Desistentes no fim / cadastrados | 8,0–30,0 % | 0,7 | 0,3–0,9 |  |
| ❌ abaixo | Inativos no fim / cadastrados | 2,0–15,0 % | 1,1 | 0,7–1,5 |  |
| ✅ dentro | Quem saiu e voltou (inativos e desistentes) | 20,0–70,0 % | 61,0 | 55,6–68,8 | 45 saíram; 41 saíram; 39 saíram; 32 saíram; 38 saíram |


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
| Casual | 2,8 | 5,4 |
| Comerciante | 4,5 | 9,5 |
| Competitivo | 4,9 | 16,6 |
| Explorador | 4,1 | 11,4 |
| Hardcore | 6,6 | 31,9 |
| Social | 3,7 | 9,6 |

## Perfis na campanha completa (média das seeds)

| Perfil | Pessoas | Sessões/semana | Horas/semana | Nível mediano no fim | Nível p90 | Sessões em dia útil iniciadas entre 18h e 23h |
|---|---:|---:|---:|---:|---:|---:|
| Casual | 353 | 3,0 | 6,0 | 19,8 | 24,0 | 62,1% |
| Comerciante | 99,4 | 4,6 | 10,0 | 15,4 | 20,0 | 53,5% |
| Competitivo | 149 | 5,2 | 18,2 | 30,0 | 30,0 | 55,8% |
| Explorador | 99,4 | 4,7 | 13,7 | 27,4 | 30,0 | 45,3% |
| Hardcore | 98,0 | 6,7 | 33,3 | 30,0 | 30,0 | 45,8% |
| Social | 202 | 3,9 | 10,4 | 13,0 | 18,4 | 57,5% |

## Evolução dos atributos do jogador (média das seeds)

Variação média desde o início, em **pontos da escala 1–20** (1 ponto ≈ 52,6 unidades do Core), ao fim de 90 dias. Valores de `population.json` (`dynamics`).

| Perfil | Mecânica | Conhecimento | PvE | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | +0,10 | +1,25 | +0,16 | +0,09 | 0,00 | 0,00 | 11,2 | 1,8 |
| Comerciante | +0,03 | +0,95 | +0,08 | +0,05 | +0,22 | 0,00 | 16,8 | 3,1 |
| Competitivo | +0,74 | +1,23 | +0,24 | +0,06 | +0,02 | -0,21 | 14,9 | 10,1 |
| Explorador | +0,25 | +1,39 | +0,15 | +0,31 | +0,01 | -0,04 | 15,2 | 5,1 |
| Hardcore | +1,01 | +0,97 | +0,70 | +0,05 | +0,01 | -0,11 | 26,9 | 3,6 |
| Social | +0,04 | +1,08 | +0,07 | +0,04 | 0,00 | 0,00 | 15,0 | 3,8 |

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
| Aposentado | 20,4 | 5,2 | 16,9 | — | 15,3 h | 0,0% | R$ 3.427 | R$ 36,9 |
| Autônomo / freelancer | 43,4 | 5,4 | 15,0 | 8,9 h | 15,2 h | 0,0% | R$ 4.323 | R$ 53,7 |
| Trabalho em horário comercial | 231 | 3,8 | 11,3 | 4,2 h | 14,8 h | 0,0% | R$ 4.410 | R$ 29,9 |
| Cuidando da casa e da família | 30,4 | 4,7 | 12,8 | 8,4 h | — | 0,0% | R$ 0,0 | R$ 2.320 |
| Comércio e serviços em escala 5x2 | 84,0 | 3,7 | 10,3 | 3,3 h | 14,9 h | 0,0% | R$ 2.605 | R$ 31,2 |
| Comércio em escala 6x1 | 37,8 | 3,6 | 9,9 | 4,8 h | 15,0 h | 0,0% | R$ 2.706 | R$ 27,3 |
| Estudante (manhã) | 106 | 4,5 | 14,3 | 8,4 h | 14,7 h | 0,0% | R$ 0,0 | R$ 730 |
| Estudante (tarde) | 44,0 | 4,6 | 13,9 | 8,7 h | 15,2 h | 0,0% | R$ 0,0 | R$ 737 |
| Home office (tecnologia) | 81,8 | 4,9 | 12,4 | 6,4 h | 15,1 h | 0,0% | R$ 8.195 | R$ 17,0 |
| Meio período (manhã) | 30,4 | 4,6 | 14,8 | 9,5 h | 14,9 h | 0,0% | R$ 1.961 | R$ 291 |
| Meio período (tarde) | 26,0 | 4,7 | 14,8 | 9,3 h | 15,1 h | 0,0% | R$ 1.948 | R$ 282 |
| Sem ocupação | 76,2 | 5,4 | 17,6 | — | 15,3 h | 0,0% | R$ 0,0 | R$ 839 |
| Trabalha de dia e estuda à noite | 56,8 | 1,6 | 4,9 | 1,0 h | 12,9 h | 53,1% | R$ 3.374 | R$ 53,2 |
| Turno noturno | 32,6 | 3,9 | 12,6 | 7,1 h | 11,4 h | 0,0% | R$ 3.601 | R$ 19,7 |
| Universitário (diurno) | 99,2 | 4,2 | 12,8 | 7,1 h | 14,8 h | 0,0% | R$ 526 | R$ 1.376 |

Como terminaram os dias da agenda:

| Dia | Parcela |
|---|---:|
| Jogou | 57,1% |
| Tinha tempo, preferiu outro lazer | 38,6% |
| Sem tempo livre | 3,0% |
| Inativo | 0,9% |
| Cansado (dormiu pouco) | 0,4% |

## Sono e dificuldade de parar (média das seeds, campanha completa)

Importância do jogo, tendência a prolongar e autocontrole são traços separados. A extensão tem limites explícitos (vezes por sessão, minutos, sono mínimo e teto do déficit); só corta sono no fim do dia, nunca uma obrigação. Sono cortado vira déficit; manhã livre e noites normais recuperam.

- Extensões na janela: 1.562 (757 gostando da sessão, 805 perto de concluir a meta); 613 avançaram sobre a hora de dormir.
- Noites encurtadas pelo jogo: 622, 79,5 min a menos em média.

| Perfil | Extensões/semana | Sono cortado/semana | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|
| Casual | 0,1 | 0,4 min | 0,2 h | 2,1% |
| Comerciante | 0,1 | 0,5 min | 0,4 h | 2,9% |
| Competitivo | 0,2 | 6,3 min | 0,3 h | 2,6% |
| Explorador | 0,2 | 2,9 min | 0,2 h | 1,8% |
| Hardcore | 0,3 | 24,0 min | 0,5 h | 3,4% |
| Social | 0,1 | 1,9 min | 0,2 h | 2,0% |

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
| Meio período (tarde) | 7,9 h | 7,9 h | 7,8 h | 0,0 h | 0,0% |
| Sem ocupação | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Trabalha de dia e estuda à noite | 7,9 h | 6,9 h | 9,7 h | 4,8 h | 41,0% |
| Turno noturno | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Universitário (diurno) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |

## Jogar no horário de trabalho (média das seeds)

Só no trabalho remoto. Pausa: jogar no intervalo permitido. Escapada: jogar no expediente, rara, pela flexibilidade do trabalho, importância do jogo, falta de autocontrole e folga do dia; sessão curta e interrompível. Deslocamento, trabalho presencial e estudo ficam protegidos. Sem consequência profissional por enquanto.

- Sessões na pausa: 1.127 (2,3% das sessões); escapadas: 227 (0,5%).
- Chamados do trabalho durante a escapada: 79,2 (5,4 terminaram a atividade, 73,8 largaram e saíram).

| Ocupação | Pausa/semana | Escapadas/semana | Dias de trabalho com escapada |
|---|---:|---:|---:|
| Autônomo / freelancer | 0,8 | 0,2 | 3,1% |
| Home office (tecnologia) | 0,7 | 0,1 | 2,8% |

## Distribuições (R3)

Medição da campanha completa (com humor), seeds somadas, na janela de medição. A referência antes da R4 (modelo de vontade) está em `docs/baselines/pre-R4/`, com o mesmo relatório e o cenário controlado (pessoas iguais por célula perfil × ocupação), que separa o efeito das regras da mistura da população.

**Duração das sessões (minutos, sessões normais).** Inclui extensões e o fim real; pausa e escapada do trabalho à parte.

| Perfil | Sessões | p10 | p25 | Mediana | p75 | p90 | Máx. | Média |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 61.242 | 88,0 | 103 | 120 | 138 | 156 | 318 | 121 |
| Comerciante | 26.370 | 97,0 | 112 | 131 | 151 | 168 | 319 | 132 |
| Competitivo | 44.008 | 156 | 181 | 213 | 248 | 281 | 511 | 216 |
| Explorador | 26.672 | 133 | 153 | 178 | 205 | 229 | 445 | 180 |
| Hardcore | 38.223 | 190 | 238 | 301 | 365 | 417 | 734 | 302 |
| Social | 45.724 | 117 | 136 | 160 | 185 | 209 | 409 | 162 |
| Pausa no trabalho | 5.634 | 36,0 | 43,0 | 54,0 | 55,0 | 55,0 | 55,0 | 48,5 |
| Escapada no expediente | 1.135 | 10,0 | 17,0 | 23,0 | 26,0 | 29,0 | 34,0 | 21,2 |

**Horas por semana, perfil × ocupação** (por pessoa registrada; "—" com menos de 10 pessoas na soma das seeds).

| Ocupação | Casual | Comerciante | Competitivo | Explorador | Hardcore | Social |
|---|---:|---:|---:|---:|---:|---:|
| Aposentado | 9,6 | 13,7 | 26,2 | 18,2 | — | 15,1 |
| Autônomo / freelancer | 7,1 | 11,8 | 20,7 | 16,6 | 42,7 | 12,6 |
| Trabalho em horário comercial | 4,9 | 8,6 | 16,4 | 12,5 | 30,9 | 9,1 |
| Cuidando da casa e da família | 5,6 | 9,1 | 15,5 | 13,7 | 30,3 | 9,5 |
| Comércio e serviços em escala 5x2 | 4,5 | 8,2 | 15,9 | 11,0 | 26,6 | 8,2 |
| Comércio em escala 6x1 | 4,2 | 8,5 | 14,7 | 10,4 | 28,1 | 7,7 |
| Estudante (manhã) | 7,1 | 11,4 | 21,1 | 15,6 | 40,1 | 11,9 |
| Estudante (tarde) | 7,0 | 11,3 | 20,3 | 14,5 | 36,6 | 11,1 |
| Home office (tecnologia) | 5,9 | 10,3 | 18,9 | 12,5 | 36,6 | 10,8 |
| Meio período (manhã) | 7,6 | 12,3 | 22,1 | 16,3 | 38,7 | 12,7 |
| Meio período (tarde) | 7,7 | 11,8 | 21,8 | 14,8 | 37,3 | 12,7 |
| Sem ocupação | 9,7 | 13,2 | 22,8 | 19,5 | 41,4 | 15,4 |
| Trabalha de dia e estuda à noite | 2,3 | 3,7 | 7,0 | 5,0 | 12,9 | 4,2 |
| Turno noturno | 5,2 | 9,7 | 17,8 | 12,4 | 34,7 | 9,2 |
| Universitário (diurno) | 6,1 | 10,5 | 19,7 | 14,5 | 36,5 | 11,3 |

**Sessões por dia de jogo:** 97,6% com uma, 2,4% com duas, 0,1% com três ou mais. Intervalo entre sessões do mesmo dia: mediana 310 min, p90 550 min.
**Segunda sessão (R4):** planejada em 0,7% dos dias com jogo (a vontade que sobrou e outra janela); deixada de lado em 0 dias, porque a pessoa já tinha jogado o que queria.

**Sessões mais longas e por quê** (as 8 maiores sessões normais, seeds somadas). Resto: terminar a atividade ou a viagem e segunda sessão emendada.

| Perfil | Ocupação | Duração | Planejada ao começar | Extensões | Resto | Vontade do dia | Disposição | Tempo livre do dia |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Hardcore | Trabalho em horário comercial | 734 min | 554 min | 180 min | 0 min | 554 min | +15% | 13,2 h |
| Hardcore | Estudante (manhã) | 692 min | 512 min | 180 min | 0 min | 512 min | +11% | 15,2 h |
| Hardcore | Trabalho em horário comercial | 688 min | 508 min | 180 min | 0 min | 508 min | +6% | 14,2 h |
| Hardcore | Aposentado | 680 min | 491 min | 180 min | 9 min | 491 min | −3% | 15,5 h |
| Hardcore | Aposentado | 679 min | 499 min | 180 min | 0 min | 499 min | +5% | 15,5 h |
| Hardcore | Sem ocupação | 679 min | 491 min | 180 min | 8 min | 491 min | +8% | 15,4 h |
| Hardcore | Meio período (manhã) | 677 min | 490 min | 180 min | 7 min | 490 min | +13% | 9,2 h |
| Hardcore | Universitário (diurno) | 676 min | 492 min | 180 min | 4 min | 492 min | +6% | 14,3 h |


**Quanto do tempo livre vira jogo** (minutos jogados ÷ minutos livres da agenda; o jogo no trabalho e as extensões entram no jogado).

| Perfil | Uso |
|---|---:|
| Casual | 8,5% |
| Comerciante | 14,3% |
| Competitivo | 26,1% |
| Explorador | 18,9% |
| Hardcore | 47,1% |
| Social | 15,0% |

| Ocupação | Uso |
|---|---:|
| Aposentado | 14,6% |
| Autônomo / freelancer | 18,5% |
| Trabalho em horário comercial | 19,5% |
| Cuidando da casa e da família | 19,8% |
| Comércio e serviços em escala 5x2 | 19,7% |
| Comércio em escala 6x1 | 21,1% |
| Estudante (manhã) | 17,9% |
| Estudante (tarde) | 17,1% |
| Home office (tecnologia) | 17,7% |
| Meio período (manhã) | 17,6% |
| Meio período (tarde) | 17,3% |
| Sem ocupação | 15,0% |
| Trabalha de dia e estuda à noite | 12,6% |
| Turno noturno | 19,5% |
| Universitário (diurno) | 17,6% |

**Composição do tempo jogado:** 741.034 h no total; extensões 1,1%; pausa e escapada do trabalho 0,7%; sono cortado pelo jogo 4.119 h (0,6% do tempo jogado).

**Dias sem sessão principal** (42,9% dos dias da agenda):

| Motivo | Parcela dos dias sem sessão |
|---|---:|
| Sem oportunidade (nenhuma janela comporta uma sessão) | 7,0% |
| Escolha: tinha tempo, preferiu outro lazer | 88,5% |
| Cansaço | 1,0% |
| Inativo | 2,0% |
| Jogou só no horário de trabalho | 1,6% |

**Efeito do tempo livre, separado** (dias da agenda por faixa de tempo livre): a frequência (chance de jogar) e a duração (minutos planejados quando joga) medidas à parte.

| Tempo livre no dia | Dias | Jogou | Duração planejada quando joga | Jogado por dia |
|---|---:|---:|---:|---:|
| até 2 h | 4.533 | 0,2% | 114 min | 0,2 min |
| 2–4 h | 20.187 | 44,2% | 144 min | 66,3 min |
| 4–6 h | 24.387 | 45,0% | 159 min | 74,0 min |
| 6–10 h | 42.866 | 56,7% | 176 min | 105 min |
| 10 h ou mais | 58.027 | 71,4% | 186 min | 126 min |

**Corte de sono (observações do criador, R2/R3).**

- **Onde pode cortar:** só a janela que termina **por causa do sono** avança sobre ele. Janela que termina por obrigação, preparo, deslocamento ou compromisso não avança (teste).
- **Teto compartilhado:** os 180 minutos por período de sono valem para a soma de todas as sessões que afetam aquele período (teste).
- **Medido:** 3.109 períodos de sono encurtados pelo jogo; corte por período: mediana 71,0 min, p90 157 min, máximo 180 min.

## Localidades (média das seeds)

Cada pessoa joga no horário e nos feriados de onde mora; o relógio do jogo é o do servidor. O início médio é uma média circular das horas (a meia-noite não distorce), então local e servidor diferem exatamente pelo fuso.

| Localidade | Fuso | Pessoas | Estrangeiros | Sessões | Início médio (local) | Início médio (servidor) |
|---|---|---:|---:|---:|---:|---:|
| Argentina | UTC−3 | 85,4 | 2,2 | 4.339 | 17h37 | 17h37 |
| Brasil | UTC−3 | 758 | 19,0 | 37.975 | 17h40 | 17h40 |
| China | UTC+8 | 75,4 | 0,8 | 3.715 | 17h57 | 06h57 |
| Estados Unidos | UTC−5 | 81,4 | 12,6 | 3.820 | 17h54 | 19h54 |

## Retenção (média das seeds, no fim)

| Perfil | Desistiram | Inativos | Voltaram alguma vez |
|---|---:|---:|---:|
| Casual | 1,5% | 1,9% | 4,9% |
| Comerciante | 0,2% | 0,4% | 0,6% |
| Competitivo | 0,1% | 0,5% | 1,2% |
| Explorador | 0,2% | 1,2% | 0,8% |
| Hardcore | 0,0% | 0,0% | 0,4% |
| Social | 0,5% | 1,0% | 1,4% |

Desistências: dia mediano 65,0, nível mediano 6,0. Sessões estendidas por estarem indo bem: 3,1%; encerradas por frustração: 0,0%.

Motivo principal das desistências (o que mais incomodava; "long_inactivity/" = sumiu por muito tempo):

| Motivo | Desistências |
|---|---:|
| long_inactivity/long_inactivity | 94,1% |
| long_inactivity/boredom | 5,9% |

## Humor e memória (média das seeds, no fim)

Satisfação, frustração, novidade e engajamento em permille (0–1000); progresso em permille do XP de referência do nível.

| Perfil | Satisfação | Insatisfeitos (< 400) | Frustração | Novidade | Engajamento | Intenção de continuar | Expectativa | Progresso da última sessão | Memórias |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 630 | 0,0% | 2,8 | 992 | 740 | 678 | 640 | 629 | 10,7 |
| Comerciante | 583 | 0,0% | 3,2 | 974 | 705 | 620 | 458 | 214 | 10,4 |
| Competitivo | 610 | 0,0% | 3,9 | 931 | 719 | 658 | 734 | 530 | 10,0 |
| Explorador | 684 | 0,0% | 3,0 | 976 | 776 | 734 | 629 | 578 | 10,3 |
| Hardcore | 583 | 0,0% | 0,0 | 610 | 669 | 672 | 858 | 575 | 7,3 |
| Social | 613 | 0,0% | 3,6 | 952 | 723 | 702 | 457 | 109 | 9,5 |

Sessões ótimas: 15,1% · ruins: 0,0% (após a semana de lançamento).

O que mais incomoda cada pessoa (motivo mais negativo acumulado):

| Motivo | Pessoas |
|---|---:|
| boredom | 97,9% |
| fatigue | 1,1% |
| nenhum | 0,9% |
| progress | 0,1% |
| crowding | 0,0% |

## Uso do tempo conectado (todas as seeds)

Caça, chefe, patrulha e emboscada ainda são atividades abstratas (tempo e XP, sem luta simulada) até o M3; aí o tempo se divide em viagem, procura, luta, recuperação e recompensa.

| Tipo | Tempo | Escolhas |
|---|---:|---:|
| Caça | 30,6% | 33,0% |
| Deslocamento | 26,3% | — |
| Socializar | 13,0% | 13,3% |
| Exploração | 6,7% | 7,2% |
| Patrulha | 6,1% | 6,4% |
| Coleta | 5,3% | 8,4% |
| Comércio | 4,5% | 9,2% |
| Emboscada | 4,4% | 6,9% |
| Missão | 2,8% | 14,0% |
| Treino | 0,3% | 1,5% |
| Conserto | 0,0% | 0,1% |

## Continuidade das sessões (média das seeds)

| Medida | Valor |
|---|---:|
| Escolhas por sessão | 5,9 |
| Continuou na mesma atividade (% das escolhas após a primeira) | 68,4% |
| Trocas de atividade por hora conectada | 0,5 |
| Trocas de zona por sessão | 0,9 |
| Ciclos seguidos na mesma atividade (média) | 2,3 |

Motivos das trocas de plano (fator principal):

| Motivo | Trocas |
|---|---:|
| quest_step | 30,1% |
| quest_completed | 23,3% |
| level_up | 10,2% |
| boredom | 9,8% |
| focus_unavailable | 9,3% |
| time_budget | 5,9% |
| goal_completed | 5,1% |
| affinity | 1,4% |
| zone_discovered | 1,4% |
| quest_offer | 0,9% |
| nothing_new | 0,8% |
| quest_deliver | 0,8% |
| goal | 0,6% |
| level_fit | 0,3% |
| crowding | 0,1% |
| company | 0,0% |
| challenge | 0,0% |
| risk | 0,0% |
| familiarity | 0,0% |
| memory | 0,0% |
| noise | 0,0% |
| interruption | 0,0% |

## Atividades pouco usadas (menos de 1% das escolhas)

| Zona | Atividade | Tipo | Escolhas | Diagnóstico |
|---|---|---|---:|---|
| Cidadela de Cinzaforte | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Covil do Rei Goblin | Enfrentar o Rei Goblin | `boss` | 0,0% | exige grupo de 5; grupos chegam no M6, então hoje ninguém pode fazer. |
| Fortaleza Sombria | Enfrentar o Senhor das Cinzas | `boss` | 0,0% | exige grupo de 10; grupos chegam no M6, então hoje ninguém pode fazer; nível mínimo 28: só 28% dos personagens chegaram até o fim. |
| Forte do Vau | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Observar a forja vulcânica | `quest` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Ruínas Sombrias | Coletar cristais sombrios | `gather` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Consertar equipamento | `repair` | 0,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Explorar trilhas da floresta | `explore` | 0,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Consertar equipamento | `repair` | 0,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Cidadela de Cinzaforte | Consertar equipamento | `repair` | 0,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Fortaleza Sombria | Ler as inscrições da cripta | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Explorar as terras queimadas | `explore` | 0,1% | nível mínimo 22: só 44% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Patrulhar as ruínas | `patrol` | 0,1% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Picos Gelados | Investigar o covil do yeti | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Explorar galerias profundas | `explore` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Caçar mortos-vivos | `hunt` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Caçar aranhas tecelãs | `hunt` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Planalto dos Ventos | Espiar o acampamento orc | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Minerar obsidiana | `gather` | 0,2% | nível mínimo 23: só 41% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Caçar lobos das colinas | `hunt` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Examinar o altar profanado | `quest` | 0,3% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Cidadela de Cinzaforte | Treinar com o mestre de armas | `train` | 0,3% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Mata dos Espinhos | Seguir os rastros dos trolls | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Procurar o estandarte perdido | `quest` | 0,4% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Forte do Vau | Treinar com o mestre de armas | `train` | 0,4% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Brejo das Brumas | Procurar o ninho das serpentes | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Investigar as galerias abandonadas | `quest` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Examinar os ninhos das harpias | `quest` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Falar com o lenhador | `quest` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Explorar a fortaleza | `explore` | 0,5% | nível mínimo 25: só 34% dos personagens chegaram até o fim. |
| Colinas do Vale | Colher ervas das encostas | `gather` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Coletar cristais de gelo | `gather` | 0,6% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Socializar na taverna | `socialize` | 0,6% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Coletar ervas curativas | `gather` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Vila Aurora | Treinar habilidades | `train` | 0,8% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Ravina Escarlate | Caçar esqueletos da ravina | `hunt` | 0,8% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Caçar javalis | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Caçar morcegos gigantes | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Caçar ursos pardos | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar lobos | `hunt` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Minerar ferro e prata | `gather` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Caçar salamandras de fogo | `hunt` | 0,9% | nível mínimo 21: só 49% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ravina Escarlate | Patrulhar a ravina | `patrol` | 0,9% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ravina Escarlate | Emboscar viajantes | `ambush` | 0,9% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Picos Gelados | Explorar os picos | `explore` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Cidadela de Cinzaforte | Socializar na taverna | `socialize` | 1,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |

## Execuções

| Seed | Hash final | Simulação |
|---:|---|---:|
| 1 | `e0fc4d5f97f2d79d` | 230,1 s |
| 2 | `011869452c8ff8c1` | 234,0 s |
| 3 | `ed412fd81cf61137` | 239,6 s |
| 4 | `c05dc0db559d642a` | 238,4 s |
| 5 | `de409e9efab0a7c4` | 233,0 s |

Medições de tempo desta máquina, não benchmark.
