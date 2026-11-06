# Variedade de horários (R6)

O diagnóstico da rotina mostrou um problema de geração, não de decisão:
- ninguém jogava entre 1h e 8h da hora local, em nenhum grupo;
- a hora de dormir saía de 22h30 a 01h, e ninguém acordava depois das 8h15;
- o horário preferido ficava entre 14h e 23h: ninguém preferia a manhã nem a madrugada;
- quem não tem obrigação dormia como todo mundo;
- o turno noturno não tinha janela depois do turno e voltava às 23h45 em toda folga.

O criador aprovou a correção com cuidados (registrados em `backlog.md`). Os parâmetros abaixo são candidatos para medição. Nenhum outro peso da rotina mudou e não há presença mínima na madrugada.

## 1. Cronotipo

- **O que é:** atributo novo (`traits.chronotype`, triangular 0 · 500 · 1000), em fluxo de sorteio próprio, de modo que nenhum outro atributo muda. 0 = muito matinal, 1000 = muito notívago.
- **É tendência, não regra:** entra só em parte, pela posição compartilhada (o mesmo mecanismo das ligações do D-054), e cada pessoa mantém a sua parte individual.
  - **Hora natural de dormir:** 70% vem do cronotipo (`routine.chronotypeBedtimeInfluencePermille`).
  - **Horário preferido de jogar:** 35% vem do cronotipo (ligação em `links`). Um notívago pode preferir jogar à tarde.
- **Os dois extremos, com o meio mais comum:**
  - **Hora natural de dormir** (`routine.bedtimeMinute`): triangular de 21h00 a 04h30, com moda às 23h30. Mediana perto da meia-noite; cerca de 6% depois das 03h e 5% antes das 22h. Não é limite universal: a véspera de folga e o sono deslocado permitem rotinas mais tardias, e a faixa vai até as 09h.
  - **Horário preferido** (`traits.preferredStartHour`): triangular de 7h a 28h (04h do dia seguinte), com moda às 20h.
- **As obrigações continuam limitando:** quem acorda cedo para trabalhar ou estudar se acostuma a dormir mais cedo, como antes (`bedtimeAdaptMinutes`). O cronotipo de um notívago com trabalho às 8h aparece nas folgas, não nos dias de trabalho.

## 2. Faixa contínua que atravessa a meia-noite

- O horário preferido vai além de 23h: 24 = meia-noite, 25 = 01h, e assim por diante.
- Um horário de relógio anterior ao acordar só vale como "madrugada seguinte" quando o dia acordado da pessoa chega perto dele: 03h vem depois das 23h.
  - Para quem dorme às 23h e prefere 7h, mas acorda às 7h30, vale o acordar. Antes, a regra jogava esse horário para o dia seguinte.
  - Para quem trabalha no turno da noite, 7h é a manhã depois do turno. Já 14h, antes de acordar às 15h50, vale o acordar.
- Regra única (`DailySchedule.AwakeMinute`), usada no início do dia, na escolha da janela e no horário habitual.

## 3. Véspera de folga

- **Para quem tem obrigações, na noite antes de um dia livre:** dorme mais tarde por um deslocamento individual (`routine.freeEve`):
  - 65% das pessoas têm o deslocamento;
  - entre 15 e 180 minutos, moda de 60;
  - metade vem do cronotipo (notívagos deslocam mais).
- **Respeita compromissos e necessidade de sono:** se amanhã há obrigação ou compromisso, a hora de dormir garante o sono necessário antes deles.
- **Desloca, não corta:** o sono do dia seguinte começa na hora deslocada e dura o necessário, então acorda mais tarde.
- Quem não tem obrigação não tem "véspera": todos os dias já seguem a hora natural.
- **O costume vem dos dias típicos:** o horário habitual de jogar nos dias de obrigação é calculado sem a véspera de folga. A sexta à noite mais longa é exceção da semana e não define o costume dos outros dias.

## 4. Turno da noite

- **Continuidade na folga:** a hora de dormir fica a meio caminho entre a hora natural e a chegada dos dias de turno (`nightShiftOffContinuityPermille` = 500). Exemplo: hora natural 23h30 e chegada às 07h45 dão 03h37 na folga.
  - Antes do próximo turno, a regra que garante o sono antes da obrigação continua valendo.
- **Jogar depois do turno é possibilidade:**
  - **A janela:** depois da chegada há uma janela de até 90 min (`postShiftWindowMinutes`). Ela pertence ao mesmo dia acordado, então a decisão de jogar do dia é a mesma, sem segunda chance diária independente.
  - **Não cria tempo livre:** não conta como tempo livre do dia, de modo que a chance do dia e a prioridade do jogo não mudam. O sono planejado começa na chegada.
  - **Cansaço:** pesa contra jogar nessa janela, tanto ao começar quanto ao voltar (`postShiftTirednessPermille` = 400).
  - **Se jogar:** o sono se desloca (seção 5). Se não jogar, dorme na chegada.

## 5. Sono deslocado × sono cortado

- **Ao acordar, se a pessoa foi dormir mais tarde que o planejado** (ficou jogando), ela acorda mais tarde, até completar o sono necessário. O limite é a obrigação de hoje (com deslocamento e preparo), o compromisso de hoje ou o fim do dia acordado. Isso acontece uma vez por noite (`routine.sleepShift`).
  - **Dormir das 03h às 11h**, sem nada que obrigue a acordar antes, não gera déficit.
  - **Ficar até as 03h e precisar acordar às 07h** reduz o sono. O que falta vira déficit, com as consequências que já existiam.
- **Enquanto dorme mais:** uma sessão marcada para esse período começa depois de acordar e se preparar, se ainda couber na janela, e fica limitada ao que sobra dela, sem invadir deslocamento ou obrigação. Uma volta ao jogo nesse período não acontece.
- **Ficar acordado jogando:** o corte de sono autorizado (R2/R3) passa a contar o que dá para recuperar acordando mais tarde. O teto por noite (`maxSleepCutMinutes`) não mudou.
- **Registro:** a agenda do dia guarda os minutos de sono deslocado (`SleepShiftMinutes`), e o "corte" registrado desconta o que foi recuperado.

## 6. Tela

- Com ninguém conectado, o aviso mostra o **próximo login realmente agendado** (hora do servidor e jogador).
- Se não há nenhum agendado, a tela diz isso, sem previsão.
- O texto fixo "a maioria entre 19h e 22h" saiu.

## 7. Medição

**Antes × depois**, com as mesmas pessoas: seeds 1 e 2 × 1.000 pessoas × 28 dias, medidos os dias 7 a 27. Números finais, já com as duas correções da conferência:
- a sessão adiada fica limitada à janela;
- a véspera de folga não define o costume.

Os resultados completos estão em `baselines/horarios/analise.md`.

**Tempo jogado por hora local** (% do tempo do grupo):

| Grupo | Madrugada (0h–6h) | Manhã | Tarde | Noite | Horas por semana (mediana) |
|---|---|---|---|---|---|
| Sem obrigação | 0,4% → 4,3% | 12,4% → 12,9% | 51,2% → 47,5% | 36,0% → 35,3% | 27,4 → 27,4 |
| Trabalho diurno | 0,3% → 2,4% | 9,0% → 9,2% | 32,1% → 31,0% | 58,6% → 57,3% | 15,1 → 15,2 |
| Estudo de manhã | 0,1% → 0,9% | 7,0% → 6,4% | 50,7% → 50,8% | 42,2% → 41,9% | 19,1 → 19,4 |
| Obrigação começa tarde | 0,5% → 6,0% | 15,9% → 14,9% | 27,2% → 23,9% | 56,4% → 55,2% | 16,2 → 16,9 |
| Trabalho + estudo à noite | 0,2% → 3,7% | 4,6% → 4,9% | 48,4% → 45,8% | 46,8% → 45,5% | 7,3 → 7,7 |
| Turno noturno | 0,3% → 8,3% | 7,0% → 8,9% | 40,6% → 33,4% | 52,0% → 49,4% | 13,4 → 15,2 |
| Todos | 0,3% → 3,1% | 10,0% → 9,9% | 39,6% → 37,8% | 50,1% → 49,2% | 16,4 → 16,7 |

**Sono planejado, hora de dormir** (mediana, com p10 e p90):
- **Sem obrigação:** 23h45 (23h10–00h15) → 00h05 (22h20–02h25); acordar 07h32 → 07h56 (p90 10h19).
- **Trabalho diurno, noite antes do trabalho:** 22h49 → 22h39 (igual, limitado pelas obrigações).
- **Trabalho diurno, dia livre:** 23h30 → 00h15 (p90 02h10).
- **Obrigação começa tarde:** 23h37 → 00h00 nos dias de obrigação; 01h05 nos dias livres.
- **Turno noturno:** na véspera de turno vindo da folga, 23h40 → 03h30.

**Passagens entre trabalho e folga** (dormir na véspera, mediana, trabalho diurno):
- véspera de folga: 23h30 → 00h15;
- véspera de trabalho: 22h54 → 22h45;
- entre dias de trabalho: 22h47 → 22h36.

**Sono real:**
- **Mediana dormida:** a mesma (7,8 h) em todos os grupos.
- **Déficit:** igual ou menor.
- **Sono deslocado:** 7% a 8,5% dos dias livres; cerca de 19% dos dias de turno noturno, quando joga depois do turno.

**Servidor** (média dos dias 14 a 27, por 1.000 pessoas):
- **02h:** 12,9 → 29,4 conectados (seed 1); 7,3 → 25,3 (seed 2).
- **08h:** 24,8 → 39,0 (seed 1).
- **20h e 21h:** iguais.
- **Pico / cadastrados:** 26,7% → 26,9% (seed 1); 29,0% → 28,5% (seed 2).

**Início das sessões:** 1,6% entre 0h e 4h (antes, 0%), e mais inícios às 6h e 7h. O resto da distribuição é quase igual.

**Geração** (seed 1):
- **Horário preferido:** 11,7% antes das 12h e 12,1% depois da meia-noite (antes, 0% e 0%).
- **Correlação de postos com o cronotipo:** +0,45 com o horário preferido (parcial); +0,92 com a hora natural de dormir de quem não tem obrigação. O cronotipo é, principalmente, a tendência de sono. Por decisão do criador, os 70% ficam como candidato.

### Conferência 1: déficit de quem trabalha e estuda à noite

Ocupação `trabalho_estudo`: trabalho das 8h às 17h e estudo das 19h às 22h30, de segunda a quinta. Seeds 1 e 2, dias 7 a 27; cada linha é a noite que termina no dia indicado.

| Noite até | Sono planejado | Falta pela agenda | Cortado jogando | Noites jogando depois de deitar | Déficit ao acordar |
|---|---|---|---|---|---|
| segunda | 7,9 h | 0 min | 1 min | 13% | 35 min |
| terça | 6,6 h | 76 min | 0 | 0% | 111 min |
| quarta | 6,7 h | 74 min | 0 | 1% | 183 min |
| quinta | 6,6 h | 74 min | 0 | 1% | 257 min |
| sexta | 7,4 h | 56 min | 0 | 3% | 278 min |
| sábado | 9,6 h | 0 | 1 min | 10% | 156 min |
| domingo | 9,3 h | 0 | 1 min | 9% | 59 min |

- **Causa:** a agenda. 98,8% do sono que faltou vem das obrigações: depois da aula, com o deslocamento e a chegada, a pessoa deita perto da meia-noite e acorda às 6h30 para trabalhar. Ficar jogando depois de deitar responde por 1,2%.
- **Acontece sem jogar à noite:** nas noites de semana quase ninguém joga, e o déficit sobe mesmo assim.
- **Recuperação:** pelas regras que já existiam, cada manhã livre recupera até 2 h e cada noite normal, 20 min. O déficit chega ao pico na sexta e volta perto do mínimo na segunda.
- **Conclusão:** não é defeito da simulação nem efeito do jogo; é a rotina dessa ocupação. Nada foi compensado.

### Conferência 2: a janela depois do turno nas contas do dia

O que "não conta como tempo livre" quer dizer:
- **Fora da conta do tempo livre do dia:** a janela fica fora da soma que define a chance de jogar no dia e a prioridade do jogo. Ela não aumenta a vontade de jogar.
- **Dentro do sono planejado:** na linha planejada do dia, esse período é sono, porque o sono começa na chegada. Cada minuto aparece uma vez só.
- **Se a pessoa joga:** o tempo jogado entra na agenda do dia (minutos jogados), e o sono real começa quando ela sai do jogo. O acordar se desloca, se a próxima obrigação permitir; senão, o que faltou vira déficit. É a mesma conta de quem fica acordado jogando à noite.
- **Ficha:** a "Semana típica" mostra a janela à parte ("depois do turno, se quiser jogar"), e não mais junto com o tempo livre, cujo total não a soma.

Conferido nos dados (seeds 1 e 2, dias 7 a 27):
- 960 dias de turno; em 214 (22%) houve jogo na janela depois do turno.
- Nenhuma sessão passou do fim da janela.
- **O sono foi contado uma vez:** em todos os 214 casos, o sono registrado é igual ao tempo entre a saída do jogo e o acordar real.
- Em nenhum caso o acordar real, mais o preparo, caiu depois do início da obrigação seguinte.
- O sono se deslocou 90 min (mediana), sem déficit.
- Não há tempo a mais fora das 24 horas: o jogo e o sono são contados no tempo real, um depois do outro.

## 8. Validação padrão (5 seeds × 1.000 × 90 dias)

Comparação com a versão anterior (P3, D-057). Relatório em `baselines/horarios/R6/`.

- **Regras do cenário:** 8 de 9 (antes, 7).
  - "Ativos no fim de semana ÷ ativos em dia útil" deixou de ser instável (1,3; intervalo 1,3–1,3).
  - Continua fora "horas por semana do casual": 9,8 → 10,0 h.
- **Provisórias:** 7 de 12 (antes, 8). Nenhuma piorou de forma real:
  - "tempo na cidade" (23,4% → 23,6%) passou a ter o intervalo cruzando o limite de 25%;
  - "andando ou esperando" (20,2% → 20,0%) ficou instável no limite de 20%.
- **Outras:**
  - horas por semana do hardcore: 36,7 → 37,4;
  - ativos em dia útil: 53,2% → 54,1%;
  - pico online: 28,3% → 28,2%;
  - progressão e missões iguais (diferenças de 0,1 a 0,6).
- Nenhum peso foi mudado para compensar.

## 9. Parâmetros (candidatos, `population.json`)

| Parâmetro | Valor | O que representa |
|---|---:|---|
| `traits.chronotype` | 0 · 500 · 1000 | Cronotipo (triangular, fluxo próprio) |
| `traits.preferredStartHour` | 7 · 20 · 28 | Horário preferido de jogar, em faixa contínua (28 = 04h) |
| `links`: horário preferido ← cronotipo | 350 | Parte do horário preferido que vem do cronotipo |
| `routine.bedtimeMinute` | 1260 · 1410 · 1710 | Hora natural de dormir (21h00 · 23h30 · 04h30) |
| `routine.chronotypeBedtimeInfluencePermille` | 700 | Parte da hora natural de dormir que vem do cronotipo |
| `routine.freeEve` | 650 ‰ · 15/60/180 min · 500 | Véspera de folga: chance de ter, minutos e parte do cronotipo |
| `routine.nightShiftOffContinuityPermille` | 500 | Folga do turno da noite: meio caminho entre a hora natural e o ritmo dos turnos |
| `routine.postShiftWindowMinutes` | 90 | Janela possível depois do turno, antes de dormir |
| `routine.postShiftTirednessPermille` | 400 | Cansaço contra jogar depois do turno |
| `routine.sleepShift` | true | Dormir mais tarde desloca o acordar quando nada obriga |

## 10. Testes

`ScheduleVarietyTests`:
- o cronotipo não muda os demais atributos;
- a influência é parcial (os dois extremos existem, e há notívago que prefere jogar cedo);
- as obrigações limitam a hora natural de dormir;
- 03h vem depois das 23h;
- véspera de folga com deslocamento individual e respeito ao compromisso;
- continuidade do turno da noite;
- a janela depois do turno não cria tempo livre nem muda a chance do dia;
- sono deslocado (sem déficit) × sono cortado (com déficit);
- o primeiro login agendado;
- o validador.
