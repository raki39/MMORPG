# Backlog por marco

Referência: [design.md](design.md) (seção 16 — roadmap) e [decisoes.md](decisoes.md).
Legenda: `[x]` feito e verificado · `[ ]` pendente · `[~]` parcial.

## Pendências de ambiente e documentação

- [ ] Adotar o plano v1.4 (`Plano_MMO_Tycoon_Godot_Claude.md`) como `docs/design.md`. O arquivo ainda não foi colocado no repositório; hoje `docs/design.md` é a v1.3 adaptada para Godot por substituição de texto.
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

## Teste de viabilidade Godot (antes do M1)

Objetivo (plano v1.4): provar na prática que o projeto Godot é criado e mantido só pelo terminal.

- [ ] Criar `GameClient/` (Godot 4.7.2 .NET) inteiramente por arquivos de texto, referenciando `Simulation.Core`
- [ ] Importar e rodar por linha de comando; gerar captura de tela automática
- [ ] Mapa 2D das regiões (posições do conteúdo) com a população se movendo (fixture claramente identificada até o M2)
- [ ] Exportar build Windows pela linha de comando (templates de exportação 4.7.2)
- [ ] Registrar resultado e limitações em `decisoes.md`

## M1 — HUD e mapa

- [ ] Tokens visuais da seção 12.1 como `Theme` do Godot (cores, espaçamentos, tipografia)
- [ ] Shell: barra superior (data/hora, pausa, velocidades 1x/2x/5x/10x, velocidade efetiva), navegação lateral, área central, painel de inspeção, faixa de eventos
- [ ] Mapa estratégico com pan, zoom, seleção de região e painel lateral
- [ ] Ponte cliente ↔ Core: snapshots de leitura e envio de comandos; simulação fora do frame de renderização
- [ ] Capturas de revisão em 1920×1080 e 1366×768

## M2 — População (resumo)

- [ ] Sessões: calendário individual, login e logout, estados de ciclo de vida
- [ ] Registro: criação de Conta e Personagem
- [ ] Conhecimento de locais por pessoa (descoberta por exploração, anúncios e amigos)
- [ ] Decisão por utilidade só sobre atividades de locais conhecidos, com valores dependentes do estado do mundo (lotação, risco, viagem, amigos)
- [ ] Explicação da escolha com os fatores calculados
- [ ] Critério: 100 pessoas entram e saem, escolhem atividades dos locais conhecidos e exibem motivos reais

## Marcos seguintes

M3 combate · M4 observação 3D (com teste de estresse visual: centenas de personagens lutando, FPS medido) · M5 economia · M6 social · M7 empresa e patches · M8 versão integrada · M9 expansão · M10 profundidade. Detalhes na seção 16 do plano.
