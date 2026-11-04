# Comportamento da pessoa simulada (fechamento do M2)

Como cada pessoa decide, sente e permanece no jogo. Complementa [atributos.md](atributos.md) (os traços e como mudam) e as decisões D-037, D-038 e D-039. Todos os números ficam em `Content/presets/inicial/population.json`, nas seções `session`, `mood`, `memory` e `retention`; o validador confere as faixas.

## Visão geral

| Camada | O que é | Onde age |
|---|---|---|
| Meta de curto prazo (D-036) | O que a pessoa quer agora: um nível, uma zona, uma missão, praticar algo | Fator "meta pessoal" na decisão |
| Plano da sessão (D-037) | A intenção da sessão: o foco escolhido na primeira decisão | Continuidade entre ciclos |
| Atividade | O que ela faz neste ciclo | Experiência, XP, tédio, memória |
| Humor (D-038) | Satisfação, frustração, novidade, engajamento, intenção de continuar | Rotina, sessão e retenção |
| Memória (D-038) | Impressão de cada lugar e acontecimentos marcantes | Fator "lembrança" e saudade |
| Retenção (D-039) | Ativo, inativo, desistente e retorno | Se e quanto a pessoa joga |

## Plano da sessão e continuidade

- **Formação:** a primeira decisão da sessão forma o plano. O motivo é o fator que mais pesou.
- **Continuar ou trocar:** ao fim de cada ciclo, continua no foco, a menos que outra opção seja melhor por:
  - 60 pontos depois do compromisso mínimo;
  - 150 pontos durante o compromisso mínimo, que dura 30 min + até 60 min pela paciência.
- **Revisão livre:** etapa ou missão concluída, nível novo, meta concluída ou abandonada e zona descoberta liberam a próxima decisão sem margem.
- **Sem custo afundado:** a viagem já feita não obriga a insistir.
- **Tédio contextual** (substitui a penalidade fixa de repetição):
  - cresce por tipo de atividade, 300 por hora;
  - cresce mais rápido para quem gosta de variedade (Exploração) e mais devagar para quem gosta muito daquele tipo;
  - fazer outra coisa alivia 400 por hora; entre sessões sobram 30%;
  - na decisão pesa até 300 pontos, aliviado pela paciência.
- **Explorar sem nada novo** ao alcance perde metade do gosto.

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
- **Novidade:** pesa pelo quanto a pessoa precisa de coisas novas (base 20%, Exploração 50%, impaciência 30%).
  - Gasta 20 por hora de jogo.
  - É renovada por zonas, atividades, níveis e missões novas.
  - Abaixo de zero é **saturação**: sem atualizações, o jogo cansa.

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
- **Acontecimentos marcantes:** até 12 por pessoa.
  - Quais: níveis múltiplos de 5, missão principal, meta concluída ou abandonada, zona descoberta, atividade ou sessão muito boa ou muito ruim, ficar sem nada para fazer.
  - Conquistas perdem 10 de intensidade por dia; frustrações, 25.
  - O mesmo fato se reforça; com a memória cheia, sai o mais fraco.

## Retenção

- **Rotina:** a chance de jogar no dia vale `rotina × fator`, com fator = 1 + (engajamento − 700) / 1000, entre 0,15 e 1,3.
  - Quem acabou de entrar fica perto de 1; desanimado, joga menos.
- **Sessões flexíveis:**
  - indo bem (nota parcial ≥ 700) e engajada, a pessoa estende a sessão uma vez em 30%, com chance de 40%;
  - com frustração ≥ 600 e uma experiência ruim, pode sair na hora (30%).
- **Inativo:** 14 dias sem jogar. Volta com chance de 25% da rotina × intenção de continuar.
- **Desistente** (`player_quit`, com o que mais incomodava como causa):
  - intenção de continuar abaixo de 420: até 6% por dia, proporcional à distância do limite;
  - ou 45 dias sem jogar.
- **Retorno do desistente** (`player_returned`):
  - depois de 30 dias, pela saudade (soma das memórias boas, que esmaecem) e pela lealdade, até 0,4% por dia;
  - volta com satisfação de pelo menos 550, frustração zerada e +300 de novidade.
  - Atualizações (M7) e convites de amigos (M6) serão os gatilhos principais.

## O que falta (causas que chegam depois)

| Fonte de satisfação ou abandono | Quando |
|---|---|
| Mortes, derrotas e saque | M3 (combate) |
| Ouro, mercado e itens | M5 (economia) |
| Amigos, grupos e guildas (inclusive convites para voltar) | M6 (social) |
| Atualizações que renovam a novidade, monetização | M7 (empresa) |

Por isso, hoje quase só desiste quem esgota o conteúdo (hardcore e competitivos, perto do nível 30).
