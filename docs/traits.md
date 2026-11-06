# Traits: os traços do jogador (P4b)

Trait é uma **característica marcante e nomeada da pessoa**: ela tem ou não tem, não é uma escala. Cada trait traz uma regra específica, que age numa situação, com efeito e contrapartida claros.

**Regra de desenho:** uma trait nunca é "+X num atributo com outro nome". Ela só age numa condição e tem custo.

As traits ficam separadas das outras camadas da pessoa:

| Camada | O que é | Exemplo |
|---|---|---|
| Perfil | quanto se dedica | Hardcore |
| Preferências | do que gosta | Coleta |
| Especialidades | familiaridade desenvolvida | Comércio 12/20 |
| Atributos | intensidade contínua, sempre ativa | Paciência 14/20 |
| Personalidade principal (`personalidade.md`) | a identidade central, uma por jogador, com regra própria | Cauteloso |
| **Traits** | os traços, de 0 a 6, com regras específicas | **Pavio curto** |

**Etapas:**
- **P4b:** catálogo, sorteio, estado da pessoa e ficha.
- **P4c:** efeitos das 15 traits da pessoa (seção 6). As traits de sistemas futuros continuam sem efeito até esses sistemas existirem.
- **P4d:** aquisição e perda.

No código o nome é `Quirk`, para não confundir com os atributos numéricos (`PersonTraits`), que vieram antes. O arquivo de conteúdo é `traits.json`.

## 1. Conteúdo (`traits.json`, validado)

Cada trait declara:
- **classificação** (`polarity`): `positive` ou `negative`;
- **origem** (`acquisition`): `innate` (nasce com a pessoa), `acquired` (só por evidência, com o sistema dela) ou `both`;
- **`drawn`**: se entra no sorteio da criação;
- **`system`**: de que sistema o efeito depende. Vazio = efeitos da própria pessoa (decisão, humor, memória, rotina); `M3`, `M5`, `M6`, `etapa3` ou `fabricacao`;
- **`weight`**: peso base, e **`affinities`**: a coerência com a pessoa;
- **`excludes`** (traits contraditórias, nos dois sentidos) e **`excludesPersonalities`** (contradição ou redundância com a personalidade principal);
- **`effect`** e **`tradeoff`**: textos da ficha.

## 2. Sorteio na criação

1. **Quantidade:** pela tabela `countWeights` (0 → 8%, 1 → 25%, 2 → 50%, 3 → 10%, 4 → 4%, 5 → 2%, 6 → 1%).
2. **Positiva ou negativa:** um sorteio por vaga, com 40% de chance de negativa (`negativeChancePermille`). Não é uma proporção fixa por pessoa: alguém pode ter só positivas ou só negativas.
3. **Escolha:** dentro da classificação, peso base × multiplicador de coerência.
   - **Afinidades:**
     - **atributos** de personalidade, Conduta, autocontrole e tendência a prolongar;
     - **preferências;**
     - **perfil** e **personalidade principal**, quando pertinentes.
   - **Termo de um atributo ou preferência:** de 1000 − força (no extremo oposto) a 1000 + força (no extremo da direção). A posição vem da distribuição do conteúdo, a mesma régua da personalidade. Nos atributos em média e dispersão, usa o desvio da média dividido pela dispersão, uma aproximação.
   - **Termo de perfil ou personalidade:** 1000 + força, se a pessoa tiver aquele perfil ou personalidade.
   - **Multiplicador:** o produto dos termos, limitado entre 0,25 e 3 (`minMultiplierPermille`, `maxMultiplierPermille`). Nada fica impossível nem obrigatório por afinidade; só as exclusões proíbem.
4. **Exclusões:** valem sobre o conjunto inteiro e com a personalidade. Sem candidata válida, a vaga fica vazia, sem forçar.
5. **Fluxo próprio** (`population.quirks`), sorteado por último: nenhum outro atributo de ninguém muda.
   - No cenário controlado do runner, as pessoas ficam sem traits, para continuarem iguais por célula.
6. **Limite total:** 8 traits, contando as adquiridas depois (`maxTotal`, para a P4d). Talento, inteligência e a "marcância" não entram.

## 3. Catálogo

**Efeitos da pessoa** (entram na P4c):

| Trait | ± | Efeito | Contrapartida | Mais provável com | Exclusões |
|---|---|---|---|---|---|
| Persistente | + | depois de um revés, mantém o plano e demora a abandonar metas | insiste além do que compensa | paciência, lealdade | Impulsivo; Inquieto e Leal |
| Impulsivo | − | troca de plano com margem menor, revisa mais, mais ruído | aproveita o que aparece | paciência baixa, variedade, autocontrole baixo | Persistente, Mercador cauteloso; Paciente |
| Colecionador | + | valor extra para atividade ou lugar nunca feitos | troca rendimento por novidade | variedade, preferência Exploração | Otimizador; Metódico |
| Otimizador | + | rendimento pesa mais, gosto menos | diversão conta menos; tédio mais rápido | competitividade | Colecionador |
| Resiliente | + | a frustração passa mais rápido | as conquistas marcam menos | paciência | Pavio curto |
| Pavio curto | − | a frustração sobe mais rápido; larga a sessão com raiva | volta mais cedo depois | agressividade, paciência baixa, Agressivo | Resiliente |
| Saudosista | + | boas lembranças duram mais; volta mais | novidade fraca frustra mais | lealdade | — |
| Rancoroso | − | lugares ruins ficam marcados por mais tempo | repete menos o que deu errado | lealdade e paciência baixas | — |
| Estudioso | + | missões e descobertas rendem mais Conhecimento | demora nas etapas de visita | paciência, preferência Exploração | — |
| Esquecido | − | as capacidades caem mais cedo sem jogar | reaprende rápido | — | Memória firme |
| Memória firme | + | as capacidades caem pela metade sem jogar | custa a dar segunda chance a um lugar | — | Esquecido |
| Observador | + | ao explorar, pistas do conteúdo das zonas vizinhas | explora mais devagar | preferência Exploração | — |
| Indeciso | − | em empate apertado, perde minutos decidindo | decide com menos ruído | variedade | — |
| Maratonista | − | com a sessão boa, a saciedade cresce mais devagar | depois de uma sessão com mais do dobro da normal, o dia seguinte começa com menos disposição | Hardcore, Devoto, tendência a prolongar (menos no Casual) | — |
| Procrastinador | − | adia o começo da sessão dentro da janela livre; se não sobrar a sessão mínima, não começa e reconsidera numa próxima janela válida | perde tempo de jogo | autocontrole baixo | — |

**Dependem de sistemas futuros** (sorteadas desde já como predisposição; "sem efeito até…" na ficha):

| Trait | ± | Sistema | Efeito | Mais provável com | Exclusões |
|---|---|---|---|---|---|
| Protetor de novatos | + | Etapa 3 | ajuda iniciantes e enfrenta quem os ataca | lealdade, agressividade baixa | Matador de novatos |
| Competidor honrado | + | Etapa 3 | prefere confrontos equilibrados | competitividade, oportunismo baixo | Matador de novatos, Provocador |
| Provocador | − | Etapa 3 | provoca e puxa confrontos | agressividade, Agressivo | Competidor honrado; Pacífico |
| Líder nato | + | M6 | iniciativa para formar grupos | sociabilidade, competitividade | Lobo solitário; Reservado |
| Companheiro fiel | + | M6 | prioriza compromissos com conhecidos | lealdade, preferência Social | Lobo solitário |
| Lobo solitário | − | M6 | joga sozinho e recusa grupos | sociabilidade baixa, Reservado | Líder nato, Companheiro fiel; Sociável |
| Mercador cauteloso | + | M5 | mantém reservas e avalia risco | aversão a risco, preferência Comércio | Impulsivo, Gastador compulsivo |
| Caçador de barganhas | + | M5 | atenção a diferenças de preço | preferência Comércio | Gastador compulsivo |
| Gastador compulsivo | − | M5 | gasta ouro por impulso | autocontrole baixo | Mercador cauteloso, Caçador de barganhas, Acumulador |
| Acumulador | − | M5 | guarda consumíveis e não usa | aversão a risco | Gastador compulsivo |

**Só no catálogo** (sem sorteio até o desenho do sistema): Frio sob pressão (+, M3), Nervoso em PvP (−, Etapa 3), Artesão meticuloso (+, fabricação).

**Só adquiridas** (P4d, com os sistemas): Traumatizado (−, M3), Matador de novatos (−, Etapa 3; nunca em Pacífico).

## 4. Ficha

- **Card "Personalidade e traços"** na visão geral, no lugar do Inventário. O inventário passou para a aba Inventário; a aba "Personagem" vem com a Etapa 1.
- **Personalidade:** emblema redondo dourado, nome, regra e o atributo que a define, com a posição ("Aversão a risco 18/20: no 1% mais alto"). Mostra também a última mudança, se houver.
- **Traços:** positivos primeiro, com emblema, sinal, efeito e situação ("Efeito a partir da P4c" ou "Sem efeito até o M5").
  - O tooltip traz efeito, contrapartida, situação, afinidades e exclusões.
  - Sem traços: diz isso e a parcela de jogadores assim, calculada pela tabela do conteúdo.
- **Ícones padronizados** (Lucide, o mesmo traço de 1,6 px), todos diferentes:
  - **personalidade:** círculo dourado;
  - **trait:** quadrado arredondado, verde (positiva) ou vermelho (negativa);
  - **efeito ainda desligado:** borda e ícone esmaecidos.
  - O mapeamento fica em `GodotClient/Scripts/Ui/TraitInfo.cs`. Uma trait nova no conteúdo, sem ícone próprio, recebe um ícone genérico pela classificação.
- **Destaques do card de atributos:** agora mostram só gostos e competitividade. Os rótulos antigos com nomes de personalidade ("Cauteloso", "Paciente"…), por outro critério, saíram para não confundir.

## 5. Medição

Validação padrão (5 seeds × 1.000 × 90 dias), P3 (traits ligadas) contra P2 (o preset antes delas). Resultados em `baselines/traits/`.

- **Simulação idêntica:** `validation.csv` igual, e nenhuma coluna comum do `people.csv` mudou nas 5.000 pessoas.
- **Quantidade:**

  | Traits | Tabela | Medido |
  |---|---|---|
  | 0 | 8% | 7,8% |
  | 1 | 25% | 24,0% |
  | 2 | 50% | 50,6% |
  | 3 | 10% | 10,5% |
  | 4 | 4% | 3,9% |
  | 5 | 2% | 2,2% |
  | 6 | 1% | 1,2% |

- **Negativas:** 39,5% das traits sorteadas. Entre quem tem duas: 36,9% só positivas, 47,1% uma de cada e 16,0% só negativas (esperado 36, 48 e 16%).
- **Frequência:** cada trait sorteável fica entre 5,8% (Gastador compulsivo) e 9,5% (Estudioso) das pessoas.
- **Coerência:**
  - Pavio curto: 16,6% dos Agressivos e 4,7% dos Pacíficos;
  - Maratonista: Devoto 17,5%, Hardcore 11,9%, Regular 6,6%, Casual 4,3%;
  - Observador: 11,9% com preferência Exploração alta e 4,7% com baixa;
  - Otimizador: 10,7% com competitividade alta e 5,4% com baixa.
- **Exclusões:** nenhuma combinação excluída (Provocador em Pacífico, Lobo solitário em Sociável, Persistente em Leal ou Inquieto: 0%).
- **Testes** (`QuirkTests`): quantidade e classificação com 10 mil pessoas, exclusões, coerência, fluxo próprio, determinismo, limites do multiplicador e validador.

## 6. Efeitos (P4c)

As magnitudes ficam em `traits.json`, seção `effects`, e são candidatas.
- **Desligar:** `effects.enabled` = false desliga todos os efeitos; as traits continuam existindo.
- **Nomes:** "permille" é um multiplicador, em que 1000 é igual a sem efeito.
- **Causa visível:** cada efeito aparece como fator próprio na decisão ("Traço: Nome") ou como causa no evento.
- **Contagem:** cada atuação é contada na pessoa (`QuirkStats`), para medição.
- **Limites preservados:** nenhum efeito cria tempo nem passa por cima de sono, obrigação, deslocamento ou janela.
  - O Procrastinador só adia dentro das janelas livres.
  - O Maratonista só muda a saciedade, e a decisão de continuar continua presa à janela e ao corte de sono autorizado.

| Trait | Ponto de avaliação | Efeito (padrão) | Contrapartida (padrão) | Causa visível |
|---|---|---|---|---|
| Persistente | plano da sessão; revisão das metas | até 2 h depois de um revés, margem de troca do plano × 1,5; prazo para abandonar meta sem progresso × 1,5 e chance de desistir por impaciência ÷ 1,5 | insiste além do que compensa | `quirk_persistente` no evento da escolha |
| Impulsivo | plano; ruído; fim de atividade | margem de troca × 0,6; ruído × 1,3; 25% de chance de revisar o plano a cada fim de atividade | — | `quirk_impulsivo` na escolha e no motivo da revisão |
| Colecionador | nota das opções | +150 para atividade nunca feita; +75 para zona nunca visitada | troca rendimento por novidade | fator `quirk_colecionador` |
| Otimizador | nota das opções; nota da sessão; tédio | rendimento × 1,3 e gosto × 0,8 na escolha | diversão × 0,7 na nota da sessão; tédio × 1,25 | fatores `quirk_otimizador` e `quirk_otimizador_gosto` |
| Resiliente | humor (meia-noite); memória | a frustração cai × 1,5 por dia | memórias de conquistas × 0,7 | — (efeito contínuo) |
| Pavio curto | revés; sessão ruim; sair com raiva; voltar | frustração ganha × 1,4; chance de largar com raiva × 1,5 | pausa antes de voltar, depois de largar com raiva, × 0,5 | `quirk_pavio_curto` no fim da sessão |
| Saudosista | memória; volta de quem desistiu; nota da sessão | boas lembranças perdem força × 0,6; chance de voltar × 1,5 | novidade fraca pesa × 1,3 na nota | `quirk_saudosista` na volta |
| Rancoroso | impressão dos lugares | impressão ruim se apaga × 0,5 | — | — (efeito contínuo, pelo fator `memory`) |
| Estudioso | Conhecimento; duração das visitas | Conhecimento de missões e descobertas × 1,5 | etapas de visita × 1,25 mais longas | — |
| Esquecido | queda por inatividade; prática | a queda começa 3 dias antes | reaprende o que perdeu com custo × 0,7 | — |
| Memória firme | queda por inatividade; impressões | a queda das capacidades × 0,5 | impressão ruim se apaga × 0,75 | — |
| Observador | exploração | ao explorar, 1 pista do conteúdo de uma zona vizinha | exploração × 1,2 mais longa | — |
| Indeciso | decisão | empate apertado (menos de 40 pontos entre as duas melhores) faz perder 5 min decidindo, uma vez por decisão | ruído × 0,7 | evento `hesitated` com `quirk_indeciso` |
| Maratonista | saciedade; disposição | com a sessão boa (nota parcial a partir de 600), a saciedade cresce × 0,6 | depois de uma sessão com mais do dobro da normal, o dia seguinte começa com −150 de disposição | — |
| Procrastinador | início do dia; voltas | o começo atrasa de 15 a 60 min dentro da janela; sem a sessão mínima, tenta a próxima janela | perde tempo de jogo | evento `session_delayed` com `quirk_procrastinador` |

### 6.1 Medição

Validação padrão (5 seeds × 1.000 × 90 dias), **Q0** (efeitos desligados) × **Q1** (ligados), com as mesmas pessoas e as mesmas traits. A tabela compara a média por pessoa de quem tem a trait. Resultados em `baselines/traits-efeitos/`.

| Trait (pessoas) | Atuação por pessoa em 90 dias | Efeito medido (Q0 → Q1) |
|---|---|---|
| Persistente (340) | manteve meta vencida 6,2 vezes; manteve o plano 0,4 vez | metas abandonadas 4,6 → 3,4; frustração 8,3 → 5,1; metas concluídas iguais |
| Impulsivo (324) | revisou o plano 77 vezes; trocou de plano 11,9 vezes | andando 20,1% → 22,8%; satisfação 599 → 608; metas e horas iguais |
| Colecionador (385) | contínuo, na nota | zonas conhecidas 14,4 → 14,8; nível 24,0 → 24,4; satisfação 598 → 609; **andando 20,8% → 24,6%** |
| Otimizador (426) | contínuo, na nota | cidade 21,9% → 16,4%; caça e chefe 32,3% → 34,3%; nível 24,1 → 24,9; **satisfação 585 → 569** (a contrapartida) |
| Resiliente (391) | contínuo | frustração 5,5 → 2,2; memórias 9,3 → 7,3; desistentes 0,8% → 0,5% |
| Pavio curto (356) | frustração a mais 176 pontos | frustração 6,4 → 14,6; saídas com raiva 0,00 → 0,02; horas iguais |
| Saudosista (443) | contínuo | memórias 9,4 → 11,7; volta de desistentes quase sem casos |
| Rancoroso (363) | contínuo | cidade 23,1% → 21,8%; o resto igual |
| Estudioso (474) | +23 de Conhecimento; +45 min em visitas | Conhecimento 545 → 555; missões 1,9% → 2,0% |
| Esquecido (320) | 5 dias de queda antecipada | Mecânica 482 → 477 (só em quem ficou dias sem jogar) |
| Memória firme (403) | contínuo | Mecânica quase igual (poucas pausas longas em 90 dias) |
| Observador (439) | 18,7 pistas; +271 min explorando | pistas 0,04 → 18,7; zonas conhecidas quase iguais |
| Indeciso (362) | hesitou 148 vezes (742 min) | espera 0% → 5,0% do tempo conectado; nível 24,2 → 23,8 |
| Maratonista (327) | 1,9 maratona; 2.046 min sem saciar | horas iguais (21,7 → 21,6); **frustração 3,1 → 5,9**; satisfação 581 → 576 |
| Procrastinador (357) | 3.228 min de atraso; 2,8 janelas perdidas | **horas por semana 19,6 → 17,9**; nível 24,8 → 24,6 |

**Geral (todos):**
- **Horas por semana:** 18,4 → 18,3.
- **Validação:**
  - regras do cenário: 8 de 9, iguais;
  - provisórias: 7 → 8 de 12. "Tempo na cidade" voltou para dentro (23,6% → 22,6%). "Andando ou esperando" foi de 20,0% para 20,7%, contando a espera do Indeciso (0,4% do total).
- **Nenhuma regra da rotina mudou.**

**Interações inesperadas** (registradas, sem recalibrar):
1. **Colecionador, Impulsivo e Indeciso andam ou esperam mais.**
   - O Colecionador vai atrás do que nunca fez e anda 3,8 pontos a mais. O Impulsivo troca mais de plano: +2,6.
   - O Indeciso perde 5% do tempo conectado decidindo. O empate de menos de 40 pontos é comum, porque o ruído da decisão é maior que isso.
   - Juntos, explicam a subida de "andando ou esperando".
2. **Maratonista não joga mais horas.** A saciedade cresce mais devagar nas sessões boas, mas a disposição menor no dia seguinte compensa, e as maratonas trazem mais frustração (cansaço e sessões longas pesam na nota).
3. **Partes quase inertes no mundo atual:**
   - **Largar com raiva** (Pavio curto) e **volta de desistentes** (Saudosista) quase não acontecem em 90 dias: as desistências ficam em 0,8%.
   - **Memória firme** e **Esquecido** dependem de pausas longas, também raras.
   - **Observador:** as pistas são de zonas vizinhas que, em geral, já são conhecidas, então quase não viram descoberta.
   - Esses efeitos devem pesar mais com combate, mortes e economia.
4. **Procrastinador perde 1,7 h por semana,** como a contrapartida prevê. As janelas perdidas (2,8 em 90 dias) são raras.

O controle (quem não tem a trait) muda pouco: até 1 ponto em "cidade" e "andando", pelo efeito das outras traits que essas pessoas têm.
