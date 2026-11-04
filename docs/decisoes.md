# Registro de decisões

Decisões que afetam persistência, determinismo, desempenho, ferramentas ou continuidade. Cada entrada registra contexto, decisão e consequências. Para mudar uma decisão, adicione uma nova entrada que a substitua; não apague o histórico.

Referência de design: [design.md](design.md).

---

## D-001 — Engine: Godot 4.7.2 (edição .NET) no lugar da Unity
**Data:** 04/11/2026 · **Estado:** aceita

**Contexto.** O plano original fixava a Unity. O criador não quer usar o editor da engine; todo o desenvolvimento acontece pela linha de comando com o Claude.

**Decisão.** Usar Godot 4.7.2 stable, edição .NET (`Godot_v4.7.2-stable_mono_win64`), a versão estável mais recente publicada em [github.com/godotengine/godot/releases](https://github.com/godotengine/godot/releases) (18/08/2026).

**Motivos.**
- Cenas (`.tscn`), temas e recursos são texto legível: o Claude cria e revisa sem abrir o editor.
- Importação, execução, capturas e exportação funcionam pela linha de comando (`--headless`).
- O cliente referencia `Simulation.Core` por `ProjectReference`, com fonte única e .NET moderno (ver D-002).
- Sem login nem ativação de licença.

**Consequências.** `UnityClient/` vira `GodotClient/` (nome do plano v1.4). O 3D precisa de técnicas de lote (`MultiMeshInstance3D`) para multidões; isso é medido no M4. Instalação local: `C:\Users\Pichau\Tools\Godot_v4.7.2-stable_mono_win64\` (SHA-512 do zip conferido com `SHA512-SUMS.txt` oficial).

## D-002 — .NET 10 (LTS) em todos os projetos
**Data:** 04/11/2026 · **Estado:** aceita

**Contexto.** Com a Unity, o Core estaria preso a `netstandard2.1` e C# 9. O .NET 8 encerra o suporte em 10/11/2026.

**Decisão.** Todos os projetos (Core, Infrastructure, Runner, Tests e o futuro GodotClient) usam `net10.0`. SDK local: 10.0.403.

**Verificação.** Projeto Godot mínimo com `Godot.NET.Sdk/4.7.2` e `<TargetFramework>net10.0</TargetFramework>`, compilado com `dotnet build` e executado com `--headless`: imprimiu `.NET 10.0.14`. Teste executado em 04/11/2026.

## D-003 — JSON de conteúdo: System.Text.Json, campos desconhecidos são erro
**Data:** 04/11/2026 · **Estado:** aceita

- Biblioteca nativa do .NET (sem dependência externa).
- Nomes de campo em camelCase; comentários e vírgula final tolerados.
- `UnmappedMemberHandling.Disallow`: campo digitado errado vira erro com arquivo e linha.
- Cada arquivo tem envelope `{ "schemaVersion": 1, ... }`. Versão diferente é recusada até existir migração.
- Hash do conteúdo: SHA-256 dos nomes e textos dos arquivos, normalizados para LF.

## D-004 — RNG: xoshiro256** 1.0, semeado por SplitMix64, fluxos derivados
**Data:** 04/11/2026 · **Estado:** aceita

- Algoritmo `xoshiro256**-1.0/splitmix64`, conferido com vetores de referência nos testes.
- Seed de cada fluxo: `Mix64(seed ^ FNV1a64(nome))`, combinada com o ID da entidade quando aplicável. `string.GetHashCode` é proibido (aleatório por processo).
- Fluxos persistentes por subsistema (`RngStreams.Get`), estados incluídos no hash e no save.
- Geração inicial por entidade (`RngStreams.ForEntity` e `PopulationGenerator`): a pessoa N é sempre igual para a mesma seed, independentemente do tamanho da população.
- Somente inteiros no domínio (`NextInt`, `ChancePermille`, `PickWeighted`).
- Mudar o algoritmo ou a ordem dos sorteios da geração exige nova versão e invalida comparações antigas.

## D-005 — Tempo: tick de 100 ms, ordem dentro do tick e salto de ticks ociosos
**Data:** 04/11/2026 · **Estado:** aceita (hipótese calibrável)

- 1 tick = 100 ms simulados; 1 dia = 864.000 ticks. Tick 0 = 00:00 da `startDate` do mundo.
- Conversão de ms autorais: arredonda para o tick mais próximo (meio arredonda para cima); duração positiva dura no mínimo 1 tick.
- Ordem em cada tick: (1) comandos previstos, por sequência; (2) eventos agendados, por (tick, sequência); (3) sistemas contínuos (a partir do M3).
- Sem sistemas contínuos ativos, o motor salta direto ao próximo trabalho. Testes provam hash idêntico ao modo tick a tick (`--no-skip`).
- Comandos sem tick explícito entram no próximo limite de tick; tick no passado é recusado.

## D-006 — Unidades inteiras no domínio
**Data:** 04/11/2026 · **Estado:** aceita

| Grandeza | Unidade |
|---|---|
| Parâmetros comportamentais | permille, 0–1000 |
| Ouro do MMORPG | cobre (`long`) |
| Dinheiro empresarial e orçamento das pessoas | centavos (`long`/`int`) |
| Distâncias no conteúdo | metros inteiros |
| Tempos no conteúdo | ms inteiros, convertidos para ticks (D-005) |

Sem `float`/`double` no estado autoritativo. Números de ponto flutuante só em relatórios e testes estatísticos.

## D-007 — Hash canônico de estado (SHA-256, formato v1)
**Data:** 04/11/2026 · **Estado:** aceita

`StateHasher` serializa campos em ordem fixa (little-endian; strings UTF-8 com tamanho) e calcula SHA-256. Usos: replay, comparação com e sem observação 3D, saves e divergências entre execuções. Todo estado novo do domínio precisa entrar no hash; ao mudar o formato, incrementar `FormatVersion`.

## D-008 — Conteúdo: JSON para troca, save SQLite como fonte da campanha, memória na execução
**Data:** 04/11/2026 · **Estado:** aceita (implementação quando o primeiro save existir)

**Contexto.** Pergunta do criador sobre o papel de JSON, SQLite e memória. A tabela da seção 14 do plano listava rascunhos como JSON.

**Decisão.**
1. JSON é o formato de autoria e troca: presets, importação e exportação.
2. Ao criar a campanha, as definições são copiadas para o SQLite do save, que passa a ser a fonte da verdade daquela campanha: versões publicadas (conteúdo ativo), criações e rascunhos. Editar o JSON do preset não altera campanhas existentes.
3. Na execução, a simulação usa `ContentDatabase` em memória. Não há acesso a banco dentro do tick.
4. Cada definição fica no SQLite como o próprio payload JSON (id, versão, estado, schemaVersion, hash), reaproveitando esquema, validador e migrações.

## D-009 — Arquivos de texto em UTF-8 sem BOM e convenção de idioma
**Data:** 04/11/2026 · **Estado:** aceita

- O parser do Godot rejeita `project.godot`/`.tscn` com BOM (verificado). O PowerShell 5.1 grava BOM com `-Encoding utf8`, então use `[IO.File]::WriteAllText` com `UTF8Encoding($false)` ou a ferramenta de escrita do Claude. Ver `.editorconfig`.
- Comentários de código: português sem acentos (ASCII). Textos exibidos ao jogador e relatórios: pt-BR com acentuação completa.

## D-010 — Atividades oferecidas pelos locais desde o esquema do M0
**Data:** 04/11/2026 · **Estado:** aceita

Seguindo o plano v1.4 (seção 4.4.1), cada região declara `activities` com tipo, requisitos (nível, grupo, classe, itens), criaturas e itens envolvidos, duração típica e risco base. O validador garante coerência: serviços só em cidade, combate nunca em cidade, emboscada só com PvP aberto, coleta só de recursos do local, boss só com criatura boss. Também avisa quando um local não oferece atividades. Os valores efetivos (recompensa, risco, tempo, benefício social) serão calculados no M2 a partir do estado do mundo.

## D-011 — Testes com xUnit 2.9.3
**Data:** 04/11/2026 · **Estado:** aceita

Template padrão do SDK 10. Testes estatísticos usam várias seeds e tolerâncias justificadas no próprio teste.

## D-012 — Identidade visual própria ("Atlas") e fontes Inter + Cinzel
**Data:** 04/11/2026 · **Estado:** aceita (desvio consciente do plano 12.1)

O criador pediu outra paleta e mais textura e dimensão. Paleta própria em `GodotClient/Scripts/Ui/UiTokens.cs`: tinta escura azulada, dourado como ação principal (`#E8B44E`) e texto levemente quente. Fontes OFL embutidas: Inter (variável, números tabulares) para a interface e Cinzel para nomes no mapa. O mapa usa um shader de terreno procedural (relevo, biomas, curvas de nível, mar animado, nuvens, dia e noite) e a população é desenhada numa única MultiMesh.

## D-013 — Operação do Godot sem editor: script único e armadilhas conhecidas
**Data:** 04/11/2026 · **Estado:** aceita

- `tools/godot.ps1 run|build|import|shot|export` concentra os comandos: ajusta o PATH (dotnet x64 primeiro), remove `ELECTRON_RUN_AS_NODE`, compila, importa e exporta.
- O export do Godot .NET exige `GodotClient/GodotClient.sln` (criada e versionada).
- No PowerShell 5.1, um log do Godot em stderr vira erro fatal com `$ErrorActionPreference = 'Stop'`; o script usa `Continue` ao chamar o Godot.
- Arquivos `.import` gerados pela importação são versionados; `.godot/` não.
- O executável lê o mundo de `Content/` ao lado do `.exe`; o export copia `Content/presets`.

## D-014 — Resultado do teste de viabilidade do Godot
**Data:** 04/11/2026 · **Estado:** concluído

Projeto `GodotClient/` criado e mantido só por texto e terminal. Ele compila com `dotnet build`, importa e roda com `--headless`, gera capturas automáticas (`--screenshot`) e exporta `build/MMOTycoon.exe` (~188 MB com o runtime .NET embutido). O executável exportado foi executado e capturado com sucesso. A simulação roda em thread própria e a tela lê snapshots.

FPS médio medido em 1920×1080, RTX 3060, velocidade 10×, 6 s, mapa com movimento de demonstração: 100 pessoas, 144 FPS (VSync); 2.000 pessoas, 143 FPS; 10.000 pessoas, 94 FPS. É uma medida desta máquina, não uma promessa de capacidade.

## D-015 — Mundo em zonas discretas de escala única (estilo Albion)
**Data:** 04/11/2026 · **Estado:** aceita (pedido do criador; amplia o plano 6 e 14)

- Cada região é uma **zona** com fronteira clara. A **função** (`city`, `hunting`) fica separada das **regras**: combate (`pvp`: none, consensual ou open), morte (`deathRule`: none, partial ou full_loot) e controle (`control`: neutral ou contestable). Cidade é sempre segura, sem perda e neutra (validador).
- **Não existe "zona de passagem":** toda zona tem estradas. As saídas ficam na fronteira, voltadas para cada zona conectada, e ligam-se ao centro (encruzilhada). Zonas vizinhas sem conexão declarada não se ligam; o validador exige conexão nos dois sentidos.
- **Escala padronizada:** `world.zoneRadius` (120 unidades = 1,2 km) vale para todas as zonas, dispostas em arranjo hexagonal.
- Conhecimento: começa na cidade e nas zonas ligadas a ela. Entrar numa zona revela as zonas ligadas às saídas dela; explorar revela duas conexões adiante.

## D-016 — Deslocamento pela geometria, posições reais no Core
**Data:** 04/11/2026 · **Estado:** aceita (hipóteses numéricas calibráveis)

- 1 unidade de mapa = 10 m; velocidade a pé `BehaviorTuning.WalkSpeed` = 15 unidades/min (9 km/h).
- `ZoneLayout` deriva do conteúdo: centro/encruzilhada, saídas (92% do raio na direção do vizinho) e pontos de atividade. Os pontos ficam fora da estrada, num anel que evita o topo (nome da zona) e as direções das saídas, ou numa posição definida no conteúdo (`spot`).
- Viagem: do ponto atual volta à encruzilhada, segue a estrada até a saída, cruza para a próxima zona, passa pela encruzilhada dela e, no destino, sai da estrada até o ponto da atividade. Cada trecho dura distância ÷ velocidade (mínimo de 1 tick). Os minutos fixos de viagem foram removidos do conteúdo.
- O personagem guarda posição, trecho atual e pontos seguintes; `PositionAt(tick)` interpola em inteiros. O mapa 2D (e o 3D no M4) mostram essas posições, sem movimento inventado na tela.
- `ZoneLayout` usa `Math.Cos/Sin/Sqrt` uma vez, na carga. É reprodutível no mesmo runtime e máquina; não há promessa de identidade binária entre plataformas.

## D-017 — Comportamento individual (início do M2)
**Data:** 04/11/2026 · **Estado:** aceita (hipóteses em `BehaviorTuning`)

- Lançamento: ao abrir o servidor, cada interessado registra-se em algum dia da primeira semana (pesos 30/22/16/12/9/6/5), perto do horário preferido. A conta e o personagem (classe ponderada pelos traços, apelido gerado) são criados no registro, e a primeira sessão começa no ato.
- Sessões: à meia-noite, cada pessoa decide se joga hoje (dias por semana, com fim de semana e feriado ajustados por `weekendBoost`), a que horas (preferido ±75 min, mais cedo no fim de semana) e por quanto tempo (±20%). No fim da sessão, termina a atividade se faltarem até 15 min ou a etapa de viagem em curso.
- Decisão por utilidade só entre atividades oferecidas por zonas conhecidas, com requisitos cumpridos e sem exigir grupo. Fatores (permille ponderado): afinidade pelos traços, adequação ao nível, companhia/lotação, risco × aversão, distância, tempo de sessão, repetição × impaciência e ruído reproduzível. Cada decisão guarda os fatores (`DecisionRecord`) e emite `activity_chosen` com as causas.
- XP por minuto e por tipo, com curva de nível `60n² + 140n` (provisória até o combate do M3).
- RNG por (pessoa, contador de decisões): o resultado não depende da ordem global além da ordem do agendador. O hash de estado passou ao formato v2.

## D-018 — Fichas em modal e rascunho de zona
**Data:** 04/11/2026 · **Estado:** aceita (desvio consciente do plano 12.2, a pedido do criador)

- Clicar num jogador ou numa zona abre uma ficha em modal grande, com abas e ações, em vez do painel lateral. Grupos de 5 ou mais pessoas no mesmo ponto viram uma bolha clicável com lista e busca.
- A configuração de zona edita um **rascunho** validado pelo mesmo `ContentValidator` do Core. Publicar no mundo ativo fica desabilitado, com explicação, até existir o sistema de atualizações (M7) ou o modo sandbox. Rascunhos e favoritos vivem só na sessão até existir o save.

## D-019 — Zona → área → atividade (lugares com posição e tamanho)
**Data:** 04/11/2026 · **Estado:** aceita (amplia o plano 4.4.1 e 6.1)

- **Zona:** mapa com regras gerais (D-015). **Área:** lugar dentro da zona, com ID, nome, tipo, descrição, raio e posição. **Atividade:** o que se faz ali. **Objetivo do personagem** (por que escolheu) e **quest** (objetivo com recompensa, ex.: "mate 10 lobos") ficam separados e chegam depois; caçar lobos existe com ou sem quest.
- A área oferece as atividades; para fazer uma delas, o personagem sai da estrada e vai até a área. Atividades sem lugar fixo (ex.: explorar) ficam na zona e usam a encruzilhada como destino.
- Tipos de área: cidade (loja, forja, taverna, praça, campo de treino); natureza (área de caça, veio de minério, campo de ervas, veio de cristal, marco); especiais (entrada de masmorra e esconderijo de guilda, que são portais para outro espaço em marcos futuros; covil de boss; passagem estreita).
- No mapa, os ícones são das **áreas** (pelo tipo), não das tarefas. A ficha da área lista as tarefas com requisitos, criaturas, saque e quem está fazendo agora.
- IDs de atividade são únicos dentro da zona, inclusive entre áreas. Ocupação, decisões e eventos continuam por `zona/atividade`.

## D-020 — Regras de construção de áreas
**Data:** 04/11/2026 · **Estado:** aceita (validador do Core; o editor de posicionamento usará as mesmas regras)

- Tipo compatível com a função da zona (`AreaKinds.AllowedIn`): cidade não tem caça e zona de caça não tem mercado.
- Raio dentro da faixa do tipo (`AreaKinds.RadiusRange`, ex.: caça 16–40, forja 8–16). O tamanho define espaço, não a quantidade de monstros ou recursos, que será configurada à parte.
- A área precisa caber na zona, não pode cobrir a encruzilhada nem uma saída (passagem obrigatória) e não pode ocupar o mesmo espaço de outra área. Não há limite arbitrário de quantidade: vale o espaço disponível.
- Posição livre (`position` relativa ao centro) ou automática, num anel que evita o nome da zona e as estradas.
- Perda por morte é marcada no mapa com força: hachura e selo vermelho "☠ FULL LOOT" e hachura e selo laranja "PERDA PARCIAL". Ruínas Sombrias passou a ser full loot.

## D-021 — Acabamento dos modais
**Data:** 04/11/2026 · **Estado:** aceita

Moldura comum (`ModalFrame`): banner com gradiente na cor do contexto, feixes e pontilhado, sombra profunda tingida, botão de fechar redondo, abas com indicador deslizante animado, transição do conteúdo, cartões com sombra e faixa de destaque, indicadores com barra lateral e ações primárias com brilho. Fichas: jogador, zona (áreas como cartões clicáveis), área (tarefas, criaturas, saque, portais) e lista de aglomeração.

## D-022 — Direção de interface "jogo de gestão" (FM com alma de MMORPG)
**Data:** 04/11/2026 · **Estado:** aceita (pedido do criador: tirar a cara de mobile/bootstrap)

- **Tipografia:** Barlow (interface), Barlow Condensed (rótulos, colunas e abas em caixa alta) e números tabulares. Cinzel fica só em nomes de zona e títulos de dossiê. Texto de 11 a 13 px.
- **Componentes:** cantos de 2 a 4 px, bordas finas, controles de 28 px, etiquetas retas com filete no lugar das pílulas, seções com faixa de título escura e cor apenas onde tem significado.
- **HUD:**
  - barra superior com indicadores (valor, variação da última hora e mini-gráfico das últimas 12 h);
  - painel lateral fixo com Visão geral (indicadores, gráfico de 24 h, "fazendo agora", perfis), Zonas (tabela ordenável), Jogadores (tabela com busca) e Eventos (feed com filtros);
  - tudo clicável, levando ao dossiê.
- **Dossiês:**
  - faixa de dados-chave no topo, abas planas e conteúdo em colunas;
  - jogador: atributos em escala de 1 a 20 com cor no estilo FM (valor bruto no tooltip), "Agora", decisão com fatores e histórico em tabela;
  - zona: tabela de áreas e gráfico de 24 h.
- **Tooltips estruturados (`RichTip`):** título, pares rótulo/valor e nota.
- **Métricas para gráficos:** amostradas na thread da simulação a cada 10 minutos simulados (inclusive no avanço inicial), sem perder amostras em velocidade alta. A tela recebe uma cópia.

## D-023 — Cliente abre com a campanha em andamento
**Data:** 04/11/2026 · **Estado:** aceita (o mapa abria vazio)

- A campanha começa à meia-noite do dia do lançamento, e os jogadores simulados entram pela própria rotina, a maioria entre 19h e 22h. Na velocidade 1x o mapa ficava cerca de 19 minutos reais sem ninguém.
- Por padrão, o cliente avança antes de abrir até o 6º dia, às 19h. Isso não muda o resultado autoritativo: é o mesmo avanço da simulação, com as métricas amostradas no caminho. A opção `--from-launch` começa do zero.
- Quando ninguém está conectado, um aviso no mapa explica o motivo e oferece "Avançar até o próximo login". O tempo avança minuto a minuto até alguém entrar, no máximo 1 dia, sem pular acontecimentos.

## D-024 — Validação com metas e várias seeds
**Data:** 04/11/2026 · **Estado:** aceita

Pedido do criador: validar o que já existe antes de seguir.

- **Metas:** ficam em `Content/presets/<mundo>/targets.json`, que só o runner usa, como o `scenario.json`.
  - São hipóteses de design ajustáveis, não regras do mundo. O Claude propôs as faixas antes das rodadas grandes, para não ajustar a meta ao resultado.
  - Métricas disponíveis: horas até um nível, % que chegou a um nível, sessões e horas por semana, divisão do tempo por tipo de atividade, ativos ÷ cadastrados, efeito do fim de semana e pico online.
- **Runner:** `--seeds 1-5` roda as seeds em paralelo, cada uma com seu próprio motor, e escreve `validation.md` e `validation.csv`.
  - Uma meta só conta como "dentro" quando todas as seeds caem na faixa (plano 17.1).
- **Estado novo no Core** (hash no formato v3), usado também nas fichas futuras:
  - tempo conectado por personagem;
  - tempo por tipo de atividade;
  - tempo jogado ao atingir cada nível.
  - O deslocamento é o restante do tempo conectado.
- **Teste estatístico do "trabalhador 19h–23h":** compara a proporção calculada exatamente a partir da regra do calendário com a observada em 5 seeds. A tolerância é de 4 erros-padrão contados por pessoa.
- **Achados da rodada de 5 seeds × 1.000 pessoas × 90 dias:** estão em [validacao.md](validacao.md).
  - A rotina bate com os perfis.
  - A progressão está cerca de 3 vezes mais lenta que a meta.
  - O deslocamento ocupa 27% do tempo.
  - As missões dominam as escolhas.
  - O PvP ainda não tem combate.
- **Correção de comportamento:** a atividade passa a ficar encerrada (personagem livre) antes da checagem de saída. Isso evita contar o mesmo tempo duas vezes.

## D-025 — Base de conteúdo e progressão antes dos objetivos pessoais
**Data:** 04/11/2026 · **Estado:** aceita (direção); números a calibrar

Decisão do criador: antes dos objetivos pessoais, corrigir conteúdo e progressão. Se recompensas e faixas estiverem erradas, os objetivos só reforçam as distorções (ex.: o quadro de missões da cidade, que hoje rende XP contínuo em qualquer nível).

**Ordem:**
1. Revisar o XP e a curva de níveis.
2. Redistribuir e ampliar as áreas.
3. Montar o catálogo de quests por cidade.
4. Medir a progressão até o nível 30.
5. Integrar os objetivos pessoais sobre essa base.

**Direção:**
- **Níveis:** teto inicial 30, configurável no conteúdo. Expansões futuras podem chegar a 100.
- **Progressão lenta.**
  - A referência é em horas jogadas, incluindo deslocamento e cidade.
  - Caça, exploração, coleta e quests são calibradas em conjunto, para nenhuma fonte dominar.
  - A curva deve permitir evoluir sem depender de diárias e sem esgotar o conteúdo de uma faixa antes de alcançar a próxima.
- **Três parâmetros separados:**
  - faixa recomendada da área;
  - nível do desafio (monstros, coleta, exploração);
  - requisitos de cada quest.
  - A faixa da zona deixa de definir a adequação de tudo que existe nela, e a faixa ampla da cidade não valoriza missões.
  - O XP de uma atividade sai do nível do desafio comparado ao do personagem.
- **Áreas:** mais áreas, com faixas menores, e distribuição gradual do conteúdo até o 30. Uma zona pode ter áreas de dificuldades diferentes.
- **Quests:** viram conteúdo estruturado, não atividade.
  - A cidade oferece a quest, e o personagem a realiza no mundo.
  - Pegar a quest no quadro é uma ação curta e sem XP.
  - Cada quest tem requisitos, etapas e recompensas.
  - Categorias:
    - **principais:** cadeias com pré-requisitos, uma vez por personagem;
    - **secundárias:** disponibilidade e repetição definidas na própria quest;
    - **diárias:** conjunto limitado por cidade e por personagem, renovado pelo calendário simulado.
  - Cada cidade tem um catálogo limitado.
  - As etapas só usam acontecimentos reais da simulação.
- **Recompensas ainda sem sistema** (ouro, itens): ficam definidas no conteúdo e marcadas como "sem efeito nesta versão". Não são simuladas.
- **Editor futuro:** permitirá criar quests próprias (texto, etapas, condições, recompensas). O formato de dados já nasce pensando nisso.
- **Parâmetros configuráveis:** todos os parâmetros de XP, curva e quests ficam no conteúdo, e não em constantes do Core.

## D-026 — Diretrizes acordadas para os objetivos pessoais (implementação depois do D-025)
**Data:** 04/11/2026 · **Estado:** aceita (diretrizes)

Proposta do criador, analisada com o Claude.

**Estrutura:**
- **Categorias:** objetivos gerais, secundários, da sessão e circunstanciais. Não formam uma escada obrigatória; dependências só existem quando são declaradas.
- **Três camadas separadas:** objetivo pessoal, plano da sessão e atividade executada.

**Continuidade:**
- Ao fim de um ciclo, a pessoa primeiro verifica se continuar ainda atende ao plano.
- Há margem mínima para trocar e compromisso mínimo configurável.
- Acontecimentos relevantes provocam revisão.
- Tempo de viagem já gasto não obriga a insistir num plano ruim.

**Sistemas ainda não implementados × bloqueio real:**
- "Sem efeito nesta versão: sistema econômico não implementado" é informação de desenvolvimento. Aparece só no diagnóstico e nunca como condição do mundo para a pessoa simulada.
- Um bloqueio real é uma condição do mundo, como "precisa de capital para abrir o negócio".
- Ambições sem sistema ficam guardadas, sem contribuição inventada. A pessoa continua com objetivos secundários executáveis conforme o perfil.

**Pontuação:**
- **Recompensa num lugar só.** A afinidade representa diversão; o avanço esperado representa progresso. O valor do XP não é contado duas vezes.
- **O nível mantém as outras funções:** elegibilidade, dificuldade e risco.
- **Agregação dos objetivos:**
  - só objetivos ativos e aplicáveis entram;
  - os pesos e limites são explícitos;
  - um objetivo pouco relevante não pode diluir uma necessidade importante;
  - ter mais objetivos não dá vantagem artificial.

**Conhecimento:**
- Começa com "sabe que existe" × "conhece".
- Entrar numa zona não revela todos os seus segredos de vez.
- No M2, descobrir zonas é a atividade disponível. Explorar o interior, achar áreas e conteúdo novo vem depois.
- "Expectativa igual ao resultado" precisa ser verificado, porque viagem, duração variável e disponibilidade mudam o progresso da sessão.

**Expectativa de progresso:**
- Combina histórico recente, expectativa do perfil e uma referência conhecida.
- Adapta-se aos poucos, com limite: progredir mal por dias não pode virar "satisfatório".
- O tédio fica separado da eficiência: quem evolui bem ainda pode querer variedade.

**Repetição:** a penalidade universal é substituída por tédio contextual, com uma preferência por variedade separada da paciência.

**Medição:**
- Antes de mexer, mede-se uma linha de base: viagem inicial, trocas, permanência por área e por zona, motivos de troca.
- "Decisões que mudariam sem o termo de objetivos" é só diagnóstico local.
- Para comparar trajetórias, usam-se execuções separadas, com os objetivos ligados e desligados por configuração.

**Save:** estado novo como dado simples e no hash prepara o caminho, mas não substitui o teste de salvar e carregar. Esse teste fica pendente até o save existir (D-008).

**Incrementos seguintes:** emoções (com escalas e valores neutros documentados), memória e sessões flexíveis, com teto de extensão.

## D-027 — Progressão no conteúdo: curva, taxa por faixa e nível do desafio
**Data:** 04/11/2026 · **Estado:** aceita (passo 1 do D-025)

**Princípio.** No jogo, o jogador é o desenvolvedor do MMORPG: ele vai criar áreas, itens, magias e ajustar a progressão.
- Por isso a progressão é **conteúdo** (`progression.json`), validada como os outros arquivos, e não constante do Core.
- O editor do jogo poderá alterá-la depois, como a configuração de zona.

**Ritmo escolhido pelo criador: opção A, lenta.**
- As referências são em **horas conectadas** (contando viagem, cidade e organização), como resultado esperado de um perfil de referência, e não como prazo para todos.
- Os primeiros níveis chegam rápido, porque o começo apresenta o jogo. A desaceleração fica forte nas faixas altas.
- O hardcore chega ao fim do conteúdo enquanto os casuais ainda ocupam áreas anteriores. O MMORPG precisa oferecer coisas relevantes para os dois, sem que o nível máximo seja o único ponto divertido.

**Regras:**
- **Curva:** XP para sair do nível n = cúbico·n³ + quadrático·n² + linear·n + constante.
  - Valor atual: 30·n² + 190·n.
  - Coeficientes negativos são proibidos, para a curva sempre crescer.
- **Taxa por faixa de nível** ("stages", pedido do criador, como em servidores de MMORPG): a partir de cada faixa, todo XP ganho é multiplicado por uma taxa menor.

  | Níveis | Taxa |
  |---|---|
  | 1–4 | ×3,0 |
  | 5–9 | ×2,0 |
  | 10–14 | ×1,4 |
  | 15–19 | ×1,0 |
  | 20–24 | ×0,8 |
  | 25–29 | ×0,7 |

- **Teto:** 30, configurável até 100. No teto, a pessoa não ganha XP.
- **XP por minuto** por tipo de atividade, num desafio de nível 1: caça 10, chefe 14, patrulha e emboscada 8, missão 8, exploração 6, coleta 5.
  - O XP por minuto cresce 20% por nível do desafio, como monstros mais fortes rendendo mais.
- **Nível do desafio** (`challengeLevel` na atividade):
  - caça e chefe usam a média dos níveis das criaturas;
  - os outros tipos que dão XP precisam declará-lo;
  - a cidade não tem desafio, então **pegar missão no quadro, comércio, taverna, treino e conserto não dão XP**.
  - A faixa da zona virou só orientação: não entra mais no XP nem no risco.
- **Diferença de nível:** sem perda até 2 níveis de distância.
  - Desafio fraco: −15% por nível além disso, com mínimo de 10%.
  - Desafio forte: −25% por nível além disso.
  - O risco percebido sobe 120 permille por nível em que o desafio passa o personagem.
- **Pontuação:** o fator `level_fit` passou a usar esse rendimento, e não a faixa da zona. O valor do XP ainda aparece só nele; a troca para "avanço esperado" fica para os objetivos (D-026).
- **Prévia nominal:** `--progression` no runner mostra horas por nível sem rodar a simulação.
  - Personagem de referência: caça no próprio nível e ganha XP em 60% do tempo conectado.
  - Nível 5 em cerca de 2 h, 10 em cerca de 11 h, 20 em cerca de 70 h e 30 em cerca de 219 h.

**Medido** (5 seeds × 1.000 pessoas × 90 dias): nível 5 em 2,7 h, nível 10 em 17 h e nível 15 em 41 h, todos dentro das metas novas.
- **Efeito colateral esperado:** os personagens passam do conteúdo atual (desafios até cerca de 15).
  - Nível mediano no fim: casual 14, competitivo e hardcore 19.
  - Sem caça do nível deles, vão para a taverna (31% do tempo).
- **O que resolve:**
  - a redistribuição das áreas até o 30 (passo 2);
  - as quests estruturadas (passo 3).
- **Desvio do plano:** a seção 4.4 não fala em taxa por faixa. Ela entrou a pedido do criador e é configurável.

## D-028 — Mapa ampliado em grade de lotes, terreno por parâmetros e conteúdo até o 30
**Data:** 04/11/2026 · **Estado:** aceita (passo 2 do D-025; mapa aprovado pelo criador)

**Grade de lotes** (`world.json`, bloco `map`).
- O mundo é uma grade hexagonal de lotes do tamanho de uma zona: 10 colunas × 7 linhas, passo de 230 × 200 unidades, linhas ímpares deslocadas meio passo.
- Cada zona ocupa o centro de exatamente um lote. O validador recusa zona fora do lote e dois lotes na mesma posição.
- Os **55 lotes livres** aparecem no mapa como terra não desenvolvida, com névoa e borda tracejada. Ao passar o mouse, aparece o tooltip "Lote livre".
- **Futuro:** o jogador (desenvolvedor do MMORPG) poderá comprar ou ocupar lotes e criar zonas, áreas, monstros e quests numa tela de edição própria. A grade e todos os parâmetros já são conteúdo validado, sem constantes no cliente.

**Terreno por zona** (`terrain` em cada zona: forma do relevo, altitude, aspereza e semente).
- **Formas:** planície, colinas, montanhas, vale com rio, planalto, brejo com poças e terras vulcânicas com fendas de lava.
- **Biomas novos:** pântano, neve, vulcânico e sombrio.
- **Correção do mapa** (pedido do criador):
  - antes, o relevo era uma cúpula no centro de cada zona (a "montanhinha") e todas as células tinham o mesmo formato;
  - agora as fronteiras são orgânicas, por deformação de ruído desligada perto dos portões para as estradas cruzarem no lugar certo;
  - o relevo vem só dos parâmetros, com mistura nas fronteiras.
- É só apresentação: não muda a simulação.

**Câmera.**
- O zoom afasta até a grade inteira, com mar em volta.
- Afastado, os rótulos encolhem para caber na zona (ou só aparecem em foco) e os emblemas de área viram pontos.

**Conteúdo** (proposta aprovada):
- **3 cidades:** Vila Aurora (1–12), **Forte do Vau** (10–20) e **Cidadela de Cinzaforte** (18–30). Todas têm mercado, forja, taverna, praça e treino.
- **12 zonas**, 6 delas novas: Brejo das Brumas, Mata dos Espinhos, Planalto dos Ventos, Picos Gelados, Terras Queimadas (PvP, perda parcial) e Fortaleza Sombria.
- **54 áreas**, cada uma com faixa recomendada (orientação).
- **26 criaturas** (19 novas, entre elas o chefe Senhor das Cinzas, nível 30, grupo de 10) e 5 materiais de coleta novos.
- **Passagens explícitas.** As zonas encostadas sem passagem (ex.: Brejo–Cidadela, Colinas–Ravina) não permitem travessia.
- **Garantias testadas:**
  - todo nível de 1 a 29 tem ao menos 2 caças com rendimento pleno, uma delas sem PvP;
  - toda zona fica a no máximo 2 passagens de uma cidade;
  - o desafio de cada área fica a até 2 níveis da faixa dela.
- **As criaturas novas ainda não têm saque** (`loot` vazio). O combate e o saque chegam no M3.

**Posicionamento automático de áreas.** Procura, a partir de um ângulo preferido, a primeira posição livre no anel externo e depois no interno. Evita o setor do nome, as estradas e as outras áreas. Era necessário para zonas com 4 ou 5 áreas.

**Medido** (5 seeds × 1.000 pessoas × 90 dias): ver [validacao.md](validacao.md).
- O tempo em combate entrou na meta (38%).
- O quadro de missões caiu para menos de 1% das escolhas por cidade.
- Nível 20 em 73 h (referência 95 h) e nível 30 em 190 h. Só 12–15% chegam ao 30, então essa mediana subestima o tempo real.
- **A recalibração da curva fica para depois das quests** (passo 3), com a escolha do criador entre:
  - (a) total perto de 200 h, com desaceleração suave no fim;
  - (b) desaceleração forte no fim, com 220–250 h.

## D-029 — Legibilidade do mapa: ilha, zonas maiores, estradas e rótulos
**Data:** 04/11/2026 · **Estado:** aceita (pedido do criador após ver o D-028)

**Problemas apontados pelo criador:** mapa poluído ao aproximar, estradas grossas e retas, texto demais por cima, zonas pequenas e fundo retangular.

**Mudanças:**
- **Zonas maiores:** raio de 120 para 140 unidades (1,4 km), grade de 270 × 234.
  - Andar entre zonas vizinhas passa de cerca de 15 para cerca de 18 minutos.
  - Medido: o deslocamento foi de 21,2% para 22,4% do tempo conectado. A progressão ficou um pouco mais lenta, dentro das mesmas metas: nível 10 em 15,3 h, 20 em 75 h, 30 em 193 h.
- **Ilha:** o `world.json` ganhou `map.mask`, uma linha por linha da grade ('#' terra, '.' mar).
  - Zona em lote de mar é erro de validação.
  - A forma atual tem 44 lotes de terra: 15 zonas e 29 livres.
  - O editor de mapa futuro poderá mudar a máscara, por exemplo para conquistar mar.
- **Estradas:**
  - finas, cor de terra, com curva suave e estável por trecho;
  - os viajantes andam sobre a mesma curva, que é só desenho: o Core mede o trecho em linha reta;
  - passagens viraram dois marcos discretos e as encruzilhadas, pontos pequenos;
  - as trilhas até as áreas são finas e somem no zoom afastado.
- **Texto:**
  - o rótulo da zona mostra só o nome e, se houver jogadores, um número;
  - a linha de regras (nível, PvP, perda) aparece como texto simples, sem caixas, só ao aproximar ou em foco;
  - os nomes de área só aparecem bem perto ou em foco;
  - no mundo todo à vista, só as cidades e a zona em foco têm nome.
- **Relevo:** a normal agora sai de diferenças finitas, não das derivadas de tela, que deixavam o terreno em blocos.

## D-030 — Terreno que a simulação respeita, estradas calculadas e cidades por nível
**Data:** 04/11/2026 · **Estado:** aceita (pedido do criador)

**Pedidos:**
- estradas naturais, cinzas e opacas, que não atravessam água;
- só a cidade com nome fixo;
- lojas dentro da cidade, com muralha e calçamento;
- cidade com níveis;
- área urbana marcada como as áreas de caça;
- rio natural;
- tudo configurável para o futuro editor de tiles.

**Água é do Core** (`ZoneTerrain`).
- É gerada com aritmética inteira determinística (`FixedNoise`), em células de 4 unidades.
- **Rio:** nasce fino dentro da zona, serpenteia em duas escalas, alarga e termina num lago. Desvia da muralha. É configurável em `terrain.river`:
  - ligado ou desligado;
  - direção de 0 a 7;
  - deslocamento, largura e sinuosidade.
  - Sem configuração, o vale ganha um rio automático pela semente.
- **Poças e lagos:** `terrain.poolsPermille`, em qualquer relevo (padrão: brejo 380).
- **Terra firme garantida** sob as áreas, na encruzilhada, nos portões e dentro da muralha.
- O mapa desenha exatamente essa água, a partir de uma textura do Core.

**Estradas calculadas** (A* determinístico na grade do terreno).
- Da encruzilhada até cada saída e até cada área (trilha), dentro da própria zona.
- Contornam as outras áreas e seguem uma ondulação natural.
- Cruzam água só por ponte curta, que é desenhada.
- **O tempo de viagem segue o comprimento real da estrada** (`LegLength`), e os viajantes andam sobre ela.
- Quando o tile muda, as estradas se recalculam sozinhas. Estradas editadas à mão ficam para o editor.

**Cidade por nível** (`cityLevel` de 1 a 5).
- **Muralha:** raio = R × (30 + 6 × nível)%. Paliçada nos níveis 1–2; pedra com torres do 3 em diante.
- **Desenho:** casas em quantidade pelo nível, calçamento dentro da muralha e aberturas onde passam as ruas.
- **Lojas, forja, taverna, praça e treino** ficam dentro da muralha como prédios, sem a mancha externa.
- **Posição da cidade no tile:** `cityPosition`, um deslocamento a partir do centro. A muralha, os prédios e as estradas se adaptam.
- **Níveis do preset:** Vila 2, Forte 3, Cidadela 4.

**Área urbana** = tudo dentro da muralha.
- Tem o mesmo desenho das áreas de caça (mancha semiopaca, núcleo e contorno tracejado).
- Acende ao passar o mouse e tem tooltip próprio.
- **É o lugar onde regras próprias vão valer.** Exemplo: uma cidade sem PvP dentro de uma zona com PvP. Ainda não implementado.

**Rótulos:**
- só as cidades têm nome fixo, logo abaixo da muralha;
- as outras zonas mostram nome e regras ao passar o mouse ou quando selecionadas.

**Capturas:**
- ignoram o mouse real;
- `--mouse=XxY` simula o hover.

**Medido** (5 seeds): deslocamento em 21,8%; nível 10 em 15,6 h, 20 em 76 h, 30 em 195 h; combate em 37%.

**Desvio do plano:** a seção 9 previa terreno só visual. Aqui, água e estradas passaram a afetar a simulação, porque a lógica do mundo exige: as estradas contornam rios e a viagem é mais longa.

## D-031 — Quests estruturadas por área de quest
**Data:** 04/11/2026 · **Estado:** aceita (passo 3 do D-025)

**Área de quest** (pedido do criador). As quests pertencem a um **quadro**, ou seja, a uma área (hoje, a praça de cada cidade), e não à cidade inteira.
- `quests.json` declara os quadros (área, atividade de aceitar e entregar, limite de diárias) e as quests.
- O editor futuro poderá criar quadros e quests em outras áreas.

**Quest** = conteúdo com:
- **descrição e categoria:**
  - **principal:** cadeia com pré-requisitos, uma vez;
  - **secundária:** uma vez no preset; a repetição é configurável por `repeatCooldownDays`;
  - **diária:** renova pelo dia simulado, com limite por quadro e por personagem.
- **requisitos:** nível mínimo e máximo, quests anteriores, classe;
- **etapas:**
  - fazer uma atividade do mundo por N minutos;
  - visitar um ponto de missão (atividade do tipo quest numa área);
  - a entrega no quadro é sempre a última etapa;
- **recompensas:** XP (fração do XP do nível de referência), ouro e itens.
  - Ouro e itens ficam definidos, mas **não implementados (M5)** e marcados assim na ficha.
  - O XP nem sempre é o foco: em várias secundárias de coleta, o XP é menor e a recompensa principal é o item.

**Regras na simulação:**
- **Aceitar e entregar são ações pontuais** (5 min) no quadro.
  - O quadro só vira opção quando há quest elegível ou entrega pendente; senão, não oferece nada. Isso corrige o quadro como atividade repetível.
  - Numa visita, a pessoa entrega o que estiver pronto e aceita até encher as vagas (máximo de 3 ativas).
- **As etapas avançam com o que a pessoa realmente faz.**
  - **Limitação explícita:** etapas de caça, coleta, patrulha e emboscada contam tempo de atividade, marcadas como "provisório".
  - Abates e itens só no M3/M5. Nada é registrado como "derrotou N" ou "coletou N".
- **Atividades do tipo quest não rendem XP contínuo.** O XP vem só da entrega.
- **Pontuação:** termos `quest_offer` (150), `quest_step` (220) e `quest_deliver`.
  - A entrega pesa 160 com uma quest pronta, mais 90 por quest pronta extra: carregar várias puxa de volta ao quadro.
- **Estatísticas por quest no Core** (hash v4): aceitaram, entregas, personagens únicos, em andamento, tempo conectado até entregar, nível na entrega, última entrega e XP entregue.
- **XP por fonte** por personagem (quests × atividades), com metas novas: `quest_xp_share` e `quests_per_character`.

**Conteúdo:**
- 3 quadros e 36 quests (por quadro: 4 principais em cadeia, 5 secundárias e 3 diárias);
- 10 pontos de missão novos nas áreas do mundo.

**Interface:**
- **Ficha da quest (estilo FM)**, com três abas:
  - Objetivos;
  - Requisitos e recompensas (quests anteriores e liberadas, clicáveis);
  - Dados (números da campanha e quem está fazendo agora, clicável).
- **Ficha da área do quadro:** aba "Missões" com o catálogo em tabela.
- **Ficha do jogador:** aba "Missões", com as ativas (progresso da etapa) e as concluídas.

**Medido** (5 seeds × 1.000 × 90 dias):
- 20% do XP vem de quests e há 21 entregas por personagem;
- nível 10 em 14,9 h, 20 em 73 h, 30 em 192 h;
- combate em 33%;
- **tempo andando em 30%** (antes das quests, 22%).

**Troca medida** (os pesos foram escolhidos com o criador em vista):
- peso de quest alto (oferta 180, entrega 320, etapa 260): 34% do XP de quests e 40% do tempo andando;
- peso baixo: quase ninguém faz quest e se anda 23%.
- O ciclo "quadro → etapa → quadro" anda por natureza. Quem deve reduzir isso é o plano da sessão e a continuidade (D-026), sem compensação artificial.

## D-032 — Desempenho do mapa: cache de desenho, lotes e orçamento por quadro
**Data:** 04/11/2026 · **Estado:** aceita (pedido do criador)

**Problema:**
- O jogo rodava a 144 fps fixos.
- Depois do mapa ampliado, das cidades e das estradas, caiu para cerca de 60 fps de perto e cerca de 100 fps de longe, antes de qualquer simulação pesada.

**Medição primeiro** (`--measure-seconds`, sem vsync, CPU e GPU separados):
- **Era CPU, não GPU.**
  - O mapa redesenhava tudo a cada quadro: 8–9,5 ms em estradas, muralhas, casas, prédios, áreas e portões, mais 2,7–3,5 ms de emblemas e nomes.
  - O shader do terreno gastava 4–5 ms.

**Mudanças:**
- **Cache de desenho.**
  - O mapa estático (estradas, cidades, áreas) e os emblemas e nomes ficam em camadas desenhadas uma vez.
  - Entre redesenhos, a câmera só move e escala essas camadas, sem reenviar comandos.
  - Redesenha quando muda a faixa de detalhe, a seleção, a luz do dia (em 20 degraus), a camada do mapa ou quando o zoom se afasta mais de 12–14% do último desenho.
  - Emblemas e nomes se atualizam 4 vezes por segundo, para os contadores de jogadores.
- **Hover sem redesenho:** os destaques (área, cidade, prédio, emblema e nome em foco) ficam numa camada dinâmica que desenha só o item focado.
- **Animações a cada quadro, mas leves:** pulso dos portais, brilho das forjas e luzes da cidade à noite.
- **Lotes:**
  - todos os preenchimentos de uma etapa (áreas, chão urbano, casas, prédios, torres, marcos, castelos) vão ao renderizador num único lote de triângulos (`CanvasItemAddTriangleArray`);
  - estradas, trilhas, tracejados, muralhas e cumeeiras usam `DrawMultiline`, uma chamada por estilo.

**Medido** (máquina do criador, 1920×1080, sem vsync):

| Vista | Antes | Depois |
|---|---|---|
| Mapa inteiro | 51 fps (19,6 ms) | ~350 fps (2,9 ms) |
| Perto | 42 fps (24 ms) | ~300–340 fps (3,0–3,3 ms) |
| Longe | 54 fps | ~450 fps |
| Com 2.000 pessoas | — | ~300 fps |
| Pior caso (redesenho em todo quadro) | 48 fps | ~115 fps |

**Regra de desempenho:**
- `tools/perf.ps1` mede as vistas fixas e falha se o p95 do quadro passar de 6,9 ms (144 Hz).
- Rodar depois de mudanças no cliente. Os números são desta máquina.

## D-033 — Sistema visual v2: dossiês de jogo de gestão (estilo FM)
**Data:** 04/11/2026 · **Estado:** aceita (primeira passada; revisão do criador pendente)

**Pedido do criador:**
- tirar a cara de "MMORPG mobile" (personagens cabeçudinhos, medalhões, chips coloridos, gráficos de mobile);
- uma interface de sistema de gestão no estilo do FM24: escura, elegante e densa;
- tipografia agradável para análise, gráficos profissionais e tooltips explicando;
- voltar e avançar entre fichas.

**Mudanças:**
- **Tipografia:**
  - Inter (OFL, em `Assets/Fonts`) no corpo e nos números tabulares;
  - Inter SemiBold nos títulos das fichas;
  - Barlow Condensed nos cabeçalhos em caixa alta;
  - Cinzel fica só nos nomes de zona no mapa.
- **Paleta mais escura e neutra:** fundo #08090C, painéis #12161C, bordas #222831 e cantos de 2 px.
- **Ficha = tela de dossiê:**
  - ocupa toda a área abaixo da barra superior;
  - barra de navegação com voltar e avançar (histórico de até 60 fichas, preservando a aba);
  - trilha ("Mundo › Vila Aurora › Quadro… › Lobos na estrada");
  - atalhos: Alt+←/→ e botões laterais do mouse.
- **Blocos:**
  - cards com cabeçalho limpo e marcador pequeno na cor do contexto;
  - indicadores compactos (rótulo em caixa alta, valor grande), que no painel lateral ocupam a célula;
  - barras finas com trilho;
  - etiquetas retangulares discretas.
- **Gráficos:** grade tracejada discreta, área em degradê, linha fina, valor do último ponto e leitura ao passar o mouse; divisões com rótulos redondos.
- **Identidade visual:**
  - ícones planos e quadrados (área, zona) no lugar dos medalhões;
  - cartão com iniciais no lugar do rosto cartunesco.
- **Ficha do jogador:**
  - novos cards: uso do tempo conectado por atividade, ritmo de evolução (horas até cada nível), minutos jogados por dia (14 dias) e "Combate · últimos abates" (vazio honesto até o M3);
  - abas reservadas "Inventário · M5" e "Banco · M5".
- **Detalhe sob demanda:** a simulação copia o detalhe só da pessoa com a ficha aberta (`WatchPerson`), não de todos a cada publicação.
- **Correções:**
  - emblema e nome duplicados no hover (mesma geometria e anel de destaque);
  - nome de zona em foco atrás da barra superior;
  - termos internos traduzidos (quest_step etc.);
  - tempo das saídas pela estrada real.

**Desempenho** (`tools/perf.ps1`): todas as vistas com p95 entre 3,7 e 5,0 ms, dentro do orçamento de 6,9 ms.
