# Validação da simulação

> Gerado pelo `Simulation.Runner` com várias seeds. As faixas vêm de `targets.json` e são **hipóteses de design ajustáveis**, não regras do mundo. Uma meta só conta como "dentro" quando todas as seeds caem na faixa.

| Campo | Valor |
|---|---|
| Conteúdo | `reports/exp-ded/mundos/C` (`ba386860d4de910f`) |
| Metas | `reports/exp-ded/mundos/C/targets.json` (`d8f348f9966ab923`) |
| Simulador | 0.1.0+ebf4e31c53eede8349491bbc1daecf818cb137e9 |
| Execuções | 5 seeds (1, 2, 3, 4, 5) · 1.000 pessoas · 90 dias |
| Janela das medidas semanais | a partir do dia 7 (exclui a semana de lançamento) |
| Tempo total | 408,7 s |

## Metas

As metas têm três classes (D-039a); o total "x de y" sozinho não diz se dá para avançar:

- **Regras do cenário:** rotina calibrada, medida numa rodada controlada com o humor neutralizado. Falhar aqui indica erro.
- **Provisórias:** balanceamento que será recalibrado (progressão, divisão do tempo, missões).
- **Dependem de sistemas futuros:** só acompanhamento, não reprovam (retenção, missões e coleta dependem de combate, economia e social).

### Regras do cenário: 6 de 9 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Dias de jogo por semana — casual | 2,0–4,0  | 3,3 | 3,2–3,4 |  |
| ✅ dentro | Dias de jogo por semana — hardcore | 5,0–7,0  | 5,9 | 5,5–6,3 |  |
| ✅ dentro | Sessões por dia de jogo — casual | 1,1–2,0  | 1,5 | 1,5–1,5 |  |
| ✅ dentro | Sessões por dia de jogo — hardcore | 1,2–2,5  | 1,5 | 1,4–1,6 |  |
| ❌ acima | Horas por semana — casual | 3,0–9,0 h | 11,0 | 10,6–11,6 |  |
| ✅ dentro | Horas por semana — hardcore | 15,0–45,0 h | 34,6 | 33,2–36,8 |  |
| ❌ acima | Jogadores ativos em dia útil / cadastrados | 30,0–55,0 % | 56,9 | 56,5–57,5 | 58 dias |
| ❌ abaixo | Ativos no fim de semana ÷ ativos em dia útil | 1,3–2,0 × | 1,3 | 1,3–1,3 |  |
| ✅ dentro | Pico online do dia / cadastrados | 15,0–35,0 % | 31,2 | 30,5–32,4 |  |

### Provisórias: 6 de 12 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ✅ dentro | Horas conectadas até o nível 5 (mediana) | 1,0–4,0 h | 2,8 | 2,7–2,8 | 100% chegaram |
| ✅ dentro | Horas conectadas até o nível 10 (mediana) | 8,0–20,0 h | 15,8 | 15,6–15,9 | 99% chegaram |
| ✅ dentro | Horas conectadas até o nível 15 (mediana) | 22,0–50,0 h | 37,1 | 36,7–37,7 | 85–88% chegaram |
| ❌ abaixo | Horas conectadas até o nível 20 (mediana) | 80,0–110 h | 71,4 | 71,0–71,8 | 69–73% chegaram |
| ✅ dentro | Horas conectadas até o nível 30 (mediana) | 170–230 h | 182 | 181–184 | 34–37% chegaram (mediana só de quem chegou: subestima o tempo real) |
| ❌ acima | Casuais no nível 10 ou mais ao fim | 60,0–95,0 % | 99,0 | 98,8–99,2 |  |
| ✅ dentro | XP vindo de quests | 15,0–35,0 % | 23,2 | 22,4–23,8 |  |
| ❌ acima | Quests concluídas por personagem (média) | 4,0–30,0  | 35,3 | 34,9–36,1 |  |
| ⚠️ instável | Tempo em atividades de combate (abstratas: caça, chefe, patrulha, emboscada; sem luta simulada até o M3) | 35,0–55,0 % | 35,5 | 34,6–36,2 |  |
| ✅ dentro | Tempo explorando | 5,0–15,0 % | 7,7 | 7,2–8,3 |  |
| ❌ acima | Tempo na cidade (comércio, taverna, treino, conserto) | 10,0–25,0 % | 27,1 | 26,7–27,9 |  |
| ❌ acima | Tempo andando ou esperando | 5,0–20,0 % | 22,1 | 21,9–22,3 |  |

### Dependem de sistemas futuros (acompanhamento): 1 de 5 dentro

| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |
|---|---|---|---:|---|---|
| ❌ abaixo | Tempo em missões | 10,0–25,0 % | 2,1 | 2,1–2,2 |  |
| ❌ abaixo | Tempo em coleta | 8,0–20,0 % | 5,4 | 5,1–5,6 |  |
| ❌ abaixo | Desistentes no fim / cadastrados | 8,0–30,0 % | 0,7 | 0,6–0,7 |  |
| ❌ abaixo | Inativos no fim / cadastrados | 2,0–15,0 % | 1,3 | 0,9–1,5 |  |
| ✅ dentro | Quem saiu e voltou (inativos e desistentes) | 20,0–70,0 % | 56,3 | 50,0–60,5 | 38 saíram; 41 saíram; 43 saíram; 40 saíram |


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
| Casual | 3,3 | 1,51 | 5,0 | 11,0 |
| Comerciante | 4,4 | 1,57 | 7,0 | 21,0 |
| Competitivo | 4,4 | 1,52 | 6,7 | 20,4 |
| Devoto | 7,0 | 1,50 | 10,5 | 58,2 |
| Explorador | 4,4 | 1,62 | 7,2 | 21,0 |
| Hardcore | 5,9 | 1,50 | 8,9 | 34,6 |
| Social | 4,5 | 1,53 | 6,8 | 20,7 |

## Perfis na campanha completa (média das seeds)

| Perfil | Pessoas | Dias de jogo/semana | Sessões por dia de jogo | Horas/semana | Nível mediano no fim | Nível p90 | Sessões em dia útil iniciadas entre 18h e 23h |
|---|---:|---:|---:|---:|---:|---:|---:|
| Casual | 346 | 3,4 | 1,52 | 11,5 | 24,0 | 30,0 | 52,1% |
| Comerciante | 97,4 | 4,5 | 1,56 | 21,0 | 20,2 | 26,0 | 49,7% |
| Competitivo | 145 | 4,6 | 1,53 | 21,1 | 30,0 | 30,0 | 52,6% |
| Devoto | 18,6 | 6,9 | 1,50 | 57,6 | 30,0 | 30,0 | 36,2% |
| Explorador | 98,0 | 4,8 | 1,64 | 22,9 | 30,0 | 30,0 | 47,4% |
| Hardcore | 97,2 | 6,0 | 1,51 | 35,3 | 30,0 | 30,0 | 51,8% |
| Social | 197 | 4,6 | 1,54 | 21,2 | 14,2 | 21,4 | 51,2% |

## Evolução dos atributos do jogador (média das seeds)

Variação média desde o início, em **pontos da escala 1–20** (1 ponto ≈ 52,6 unidades do Core), ao fim de 90 dias. Valores de `population.json` (`dynamics`).

| Perfil | Mecânica | Conhecimento | PvE | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | +0,24 | +1,63 | +0,26 | +0,15 | +0,01 | -0,02 | 16,7 | 5,1 |
| Comerciante | +0,04 | +1,07 | +0,11 | +0,05 | +0,42 | -0,01 | 24,4 | 8,3 |
| Competitivo | +0,78 | +1,23 | +0,28 | +0,06 | +0,03 | -0,24 | 18,8 | 12,5 |
| Devoto | +1,16 | +0,98 | +1,02 | +0,05 | +0,01 | -0,17 | 43,8 | 3,1 |
| Explorador | +0,36 | +1,55 | +0,17 | +0,47 | +0,02 | -0,06 | 23,2 | 7,1 |
| Hardcore | +1,01 | +0,97 | +0,82 | +0,05 | 0,00 | -0,11 | 34,3 | 5,0 |
| Social | +0,07 | +1,32 | +0,09 | +0,05 | +0,01 | 0,00 | 23,3 | 10,6 |

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
| Aposentado | 20,0 | 9,2 | 28,3 | — | 15,3 h | 0,0% | R$ 3.438 | R$ 37,2 |
| Autônomo / freelancer | 42,2 | 8,7 | 22,4 | 8,9 h | 15,1 h | 0,0% | R$ 4.312 | R$ 41,2 |
| Trabalho em horário comercial | 227 | 5,5 | 16,8 | 4,2 h | 14,7 h | 0,0% | R$ 4.417 | R$ 30,0 |
| Cuidando da casa e da família | 30,2 | 8,7 | 20,0 | 8,4 h | — | 0,0% | R$ 0,0 | R$ 2.264 |
| Comércio e serviços em escala 5x2 | 82,4 | 5,1 | 14,7 | 3,3 h | 14,9 h | 0,0% | R$ 2.595 | R$ 31,2 |
| Comércio em escala 6x1 | 37,0 | 5,0 | 14,0 | 4,8 h | 15,0 h | 0,0% | R$ 2.721 | R$ 27,8 |
| Estudante (manhã) | 108 | 7,7 | 24,2 | 8,4 h | 14,7 h | 0,0% | R$ 0,0 | R$ 732 |
| Estudante (tarde) | 46,2 | 7,9 | 23,1 | 8,7 h | 15,2 h | 0,0% | R$ 0,0 | R$ 749 |
| Home office (tecnologia) | 80,2 | 7,4 | 19,4 | 6,4 h | 15,1 h | 0,0% | R$ 8.225 | R$ 16,7 |
| Meio período (manhã) | 30,0 | 7,7 | 23,8 | 9,5 h | 14,8 h | 0,0% | R$ 1.964 | R$ 291 |
| Meio período (tarde) | 25,6 | 8,1 | 22,2 | 9,3 h | 15,1 h | 0,0% | R$ 1.962 | R$ 290 |
| Sem ocupação | 80,8 | 9,8 | 32,6 | — | 15,3 h | 0,0% | R$ 0,0 | R$ 832 |
| Trabalha de dia e estuda à noite | 56,2 | 2,5 | 7,9 | 1,0 h | 12,9 h | 53,1% | R$ 3.388 | R$ 52,4 |
| Turno noturno | 31,8 | 5,9 | 17,6 | 7,1 h | 11,4 h | 0,0% | R$ 3.611 | R$ 19,7 |
| Universitário (diurno) | 102 | 6,8 | 21,3 | 7,1 h | 14,8 h | 0,0% | R$ 522 | R$ 1.366 |

Como terminaram os dias da agenda:

| Dia | Parcela |
|---|---:|
| Jogou | 60,3% |
| Tinha tempo, preferiu outro lazer | 35,4% |
| Sem tempo livre | 3,0% |
| Inativo | 0,9% |
| Cansado (dormiu pouco) | 0,4% |
| Sem vontade (engajamento baixo) | 0,0% |

## Sono e dificuldade de parar (média das seeds, campanha completa)

Importância do jogo, tendência a prolongar e autocontrole são traços separados. A extensão tem limites explícitos (vezes por sessão, minutos, sono mínimo e teto do déficit); só corta sono no fim do dia, nunca uma obrigação. Sono cortado vira déficit; manhã livre e noites normais recuperam.

- Extensões na janela: 514 (182 gostando da sessão, 332 perto de concluir a meta); 514 avançaram sobre a hora de dormir.
- Noites encurtadas pelo jogo: 472, 72,4 min a menos em média.

| Perfil | Extensões/semana | Sono cortado/semana | Déficit no fim | Cansados |
|---|---:|---:|---:|---:|
| Casual | 0,0 | 0,4 min | 0,3 h | 2,1% |
| Comerciante | 0,0 | 1,2 min | 0,4 h | 2,8% |
| Competitivo | 0,0 | 3,1 min | 0,3 h | 2,1% |
| Devoto | 0,1 | 10,1 min | 0,0 h | 0,0% |
| Explorador | 0,0 | 2,5 min | 0,2 h | 1,6% |
| Hardcore | 0,2 | 13,4 min | 0,4 h | 3,4% |
| Social | 0,0 | 2,2 min | 0,2 h | 1,9% |

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
| Trabalha de dia e estuda à noite | 7,9 h | 6,9 h | 9,7 h | 4,6 h | 38,9% |
| Turno noturno | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |
| Universitário (diurno) | 7,8 h | 7,8 h | 7,8 h | 0,0 h | 0,0% |

## Jogar no horário de trabalho (média das seeds)

Só no trabalho remoto. Pausa: jogar no intervalo permitido. Escapada: jogar no expediente, rara, pela flexibilidade do trabalho, importância do jogo, falta de autocontrole e folga do dia; sessão curta e interrompível. Deslocamento, trabalho presencial e estudo ficam protegidos. Sem consequência profissional por enquanto.

- Sessões na pausa: 1.192 (1,5% das sessões); escapadas: 237 (0,3%).
- Chamados do trabalho durante a escapada: 82,0 (6,4 terminaram a atividade, 75,6 largaram e saíram).

| Ocupação | Pausa/semana | Escapadas/semana | Dias de trabalho com escapada |
|---|---:|---:|---:|
| Autônomo / freelancer | 0,8 | 0,2 | 3,3% |
| Home office (tecnologia) | 0,8 | 0,1 | 2,9% |

## Distribuições (R3)

Medição da campanha completa (com humor), seeds somadas, na janela de medição. A referência antes da R4 (modelo de vontade) está em `docs/baselines/pre-R4/`, com o mesmo relatório e o cenário controlado (pessoas iguais por célula perfil × ocupação), que separa o efeito das regras da mistura da população.

**Duração das sessões (minutos, sessões normais).** Inclui extensões e o fim real; pausa e escapada do trabalho à parte.

| Perfil | Sessões | p10 | p25 | Mediana | p75 | p90 | Máx. | Média |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 105.053 | 66,0 | 90,0 | 127 | 173 | 212 | 508 | 134 |
| Comerciante | 39.554 | 91,0 | 131 | 179 | 230 | 280 | 619 | 183 |
| Competitivo | 58.758 | 91,0 | 131 | 179 | 230 | 282 | 652 | 184 |
| Devoto | 11.509 | 117 | 229 | 326 | 442 | 522 | 967 | 330 |
| Explorador | 44.709 | 89,0 | 125 | 172 | 221 | 273 | 656 | 177 |
| Hardcore | 51.397 | 111 | 175 | 234 | 295 | 361 | 752 | 236 |
| Social | 80.265 | 90,0 | 129 | 179 | 232 | 283 | 720 | 184 |
| Pausa no trabalho | 5.961 | 41,0 | 55,0 | 55,0 | 55,0 | 55,0 | 55,0 | 51,7 |
| Escapada no expediente | 1.183 | 9,0 | 17,0 | 23,0 | 26,0 | 29,0 | 34,0 | 21,3 |

**Horas por semana, perfil × ocupação** (por pessoa registrada; "—" com menos de 10 pessoas na soma das seeds).

| Ocupação | Casual | Comerciante | Competitivo | Devoto | Explorador | Hardcore | Social |
|---|---:|---:|---:|---:|---:|---:|---:|
| Aposentado | 19,2 | 30,9 | 33,5 | — | 36,3 | — | 31,6 |
| Autônomo / freelancer | 13,2 | 24,9 | 26,4 | — | 23,9 | 43,3 | 26,7 |
| Trabalho em horário comercial | 9,7 | 17,5 | 18,3 | — | 20,0 | 32,5 | 17,7 |
| Cuidando da casa e da família | 10,3 | 19,6 | 19,1 | — | 20,5 | 34,5 | 22,2 |
| Comércio e serviços em escala 5x2 | 8,4 | 16,4 | 17,6 | — | 18,3 | 27,1 | 14,7 |
| Comércio em escala 6x1 | 9,4 | 15,2 | 13,1 | — | 17,1 | 27,7 | 13,7 |
| Estudante (manhã) | 13,5 | 25,6 | 25,0 | 54,4 | 28,0 | 41,6 | 25,7 |
| Estudante (tarde) | 12,4 | 20,2 | 20,9 | 50,4 | 25,7 | 40,3 | 25,9 |
| Home office (tecnologia) | 11,2 | 21,9 | 22,7 | — | 19,7 | 37,1 | 22,6 |
| Meio período (manhã) | 12,7 | 28,2 | 28,8 | — | 26,3 | 47,1 | 28,1 |
| Meio período (tarde) | 12,4 | 23,3 | 23,5 | — | 29,8 | 42,1 | 24,5 |
| Sem ocupação | 19,2 | 31,8 | 29,9 | 67,6 | 33,5 | 47,6 | 32,6 |
| Trabalha de dia e estuda à noite | 4,9 | 8,7 | 8,6 | — | 8,9 | 14,5 | 8,5 |
| Turno noturno | 11,0 | 17,0 | 17,5 | — | 18,3 | 32,8 | 18,0 |
| Universitário (diurno) | 12,3 | 22,6 | 23,4 | 50,9 | 24,1 | 40,1 | 20,9 |

**Sessões por dia de jogo:** 58,4% com uma, 31,0% com duas, 10,7% com três ou mais. Intervalo entre sessões do mesmo dia: mediana 82,0 min, p90 205 min.
**Voltas depois de uma pausa (v3):** em 39,6% dos dias com jogo. Sessões normais por dia com jogo: uma 58,9%, duas 30,8%, três ou mais 10,3%. Voltas consideradas e recusadas: 34.456. Sessão da noite deixada de lado depois de jogar no trabalho: 34 dias.

**Por que as sessões começaram e terminaram** (sessões normais, seeds somadas).

| Início | Parcela | | Fim | Parcela |
|---|---:|---|---|---:|
| Antes do horário, por vontade | 45,4% | | Decidiu parar (saciedade, cansaço, outras opções) | 70,7% |
| Voltou depois de uma pausa | 34,5% | | Hora de dormir | 22,7% |
| No horário de costume | 20,0% | | Fim do tempo livre (obrigação, preparo, compromisso) | 5,8% |
|  |  | | Depois de ficar acordado jogando | 0,6% |
|  |  | | Fim planejado (sem decisão) | 0,2% |

**Sessões mais longas e por quê** (as 8 maiores sessões normais, seeds somadas). Resto: terminar a atividade ou a viagem.

| Perfil | Ocupação | Duração | Ao começar | Continuou | Ficou acordado | Resto | Disposição | Tempo livre do dia |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Devoto | Estudante (tarde) | 967 min | 567 min | 220 min | 180 min | 0 min | −2% | 15,8 h |
| Devoto | Estudante (manhã) | 827 min | 390 min | 437 min | 0 min | 0 min | −0% | 15,6 h |
| Devoto | Sem ocupação | 824 min | 403 min | 420 min | 0 min | 1 min | +12% | 15,7 h |
| Devoto | Sem ocupação | 815 min | 514 min | 300 min | 0 min | 1 min | +6% | 15,3 h |
| Devoto | Estudante (manhã) | 811 min | 485 min | 325 min | 0 min | 1 min | −5% | 14,8 h |
| Devoto | Sem ocupação | 810 min | 410 min | 390 min | 0 min | 10 min | +13% | 15,8 h |
| Devoto | Sem ocupação | 810 min | 420 min | 390 min | 0 min | 0 min | +1% | 15,1 h |
| Devoto | Estudante (manhã) | 807 min | 521 min | 275 min | 0 min | 11 min | −4% | 14,4 h |


**Quanto do tempo livre vira jogo** (minutos jogados ÷ minutos livres da agenda; o jogo no trabalho e as extensões entram no jogado).

| Perfil | Uso |
|---|---:|
| Casual | 16,3% |
| Comerciante | 30,4% |
| Competitivo | 30,7% |
| Devoto | 64,6% |
| Explorador | 31,7% |
| Hardcore | 51,5% |
| Social | 30,5% |

| Ocupação | Uso |
|---|---:|
| Aposentado | 24,6% |
| Autônomo / freelancer | 27,6% |
| Trabalho em horário comercial | 29,2% |
| Cuidando da casa e da família | 31,6% |
| Comércio e serviços em escala 5x2 | 28,9% |
| Comércio em escala 6x1 | 29,7% |
| Estudante (manhã) | 30,6% |
| Estudante (tarde) | 28,5% |
| Home office (tecnologia) | 28,0% |
| Meio período (manhã) | 28,0% |
| Meio período (tarde) | 26,4% |
| Sem ocupação | 28,0% |
| Trabalha de dia e estuda à noite | 20,9% |
| Turno noturno | 27,5% |
| Universitário (diurno) | 29,8% |

**Composição do tempo jogado:** 1.184.541 h no total; extensões 0,2%; pausa e escapada do trabalho 0,5%; sono cortado pelo jogo 2.849 h (0,2% do tempo jogado).

**Dias sem sessão principal** (39,7% dos dias da agenda):

| Motivo | Parcela dos dias sem sessão |
|---|---:|
| Sem oportunidade (nenhuma janela comporta uma sessão) | 7,5% |
| Escolha: tinha tempo, preferiu outro lazer | 87,5% |
| Cansaço | 1,0% |
| Desânimo (engajamento baixo) | 0,1% |
| Inativo | 2,3% |
| Jogou só no horário de trabalho | 1,6% |

**Efeito do tempo livre, separado** (dias da agenda por faixa de tempo livre): a frequência (chance de jogar) e a duração (minutos planejados quando joga) medidas à parte.

| Tempo livre no dia | Dias | Jogou | Duração planejada quando joga | Jogado por dia |
|---|---:|---:|---:|---:|
| até 2 h | 4.473 | 0,0% | 109 min | 0,0 min |
| 2–4 h | 19.965 | 48,8% | 157 min | 84,9 min |
| 4–6 h | 23.984 | 49,5% | 168 min | 98,5 min |
| 6–10 h | 43.075 | 60,2% | 160 min | 163 min |
| 10 h ou mais | 58.503 | 73,3% | 172 min | 223 min |

**Corte de sono (observações do criador, R2/R3).**

- **Onde pode cortar:** só a janela que termina **por causa do sono** avança sobre ele. Janela que termina por obrigação, preparo, deslocamento ou compromisso não avança (teste).
- **Teto compartilhado:** os 180 minutos por período de sono valem para a soma de todas as sessões que afetam aquele período (teste).
- **Medido:** 2.361 períodos de sono encurtados pelo jogo; corte por período: mediana 68,0 min, p90 124 min, máximo 180 min.

## Localidades (média das seeds)

Cada pessoa joga no horário e nos feriados de onde mora; o relógio do jogo é o do servidor. O início médio é uma média circular das horas (a meia-noite não distorce), então local e servidor diferem exatamente pelo fuso.

| Localidade | Fuso | Pessoas | Estrangeiros | Sessões | Início médio (local) | Início médio (servidor) |
|---|---|---:|---:|---:|---:|---:|
| Argentina | UTC−3 | 85,4 | 2,2 | 6.895 | 16h25 | 16h25 |
| Brasil | UTC−3 | 758 | 19,0 | 60.745 | 16h29 | 16h29 |
| China | UTC+8 | 75,4 | 0,8 | 5.930 | 16h33 | 05h33 |
| Estados Unidos | UTC−5 | 81,4 | 12,6 | 6.168 | 16h37 | 18h37 |

## Retenção (média das seeds, no fim)

| Perfil | Desistiram | Inativos | Voltaram alguma vez |
|---|---:|---:|---:|
| Casual | 1,2% | 2,0% | 4,3% |
| Comerciante | 0,7% | 0,7% | 1,3% |
| Competitivo | 0,7% | 1,2% | 1,5% |
| Devoto | 0,0% | 0,0% | 0,0% |
| Explorador | 0,2% | 1,4% | 1,2% |
| Hardcore | 0,0% | 0,2% | 0,6% |
| Social | 0,3% | 1,0% | 1,2% |

Desistências: dia mediano 62,0, nível mediano 7,0. Sessões estendidas por estarem indo bem: 0,6%; encerradas por frustração: 0,0%.

Motivo principal das desistências (o que mais incomodava; "long_inactivity/" = sumiu por muito tempo):

| Motivo | Desistências |
|---|---:|
| long_inactivity/long_inactivity | 84,8% |
| long_inactivity/boredom | 9,1% |
| boredom | 6,1% |

## Humor e memória (média das seeds, no fim)

Satisfação, frustração, novidade e engajamento em permille (0–1000); progresso em permille do XP de referência do nível.

| Perfil | Satisfação | Insatisfeitos (< 400) | Frustração | Novidade | Engajamento | Intenção de continuar | Expectativa | Progresso da última sessão | Memórias |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 593 | 0,0% | 5,8 | 969 | 711 | 639 | 613 | 558 | 10,4 |
| Comerciante | 551 | 0,0% | 14,7 | 911 | 675 | 585 | 455 | 171 | 10,1 |
| Competitivo | 589 | 0,0% | 9,5 | 873 | 698 | 634 | 724 | 544 | 9,7 |
| Devoto | 513 | 0,0% | 0,0 | 116 | 570 | 623 | 869 | 521 | 2,9 |
| Explorador | 632 | 0,0% | 1,6 | 852 | 727 | 682 | 592 | 454 | 8,9 |
| Hardcore | 571 | 0,0% | 0,3 | 613 | 660 | 660 | 866 | 578 | 7,7 |
| Social | 584 | 0,0% | 12,9 | 850 | 692 | 671 | 453 | 62,5 | 8,9 |

Sessões ótimas: 9,6% · ruins: 0,0% (após a semana de lançamento).

O que mais incomoda cada pessoa (motivo mais negativo acumulado):

| Motivo | Pessoas |
|---|---:|
| boredom | 98,1% |
| fatigue | 0,8% |
| nenhum | 0,8% |
| progress | 0,3% |

## Uso do tempo conectado (todas as seeds)

Caça, chefe, patrulha e emboscada ainda são atividades abstratas (tempo e XP, sem luta simulada) até o M3; aí o tempo se divide em viagem, procura, luta, recuperação e recompensa.

| Tipo | Tempo | Escolhas |
|---|---:|---:|
| Caça | 27,7% | 28,2% |
| Deslocamento | 22,1% | — |
| Socializar | 19,0% | 18,5% |
| Exploração | 7,7% | 7,6% |
| Comércio | 6,8% | 13,2% |
| Coleta | 5,4% | 8,0% |
| Patrulha | 4,7% | 4,7% |
| Emboscada | 3,1% | 4,7% |
| Missão | 2,1% | 10,6% |
| Treino | 1,3% | 4,2% |
| Conserto | 0,1% | 0,4% |

## Continuidade das sessões (média das seeds)

| Medida | Valor |
|---|---:|
| Escolhas por sessão | 6,2 |
| Continuou na mesma atividade (% das escolhas após a primeira) | 74,2% |
| Trocas de atividade por hora conectada | 0,5 |
| Trocas de zona por sessão | 0,7 |
| Ciclos seguidos na mesma atividade (média) | 2,7 |

Motivos das trocas de plano (fator principal):

| Motivo | Trocas |
|---|---:|
| quest_step | 26,9% |
| quest_completed | 21,8% |
| time_budget | 10,7% |
| focus_unavailable | 9,9% |
| boredom | 9,0% |
| level_up | 7,6% |
| goal_completed | 5,8% |
| affinity | 3,3% |
| nothing_new | 1,3% |
| zone_discovered | 0,9% |
| quest_deliver | 0,9% |
| goal | 0,7% |
| quest_offer | 0,7% |
| level_fit | 0,3% |
| crowding | 0,1% |
| company | 0,1% |
| risk | 0,0% |
| challenge | 0,0% |
| memory | 0,0% |
| familiarity | 0,0% |
| noise | 0,0% |
| interruption | 0,0% |

## Atividades pouco usadas (menos de 1% das escolhas)

| Zona | Atividade | Tipo | Escolhas | Diagnóstico |
|---|---|---|---:|---|
| Cidadela de Cinzaforte | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Covil do Rei Goblin | Enfrentar o Rei Goblin | `boss` | 0,0% | exige grupo de 5; grupos chegam no M6, então hoje ninguém pode fazer. |
| Fortaleza Sombria | Enfrentar o Senhor das Cinzas | `boss` | 0,0% | exige grupo de 10; grupos chegam no M6, então hoje ninguém pode fazer; nível mínimo 28: só 41% dos personagens chegaram até o fim. |
| Forte do Vau | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Observar a forja vulcânica | `quest` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Recrutar para grupo | `recruit` | 0,0% | recrutamento depende do sistema de grupos (M6); atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Ruínas Sombrias | Coletar cristais sombrios | `gather` | 0,0% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Vila Aurora | Consertar equipamento | `repair` | 0,0% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Explorar trilhas da floresta | `explore` | 0,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Consertar equipamento | `repair` | 0,1% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Explorar as terras queimadas | `explore` | 0,1% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Fortaleza Sombria | Ler as inscrições da cripta | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Patrulhar as ruínas | `patrol` | 0,1% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Explorar galerias profundas | `explore` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Picos Gelados | Investigar o covil do yeti | `quest` | 0,1% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Planalto dos Ventos | Espiar o acampamento orc | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Caçar mortos-vivos | `hunt` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Caçar aranhas tecelãs | `hunt` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ruínas Sombrias | Examinar o altar profanado | `quest` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mata dos Espinhos | Seguir os rastros dos trolls | `quest` | 0,2% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Procurar o estandarte perdido | `quest` | 0,2% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Brejo das Brumas | Procurar o ninho das serpentes | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Investigar as galerias abandonadas | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Examinar os ninhos das harpias | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Falar com o lenhador | `quest` | 0,3% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Cidadela de Cinzaforte | Consertar equipamento | `repair` | 0,3% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Terras Queimadas | Minerar obsidiana | `gather` | 0,3% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Colher ervas das encostas | `gather` | 0,4% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Socializar na taverna | `socialize` | 0,5% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Mina Velha | Minerar ferro e prata | `gather` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Emboscar viajantes | `ambush` | 0,5% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Floresta Sussurrante | Coletar ervas curativas | `gather` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Caçar lobos das colinas | `hunt` | 0,5% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Ravina Escarlate | Patrulhar a ravina | `patrol` | 0,6% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Ravina Escarlate | Caçar esqueletos da ravina | `hunt` | 0,6% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Mina Velha | Caçar morcegos gigantes | `hunt` | 0,6% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Floresta Sussurrante | Caçar lobos | `hunt` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Forte do Vau | Treinar com o mestre de armas | `train` | 0,7% | atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa. |
| Floresta Sussurrante | Caçar javalis | `hunt` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Mina Velha | Caçar goblins | `hunt` | 0,7% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Brejo das Brumas | Explorar o brejo | `explore` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Colinas do Vale | Caçar ursos pardos | `hunt` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Caçar salamandras de fogo | `hunt` | 0,8% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Colinas do Vale | Explorar as colinas | `explore` | 0,8% | pouco atraente na fórmula atual (rever na conferência da fórmula). |
| Terras Queimadas | Enfrentar golens de magma | `hunt` | 0,8% | zona PvP: o risco extra afasta quem tem aversão a risco. |
| Fortaleza Sombria | Purgar a cripta | `hunt` | 1,0% | nível mínimo 26: só 48% dos personagens chegaram até o fim. |
| Colinas do Vale | Caçar harpias | `hunt` | 1,0% | pouco atraente na fórmula atual (rever na conferência da fórmula). |

## Execuções

| Seed | Hash final | Simulação |
|---:|---|---:|
| 1 | `e4efad249f360f54` | 321,0 s |
| 2 | `0cabc2559883e4cb` | 327,9 s |
| 3 | `42adb5f9436fa111` | 325,4 s |
| 4 | `9655d43543c64ab9` | 323,1 s |
| 5 | `7c373e48d72041b7` | 317,0 s |

Medições de tempo desta máquina, não benchmark.
