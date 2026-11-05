# Rotina da pessoa simulada: sistema, cálculos e números

Referência do sistema de rotina, das decisões P1, P2 e P3 até a D-051 (revisão R1 a R4, com a vontade de jogar da v3). Todos os números estão em `Content/presets/inicial/population.json` (seção `routine` e perfis) e em `occupations.json`; o validador confere as faixas. Permille: 1000 = 100%.

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

   | Perfil | Importância do jogo | Hábito | Sessão base | Gosto de horário | Tendência a prolongar | Autocontrole |
   |---|---:|---:|---:|---:|---:|---:|
   | Casual | 350 ±150 | 400 ±200 | 80 ±45 min | 20h ±3 | 300 ±300 | 600 ±300 |
   | Social | 500 | 550 | 110 ±60 | 20h ±3 | 500 | 550 |
   | Explorador | 550 | 500 | 120 ±60 | 15h ±6 | 500 | 550 |
   | Comerciante | 600 | 650 | 90 ±50 | 19h ±4 | 400 | 650 |
   | Competitivo | 650 | 600 | 150 ±80 | 21h ±2 | 650 | 450 |
   | Hardcore | 820 ±100 | 750 | 240 ±100 | 19h ±4 | 800 | 400 |

   Importância, tendência a prolongar e autocontrole são sorteados de forma independente dentro do perfil (o teste mede correlação ≈ 0).
2. **Localidade:** residência e fuso. Brasil UTC−3 (peso 760), EUA UTC−5 (80), China UTC+8 (70), Argentina UTC−3 (90). Os feriados são os de onde a pessoa mora.
3. **Ocupação:** pelo peso, com a variação de horário e de deslocamento e a folga da escala.
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

**Saciedade** (`Satiety`, 0–1000): cresce enquanto a pessoa joga e cai nas pausas. Não existe cota nem total do dia.
- Jogando: sobe a fração m ÷ (m + τ) do que falta, com τ = 2 sessões normais (`satietyGrowthSessionsPermille` 2000). Depois de uma sessão normal, a saciedade fica em um terço.
- Em pausa: cai a fração m ÷ (m + 120 min) (`satietyRecoveryMinutes`): metade a cada 2 h.
- Zera ao acordar.
- Todo o tempo jogado conta, inclusive no trabalho e o interrompido. A frustração de uma interrupção pelo trabalho entra à parte, como vontade de terminar (+150 na vontade de jogar da próxima decisão).

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

## 9. Coerência garantida por testes

| Regra | Teste |
|---|---|
| Turno diurno nunca exige acordar antes das 04:30 | `TurnoDiurno_NinguemAcordaDeMadrugada` |
| O horário habitual cai no tempo livre e a sessão típica cabe antes de dormir | `HoraPreferida_CaiNoTempoLivreDaRotina` |
| O gosto de horário fica separado do horário habitual | `GostoDeHorario_SeparadoDoHorarioHabitual` |
| Quem prioriza o jogo começa logo depois de chegar; o casual, bem depois e com sessão menor | `QuemPriorizaOJogo_ComecaLogoQueChega_OCasualMaisTarde` |
| Hardcore de meio período de manhã joga logo depois do almoço | `Hardcore_DeMeioPeriodoDeManha_JogaLogoDepoisDoAlmoco` |
| A sessão normal é preferência; a importância não muda a duração | `SessaoNormal_EPreferencia_ImportanciaNaoMudaADuracao` |
| Mais tempo livre dá mais jogo estimado a quem prioriza o jogo (pelas voltas e pelo continuar) | `MaisTempoLivre_MaisJogoEstimado_ParaQuemPriorizaOJogo` |
| Sem saciedade nem cansaço, a decisão é a própria prioridade; saciedade e cansaço reduzem | `Decisao_SemSaciedadeNemCansaco_EAPropriaPrioridade_ESaciedadeReduz` |
| A saciedade cresce com o tempo jogado, cai na pausa e fica entre 0 e 1000 | `Saciedade_CresceComOTempoJogado_CaiNaPausa_NuncaPassaDosLimites` |
| Um sorteio por dia para começar; o passo não muda quanto se começa cedo | `Comecar_UmSorteioSoPorDia_OPassoNaoMudaQuantoSeComecaCedo` |
| A participação é a chance do dia, não o passo de avaliação | `Participacao_ENaChanceDoDia_NaoNoPassoDeAvaliacao` |
| Continuar só dentro do tempo livre; ficar acordado só passando da hora de dormir | `Continuar_DentroDoTempoLivre_EFicarAcordadoSoPassandoDaHoraDeDormir` |
| Voltar depois de uma pausa é comum no dia livre; casual pode jogar três vezes | `Voltar_DepoisDeUmaPausa_ComumNoDiaLivre_ECasualPodeJogarTresVezes` |
| Jogo no trabalho não desconta minutos da noite | `JogoNoTrabalho_NaoDescontaMinutosDaSessaoDaNoite` |
| No dia de obrigação também se usa a janela antes do trabalho | `DiaDeObrigacao_TambemUsaAJanelaAntesDoTrabalho` |
| O horário de costume influencia: começa nele ou antes, por vontade | `Sessoes_ComecamNoHorarioDeCostumeOuAntesPorVontade` |
| Hábito: falta por obrigação é neutra, escolha reduz, sessão interrompida conta | `Habito_FaltaPorObrigacaoENeutra_EscolhaReduz_SessaoInterrompidaConta` |
| A disposição do dia é reproduzível, pequena e centrada | `DisposicaoDoDia_ReproduzivelPorPessoaEDia_PequenaECentrada` |
| O sono natural se adapta às obrigações (+1 h no dia livre, no máximo) | `HoraDeDormir_SeAcostumaAsObrigacoes` |
| Ninguém joga durante obrigações; no trabalho remoto, só na pausa ou na escapada permitida | `NinguemComecaASessaoDuranteUmaObrigacao` |
| A extensão tem limites e nunca invade obrigação | `Extensao_TemLimites_ConsomeSonoSoNoFimDoDia_ENuncaInvadeObrigacoes` |
| Compromisso só em dia livre, raro, sem sessão durante | `Compromisso_SoEmDiaLivre_Raro_ESemSessaoDurante` |
| Importância, tendência e autocontrole são independentes | `ImportanciaTendenciaEAutocontrole_SaoSeparados` |

## 10. Números da validação (5 seeds × 90 dias × 1.000 pessoas, v3, D-051)

**Cenário controlado** (rotina pura, sem humor): 4 de 7 regras dentro. As três fora medem o modelo antigo de uma sessão por dia (ver D-051): sessões por semana do casual 4,2 (faixa 2–4) e do hardcore 9,5 (5–7), horas do casual 6,8 h (2–6). Os dias com jogo não mudaram.

| Meta | Faixa | Resultado | Entre seeds |
|---|---|---|---|
| Casual: sessões por semana | 2–4 | 4,2 | 4,2–4,3 |
| Casual: horas por semana | 2–6 h | 6,8 h | 6,7–6,9 |
| Hardcore: sessões por semana | 5–7 | 9,5 | 9,0–10,0 |
| Hardcore: horas por semana | 15–45 h | 36,8 h | 35,8–38,0 |
| Ativos em dia útil | 30–55% | 51,4% | 50,3–52,4 |
| Fim de semana ÷ dia útil | 1,3–2,0× | 1,3× | 1,3–1,4× |
| Pico online do dia | 15–35% | 24,1% | 22,8–24,7 |

**Campanha completa** (com humor):

| Perfil | Sessões/semana | Horas/semana |
|---|---:|---:|
| Casual | 4,5 | 7,2 |
| Social | 6,1 | 13,1 |
| Comerciante | 7,7 | 14,6 |
| Explorador | 7,7 | 17,7 |
| Competitivo | 7,8 | 21,7 |
| Hardcore | 9,5 | 37,1 |

As distribuições completas e as comparações com a pré-R4 e a R4 estão em `docs/baselines/pos-R4b/` e no D-051.

**Pendente:** com todos jogando mais, ficam fora as metas de progressão.
- Horas até o nível 20: 70 h, contra 80–110.
- Casuais no nível 10 ou mais: 98,9%.
- Tempo andando ou esperando: 20,8% (faixa até 20%).

Elas são recalibradas com a curva de XP na Etapa 2.
