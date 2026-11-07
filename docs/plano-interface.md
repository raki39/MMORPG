# Plano de organização da interface (D-068)

Aprovado pelo criador em 06/11/2026. O trabalho é organizar, tirar redundâncias, melhorar os cards necessários e apresentar a informação de forma profissional, **sem refazer a identidade visual**.

## Regras do criador

- Preservar a identidade atual: cores, tipografia, ícones da interface, cards e estilo dos gráficos.
- **Não retirar informação**, só o que for redundante. A quantidade de informação é parte do jogo.
- **A tela é o contêiner:** uma página pode ter mais de uma seção, mas cada seção ocupa a tela inteira disponível. Nada de card cortado no fim da tela.
- **Rolagem por seções de tela inteira.**
- **Cards com o mesmo tamanho para qualquer jogador.** Se o conteúdo passar, rola dentro do card.
- Não diminuir fontes nem espaçamentos para caber, exceto no cabeçalho da ficha.
- Um pouco mais de cara de jogo, um pouco menos planilha: emblemas, ícones e etiquetas. Cada aba pode ter sua própria grade.

## O que as capturas mostraram (antes)

- A altura da linha vinha do card mais cheio. Por isso a primeira tela terminava no meio de cards e os tamanhos mudavam de um jogador para outro (a primeira linha da Visão geral variava de 360 a 396 px).
- A Visão geral juntava conteúdo de quase todas as abas: humor, memórias, horários, uso do tempo, evolução, os 19 atributos e o histórico. "Progresso e metas" repetia o cabeçalho e a aba Objetivos.
- Missões, Objetivos, Histórico, as abas futuras, Jogadores da zona e a página de área tinham pouco conteúdo e muito vazio.
- O cabeçalho tinha nove dados em linhas desencontradas, misturando estado com características.
- Os ícones eram genéricos nas classes e desenhados à mão nos emblemas de zona e área. Todas as missões usavam o mesmo pergaminho.
- Havia texto explicativo dentro dos cards, que pertence ao ⓘ.

## Abas da ficha do jogador

Visão geral · Objetivos · Missões · Progressão · Estatísticas · Rotina · Humor e memória · Histórico · Combate M3 · Inventário M5 · Banco M5.

- **Visão geral:** o dossiê completo da pessoa (correção do criador na etapa 3: não é um resumo magro e os atributos ficam nela, sem aba própria).
- **Estatísticas:** o jogador no servidor (comparação, classificação, níveis no servidor).
- **Progressão:** a evolução do próprio personagem (curva de nível, XP, uso do tempo, zonas).

| Card | Destino |
|---|---|
| Situação atual, Atributos do jogador, Personalidade e traços | Visão geral, na primeira linha |
| Missões | Missões (as ativas na Visão geral) |
| Evolução, Uso do tempo, Zonas frequentadas | Progressão |
| Horários de jogo, Sessões · últimos 14 dias | Rotina (feito na etapa 1) |
| Progresso e metas | Nível no card do personagem; objetivo e meta em Objetivos (resumo na Visão geral); perfil e personalidade já estão no cabeçalho |
| Humor, Memórias marcantes | Humor e memória (resumo na Visão geral) |
| Atividades recentes | Histórico (os últimos na Visão geral) |

### Como ficou (etapa 3)

Todas as abas em telas (`ScreenPage`) com cards de altura fixa. A Visão geral e a Rotina têm duas telas; as outras abas, uma. Na Visão geral, os cards que têm uma aba de detalhe levam um atalho (›) no título. As divisas das duas linhas coincidem: as de uma linha estão entre as da outra (colunas entre parênteses).

| Aba | Linha de cima | Linha de baixo |
|---|---|---|
| Visão geral, tela 1 | Situação atual (3) · Atributos do jogador, com o gosto marcante (6) · Personalidade e traços (3) | Progresso e metas (3) · Evolução (6) · Missões em andamento (3) |
| Visão geral, tela 2 | Humor (3) · Memórias marcantes (6) · Mudanças desde o cadastro (3) | Preferências (3) · Últimos acontecimentos (6) · Uso do tempo (3) |
| Objetivos | Objetivos gerais (4) · Objetivos secundários (4) · Metas atuais (4) | Conquistas e encerrados (4) · Histórico (8) |
| Missões | Em andamento (8) · Resumo (4) | Diário de missões (8) · Concluídas (4) |
| Progressão | Nível e experiência (4) · Evolução (8) | Uso do tempo (4) · XP por fonte (4) · Zonas frequentadas (4) |
| Estatísticas | Comparação com o servidor (8) · Classificação (4) | Níveis no servidor (12) |
| Rotina | (etapa 1, duas telas; a Ocupação sem a semana em 168 horas) | |
| Humor e memória | Humor (4) · O que pesa no humor (4) · Tédio por atividade (4) | Memórias marcantes (8) · Lugares que marcaram (4) |
| Histórico | Acontecimentos, até 150 (12) | — |

**Abas numa linha só:** em tela estreita, as ações viram ícones e, se ainda faltar espaço, as últimas abas vão para o botão "Mais ▾" no fim da linha. Quando a aba aberta está nele, o botão fica marcado e mostra o nome dela ("Mais · Banco").

**Informação nova na tela, que já existia na simulação:** as sete preferências com valor (antes só no tooltip), o valor de cada atributo no cadastro, os traços ganhos e perdidos com data e motivo, todos os fatores do humor com a parcela de cada um (antes só os três maiores), o tédio por tipo de atividade, as entregas por categoria e o diário de missões.

**Redundâncias tiradas:** perfil e personalidade repetidos no card de progresso (estão no cabeçalho e na Visão geral); "Perdeu: …" no card de traços (vai com data e motivo em Mudanças).

**A Visão geral repete de propósito** a Evolução, o Uso do tempo, o Humor e as Memórias, que também estão nas abas de detalhe: é o dossiê da pessoa numa olhada (pedido do criador).

## Cabeçalho da ficha

- Seis dados fixos numa linha (nível, status, zona, perfil, ritmo e horas), com fonte menor e reticências com tooltip. A altura é a mesma para todos.
- As características viram etiquetas com ícone abaixo do nome: preferências, personalidade, talento e experiência.
- As ações sobem para a linha do nome. As abas ficam numa linha só.

## Ícones (opção A)

Emblemas desenhados em SVG, no traço de linha do Lucide e gerados como os atuais. Cada um fica numa moldura colorida pelo tipo. Cobrem:
- classes e papéis;
- tipos de zona;
- tipos de área;
- categorias de missão;
- acontecimentos.

O Lucide continua nas ações e nas linhas de dados.

## Etapas

1. **Base do layout:** página em telas, linhas por peso, cards fixos com rolagem interna, rolagem de tela em tela. Aplicada na Rotina. **Feita.**
2. Cabeçalho da ficha. **Feita.**
3. Reorganização das abas da ficha. **Feita** (detalhe abaixo).
4. Emblemas e ícones do jogo.
5. Zona, área, missão e listas.
6. Conferência final: todas as telas em 1920×1080 e 1366×768, três jogadores, `tools/perf.ps1`.

Cada etapa tem capturas de antes e depois para aprovação.
