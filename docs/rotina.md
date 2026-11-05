# Rotina da pessoa simulada: sistema, cálculos e números

Referência do sistema de rotina, das decisões P1, P2 e P3 até a D-046. Todos os números estão em `Content/presets/inicial/population.json` (seção `routine` e perfis) e em `occupations.json`; o validador confere as faixas. Permille: 1000 = 100%.

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
          JANELAS LIVRES ──► chance de jogar ──► sessão (início, duração)
                 │                  ▲                     │
                 │      humor · hábito · cansaço          ▼
                 │                               extensão · corte de sono
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
7. **Hora preferida de jogar** (coerente com a rotina e a vontade, D-045):
   - **Dias do hábito:** os dias com obrigação. A sessão considerada fica limitada à maior janela livre deles. Sem tempo nesses dias, valem os dias livres.
   - **Alvo do dia:** em cada dia do hábito, na janela mais perto do gosto de horário:
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
1. **Hábito proporcional:** × (1000 ± hábito × 250 ÷ 1000), mais se jogou ontem e menos se não jogou.
2. **Humor:** × fator do engajamento, 1000 + (engajamento − 700), entre 150 e 1300. Desligado no cenário controlado.
3. **Inativo:** × 250 × intenção de continuar ÷ 1000.
4. **Cansaço:** × (1000 − cansaço × 400 ÷ 1000).

**Duração desejada** (`DesiredMinutes`):
- sessão base × variação (±20%) × (1 + (0,25 + importância × 0,8) × folga);
- × (1 − cansaço × 0,3);
- mínimo de 20 min.

Exemplo: o hardcore de meio período (base 152 min, importância 725, folga 1,0) quer cerca de 4h40.

**Início** (`PlaceMain`):
- hora preferida ± até 75 min de variação diária;
- vai para a janela que contém essa hora (ou a mais perto; em empate, a maior);
- começa mais cedo se a sessão não couber;
- dura o que couber até o fim da janela.

**Segunda sessão:** para importância acima de 650. Chance = (importância − 650) × 200 ÷ 350 (até 20%), em outra janela, com 45% da sessão base.

**Estimativa** (`ExpectWeek`, sem humor, cansaço nem extensões): a mesma regra do planejador, calculada na **semana real, dia a dia**. A chance de cada dia depende de ter jogado na véspera, e a semana se repete até estabilizar, então dias úteis e livres recebem chances diferentes. A probabilidade é limitada só no fim, e a segunda sessão esperada entra na conta. A tela chama de "estimativa".

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
- **Ficar mais um pouco:**
  - **Motivo:** gostando (nota parcial ≥ 700) ou perto de concluir a meta (70% do caminho).
  - **Chance:** 40% × tendência × (1 − autocontrole × 0,8) × (1 − cansaço).
  - **Tamanho:** 30% da sessão planejada × (0,5 + tendência), mínimo 10 min.
  - **Limites:** 2 vezes e 180 min por sessão, e sempre até o limite absoluto. Não vale na pausa nem na escapada do trabalho.
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
| **Vontade de jogar** | Importância, hábito, tendência a prolongar e autocontrole (1–20); satisfação e engajamento com o efeito de agora na chance; previsto por semana; sessão do dia útil; quando costuma começar e sair; barras "previsto" e "real (30 dias)" do uso do tempo livre; frase de leitura do perfil | Traços, humor, `Expect` e agenda |
| **Semana típica** | Por dia: tempo livre (azul; verde em dia livre) × jogo previsto (dourado), chance do dia embaixo; livre na semana, previsto, obrigações, sono por noite | `Build` + `Expect` |
| **Ocupação** | Nome e o que faz (trabalha, estuda…), etiquetas de condição, turno, horas por semana, dias, renda, cartões dia a dia (horário, tempo livre, maior janela), a semana em 168 horas | Ocupação + semana típica |
| **Últimos 30 dias** | Linha suave do tempo livre (área) e do jogado; ponto no zero com a cor do motivo; faixas verdes nos dias livres; losango de jogo no trabalho; ponto roxo de compromisso | Agenda + registro de sessões |
| **Sono e cansaço** | Anel de cansaço, déficit e teto, sono necessário, hora real de deitar (dia útil e livre), sono perdido para o jogo, sono de cada noite contra a necessidade | Sono (P3) |
| **Como terminaram os dias** | Rosca dos motivos, sequência atual, minutos planejados por dia de jogo | Agenda |

Ordem da página: quem é → o padrão → o que aconteceu.

## 9. Coerência garantida por testes

| Regra | Teste |
|---|---|
| Turno diurno nunca exige acordar antes das 04:30 | `TurnoDiurno_NinguemAcordaDeMadrugada` |
| A hora preferida cai no tempo livre e a sessão típica cabe antes de dormir | `HoraPreferida_CaiNoTempoLivreDaRotina` |
| Quem prioriza o jogo começa logo depois de chegar; o casual, bem depois e com sessão menor | `QuemPriorizaOJogo_ComecaLogoQueChega_OCasualMaisTarde` |
| Hardcore de meio período de manhã joga logo depois do almoço | `Hardcore_DeMeioPeriodoDeManha_JogaLogoDepoisDoAlmoco` |
| A sessão cresce com o tempo livre para quem prioriza o jogo | `SessaoDesejada_CresceComTempoLivre_ParaQuemPriorizaOJogo` |
| O sono natural se adapta às obrigações (+1 h no dia livre, no máximo) | `HoraDeDormir_SeAcostumaAsObrigacoes` |
| Ninguém joga durante obrigações; no trabalho remoto, só na pausa ou na escapada permitida | `NinguemComecaASessaoDuranteUmaObrigacao` |
| A extensão tem limites e nunca invade obrigação | `Extensao_TemLimites_ConsomeSonoSoNoFimDoDia_ENuncaInvadeObrigacoes` |
| Compromisso só em dia livre, raro, sem sessão durante | `Compromisso_SoEmDiaLivre_Raro_ESemSessaoDurante` |
| Importância, tendência e autocontrole são independentes | `ImportanciaTendenciaEAutocontrole_SaoSeparados` |

## 10. Números da validação (5 seeds × 90 dias × 1.000 pessoas, depois de D-045 e D-046)

**Cenário controlado** (rotina pura, sem humor): 7 de 7 regras dentro.

| Meta | Faixa | Resultado | Entre seeds |
|---|---|---|---|
| Casual: sessões por semana | 2–4 | 2,9 | 2,8–2,9 |
| Casual: horas por semana | 2–6 h | 5,6 h | 5,6–5,7 |
| Hardcore: sessões por semana | 5–7 | 6,5 | 6,3–6,7 |
| Hardcore: horas por semana | 15–45 h | 37,7 h | 36,1–39,3 |
| Ativos em dia útil | 30–55% | 51,8% | 50,7–52,9 |
| Fim de semana ÷ dia útil | 1,3–2,0× | — | 1,31–1,35× |
| Pico online do dia | 15–35% | 25,7% | 24,1–26,4 |

**Campanha completa** (com humor):

| Perfil | Sessões/semana | Horas/semana |
|---|---:|---:|
| Casual | 3,1 | 6,0 |
| Social | 3,9 | 11,2 |
| Comerciante | 4,6 | 11,3 |
| Explorador | 4,7 | 14,6 |
| Competitivo | 5,2 | 20,3 |
| Hardcore | 6,5 | 37,9 |

**Pendente:** com todos jogando mais, ficam fora as metas de progressão.
- Horas até o nível 20: 68 h, contra 80–110.
- Casuais no nível 10 ou mais: 99%.
- Quests por personagem: 30,9, contra o máximo de 30.
- Tempo andando ou esperando: 27%.

Elas são recalibradas com a curva de XP na Etapa 2.
