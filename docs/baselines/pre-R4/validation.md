# Validação da simulação

> Gerado pelo `Simulation.Runner` com várias seeds. As faixas vêm de `targets.json` e são **hipóteses de design ajustáveis**, não regras do mundo. Uma meta só conta como "dentro" quando todas as seeds caem na faixa.

| Campo | Valor |
|---|---|
| Conteúdo | `Content/presets/inicial` (`81726331478c48f9`) |
| Metas | `Content/presets/inicial/targets.json` (`322f4c445d8c8479`) |
| Simulador | 0.1.0+b48f85700c8fa37ad3232d4369cbbbc69299b7ff |
| Execuções | 5 seeds (1, 2, 3, 4, 5) · 1.000 pessoas · 90 dias |
| Janela das medidas semanais | a partir do dia 7 (exclui a semana de lançamento) |
| Tempo total | 274,0 s |

## Metas

As metas têm três classes (D-039a); o total "x de y" sozinho não diz se dá para avançar:

- **Regras do cenário:** rotina calibrada, medida numa rodada controlada com o humor neutralizado. Falhar aqui indica erro.
- **Provisórias:** balanceamento que será recalibrado (progressão, divisão do tempo, missões).
- **Dependem de sistemas futuros:** só acompanhamento, não reprovam (retenção, missões e coleta dependem de combate, economia e social).

### Regras do cenário: 7 de 7 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Sessões por semana — casual | 2,0–4,0  | 2,8 | 2,8–2,9 |  |
| ✅ dentro | Sessões por semana — hardcore | 5,0–7,0  | 6,5 | 6,1–6,7 |  |
| ✅ dentro | Horas por semana — casual | 2,0–6,0 h | 5,5 | 5,5–5,6 |  |
| ✅ dentro | Horas por semana — hardcore | 15,0–45,0 h | 36,8 | 35,4–38,3 |  |
| ✅ dentro | Jogadores ativos em dia útil / cadastrados | 30,0–55,0 % | 51,2 | 50,0–52,6 | 58 dias |
| ✅ dentro | Ativos no fim de semana ÷ ativos em dia útil | 1,3–2,0 × | 1,3 | 1,3–1,4 |  |
| ✅ dentro | Pico online do dia / cadastrados | 15,0–35,0 % | 26,9 | 26,0–27,5 |  |

### Provisórias: 8 de 12 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Horas conectadas até o nível 5 (mediana) | 1,0–4,0 h | 2,8 | 2,8–2,9 | 100% chegaram |
| ✅ dentro | Horas conectadas até o nível 10 (mediana) | 8,0–20,0 h | 15,6 | 15,5–15,7 | 98–99% chegaram |
| ✅ dentro | Horas conectadas até o nível 15 (mediana) | 22,0–50,0 h | 36,0 | 35,7–36,4 | 82–83% chegaram |
| ❌ abaixo | Horas conectadas até o nível 20 (mediana) | 80,0–110 h | 68,3 | 67,9–68,7 | 55–57% chegaram |
| ✅ dentro | Horas conectadas até o nível 30 (mediana) | 170–230 h | 173 | 171–175 | 24–26% chegaram (mediana só de quem chegou: subestima o tempo real) |
| ❌ acima | Casuais no nível 10 ou mais ao fim | 60,0–95,0 % | 98,7 | 97,4–99,4 |  |
| ✅ dentro | XP vindo de quests | 15,0–35,0 % | 23,7 | 23,3–24,0 |  |
| ❌ acima | Quests concluídas por personagem (média) | 4,0–30,0  | 30,2 | 29,5–31,0 |  |
| ✅ dentro | Tempo em atividades de combate (abstratas: caça, chefe, patrulha, emboscada; sem luta simulada até o M3) | 35,0–55,0 % | 40,6 | 40,1–41,0 |  |
| ✅ dentro | Tempo explorando | 5,0–15,0 % | 6,4 | 6,0–6,9 |  |
| ✅ dentro | Tempo na cidade (comércio, taverna, treino, conserto) | 10,0–25,0 % | 18,2 | 17,8–18,8 |  |
| ❌ acima | Tempo andando ou esperando | 5,0–20,0 % | 26,7 | 26,6–26,8 |  |

### Dependem de sistemas futuros (acompanhamento): 1 de 5 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ❌ abaixo | Tempo em missões | 10,0–25,0 % | 2,7 | 2,7–2,7 |  |
| ❌ abaixo | Tempo em coleta | 8,0–20,0 % | 5,3 | 5,0–5,6 |  |
| ❌ abaixo | Desistentes no fim / cadastrados | 8,0–30,0 % | 0,6 | 0,4–1,1 |  |
| ❌ abaixo | Inativos no fim / cadastrados | 2,0–15,0 % | 1,3 | 0,6–1,7 |  |
| ✅ dentro | Quem saiu e voltou (inativos e desistentes) | 20,0–70,0 % | 61,2 | 56,8–65,1 | 47 saíram; 44 saíram; 43 saíram; 35 saíram |


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
| Casual | 2,8 | 5,5 |
| Comerciante | 4,5 | 10,8 |
| Competitivo | 4,9 | 18,9 |
| Explorador | 4,1 | 12,5 |
| Hardcore | 6,5 | 36,8 |
| Social | 3,7 | 10,4 |

## Perfis na campanha completa (média das seeds)

| Perfil | Pessoas | Sessões/semana | Horas/semana | Nível mediano no fim | Nível p90 | Sessões em dia útil iniciadas entre 18h e 23h |
|---|---:|---:|---:|---:|---:|---:|
| Casual | 353 | 3,0 | 5,9 | 19,6 | 24,0 | 62,4% |
| Comerciante | 99,4 | 4,6 | 11,2 | 16,8 | 20,8 | 53,8% |
| Competitivo | 149 | 5,1 | 20,2 | 30,0 | 30,0 | 54,3% |
| Explorador | 99,4 | 4,7 | 14,4 | 27,8 | 30,0 | 45,1% |
| Hardcore | 98,0 | 6,5 | 37,3 | 30,0 | 30,0 | 45,3% |
| Social | 202 | 3,9 | 11,0 | 13,2 | 19,0 | 57,6% |

## Evolução dos atributos do jogador (média das seeds)

Variação média desde o início, em **pontos da escala 1–20** (1 ponto ≈ 52,6 unidades do Core), ao fim de 90 dias. Valores de `population.json` (`dynamics`).

| Perfil | Mecânica | Conhecimento | PvE | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | +0,09 | +1,26 | +0,16 | +0,09 | 0,00 | 0,00 | 11,4 | 1,6 |
| Comerciante | +0,03 | +0,98 | +0,09 | +0,05 | +0,24 | 0,00 | 17,5 | 3,4 |
| Competitivo | +0,79 | +1,25 | +0,25 | +0,06 | +0,02 | -0,23 | 15,6 | 9,5 |
| Explorador | +0,27 | +1,41 | +0,16 | +0,32 | +0,01 | -0,04 | 15,4 | 5,2 |
| Hardcore | +1,01 | +0,96 | +0,70 | +0,05 | +0,01 | -0,12 | 27,0 | 3,1 |
| Social | +0,04 | +1,12 | +0,08 | +0,04 | 0,00 | 0,00 | 15,3 | 3,8 |

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
| Aposentado | 20,4 | 5,3 | 18,7 | — | 15,3 h | 0,0% | R$ 3.427 | R$ 36,9 |
| Autônomo / freelancer | 43,4 | 5,5 | 16,9 | 8,9 h | 15,1 h | 0,0% | R$ 4.323 | R$ 53,7 |
| Trabalho em horário comercial | 231 | 3,8 | 12,0 | 4,2 h | 14,8 h | 0,0% | R$ 4.410 | R$ 29,9 |
| Cuidando da casa e da família | 30,4 | 4,3 | 12,0 | 8,4 h | — | 0,0% | R$ 0,0 | R$ 2.320 |
| Comércio e serviços em escala 5x2 | 84,0 | 3,7 | 10,8 | 3,3 h | 14,9 h | 0,0% | R$ 2.605 | R$ 31,2 |
| Comércio em escala 6x1 | 37,8 | 3,6 | 10,1 | 4,8 h | 15,0 h | 0,0% | R$ 2.706 | R$ 27,3 |
| Estudante (manhã) | 106 | 4,4 | 15,6 | 8,4 h | 14,7 h | 0,0% | R$ 0,0 | R$ 730 |
| Estudante (tarde) | 44,0 | 4,6 | 14,9 | 8,7 h | 15,2 h | 0,0% | R$ 0,0 | R$ 737 |
| Home office (tecnologia) | 81,8 | 4,9 | 13,7 | 6,4 h | 15,1 h | 0,0% | R$ 8.195 | R$ 17,0 |
| Meio período (manhã) | 30,4 | 4,6 | 16,4 | 9,5 h | 14,9 h | 0,0% | R$ 1.961 | R$ 291 |
| Meio período (tarde) | 26,0 | 4,7 | 15,8 | 9,3 h | 15,1 h | 0,0% | R$ 1.948 | R$ 282 |
| Sem ocupação | 76,2 | 5,4 | 19,6 | — | 15,3 h | 0,0% | R$ 0,0 | R$ 839 |
| Trabalha de dia e estuda à noite | 56,8 | 1,5 | 5,1 | 1,0 h | 12,9 h | 53,2% | R$ 3.374 | R$ 53,2 |
| Turno noturno | 32,6 | 4,0 | 13,6 | 7,1 h | 11,4 h | 0,0% | R$ 3.601 | R$ 19,7 |
| Universitário (diurno) | 99,2 | 4,2 | 14,0 | 7,1 h | 14,8 h | 0,0% | R$ 526 | R$ 1.376 |

Como terminaram os dias da agenda:

| Dia | Parcela |
|---|---:|
| Jogou | 56,9% |
| Tinha tempo, preferiu outro lazer | 38,8% |
| Sem tempo livre | 3,0% |
| Inativo | 0,8% |
| Cansado (dormiu pouco) | 0,5% |
| Sem vontade (engajamento baixo) | 0,0% |

## Sono e dificuldade de parar (média das seeds, campanha completa)

Importância do jogo, tendência a prolongar e autocontrole são traços separados. A extensão tem limites explícitos (vezes por sessão, minutos, sono mínimo e teto do déficit); só corta sono no fim do dia, nunca uma obrigação. Sono cortado vira déficit; manhã livre e noites normais recuperam.

- Extensões na janela: 1.524 (747 gostando da sessão, 777 perto de concluir a meta); 664 avançaram sobre a hora de dormir.
- Noites encurtadas pelo jogo: 681, 88,6 min a menos em média.

| Perfil | Extensões/semana | Sono cortado/semana | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|
| Casual | 0,1 | 0,4 min | 0,3 h | 2,1% |
| Comerciante | 0,1 | 0,7 min | 0,4 h | 2,9% |
| Competitivo | 0,2 | 8,3 min | 0,4 h | 2,7% |
| Explorador | 0,2 | 3,2 min | 0,2 h | 1,8% |
| Hardcore | 0,3 | 29,3 min | 0,5 h | 3,4% |
| Social | 0,1 | 2,1 min | 0,2 h | 1,6% |

Sono da noite anterior pela agenda (o sono cortado pelo jogo vai direto para o déficit). Em dia livre a pessoa dorme mais para recuperar. O déficit no fim depende do dia da semana em que a campanha termina.

| Ocupação | Precisa | Sono em dia com obrigação | Sono em dia livre | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|---:|
| Aposentado | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Autônomo / freelancer | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Trabalho em horário comercial | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Cuidando da casa e da família | 7,8 h | 7,8 h | — | 0,0 h | 0,0% |
| Comércio e serviços em escala 5x2 | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Comércio em escala 6x1 | 7,8 h | 7,8 h | 7,8 h | 0,1 h | 0,0% |
| Estudante (manhã) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Estudante (tarde) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Home office (tecnologia) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Meio período (manhã) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Meio período (tarde) | 7,9 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Sem ocupação | 7,8 h | — | 7,8 h | 0,0 h | 0,0% |
| Trabalha de dia e estuda à noite | 7,9 h | 6,9 h | 9,7 h | 4,9 h | 39,7% |
| Turno noturno | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Universitário (diurno) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |

## Jogar no horário de trabalho (média das seeds)

Só no trabalho remoto. Pausa: jogar no intervalo permitido. Escapada: jogar no expediente, rara, pela flexibilidade do trabalho, importância do jogo, falta de autocontrole e folga do dia; sessão curta e interrompível. Deslocamento, trabalho presencial e estudo ficam protegidos. Sem consequência profissional por enquanto.

- Sessões na pausa: 1.126 (2,3% das sessões); escapadas: 227 (0,5%).
- Chamados do trabalho durante a escapada: 79,2 (9,2 terminaram a atividade, 70,0 largaram e saíram).

| Ocupação | Pausa/semana | Escapadas/semana | Dias de trabalho com escapada |
|---|---:|---:|---:|
| Autônomo / freelancer | 0,8 | 0,2 | 3,1% |
| Home office (tecnologia) | 0,7 | 0,1 | 2,8% |

## Distribuições (R3: referência antes de mudar a vontade de jogar)

Medição da campanha completa (com humor), seeds somadas, na janela de medição. Serve de referência corrigida (depois da R1 e da R2) para comparar a R4. Nada foi recalibrado.

**Duração das sessões (minutos, sessões normais).** Inclui extensões e o fim real; pausa e escapada do trabalho à parte.

| Perfil | Sessões | p10 | p25 | Mediana | p75 | p90 | Máx. | Média |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 61.474 | 84,0 | 99,0 | 117 | 138 | 157 | 276 | 119 |
| Comerciante | 26.518 | 104 | 122 | 145 | 170 | 195 | 371 | 147 |
| Competitivo | 43.805 | 165 | 198 | 235 | 282 | 326 | 608 | 242 |
| Explorador | 26.667 | 134 | 157 | 185 | 216 | 248 | 464 | 189 |
| Hardcore | 36.817 | 202 | 248 | 340 | 448 | 526 | 853 | 351 |
| Social | 45.757 | 119 | 140 | 168 | 199 | 228 | 490 | 171 |
| Pausa no trabalho | 5.631 | 36,0 | 43,0 | 54,0 | 55,0 | 55,0 | 55,0 | 48,5 |
| Escapada no expediente | 1.134 | 10,0 | 18,0 | 23,0 | 26,0 | 29,0 | 34,0 | 21,3 |

**Horas por semana, perfil × ocupação** (por pessoa registrada; "—" com menos de 10 pessoas na soma das seeds).

| Ocupação | Casual | Comerciante | Competitivo | Explorador | Hardcore | Social |
|---|---:|---:|---:|---:|---:|---:|
| Aposentado | 9,6 | 16,1 | 29,0 | 19,5 | — | 16,2 |
| Autônomo / freelancer | 7,4 | 13,9 | 24,2 | 17,6 | 50,7 | 13,3 |
| Trabalho em horário comercial | 4,9 | 9,6 | 17,8 | 12,7 | 33,8 | 9,5 |
| Cuidando da casa e da família | 5,4 | 9,4 | 15,8 | 13,5 | 23,8 | 10,1 |
| Comércio e serviços em escala 5x2 | 4,5 | 9,1 | 16,5 | 11,1 | 28,3 | 8,8 |
| Comércio em escala 6x1 | 4,0 | 9,1 | 15,6 | 10,4 | 28,4 | 8,1 |
| Estudante (manhã) | 6,9 | 12,7 | 23,7 | 16,3 | 47,6 | 12,2 |
| Estudante (tarde) | 6,9 | 12,6 | 22,6 | 15,6 | 38,9 | 12,0 |
| Home office (tecnologia) | 5,9 | 11,7 | 22,1 | 13,7 | 41,7 | 11,5 |
| Meio período (manhã) | 7,4 | 14,0 | 25,2 | 17,3 | 47,4 | 14,1 |
| Meio período (tarde) | 7,6 | 13,2 | 23,2 | 15,5 | 40,8 | 14,1 |
| Sem ocupação | 9,5 | 14,9 | 26,5 | 20,8 | 50,5 | 17,0 |
| Trabalha de dia e estuda à noite | 2,1 | 4,0 | 7,8 | 5,5 | 13,2 | 4,0 |
| Turno noturno | 5,1 | 10,7 | 19,2 | 13,2 | 38,6 | 9,9 |
| Universitário (diurno) | 6,1 | 11,9 | 22,1 | 15,3 | 43,1 | 12,0 |

**Sessões por dia de jogo:** 98,1% com uma, 1,9% com duas, 0,1% com três ou mais. Intervalo entre sessões do mesmo dia: mediana 310 min, p90 500 min.

**Quanto do tempo livre vira jogo** (minutos jogados ÷ minutos livres da agenda; o jogo no trabalho e as extensões entram no jogado).

| Perfil | Uso |
|---|---:|
| Casual | 8,5% |
| Comerciante | 15,9% |
| Competitivo | 29,2% |
| Explorador | 20,2% |
| Hardcore | 53,5% |
| Social | 16,0% |

| Ocupação | Uso |
|---|---:|
| Aposentado | 16,0% |
| Autônomo / freelancer | 20,8% |
| Trabalho em horário comercial | 20,9% |
| Cuidando da casa e da família | 19,1% |
| Comércio e serviços em escala 5x2 | 20,7% |
| Comércio em escala 6x1 | 22,0% |
| Estudante (manhã) | 19,5% |
| Estudante (tarde) | 18,7% |
| Home office (tecnologia) | 19,9% |
| Meio período (manhã) | 19,4% |
| Meio período (tarde) | 18,3% |
| Sem ocupação | 16,8% |
| Trabalha de dia e estuda à noite | 13,1% |
| Turno noturno | 21,3% |
| Universitário (diurno) | 19,7% |

**Composição do tempo jogado:** 798.988 h no total; extensões 1,1%; pausa e escapada do trabalho 0,6%; sono cortado pelo jogo 5.031 h (0,6% do tempo jogado).

**Dias sem sessão principal** (43,1% dos dias da agenda):

| Motivo | Parcela dos dias sem sessão |
|---|---:|
| Sem oportunidade (nenhuma janela comporta uma sessão) | 7,0% |
| Escolha: tinha tempo, preferiu outro lazer | 88,5% |
| Cansaço | 1,1% |
| Desânimo (engajamento baixo) | 0,0% |
| Inativo | 1,9% |
| Jogou só no horário de trabalho | 1,5% |

**Efeito do tempo livre, separado** (dias da agenda por faixa de tempo livre): a frequência (chance de jogar) e a duração (minutos planejados quando joga) medidas à parte.

| Tempo livre no dia | Dias | Jogou | Duração planejada quando joga | Jogado por dia |
|---|---:|---:|---:|---:|
| até 2 h | 4.542 | 0,2% | 110 min | 0,2 min |
| 2–4 h | 20.194 | 43,5% | 146 min | 65,9 min |
| 4–6 h | 24.398 | 44,5% | 164 min | 75,4 min |
| 6–10 h | 42.845 | 56,4% | 192 min | 113 min |
| 10 h ou mais | 58.021 | 71,6% | 210 min | 142 min |

**Corte de sono (observações do criador, R2/R3).**

- **Onde pode cortar:** só a janela que termina **por causa do sono** avança sobre ele. Janela que termina por obrigação, preparo, deslocamento ou compromisso não avança (teste).
- **Teto compartilhado:** os 180 minutos por período de sono valem para a soma de todas as sessões que afetam aquele período (teste).
- **Medido:** 3.406 períodos de sono encurtados pelo jogo; corte por período: mediana 81,0 min, p90 180 min, máximo 180 min.

## Localidades (média das seeds)

Cada pessoa joga no horário e nos feriados de onde mora; o relógio do jogo é o do servidor. O início médio é uma média circular das horas (a meia-noite não distorce), então local e servidor diferem exatamente pelo fuso.

| Localidade | Fuso | Pessoas | Estrangeiros | Sessões | Início médio (local) | Início médio (servidor) |
|---|---|---:|---:|---:|---:|---:|
| Argentina | UTC−3 | 85,4 | 2,2 | 4.295 | 17h31 | 17h31 |
| Brasil | UTC−3 | 758 | 19,0 | 37.816 | 17h35 | 17h35 |
| China | UTC+8 | 75,4 | 0,8 | 3.655 | 17h51 | 06h51 |
| Estados Unidos | UTC−5 | 81,4 | 12,6 | 3.854 | 17h52 | 19h52 |

## Retenção (média das seeds, no fim)

| Perfil | Desistiram | Inativos | Voltaram alguma vez |
|---|---:|---:|---:|
| Casual | 1,3% | 2,2% | 5,2% |
| Comerciante | 0,0% | 1,4% | 1,6% |
| Competitivo | 0,0% | 0,5% | 0,8% |
| Explorador | 0,0% | 0,4% | 0,8% |
| Hardcore | 0,2% | 1,0% | 0,8% |
| Social | 0,8% | 0,6% | 1,8% |

Desistências: dia mediano 58,0, nível mediano 6,0. Sessões estendidas por estarem indo bem: 3,1%; encerradas por frustração: 0,0%.

Motivo principal das desistências (o que mais incomodava; "long_inactivity/" = sumiu por muito tempo):

| Motivo | Desistências |
|---|---:|
| long_inactivity/long_inactivity | 96,8% |
| long_inactivity/boredom | 3,2% |

## Humor e memória (média das seeds, no fim)

Satisfação, frustração, novidade e engajamento em permille (0–1000); progresso em permille do XP de referência do nível.

| Perfil | Satisfação | Insatisfeitos (< 400) | Frustração | Novidade | Engajamento | Intenção de continuar | Expectativa | Progresso da última sessão | Memórias |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 635 | 0,0% | 2,1 | 992 | 743 | 683 | 643 | 663 | 10,8 |
| Comerciante | 579 | 0,0% | 4,1 | 971 | 701 | 615 | 459 | 212 | 10,5 |
| Competitivo | 607 | 0,0% | 3,4 | 897 | 714 | 655 | 735 | 510 | 9,8 |
| Explorador | 682 | 0,0% | 3,3 | 971 | 774 | 732 | 634 | 576 | 10,2 |
| Hardcore | 570 | 0,0% | 0,3 | 507 | 649 | 659 | 834 | 524 | 5,9 |
| Social | 612 | 0,0% | 4,4 | 954 | 723 | 701 | 456 | 102 | 9,7 |

Sessões ótimas: 15,1% · ruins: 0,0% (após a semana de lançamento).

O que mais incomoda cada pessoa (motivo mais negativo acumulado):

| Motivo | Pessoas |
|---|---:|
| boredom | 98,1% |
| fatigue | 1,0% |
| nenhum | 0,9% |
| progress | 0,1% |
| crowding | 0,0% |

## Uso do tempo conectado (todas as seeds)

Caça, chefe, patrulha e emboscada ainda são atividades abstratas (tempo e XP, sem luta simulada) até o M3; aí o tempo se divide em viagem, procura, luta, recuperação e recompensa.

| Tipo | Tempo | Escolhas |
|---|---:|---:|
| Caça | 29,7% | 32,1% |
| Deslocamento | 26,7% | — |
| Socializar | 13,4% | 13,8% |
| Exploração | 6,4% | 6,9% |
| Patrulha | 6,4% | 6,7% |
| Coleta | 5,3% | 8,5% |
| Comércio | 4,6% | 9,5% |
| Emboscada | 4,5% | 7,0% |
| Missão | 2,7% | 14,0% |
| Treino | 0,3% | 1,4% |
| Conserto | 0,0% | 0,1% |

## Continuidade das sessões (média das seeds)

| Medida | Valor |
|---|---:|
| Escolhas por sessão | 6,3 |
| Continuou na mesma atividade (% das escolhas após a primeira) | 68,1% |
| Trocas de atividade por hora conectada | 0,5 |
| Trocas de zona por sessão | 1,0 |
| Ciclos seguidos na mesma atividade (média) | 2,3 |

Motivos das trocas de plano (fator principal):

| Motivo | Trocas |
|---|---:|
| quest_step | 29,6% |
| quest_completed | 23,0% |
| boredom | 10,6% |
| focus_unavailable | 9,9% |
| level_up | 9,4% |
| time_budget | 5,7% |
| goal_completed | 5,0% |
| affinity | 1,7% |
| zone_discovered | 1,3% |
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
| interruption | 0,0% |

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
| Terras Queimadas | Explorar as terras queimadas | `explore` | 0,1% | nível mínimo 22: só 44% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Fortaleza Sombria | Ler as inscrições da cripta | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar aranhas tecelãs | `hunt` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Investigar o covil do yeti | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Patrulhar as ruínas | `patrol` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Explorar galerias profundas | `explore` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Caçar mortos-vivos | `hunt` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Planalto dos Ventos | Espiar o acampamento orc | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Minerar obsidiana | `gather` | 0,2% | nível mínimo 23: só 40% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ruínas Sombrias | Examinar o altar profanado | `quest` | 0,3% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Caçar lobos das colinas | `hunt` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Cidadela de Cinzaforte | Treinar com o mestre de armas | `train` | 0,3% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Mata dos Espinhos | Seguir os rastros dos trolls | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Procurar o estandarte perdido | `quest` | 0,4% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Forte do Vau | Treinar com o mestre de armas | `train` | 0,4% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Brejo das Brumas | Procurar o ninho das serpentes | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Investigar as galerias abandonadas | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Examinar os ninhos das harpias | `quest` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Falar com o lenhador | `quest` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Colher ervas das encostas | `gather` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Fortaleza Sombria | Explorar a fortaleza | `explore` | 0,6% | nível mínimo 25: só 35% dos personagens chegaram até o fim. |
| Picos Gelados | Coletar cristais de gelo | `gather` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Coletar ervas curativas | `gather` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Vila Aurora | Treinar habilidades | `train` | 0,7% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Caçar javalis | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Caçar esqueletos da ravina | `hunt` | 0,8% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Minerar ferro e prata | `gather` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Socializar na taverna | `socialize` | 0,8% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Mina Velha | Caçar morcegos gigantes | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar lobos | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Patrulhar a ravina | `patrol` | 0,8% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Caçar ursos pardos | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Emboscar viajantes | `ambush` | 0,9% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Picos Gelados | Explorar os picos | `explore` | 0,9% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Caçar salamandras de fogo | `hunt` | 0,9% | nível mínimo 21: só 49% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Explorar as colinas | `explore` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Caçar goblins | `hunt` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Enfrentar golens de magma | `hunt` | 1,0% | nível mínimo 24: só 38% dos personagens chegaram até o fim; zona PvP: o risco extra afasta quem tem aversão a risco. |

## Execuções

| Seed | Hash final | Simulação |
|---:|---|---:|
| 1 | `dda5af51cce80175` | 184,8 s |
| 2 | `f77e4deeca34b63d` | 184,4 s |
| 3 | `52805c400149b088` | 195,4 s |
| 4 | `84ec59fa6ba2967a` | 191,0 s |
| 5 | `bccd18e0460d44ad` | 183,5 s |

Medições de tempo desta máquina, não benchmark.
