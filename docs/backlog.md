# Backlog por marco

Referência: [design.md](design.md) (seção 16 — roadmap) e [decisoes.md](decisoes.md).
Legenda: `[x]` feito e verificado · `[ ]` pendente · `[~]` parcial.

## Pendências de ambiente e documentação

- [x] Plano do criador em `docs/Plano_MMO_Tycoon_Godot_Claude.md` (v1.5, com o ciclo JSON → save SQLite → memória); `docs/design.md` virou apontador.
- [ ] PATH: `C:\Program Files (x86)\dotnet` (x86, sem SDK) vem antes de `C:\Program Files\dotnet` no PATH do sistema. Até corrigir, chame `"C:\Program Files\dotnet\dotnet.exe"` ou ajuste a ordem (precisa de administrador).
- [ ] Remover a pasta parcial da Unity `C:\Program Files\Unity` (~2,3 GB; precisa de administrador).

## M0 — Fundação ✅

Critério: compilar sem Godot, executar cenário reproduzível e exportar CSV/Markdown com metadados.

- [x] Solution com `Simulation.Core`, `Simulation.Infrastructure`, `Simulation.Runner` e `Simulation.Tests` (.NET 10)
- [x] Relógio de tick fixo (100 ms), calendário com fins de semana e feriados (D-005)
- [x] RNG determinístico com fluxos por subsistema e entidade, conferido com vetores de referência (D-004)
- [x] Agendador de eventos com desempate estável
- [x] Comandos registrados: sequência, tick previsto, aplicação e rejeição com motivo; replay testado
- [x] Eventos com causas (ex.: `server_opened` aponta o comando que o causou)
- [x] Hash canônico do estado (D-007); salto de ticks ociosos equivalente ao tick a tick
- [x] Conteúdo JSON do mundo inicial: 5 locais com atividades, 3 classes, 12 habilidades, 23 itens, 6 criaturas + 1 boss, 6 perfis de população
- [x] Validação de conteúdo com arquivo e caminho exato do campo; campos desconhecidos são erro
- [x] Geração de população com perfis, ruído individual, correlações controladas e aparência por ID
- [x] Runner com `daily.csv`, `events.csv`, `commands.csv`, `population.csv`, `summary.md` e `metadata.json`
- [x] 65 testes automatizados

## Teste de viabilidade Godot (antes do M1) ✅

Objetivo (plano v1.4): provar na prática que o projeto Godot é criado e mantido só pelo terminal.

- [x] Criar `GodotClient/` (Godot 4.7.2 .NET) inteiramente por arquivos de texto, referenciando `Simulation.Core`
- [x] Importar e rodar por linha de comando; gerar captura de tela automática
- [x] Mapa 2D das regiões com a população se movendo (fixture identificada como demonstração)
- [x] Exportar build Windows pela linha de comando (`tools/godot.ps1 export`)
- [x] Registrar resultado e limitações em `decisoes.md` (D-012 a D-014)

## M1 — HUD e mapa

- [ ] Tokens visuais da seção 12.1 como `Theme` do Godot (cores, espaçamentos, tipografia)
- [ ] Shell: barra superior (data/hora, pausa, velocidades 1x/2x/5x/10x, velocidade efetiva), navegação lateral, área central, painel de inspeção, faixa de eventos
- [ ] Mapa estratégico com pan, zoom, seleção de região e painel lateral
- [ ] Ponte cliente ↔ Core: snapshots de leitura e envio de comandos; simulação fora do frame de renderização
- [ ] Capturas de revisão em 1920×1080 e 1366×768

## M2 — População (em andamento)

- [x] Registro progressivo no lançamento; conta e personagem (classe pelos traços, apelido)
- [x] Sessões pelo calendário individual (horário preferido, dias por semana, fim de semana/feriado)
- [x] Conhecimento de zonas (inicial, saídas visíveis, exploração)
- [x] Decisão por utilidade entre atividades das zonas conhecidas, com fatores registrados e explicação na ficha
- [x] Deslocamento geométrico pelas estradas das zonas; posições reais no mapa (D-015, D-016)
- [x] XP e níveis provisórios
- [x] Relatório do runner com registros, DAU, pico online, sessões, escolhas por zona, níveis e exemplos de decisão
- [ ] Abandono e retorno (satisfação, frustração, intenção de retorno)
- [ ] Memória de experiências marcantes influenciando escolhas
- [ ] Teste estatístico do critério "trabalhador 19h–23h" com amostra e tolerância documentadas

## Cliente (mapa e fichas)

- [x] Mapa de zonas: células, fronteiras, estradas, saídas, pontos de atividade, camadas (lotação, perigo, nível)
- [x] Fichas em modal de jogador e zona; bolhas de aglomeração com lista e busca
- [x] Rascunho de configuração de zona com validação do Core (publicação no M7)
- [x] Áreas dentro das zonas (tipos lógicos, portais de masmorra e esconderijo) e ficha da área (D-019)
- [x] Regras de construção de áreas no validador (D-020)
- [x] Selos e hachura de perda por morte; contagem de jogadores por zona em destaque
- [ ] Editor de áreas com posicionamento livre, prévia de tamanho, acesso e conflitos (usa as regras do D-020)
- [ ] Quantidade de monstros e recursos por área (M3/M5), independente do tamanho
- [ ] Quests como objetivos com recompensa, separadas das atividades
- [x] HUD de gestão: indicadores com mini-gráfico, painel lateral (visão geral, zonas, jogadores, eventos), tabelas e tooltips estruturados (D-022)
- [x] Dossiê do jogador estilo FM (atributos 1–20) e dossiê de zona com tabela e gráfico
- [ ] Dossiê de área no mesmo padrão denso (faixa de dados e tabela de tarefas)
- [ ] Ícones de áreas mais detalhados no mapa
- [ ] Navegação com histórico (voltar) entre dossiês
- [ ] Tabelas de Comunidade/Economia com virtualização
- [ ] Revisão visual completa em 1366×768 das fichas

## Marcos seguintes

M3 combate · M4 observação 3D (com teste de estresse visual: centenas de personagens lutando, FPS medido) · M5 economia · M6 social · M7 empresa e patches · M8 versão integrada · M9 expansão · M10 profundidade. Detalhes na seção 16 do plano.
