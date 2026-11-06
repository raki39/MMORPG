# Objetivos pessoais

Decisões no D-062 (O1), no D-063 (O2) e no D-064 (O3). Entregas: **O1** (catálogo, dados, sorteio inicial, viabilidade e aba Objetivos, sem influência), **O2** (ligação com metas e satisfação) e **O3** (evolução).

## 1. Objetivo × meta

| | Objetivo | Meta |
|---|---|---|
| O que é | o que a pessoa quer conquistar naquele MMORPG, atravessando várias sessões | um passo de curto prazo: chegar a um nível, descobrir uma zona, concluir uma missão, praticar uma atividade |
| Onde fica | na pessoa (sobrevive a personagens futuros) | no personagem |
| Quantos | 1 ou 2 gerais e de 0 a 3 secundários | até 2 ativas |
| Influência | só através das metas (O2): adoção e comprometimento | fator "meta" na decisão |

- As metas atuais continuam iguais.
- **Geral e secundário** são a importância **para aquela pessoa**, não categorias do catálogo. Entrar numa guilda pode ser a ambição central de um jogador e um projeto secundário de outro.
- **Horizonte** (curto, médio ou longo prazo) é uma característica do objetivo, separada da importância. Um secundário também pode ser de longo prazo.
- Os objetivos não formam uma escada obrigatória: podem ser independentes ou relacionados.

## 2. Catálogo inicial (`objectives.json`, valores candidatos)

| Objetivo | Critério verificável | Depende de | Preparação possível hoje | Dificuldade | Horizonte |
|---|---|---|---|---|---|
| Alcançar o nível máximo | nível = máximo | — (pode avançar) | — | 650 | longo |
| Concluir a campanha principal | todas as missões principais | — (pode avançar) | nível mínimo da próxima missão principal | 450 | médio |
| Conhecer todo o mundo | todas as zonas elegíveis do mundo (alcançáveis a partir da cidade inicial) | — (pode avançar) | — | 500 | médio (no preset atual, de 15 zonas) |
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
- **Progresso** só aparece quando é mensurável com o que existe: nível de 1 a 30, missões principais concluídas e zonas elegíveis já conhecidas pela pessoa.
- **"Conhecer todo o mundo":** o total é o de zonas elegíveis do mundo (no preset, as 15, todas alcançáveis), não o de zonas que a pessoa já conhece. Os marcos de exploração não contam, porque não são registrados por pessoa.
- **Requisito conhecido pela pessoa** (O2): a preparação de chefe ou PvP só vale para ela se conhece a zona ou tem a pista daquela atividade. A missão principal vale se conhece a zona do quadro. O item escolhido e o nível máximo ela já conhece. A ficha mostra o requisito mesmo assim, com a nota "a pessoa ainda não conhece este lugar".
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

## 7. Ligação com metas e satisfação (O2)

Configuração em `objectives.json`, seção `influence` (valores candidatos).

### 7.1 Vínculo meta → objetivo

Calculado quando a meta é candidata e guardado com ela (objetivo, tipo e de onde vem). Só existe vínculo quando a meta contribui de verdade:

| Meta | Objetivo | Tipo |
|---|---|---|
| chegar a um nível ou desbloquear por nível | Alcançar o nível máximo | avanço |
| concluir uma missão **principal** | Concluir a campanha principal | avanço |
| descobrir uma zona elegível ainda desconhecida | Conhecer todo o mundo | avanço |
| chegar a um nível, com o requisito de nível ainda não cumprido e conhecido pela pessoa | Grandes desafios PvE (nível do próximo chefe), PvP (nível da primeira atividade PvP), Equipamento raro (nível do item), Mais forte (nível máximo), Campanha (nível da próxima missão principal) | preparação |

- **Nunca há vínculo** com guilda, fortuna, fabricação ou comércio: nenhum requisito existe ainda, e o nível não serve de preparação genérica.
- **Praticar uma atividade** não se liga a nada, porque não avança nenhum critério concreto.
- **Missão secundária** não avança a campanha.
- **Metas independentes** (necessidade, oportunidade ou vontade do momento) continuam iguais.

### 7.2 Influência

- **Na adoção:** peso da meta candidata × (1 + reforço), com reforço = 1,0 × importância × comprometimento do objetivo.
  - Vale **só o maior reforço** entre os objetivos que a meta atende. Ter mais objetivos não dá vantagem.
- **No comprometimento da meta adotada:** o objetivo mais forte fecha parte da distância até 100%: 0,4 × importância × comprometimento.
  - O comprometimento já pesa no fator "meta" da decisão. É por esse caminho, e só por ele, que o objetivo chega às atividades.
- **Nenhum bônus direto nas atividades** e nenhuma candidata nova: as metas que já existem cobrem os próximos passos concretos (próxima missão principal, zona da fronteira, nível seguinte).
- **A prioridade de progressão inicial** continua separada, com a regra de "vale o maior" entre ela e as metas de progressão.

### 7.3 Conclusão e satisfação

- **Quando:** depois de subir de nível, concluir uma missão ou descobrir uma zona, os objetivos com o critério cumprido são marcados como concluídos, com a data, o histórico, o evento `objective_completed` e a causa.
- **Recompensa:** conquista de 150 pontos na sessão e memória "concluiu o objetivo" (intensidade 800).
- **Mesmo acontecimento:** se ele também concluiu uma meta ligada ao objetivo, vale **a maior recompensa**, não a soma.
  - Exemplo: descobrir a última zona conclui a meta (80) e o objetivo (150): a sessão recebe 150.
  - A memória do objetivo é própria, porque tem outro significado; os pontos não se repetem.
- **Nota da sessão:** nenhum termo novo.
- **Progresso por XP não substitui avanço** em riqueza, fabricação, guilda ou outros temas. Esses temas entram quando houver progresso real nos sistemas deles.
- **Influência desligada** (`influence.enabled = false`): os vínculos e as conclusões continuam registrados, sem nenhum efeito. É a base da medição pareada.

### 7.4 Interface

- **Metas atuais (aba Objetivos):** cada meta mostra "Ajuda: objetivo (avanço)" ou "objetivo (preparação: requisito)", ou "Meta independente".
- **Objetivos:**
  - "Metas ligadas: N adotadas · N concluídas · N abandonadas";
  - "Concluído em dd/mm" quando o critério foi cumprido;
  - progresso com a unidade (missões, zonas).
- **Visão geral:** o tooltip de cada meta mostra o que ela ajuda.
- **1366×768:** as ações do cabeçalho (Seguir no mapa, Localizar, Favoritar, Ver área) viram só ícone, com o rótulo no tooltip, para as abas caberem numa linha. Os cards empilham, como define o kit visual.

### 7.5 Medição (O2)

Rodadas pareadas: as mesmas pessoas e os mesmos objetivos, com a influência **desligada** × **ligada** (5 seeds × 1.000 × 90 dias). Detalhes em `baselines/objetivos/O2/analise.md`.

- **A desligada é idêntica à O1:** mesmo `validation.csv` e as mesmas colunas antigas de `people.csv`. Registrar vínculos e conclusões não muda nada.
- **Grupos** (pelos objetivos sorteados):
  - **acionáveis** (2.999 pessoas): algum objetivo pode avançar agora;
  - **só preparação** (1.422): nenhum pode avançar, mas algum tem preparação por nível;
  - **indisponíveis** (579): só objetivos de sistemas futuros, sem requisito.

| Desligada → ligada | Acionáveis | Só preparação | Indisponíveis | Todos |
|---|---|---|---|---|
| % das metas ligadas a objetivos | 33,0 → 36,1 | 20,5 → 23,1 | 0 → 0 | 25,5 → 28,1 |
| metas ligadas: % concluídas | 79,2 → 80,3 | 89,8 → 91,2 | — | 81,7 → 82,9 |
| metas ligadas: % abandonadas | 16,6 → 15,8 | 6,8 → 5,3 | — | 14,3 → 13,3 |
| metas independentes: % concluídas | 80,5 → 80,4 | 75,8 → 76,0 | 78,2 → 78,6 | 78,7 → 78,7 |
| metas de nível (chegar + desbloquear) por pessoa | 8,66 → 9,10 | 8,59 → 9,26 | 8,35 → 8,36 | 8,61 → 9,06 |
| metas de praticar por pessoa | 10,5 → 9,9 | 11,3 → 10,8 | 10,9 → 10,8 | 10,8 → 10,3 |
| nível em 90 dias | 24,3 → 24,5 | 24,2 → 24,5 | 22,9 → 23,1 | 24,1 → 24,3 |
| % do tempo socializando | 14,6 → 14,3 | 17,5 → 16,8 | 21,8 → 21,3 | 16,3 → 15,8 |
| % do tempo caçando | 30,1 → 30,2 | 27,9 → 28,4 | 24,8 → 25,1 | 28,8 → 29,1 |
| satisfação (0–1000) | 589 → 592 | 581 → 585 | 585 → 587 | 587 → 589 |

- **O efeito é pequeno e vai na direção esperada:** mais metas que servem às ambições e menos metas de praticar. O sinal é o mesmo nas 5 seeds: % ligadas de +2,3 a +2,8 pontos, nível de +0,06 a +0,28, satisfação de +1,8 a +5,1.
- **Indisponíveis:** nenhuma meta ligada, como deve ser. As pequenas diferenças vêm só da interação com os outros jogadores (lotação, a mesma seed divergindo).
- **Validação:** regras do cenário 8 de 9 e provisórias 8 de 12, iguais. A maior diferença foi o tempo na cidade (22,8% → 22,1%), dentro da faixa.
- **Objetivos concluídos em 90 dias** (influência ligada):

  | Objetivo | Concluíram | Dia mediano |
  |---|---|---|
  | Conhecer todo o mundo | 89% de quem tinha | 20 |
  | Alcançar o nível máximo | 38% | 55 |
  | Concluir a campanha principal | 35% | 56 |

  - **"Conhecer todo o mundo" é rápido neste mundo:** com 15 zonas, a maioria conhece quase todas em três semanas. O horizonte "longo" do catálogo não vale para o preset atual.
  - **Nível máximo em 55 dias** reflete a curva provisória de XP, a recalibrar na Etapa 2.

## 8. Evolução (O3)

Configuração em `objectives.json`: seção `evolution` (valores candidatos) e, em cada objetivo, `nature`, `opens` e `experienceTypes`.

### 8.1 Conquista definitiva × condição a manter

| Natureza | Objetivos | Ao cumprir o critério |
|---|---|---|
| **Conquista definitiva** | nível máximo, campanha principal, conhecer todo o mundo, grandes desafios PvE, equipamento raro | fica **concluída** para sempre e nunca volta como candidata |
| **Condição a manter** | mais fortes, PvP competitivo, grande fortuna, criar e liderar guilda, participar de guilda, melhor fabricante, maiores comerciantes | fica **mantida**. Se o critério deixa de valer, passa a **perdida**: a conquista passada continua registrada (data, vezes alcançada e perdida) e a pessoa pode querer recuperá-la, com reforço de recuperação |

- **Lugar na ficha:** concluídos e encerrados saem das vagas de ativos, liberam espaço e ficam no card "Conquistas e encerrados", com a data, o motivo e o resultado.
- **Hoje:** nenhuma condição pode ser cumprida, porque todas dependem de sistemas futuros. A perda é conferida por teste, com um conteúdo de teste.
- **Nível máximo × "mais forte":** o nível máximo é preparação para "Ter um personagem entre os mais fortes", mas não conclui essa ambição nem comprova força.

### 8.2 Renovação

- **Quando:** depois de um acontecimento, avaliado no dia jogado seguinte e respeitando o intervalo entre mudanças (7 dias jogados). Os acontecimentos são de dois tipos.
  - **Necessidade:** concluiu, abandonou ou perdeu um objetivo, ou chegou ao nível máximo.
    - A pessoa procura um objetivo novo entre todos os relevantes.
    - Se nenhum for relevante, **registra a falta**: histórico "sem objetivo relevante", evento `objective_gap` e contador.
    - A vaga não é preenchida. Na ficha, quem fica sem ambição ativa mostra "Sem ambição ativa: nenhum objetivo relevante no conteúdo".
  - **Oportunidade:** conheceu uma zona com chefe ou PvP.
    - Só pode vir o objetivo que esse conteúdo torna concreto: vencer chefes, se houver chefe, e PvP, se houver PvP.
    - Não achar nada não é falta de conteúdo.
- **Candidatas:**
  - **ficam de fora:**
    - os ativos e os mantidos;
    - as conquistas concluídas;
    - os abandonados há menos de 60 dias jogados;
    - os já cumpridos;
    - os sem parâmetro possível;
    - os sem interesse real;
  - **interesse real:** é o ajuste da O1 (ficha, ambição pela dedicação) vezes as experiências. O mínimo é 1,0; para objetivo de sistema futuro, 1,4.
- **Peso de cada candidata:**
  - **viabilidade:**
    - pode avançar: 1,0;
    - preparação conhecida: 0,8;
    - sistema futuro sem preparação: 0,3;
  - **relacionado** (`opens`): +80%, um só reforço.
    - Vale de um objetivo alcançado (concluído ou mantido).
    - De um objetivo ativo, só se ele for de sistema futuro e o novo tiver preparação concreta ou puder avançar.
    - Exemplo: o nível máximo, depois de alcançado, abre chefes, PvP, "mais forte", fortuna, fabricação, guilda e equipamento raro. Enquanto ativo, não abre nada.
  - **recuperar uma condição perdida:** +30%.
- **Experiências reais:**
  - as memórias marcantes boas aproximam e as ruins afastam, nos tipos de atividade do objetivo (por exemplo, caça para os chefes, exploração para o mundo, socializar para as guildas);
  - patrulha, emboscada e chefe não contam, porque ainda não têm combate real.
- **Vaga:**
  - **sem objetivo geral ativo:** o novo entra como geral;
  - **mais importante que o geral menos importante, com vaga de geral:** geral;
  - **senão:** secundário;
  - **vagas cheias:** o novo só entra se for mais importante, por uma margem, que o secundário menos importante. Esse secundário é **deixado de lado** (substituição voluntária, sem frustração) e pode voltar.
- **Sem sequência fixa:** o sorteio é ponderado pela ficha de cada um; nenhum objetivo se repete com números maiores, e gerais e secundários continuam independentes.
- **Atualizações de conteúdo** (M7) entram como novos acontecimentos quando existirem.

### 8.3 Prioridade e abandono

- **Promoção:** sem objetivo geral ativo, o secundário mais importante passa a geral, como consequência e sem intervalo.
- **Estagnação:** um objetivo que podia avançar e ficou 30 dias jogados sem progresso perde 150 de importância. Se ficar abaixo de um secundário, os dois trocam de lugar.
  - **Sistemas futuros nunca perdem prioridade por não avançar.**
- **Abandono (D-065):** abandonar metas **não** leva a desistir do objetivo.
  - A regra de "falhas seguidas" da primeira versão da O3 foi retirada. Ela contrariava o princípio do criador (não conseguir realizar agora, ou estar perto, não é motivo para desistir), e as "falhas" eram quase todas defeitos das metas de missão (diagnóstico em `baselines/metas-missao/`).
  - A desistência precisa de um motivo específico (D-066):
    - **interesse em queda sustentada:** o ajuste atual abaixo de 800, com queda desde a adoção de pelo menos 300 × (500 + importância)/1000, por 14 dias jogados;
    - **experiência negativa com ligação concreta** (`negativeLink`): pelo menos 3 memórias ruins marcantes distintas, por 7 dias jogados. Para a campanha, os lugares das etapas das missões principais; para o mundo, a exploração.
  - **Primeiro deixa de lado**, sem frustração. Sem melhora por 30 dias jogados, abandona; só a experiência negativa tem revés. Se o motivo passar, retoma.
  - No mundo atual, isso não acontece: ninguém mudou de personalidade em 90 dias e não há experiências ruins marcantes.
  - A sequência de metas ligadas abandonadas continua registrada como medida, sem efeito.
  - O estado "abandonado" e o intervalo de 60 dias para voltar ficam prontos para esse critério.
- **Influência desligada:** a evolução continua registrando, sem efeito no humor nem no plano. **Sorteio desligado:** não há objetivos nem evolução.

### 8.4 Medição (O3)

Rodadas pareadas com as mesmas pessoas e a evolução **desligada** × **ligada** (5 seeds × 1.000 × 90 dias). Detalhes em `baselines/objetivos/O3/analise.md`.

- **A desligada é idêntica à O2 ligada:** mesmo `validation.csv` e as mesmas colunas de `people.csv`.
- **Mudanças em 90 dias** (por 100 pessoas):

  | Mudança | Por 100 pessoas |
  |---|---|
  | objetivo novo | 78 |
  | concluído | 44 |
  | falta de objetivo relevante | 23 |
  | promovido a geral | 10,5 |
  | abandonado | 8,5 |
  | perdeu importância | 5,7 |
  | deixado de lado | 3,8 |

- **Motivos dos objetivos novos:**
  - oportunidade: 1.537 vezes, só chefes e PvP;
  - objetivo concluído: 1.508;
  - nível máximo: 636;
  - abandono: 228.
- **Mais adotados:** grandes desafios PvE, PvP, campanha principal, "mais fortes" e fabricação.
- **Faltas de objetivo relevante:**
  - 18,3% das pessoas tiveram ao menos uma;
  - **38% de quem chegou ao nível máximo**.
  - A falta de conteúdo de fim de jogo aparece, como pedido, sem progresso fictício.
- **No nível máximo no fim** (1.781 pessoas):
  - só 4,4% têm algum objetivo ativo que pode avançar ou tem preparação conhecida;
  - os ativos são quase todos ambições de sistemas futuros: chefes 990, PvP 782, "mais fortes" 754, fabricação 641 e fortuna 548;
  - sem nenhum objetivo ativo: 0,8% (5,9% com a evolução desligada).
- **Abandonos:** quase todos da campanha principal (409 de 423), consequência de defeitos nas metas de missão. Depois do D-065 não há abandono de objetivo por falha de meta (0).
- **Comportamento:**
  - objetivos ativos no fim: de 2,27 para 2,88 por pessoa;
  - metas ligadas: de 28,1% para 30,6%;
  - nível e horas: iguais;
  - satisfação: −1,5. A queda aparece em 4 das 5 seeds e vem sobretudo de quem abandonou um objetivo (−4,9, o custo do abandono); entre os demais, −1,2;
  - validação: regras do cenário 8 de 9 e provisórias 8 de 12, iguais.
