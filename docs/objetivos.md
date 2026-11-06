# Objetivos pessoais

Decisões no D-062. Entregas: **O1** (catálogo, dados, sorteio inicial, viabilidade e aba Objetivos, sem influência), **O2** (ligação com metas e satisfação) e **O3** (evolução).

## 1. Objetivo × meta

| | Objetivo | Meta |
|---|---|---|
| O que é | o que a pessoa quer conquistar naquele MMORPG, atravessando várias sessões | um passo de curto prazo: chegar a um nível, descobrir uma zona, concluir uma missão, praticar uma atividade |
| Onde fica | na pessoa (sobrevive a personagens futuros) | no personagem |
| Quantos | 1 ou 2 gerais e de 0 a 3 secundários | até 2 ativas |
| Influência | O1: nenhuma. O2: só através das metas | fator "meta" na decisão |

- As metas atuais continuam iguais.
- **Geral e secundário** são a importância **para aquela pessoa**, não categorias do catálogo. Entrar numa guilda pode ser a ambição central de um jogador e um projeto secundário de outro.
- **Horizonte** (curto, médio ou longo prazo) é uma característica do objetivo, separada da importância. Um secundário também pode ser de longo prazo.
- Os objetivos não formam uma escada obrigatória: podem ser independentes ou relacionados.

## 2. Catálogo inicial (`objectives.json`, valores candidatos)

| Objetivo | Critério verificável | Depende de | Preparação possível hoje | Dificuldade | Horizonte |
|---|---|---|---|---|---|
| Alcançar o nível máximo | nível = máximo | — (pode avançar) | — | 650 | longo |
| Concluir a campanha principal | todas as missões principais | — (pode avançar) | nível mínimo da próxima missão principal | 450 | médio |
| Conhecer todo o mundo | todas as zonas conhecidas | — (pode avançar) | — | 500 | longo |
| Ter um personagem entre os mais fortes | classificação real de força, não o índice de poder | combate, classificações | nível máximo | 1000 | longo |
| Completar os grandes desafios PvE | vencer os chefes do mundo | grupos, combate | nível do próximo chefe | 900 | longo |
| Ser competitivo no PvP | posição relevante no PvP real | PvP real, classificações | nível da primeira atividade PvP | 900 | longo |
| Acumular uma grande fortuna | riqueza **possuída** (saldo; patrimônio com a economia), não a soma do ouro recebido | economia | — | 800 | longo |
| Criar e liderar uma guilda | fundar e manter uma guilda | guildas | — | 850 | longo |
| Participar de uma guilda compatível | ser membro de uma guilda com interesses parecidos | guildas | — | 200 | curto |
| Ser um dos melhores fabricantes (de armas, armaduras ou consumíveis) | posição entre os fabricantes da especialidade | fabricação, classificações | — | 850 | longo |
| Ser um dos maiores comerciantes | prestígio no mercado: volume e reputação | economia, classificações | — | 850 | longo |
| Conquistar um equipamento raro (um item raro ou épico que a classe usa) | ter o item | itens e inventário | nível exigido pelo item | 350 | médio |

- **Sem inventar alvo:** o equipamento raro é escolhido entre os itens raros ou épicos do conteúdo que a classe pode usar. O Guerreiro não tem arma rara no conteúdo atual, só a Coroa do Rei Goblin, que é épica e serve para qualquer classe.
- A especialidade de fabricação é uma categoria de item que existe no conteúdo.
- **Entrar numa guilda e criar uma** podem coexistir: a pessoa pode querer participar de uma agora e liderar a própria no futuro. Metas operacionais contraditórias no mesmo momento serão evitadas na O2.
- **"Comerciante reconhecido" virou "Ser um dos maiores comerciantes":** é prestígio pelo volume e pela reputação no mercado, separado de riqueza. Pesa pela Competitividade, que é a busca de reconhecimento.
- "Conhecer uma região" continua sendo meta de exploração, não objetivo. "Participar de uma comunidade" ficou de fora, por ser vago: o objetivo é a guilda.

## 3. Sorteio inicial (no registro)

O sorteio acontece no registro, com a classe já escolhida, porque o equipamento raro depende dela. Usa um fluxo de números próprio (`population.objectives`), então não muda nenhum outro sorteio.

1. **Quantidades:** gerais pelos pesos `[0, 750, 250]` (1 ou 2, a maioria com 1); secundários pelos pesos `[200, 350, 300, 150]` (de 0 a 3).
2. **Ajuste de cada objetivo à pessoa:** produto dos termos de afinidade e do termo de ambição, limitado de 0,2 a 4.
   - **Afinidades:** as mesmas chaves das traits (`preferences.x`, `traits.x` de personalidade e conduta, `personality.x`, `profile.x`) e mais `quirk.x`, ter a trait. Exemplos:
     - **Conhecer todo o mundo:** preferência Exploração (forte), Variedade; Colecionador e Observador aumentam.
     - **PvP:** preferência PvP, Competitividade e Agressividade; Pacífico e Nervoso no PvP diminuem; Competidor honrado aumenta.
     - **Guildas:** Sociabilidade e preferência Social; Líder nato aumenta "criar", Lobo solitário diminui as duas.
     - **Fortuna:** preferência Comércio e Oportunismo; Acumulador aumenta, Gastador compulsivo diminui.
   - **Ambição pela dedicação (Perfil):** termo = 1 + 0,6 × a × d, com a = (importância do jogo − 500)/500 e d = (dificuldade − 500)/500. Quem se dedica muito puxa para objetivos de fim de jogo; quem se dedica pouco, para os mais simples. É tendência, não regra.
3. **Escolha:** sorteio ponderado (peso do catálogo × ajuste), sem repetir.
4. **Importância:** 500 + (ajuste − 1000) × 25% ± 100, de 50 a 1000. Os mais importantes para a pessoa são os gerais.
5. **Comprometimento:** 300 + 40% da importância do jogo + 15% da paciência + 15% da lealdade. Será usado na O2.
6. **Motivos:** os três termos que mais pesaram ficam guardados, para a ficha e para o evento `objective_set`.

No cenário controlado ninguém recebe objetivos, para manter as pessoas iguais em cada célula.

## 4. Viabilidade

Calculada a cada consulta, nunca guardada como progresso:

| Viabilidade | Quando |
|---|---|
| **Pode avançar agora** | os sistemas existem e nenhum requisito conhecido falta |
| **Exige preparação** | os sistemas existem, mas falta um requisito conhecido do conteúdo (hoje, o nível mínimo da próxima missão principal) |
| **Depende de sistema futuro** | o critério usa um sistema que ainda não existe. Não há progresso |

- **Preparação e etapa final aparecem separadas.** Exemplo: "Preparação: nível 15 (Enfrentar o Rei Goblin)" e "Etapa final indisponível: depende de Grupos e Combate".
- A preparação só usa **requisitos concretos do conteúdo**:
  - o nível mínimo do chefe;
  - o nível mínimo da atividade PvP;
  - o nível exigido pelo item;
  - o nível máximo, para "mais forte".
- Não há requisito inventado. Guilda, fortuna, fabricação e comércio não têm preparação.
- **Patrulha e emboscada** só entram como requisito de acesso ao conteúdo PvP. Nunca contam como vitória ou experiência de combate.
- **Progresso** só aparece quando é mensurável com o que existe: nível de 1 a 30, missões principais concluídas e zonas conhecidas.
- **Sistemas que existem hoje** (`ObjectiveSystems.Implemented`): níveis, missões e exploração. Quando outros sistemas chegarem, entram nessa lista e o critério deles passa a ser avaliado.

## 5. Interface

- **Aba Objetivos** (depois da Visão geral):
  - Objetivos gerais e Objetivos secundários. Cada um mostra:
    - emblema e nome;
    - importância;
    - horizonte;
    - viabilidade;
    - progresso, quando mensurável;
    - preparação e etapa final indisponível.

    O tooltip mostra a descrição, a origem, o comprometimento, a data e o que pesou na escolha.
  - Metas atuais.
  - Histórico.
- **Visão geral:** só uma linha, "Objetivo principal", no card Progresso e metas, com acesso à aba.

## 6. Medição da O1

Validação padrão (5 seeds × 1.000 × 90 dias), 5.000 pessoas registradas. Detalhes em `baselines/objetivos/`.

- **Comportamento igual:** `validation.csv` e `people.csv` (fora a coluna nova) são idênticos aos da E1a.
- **Quantidades:**
  - gerais: 74,6% das pessoas com um e 25,4% com dois;
  - secundários: 19,6% com nenhum, 35,5% com um, 29,3% com dois e 15,6% com três.
- **Coerência** (presença do objetivo no terço de baixo × terço de cima):

  | Objetivo | Medida por | Terço de baixo | Terço de cima |
  |---|---|---|---|
  | Conhecer todo o mundo | Exploração | 11,6% | 32,3% |
  | Ser competitivo no PvP | PvP | 9,3% | 31,4% |
  | Participar de uma guilda | Social | 14,1% | 37,0% |
  | Melhor fabricante | Fabricação | 7,5% | 30,6% |
  | Grande fortuna | Comércio | 14,6% | 28,5% |
  | Mais forte | Competitividade | 12,0% | 29,5% |
  | Maiores comerciantes | Competitividade | 9,1% | 18,9% |
  | Grandes desafios PvE | PvE | 12,0% | 27,4% |

  Nenhum objetivo fica exclusivo de um grupo.
- **Ambição por perfil** (objetivos gerais):

  | Perfil | Dificuldade média | Fim de jogo (≥ 850) | Com um geral que pode avançar agora |
  |---|---|---|---|
  | Casual | 626 | 39,9% | 35,3% |
  | Regular | 675 | 47,6% | 31,2% |
  | Hardcore | 766 | 61,8% | 23,4% |
  | Devoto | 750 | 54,9% | 19,6% |

  - O objetivo geral mais comum entre os casuais é participar de uma guilda (18%).
  - Entre os hardcore, são "mais forte", PvP e os chefes (15% a 17%).
- **Consequência do mundo atual:** só 31,6% das pessoas têm um objetivo **geral** que pode avançar agora, e 60,0% têm algum objetivo que pode avançar agora.
  - As demais ambições dependem de sistemas futuros. Algumas têm preparação por nível.
  - Isso não é corrigido com progresso fictício. Na O2, as metas só se ligam ao que avança de verdade ou à preparação concreta.
- **Entrar e fundar guilda juntos:** 5,2% das pessoas.

## 7. Decisões já tomadas para O2 e O3

- **Caminho da influência:** a influência nas atividades passa só pelas metas, sem bônus direto pelo mesmo objetivo.
- **Uma meta para vários objetivos:** pode contribuir para mais de um, sem multiplicar o bônus.
- **Metas sem vínculo** continuam existindo.
- **Conquistas e reveses** da meta e do objetivo podem ter significados diferentes, mas não duplicam a mesma recompensa emocional.
- **Nota da sessão:** nenhum termo novo por enquanto.
- **Progresso por XP não substitui avanço** em riqueza, fabricação, guilda ou outros temas. Esses temas serão integrados quando houver progresso real nos sistemas deles.
- **Deixar um objetivo de lado** por uma nova prioridade (substituição voluntária) não gera frustração automática. Isso é diferente de abandonar por experiência negativa.
- **Estabilidade:** os objetivos mudam só por acontecimentos simulados, com motivo registrado.
