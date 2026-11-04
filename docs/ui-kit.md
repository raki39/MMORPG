# Kit visual (D-034)

Referência para qualquer tela nova. Estilo de dossiê de jogo de gestão: escuro, denso e legível. Os dados vêm em cards e gráficos, cada número tem uma explicação em tooltip, e nada lembra um MMORPG mobile. Tudo está no código, sem editor (`GodotClient/Scripts/Ui`).

## Estrutura das telas

- **Mapa:** é a única tela em que o tempo passa.
  - O clique abre um **cartão-resumo** (`QuickCard`) ao lado do cursor.
  - "Ver informações" abre a página.
- **Páginas** (`ModalFrame`): ocupam tudo abaixo da barra superior.
  - O tempo pausa (opção nas Configurações) e volta na mesma velocidade.
  - Esc ou "Voltar ao mapa" retorna ao mapa.
- **Barra superior:** voltar/avançar, menu (Mapa, Jogadores, Zonas, Missões), indicadores, relógio, velocidade ou "Voltar ao mapa", e a engrenagem das Configurações.
- **Histórico:** voltar/avançar cobre mapa e páginas.
  - Atalhos: Alt+←/→ e os botões laterais do mouse.
  - Teclas M, J, Z e Q abrem as seções.

## Página

1. **Cabeçalho:**
   - à esquerda, retrato ou emblema (`Art`), que ocupa a altura toda;
   - a trilha discreta;
   - nome com 28 px seminegrito e subtítulo com 13 px;
   - a faixa de dados (`Fact`): ícone ou marcador, valor de 15 px, rótulo de 11 px e filetes entre células;
   - um destaque à direita (`Highlight`), por exemplo o meio-medidor;
   - abas de texto com 14 px, alinhadas ao nome; a ativa é dourada e sublinhada;
   - ações à direita das abas.
2. **Corpo:** linhas de cards numa **grade de 12 colunas** (`Grid` / `SpanRow`).
   - Cada card ocupa N colunas, com vão de 12 px.
   - As divisas caem sempre nas mesmas linhas verticais, de preferência 3, 6 e 9. Linhas diferentes podem ter cards de tamanhos diferentes, mas as bordas coincidem.
   - Os cards de uma linha têm a mesma altura.
3. **Visão geral:** só cards com dados que já existem.
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
- Tudo é desenhado alinhado ao pixel (linhas em meio pixel, retângulos inteiros).

## Interação

- Linhas clicáveis (`HoverRow`): fundo claro e filete dourado no hover, cursor de mão e seta › quando levam a outra página.
- Tooltips estruturados (`RichTip`) explicam a conta de cada número.
- O cartão-resumo se atualiza ao vivo (fora do hover).

## Conferência

- Toda tela nova passa por captura em 1920×1080 (`tools/godot.ps1 shot`, com `--page=`, `--open-*`, `--peek-*` e `--scroll=`).
- A conferência compara alinhamento na grade, tamanhos mínimos e contraste, e procura cards vazios.
- Depois, `tools/perf.ps1`.
