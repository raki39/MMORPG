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
