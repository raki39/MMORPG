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

## M1 — HUD e mapa (quase concluído)

Critério do plano: revisar o padrão visual cedo, com fixtures identificadas e conexão mínima ao Core. Foi além disso: o mapa e as fichas já mostram a simulação real do M2.

- [x] Tema do Godot com tokens próprios (cores, espaçamentos, tipografia). Não usa a paleta da seção 12.1, a pedido do criador (D-012, D-022).
- [x] Shell: barra superior com data/hora, pausa, velocidades 1x/2x/5x/10x e velocidade efetiva; painel lateral (visão geral, zonas, jogadores, eventos) no lugar da navegação lateral e da faixa de eventos
- [x] Mapa estratégico com pan, zoom, seleção de zona e fichas em modal
- [x] Ponte cliente ↔ Core: snapshots de leitura, comandos por fila, simulação em thread própria (fora do frame)
- [x] Capturas de revisão em 1920×1080 (automáticas, por linha de comando)
- [ ] Capturas e revisão em 1366×768

## Roteiro por etapas (combinado com o criador em 04/11/2026)

Uma etapa por vez, fechada por inteiro (testes, validação, documentação, exportação e resumo ao criador) antes da próxima. Interface só a do sistema da etapa; polimento visual vai para a lista do M8.

1. ~~Fechar o M2: plano da sessão, humor, memória, abandono e retorno~~ (feito: D-037, D-038, D-039, revisão D-039a)
1b. **Pessoa: rotina de vida e traits** ([plano-pessoa.md](plano-pessoa.md), aprovado com ajustes no D-041):
   - ~~P0: parâmetros da decisão no conteúdo~~ (feito, D-041);
   - ~~P1: localidade e fuso~~ (feito, D-042);
   - ~~P2: obrigações, tempo livre, vontade de jogar e renda real~~ (feito, D-043);
   - ~~P3: sono e dificuldade de parar, e jogar no horário de trabalho remoto~~ (feito, D-044);
     - depois: eventos dentro do jogo e atualizações grandes também mexem na tendência a prolongar e na volta de quem saiu (M7, com os outros motivos de retorno);
   - revisão da rotina antes da P4 (aprovada pelo criador): ~~R1 contabilidade do dia~~ (D-047), ~~R2 limites, probabilidade e estimativa~~ (D-048), ~~R3 distribuições e referência pré-R4~~ (D-049), ~~R4 modelo de vontade~~ (D-050, corrigido na v3: D-051), R5 recalibrar depois da medição e reescrever `rotina.md` separando invariantes, regras dos presets e tendências;
   - P4: traits;
   - a reputação do personagem entra com a Etapa 3 e as finanças com o M7.
2. **Etapa 1 — O personagem** (decisões no D-040):
   - estatísticas diretas por classe + nível + equipamento: vida, recurso de classe, ataque e defesa físicos e mágicos, velocidade de movimento (fórmulas no conteúdo; nomes sem confundir com os atributos do jogador);
   - revisão das 3 classes e das 12 magias;
   - itens como instâncias (dono, origem) e equipamento inicial;
   - inventário e carteira de ouro com fontes registradas;
   - aba "Personagem".
3. **Etapa 2 — Combate PvE (M3):**
   - criaturas vivas por área e combate determinístico sem 3D;
   - magias e efeitos combináveis;
   - XP e saque por abate;
   - morte sem perda (D-040): reaparece na cidade vinculada depois de um intervalo, recupera os recursos e viaja de novo;
   - Mecânica e Conhecimento na execução (sai o bônus de XP);
   - recalibrar a curva (a ou b).
4. **Etapa 3 — PvP e chefe (M3):**
   - luta real em emboscada e patrulha;
   - perda por morte pela regra da zona;
   - chefe com grupo provisório;
   - painel de classes inicial.
5. **Depois, na ordem:** ritmo de gestão (plano 3.3) → M5 economia → M6 social → M7 empresa e atualizações → M8 versão integrada. A visão 3D (M4) fica para depois dos sistemas básicos, a pedido do criador.

## M2 — População ✅ (fechado em 04/11/2026, D-039)

Critério do plano: 100 pessoas entram e saem, escolhem atividades entre as oferecidas pelos locais que conhecem e exibem motivos reais. Cumprido, com humor, memória, plano da sessão, abandono e retorno.

Feito:
- [x] Registro progressivo no lançamento; conta e personagem (classe pelos traços, apelido)
- [x] Sessões pelo calendário individual (horário preferido, dias por semana, fim de semana/feriado)
- [x] Conhecimento de zonas (inicial, saídas visíveis, exploração)
- [x] Decisão por utilidade entre atividades das zonas conhecidas, com fatores registrados e explicação na ficha
- [x] Deslocamento geométrico pelas estradas das zonas; posições reais no mapa (D-015, D-016)
- [x] XP e níveis provisórios
- [x] Relatório do runner com registros, DAU, pico online, sessões, escolhas por zona, níveis e exemplos de decisão

Validação (D-024, [validacao.md](validacao.md)):
- [x] Metas de referência em `targets.json` (hipóteses, ajustáveis pelo criador)
- [x] Runner com várias seeds em paralelo e relatório de validação (metas, perfis, uso do tempo, atividades pouco usadas com diagnóstico)
- [x] Tempo jogado, tempo por tipo de atividade e tempo até cada nível por personagem (hash v3)
- [x] Teste estatístico do critério "trabalhador 19h–23h" (esperado 96,08%, observado 96,07%, tolerância documentada)
- [x] Rodada de 5 seeds × 1.000 pessoas × 90 dias: rotina validada; progressão, deslocamento e divisão do tempo fora das metas (ver validacao.md)
- [ ] Ajustar as metas com o criador (principalmente o ritmo de progressão)

Base de conteúdo e progressão (próximo passo; D-025):
- [x] Revisar XP e curva (D-027): `progression.json` com teto 30, curva, taxa por faixa de nível, XP por tipo, crescimento por desafio e diferença de nível; ritmo A; prévia `--progression`; medido em 5 seeds
- [x] Nível do desafio por atividade (caça pelas criaturas); a cidade não dá XP; a faixa da zona não entra mais no XP nem no risco
- [ ] Faixa recomendada por área e requisitos de quest (com a redistribuição e o catálogo)
- [ ] Revisão visual das fichas alteradas no D-027 (área: nível do desafio e XP por ciclo; jogador: taxa da faixa) antes de fechar a entrega integrada
- [x] Redistribuir e ampliar áreas até o nível 30 (D-028): 3 cidades, 12 zonas, 54 áreas com faixa, 19 criaturas novas; 2+ caças por nível (uma sem PvP); grade de lotes com 55 livres; terreno por parâmetros
- [x] Legibilidade do mapa (D-029): ilha por máscara, zonas maiores, estradas finas e curvas, rótulos enxutos
- [x] Terreno que a simulação respeita (D-030): água do Core (rio natural configurável, poças), estradas por A* com pontes, viagem pelo comprimento da estrada
- [x] Cidades por nível (D-030): muralha, calçamento, casas, prédios dentro; área urbana marcada e com hover; posição da cidade no tile configurável
- [x] Desempenho do mapa (D-032): cache de desenho, hover sem redesenho, preenchimentos em lote; `tools/perf.ps1` com orçamento de 6,9 ms (p95)
- [ ] Desempenho: emblemas e nomes em lote (pior caso de redesenho ~8,7 ms); shader do terreno com saída rápida no mar; medir com 10.000 pessoas e simulação pesada
- [ ] Recalibrar a curva depois do passo 3: escolha do criador entre (a) ~200 h com desaceleração suave no fim ou (b) desaceleração forte com 220–250 h
- [x] Quests por área de quest (D-031): quadros nas praças, 36 quests (principais em cadeia, secundárias e diárias com limite), etapas reais (etapas de caça e coleta marcadas como provisórias), recompensas de XP, ouro e itens (estes dois não implementados); aceitar e entregar pontuais; estatísticas por quest; ficha da quest estilo FM
- [ ] Editor de quests e quadros (texto, etapas, condições, recompensas; quadros em outras áreas)
- [ ] Etapas de quest por abates e itens quando existirem combate (M3) e inventário (M5); outras recompensas (reputação, desbloqueios)
- [ ] Medir a progressão até o 30 (horas por nível, XP por fonte, conteúdo esgotado antes da próxima faixa) e ajustar as metas

Continuidade, humor e retenção (D-026; fechamento do M2):
- [x] Linha de base de continuidade e motivos de troca no relatório de validação (D-037)
- [x] Plano da sessão, continuidade (margem, compromisso mínimo, revisão por acontecimentos) e tédio por tipo com preferência por variedade (D-037)
- [x] Expectativa de progresso com piso (D-038)
- [x] Humor: satisfação, frustração, novidade, engajamento e intenção de continuar, com escalas documentadas (D-038, docs/comportamento.md)
- [x] Memória de experiências influenciando escolhas (impressão por lugar) e acontecimentos marcantes (D-038)
- [x] Sessões flexíveis: estender com teto, encerrar por frustração (D-039)
- [x] Inativo, desistente e retorno, com causas registradas (D-039)
- [x] Ficha: Humor, Memórias marcantes, plano da sessão; listas com Inativos e Desistiram (D-039)
- [x] Revisão D-039a: metas em três classes (regra do cenário em rodada controlada, provisória, sistema futuro), traço Variedade, novidade em horizonte de meses, saudade sem recomeço fabricado e sem ciclos
- [ ] Revisão do criador das metas de retenção e progressão (hipóteses em targets.json)
- [ ] Outros motivos de retorno: atualizações (M7), convites de amigos (M6), campanhas de marketing e streamers (M7)
- [ ] Conhecimento "sabe que existe" × "conhece" e exploração do interior das zonas (com o editor de mapa)
- [ ] Ambições de longo prazo em etapas (ficar muito forte, liderar guilda)
- [ ] Teste de salvar e carregar metas, planos, humor e memória (quando o save existir)

## Cliente (mapa e fichas)

- [x] Mapa de zonas: células, fronteiras, estradas, saídas, pontos de atividade, camadas (lotação, perigo, nível)
- [x] Fichas em modal de jogador e zona; bolhas de aglomeração com lista e busca
- [x] Rascunho de configuração de zona com validação do Core (publicação no M7)
- [x] Áreas dentro das zonas (tipos lógicos, portais de masmorra e esconderijo) e ficha da área (D-019)
- [x] Regras de construção de áreas no validador (D-020)
- [x] Selos e hachura de perda por morte; contagem de jogadores por zona em destaque
- [ ] Editor de mapa (tela própria): ocupar ou comprar lotes livres, criar zonas, áreas, monstros e quests; usa a grade e as regras do D-020/D-028
- [ ] Editor de tiles: posicionar a cidade, rio, lagos e áreas (dentro ou fora da muralha), criar e editar estradas e conexões; tudo já é conteúdo validado (D-030)
- [ ] Regras por área: área com regras próprias sobre a zona (ex.: cidade sem PvP dentro de zona PvP; área urbana como contêiner)
- [ ] Estilos de cidade (vários desenhos selecionáveis, com e sem rio) e evolução do nível da cidade no jogo
- [ ] Editor de áreas com posicionamento livre, prévia de tamanho, acesso e conflitos (usa as regras do D-020)
- [ ] Saque das criaturas novas (loot vazio até o M3)
- [ ] Quantidade de monstros e recursos por área (M3/M5), independente do tamanho
- [x] Ficha de quest (estilo FM) e catálogo do quadro na ficha da área (D-031)
- [x] HUD de gestão: indicadores com mini-gráfico, painel lateral (visão geral, zonas, jogadores, eventos), tabelas e tooltips estruturados (D-022)
- [x] Dossiê do jogador estilo FM (atributos 1–20) e dossiê de zona com tabela e gráfico
- [x] Sistema visual v2 dos dossiês (D-033): tela cheia, voltar/avançar, Inter, cards, indicadores e gráficos analíticos; ficha do jogador com uso do tempo, ritmo de evolução e minutos por dia
- [x] Telas do jogo (D-034): menu, páginas com tempo pausado, cartão-resumo no mapa, listas e Configurações (tela cheia, vídeo)
- [x] Kit visual v3 (D-034, docs/ui-kit.md): texto nítido, grade de 12 colunas, cards com textura, ícones Lucide, gráficos novos; Visão geral do jogador, da zona e da missão
- [ ] Revisão do criador do kit v3 em 1920×1080
- [ ] Kit v3 na página de área, nas abas secundárias (Missões, Histórico, Rotina, Jogadores e Regras da zona) e no painel lateral do mapa
- [ ] Escala da interface nas Configurações (telas menores que 1600×900)
- [x] Barra superior de altura fixa com submenus, indicadores sem gráfico, painel lateral recolhível e painéis do mapa no kit (D-034)
- [x] Menu principal lateral recolhível, barra mais limpa, painel do mapa de 340 px fechado ao iniciar (D-035)
- [x] Ficha do jogador: personalidade no cabeçalho, Inventário · M5, Progresso e objetivos, Horários de jogo, aba Estatísticas (D-035)
- [x] Ajustes de UX: dica vazia, menu lateral, rolagem, tabelas (vão, ordenação), filtros nas listas, personalidade e perfil, quem concluiu cada missão (D-035)
- [x] Metas de curto prazo na simulação e no card "Progresso e metas" (D-036, D-036a)
- [x] Atributos do jogador vivos (sobem e caem devagar), talento e potencial; dica do que cada um faz (D-036, docs/atributos.md)
- [x] Revisão D-036a: talento × experiência, potencial absoluto, gosto × execução, relações claras (zona familiar, desafio, estimativa de viagem), parâmetros em `population.json`, evolução medida no relatório
- [ ] Ambições de longo prazo em etapas (ficar muito forte, liderar guilda), separadas das metas de curto prazo
- [ ] Coordenação evoluindo em atividades cooperativas (grupos e raides, M6); experiências ruins persistentes (mortes, perdas) mudando gostos e aversão a risco (M3)
- [ ] Classes e itens iniciais (próximo passo combinado com o criador)
- [ ] Aba "Personagem" na página do jogador: classe, nível, itens, status e combate do personagem (separado dos atributos do jogador)
- [ ] Combate (M3) usa os atributos do jogador junto com os do personagem (Mecânica, PvP, Coordenação); chamar OnPvpKill nos abates
- [ ] Inatividade por atualização perdida (M7) no lugar dos 14 dias para o Conhecimento
- [ ] Relatório de moderadores (inspeção de jogadores, M9)
- [ ] Abas de Inventário e Banco (M5) e últimos abates (M3) com dados reais
- [ ] Ícones de áreas mais detalhados no mapa
- [x] Navegação com histórico (voltar e avançar) entre dossiês (D-033)
- [ ] Tabelas de Comunidade/Economia com virtualização
- [ ] Revisão visual completa em 1366×768 das fichas

## M3 — Combate (depois do M2)

Pedido do criador: combate nas caças (PvE), no PvP e contra chefes.
- [ ] Caça com criaturas reais (quantidade e reaparecimento por área), dano, morte e saque
- [ ] PvP de verdade em emboscada e patrulha (hoje elas dão XP sem combate)
- [ ] Morte com as regras da zona (perda parcial ou total) e propriedade dos itens
- [ ] Chefe em grupo (depende de grupos, M6, ou de uma versão provisória)

## Marcos seguintes

M3 combate · M4 observação 3D (com teste de estresse visual: centenas de personagens lutando, FPS medido) · M5 economia · M6 social · M7 empresa e patches · M8 versão integrada · M9 expansão · M10 profundidade. Detalhes na seção 16 do plano.
