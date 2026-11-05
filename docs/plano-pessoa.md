# Plano: rotina de vida, traits e reputação

**Estado:** aprovado com ajustes (D-041, 04/11/2026). P0 (D-041), P1 (D-042), P2 (D-043) e P3 (D-044) feitas; P4 em seguida, depois a Etapa 1.

**Origem:** pedido do criador antes da Etapa 1.
- Reputação do personagem, traits comportamentais da pessoa e rotina de vida (obrigações, sono, tempo livre, vontade de jogar, localidade e fuso, finanças).
- Tudo influenciando decisões reais, com parâmetros em conteúdo e causas registradas.
- Observação do criador: o público de MMORPG tende a ter mais tempo livre e a jogar muito, mas ter tempo não significa ser hardcore. No futuro, as guildas também darão mais vontade de continuar jogando.

## 1. O que existe hoje e o que acontece com cada coisa

| Hoje | O que faz | Proposta |
|---|---|---|
| `preferredStartHour`, `daysPerWeek`, `weekendBoost` (por pessoa, do perfil) | Uma sessão por dia, no máximo. Chance diária = dias/7, com reforço no fim de semana. Começa perto da hora preferida. | **Substituídos** pelo calendário semanal: obrigações e tempo livre decidem quando dá; a vontade de jogar decide se joga. Um sistema só. |
| `sessionMinutes` | Duração típica ±20% | Vira **duração habitual**, limitada pela janela livre e pelo sono |
| Calendário da campanha (`world.json`: data inicial, feriados brasileiros) | Dia útil, fim de semana e feriado iguais para todos | Vira o **calendário do servidor**. Cada pessoa usa o calendário da sua residência, com feriados e fuso. |
| Engajamento (D-038) multiplicando a chance de jogar (D-039) | Desanimado joga menos | Vira um componente da **vontade de jogar**, ao lado da importância do jogo e do hábito |
| Sessão que se estende (D-039: +30% uma vez) | "Ficou mais um pouco" | **Substituída** pela dificuldade de parar com custo de sono (seção 3.5) |
| `leisureBudgetCentsPerMonth`, `willingnessToPay`, `paidAdvantageSensitivity` | Sem efeito | Viram o **modelo financeiro** (seção 6); implementação com a monetização (M7) |
| Traços contínuos: Paciência, Agressividade, Variedade, Oportunismo… (D-036/D-039a) | Pesos na decisão, no tédio, nas metas | **Continuam.** As traits nomeadas não os duplicam: atuam em pontos específicos e têm condições e contrapartidas próprias (seção 4). |
| Regra de PvP da zona: `none`, `consensual`, `open` | Só aumenta o risco percebido | Vira **regime de conduta** (seção 5). A perda por morte (`deathRule`) continua separada. |
| `BehaviorTuning` (pesos da decisão no código) | Pesos de afinidade, risco, viagem, missões… | **Vão para o conteúdo** na entrega P0 |

## 2. Princípios

- **Determinismo e estado:** inteiros, sorteios por fluxo derivado de (pessoa, dia, janela), estado novo no hash e eventos com causa.
- **Conteúdo configurável:** cada número fica no conteúdo e é validado; nada de parâmetro sem função real no momento em que entra.
- **Explicável:** a ficha diz por que a pessoa jogou ou não, dormiu menos ou escolheu um alvo.
  - Para não gerar milhares de eventos por dia, o motivo do dia fica como **estado** (a agenda do dia). Só o que é marcante vira evento.
- **Sem bônus mágicos:** trait da pessoa muda decisões, nunca o dano do personagem.
- **Horizontes realistas:** déficit de sono em dias; hábitos em semanas; traits adquiridas por padrões de semanas.
- **Público-alvo com mais tempo livre, mas sem automatismo:** a distribuição de ocupações favorece quem tem mais tempo livre, e a importância do jogo é sorteada quase independente do tempo livre (correlação fraca).
  - Há trabalhadores que priorizam o jogo nas poucas horas que têm, e desempregados que jogam pouco.
  - Os perfis (casual, hardcore…) passam a descrever estilo e dedicação, não horário.

## 3. Rotina de vida

### 3.1 Localidade, fuso e calendário

- **Conteúdo novo:** `localities.json`. Cada localidade tem:
  - id e nome;
  - deslocamento do fuso em minutos (inteiro; sem horário de verão por enquanto);
  - idiomas;
  - feriados;
  - dias de fim de semana.
- **Na pessoa:**
  - nacionalidade (contexto, sem efeito sozinha);
  - localidade de residência (fuso e feriados);
  - idiomas (o social do M6 usa).
  - A nacionalidade não determina horário, renda nem personalidade.
- **O relógio do mundo é global (horário do servidor).** Cada pessoa vê a própria rotina no horário local: `local = servidor + diferença de fuso`.
- **Começo proposto:**
  - Brasil, UTC−3, pt (cerca de 85%);
  - Portugal, UTC+0, pt;
  - Argentina, UTC−3, es;
  - Estados Unidos (Leste), UTC−5, en.
  - Os feriados atuais passam para a localidade Brasil.

### 3.2 Obrigações e tempo livre

- **Modelos de ocupação** (conteúdo). Cada um traz dias, turnos, horários, deslocamento e variação individual:
  - trabalho comercial (seg–sex, dia);
  - escala de fim de semana;
  - turno noturno;
  - meio período;
  - estudo diurno;
  - estudo noturno;
  - autônomo flexível;
  - sem ocupação.
- **Combinações:** uma pessoa pode ter até duas obrigações (trabalho + estudo), desde que não se sobreponham.
- **Tempo livre por dia local:** 24 h − obrigações − deslocamento − sono − tempo fixo de vida (refeições e tarefas, configurável). O resultado são **janelas livres**, por exemplo antes do trabalho, à noite ou a tarde do fim de semana.
- **Feriados e fins de semana** liberam as obrigações de quem as tem nesses dias. Para quem trabalha em escala, podem ser dia de trabalho. Não há aumento universal de atividade.
- **Distribuição inicial** (hipótese para o público de MMORPG, a calibrar):

| Ocupação | Participação |
|---|---:|
| Estudante diurno | 22% |
| Trabalho comercial | 30% |
| Trabalho + estudo noturno | 8% |
| Meio período | 10% |
| Escala, turno ou noturno | 10% |
| Autônomo flexível | 6% |
| Sem ocupação | 14% |

### 3.3 Vontade de jogar

Ter tempo livre não significa jogar o tempo todo. Para cada janela livre, a pessoa decide se joga e por quanto tempo, com base em:
- **importância do jogo entre os lazeres** (traço novo; é o principal diferencial do "hardcore");
- **hábito** (traço novo: regularidade quando há tempo);
- **engajamento atual** (humor, D-038);
- **fadiga** (sono, seção 3.4);
- **metas e acontecimentos** (meta perto de concluir, conteúdo novo);
- **tamanho da janela**: janelas curtas valem uma sessão curta, ou nenhuma, se a pessoa não gosta de sessões curtas.

A rotina permite nenhuma, uma ou mais sessões por dia, como uma sessão curta antes do expediente e outra à noite.

### 3.4 Sono

- **Traços novos:**
  - necessidade de sono (cerca de 8 h ± 45 min);
  - cronotipo (matutino ↔ noturno: posição preferida do sono);
  - tolerância ao déficit.
- **Déficit acumulado** em minutos:
  - sobe quando dorme menos que a necessidade;
  - cai com sono extra nos dias livres (até +2 h por noite, configurável);
  - tem teto: acima de um limite, a pessoa não estende mais.
- **Fadiga**, derivada do déficit: reduz a vontade de jogar no dia seguinte e as sessões longas. No M3, também a execução no combate, como penalidade de desempenho da pessoa, não do personagem.

### 3.5 Gostar do jogo × dificuldade de parar

São coisas separadas:
- **importância do jogo:** quanto quer jogar;
- **tendência a prolongar:** quanto a sessão "puxa";
- **autocontrole:** respeitar o horário planejado.

Perto do fim planejado, a pessoa pode estender a sessão:
- quando está indo bem ou falta pouco para uma meta, conforme a tendência a prolongar e o autocontrole;
- até o limite da janela, e além dela só se entrar no sono, gerando déficit;
- com teto de extensão e sem passar do déficit máximo.

A extensão gera evento com causa, como "estendeu para concluir a meta, dormiu 1 h 20 a menos".

### 3.6 Explicação na ficha

Cada pessoa guarda a **agenda do dia** como estado: janelas livres, sessões planejadas, o que jogou e o motivo de não jogar.
- **Motivos de não jogar:**
  - "sem tempo livre";
  - "trabalho à noite";
  - "cansado (dormiu pouco)";
  - "sem vontade (engajamento baixo)";
  - "tinha tempo, preferiu outro lazer".
- **Na aba Rotina:**
  - a semana típica (obrigações, sono e janelas);
  - os últimos 14 dias, com o motivo de cada dia;
  - o déficit de sono.

## 4. Traits (características particulares da pessoa)

### 4.1 Modelo (conteúdo `traits.json`)

Cada trait declara:
- **classificação:** positiva ou negativa, explícita;
- **efeitos numéricos:** cada efeito age num **ponto de avaliação** conhecido pelo Core, com condição;
- **fatores de decisão que modifica**, que aparecem na explicação da ficha;
- **aquisição, manutenção e perda:** se é inicial (predisposição), adquirida por evidência ou ambas;
- **incompatibilidades e limite de acúmulo;**
- **geração:** peso, requisitos e pesos pelos traços contínuos;
- **dependências:** de quais sistemas depende (combate, social, economia). Sem eles, a trait existe mas fica "sem efeito até o M…", e só é sorteada se for predisposição;
- **texto da ficha.**

**Pontos de avaliação** são uma lista fechada, como os efeitos de magia (plano 6.4). Novos pontos exigem implementação e teste.

| Ponto | Já existe hoje? | Para que serve |
|---|---|---|
| Margem de troca do plano; minutos de compromisso | Sim (D-037) | Persistente, Impulsivo |
| Tempo até abandonar uma meta; frustração por revés | Sim (D-036/D-038) | Persistente |
| Revisão do plano por acontecimento; ruído da decisão | Sim | Impulsivo |
| Valor de algo nunca feito: atividade ou lugar novo | Sim (novidade) | Colecionador (parte) |
| Atratividade de alvo, defesa de terceiros, confronto equilibrado | M3 / Etapa 3 | Matador e Protetor de Novatos, Competidor Honrado |
| Iniciativa de grupo, compromisso com conhecidos | M6 | Líder Nato, Companheiro Fiel |
| Reserva de ouro, atenção a preços, itens não possuídos | Etapa 1 / M5 | Mercador Cauteloso, Caçador de Barganhas, Colecionador (itens) |

### 4.2 Geração

- **Quantidade:** sorteada de uma tabela em conteúdo, com máximo 6 e 2 como mais comum. Proposta: 1 → 15%, 2 → 35%, 3 → 22%, 4 → 14%, 5 → 9%, 6 → 5%. Zero traits? Decisão do criador.
- **Classificação,** separada da quantidade: para cada vaga, sorteia positiva ou negativa.
  - A chance de negativa vem do conteúdo (ex.: 35%), ajustada por traços de conduta (propensão a trapaça e oportunismo aumentam; lealdade reduz), com piso e teto (ex.: 15% a 60%).
  - Nenhuma regra garante só positivas ou só negativas.
- **Escolha da trait:** dentro da classificação, ponderada pelos pesos e requisitos de cada trait (ex.: Matador de Novatos pesa pela agressividade e pelo oportunismo).
  - Sem duplicar.
  - Respeita as incompatibilidades sobre o **conjunto inteiro**.
  - Se não houver candidata válida, a vaga fica vazia, sem forçar.
- **Sobre usar "intelecto":** recomendo **não** usar.
  - Não existe intelecto da pessoa: o Talento é potencial de aprendizado (D-036a) e não deve significar boa conduta.
  - Mais traits não significa pessoa melhor.
  - Se o criador quiser um parâmetro para a quantidade, proponho a **marcância da personalidade**: quão distintiva é a pessoa, oculta e sem relação com conduta.
- **Limites:**
  - total de 8 traits com as adquiridas;
  - no máximo 1 adquirida a cada 30 dias de jogo;
  - perda só por evidência contrária sustentada.

### 4.3 Catálogo inicial

| Trait | Classe | Efeito (ponto de avaliação) | Contrapartida | Aquisição | Incompatível com | Ativa em |
|---|---|---|---|---|---|---|
| Persistente | + | Menos frustração por revés; demora mais a abandonar metas após falhas; margem de troca maior depois de um revés | Insiste além do vantajoso (custo de oportunidade visível na decisão) | Inicial ou por metas concluídas depois de falhas | Impulsivo | P4 (agora) |
| Impulsivo | − | Margem de troca menor; revisa o plano com mais acontecimentos; mais ruído; mais chance de largar a sessão com frustração | Troca de plano mais facilmente | Inicial | Persistente, Mercador Cauteloso | P4 (agora) |
| Colecionador | + | Valor extra para atividade ou lugar nunca feito; depois, para itens não possuídos | Gasta tempo e recursos em coisas pouco úteis | Inicial | — | P4 (parte); itens na Etapa 1/M5 |
| Matador de Novatos | − | Atratividade de alvos percebidos como mais fracos, pesada pelo risco de reação e de punição | Mais exposição a punições e conflitos; nenhum dano extra | **Só adquirida:** padrão de mortes injustificadas de vítimas bem mais fracas (ex.: 5 em 30 dias, diferença ≥ 5 níveis); perde após 60 dias sem o padrão | Protetor de Novatos, Competidor Honrado | Etapa 3 |
| Protetor de Novatos | + | Prioriza ajudar iniciantes e enfrentar seus agressores | Sacrifica progresso e assume riscos | Inicial ou por defesas de iniciantes | Matador de Novatos | Etapa 3 |
| Competidor Honrado | + | Prefere confrontos equilibrados; evita atacar indefesos | Deixa passar vitórias fáceis | Inicial | Matador de Novatos | Etapa 3 |
| Líder Nato | + | Mais iniciativa para formar grupos e organizar objetivos | Não garante obediência nem sucesso | Inicial ou por grupos liderados | — | M6 |
| Companheiro Fiel | + | Prioriza compromissos com conhecidos | Escolhe atividades menos eficientes para acompanhar amigos | Inicial | — | M6 |
| Mercador Cauteloso | + | Mantém reservas; avalia risco antes de negociar | Perde oportunidades lucrativas | Inicial | Impulsivo | M5 |
| Caçador de Barganhas | + | Mais atenção a diferenças de preço | Gasta tempo procurando negócio | Inicial | — | M5 |

Traits de M3, M5 e M6 podem ser sorteadas desde já como predisposições. Elas aparecem na ficha como "sem efeito até o M…", para não mudar as populações quando os sistemas chegarem. Matador de Novatos não nasce com a pessoa.

## 5. Reputação do personagem

- **Pertence ao personagem, não à pessoa.** Cada personagem futuro terá a sua.
- **Não altera amizades, afinidade nem relações de guilda.** Acontecimentos concretos afetam relações pelos sistemas sociais (M6); o saldo, por si só, nunca.
- **Começa geral,** com espaço para reputações locais ou de facção.
- **Regime de conduta da zona**, separado da perda por morte:

| Regime | Atacar quem não é hostil | Cidades |
|---|---|---|
| Sem PvP | Impossível | Sempre |
| Consensual | Só entre quem aceitou (duelo, PvP ativado) | — |
| Criminoso | Permitido, mas é crime | — |
| Livre | Permitido sem penalidade (zona de guerra) | — |

  Full loot decide só o que se perde ao morrer, não a legitimidade.
- **Modo PK:** ativar PK sinaliza o personagem como hostil. A penalidade começa no ataque injustificado; o conteúdo pode mudar para "começa ao ativar".
- **Registro de agressão** (estado):
  - agressor, vítima e instante;
  - zona e regime;
  - se foi justificada;
  - janela de defesa, durante a qual a vítima e seus aliados revidam sem penalidade;
  - participantes (suporte e grupo quando existirem);
  - como terminou.
  - Penalidades valem **por episódio**, não por golpe.
- **Saldo** com fontes limitadas:
  - missões (por categoria; repetição sem ganho dentro de um prazo);
  - criaturas (ganho pequeno, só de desafio relevante, com teto diário);
  - ataque injustificado (−);
  - morte da vítima (− adicional).
- **Histórico de crimes separado do saldo.** O estado "criminoso" vem dos crimes ativos, que só esfriam com o tempo de boa conduta. Farmar criaturas não apaga assassinatos.
- **Mortes repetidas contra a mesma pessoa** num prazo não geram vantagem e ficam marcadas como suspeitas (combinadas).
- **Consequências** (guardas, serviços, recrutamento) só quando esses sistemas existirem.
- **Quando:** na Etapa 3 (PvP), porque precisa de ataques, mortes e autoria das agressões. Os ganhos por missão e criatura entram junto, para nascer com os limites contra exploração.

## 6. Situação financeira

- **Modelo** (por pessoa, mensal, em centavos):
  - renda do trabalho, vinda da ocupação, com variação;
  - apoio familiar ou outras fontes;
  - despesas fixas;
  - reserva;
  - orçamento de lazer = (renda + apoio − despesas) × parcela de lazer;
  - parcela que aceita destinar ao MMORPG.
- O trabalho afeta a renda e a disponibilidade por mecanismos separados. Não ter emprego não significa não ter dinheiro.
- O interesse em RMT é separado das compras oficiais (M9).
- **Quando:** proponho implementar com a monetização (M7), porque antes disso não tem função. Fica documentado agora, e a ocupação já nasce com a faixa de renda no modelo.

## 7. Entregas verificáveis

| Entrega | Escopo | Verificação |
|---|---|---|
| **P0 — Parâmetros e conceitos** | `BehaviorTuning` vai para o conteúdo (seção `behavior`); este plano aprovado; esquemas novos validados (`localities`, `occupations`, `traits`) | Mesmo hash de estado antes e depois, porque só os números mudaram de lugar; testes de validação dos esquemas |
| **P1 — Localidade e fuso** | Localidades, residência, nacionalidade, idiomas; rotina em horário local; feriados por residência; relatório por hora do servidor | O teste "trabalhador 19h–23h" passa no horário local; feriado só para quem mora lá; pico do servidor espalhado pelos fusos |
| **P2 — Obrigações, tempo livre e vontade de jogar** | Modelos de ocupação e combinações; janelas livres; nenhuma, uma ou mais sessões por dia; importância do jogo e hábito; agenda e motivo do dia na ficha; substitui dias por semana, reforço de fim de semana e hora preferida | Cenários contrastantes: trabalhador com pouco lazer, estudante, desempregado que joga pouco, trabalhador que prioriza o jogo; metas de rotina redefinidas por ocupação |
| **P3 — Sono e dificuldade de parar** | Necessidade, cronotipo, déficit e recuperação; extensão com custo de sono no lugar da extensão do D-039; fadiga | Quem prolonga dorme menos e recupera no fim de semana; o déficit tem teto; nenhuma extensão ilimitada |
| **P4 — Traits** | Catálogo, geração (quantidade, classificação, incompatibilidades), aquisição e perda por evidência, ficha; efeitos ativos só de Persistente, Impulsivo e Colecionador (parte) | Teste estatístico com 10 mil pessoas: quantidades, proporção de classificações e nenhuma combinação proibida; efeitos medidos por decisão |
| **Etapa 1** | Personagem (D-040) | Como no roteiro |
| **R — Reputação** (com a Etapa 3) | Regimes de zona, agressões, crimes, saldo com limites; ativar traits de PvP | Agressão injustificada penaliza só o agressor; defesa legítima sem penalidade; PvP consentido segue o regime; farm não apaga crimes; personagens com reputações independentes |
| **Depois** | Finanças (M7); idioma, fuso, Líder Nato, Companheiro Fiel e guildas ancorando a vontade de jogar (M6); RMT (M9) | Nos marcos |

Cada entrega fecha com testes, validação de 5 seeds, documentação, exportação e resumo.

## 8. Decisões do criador (D-041)

- **Traits:**
  - pode haver pessoa sem traits;
  - duas é o mais comum, e mais de duas é difícil (≈ 17%);
  - a marcância desloca a tabela com efeito moderado;
  - as iniciais precisam fazer sentido com atributos, personalidade e perfil;
  - o catálogo deve ser grande e criativo;
  - às vezes uma pessoa tem só negativas.
- **Localidades:** Brasil, Argentina, Estados Unidos, China.
- **Ocupações:** criativas e realistas (P2).
- **Finanças:** M7, mas a renda real já entra na P2, sem efeito.
- **Ordem:** P0 → P4, Etapa 1; reputação na Etapa 3.

### Perguntas originais (respondidas)

1. **Quantidade de traits:** o mínimo é 1 ou 0? Proposta: 1 → 15%, 2 → 35%, 3 → 22%, 4 → 14%, 5 → 9%, 6 → 5%.
2. **Parâmetro da quantidade:** nenhum (só a tabela, como recomendo) ou marcância da personalidade. O "intelecto" fica de fora em qualquer caso.
3. **Localidades iniciais e participações** (seção 3.1) e fuso do servidor (UTC−3).
4. **Distribuição de ocupações** do público (seção 3.2).
5. **Finanças só no M7**, com o modelo documentado agora.
6. **Ordem:** P0 → P1 → P2 → P3 → P4 antes da Etapa 1, e a reputação junto da Etapa 3.
