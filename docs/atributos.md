# Atributos do jogador (D-036, revisão D-036a)

Os atributos são da **pessoa** (o jogador), não do personagem. O personagem tem classe, nível, itens e status. No combate (M3), os dois entram juntos: dois personagens do mesmo nível e equipamento se saem diferente conforme os atributos de quem joga.

Cada atributo representa **uma coisa só**:

- **O gosto não é atributo:** fica nas **Preferências** (PvE, PvP, Crafting, Comércio, Coleta, Exploração, Social), um campo da pessoa ao lado do Perfil. Elas dão o peso principal de gosto nas escolhas, sem obrigar (`perfil-preferencias.md`).
- **Especialidade** (PvE, PvP, Comércio, Exploração, Socializar) é a afinidade desenvolvida e a familiaridade. Dá só um reforço mínimo e limitado no gosto (5%, com teto) e cresce com a prática. **Benefícios de execução ainda não existem**; serão propostos com o combate, a economia e os grupos.
- **Capacidades** (Mecânica, Conhecimento, Coordenação) decidem a **execução**. Evoluem com prática, talento e potencial.
- **Personalidade** (Sociabilidade, Agressividade, Paciência, Lealdade, Aversão a risco, Variedade) e **conduta** (Competitividade, Trapaça, Oportunismo) mudam pouco ou nada.
  - A **Agressividade** é confronto (emboscada, tanque, PvP).
  - A **Competitividade** é disputar, superar e ser reconhecido. Deve acompanhar os interesses da pessoa: um comerciante competitivo não precisa buscar nível alto. Hoje os efeitos são parciais e só na progressão (desafio acima do nível, metas de nível, expectativa de progresso); os demais entram com os sistemas correspondentes.

Todos os números ficam em `population.json`, nas seções `dynamics` (evolução) e `goals` (metas). O jogador, como desenvolvedor do MMORPG, poderá editá-los, e o validador confere as faixas.

## Unidades

- **Core:** 0 a 1000 (permille), em inteiros.
- **Interface:** escala 1–20. **1 ponto visível ≈ 52,6 unidades do Core** (1000 / 19).
- **Prática:** "minutos por unidade" = minutos da atividade para +1 unidade, antes do talento e da desaceleração perto do potencial.

Mudanças visíveis (quando o valor 1–20 muda) geram o evento `trait_changed` com a causa.

## Talento × experiência

São coisas diferentes:

- **Talento** é o traço fixo da pessoa. Diz quanto e quão rápido ela pode aprender. Vem da distribuição da população (`population.json`, `traits.talent`), igual para todos os perfis.
- **Experiência** é a prática neste jogo (horas jogadas). Ela aparece na ficha e não é um traço. O **Conhecimento** cresce com ela, e quem já jogava o gênero chega com mais Conhecimento.

| Talento | Valor | | Experiência | Horas jogadas |
|---|---|---|---|---|
| Baixo | < 300 | | Novato | < 10 h |
| Moderado | 300–599 | | Iniciante | 10–49 h |
| Alto | 600–799 | | Experiente | 50–199 h |
| Excepcional | ≥ 800 | | Veterano | ≥ 200 h |

O talento vale só para as capacidades:

- **Potencial absoluto (escolha consciente):** `max(início + 50, 250 + talento × 0,7)`.
  - O potencial **não depende** de onde a pessoa começa: quem começa baixo, mas tem talento, pode chegar alto.
  - O início só garante uma folga mínima de 50 unidades.
  - Consequência: quem começa com capacidades altas fica mais perto do próprio potencial e evolui pouco; quem começa baixo, com talento, tem mais espaço para crescer. Desde o D-054, as capacidades iniciais e o talento vêm da população toda, sem depender do perfil: a diferença entre dedicações aparece pela prática (quem joga mais aprende mais).
  - Talento 0 → 250 (≈ 6/20); 500 → 600 (≈ 12/20); 800 → 810 (≈ 16/20); 1000 → 950 (≈ 19/20).
- **Velocidade:** o custo da prática vai de 1,4× (talento 0) a 0,6× (talento 1000).
- **Desaceleração:** perto do potencial a prática fica até 3× mais cara.

Gostos e personalidade não dependem do talento: mudam pela experiência, dentro de uma faixa ao redor do valor de nascença.

## Limites

- **Piso:** início − faixa. A faixa é 250 nas capacidades, 200 nos gostos e 100 na personalidade.
- **Teto:**
  - capacidades: o potencial acima;
  - gostos e personalidade: início + faixa.

## Ritmo esperado (com talento moderado, longe do potencial)

| Atributo | O que faz subir | Horas por ponto visível |
|---|---|---|
| Mecânica | Tempo em combate (150 min/unidade) | ≈ 130 h de combate (100 h com talento 800; 185 h com talento 0) |
| Conhecimento | Jogar (400 min/unidade) | ≈ 350 h de jogo |
| | Explorar (90 min/unidade) | ≈ 80 h explorando |
| | Zona descoberta (+2), missão concluída (+1), primeira vez num tipo de atividade (+3) | — |
| Gosto (PvE, PvP, Comércio, Exploração, Socializar) | Experiência boa e persistente: encaixe de nível ≥ 600 (360 min/unidade) | ≈ 315 h de experiências boas |
| Aversão a risco (desce) | Enfrentar risco ≥ 300 sem perder (600 min/unidade) | ≈ 525 h |
| Sociabilidade | Tempo na taverna (600 min/unidade) | ≈ 525 h |

**Repetição não reduz o gosto.** Repetir gera tédio na decisão (fator "cansaço de repetir", menor para os pacientes). Só experiências persistentes mudam uma preferência. Experiências ruins persistentes (mortes, perdas, fracassos) entram com o combate (M3).

## Inatividade

- Mecânica: −1 unidade por dia depois de 7 dias sem jogar.
- Conhecimento: −1 unidade por dia depois de 14 dias sem jogar.
- Com as atualizações (M7), o Conhecimento passará a cair por atualização perdida.

| Dias parado | Mecânica | Conhecimento |
|---:|---:|---:|
| 7 | 0 | 0 |
| 14 | −0,13 | 0 |
| 30 | −0,44 | −0,30 |
| 60 | −1,01 | −0,87 |
| 90 | −1,58 | −1,44 |

(Pontos da escala 1–20, sem chegar ao piso.)

## Evolução medida em 90 dias

O relatório de validação (`reports/<pasta>/report.md`, seção "Evolução dos atributos do jogador") mostra a variação média por perfil em pontos da escala, junto com as metas concluídas e abandonadas.

Medido no D-036a (90 dias × 1.000 pessoas × 5 seeds), em pontos da escala 1–20:

| Perfil | Horas/semana | Mecânica | Conhecimento | PvE | Exploração | Comércio | Variedade (D-039a) | **Preferência por variedade:** tédio mais rápido ao repetir; menos atração pela zona familiar; maior necessidade de novidade. Separada da Paciência (tolera repetir) e da Exploração (gosta de explorar) | — (personalidade) | — |
| Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | 4,2 | +0,05 | +1,10 | +0,11 | +0,11 | 0,00 | 0,00 | 10,5 | 1,0 |
| Comerciante | 8,5 | +0,05 | +1,01 | +0,09 | +0,06 | +0,12 | 0,00 | 14,6 | 2,7 |
| Competitivo | 14,2 | +0,60 | +1,18 | +0,23 | +0,08 | +0,01 | −0,15 | 13,8 | 10,5 |
| Explorador | 9,4 | +0,05 | +1,17 | +0,06 | +0,33 | 0,00 | −0,01 | 12,8 | 4,1 |
| Hardcore | 25,0 | +0,87 | +0,97 | +0,57 | +0,05 | 0,00 | −0,08 | 21,3 | 5,0 |
| Social | 9,0 | +0,05 | +1,14 | +0,07 | +0,05 | 0,00 | 0,00 | 14,8 | 3,1 |

Esta tabela é da estrutura anterior (sete perfis). Com a estrutura do D-054 (5 seeds × 1.000 × 90 dias), em pontos da escala:

| Perfil | Mecânica | Conhecimento | PvE (especialidade) | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Casual | +0,17 | +1,10 | +0,18 | +0,11 | +0,01 | −0,02 | 14,9 | 5,4 |
| Regular | +0,37 | +1,36 | +0,26 | +0,14 | +0,02 | −0,04 | 20,6 | 8,7 |
| Hardcore | +0,66 | +1,69 | +0,38 | +0,16 | +0,03 | −0,08 | 30,1 | 11,9 |
| Devoto | +0,82 | +2,20 | +0,41 | +0,17 | +0,04 | −0,12 | 38,4 | 9,9 |

Como ler a tabela anterior (seed 1):

- **Mecânica:**
  - comerciante, explorador e social passam só 8 a 9 h em combate em 90 dias, então ganham 2 a 3 unidades;
  - casuais têm 15 h de combate, mas 1 em cada 5 perde Mecânica em pausas de mais de 7 dias;
  - competitivos (112 h de combate) e hardcore (189 h) ganham menos do que a conta simples porque começam perto do potencial e a desaceleração pesa.
- **Conhecimento** sobe cerca de 1 ponto em todos os perfis, quase todo nas primeiras semanas (zonas, missões e atividades novas): é o "aprender o jogo".
- **Gostos** mudam pouco (até +0,6 de PvE no hardcore): só experiências boas e persistentes os movem.

## O que cada atributo faz hoje e como muda

| Atributo | Influencia (simulação) | Sobe com | Cai com |
|---|---|---|---|
| PvE (especialidade) | Reforço mínimo no gosto por caça e chefe (5%, teto 50); o gosto vem da preferência PvE | Experiências boas de caça e chefe; metas cumpridas motivadas pela preferência PvE | — (repetir só gera tédio) |
| PvP (especialidade) | Reforço mínimo no gosto por patrulha e emboscada. **Não melhora a execução** | Experiências boas de PvP; abate de jogador (+3, M3) | — |
| Comércio (especialidade) | Reforço mínimo no gosto por comerciar. A coleta não usa este atributo | Experiências boas de comércio | — |
| Exploração (especialidade) | Reforço mínimo no gosto por explorar | Experiências boas explorando; 1 unidade a cada 4 zonas descobertas | — |
| Socializar (especialidade) | Reforço mínimo no gosto pela taverna | Experiências boas na taverna | — |
| Sociabilidade | Valor da companhia na taverna; compromissos sociais; classe de suporte; puxa um pouco a preferência Social | Tempo na taverna | — |
| Agressividade | Confronto: emboscada; classe tanque; puxa um pouco a preferência PvP (PK no futuro). Não é competitividade | — (personalidade) | — |
| Paciência | Tolera repetição (alivia o tédio); **persistência** nas metas (demora mais para desistir) | — (personalidade) | — |
| Lealdade | Apego às metas (comprometimento); patrulha; classe tanque. Grupos e guildas no M6 | — (personalidade) | — |
| Aversão a risco | Pesa contra risco e PvP aberto; reduz o gosto por desafio (30%) | — | Enfrentar risco sem perder; mortes (M3) a farão subir |
| Mecânica | **Execução**: XP em combate (×0,88–1,12), menos medo do risco, confiança para desafio (30%); no M3, execução em PvE e PvP | Combate; abate de jogador (prática, M3) | Mais de 7 dias sem jogar |
| Conhecimento | XP geral (×0,95–1,05); **estima melhor o tempo de viagem** (erro de até ±30% com conhecimento zero; o tempo real é igual para todos); decisões mais consistentes (menos ruído) | Jogar, explorar, descobrir, concluir missões, primeira vez num tipo de atividade | Mais de 14 dias sem jogar |
| Coordenação | Lugares cheios pesam menos na caça e na coleta; classe tanque; execução em grupo (M6) | **Só atividades cooperativas** (grupos e raides, M6); estável por enquanto | — |
| Trapaça | Sem efeito até os incidentes (M9) | — | — |
| Oportunismo | Foge mais de lugares disputados; emboscada; incidentes no M9 | — | — |
| Competitividade | Disputar, superar e ser reconhecido: gosto por desafio acima do nível (40%); metas de nível e zonas mais altas aceitas; expectativa de progresso; quanto liga para evoluir. Não decide agressividade, PK, habilidade nem horas. Rankings e duelos no futuro | — (conduta) | — |

### Fatores de decisão ligados aos atributos

- **Zona familiar** (D-036a): a zona onde a pessoa já passou tempo atrai.
  - Cresce com os minutos na zona até 600 min (peso máximo 80).
  - É menor para quem prefere variedade: `× (1 − Variedade / 2)` (D-039a).
  - A lealdade não entra aqui.
- **Gosto por desafio** (D-036a): conteúdo acima do nível (até 3 níveis, 40 por nível).
  - Apetite = Competitividade × 0,4 + Mecânica × 0,3 + (1000 − Aversão a risco) × 0,3.
- **Distância:** o custo é o tempo real de viagem em relação ao tempo de sessão. O Conhecimento só muda a **precisão da estimativa**.

## Metas de curto prazo

São as metas de sessão ou secundárias (chegar a um nível, descobrir uma zona, concluir uma missão, praticar uma atividade). **Ambições de longo prazo** são os objetivos pessoais (`objetivos.md`, D-062), separados das metas de curto prazo.

- **Adoção:** ao conectar, se houver vaga.
  - Até 2 metas para todos, chance fixa de 45%.
  - Paciência não muda a quantidade nem a chance.
- **Escolha:** ponderada pelas preferências, pela Competitividade e pela personalidade (nenhuma depende do perfil):

| Meta | Peso | Alvo |
|---|---|---|
| Chegar a um nível | 150 + preferência PvE·0,3 + Competitividade·0,2 + (1000 − Paciência)·0,1 | 1 a 3 níveis acima |
| Descobrir uma zona | 100 + preferência Exploração·0,9 | Uma zona vizinha das conhecidas, dentro do nível (os competitivos aceitam zonas mais altas) |
| Concluir uma missão | 120 + (preferências PvE + Exploração)·0,2 + Paciência·0,2 + Conhecimento·0,1 | A principal ativa ou que pode ser aceita agora (com vaga); senão, uma secundária (D-065) |
| Praticar a atividade preferida | 100 + preferência do tipo·0,5 + Paciência·0,2 | 120 a 420 minutos |

- **Influência:** fator "meta pessoal" na decisão.
  - Até 200 × comprometimento (400 + Paciência·0,3 + Lealdade·0,3, em permille).
  - A soma das metas numa opção tem teto de 1,5× o peso.
  - O ruído, o gosto, o risco e a distância ainda podem vencer, então não é regra.
- **Persistência e abandono:**
  - depois de 3 a 12 horas de jogo sem progresso, conforme a paciência;
  - ou, ao conectar, com 4% de chance, se a paciência for < 300;
  - a meta largada não volta com o mesmo alvo na mesma revisão (D-065; as de nível têm bloqueio até subir de nível, D-055).
- **O que conta como progresso numa meta de missão** (D-065): aceitar a missão, minutos parciais da etapa, concluir uma etapa e a entrega. Uma missão pronta só para entregar não recebe progresso até a entrega.
- **Conclusão:** reforça o traço que motivou a meta (+1 unidade).
- **Registro:** tudo gera evento (`goal_set`, `goal_completed`, `goal_abandoned`) com o motivo, e o estado entra no hash.
