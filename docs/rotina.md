# Rotina da pessoa simulada: sistema, cálculos e números

Referência do sistema de rotina, das decisões P1, P2 e P3 até a D-052 (revisão R1 a R5, com a vontade de jogar da v3). A seção 9 separa o que é invariante da simulação, o que é regra do preset inicial e o que é tendência estatística. Todos os números estão em `Content/presets/inicial/population.json` (seção `routine` e perfis) e em `occupations.json`; o validador confere as faixas. Permille: 1000 = 100%.

## 1. Visão geral: como as peças se ligam

```
Perfil ──► traços (importância, hábito, sessão base, gosto de horário, tendência a prolongar, autocontrole, sociabilidade)
Localidade ──► fuso e feriados
Ocupação ──► obrigações (horário individual, deslocamento, folga)
                 │
                 ▼
        DIA MONTADO (todo dia, meia-noite local)
   sono · obrigações · deslocamento · preparo · chegada · compromisso
                 │
                 ▼
          JANELAS LIVRES ──► jogar hoje? ──► quando começar ──► sessão
                 │                  ▲                               │
                 │   hábito · disposição · humor · cansaço          ▼
                 │                          continuar · parar · voltar · ficar acordado
                 ▼                                         │
         AGENDA (30 dias) ◄──────── déficit de sono ◄──────┘
```

A regra que amarra tudo (D-045) é a coerência: turno, sono, hora preferida, duração da sessão e vontade de jogar contam a mesma história. Quando um valor sorteado contradiz outro, ele é ajustado. A interface mostra os valores reais calculados, não as preferências cruas.

## 2. Ocupações (`occupations.json`)

| Ocupação | Peso | Horário base | Desloc. (por trecho) | Variação individual | Trabalha em feriado | Observações |
|---|---:|---|---:|---:|---|---|
| Horário comercial | 24 | seg–sex 08:00–17:00 | 50 min | ±60 min | não | — |
| Estudante (manhã) | 12 | seg–sex 07:00–12:30 | 30 | ±20 | não | — |
| Universitário | 10 | seg–sex 08:00–12:00; ter/qui 14:00–17:30 | 45 | ±60 | não | aula da tarde no campus, direto depois de 1 h de almoço |
| Home office | 8 | seg–sex 09:00–18:00 | 0 | ±60 | não | remoto, pausa 12:00–13:00, flexibilidade 600 |
| Sem ocupação | 8 | — | — | — | — | todo dia livre |
| Escala 5x2 | 9 | seg–sex 09:00–18:50 | 45 | ±90 | sim | folga sempre no sábado e no domingo |
| Escala 6x1 | 4 | 10:00–18:20, seis dias | 45 | ±90 | sim | uma folga fixa em qualquer dia (inclusive domingo) |
| Trabalha e estuda | 6 | seg–sex 08:00–17:00 + seg–qui 19:00–22:30 | 45 (ida) / 40 (volta da faculdade) | ±30 | não | trabalho → refeição 45 min → transferência 30 min → faculdade → casa |
| Estudante (tarde) | 5 | seg–sex 13:00–18:00 | 30 | ±20 | não | — |
| Autônomo | 5 | 10:00–16:30, cinco dias entre seg e sáb | 0 | ±120 | sim | remoto, pausa 12:30–13:30, flexibilidade 700, folga fixa |
| Turno noturno | 4 | seg–sex 22:00–06:00 | 35 | ±30 | sim | dorme de manhã |
| Meio período (manhã) | 3 | seg–sex 08:00–12:00 | 40 | ±30 | não | — |
| Meio período (tarde) | 3 | seg–sex 13:30–17:30 | 40 | ±30 | não | — |
| Cuidando da casa | 3 | todo dia 06:30–08:30, 11:30–13:30, 17:00–20:00 | 0 | ±30 | sim | — |
| Aposentado | 2 | — | — | — | — | todo dia livre |

- **Deslocamento:** varia ±30% por pessoa.
- **Trajeto de cada bloco** (`route`): `home` sai de casa e volta para casa; `fromPrevious` vem direto do bloco anterior. Neste caso há refeição (`mealMinutes`) e transferência (`transferMinutes`), e o resto é espera fora de casa.
- **Validador:** nenhum turno diurno pode exigir acordar antes das 04:30, mesmo com a maior variação e o deslocamento mais longo.

## 3. Criação da pessoa (uma vez)

1. **Traços do perfil** (média ± faixa):

   | Perfil | Peso (parcela) | Importância do jogo | Hábito | Sessão normal | Gosto de horário | Tendência a prolongar | Autocontrole |
   |---|---:|---:|---:|---:|---:|---:|---:|
   | Casual | 35 (34,3%) | 350 ±150 | 400 ±200 | 80 ±45 min | 20h ±3 | 300 ±300 | 600 ±300 |
   | Social | 20 (19,6%) | 500 | 550 | 110 ±60 | 20h ±3 | 500 | 550 |
   | Explorador | 10 (9,8%) | 550 | 500 | 120 ±60 | 15h ±6 | 500 | 550 |
   | Comerciante | 10 (9,8%) | 600 | 650 | 90 ±50 | 19h ±4 | 400 | 650 |
   | Competitivo | 15 (14,7%) | 650 | 600 | 150 ±80 | 21h ±2 | 650 | 450 |
   | Hardcore | 10 (9,8%) | 820 ±100 | 750 | 240 ±100 | 19h ±4 | 800 | 400 |
   | Devoto (R5) | 2 (2,0%) | 930 ±70 | 900 ±100 | 360 ±120 | 16h ±5 | 650 ±350 | 500 ±350 |

   A parcela é o peso dividido pela soma dos pesos (102). O **Devoto** é o jogador extremamente dedicado. Os outros traços seguem o hardcore, com lealdade 750 ±200. Não tem regra própria na simulação (há um teste para isso): joga mais porque a prioridade alta e a sessão normal longa entram nas mesmas decisões de todos, com os mesmos limites de obrigações, sono e cansaço.

   Importância, tendência a prolongar e autocontrole são sorteados de forma independente dentro do perfil (o teste mede correlação ≈ 0).
2. **Localidade:** residência e fuso. Brasil UTC−3 (peso 760), EUA UTC−5 (80), China UTC+8 (70), Argentina UTC−3 (90). Os feriados são os de onde a pessoa mora.
3. **Ocupação:** pelo peso, com a variação de horário e de deslocamento e a folga da escala.
   - Um perfil pode ter ocupações próprias (`occupationWeights`, regra do preset). No preset inicial, só o Devoto tem: sem ocupação 40, universitário 25, estudante de manhã 20, estudante de tarde 15.
   - Os outros perfis usam o peso geral, independente do perfil: ter tempo não é ser hardcore.
4. **Sono:**
   - necessidade de 470 ±45 min (5 a 11 h);
   - hora natural de dormir 23:45 ±1h15;
   - a hora natural **se adapta às obrigações**: fica no máximo 60 min (`bedtimeAdaptMinutes`) depois da hora típica de deitar antes de um dia de obrigação. O turno da noite segue o próprio ritmo.
5. **Renda e apoio:** faixa da ocupação × fator da localidade (Brasil 1,0, Argentina 0,85, China 1,3, EUA 4,2). Sem efeito até o M7.
6. **Tolerância ao déficit de sono:** 500 ±200.
7. **Gosto de horário × horário habitual** (D-045; separados na R4):
   - **Gosto:** a hora sorteada pelo perfil (`preferredStartHour`) fica guardada como está. É o que a pessoa preferiria.
   - **Horário habitual** (`HabitualStartHour`, calculado, `ViableStartHour`): o que a rotina permite e a pressa de começar escolhe. É ele que o planejador usa. Uma pessoa pode gostar das 20h e normalmente só conseguir às 21h.
   - **Dias do hábito:** os dias com obrigação. A sessão considerada fica limitada à maior janela livre deles. Sem tempo nesses dias, valem os dias livres.
   - **Alvo do dia:** em cada dia do hábito, na janela mais perto do gosto:
     - falta de interesse = (1 − importância)²;
     - ansioso = início da janela + falta de interesse × 240 min;
     - alvo = ansioso + (max(gosto, ansioso) − ansioso) × falta de interesse.
     - O alvo vai até onde a sessão ainda cabe inteira antes de dormir.
   - **Escolha:** a hora cheia em que a sessão começa na hora (ou até 59 min depois, se a janela abre um pouco depois) e cabe inteira, no maior número de dias do hábito. Em empate, a mais perto do alvo.
   - **Exemplos de início:**

     | Quem | Chega em casa | Começa |
     |---|---|---|
     | Hardcore (importância 820) | 18:35 | ~19h |
     | Casual (importância 300) | 18:35 | ~20h30–21h |
     | Hardcore de meio período de manhã | 13:25 | ~14h |
8. **Previsto por semana:** soma do previsto de cada dia (seção 5).

## 4. O dia montado (todo dia, à meia-noite local) — R1, D-047

**Dia civil × dia acordado:**
- O calendário, os feriados, a agenda e os relatórios usam o **dia civil**, de meia-noite a meia-noite.
- O **dia acordado** é só a organização interna do cálculo.
- O ciclo de sono atravessa as fronteiras do dia civil. Por isso relatórios e a "semana em 168 horas" recortam os ciclos numa janela fixa de dias civis (`CivilWindow`), com cada minuto contado uma única vez.
- No turno noturno de sexta, as horas depois da meia-noite (o trabalho, a volta para casa e o sono) pertencem ao **sábado civil**.

`DailySchedule.Build` monta o **dia acordado**: do início do sono que termina hoje (`SleepStart`, que pode ser na véspera) até a hora de dormir (`Bed`, que pode passar da meia-noite). O dia seguinte começa onde este termina, então cada minuto pertence a um único dia e a semana fecha 7 × 24 h.

1. **Início do sono que termina hoje:** é o fim do dia acordado de ontem.
   - Depois de um turno da noite: fim do turno + deslocamento + chegada (de manhã).
   - Nos outros dias: a hora natural de dormir, cedo o bastante para a obrigação de hoje, e nunca antes de voltar e comer.
2. **Acordar:** a tempo da primeira obrigação (início − deslocamento − 45 min de preparo), ou depois do sono necessário. Se sobrar manhã e houver déficit, soma a recuperação (até 2 h, decidida no planejamento e guardada na agenda).
3. **Obrigações:**
   - De casa: deslocamento antes, obrigação, deslocamento depois e 45 min de chegada (comer, descansar).
   - Em sequência direta: refeição, transferência e espera fora de casa até o próximo bloco.
   - Blocos sem deslocamento (home office, autônomo, cuidando da casa) não têm chegada.
4. **Fim do dia (`Bed`):**
   - Depois de um turno da noite, a volta para casa e a chegada.
   - Nos outros dias, a hora natural de dormir, limitada pela obrigação de amanhã (feriado e folga de amanhã liberam) e nunca antes de voltar e comer.
5. **Compromisso do dia livre** (D-046), só em fim de semana, feriado ou folga:

   | Tipo | Início | Duração | Peso | Social |
   |---|---|---|---:|---|
   | festa | 20:00–21:30 | 2h30–4h | 3 | sim |
   | visita à família | 11:00–15:00 | 2h30–5h | 4 | não |
   | passeio com amigos | 14:00–17:00 | 2–4h | 3 | sim |
   | resolver pendências | 09:00–12:00 | 1h30–3h | 2 | não |

   - Chance: 7% × (0,6 + 0,8 × sociabilidade). O sorteio é próprio da pessoa e do dia.
   - O compromisso termina 15 min antes de dormir, seguido de chegada.
   - **Simplificação registrada:** compromissos ainda não cortam sono.
6. **Partes sem sobreposição e janelas livres:** as partes acordadas (preparo, deslocamento, obrigação, refeição, transferência, espera, chegada, compromisso) nunca se sobrepõem. As janelas livres são o que sobra entre elas (mínimo 30 min). `Timeline()` pinta cada minuto do dia uma única vez; as brechas curtas viram "acordado".

## 5. Vontade de jogar no dia

**Escalas:**
- Atributos e probabilidades vão de 0 a 1000 (permille: 1000 = 100%).
- Fatores multiplicam em permille (1000 = ×1).
- Frações como "35%" aparecem no texto já convertidas.

**Chance base** (`BaseChance`), já uma probabilidade de 0 a 1000:
- folga = min(1000, minutos livres × 1000 ÷ 600);
- base = importância + (1000 − importância) × 350 ÷ 1000 × folga ÷ 1000. O tempo livre ajuda mais quem liga pouco;
- carga = min(1000, minutos de obrigação × 1000 ÷ 600);
- base × (1000 − carga × 650 ÷ 1000 × (1000 − importância) ÷ 1000) ÷ 1000. O dia cheio cansa, menos para quem prioriza o jogo.

**Probabilidade do dia** (R2): todos os fatores são **combinados e a probabilidade final é limitada a 0–1000 uma única vez, no fim**. Assim o resultado não depende da ordem, e um bônus não é cortado antes de uma penalidade. Os fatores:
1. **Hábito** (R4): × (1000 + hábito × 250 ÷ 1000 × (2 × regularidade − 1000) ÷ 1000).
   - A **regularidade** (`Regularity`, 0–1000) vem da agenda dos últimos 7 dias (`habitHistoryDays`), com os mais recentes pesando mais (peso 7 para ontem, 1 para seis dias atrás).
   - **Contam como jogo:** sessão planejada (mesmo interrompida) ou jogo no trabalho.
   - **Contam como falta:** outro lazer, cansaço, desânimo e inatividade.
   - **Neutro:** dia sem oportunidade, sem janela ou só com obrigação. Sem dias para medir, a regularidade é 500.
   - O traço (hábito) diz quanto o histórico pesa: regularidade total dá até +25%, nenhuma dá até −25%, e meio a meio não muda nada.
2. **Disposição do dia** (R4): × (1000 + disposição). A disposição é um sorteio pequeno e centrado (média de três uniformes em ±200), próprio de cada pessoa e dia (`routine.disposition`): o mesmo dia dá sempre o mesmo valor. Representa querer descansar ou fazer outra coisa, mesmo gostando do jogo.
3. **Humor:** × fator do engajamento, 1000 + (engajamento − 700), entre 150 e 1300. Desligado no cenário controlado.
4. **Inativo:** × 250 × intenção de continuar ÷ 1000.
5. **Cansaço:** × (1000 − cansaço × 400 ÷ 1000).

A importância do jogo entra **só na prioridade**: na chance base, na segunda sessão, na pausa do trabalho e na pressa de começar. Não muda a duração.

A importância do jogo é a **prioridade** dele entre os lazeres. Ela pesa em cada decisão (jogar no dia, começar antes, continuar, voltar, jogar na pausa do trabalho) e não muda a duração normal da sessão.

### Decisões ao longo do dia (v3, D-051)

Depois de decidir jogar hoje (o sorteio acima, um por dia), a pessoa toma decisões separadas: **quando começar**, **continuar ou parar** e **voltar** depois de uma pausa. Todas usam a mesma comparação:

> chance = vontade de jogar ÷ (vontade de jogar + outras opções + vontade de parar)

- **Prioridade dentro do dia** (`SessionPriority`): importância + (1000 − importância) × 35% × folga. É a chance base sem o peso do dia de obrigações, que entra uma vez só, na chance de jogar no dia.
- **Vontade de jogar** (`Pull`, até 2000): prioridade × disposição do dia × humor (engajamento) × experiência; para continuar, também × tendência a prolongar (±20%).
  - **A experiência** é a nota da sessão comparada com a satisfação habitual da pessoa: igual ao habitual não muda nada. Assim o humor, que já vem da média das notas, não conta duas vezes.
  - Os fatores são combinados e limitados uma vez.
- **Outras opções:** 1000 − prioridade (o resto do lazer).
- **Vontade de parar** (`StopPressure`): saciedade + cansaço do sono × 40%.

Sem saciedade, sem cansaço e com tudo neutro, a chance é a própria prioridade do jogo.

**Saciedade** (`Satiety`, 0–1000): cresce enquanto a pessoa joga e cai nas pausas. Ela **aumenta a vontade de parar**. Não existe cota nem total do dia.
- **Jogando:** o que falta até 1000 cai pela metade a cada 1,5 sessão normal (`satietyHalfLifeSessionsPermille` 1500). Depois de uma sessão normal, a saciedade fica em 37%; depois de três, em 75%.
- **Em pausa:** cai pela metade a cada 2 h (`satietyRecoveryHalfLifeMinutes` 120).
- **Meia-vida (R5):** o resultado não depende de quantas vezes se atualiza, seja por blocos de decisão ou por pausas divididas. A conta é inteira, por uma tabela fixa de 2^(−k/64), e arredonda para o mais próximo.
- **Ao acordar:** zera.
- **O que conta:** todo o tempo jogado, inclusive no trabalho e o interrompido. A frustração de uma interrupção pelo trabalho entra à parte, como vontade de terminar (+150 na vontade de jogar da próxima decisão).

**Quando começar** (`StartMinute`): um sorteio só por dia, num fluxo próprio (`routine.start`), sem tentativas independentes.
- A vontade de começar vale **por hora** e se acumula no tempo livre, em passos de 10 min (`startStepMinutes`, só arredonda o horário). A pessoa começa quando a chance acumulada passa do valor sorteado.
- O horário de costume é o gosto no dia livre e o horário habitual no dia de obrigação, cada um com a variação do dia de ±75 min.
- Antes dele há resistência: +300 na vontade de parar por hora de antecedência (`earlyStartResistancePerHour`).
- Toda janela livre conta, inclusive a de antes do trabalho (turno da noite, trabalho à tarde). No dia livre, a primeira janela começa depois da manhã (60 min).
- No horário de costume (ou no primeiro tempo livre depois dele) a pessoa começa. Se ele não cabe no dia, começa no último momento em que ainda cabe a sessão normal.
- A participação no dia é a chance do dia. O passo e a frequência das avaliações não a mudam: no cenário controlado, os dias com jogo são iguais antes e depois.

**Jogo no trabalho** (pausa e escapada): não desconta minutos da noite. A saciedade que sobrou e a experiência de lá podem fazer a pessoa desistir de jogar à noite (`SkippedAfterWork`), e pesam no continuar e no voltar.

**Continuar ou parar:**
- A primeira sessão vai até a sessão normal (`sessionMinutes`), ou até o fim da janela. A sessão normal é uma **preferência**, não um limite.
- Ao chegar nela, e depois a cada 30 min (`continueBlockMinutes`), a pessoa decide pela comparação.
- Continuar só cabe dentro do tempo livre. Com menos de 5 min de janela (`continueMinMinutes`), a decisão já não cabe, e vale a regra de terminar a atividade.
- Parar é sempre possível. Motivos de fim: decidiu parar, hora de dormir, fim do tempo livre.

**Ficar acordado jogando** (P3, só no fim do dia): quando a janela acaba na hora de dormir e a pessoa ainda está jogando, ela pode passar dela.
- Precisa de motivo explícito: gostando (nota parcial ≥ 700) ou perto da meta.
- Chance: 40% × tendência × (1 − autocontrole × 0,8) × (1 − cansaço).
- Sempre dentro do corte de sono autorizado (R2/R3).
- Não existe mais extensão dentro do tempo livre: essa parte é a decisão de continuar.

**Voltar:**
- Depois de parar (por vontade, fim do tempo livre ou frustração), a pausa é sorteada entre 30 e 120 min (`returnPauseMin/MaxMinutes`).
- A volta é na mesma janela, se ainda cabe a sessão mínima, ou no começo da próxima (depois da aula, do compromisso).
- A saciedade caiu na pausa. A decisão usa a experiência da última sessão.
- Se volta, joga até a sessão normal e decide de novo se continua.
- Até 5 sessões normais por dia (`maxSessionsPerDay`).

**Estimativa** (`ExpectWeek`, sem humor, cansaço, jogo no trabalho nem corte de sono): a mesma regra do planejador.
- A chance de cada dia usa a regularidade dos dias anteriores com oportunidade e a disposição amostrada; a semana se repete até estabilizar.
- Os minutos de um dia com jogo vêm de 24 dias simulados pela regra da v3 (`SimulateDay`: começar, continuar, parar e voltar), com sorteio fixo da pessoa. O resultado é sempre o mesmo.
- O início e o fim típicos (fim da última sessão) são as medianas.

A tela chama isso de "estimativa".

**Motivo do dia na agenda:**

| Motivo | Quando |
|---|---|
| Jogou | Fez a sessão principal |
| Jogou só no horário de trabalho | Sem sessão principal, mas jogou na pausa ou na escapada |
| Sem tempo livre | Nenhuma janela comporta 20 min |
| Cansado | Cansaço ≥ 50% |
| Sem vontade | Engajamento 150 abaixo do neutro |
| Tinha tempo, preferiu outro lazer | Nenhum dos anteriores |
| Inativo / Voltou | Situação de retenção |

## 6. Sono, déficit e cansaço (P3; contabilidade R1)

- **Noite contada uma vez, ao acordar** (evento `wake`): vai do início real até a hora de acordar. O início real é a hora de deitar planejada, ou a saída do jogo se foi depois. Os horários são ticks absolutos da simulação. A saída do jogo não mexe no déficit; só registra a hora.
- **Regra do déficit** (`DeficitChange`), para uma necessidade de 8 h:

  | Dormiu | Déficit |
  |---|---|
  | 7 h | +60 min |
  | 8 h | −20 min (recuperação explícita de noite completa, `recoveryNormalNightMinutes`) |
  | 9 h | −60 − 20 = −80 min |

  - A recuperação de manhã livre faz a pessoa acordar mais tarde, e por isso dormir mais; entra na mesma conta, sem duplicar.
  - Teto = 15 h × (0,5 + tolerância).
- **Cansaço de hoje:** usa a projeção da noite em andamento no planejamento. A conta real fecha ao acordar e corrige a agenda.
- **Cansaço:** déficit ÷ 10 h, até 100%. Efeitos:
  - −40% na chance;
  - −30% na duração;
  - −12% na nota;
  - motivo "cansado" a partir de 50%.

## 7. Durante a sessão

- **Limite planejado e limite absoluto** (R2): toda sessão tem o fim planejado e um **limite absoluto** que nada ultrapassa.
  - **Sessão normal:** fim da janela livre. Se a janela acaba na hora de deitar, mais o corte de sono autorizado: o menor entre 180 min, o que cabe no teto do déficit e o que mantém 4 h de sono antes do próximo acordar.
  - **Pausa e escapada do trabalho:** o fim planejado.
  - **Adiar a saída**, seja para terminar a atividade (até 15 min), terminar o trecho de viagem, estender ou atender o chamado do trabalho (5 min, `workCallToleranceMinutes`), **só dentro do limite absoluto**. Nunca entra em preparo, deslocamento, obrigação incompatível ou compromisso.
- **Continuar** (v3): decisão a cada 30 min depois da sessão normal, só dentro do tempo livre (seção 5).
- **Ficar acordado jogando** (P3, só na hora de dormir):
  - **Motivo:** gostando (nota parcial ≥ 700) ou perto de concluir a meta (70% do caminho).
  - **Chance:** 40% × tendência × (1 − autocontrole × 0,8) × (1 − cansaço).
  - **Tamanho:** 30% da sessão até ali × (0,5 + tendência), mínimo 10 min.
  - **Limites:** 2 vezes e 180 min por sessão, e sempre até o limite absoluto. Não vale na pausa nem na escapada do trabalho, nem no cenário controlado.
- **Trabalho remoto:**
  - **Pausa:** chance de 30% × importância. Sessão de até metade da sessão base, dentro da pausa.
  - **Escapada:** chance de 40% × flexibilidade × importância × (1 − autocontrole) × folga do dia (carga sorteada todo dia). Dura 25 ±10 min, longe do começo e do fim do expediente.
  - **Chamado do trabalho:** em 35% das escapadas. Se a atividade termina em até 15 min, a pessoa termina e sai; senão, larga e sai.
  - **Custo de largar** (pesa na escolha da atividade): chefe 1000, emboscada 850, patrulha 700, caça 550, missão 400, recrutar e explorar 300, treinar 150, coletar 100, consertar 50, comércio e conversa 0. Peso de 30%.

## 8. A aba Rotina (o que cada card mostra e de onde vem)

| Card | Mostra | Fonte |
|---|---|---|
| **Onde mora** | Hora local agora, fuso, residência, nacionalidade, idiomas | Localidade (P1) |
| **Finanças** | Rosca de renda e apoio, orçamento de lazer, disposição a pagar, aversão a vantagem paga | Renda real (P2), sem efeito até o M7 |
| **Vontade de jogar** | Importância, hábito (com a regularidade dos últimos 7 dias e o efeito de hoje na dica), tendência a prolongar e autocontrole (1–20); satisfação e engajamento com o efeito de agora na chance; disposição de hoje; previsto por semana; jogo num dia útil (estimativa, com as voltas); horário preferido × quando costuma começar e sair (fim da última sessão); barras "previsto" e "real (30 dias)" do uso do tempo livre; frase de leitura do perfil | Traços, humor, `Expect` e agenda |
| **Semana típica** | Por dia: tempo livre (azul; verde em dia livre) × jogo previsto (dourado), chance do dia embaixo; livre na semana, previsto, obrigações, sono por noite | `Build` + `Expect` |
| **Ocupação** | Nome e o que faz (trabalha, estuda…), etiquetas de condição, turno, horas por semana, dias, renda, cartões dia a dia (horário, tempo livre, maior janela), a semana em 168 horas | Ocupação + semana típica |
| **Últimos 30 dias** | Linha suave do tempo livre (área) e do jogado; ponto no zero com a cor do motivo; faixas verdes nos dias livres; losango de jogo no trabalho; ponto roxo de compromisso; na dica, as sessões do dia, as voltas, as voltas recusadas, a noite deixada de lado depois de jogar no trabalho e a disposição do dia | Agenda + registro de sessões |
| **Sono e cansaço** | Anel de cansaço, déficit e teto, sono necessário, hora real de deitar (dia útil e livre), sono perdido para o jogo, sono de cada noite contra a necessidade | Sono (P3) |
| **Como terminaram os dias** | Rosca dos motivos, sequência atual, minutos planejados por dia de jogo | Agenda |

Ordem da página: quem é → o padrão → o que aconteceu.

## 9. Invariantes, regras do preset e tendências (R5)

Cada regra da rotina é de uma de três classes. Os testes da rotina levam a mesma classe (`[Trait("Classe", ...)]`) e podem rodar separados, por exemplo `dotnet test --filter "Classe=Invariante"`.

**Invariantes da simulação** (valem para qualquer conteúdo; falhar é erro de código; 39 testes):
- **Limites reais:** nenhuma sessão invade sono real, preparo, deslocamento, obrigação incompatível ou compromisso. No trabalho remoto, só na pausa ou na escapada autorizadas.
- **Corte de sono:** só na janela que termina no sono; até 180 min por período, compartilhados; sono mínimo de 4 h.
- **Contabilidade:** cada sono é contado uma vez, no despertar. Cada minuto do dia pertence a uma parte só, e a semana civil fecha 168 h. O turno da noite de sexta pertence ao sábado civil.
- **Probabilidades:** fatores combinados e limitados uma vez; a estimativa usa a mesma política.
- **Decisões:**
  - sem saciedade nem cansaço, a chance é a prioridade;
  - a saciedade cresce com o tempo jogado e cai nas pausas, sem depender da frequência de atualização;
  - um sorteio por dia para o horário de começar, e a participação não depende do passo;
  - continuar só dentro do tempo livre; ficar acordado só passando da hora de dormir;
  - jogo no trabalho não desconta minutos.
- **Hábito e disposição:** faltar por obrigação é neutro e sessão interrompida conta como jogo; a disposição é reproduzível e centrada.
- **Determinismo:** mesma seed, mesmo resultado; o estado entra no hash. O validador recusa conteúdo inválido.

**Regras do preset inicial** (valem para o conteúdo de `presets/inicial`; outro preset pode mudar; 16 testes):
- turno diurno nunca exige acordar antes das 04:30;
- escala 5×2 de segunda a sexta, com folga no fim de semana; 6×1 com folga em qualquer dia;
- quem trabalha e estuda vai direto do trabalho para a aula e come no caminho;
- home office sem deslocamento nem chegada;
- tolerância de 5 min ao chamado do trabalho;
- horários e rendas das ocupações;
- o gosto de horário fica separado do horário habitual (quem sai às 18:50 e gosta das 17 h costuma começar depois das 19 h).

**Tendências estatísticas** (comportamento esperado em média, com faixa; 12 testes e as metas de `targets.json`):
- quem prioriza o jogo começa logo depois de chegar; o casual, mais tarde;
- mais tempo livre, mais jogo para quem prioriza;
- voltar depois de uma pausa é comum no dia livre;
- quem trabalha à tarde ou à noite também usa a janela antes do trabalho;
- o cansaço reduz o jogo;
- o horário de costume influencia o começo;
- chineses jogam na noite local, que é manhã no servidor;
- dias de jogo, sessões por dia, horas por semana, ativos, fim de semana e pico online (metas, seção 10).

## 10. Números da validação (5 seeds × 90 dias × 1.000 pessoas, R5, D-052)

**Metas da rotina** (rodada controlada, humor neutro): **9 de 9** dentro. Na R5, as medidas foram separadas e as faixas revistas pelo significado (histórico e justificativas em `validacao.md`, seção "R5").

| Meta | Faixa | Resultado | Entre seeds |
|---|---|---|---|
| Casual: dias de jogo por semana | 2–4 | 2,8 | 2,8–2,8 |
| Hardcore: dias de jogo por semana | 5–7 | 6,2 | 6,0–6,4 |
| Casual: sessões por dia de jogo | 1,1–2,0 | 1,5 | 1,5–1,5 |
| Hardcore: sessões por dia de jogo | 1,2–2,5 | 1,5 | 1,5–1,6 |
| Casual: horas por semana | 3–9 h | 6,7 h | 6,6–6,8 |
| Hardcore: horas por semana | 15–45 h | 36,5 h | 35,6–37,7 |
| Ativos em dia útil | 30–55% | 51,4% | 50,2–52,4 |
| Fim de semana ÷ dia útil | 1,3–2,0× | 1,3× | 1,3–1,4× |
| Pico online do dia | 15–35% | 23,9% | 22,7–24,4 |

**Campanha completa** (com humor):

| Perfil | Dias de jogo/semana | Sessões por dia de jogo | Horas/semana |
|---|---:|---:|---:|
| Casual | 2,9 | 1,51 | 7,1 |
| Social | 3,8 | 1,57 | 12,9 |
| Comerciante | 4,4 | 1,71 | 14,4 |
| Explorador | 4,5 | 1,70 | 17,5 |
| Competitivo | 4,9 | 1,58 | 21,4 |
| Hardcore | 6,2 | 1,52 | 36,8 |
| Devoto | 6,9 | 1,52 | 57,1 |

- **Sessões por dia de jogo:** uma em 58%, duas em 30%, três ou mais em 12%; pausa mediana de 80 min.
- **Fim das sessões:** decidiu parar 79%, hora de dormir 16%, fim do tempo livre 5%.
- **Sono cortado:** 0,2% do tempo jogado.
- **Devoto:** sessão mediana de 342 min (p90 477); por pessoa, 47 a 69 h por semana (p10 a p90); 64% do tempo livre vira jogo; 0% cansado.
- **Referências:** pré-R4, R4, v3, R5 e R5 com o Devoto em `docs/baselines/`.

**Pendente:** com todos jogando mais, ficam fora as metas de progressão.
- Horas até o nível 20: 70 h, contra 80–110.
- Casuais no nível 10 ou mais: 98,9%.
- Tempo andando ou esperando: 21,0% (faixa até 20%).

Elas são recalibradas com a curva de XP na Etapa 2.

## 11. Parâmetros e hipóteses (`population.json`, seção `routine`)

São hipóteses configuráveis, a menos que a coluna diga outra coisa. Os valores são os do preset inicial.

| Parâmetro | Valor | O que representa |
|---|---:|---|
| `freeDayChanceBonus` | 350 | O tempo livre aumenta a prioridade de quem liga pouco para o jogo |
| `busyDayPenaltyPermille` | 650 | O dia de obrigações reduz a chance de jogar no dia (só nela) |
| `habitStreakPermille` · `habitHistoryDays` | 250 · 7 | Peso do hábito e dias da regularidade |
| `dispositionSpreadPermille` | 200 | Variação da disposição do dia (±; a maioria fica em ±7%) |
| `satietyHalfLifeSessionsPermille` | 1500 | Meia-vida da saciedade jogando, em sessões normais |
| `satietyRecoveryHalfLifeMinutes` | 120 | Meia-vida da saciedade em pausa |
| `continueBlockMinutes` · `continueMinMinutes` | 30 · 5 | Cada quanto se decide continuar e o resto mínimo da janela |
| `continueTendencyWeightPermille` | 400 | Peso da tendência a prolongar no continuar (±20%) |
| `experienceWeightPermille` | 500 | Peso da experiência da sessão (em relação ao habitual) |
| `returnPauseMinMinutes` · `returnPauseMaxMinutes` | 30 · 120 | Pausa antes de reconsiderar voltar |
| `unfinishedBonusPermille` | 150 | Vontade de terminar depois de uma interrupção pelo trabalho |
| `freeDayMorningMinutes` | 60 | Manhã do dia livre antes da primeira oportunidade |
| `earlyStartResistancePerHour` | 300 | Resistência a começar antes do horário de costume, por hora |
| `startStepMinutes` | 10 | Passo que arredonda o horário de começar (não muda a participação) |
| `maxSessionsPerDay` | 5 | Teto de sessões normais por dia |
| `maxSleepCutMinutes` · `minSleepMinutes` | 180 · 240 | Limites do corte de sono (regra de segurança, R2/R3) |
| `estimateSamples` | 24 | Dias simulados por dia da semana na estimativa (não muda a simulação) |

## 12. Exemplos individuais (mesmas pessoas, seed 11, campanha com humor)

**Semana de 10/04 a 16/04** (detalhes em `docs/baselines/pos-R4b/semanas.md`):

| Pessoa | pré-R4 | R4 | v3 | Como foi na v3 |
|---|---:|---:|---:|---|
| Trabalhador dedicado (hardcore, horário comercial) | 37h19 | 34h21 | 43h48 | 4 h por dia útil (a noite toda); sábado e domingo de 10h a 11h30 em 2 ou 3 sessões |
| Hardcore sem ocupação | 53h52 | 44h04 | 50h44 | De 4h a 11h por dia, 1 ou 2 sessões, começando entre 11h e 17h |
| Casual em escala 5×2 | — | 3h41 | 4h04 | Só no fim de semana; no domingo, duas sessões com pausa |
| Social que trabalha e estuda | 3h09 | 7h51 | 10h40 | Sem tempo de segunda a quinta; no sábado, três sessões (6h53) |

**A sessão mais longa da validação** (12h25; seed 1, pessoa 329: hardcore, escala 5×2, dia livre com tempo livre das 08:09 às 23:15):
- **Traços:** importância 840, sessão normal 225 min, tendência a prolongar 910, autocontrole 198; engajamento 690.
- **Começo:** às 10:19, antes do horário de costume, por vontade.
- **Decisões de continuar:** às 14:04, ao fim da sessão normal, a chance era de 70% (saciedade 33%). Depois, a cada 30 min, a saciedade subiu até 76% e a chance caiu até 54%.
- **Fim:** às 22:44 decidiu parar, 31 min antes de dormir, sem cortar sono. Cansaço 18 de 1000 (descansada).
- **Leitura:** os mecanismos funcionam. Com uma vontade de jogar acima de 1000 (engajada, gostando, tendência alta), a vontade de parar, que vai no máximo a 1000 + 400 de cansaço, não leva a chance de parar muito acima de 45% por bloco. Por isso existe a cauda de sessões muito longas (D-052).

## 13. Simplificações registradas

- Compromissos não cortam sono.
- O cansaço é só o do sono (déficit). O desgaste de jogar horas seguidas está na saciedade, que é a única medida do "já joguei" e zera ao acordar.
- A estimativa da tela não tem humor, cansaço, jogo no trabalho nem corte de sono.
- O fuso é fixo por localidade, sem horário de verão.
- Renda e apoio não têm efeito até o M7.
