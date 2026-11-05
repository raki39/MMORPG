# Comportamento da pessoa simulada (fechamento do M2)

Como cada pessoa decide, sente e permanece no jogo. Complementa [atributos.md](atributos.md) (os traços e como mudam) e as decisões D-037, D-038, D-039 e D-039a. Todos os números ficam em `Content/presets/inicial/population.json`, nas seções `session`, `mood`, `memory` e `retention`; o validador confere as faixas.

## Visão geral

| Camada | O que é | Onde age |
|---|---|---|
| Meta de curto prazo (D-036) | O que a pessoa quer agora: um nível, uma zona, uma missão, praticar algo | Fator "meta pessoal" na decisão |
| Plano da sessão (D-037) | A intenção da sessão: o foco escolhido na primeira decisão | Continuidade entre ciclos |
| Atividade | O que ela faz neste ciclo | Experiência, XP, tédio, memória |
| Humor (D-038) | Satisfação, frustração, novidade, engajamento, intenção de continuar | Rotina, sessão e retenção |
| Memória (D-038) | Impressão de cada lugar e acontecimentos marcantes | Fator "lembrança" e saudade |
| Retenção (D-039) | Ativo, inativo, desistente e retorno | Se e quanto a pessoa joga |

Quem gosta de repetir e quem precisa de variedade é um traço próprio: **Variedade** (preferência por variedade, D-039a). Ela é separada da Paciência (tolerar a repetição) e da Exploração (gostar de explorar).

## Plano da sessão e continuidade

- **Formação:** a primeira decisão da sessão forma o plano. O motivo é o fator que mais pesou.
- **Continuar ou trocar:** ao fim de cada ciclo, continua no foco, a menos que outra opção seja melhor por:
  - 60 pontos depois do compromisso mínimo;
  - 150 pontos durante o compromisso mínimo, que dura 30 min + até 60 min pela paciência.
- **Revisão livre:** etapa ou missão concluída, nível novo, meta concluída ou abandonada e zona descoberta liberam a próxima decisão sem margem.
- **Sem custo afundado:** a viagem já feita não obriga a insistir.
- **Tédio contextual** (substitui a penalidade fixa de repetição):
  - cresce por tipo de atividade, 300 por hora;
  - cresce mais rápido para quem prefere variedade e mais devagar para quem gosta muito daquele tipo;
  - fazer outra coisa alivia 400 por hora; entre sessões sobram 30%;
  - na decisão pesa até 300 pontos, aliviado pela paciência.
- **Explorar sem nada novo** ao alcance perde metade do gosto.
- **Zona familiar:** atrai menos quem prefere variedade.

## Experiência e humor

**Experiência de cada atividade**, em pontos:

| Componente | Conta |
|---|---|
| Gosto | (gosto pelo tipo − 500) × 0,3 |
| Encaixe de nível | (rendimento − 600) × 0,2, nas atividades com XP |
| Lotação | −lotação × 0,2, aliviada pela Coordenação (caça e coleta) |
| Companhia | Sociabilidade na taverna; gosto por socializar no resto |
| Tédio | −tédio × 0,25 |
| Risco | risco × (500 − Aversão a risco) × 0,15: emociona quem tem pouca aversão |

**Nota da sessão** (0–1000, neutro 500):
- média dos componentes;
- progresso contra a expectativa;
- novidade;
- conquistas e reveses.

**Pesos pessoais:**
- **Progresso:** pesa pelo quanto a pessoa liga para evoluir (PvE 50%, competitividade 30%, PvP 20%).
  - É o XP da sessão contra a referência do nível (caça no próprio nível).
  - A expectativa começa pela competitividade, aproxima-se do real aos poucos e tem piso.
  - No nível máximo o termo é neutro.
- **Novidade, em horizonte de meses (D-039a):** atualizações de MMORPG saem a cada 3 a 6 meses, e ninguém quer conteúdo novo depois de uma semana.
  - **Ritmo:** gasta 2 pontos por hora de jogo (as frações das sessões curtas se acumulam) e é renovada por zonas, atividades, níveis e missões novas.
  - **Hardcore que já esgotou tudo** (25 h por semana): cerca de 2,5 meses para a novidade ficar neutra (500) e cerca de 5 meses para chegar a zero. Abaixo de zero é saturação.
  - **Casual:** anos.
  - **Peso na nota:** (novidade − 500) × 0,3, com limite de ±300, vezes a necessidade de novidade. A necessidade é 10% base + Variedade 50% + Exploração 20% + impaciência 20%. Quem gosta de repetir sente pouco.

**Humor** (permille):

| Estado | Como se forma |
|---|---|
| Satisfação | Média móvel das notas; sessões de menos de 2 h pesam proporcionalmente menos |
| Frustração | Sobe com metas abandonadas, falta do que fazer e sessões ruins; cai 40 por dia e com sessões boas |
| Engajamento | 70% satisfação, 10% calma, 20% novidade |
| Intenção de continuar | Satisfação − frustração × 0,5 + (Lealdade − 500) × 0,2 + nível × 4 (até 60) |
| O que agrada / incomoda | Componentes acumulados de cada sessão, perdendo 5% por dia |

## Memória

- **Impressão de cada lugar** (zona/atividade, −1000 a 1000):
  - aprende 30% com cada experiência;
  - na decisão vira o fator "lembrança do lugar" (até ±150);
  - boas impressões perdem 1,5% por dia e más perdem 4%.
- **Acontecimentos marcantes:** até 12 por pessoa, como memória resumida.
  - Quais: níveis múltiplos de 5, missão principal, meta concluída ou abandonada, zona descoberta, atividade ou sessão muito boa ou muito ruim, ficar sem nada para fazer.
  - Conquistas perdem 10 de intensidade por dia; frustrações, 25.
  - O mesmo fato se reforça; com a memória cheia, sai o mais fraco.
- **Estados importantes ficam fora dela**, como estado próprio: nível, missões concluídas, metas, atributos e impressões dos lugares; vínculos com outras pessoas no M6.

## Retenção

- **Rotina:** a chance de jogar no dia vale `rotina × fator`, com fator = 1 + (engajamento − 700) / 1000, entre 0,15 e 1,3.
  - Quem acabou de entrar fica perto de 1; desanimado, joga menos.
- **Sessões flexíveis:**
  - estender a sessão segue a P3 (seção "Sono e dificuldade de parar" abaixo);
  - com frustração ≥ 600 e uma experiência ruim, pode sair na hora (30%).
- **Inativo:** 14 dias sem jogar (o dia de uma volta conta como presença). Volta com chance de 25% da rotina × intenção de continuar.
- **Desistente** (`player_quit`, com o que mais incomodava como causa):
  - intenção de continuar abaixo de 420: até 6% por dia, proporcional à distância do limite;
  - ou 45 dias sem jogar.
- **Retorno pela saudade** (D-039a):
  - só contam memórias boas realmente registradas e que ainda não motivaram uma volta;
  - cada volta por saudade gasta essas memórias, multiplica o intervalo mínimo (30 dias × voltas + 1) e divide a chance pelo mesmo fator;
  - chance de até 0,4% por dia, também pela lealdade;
  - a pessoa volta com o humor que tem (sem recomeço fabricado), joga no dia da volta e tem 14 dias de carência, em que só as novas sessões decidem.
- **Outros motivos de retorno** (sem essa exigência de memórias novas, porque quem está fora não cria experiências novas):
  - atualizações (M7), sobretudo as grandes ou marcantes, e eventos dentro do jogo, que também aumentam a vontade de prolongar a sessão;
  - convites de amigos (M6);
  - campanhas de marketing e streamers (M7).

## Sono e dificuldade de parar (P3, D-044)

Três traços separados, todos sorteados por perfil e independentes dentro dele:

| Traço | O que decide |
|---|---|
| Importância do jogo (P2; R4) | Prioridade do jogo: se joga no dia, se a vontade que sobra vira segunda sessão, jogar na pausa do trabalho e a pressa de começar. Não muda a duração (D-050) |
| Tendência a prolongar | Vontade de continuar depois da sessão normal (v3, ±20%) e, na hora de dormir, ficar acordado jogando quando a sessão está boa ou a meta está perto |
| Autocontrole | Freia ficar acordado jogando e a escapada no expediente |

- **Continuar e voltar (v3, D-051):** a sessão normal é uma preferência; ao chegar nela a pessoa decide continuar ou parar, e depois de uma pausa pode voltar. Saciedade e cansaço pesam contra; ver `rotina.md`.
- **Ficar acordado (extensão, só na hora de dormir):** motivo explícito, "gostando da sessão" (nota parcial ≥ 700) ou "perto de concluir a meta" (70% do caminho).
  - Chance = 40% × tendência × (1 − autocontrole × 0,8) × (1 − cansaço).
  - Tamanho = 30% da sessão planejada × (0,5 + tendência), mínimo 10 min.
  - Limites explícitos: 2 vezes por sessão e 180 min no total. Nunca na pausa ou na escapada do trabalho nem no cenário controlado.
  - Só quando a janela termina na hora de dormir e a sessão chegou ao fim dela (nunca antes de uma obrigação nem de um turno da noite); dentro do tempo livre a decisão é continuar.
  - O sono cortado tem três tetos: 180 min por noite, o teto do déficit e um sono mínimo de 4 h.
- **Sono:** cada pessoa tem necessidade de sono e hora preferida de dormir (P2).
  - O dia se monta a partir das obrigações. Noite mais curta que a necessidade vira **déficit**, e o sono cortado pelo jogo também entra no déficit na saída da sessão (evento `stayed_up_late`, com motivo e minutos).
  - Recuperação: manhã livre dorme até 2 h a mais; noite normal recupera 20 min.
  - Teto do déficit: 15 h × (0,5 + tolerância ao déficit), com a tolerância variando por pessoa.
- **Cansaço:** déficit ÷ 10 h, até 100%. O cansaço:
  - reduz a chance de jogar (até −40%) e a duração desejada (até −30%);
  - pesa na nota da sessão (motivo "Cansaço");
  - acima de 50%, "Cansado (dormiu pouco)" vira o motivo do dia sem jogo.
- **Jogar no horário de trabalho:** só em bloco de trabalho remoto.
  - **Pausa:** jogar no intervalo declarado na ocupação (`breaks`). A chance vem da importância do jogo; a sessão é curta e cabe na pausa.
  - **Escapada:** jogar no expediente. Rara, pela flexibilidade da ocupação (`playDuringWorkFlexibility`) × importância × falta de autocontrole × folga do dia (carga sorteada). Dura cerca de 25 min, longe do início e do fim do expediente.
  - Pausa e escapada são **interrompíveis**: a decisão pesa o custo de largar cada atividade (`interruptCost`: chefe 1000, emboscada 850 … comércio e conversa 0).
  - **Chamado do trabalho** (35% das escapadas): termina a atividade se falta pouco (regra de saída) ou larga e sai.
  - A agenda registra as sessões no trabalho e os minutos sobre o expediente. Ainda sem consequência profissional.
  - Deslocamento, trabalho presencial e estudo ficam protegidos (teste).

## Coerência dos horários (D-045)

Turno, sono, hora preferida de jogar e tempo livre precisam contar a mesma história.
- Ninguém acorda de madrugada para um turno diurno; o validador recusa o conteúdo que permitiria isso.
- A hora natural de dormir se adapta às obrigações: no dia livre, no máximo 1 h depois do dia útil.
- A hora preferida de jogar cai no tempo livre dos dias com obrigação, pela mesma conta do planejador, e acompanha a vontade de jogar:
  - quem dá muita importância ao jogo começa logo que chega;
  - o casual começa mais tarde e segue o próprio horário de lazer;
  - a sessão típica sempre cabe antes de dormir.

## Cenário controlado

O runner mede a rotina pura numa segunda rodada com `NeutralMood`: o humor não muda a frequência nem a permanência (sem fator de engajamento, sessões flexíveis, inatividade ou desistência). As metas de classe "regra do cenário" usam essa rodada. Na campanha completa, faltar por desânimo é resultado legítimo.

## O que falta (causas que chegam depois)

| Fonte de satisfação ou abandono | Quando |
|---|---|
| Mortes, derrotas e saque | M3 (combate) |
| Ouro, mercado, itens e equipamento | Etapa 1 e M5 (economia) |
| Chefes, PvP, guildas: interesse no fim do conteúdo além do nível | M3 e M6 |
| Amigos e grupos (inclusive convites para voltar) | M6 (social) |
| Atualizações que renovam a novidade, marketing e streamers | M7 (empresa) |

Por isso, hoje quase ninguém desiste em 90 dias: o conteúdo só começa a pesar depois de meses sem atualização.
