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

## D-034 — Telas do jogo, pausa fora do mapa e kit visual v3
**Data:** 04/11/2026 · **Estado:** aceita (revisão do criador pendente)

**Pedido do criador** (comparando com uma tela de referência de jogo de gestão):
- O mapa é a única tela em que o tempo passa.
- No mapa, o clique abre um resumo pequeno; "Ver informações" abre a página completa, num menu de telas.
- Interface nítida, com detalhe e vida: fontes do tamanho certo, gráficos caprichados, alinhamento e espaçamento consistentes.
- Configurações de vídeo e tela cheia.
- Textura sutil nos cards.
- Bordas dos cards alinhadas entre linhas.

**Decisões:**
- **Telas:**
  - menu no topo (Mapa, Jogadores, Zonas, Missões) e engrenagem de Configurações;
  - fichas viram páginas;
  - ao sair do mapa o tempo pausa e volta na mesma velocidade (opção "Pausar fora do mapa");
  - o mapa e seus painéis ficam ocultos e sem processamento nas páginas;
  - histórico voltar/avançar global (Alt+←/→, botões laterais do mouse);
  - atalhos M, J, Z e Q.
- **Cartão-resumo no mapa** (jogador, zona e área): dados ao vivo e "Ver informações". O painel lateral e as bolhas também abrem o cartão.
- **Páginas de lista:** Jogadores (filtros, busca, ordenação, até 300 linhas), Zonas e Missões (por categoria).
- **Configurações:** modo de tela (F11 / Alt+Enter), tamanho da janela, VSync, limite de FPS, MSAA 2D, texto nítido/suave, FPS na barra, pausa fora do mapa, velocidade inicial, aviso de campanha e atalhos. Ficam salvas em `user://settings.cfg`, fora do estado da simulação.
- **Kit visual v3** (`docs/ui-kit.md`):
  - escala de tipos de 11 a 28 px, rótulos claros em peso médio;
  - texto preso ao pixel com hinting leve e suavização LCD (comparação ampliada de quatro modos);
  - cards com degradê, textura gerada por código, brilho e hover;
  - grade de 12 colunas (`SpanRow`) para as bordas coincidirem entre linhas;
  - ícones Lucide (ISC) rasterizados no tamanho exato;
  - gráficos novos: linha com pontos e pílula de valor, colunas com topo arredondado, rosca, anéis, notas de 1 a 5 e meio-medidor;
  - linhas clicáveis com hover.
- **Ficha do jogador, Visão geral:**
  - situação atual com os fatores da decisão;
  - atributos em quatro grupos com barras e traços marcantes com ícones;
  - relatório do analista com notas de 1 a 5 (engajamento contra a rotina, ritmo de XP/h contra a mediana do servidor, exploração, missões; a conta fica no tooltip);
  - forma dos últimos 14 dias contra a rotina e as últimas sessões;
  - evolução, uso do tempo e classificação no servidor;
  - zonas frequentadas, missões e atividades recentes clicáveis.
  - Combate, Inventário e Banco ficam em abas com o marco.
- **Zona, missão e listas:** Visão geral da zona (lotação, perigo, atividades, áreas, regras, saídas, movimento, níveis e quem está aqui); Visão geral da missão (linha do tempo das etapas, requisitos e recompensas, funil de desempenho, distribuição por etapa).
- **Dados de apoio (só cliente, não mudam a simulação):**
  - diário de sessões por pessoa a partir dos eventos;
  - tempo conectado por personagem no snapshot.
- **O que não é mostrado:** o humor da pessoa existe no Core, mas nenhum sistema o altera ainda, então não aparece como dado (D-026).

- **Barra superior e interface do mapa** (pedido do criador, após ver a barra sobrepondo o painel lateral):
  - **barra:**
    - altura fixa de 56 px, com o conteúdo cortado; os botões do menu eram mais altos que a barra e a faziam crescer por cima do painel;
    - menu mais espaçado, com seta e **submenu** ao passar o mouse:
      - Mapa: camadas e enquadrar;
      - Jogadores: todos, conectados, favoritos;
      - Zonas: todas e cada cidade;
      - Missões: por categoria;
    - indicadores sem minigráficos;
    - relógio com ícone;
  - **painel lateral recolhível:** botão no grupo de controles do mapa ou tecla P, com animação; o mapa desliza sem perder o zoom e a preferência fica salva;
  - **painéis do mapa no kit:** camadas com ícones, controles com ícones, aviso, lista da bolha e cartão-resumo.

**Desempenho:**
- `tools/perf.ps1`: todas as vistas do mapa com p95 entre 2,8 e 4,8 ms, dentro do orçamento.
- Página do jogador: cerca de 1.100 FPS, p95 de 1,1 ms.

## D-035 — Menu principal lateral e ficha do jogador sem o "relatório"
**Data:** 04/11/2026 · **Estado:** aceita

**Contexto:**
- O menu no topo não comportaria as seções futuras (mercado, finanças, equipe, atualizações, relatórios, incidentes).
- O criador pediu um menu lateral.
- Pediu também para tirar da ficha do jogador o que era só inspiração visual da referência ("Forma", "Relatório do analista").

**Decisões:**
- **Menu principal à esquerda:**
  - aberto ao iniciar, com 224 px; recolhido vira uma faixa de ícones de 60 px, não some;
  - recolhe pelo botão no rodapé ou pela tecla N;
  - seções Mundo (Mapa, Zonas, Missões), Comunidade (Jogadores, Guildas · M6), Economia e empresa (Mercado · M5, Finanças, Equipe e Atualizações · M7) e Relatórios (Relatórios · M8, Incidentes · M9);
  - os itens futuros aparecem apagados com o marco;
  - subitens em sanfona: camadas do mapa, cidades, categorias de missão, conectados, favoritos;
  - Configurações no rodapé;
  - aparece em todas as telas.
- **Barra superior mais limpa:** voltar/avançar, nome da tela, indicadores, relógio, velocidade ou "Voltar ao mapa". A marca e a engrenagem foram para o menu lateral.
- **Painel do mapa à direita:** 340 px (era 430), sempre fechado ao abrir o jogo; abre com P ou pelo botão nos controles do mapa.
- **Ficha do jogador:**
  - **Cabeçalho:** ganha a personalidade principal (o traço que mais se afasta do meio, alto ou baixo).
  - **Visão geral:**
    - Inventário · M5 no lugar do relatório: espaços de equipamento pelas categorias de item do conteúdo (arma, armadura), mais o ouro e os itens que as missões entregues teriam dado, ainda não aplicados;
    - Progresso e objetivos no lugar da "forma": nível e XP com previsão do próximo nível, personalidade e perfil, objetivos atuais (próximo nível e missões em andamento);
    - Horários de jogo: minutos por hora do dia contra a janela da rotina. Para quem opera o MMORPG isso indica carga do servidor e o horário de eventos e manutenção.
  - **Aba Estatísticas** (a comparação com o servidor deixou de ser "relatório do analista"): comparação com a mediana do servidor, sessões dos últimos 14 dias, classificação, XP por fonte e distribuição de níveis do servidor.
  - Relatório de moderadores fica para quando houver moderação (inspeção de jogadores, M9).
- **Objetivos pessoais:** ainda não existem na simulação; são o próximo passo do roteiro (D-026). O card mostra só metas que já existem e avisa isso.
- **Textura dos cards:** reduzida; a hachura mais forte incomodava a vista.

**Desempenho:**
- `tools/perf.ps1`: p95 entre 2,4 e 4,2 ms em todas as vistas.
- 112 testes aprovados.

**Ajustes de UX (revisão do criador, 04/11/2026):**
- **Dica vazia:** o Godot 4.7 pede a dica personalizada mesmo sem texto, e as linhas clicáveis desenhavam uma moldura vazia ao passar o mouse. Agora uma dica sem texto devolve um controle invisível (`RichTip.Make`) e nada aparece.
- **Menu lateral:** subopções menos recuadas, com reticências e dica com o nome completo; a seção ativa abre os subitens sozinha.
- **Rolagem:** a barra fica no canto direito, com folga de 14 px entre ela e o conteúdo, em todas as páginas e no painel do mapa.
- **Tabelas:**
  - vão de 18 px entre colunas (o nível não encosta mais na situação);
  - toda coluna ordenável mostra o indicador (seta dourada na coluna ativa, setas discretas nas outras), sempre à direita do título;
  - no painel estreito só aparece a seta da ativa, e saíram as colunas que não cabiam.
- **Filtros nas listas:**
  - Jogadores: perfil, classe, personalidade, zona;
  - Zonas: função, combate;
  - Missões: quadro, situação;
  - botão "Limpar" e contagem de resultados.
- **Jogadores:** coluna Personalidade; perfil no cabeçalho da ficha (saiu do subtítulo).
- **Ficha do jogador:** Missões na 2ª linha e Progresso e objetivos na 3ª, para os cards da 2ª linha não esticarem.
- **Missões:** lista "Quem concluiu" na Visão geral (10 mais recentes) e aba "Concluíram" com a tabela completa (perfil, classe, nível atual, entregas, última entrega, situação). O snapshot passa a levar o dia da última entrega de cada missão por personagem.

## D-036 — Atributos vivos do jogador, talento e objetivos pessoais
**Data:** 04/11/2026 · **Estado:** aceita (calibração a revisar)

**Pedido do criador:**
- Objetivos pessoais coerentes com o perfil e a personalidade, que influenciem sem ser regra fixa.
- Saber o que cada atributo faz, com dica na ficha.
- Todos os atributos devem agir de verdade.
- Os atributos devem subir e cair devagar, como nos jogos de gestão esportiva, até um potencial que depende de um traço de talento.
- Os atributos são do jogador (a pessoa), não do personagem.

**Decisões** (detalhe em `docs/atributos.md`):
- **Atributos agindo:**
  - Mecânica: XP em combate e percepção de risco.
  - Conhecimento: XP geral, peso do deslocamento e consistência das decisões.
  - Coordenação: convivência com lugares cheios.
  - Lealdade: fator "zona onde já estava".
  - Agressividade: fator "desafio".
  - Oportunismo: fuga de disputa.
  - Os que já agiam (gostos, sociabilidade, paciência, aversão a risco) continuam.
  - Trapaça segue sem efeito até o M9.
- **Atributos vivos:**
  - sobem com prática e experiência e caem com inatividade (Mecânica após 7 dias, Conhecimento após 14);
  - ritmo lento, com desaceleração perto do teto;
  - cada traço fica entre um piso e um teto; os valores de nascença (`BaseTraits`) ficam guardados;
  - mudança visível na escala 1–20 gera `trait_changed` com a causa.
- **Talento (novo traço da pessoa, fixo):**
  - definido por perfil em `population.json`;
  - faixas: Noob, Inexperiente, Padrão, Experiente, Prodígio, Gênio;
  - define o potencial e a velocidade de aprendizado só das capacidades;
  - sorteado por último no gerador, para não mudar os traços já existentes das populações.
- **Objetivos pessoais** (chegar a um nível, descobrir uma zona, concluir uma missão, praticar a atividade preferida):
  - escolhidos ao conectar, com peso pelos traços;
  - influenciam pelo fator "objetivo pessoal", proporcional ao comprometimento;
  - podem ser abandonados (falta de progresso ou impaciência);
  - concluídos, reforçam o motivo.
- **Recalibração das missões:** os atributos vivos puxam a população para a caça ao longo da campanha (quem gosta reforça o gosto; a confiança diante do risco cresce). Para manter o equilíbrio do D-031, os pesos das missões subiram: oferta 150 → 210, entrega 160 → 220, etapa 220 → 250. A decisão foi medida desligando cada fator isoladamente.
- **Determinismo e hash:** tudo determinístico, em inteiros e com causas; `StateHasher` passou para v5.
- **Testes:** 12 novos.

**Validação** (90 dias × 1.000 pessoas × 5 seeds): 15 de 21 metas dentro da faixa, como no D-031.
- Combate entrou na faixa: 35,7%.
- Horas até o nível 30: 192 → 171, na borda da faixa (170–230). Isso é efeito do crescimento da Mecânica e do Conhecimento; entra na recalibração da curva, que depende da escolha do criador (a ou b, D-028).

**Combate (M3):** os atributos do jogador entram junto com os do personagem. Dois personagens iguais se saem diferente conforme a Mecânica e o gosto ou experiência em PvP de quem joga. O gancho `OnPvpKill` já existe.

**Interface:**
- página do jogador com "Atributos do jogador";
- seta de evolução desde o início;
- barra com o potencial das capacidades;
- dica de cada atributo: o que influencia e o que o faz mudar;
- talento no cabeçalho;
- objetivos reais em "Progresso e objetivos";
- eventos novos no histórico;
- tiramos "Sessão até" do cabeçalho.

**Ajustes de interface no D-036:**
- **Dicas:**
  - o fio dourado do topo pintava a borda inteira de laranja; agora a borda é neutra (o fundo vem do `TooltipPanel` do tema);
  - título de 14 px, valores de 13 px, notas legíveis com quebra de linha.
- **Dicas que piscavam:**
  - as páginas se reconstruíam a cada segundo e recriavam o elemento sob o mouse;
  - agora a página só se atualiza quando a simulação avança (fora do mapa o tempo está pausado) ou quando chega o detalhe do jogador, e nunca com o mouse parado;
  - o painel do mapa também não se refaz enquanto o mouse está parado sobre ele.
- **Engajamento** de conta nova: janela mínima de 2 dias, para não estourar (antes passava de 500%).

## D-036a — Revisão dos atributos: talento × experiência, relações claras e parâmetros no conteúdo
**Data:** 04/11/2026 · **Estado:** aceita (calibração a revisar)

**Pedido do criador** (revisão do D-036 antes de consolidar): cada atributo deve representar uma coisa clara, para a simulação não produzir diferenças por relações que não fazem sentido.

**Decisões** (detalhe em `docs/atributos.md`):
1. **Talento × experiência:**
   - As faixas de talento agora são Baixo, Moderado, Alto e Excepcional.
   - "Noob" e "Experiente" descreviam experiência. A **experiência** passa a vir da prática (horas jogadas: Novato, Iniciante, Experiente, Veterano) e aparece na ficha.
   - **Potencial absoluto (escolha consciente):** `max(início + 50, 250 + talento × 0,7)`. Não depende de onde a pessoa começou. Hardcore e competitivos começam perto do potencial; casuais talentosos têm espaço para crescer.
2. **Gosto × execução:**
   - Gostos decidem a vontade; Mecânica, Conhecimento e Coordenação decidem a execução.
   - Abater um jogador (M3) reforça o gosto por PvP e conta como prática de combate (Mecânica). Não muda mais a Agressividade.
3. **Relações forçadas corrigidas:**
   - **Zona familiar:** antes era a Lealdade preferindo a zona anterior. Agora vem dos minutos que a pessoa já passou na zona (novo `ZoneMinutes` no personagem, no hash). É menor para quem gosta de variedade (Exploração). A Lealdade fica no apego às metas, na patrulha, na classe e nas guildas (M6).
   - **Coordenação:** não evolui mais na taverna; só em atividades cooperativas (grupos e raides, M6).
   - **Desafio acima do nível:** combina competitividade (Agressividade, 40%), confiança (Mecânica, 30%) e pouca aversão a risco (30%).
   - **Conhecimento e viagem:** o custo da viagem é o tempo real para todos. O Conhecimento só torna a **estimativa** mais precisa (erro de até ±30% com conhecimento zero, sorteado de forma determinística).
4. **Unidades e ritmo:**
   - 1 ponto visível ≈ 52,6 unidades do Core.
   - Mecânica ≈ 130 h de combate por ponto com talento moderado, longe do potencial.
   - **Repetição não reduz mais o gosto:** gera tédio na decisão (fator de repetição). Só experiências persistentes mudam uma preferência. A causa `repetition_fatigue` saiu.
   - O relatório de validação ganhou a seção "Evolução dos atributos do jogador": variação medida por perfil, metas concluídas e abandonadas, e tabela de inatividade.
5. **Metas de curto prazo:**
   - Os objetivos atuais passam a se chamar metas de curto prazo (sessão ou secundárias). Ambições de longo prazo virão em etapas, separadas.
   - Até 2 metas para todos, com chance fixa de 45% ao conectar. A paciência só pesa na persistência (tempo até desistir), não na quantidade.
6. **Parâmetros no conteúdo:**
   - `TraitTuning` e `GoalTuning` saíram do código. Os números estão em `population.json` (`dynamics` e `goals`), com faixas no validador.
   - Os pesos de zona familiar e desafio também foram para lá.

**Determinismo e testes:**
- `StateHasher` v6 (tempo por zona).
- 8 testes novos (132 no total): potencial absoluto, repetição sem queda de gosto, coordenação fora da taverna, familiaridade, apetite por desafio, estimativa de viagem, quantidade de metas e parâmetros no conteúdo.

**Validação** (90 dias × 1.000 pessoas × 5 seeds): 15 de 21 metas dentro da faixa, como no D-036.
- Combate: 35,3%, na borda da faixa (instável).
- Horas até o nível 30: 174, dentro.
- Evolução medida em 90 dias:
  - Mecânica: +0,05 nos perfis que quase não combatem; +0,6 nos competitivos e +0,9 nos hardcore.
  - Conhecimento: ≈ +1 em todos os perfis.
  - Gostos: até +0,6.

**Interface:**
- talento com as faixas novas;
- fato "Experiência" no cabeçalho;
- dicas dos atributos reescritas;
- "Progresso e metas";
- fatores "Zona familiar" e "Gosto por desafio" com a explicação nova.

## D-037 — Plano da sessão, continuidade e tédio contextual (fechamento do M2, parte 1)
**Data:** 04/11/2026 · **Estado:** aceita (calibração a revisar)

**Pedido:** fechar o M2 antes da Etapa 1, a começar pelo plano da sessão das diretrizes do D-026.

**Decisões** (parâmetros em `population.json`, seção `session`):
- **Três camadas separadas:** meta pessoal (D-036) → plano da sessão → atividade executada.
- **Plano da sessão:** formado na primeira decisão da sessão, com o fator que mais pesou como motivo (evento `session_plan`).
- **Continuidade:**
  - ao fim de cada ciclo, a pessoa continua no foco do plano, a menos que outra opção seja melhor por uma margem: 60 pontos, ou 150 durante o compromisso mínimo;
  - o compromisso mínimo dura 30 min + até 60 min pela paciência;
  - a viagem já feita não conta: não há custo afundado.
- **Revisão por acontecimentos:** etapa ou missão concluída, nível novo, meta concluída ou abandonada e zona descoberta liberam a próxima decisão sem margem.
- **Toda troca registra o motivo** (`plan_changed`, 1ª causa).
- **Tédio contextual no lugar da penalidade universal de repetição:**
  - cresce por tipo de atividade com os minutos;
  - cresce mais rápido para quem gosta de variedade (Exploração) e mais devagar para quem gosta muito daquele tipo;
  - alivia fazendo outra coisa e se dissipa entre sessões (sobram 30%);
  - a paciência reduz o peso dele na decisão.
- **Explorar sem nada novo ao alcance perde metade do gosto** (fator "nada novo"). Quem já conhece tudo ao redor ainda pode explorar pelo XP, mas sem a graça de descobrir.

**Linha de base × resultado** (90 dias × 1.000 pessoas × 5 seeds):

| Medida | Antes (D-036a) | Depois |
|---|---:|---:|
| Continuou na mesma atividade | 49,7% | 70,5% |
| Trocas de atividade por hora | 0,7 | 0,5 |
| Ciclos seguidos na mesma atividade | 1,6 | 2,3 |
| Tempo andando ou esperando | 31,2% | 24,7% |
| Tempo em combate | 35,3% (instável) | 40,4% |
| Tempo explorando | 12,9% | 6,1% |
| Horas até o nível 30 | 174 | 164 |
| Metas dentro da faixa | 15/21 | 15/21 |

- **Motivos das trocas:** acontecimentos (etapa e missão ≈ 48%, nível ≈ 11%), tédio ≈ 14%, falta de tempo ≈ 8% e foco indisponível ≈ 8%.
- **Horas até o 30 abaixo da faixa:** com menos tempo andando, todos sobem mais rápido. A recalibração da curva (escolha a ou b do D-028) fica para depois do combate, porque o XP de caça vai mudar com abates reais.

**Testes:** 8 novos (tédio, variedade, gosto, paciência, motivo da troca, margem e revisão por acontecimentos, explorar sem novidade, determinismo e hash, validação do conteúdo). `StateHasher` v7.

## D-038 — Experiência, memória e humor (fechamento do M2, parte 2)
**Data:** 04/11/2026 · **Estado:** aceita (calibração a revisar)

**Pedido:** satisfação, frustração e memória de experiências antes da Etapa 1 (plano 4.2 "Estado" e 4.5 "Memória").

**Decisões** (parâmetros em `population.json`, seções `mood` e `memory`):
- **Experiência de cada atividade** (componentes explicáveis, em pontos):
  - gosto pelo tipo;
  - encaixe de nível;
  - lotação, aliviada pela Coordenação;
  - companhia;
  - tédio;
  - risco: emociona quem tem pouca aversão e incomoda quem tem muita.
- **Nota da sessão** (0–1000, neutro 500): média dos componentes, progresso contra a expectativa, novidade e acontecimentos.
  - Conquistas: nível, missão, meta, zona descoberta.
  - Reveses: meta abandonada, ficar sem nada para fazer.
- **Pesos pessoais** (plano 4.4: "os pesos pertencem ao perfil individual"):
  - o progresso pesa pelo quanto a pessoa liga para evoluir (PvE 50%, competitividade 30%, PvP 20%);
  - a novidade pesa pelo quanto ela precisa de coisas novas (Exploração e impaciência).
  - Sem isso, quem joga para socializar ou negociar ficava 80% insatisfeito por falta de XP, o que não faz sentido.
- **Expectativa de progresso** (D-026):
  - começa pela competitividade;
  - aproxima-se do progresso real aos poucos;
  - tem piso: progredir mal por dias não vira "satisfatório".
  - A referência é a caça no próprio nível. No teto de nível o termo é neutro e a novidade decide (conteúdo esgotado).
- **Humor:**
  - **satisfação:** média móvel das notas, com sessões curtas pesando menos;
  - **frustração:** sobe com reveses e sessões ruins, cai com sessões boas e com o tempo;
  - **novidade:** gasta 20 por hora de jogo e é renovada por descobertas, atividades novas, níveis e missões;
  - **engajamento e intenção de continuar:** derivam dos três; a Lealdade e o investimento (nível) seguram quem está insatisfeito;
  - **motivos acumulados:** o que agrada ou incomoda, esmaecendo com o tempo; explicam a satisfação na ficha.
- **Memória:**
  - **impressão de cada lugar** (zona/atividade): aprende com cada experiência e vira o fator "lembrança" na decisão (até ±150). Más impressões esmaecem mais rápido que as boas.
  - **acontecimentos marcantes:** até 12 por pessoa (níveis múltiplos de 5, missão principal, meta concluída ou abandonada, zona descoberta, atividade ou sessão muito boa ou muito ruim). Conquistas duram mais que frustrações; o mesmo fato se reforça; com a memória cheia, sai o mais fraco.
- **Eventos:** `session_great` e `session_bad`, com os três componentes que mais pesaram.

**Validação** (90 dias × 1.000 pessoas × 5 seeds): 15 de 21 metas, igual ao D-037; o humor ainda não muda o comportamento.

| Perfil | Satisfação | Insatisfeitos | Novidade no fim |
|---|---:|---:|---:|
| Casual | 625 | 0% | 863 |
| Explorador | 597 | 1% | 474 |
| Social | 515 | 2% | 146 |
| Comerciante | 504 | 2% | 224 |
| Competitivo | 489 | 3% | 69 |
| Hardcore | 481 | 0% | 1 |

- Hardcore e competitivos esgotam a novidade (nível 30 e mundo conhecido): é o sinal de que o servidor precisa de atualizações (M7).
- 13% das sessões são ótimas e 0,1% são ruins.

**Testes:** 9 novos (componentes, pesos pessoais, limite e reforço da memória, esmaecimento, impressão na decisão, sessão boa e ruim, piso da expectativa, determinismo e hash, validação). `StateHasher` v8.

## D-039 — Abandono, retorno e sessões flexíveis; M2 fechado
**Data:** 04/11/2026 · **Estado:** aceita (calibração a revisar com o criador)

**Pedido:** fechar o M2 com abandono e retorno, antes da Etapa 1.

**Decisões** (parâmetros em `population.json`, seção `retention`; resumo em `docs/comportamento.md`):
- **O humor muda a rotina:** a chance de jogar no dia é multiplicada por um fator do engajamento (0,15–1,3). O neutro é o engajamento de quem acabou de entrar.
- **Sessões flexíveis** (plano 4.3):
  - sessão indo bem e pessoa engajada estende uma vez (+30%);
  - frustração alta com mais uma experiência ruim pode encerrar a sessão na hora.
- **Inativo:** 14 dias sem jogar. Joga com chance reduzida, conforme a intenção de continuar.
- **Desistente:**
  - intenção de continuar abaixo de 420 (chance diária proporcional) ou 45 dias sem jogar;
  - o evento `player_quit` leva o que mais incomodava como causa (novidade, tédio, progresso…).
- **Retorno:**
  - o inativo que volta a jogar registra `player_returned`;
  - o desistente pode voltar depois de 30 dias, pela saudade (memórias boas, que esmaecem) e pela lealdade, com recomeço: satisfação mínima, frustração zerada e parte da novidade;
  - atualizações (M7) e convites (M6) serão os gatilhos principais.
- **Ajustes de modelo durante a calibração:**
  - a novidade pode ficar negativa (saturação), porque sem atualizações o jogo cansa;
  - a intenção de continuar parte da satisfação, e não mais do engajamento, que quase não variava.
- **Metas novas** (hipóteses do Claude em `targets.json`): desistentes 8–30%, inativos 2–15%, retorno 20–70%.

**Validação** (90 dias × 1.000 pessoas × 5 seeds): 14 de 24 metas.
- **Desistentes:** 2,5%, abaixo da faixa.
  - Desistem 12% dos hardcore e 5,5% dos competitivos, no dia mediano 78 e no nível 30, por falta de novidade (73%) e tédio (18%).
  - Casuais, exploradores, sociais e comerciantes quase não desistem.
  - Motivo: hoje as únicas causas são o conteúdo esgotado e o tédio. Mortes (M3), economia (M5) e social (M6) trarão as outras, inclusive o abandono de casuais.
  - A faixa fica como está para o criador decidir: mudar a meta para caber no resultado esconderia o que falta.
- **Inativos:** 0,2% no fim (rotinas regulares).
- **Retorno:** 26,5% de quem saiu (instável).
- **Sessões por semana do hardcore:** 4,7, abaixo de 5. É o efeito de desanimar no fim do conteúdo: 98% dos hardcore estão insatisfeitos no dia 90, com a novidade esgotada. É o sinal de que o servidor precisa de atualizações.
- Sessões estendidas: 4,4%; encerradas por frustração: ~0%.

**Interface:**
- ficha do jogador com os cards Humor e Memórias marcantes, e o plano da sessão na Situação atual;
- o medidor do cabeçalho passou a se chamar "Ritmo de jogo", para não confundir com o engajamento do humor;
- situação Inativo e Desistiu no cabeçalho e nas listas (abas novas);
- eventos novos no histórico e no painel.

**Testes:** 8 novos (157 no total): fator de engajamento, inativo e retorno, desistência por inatividade e por intenção (com causa), retorno pela saudade, extensão de sessão, determinismo e validação. `StateHasher` v9.

**M2 fechado:**
- o critério do plano está cumprido: 100 pessoas entram e saem, escolhem entre o que os lugares conhecidos oferecem e mostram os motivos reais;
- também estão prontos o humor, a memória, o plano da sessão e a retenção.
- **Pendências registradas:**
  - teste de salvar e carregar (quando o save existir);
  - revisão do criador das metas de progressão e retenção;
  - recalibração da curva depois do combate.

## D-039a — Revisão do fechamento do M2: classes de meta, variedade, novidade em meses e saudade sem recomeço
**Data:** 04/11/2026 · **Estado:** aceita

**Pedido do criador** (revisão antes da Etapa 1):
1. Não perseguir as metas de abandono artificialmente.
2. Investigar os 98% de hardcore insatisfeitos.
3. A memória não deve fabricar saudade.
4. Guardar as 162 h como resultado provisório.
5. Nomear o "tempo em combate" corretamente.

Durante a revisão, o criador acrescentou: falta de conteúdo novo é questão de meses, não de dias, porque atualizações de MMORPG saem a cada 3 a 6 meses.

**Decisões:**
- **Classes de meta** (`kind` em `targets.json`), porque "x de y" sozinho não diz se dá para avançar:
  - **regra do cenário (7):** rotina e calendário, medidos numa rodada controlada com o humor neutralizado (`WorldConfig.NeutralMood`). Falhar indica erro. Na campanha completa, faltar por desânimo é resultado legítimo;
  - **provisória (12):** progressão, divisão do tempo, missões;
  - **dependente de sistema futuro (5):** tempo em missões e em coleta, desistentes, inativos, retorno. Só acompanhamento, não reprovam.
  - Invariantes de verdade (determinismo, propriedade, cidade segura) continuam como testes, não como faixas.
- **Traço novo: Variedade (preferência por variedade).** Pesa no tédio, na atração pela zona familiar e na necessidade de novidade.
  - A Exploração volta a ser só o gosto por explorar (o D-036a usava a Exploração no lugar da variedade).
  - Vem do perfil: casual 500, competitivo 450, social 550, comerciante 350, explorador 750, hardcore 300.
  - É sorteado por último, depois do talento, para não mudar os traços já gerados.
- **Novidade em horizonte de meses:**
  - gasta 2 pontos por hora de jogo, não mais 20, com as frações das sessões curtas acumuladas;
  - o termo tem limite (±300 antes do peso pessoal);
  - a necessidade de novidade é 10% base + Variedade 50% + Exploração 20% + impaciência 20%.
  - **Diagnóstico que motivou a mudança:** com o modelo do D-039, até os hardcore pacientes ficavam 100% insatisfeitos no dia 90. A novidade tirava cerca de 233 pontos deles e cerca de 246 dos impacientes; ou seja, dominava a satisfação de quem gosta de repetição.
- **Saudade sem recomeço fabricado:**
  - a volta por saudade só conta memórias boas registradas depois da última volta por saudade;
  - cada volta aumenta o intervalo mínimo e reduz a chance seguinte;
  - a pessoa volta com o humor que tem (saíram a satisfação mínima, a frustração zerada e o bônus de novidade), joga no dia da volta e tem 14 dias de carência antes de poder desistir de novo por intenção;
  - o dia da volta conta como presença para a inatividade (antes, quem voltava podia ficar inativo e desistir na mesma noite);
  - atualizações, convites e campanhas de marketing e streamers serão os outros motivos de retorno (M6/M7), sem a exigência de memórias novas.
- **Os 12 acontecimentos** ficam como memória resumida; estados importantes (nível, missões, metas, atributos, impressões e, no M6, vínculos) ficam separados.
- **162 h até o nível 30:** resultado de referência depois do D-037 (anda-se menos). Não será compensado agora; a recalibração rumo a cerca de 200 h vem depois do combate.
- **Combate abstrato:** a meta e o relatório agora dizem "atividades de combate (abstratas, sem luta simulada até o M3)".

**Validação** (90 dias × 1.000 pessoas × 5 seeds, mais as rodadas controladas):
- regras do cenário: 7 de 7; provisórias: 8 de 12 (curva até o 20 e o 30, casuais no nível 10, tempo andando); dependentes de sistemas futuros: acompanhamento;
- nenhum perfil insatisfeito no dia 90 (satisfação de 590 a 690); desistentes: 0,2%.

**180 dias** (3 seeds):
- a novidade dos hardcore vai de 772 (dia 90) a 213 (dia 180) e a satisfação, de 607 a 542; ainda nenhum insatisfeito;
- a falta de conteúdo passa a pesar de verdade entre o 7º e o 8º mês sem atualização;
- casuais e exploradores seguem com novidade alta.

**Testes:** 7 novos (164 no total): variedade separada da exploração, saturação limitada, novidade lenta com sessões curtas, saudade sem reuso de memórias, carência, cenário controlado, classe das metas. `StateHasher` v9 (campos novos).

## D-040 — Personagem e combate inicial: estatísticas diretas e morte sem perda
**Data:** 04/11/2026 · **Estado:** aceita (para a Etapa 1 e o combate do M3)

**Decisões do criador:**
- **Atributos do personagem:** estatísticas diretas.
  - Vida, recurso de classe, ataque físico, ataque mágico, defesa física, defesa mágica e velocidade de movimento.
  - Alcance, custo, recarga e tempo de lançamento ficam nas habilidades.
  - O atual `intelligence` das classes é o ataque mágico do personagem, não um atributo do jogador, e será renomeado para não confundir.
  - Os atributos do jogador (Mecânica, Conhecimento, Coordenação) continuam da pessoa e entram na execução do combate.
- **Morte contra monstros (piloto):**
  - sem perda de XP nem de equipamento;
  - a pessoa reaparece na cidade vinculada depois de um intervalo configurável, recupera os recursos e precisa viajar de novo;
  - o custo real é o tempo perdido, a tentativa interrompida e a reação emocional de cada pessoa;
  - desgaste, reparos e outras penalidades entram depois;
  - full loot continua uma regra separada, aplicada nos contextos definidos (zonas, M9).

## D-041 — Decisões do criador para a pessoa (rotina, traits) e P0: pesos da decisão no conteúdo
**Data:** 04/11/2026 · **Estado:** aceita

**Decisões do criador** sobre o `docs/plano-pessoa.md`:
- **Quantidade de traits:** pode haver pessoa sem traits.
  - Duas é a quantidade mais comum, e é difícil ter mais de duas.
  - Proposta de tabela: 0 → 18%, 1 → 27%, 2 → 38%, 3 → 10%, 4 → 4,5%, 5 → 2%, 6 → 0,5% (cerca de 17% com mais de duas).
  - A "marcância da personalidade" desloca a tabela com efeito moderado, sem tornar comum ter mais de duas.
- **Traits** (pedido durante a P0):
  - as iniciais precisam combinar com os atributos, a personalidade e o perfil da pessoa, senão ficam desconexas;
  - o catálogo deve ser grande e criativo, com positivas e negativas;
  - algumas pessoas têm só negativas, por exemplo duas.
- **Localidades:** Brasil, Argentina, Estados Unidos e China.
- **Ocupações:** distribuição criativa e realista, a montar na P2.
- **Finanças:** o modelo completo fica para o M7, mas a **renda real** da pessoa já é criada na P2, sem efeito por enquanto.
- **Ordem:** P0 → P1 → P2 → P3 → P4, depois a Etapa 1. A reputação fica só na Etapa 3.

**P0 — pesos da decisão no conteúdo:**
- As constantes de `BehaviorTuning` (pesos da utilidade, missões, ruído, variação de sessão, tolerância de saída, velocidade a pé, duração das atividades, espera e dias de registro) foram para `population.json`, seção `behavior`, com validação. O arquivo do código foi removido.
- A velocidade a pé vai para a classe na Etapa 1 (D-040: velocidade de movimento).
- **Verificação:** eventos, população, personagens e o resumo diário ficaram idênticos antes e depois (30 dias × 100 pessoas e 20 dias × 1.000 pessoas).
  - Só a coluna `state_hash` muda, porque o hash de estado inclui o hash do conteúdo (`Content.SourceHash`) e o `population.json` mudou.
  - Esse é o método para checar mudanças que não deveriam alterar a simulação: comparar os relatórios, não o hash.
- **Verificação completa** (pedido do criador): relatórios podem não cobrir todo o estado interno.
  - Criei `StateHasher.ComputeWithoutContentId`: o estado completo, tudo o que o hash cobre menos a identificação do conteúdo. O hash original continua igual.
  - O runner agora mostra os dois hashes, e o `summary.md` e o `metadata.json` registram ambos.
  - A P0 foi verificada de novo assim, com a versão anterior (`339c95e`) e a da P0 (`06c8aa6`) com o mesmo cálculo: estados idênticos nas duas execuções (`1ec28d15…` com 30 dias × 100 pessoas; `94b45f09…` com 20 dias × 1.000 pessoas). Os hashes originais diferem, como devem.
  - Para mudanças desse tipo, compare o hash sem a identificação do conteúdo.
- **Testes:** 164 passam.

## D-042 — P1: localidade, fuso e calendário da residência
**Data:** 04/11/2026 · **Estado:** aceita

**Decisões:**
- **Conteúdo novo, `localities.json`:**
  - Brasil (UTC−3, 76%), Argentina (UTC−3, 9%), Estados Unidos (Leste, UTC−5, 8%) e China (UTC+8, 7%), em pesos configuráveis;
  - cada localidade tem idiomas com a parcela de falantes, parcela de residentes estrangeiros, feriados e nomes próprios.
  - **Simplificações registradas:** sem horário de verão; feriados móveis só com as datas de 2027; feriados lunares da China a completar pelo editor.
- **O servidor tem localidade** (`world.json`: `serverLocalityId` = brasil). Os feriados saíram do `world.json` e passaram para a localidade. O relógio do jogo e o resumo diário usam o fuso e o calendário do servidor.
- **Pessoa:**
  - residência pelo peso;
  - nacionalidade igual à residência, salvo os residentes estrangeiros (ex.: 12% nos EUA);
  - idiomas: o nativo da nacionalidade mais os da residência, pela parcela de falantes;
  - diferença para o horário do servidor;
  - nome da nacionalidade quando ela tem os seus (os brasileiros mantêm os nomes gerais).
  - Tudo é sorteado depois dos traços (e o nome num fluxo próprio), para não mudar as populações já geradas.
  - A nacionalidade não decide horário, renda nem personalidade.
- **Rotina no horário local:**
  - o planejamento diário roda à meia-noite local de cada localidade (um evento por localidade, com o dia local);
  - o horário preferido é local;
  - o tipo do dia (útil, fim de semana, feriado) vem do calendário da residência;
  - inatividade, humor e retorno contam pelo dia do servidor.
- **Validação:**
  - o horário de início e o dia útil são medidos no fuso de cada pessoa;
  - seção nova "Localidades" no relatório, com pessoas, estrangeiros, sessões e início médio local e no servidor.
- **Interface:**
  - card "Onde mora" na aba Rotina (residência, fuso, nacionalidade, idiomas, hora local e diferença do servidor);
  - gráfico "Horários de jogo" em hora local;
  - coluna e filtro "País" na lista de jogadores.

**Validação** (90 dias × 1.000 pessoas × 5 seeds):
- regras do cenário: 7 de 7; provisórias: 8 de 12, como antes;
- início médio (média circular): Brasil 18h47; Argentina 18h43; China 18h48 local, que é 7h48 no servidor; EUA 18h56 local, que é 20h56 no servidor;
- **correção depois da revisão do criador:** a primeira versão do relatório usava a média aritmética das horas e mostrava "18h40 local = 7h51 no servidor" para a China, com 11h11 de diferença em vez de 11h.
  - Os instantes estavam certos (o teste confere cada sessão: local = servidor + 11 h); o erro era só da média.
  - Uma sessão à 0h30 local conta como 0,5 no fuso local e 13,5 no servidor, e isso distorce a média aritmética.
  - Agora o início médio é uma média circular (a meia-noite não distorce): local e servidor diferem exatamente pelo fuso, com teste.
- o pico do servidor agora se espalha pelos fusos.

**Testes:** 7 novos (171 no total): localidades do conteúdo, sorteio de residência, nacionalidade, idiomas e nome, feriados por residência, chineses jogando na noite local (manhã do servidor), planejamento na meia-noite local, validação, determinismo e hash.

## D-043 — P2: obrigações, tempo livre, vontade de jogar e renda real
**Data:** 04/11/2026 · **Estado:** aceita (calibração a revisar)

**Decisões:**
- **Conteúdo novo, `occupations.json`:** 13 ocupações, com pesos para o público de MMORPG.

  | Grupo | Ocupações (peso) |
  |---|---|
  | Estudo | estudante de manhã (12), estudante à tarde (5), universitário diurno (10) |
  | Trabalho | horário comercial (24), home office em tecnologia (8), escala 6x1 do comércio (7), meio período (6), autônomo (5), turno noturno (4) |
  | Trabalho e estudo | trabalha de dia e estuda à noite (6) |
  | Sem trabalho fixo | sem ocupação (8), cuidando da casa e da família (3), aposentado (2) |

  - Cada ocupação tem blocos de obrigação (dias, horário que pode cruzar a meia-noite, deslocamento, folga pessoal da escala), variação individual de horário e se trabalha em feriado.
  - Cada uma tem também a faixa de renda e a chance e a faixa de apoio familiar.
  - A ocupação é sorteada independente do perfil: ter tempo não é ser hardcore.
- **Pessoa:**
  - "dias por semana" e "reforço de fim de semana" deram lugar à **importância do jogo** (o que define o hardcore) e ao **hábito** (regularidade), nas mesmas posições do sorteio. Talento, variedade e localidade de ninguém mudaram.
  - Depois da localidade vêm ocupação, variação de horário e deslocamento, folga da escala, necessidade de sono, hora de dormir, renda e apoio.
  - A **renda real** é a faixa da ocupação × fator de renda da localidade (em reais equivalentes; EUA 4,2×). Fica sem efeito até o M7.
- **Dia local montado** (`DailySchedule`, lógica pura):
  - obrigações com deslocamento;
  - quem trabalhou à noite dorme de manhã;
  - os demais acordam a tempo da primeira obrigação e dormem a tempo da do dia seguinte (o déficit de sono fica para a P3);
  - 45 min de preparo depois de acordar e 45 min ao chegar de cada obrigação;
  - o resto são janelas livres.
- **Vontade de jogar** (`population.json`, seção `routine`):
  - **chance do dia:** a importância do jogo é a base. O tempo livre aumenta a chance de quem dá pouca importância ao jogo. A carga de obrigações reduz a chance, mas a dedicação supera o cansaço. O hábito soma ou tira conforme jogou ontem, e o humor (D-039) ajusta.
  - **sessão:** vai para a janela que serve ao horário preferido, começando mais cedo se a sessão desejada não couber. Em dia livre dura mais, conforme a importância.
  - **segunda sessão:** possível em outra janela, para quem dá muita importância ao jogo.
  - **registro e retorno:** caem na próxima janela livre.
  - **extensão (D-039):** só até o fim da janela, nunca invade obrigações.
- **Agenda do dia:** fica como estado, sem evento (14 dias): tempo livre, janelas, sessões e motivo ("jogou", "sem tempo livre", "tinha tempo, preferiu outro lazer", "sem vontade", "inativo", "voltou").
- Substitui "dias por semana", "reforço de fim de semana" e a hora preferida como horário fixo (agora é preferência dentro do tempo livre). É um sistema só.

**Validação** (90 dias × 1.000 pessoas × 5 seeds):
- regras do cenário: 7 de 7, na rodada controlada; provisórias: 9 de 12.
- dias úteis com 50,9% dos cadastrados ativos; fim de semana 1,3× maior; casual 2,5 sessões e 3,4 h por semana; hardcore 6,6 sessões e 31,8 h.
- **Por ocupação:**

  | Ocupação | Sessões/semana | Horas/semana | Tempo livre em dia de obrigação | Dias sem tempo |
  |---|---:|---:|---:|---:|
  | Trabalha e estuda | 1,7 | 5,2 | 1,1 h | 56% |
  | Horário comercial | 3,8 | 10,2 | 4,3 h | 0% |
  | Estudante (manhã) | 4,5 | 11,4 | 8,6 h | 0% |
  | Sem ocupação | 5,5 | 17,1 | (todo dia livre) | 0% |

  Entre quem não tem ocupação, quem dá pouca importância ao jogo joga menos dias que o trabalhador que prioriza o jogo (teste).
- **Agenda:** 56% dos dias com jogo; 37% "tinha tempo, preferiu outro lazer"; 3% sem tempo livre.
- **Regra do cenário reescrita:** o trabalhador comercial joga depois do expediente e antes de dormir (≥ 90% dos inícios de dia útil entre 18h e 23h, nenhum no meio do expediente).
  - O teste antigo de 19h–23h comparava com uma conta exata do início, que supunha hora preferida ± variação. Agora o início se ajusta à janela livre.

**Interface:**
- aba Rotina com os cards Onde mora, Vontade de jogar (importância, hábito, horário, sessão típica, previsto por semana) e Finanças (renda e apoio);
- semana típica (obrigações e horas livres por dia, com dica do acordar, dormir e janelas) e últimos dias (o motivo de cada um);
- "Ritmo de jogo" agora compara com o previsto pela rotina nova;
- coluna e filtro "Ocupação" na lista de jogadores.

**Testes:** 11 novos (184 no total):
- montagem do dia: comercial, estudante, turno noturno, trabalho e estudo, feriado, escala e folga;
- encaixe da sessão;
- ninguém começa a sessão durante uma obrigação;
- tempo livre não é jogar;
- renda por ocupação e país, com apoio aos estudantes;
- validação das ocupações;
- determinismo.

`StateHasher` v10.

## D-044 — P3: sono, dificuldade de parar e jogar no horário de trabalho remoto
**Data:** 04/11/2026 · **Estado:** aceita (calibração a revisar)

**Contexto:** o criador aprovou a P2 e pediu a P3 com cinco coisas separadas: importância do jogo, tendência a prolongar, autocontrole, sono (necessidade, déficit e recuperação) e cansaço. A pessoa pode jogar além do planejado e dormir menos, mas sem horas ilimitadas. Pediu também que quem trabalha em casa às vezes jogue durante o trabalho, sem transformar o home office em tempo livre.

**Decisões:**
- **Traços novos (sorteados por perfil, independentes dentro dele):** tendência a prolongar e autocontrole, além da tolerância ao déficit de sono. Entram no fim do sorteio da rotina; nada sorteado antes muda.
- **Extensão da sessão** (substitui a do D-039):
  - motivo explícito: "gostando da sessão" (nota parcial ≥ 700) ou "perto de concluir a meta" (70% do caminho);
  - chance = 40% × tendência × (1 − autocontrole × 0,8) × (1 − cansaço); tamanho = 30% da sessão × (0,5 + tendência), mínimo 10 min;
  - limites: 2 vezes por sessão, 180 min no total; não vale na pausa nem na escapada do trabalho nem no cenário controlado.
- **Sono cortado:** a extensão usa primeiro o resto da janela livre e só avança sobre o sono quando a janela termina na hora de dormir. Nunca antes de uma obrigação nem antes de um turno da noite: esse caso apareceu no teste e foi corrigido.
  - Três tetos por noite: 180 min, o teto do déficit e 4 h de sono mínimo.
  - O que foi cortado entra no déficit na saída (evento `stayed_up_late`, com o motivo e os minutos).
- **Déficit e recuperação:**
  - noite mais curta que a necessidade (pela rotina ou pelo jogo) soma ao déficit;
  - manhã livre dorme até 2 h a mais; noite normal recupera 20 min;
  - teto do déficit = 15 h × (0,5 + tolerância).
- **Cansaço** = déficit ÷ 10 h, até 100%:
  - reduz a chance de jogar (até −40%) e a sessão desejada (até −30%);
  - pesa na nota da sessão (motivo "Cansaço");
  - acima de 50%, o dia sem jogo fica com o motivo "Cansado (dormiu pouco)".
- **Trabalho remoto** (`occupations.json`: `remote` e `breaks` por bloco, `playDuringWorkFlexibility` por ocupação; home office 600, autônomo 700, demais 0):
  - **pausa:** jogar no intervalo declarado. A chance vem da importância do jogo; a sessão é curta e cabe na pausa;
  - **escapada:** jogar no expediente; chance = 40% × flexibilidade × importância × (1 − autocontrole) × folga do dia (carga sorteada a cada dia). Cerca de 25 min, longe do começo e do fim do expediente; nunca em cima da pausa;
  - ambas são **interrompíveis**: a decisão desconta o custo de largar cada atividade (`interruptCost`: chefe 1000, emboscada 850, patrulha 700, caça 550 … comércio e conversa 0);
  - **chamado do trabalho** (35% das escapadas): termina a atividade se falta pouco (regra de saída) ou larga e sai (`work_interruption`);
  - a agenda guarda as sessões no trabalho e os minutos sobre o expediente; o início da sessão traz o motivo (`work_break` ou `during_work`);
  - nenhuma consequência profissional por enquanto. Deslocamento, trabalho presencial e estudo ficam protegidos.
- **Validador:** flexibilidade só com bloco remoto; pausa só em bloco remoto e dentro dele; faixas da seção `routine` e custos de interrupção.
- **Calibração:** a penalidade do dia cheio de obrigações passou de 600 para 650.
  - Motivo: as sessões no almoço aumentam os ativos em dia útil, e o reforço do fim de semana tinha caído para 1,28–1,34 (meta 1,3–2,0).
  - Tentativa descartada: subir o bônus de dia livre (350 → 400) quase não mudou a razão (1,29–1,35), levou os ativos em dia útil a 53,7% e derrubou uma meta provisória.

**Validação** (90 dias × 1.000 pessoas × 5 seeds):
- **Metas:**
  - regras do cenário: 7 de 7;
  - provisórias: 9 de 12;
  - dependentes de sistemas futuros: 2 de 5 (acompanhamento).
- **Rotina no cenário controlado:**
  - casual: 2,5 sessões e 3,4 h por semana; hardcore: 6,7 sessões e 31,6 h;
  - dia útil: 50,8% dos cadastrados ativos;
  - fim de semana: 1,31–1,36×, ainda perto do piso da faixa.
- **Extensões** (média por seed, na janela):
  - 1.548 no total: 716 gostando da sessão e 832 perto de concluir a meta;
  - 746 avançaram sobre a hora de dormir;
  - 759 noites encurtadas, 80 min a menos em média.
- **Sono cortado por semana**, por pessoa: hardcore 32 min, competitivo 9 min, casual e explorador quase nada.
- **Sono pela rotina:** quem trabalha e estuda dorme 7,0 h em dia útil e 9,6 h em dia livre (recupera), com necessidade de 7,8 h. Termina com 3,1 h de déficit e 28% cansados. As demais ocupações ficam perto de zero.
- **Trabalho remoto:**
  - pausa: 1.040 sessões (2,1% de todas);
  - escapada: 202 sessões (0,4%), com 71 chamados do trabalho (30 terminaram a atividade, 41 largaram e saíram);
  - home office: 0,7 pausa e 0,1 escapada por semana; escapada em 2,5% dos dias de trabalho.

**Interface — aba Rotina refeita com o criador** (`PlayerModal.Routine.cs`, gráficos em `RoutineCharts.cs`):
- **Linha 1:**
  - **Semana típica:** barras agrupadas por dia, com o tempo livre em azul (verde em dia livre) e o jogo previsto em dourado. O previsto é a chance do dia × a sessão que cabe na janela, a mesma conta do "previsto por semana". Embaixo de cada dia, a chance de jogar.
  - **Ocupação, ao lado:** etiquetas com ícone (remoto ou presencial, deslocamento por trecho, feriado, pausa, escapada) e um cartão **por dia da semana**, com as obrigações e horários desta pessoa, o tempo livre e a maior janela. Fecha com a semana em 168 horas.
- **Linha 2:**
  - **Últimos 30 dias:** gráfico de linha, com o tempo livre como área azul e o tempo jogado como linha dourada. O dia sem jogo fica no zero, na cor do motivo. Faixas verdes marcam os dias livres e um losango marca o jogo no trabalho.
  - **Sono e cansaço:** anel de cansaço, déficit e teto, e o sono de cada noite contra a necessidade.
  - **Como terminaram os dias:** rosca dos motivos, com "jogou só no horário de trabalho" à parte, para as contagens baterem entre os cards. Inclui a sequência atual.
- **Linha 3:**
  - **Vontade de jogar:** os quatro traços em duas colunas, previsto, sessão, horário preferido, uso do tempo livre e uma frase de leitura do perfil.
  - **Onde mora** e **Finanças** (rosca de renda e apoio).
- **Descartado com o criador:**
  - agenda em faixas de 24 h;
  - colunas empilhadas com trilho vazio;
  - horários agrupados ("seg–sex"), errado porque cada dia pode ter horário próprio.
- **Agenda guardada:** passou de 14 para 30 dias (`routine.planHistoryDays`, conteúdo). O estado fica maior; as decisões não mudam.
- **Histórico e painel lateral** descrevem:
  - pausa e escapada;
  - extensão com sono cortado e "foi dormir mais tarde";
  - chamado do trabalho.
- **Fatores novos:** "Cansaço" e "Sessão interrompível", com explicação.

**Para depois (pedido do criador):** eventos dentro do jogo e atualizações grandes ou marcantes poderão influenciar a tendência a prolongar e a volta de quem saiu. Entram com os eventos do jogo e as atualizações (M7), junto dos outros motivos de retorno.

**Testes:** 10 novos (194 no total):
- recuperação em manhã livre;
- déficit de quem trabalha e estuda;
- limites da extensão (nunca invade obrigação, déficit dentro do teto);
- cenário controlado sem extensão;
- cansado joga menos e o motivo fica na agenda;
- traços separados (correlação dentro do perfil ≈ 0);
- pausa e escapada só para quem pode e não todo dia;
- sessão interrompível evita atividade cara de largar;
- pausas inválidas são erro;
- determinismo.

O teste "ninguém começa a sessão durante uma obrigação" agora aceita sobreposição só em bloco remoto: dentro da pausa declarada, ou como escapada de ocupação com flexibilidade. O de extensão do D-039 aceita os dois motivos.

`StateHasher` v11.

## D-045 — Coerência dos horários da pessoa (turno, sono e hora preferida)
**Data:** 04/11/2026 · **Estado:** aceita

**Contexto:** o criador encontrou uma pessoa de meio período que trabalhava das 04:55 às 08:55, dormia às 19:49 nos dias úteis, preferia jogar às 22h e aparecia como "deita 23:30". Cada número vinha de um sorteio independente, e nada conferia se a história fechava.

**Causas:**
- **Variação de horário grande demais:** o meio período usava uma variação de ±4 h para representar "manhã ou tarde", e por isso havia gente saindo de casa de madrugada.
- **Hora preferida sem relação com a rotina:** era sorteada pelo perfil e nunca confrontada com o tempo livre.
- **Hora natural de dormir sem relação com o turno:** também sorteada à parte, ignorando quem acorda cedo.

**Decisões:**
- **Turnos reais no conteúdo:** o meio período virou duas ocupações, manhã (08:00–12:00) e tarde (13:30–17:30), com variação de ±30 min, como já era com os estudantes.
- **Validador:** recusa uma ocupação diurna em que a variação e o deslocamento mais longo fariam alguém acordar antes das 04:30. O turno da noite, que cruza a meia-noite, fica de fora.
- **Sono que se acostuma às obrigações:** a hora natural de dormir fica no máximo 1 h depois da hora típica (mediana) das noites antes de obrigação. O parâmetro é `routine.bedtimeAdaptMinutes`. O turno da noite segue o próprio ritmo.
- **Hora preferida que conversa com a rotina e com a vontade de jogar** (pedido do criador: "o viciado que sai às 18h já está jogando; o casual chega e só quer jogar às 21h, 22h, umas 2 horinhas"):
  - **Alvo de cada dia:** começa na janela livre mais próxima do gosto de horário sorteado pelo perfil.
    - A espera e o peso do gosto de horário crescem com o **quadrado** da falta de importância. Importância 15/20 começa quase na hora; só o casual espera de verdade.
    - O teto da espera é `routine.preferredDelayMaxMinutes` (240 min, para importância 0); quem espera mais também segue mais o próprio horário de lazer.
    - Caso do criador: um hardcore de meio período de manhã, livre às 13:25, preferia 16h; agora prefere 14h.
  - **A sessão típica cabe inteira até dormir:** a hora preferida nunca é "puxada para antes". Antes, 21h com uma sessão de 2h43 forçava o início às 20:47.
  - **Dias que definem o hábito:** os dias com obrigação. Neles, a sessão considerada é limitada à maior janela que esses dias têm: o hardcore joga a noite toda depois do trabalho. Uma brecha de 40 min antes do expediente não vira "hora preferida" se outro dia útil tem mais tempo. Sem tempo nos dias úteis, valem os dias livres.
  - **Hora final:** a hora cheia que funciona no maior número desses dias, pela mesma conta do planejador (`StartsAtPreferred`); em empate, a mais próxima do alvo.
  - **Exemplos:**

    | Quem | Chega em casa | Começa a jogar |
    |---|---|---|
    | Hardcore do horário comercial | 18:35 | ~19h |
    | Casual do horário comercial | 18:35 | ~21h, por ~1h10 |
    | Lyrus (meio período à tarde, competitiva, sessão de 2h43) | — | 20h |
- **Populações preservadas:** nada novo é sorteado; os ajustes só corrigem o que já foi sorteado. A troca de uma ocupação por duas muda quem cai em cada uma.
- **Duração coerente com o tempo livre:** quem prioriza o jogo joga mais em qualquer dia com muito tempo livre, não só no dia sem obrigações. Exemplo: meio período, ou estudante à tarde. A sessão cresce com importância × tempo livre do dia.
- **Mais jogo para todos em dia de muito tempo livre** (pedido do criador: o previsto estava baixo para todos os perfis, não só o hardcore). A sessão cresce com uma base para todos (`routine.freeTimeDurationBase`, 250) mais a parte da importância.
  - No cenário controlado, o casual foi de 3,4 para 4,1 h por semana e o hardcore de 31,6 para 37 h.
  - A meta do hardcore subiu de 15–35 para 15–45 h por semana, por direção do criador: quem tem muito tempo livre e prioriza o jogo chega a 35–45 h.
- **Hábito proporcional:** antes, quem não jogou ontem perdia pontos fixos de chance, o que cortava pela metade a chance de quem já tinha pouca. O casual que joga duas vezes por semana era "punido" por não ter jogado ontem. Agora o hábito multiplica a chance: +x% se jogou ontem, −x% se não jogou. Vale no planejador e no previsto.
- **Casual um pouco mais presente** (pedido do criador, "pra um casual tá meio baixo demais"): importância do jogo 300 → 350 e sessão base 70 → 80 min.
  - O casual da captura foi de 2,0 para 3,3 h por semana; nos dias úteis, de 8% para 20%.
  - No cenário controlado, o casual faz 2,9 sessões e 5,6 h por semana.
- **"Previsto" honesto:** usa a mesma conta do planejador, numa função só (`Expect`) que vale para a semana típica e para o total da semana.
  - Inclui o **hábito**: quem jogou ontem tende a jogar hoje (frequência de longo prazo da cadeia "jogou ontem?").
  - Inclui a **segunda sessão** de quem prioriza o jogo.
  - Fica de fora só o humor.
  - O hardcore do exemplo foi de 16,7 h para 27 h previstas por semana (33% do tempo livre). O real dos últimos 30 dias aparece ao lado (40%).
- **Interface:**
  - O card Vontade de jogar mostra o hábito pela mesma conta do planejador: previsto por semana, a sessão do dia útil (a real, não a base do perfil), e quando **costuma começar** e **costuma sair**.
  - O card Vontade de jogar mostra a **satisfação com o jogo** e o **engajamento**, com o efeito de agora na chance de jogar (ex.: "+16%"), e as barras "previsto" e "real (30 dias)" do uso do tempo livre.
  - As etiquetas de condição (presencial, deslocamento, feriado, pausa, escapada) ficam no topo do card de Ocupação, ao lado de "Trabalha".
  - O card de Ocupação diz o que a pessoa faz ("Trabalha", "Estuda", "Trabalha e estuda", "Cuida da casa e da família", "Aposentado", "Não trabalha nem estuda") e mostra turno, obrigações por semana, dias com obrigação e renda.
  - O card de sono mostra a hora real de deitar antes de dia de obrigação e antes de dia livre ("dorme de manhã" no turno da noite), em vez da preferência sorteada.

**Testes:** 8 novos.
- Turno diurno nunca exige acordar antes das 04:30.
- Hora preferida dentro do tempo livre na maioria dos dias com obrigação, e em todos eles para pelo menos 85% das pessoas.
- Sono adaptado às obrigações.
- Exemplo do meio período, sem madrugada e com a sessão típica cabendo depois da hora preferida.
- Quem prioriza o jogo começa até cerca de 1 h depois de chegar; o casual, mais de 1 h depois disso, e com sessões mais curtas.
- Hardcore de meio período de manhã joga logo depois do almoço.
- A sessão desejada cresce com o tempo livre para quem prioriza o jogo.
- A regra do trabalhador comercial passou a medir "começa depois de chegar do trabalho (horário dele) e antes das 23h30", no lugar das 18h fixas: quem entra às 7h chega às 17:30 e já joga.
- O validador recusa turno de madrugada.

**Regra para o que vier:** todo parâmetro de horário novo precisa passar por um teste que confira a coerência com o turno, o sono e o tempo livre.

## D-046 — Compromissos ocasionais nos dias livres
**Data:** 05/11/2026 · **Estado:** aceita

**Contexto:** pedido do criador. Às vezes, num fim de semana ou feriado, quem tem tempo livre ganha um compromisso (ir a uma festa, visitar a mãe). O compromisso em si não importa; importa que exista, com probabilidade bem baixa.

**Decisões:**
- **Quando:** só em dia livre de fim de semana, feriado ou folga.
- **Chance:** `routine.commitmentChancePermille` (70) por dia elegível, escalada pela sociabilidade, de 0,6× a 1,4×. Na média, cerca de 7% desses dias.
- **Tipos** (`routine.commitments`, conteúdo editável), cada um com faixa de início, duração e peso; os sociais pesam mais para quem é sociável:

  | Tipo | Início | Duração | Social |
  |---|---|---|---|
  | festa | 20:00–21:30 | 2h30–4h | sim |
  | visita à família | 11:00–15:00 | 2h30–5h | não |
  | passeio com amigos | 14:00–17:00 | 2–4h | sim |
  | resolver pendências | 09:00–12:00 | 1h30–3h | não |

- **Efeito no dia:** o compromisso fica dentro do período acordado e termina antes de dormir. Tira o tempo livre do dia, com a chegada em casa depois dele. Não conta como obrigação (não cansa como um dia de trabalho) e não muda o sono.
- **Determinismo:** o sorteio é próprio da pessoa e do dia, então o mesmo dia sempre dá o mesmo resultado, não importa quando a agenda é consultada. A agenda guarda o compromisso (`StateHasher` v12).
- **Interface:** ponto roxo no alto do gráfico dos últimos 30 dias, com o compromisso e o horário na leitura do dia.

**Calibração:** a proporção de ativos no fim de semana ÷ dia útil ficou no limite, com 1,299 numa seed contra o mínimo de 1,3. Os compromissos quase não pesam nisso. O que pesa é a hora preferida coerente (D-045), que espalha as sessões. O cansaço de um dia cheio de obrigações (`busyDayPenaltyPermille`) chegou a ir para 700. Depois da duração com tempo livre e do hábito proporcional (D-045), o fim de semana ganhou folga e o valor voltou a 650.

**Testes:** 4 novos (206 no total, com os da D-045).
- Só em dia livre, raro (2–15% dos dias livres), e sem sessão durante o compromisso.
- Determinístico e mais comum para quem é sociável.
- Tira tempo livre sem invadir o sono.
- Conteúdo inválido é erro.
