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
- **P4b (esta):** catálogo, sorteio, estado da pessoa e ficha. Os efeitos ficam **desligados**: as traits existem, mas não mudam a simulação.
- **P4c:** efeitos das traits da pessoa (incluindo Maratonista e Procrastinador).
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
