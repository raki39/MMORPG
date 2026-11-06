# Kit visual (D-034)

Referência para qualquer tela nova. Estilo de dossiê de jogo de gestão: escuro, denso e legível. Os dados vêm em cards e gráficos, cada número tem uma explicação em tooltip, e nada lembra um MMORPG mobile. Tudo está no código, sem editor (`GodotClient/Scripts/Ui`).

## Estrutura das telas

- **Mapa:** é a única tela em que o tempo passa.
  - O clique abre um **cartão-resumo** (`QuickCard`) ao lado do cursor.
  - "Ver informações" abre a página.
- **Páginas** (`ModalFrame`): ocupam tudo abaixo da barra superior.
  - O tempo pausa (opção nas Configurações) e volta na mesma velocidade.
  - Esc ou "Voltar ao mapa" retorna ao mapa.
- **Menu principal à esquerda** (`NavSidebar`, D-035):
  - 224 px aberto (sempre ao iniciar); recolhido vira uma faixa de ícones de 60 px (tecla N);
  - seções com itens de ícone e nome; subitens em sanfona;
  - itens futuros apagados com o marco;
  - Configurações e o botão de recolher no rodapé.
- **Barra superior:** altura fixa de 56 px, com o conteúdo cortado. Contém voltar/avançar, nome da tela, indicadores sem gráfico, relógio, e velocidade ou "Voltar ao mapa".
- **Painel do mapa à direita:** 340 px, sempre fechado ao iniciar (tecla P ou botão nos controles do mapa).
- **Painéis do mapa e páginas:** se posicionam pela largura atual do menu e do painel.
- **Histórico:** voltar/avançar cobre mapa e páginas.
  - Atalhos: Alt+←/→ e os botões laterais do mouse.
  - Teclas M, J, Z e Q abrem as seções.

## Página

1. **Cabeçalho** (padrão D-068, o mesmo visual e organização em todas as páginas; cada uma mostra o que é dela):
   - à esquerda, retrato ou emblema (`Art`, 92 px; 76 px nas listas e em Configurações);
   - a trilha discreta;
   - o **título** com 24 px seminegrito (`TitleText`);
   - embaixo, o **bloco de identificação** em duas colunas alinhadas (`InfoGrid` com `Info`: ícone e texto de 13 px). O segundo texto de cada linha começa no mesmo lugar. O bloco tem **largura fixa por tipo de página**, para as etiquetas começarem no mesmo ponto em qualquer ficha; texto longo termina em reticências, com tooltip. Nas listas e em Configurações, o bloco é só a descrição;
   - ao lado, as **etiquetas** (`Tag`): sem fundo, todas da mesma largura na página (`TagWidth`), filetes entre elas, rótulo curto de 12 px em cima, ícone e valor de 13 px embaixo, explicação no tooltip. Ficam numa linha só, ao lado do bloco quando cabem e numa linha própria abaixo quando não (1366);
   - um **destaque** à direita (`Highlight`) quando faz sentido: o ritmo de jogo na ficha da pessoa, a lotação na zona;
   - o que vai em cada página:

     | Página | Bloco de identificação | Etiquetas | Destaque |
     |---|---|---|---|
     | Pessoa | classe e papel \| nível; status \| zona; pessoa \| horas jogadas | perfil, preferências, personalidade, talento, experiência | ritmo de jogo |
     | Zona | função e bioma \| níveis; jogadores agora \| áreas; saídas \| capacidade | combate, morte, controle, perigo | lotação |
     | Área | tipo \| faixa; zona \| jogadores agora; a descrição | combate, morte, tarefas, tamanho (e "leva a") | — |
     | Missão | categoria \| repetição; quadro \| zona | nível, etapas, XP, concluíram, fazendo agora | — |
     | Listas e Configurações | a descrição | os números da lista ou do computador | — |
   - abas de texto com 14 px, alinhadas ao título; a ativa é dourada e sublinhada;
   - ações à direita das abas. Se abas e ações não cabem numa linha (1366×768), as ações com ícone mostram só o ícone, com o rótulo no tooltip, e as abas continuam numa linha só.
2. **Corpo:** linhas de cards numa **grade de 12 colunas** (`Grid` / `SpanRow`).
   - Cada card ocupa N colunas, com vão de 12 px.
   - As divisas caem sempre nas mesmas linhas verticais, de preferência 3, 6 e 9. Linhas diferentes podem ter cards de tamanhos diferentes, mas as bordas coincidem.
   - Os cards de uma linha têm a mesma altura.
3. **Página em telas** (`ScreenPage`, D-068; `plano-interface.md`):
   - a página se divide em seções, e **cada seção ocupa exatamente a altura visível**; nada fica cortado no fim da tela;
   - dentro da seção, cada linha recebe a altura por **peso**, com um mínimo legível; os cards preenchem a linha;
   - **card de altura fixa** (`FillCard`): a altura vem do layout, nunca do conteúdo, então é igual para qualquer jogador. Se o conteúdo passar, rola dentro do card (barra fina), e a rolagem interna se mantém nas atualizações ao vivo;
   - gráficos marcados com `Grow` crescem com o card;
   - a **roda do mouse salta de tela em tela** (animação curta). Dentro de um card com conteúdo maior, a roda rola o card primeiro;
   - **em 1366×768**, a seção mantém as suas linhas, com cards mais baixos e rolagem dentro do card. Só passa para a tela seguinte se uma linha ficaria abaixo de 180 px. Uma linha de cards empilhados vira uma linha por card;
   - capturas: `--section=N` abre a tela N; `--wheel=N` gira a roda N vezes.
4. **Visão geral:** só cards com dados que já existem.
   - O que depende de sistemas futuros fica numa aba própria com o marco ("Combate · M3", "Inventário · M5").
   - Não há card vazio na visão geral.

## Card (`Card` / `CardPanel`)

- **Fundo:**
  - degradê vertical `#151920` → `#101318`;
  - textura sutil (grão + hachura diagonal, gerada por código);
  - brilho dourado muito leve no canto superior direito.
- **Borda e cantos:** borda de 1 px (`#222831`) que clareia no hover; cantos de 4 px.
- **Espaçamento:** margem interna de 16 px nas laterais e 13–15 px no topo e na base.
- **Cabeçalho:** título dourado de 14 px seminegrito, conteúdo opcional à direita e ícone ⓘ com a explicação.
- **Subtítulo interno:** `Section` (13 px seminegrito claro com filete).
- **Filete:** `Divider`.

## Tipografia (Inter, OFL)

| Uso | Tamanho | Peso |
|---|---|---|
| Nome da página | 28 | SemiBold |
| Número grande (indicador) | 20–22 | SemiBold, algarismos tabulares |
| Linha de destaque do card | 15–16 | SemiBold |
| Título do card, abas, menu | 14 | SemiBold / Medium |
| Rótulo de dado, corpo | 13 | Medium / Regular |
| Dado secundário, tabela | 12 | Medium / Regular |
| Legenda, eixo | 11 | Medium / Regular (mínimo absoluto) |

- **Renderização:**
  - glifos presos ao pixel;
  - hinting leve com autohinter;
  - suavização LCD, como o ClearType ("Texto: Nítido/Suave" nas Configurações).
- **Fontes fora do Inter:**
  - Cinzel só nos nomes de zona do mapa;
  - Barlow condensada não é mais usada nas páginas.

## Cores

| Papel | Cor |
|---|---|
| Fundo da página | `#08090C` com brilho radial suave |
| Texto principal | `#ECEEF1` |
| Rótulos de dados | `#C4CAD3` |
| Secundário | `#8B94A1` |
| Eixos | `#6C7581` |
| Marca (títulos, aba ativa, ação principal) | dourado `#D4A64A` / `#E8C67F` |
| Bom | `#4CC38A` |
| Atenção | `#E89A4A` |
| Ruim | `#E5544F` |
| Série neutra | `#4DA8E8` |
| Excepcional | roxo `#B48CF2` |

- **Atributos de 1 a 20:**
  - 18+ roxo;
  - 14–17 verde;
  - 9–13 cinza;
  - 6–8 cinza escuro;
  - 5 ou menos vermelho.
- **Notas de 1 a 5:** 5 verde, 4 verde-claro, 3 dourado, 2 laranja, 1 vermelho.
- **Uso da cor:** sempre com significado; o resto fica em cinzas.

## Ícones

- Lucide (ISC, `Assets/Icons/Lucide-LICENSE.txt`), embutidos em `Icons.cs` (gerado por `tools/gen_icons.py`).
- Rasterizados no tamanho exato de uso, com traço visual de cerca de 1,6 px; brancos e tingidos por `Modulate`.
- Tamanhos: 14 (linhas), 16 (dados), 18–20 (destaques).

## Gráficos

- **Linha** (`LineChart`):
  - grade contínua fina e rótulos de 11 px;
  - linha de 2 px com pontos e área em degradê;
  - valor do último ponto numa pílula;
  - leitura no hover (guia, ponto, caixa).
- **Colunas** (`ColumnChart`):
  - colunas com topo arredondado e cor por desempenho (ou por índice);
  - média tracejada opcional e escala de minutos ou de contagem;
  - leitura no hover.
- **Rosca** (`DonutChart`), **anel** (`RingGauge`), **nota 1–5** (`RingRating`), **meio-medidor** (`SemiGauge`) e **pontos 1–5** (`Pips`).
- **Barras finas** (`ThinBar`, 3–4 px, presas ao pixel).
- **Barras agrupadas** (`GroupedBarChart`):
  - uma barra sólida por série, lado a lado, sem trilho vazio;
  - valor em cima só quando cabe;
  - embaixo, o rótulo e um texto curto ou ícone;
  - nada de texto espremido entre barras: o detalhe vai na leitura.
- **Tendência diária** (`DayTrendChart`):
  - área para a capacidade (tempo livre) e linha com pontos para o realizado;
  - ponto no zero na cor do motivo;
  - faixas sutis para os dias livres.
- **Sono** (`SleepChart`): colunas contra uma referência tracejada.
- **Leitura** (`Readout`): título e linhas coloridas.
- **Cartões internos**, como os dias da semana: fundo `#12161C`, borda de 1 px, dourada no item de hoje.
- **Etiquetas com ícone** (`Chip`) para condições.
- Tudo é desenhado alinhado ao pixel (linhas em meio pixel, retângulos inteiros).

## Como fazer uma tela boa (lições da aba Rotina, aprovada pelo criador)

1. **Cada card responde a uma pergunta.** Antes do gráfico, escrever a pergunta, por exemplo "quanto tempo livre ela tem e quanto se espera que jogue?". O título e o ⓘ dizem isso em linguagem simples.
2. **A ordem conta uma história:**
   - primeiro, quem é a pessoa (contexto: onde mora, finanças, vontade);
   - depois, o padrão (semana típica e ocupação);
   - por fim, o que aconteceu (últimos 30 dias, sono, motivos).
3. **O que se lê junto fica lado a lado.** Exemplo: a semana típica ao lado das obrigações que a explicam.
4. **Gráfico certo para cada dado:**
   - série diária longa: linha, curva monótona suave, com os pontos nos valores reais;
   - capacidade × realizado: área para a capacidade e linha para o realizado;
   - comparação por categoria (dias da semana): barras sólidas agrupadas, lado a lado;
   - parte de um todo: rosca ou barra única proporcional (ex.: a semana em 168 horas);
   - perfil: atributos 1–20 com barra fina.
5. **O que deu errado e não volta:**
   - agenda em faixas de 24 h (parece Gantt e cansa);
   - barras com trilho ou fundo vazio, ou com uma série por dentro da outra;
   - texto espremido entre barras ou sob colunas estreitas;
   - **agrupar dados que podem ser diferentes**, como "seg–sex". Cada dia é mostrado por si (dia a dia).
6. **O texto no gráfico é mínimo:**
   - rótulo curto e valor em cima só quando cabe;
   - o detalhe vai na leitura ao passar o mouse (`Readout`);
   - a legenda mostra só o que aparece na tela;
   - qualquer marca nova (%, losango, faixa) é explicada na legenda ou no ⓘ.
7. **A mesma cor tem o mesmo sentido na tela inteira.** Azul é tempo livre, verde é dia livre ou bom, dourado é jogo (jogado ou previsto), vermelho é falta ou zero, laranja é atenção.
8. **Números que aparecem em dois cards usam a mesma definição.** Exemplo: "dias com jogo" na linha dos 30 dias e na rosca dos motivos. Se não baterem, falta uma categoria, como "jogou só no trabalho".
9. **Card cheio de dado útil, sem espaço morto.** Para ocupar o espaço:
   - cartões internos (um por dia, com borda dourada no de hoje);
   - etiquetas com ícone para condições (`Chip`);
   - mini-indicadores (`MiniStat`);
   - uma frase de leitura do perfil, como um relatório de olheiro.
10. **Conferir antes de mostrar:**
    - captura em 1920×1080 (cards lado a lado) e em 1366×768 (cards empilhados);
    - texto cortado ou sobreposto é defeito, mesmo que só aconteça numa das larguras;
    - rodar `tools/perf.ps1`.

## Interação

- Linhas clicáveis (`HoverRow`): fundo claro e filete dourado no hover, cursor de mão e seta › quando levam a outra página.
- Tooltips estruturados (`RichTip`) explicam a conta de cada número.
- O cartão-resumo se atualiza ao vivo (fora do hover).

## Conferência

- Toda tela nova passa por captura em 1920×1080 (`tools/godot.ps1 shot`, com `--page=`, `--open-*`, `--peek-*` e `--scroll=`).
- A conferência compara alinhamento na grade, tamanhos mínimos e contraste, e procura cards vazios.
- Depois, `tools/perf.ps1`.
