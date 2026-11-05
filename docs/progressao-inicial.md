# Progressão inicial, metas de desbloqueio e descoberta orientada (D-055)

As Preferências dizem do que a pessoa gosta. Este documento trata do que ela **precisa** fazer: as etapas que levam ao que ela quer, e que podem ser atividades de que gosta menos. Ela cumpre a etapa e depois volta ao que prefere. Nenhum destes mecanismos muda Preferências ou atributos, nem torna caça ou missões obrigatórias. Também não mexem nas regras da rotina.

## 1. Progressão inicial (`population.json`, seção `earlyProgression`)

Prioridade igual para **todos os personagens abaixo do nível-alvo**, independente de Perfil e Preferências. É uma prioridade inicial, não uma sequência obrigatória.

| Campo | Valor inicial | O que faz |
|---|---|---|
| `enabled` | sim | liga ou desliga o incentivo |
| `targetLevel` | 10 | nível em que o incentivo acaba (configuração do preset, não regra fixa) |
| `weight` | 150 | peso máximo na nota (o validador limita a 300, o peso de uma missão) |
| `fullUntilLevel` | 5 | peso pleno até este nível; depois cai em linha reta até zero no nível-alvo |
| `minFitPermille` | 600 | rendimento mínimo para uma atividade contar como progressão adequada |

**O fator "Progressão inicial" na nota de cada opção:**

`peso × redução pelo nível × contribuição`

- **Contribuição:** é o maior entre:
  - o **rendimento de XP** da atividade, quando ele é adequado (pelo menos `minFitPermille`; vale para caça, coleta e exploração no nível certo);
  - **1000 para opções de missão** (aceitar no quadro, fazer uma etapa, entregar).
  - Uma opção conta **uma vez**, mesmo que seja missão e dê XP.
- **Redução pelo nível** (com os valores iniciais): pleno até o nível 5; 80% no nível 6, 60% no 7, 40% no 8, 20% no 9; zero a partir do nível 10.
- **Com metas pessoais de progressão** (chegar a um nível, desbloqueio e concluir missão): o incentivo **não soma** com elas. Na mesma opção vale o maior dos dois: o fator mostra só o que passar da parte de progressão das metas.
- **Com o rendimento** ("adequada ao nível"): esse termo existe em qualquer nível e mede quanto a opção rende. A progressão inicial é a prioridade de render agora, enquanto o personagem se estabelece. Os dois aparecem separados no detalhe da decisão.
- **O que fica de fora:** opções que não cabem no tempo da sessão. Risco, distância, lotação, tédio e o limite da sessão continuam pesando normalmente. Com o peso inicial (até 150), um risco alto (até −350) ou a falta de tempo (−400) continuam vencendo.
- **O que não recebe:** comércio, taverna, treino e conserto (não rendem progressão), missões já concluídas (não são oferecidas de novo) e qualquer opção a partir do nível-alvo.
- **As preferências escolhem o caminho:** o gosto continua na nota, então quem prefere coletar tende a progredir coletando, e quem prefere explorar, explorando.
- **Depois:** melhorar equipamento e vender excedentes podem entrar quando inventário e economia existirem, pelas necessidades concretas. Hoje esses benefícios não são simulados.

## 2. Metas de desbloqueio (seção `goals`)

Liga uma atividade desejada ao requisito que a libera. Exemplo: "nível 6 para coletar minério na Mina Velha", motivada pela preferência Coleta.

- **Quando aparece:** para **cada preferência relevante** (a partir de `unlockMinPreference` = 450, não só a mais forte), procura-se a atividade daquele tema conhecida pela pessoa (zona conhecida) com o menor nível mínimo acima do atual, até `unlockMaxLevelsAhead` = 6 níveis à frente.
  - Ficam de fora atividades com requisito que ainda não existe na simulação (grupo, item, classe).
- **Temas:** PvE → caça; PvP → patrulha e emboscada; Coleta → coleta; Exploração → exploração. Comércio e Social não têm degraus de nível no preset, e Crafting ainda não tem atividade.
- **Escolha:** cada opção vira candidata, com peso `80 + preferência × 0,6`, ao lado das outras metas; a escolha entre elas é ponderada. A pessoa tem no máximo uma meta de desbloqueio por vez.
- **Na decisão:** a meta puxa qualquer fonte de XP com rendimento adequado e as entregas de missão, com a mesma regra da meta "chegar a um nível". A pessoa escolhe a fonte pelo gosto e pelo rendimento (coleta de nível baixo, exploração, missões ou caça). **A preferência não muda.**
- **Fim:** a meta termina ao chegar ao nível. Cumprida, ela reforça a especialidade do tema; Coleta não tem especialidade, então não reforça nada.

## 3. Descoberta orientada pelo conteúdo

A meta "descobrir uma zona" pode mirar uma zona desconhecida **com conteúdo de que a pessoa gosta**, desde que ela tenha **informação válida** sobre esse conteúdo.

- **Informação válida hoje:** as etapas das missões aceitas. Ao ler a missão, a pessoa fica sabendo o que há nos lugares das etapas (`ContentHints` do personagem, "zona/atividade").
  - **Sem pista,** ela não sabe o conteúdo de uma zona desconhecida.
  - **Outras fontes,** como conversas na taverna, outros jogadores, guildas ou guias, entram com os sistemas correspondentes e vão alimentar as mesmas pistas.
- **Alvos válidos:** zona desconhecida da fronteira (vizinha de uma conhecida), dentro do nível aceito (como na descoberta comum), com uma pista de atividade de um tema que a pessoa prefere (a partir de 450).
- **Peso:** `100 + preferência × 0,9`. A descoberta comum, por gosto de explorar, continua existindo.

## 4. Fora do escopo agora

- `requiredQuestIds` (desbloqueio por missão): só quando houver uma necessidade concreta.
- **Planejador de várias etapas** (item → material → combate): depois.
- **Crafting, materiais por combate, itens exigidos, chefes em grupo:** com fabricação, inventário e economia (M5) e grupos (M6).

## 5. Medição

Validação padrão (5 seeds × 1.000 pessoas × 90 dias), com as mesmas pessoas em quatro versões para separar os efeitos. Detalhes em `baselines/progressao-inicial/analise.md`.

| Versão | Progressão inicial | Desbloqueio e descoberta |
|---|---|---|
| V0 | não | não |
| V1 | sim | não |
| V2 | não | sim |
| V3 (preset) | sim | sim |

- **V0 reproduz o D-054 exatamente:** mesmo arquivo de metas e as mesmas horas e níveis nas 5.000 pessoas. O código novo não muda nada com os mecanismos desligados.
- **Rotina preservada:** dias, sessões e horas por perfil iguais nas quatro versões (regras do cenário 7 de 9 em todas). Casual: 8,9 a 9,0 h (mediana); hardcore: 35,9 a 36,1 h.

**Tempo de jogo até o nível 10** (horas conectadas; mediana · p90):

| Grupo | V0 | V1 (inicial) | V2 (metas) | V3 (as duas) |
|---|---|---|---|---|
| Todos | 15,8 · 31,7 | 15,2 · 23,7 | 15,7 · 29,4 | 15,2 · 22,6 |
| Prefere PvE | 13,1 · 15,9 | 12,9 · 15,4 | 13,1 · 15,7 | 13,0 · 15,7 |
| Prefere Coleta | 17,4 · 22,5 | 16,9 · 21,1 | 17,2 · 21,9 | 17,0 · 21,0 |
| Prefere Comércio | 20,6 · 43,3 | 17,2 · 31,4 | 19,3 · 45,4 | 16,6 · 30,2 |
| Prefere Social | 23,7 · 64,1 | 19,2 · 48,5 | 22,2 · 58,6 | 18,3 · 43,3 |

- **Onde o incentivo atua:** quem já progredia rápido (prefere PvE) quase não muda. O incentivo encurta a cauda de quem prefere atividades sem XP (comércio, social). Por perfil, o p90 cai de 27 a 34 h para 22 a 23 h.

**Atividades antes e depois do nível 10** (parcela do tempo em atividades; antes · depois):

| Grupo | V0 | V1 | V3 |
|---|---|---|---|
| Caça e chefe | 32,5% · 35,6% | 37,3% · 35,2% | 38,5% · 36,4% |
| Missões | 6,7% · 1,9% | 7,7% · 1,9% | 8,0% · 1,8% |
| Exploração | 9,5% · 10,9% | 11,5% · 10,8% | 11,8% · 11,1% |
| Coleta | 14,1% · 14,3% | 16,3% · 14,2% | 16,9% · 14,9% |
| Cidade | 37,1% · 32,7% | 27,2% · 33,3% | 24,8% · 30,7% |

- **Antes do nível 10:** a cidade perde tempo para caça, missões, exploração e coleta.
- **Depois do nível 10,** com o incentivo encerrado, a mistura volta ao que era. A pequena diferença em V3 vem das metas de desbloqueio, que seguem depois do nível 10.
- **Preferências continuam guiando:** quem prefere Social passa de 17% para 25% de caça antes do nível 10, mas continua na taverna a maior parte do tempo. Quem prefere Coleta progride coletando: 59% do tempo em coleta antes do nível 10 (61% em V0) e 70% depois.

**Personagens abaixo do nível 10 com 20 h ou mais em atividades** (90 dias):

| Versão | Quantos | Abaixo do nível 5 | Preferência predominante |
|---|---|---|---|
| V0 | 82 de 4.947 (1,7%) | 3 | Social 67 |
| V1 | 44 (0,9%) | 0 | Social 35 |
| V2 | 57 (1,2%) | 2 | Social 43 |
| V3 | 35 (0,7%) | 0 | Social 29, Comércio 3, Crafting 3 |

- **Eles têm caminho de progressão:** todos têm caça, coleta, exploração e missões disponíveis. Ficam abaixo do nível 10 porque escolhem a taverna, que não dá XP. A progressão não é obrigatória.

**Metas** (V3):
- **Desbloqueio:** cerca de 25 mil metas adotadas e 16 mil cumpridas (64%), em torno de cinco por pessoa em 90 dias. O nível mediano de quem prefere Comércio sobe de 20 para 22; o de quem prefere Social, de 16 para 18.
- **Descoberta orientada por outra preferência:** só 6 metas. Com a regra de informação válida, quase não há pista utilizável. Causa provável, não medida à parte: as etapas das missões aceitas apontam sobretudo para zonas que a pessoa já conhece ou que estão fora da fronteira. O mecanismo funciona (testes), mas depende de novas fontes de informação (conversas, outros jogadores, guildas) para ter efeito.
- **Missões:** a parcela de XP vinda delas cai de 19,5% para 17,7%, porque as metas de desbloqueio passam a disputar as vagas de meta com as metas de missão.

**Outras medidas** (V0 → V3):
- tempo em combate de 31,3% para 32,9%;
- cidade de 26,8% para 24,4%;
- casuais no nível 10 ou mais ao fim de 95,3% para 97,5% (a meta provisória de 60 a 95% já estava fora);
- horas até o nível 10 de 15,8 para 15,2;
- horas até o nível 30 de 195 para 197.
