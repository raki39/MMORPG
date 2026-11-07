# Dicas de UX

Este guia é para quem for montar ou refazer uma tela do jogo. Ele reúne o que aprendemos com o criador na reorganização da interface (D-068). O `ui-kit.md` diz **com o que** montar (componentes, cores, tipografia). Aqui está **como pensar** a tela, a lista de conferência e os erros que já cometemos.

## 1. Antes de montar: o inventário

Nunca começar pela grade. Começar pelo conteúdo:

1. **Listar os cards** e, para cada um, escrever:
   - a pergunta que ele responde, por exemplo "quando chega ao próximo nível?";
   - o que mostra;
   - a largura e a altura de que precisa.
2. **Agrupar por assunto.** Cada linha fala de uma coisa, como progressão, como joga ou posição no servidor.
3. **Só então escolher as larguras** e conferir se tudo cabe numa tela.

Exemplo: o inventário da aba Estatísticas, que o criador aprovou.

| Card | Conteúdo | Pede |
|---|---|---|
| Nível e experiência | anel e 7 números | 4 colunas (anel ao lado dos dados) |
| Evolução | curva de horas por nível | 5 (gráfico de linha) |
| Próximos níveis | lista de 8 linhas curtas | 3 |
| Uso do tempo | rosca e 6 itens | 4 |
| XP por fonte | rosca e 2 valores | 3 |
| Zonas frequentadas | lista com barras | 5 |
| Classificação | 4 posições | 3 |
| Comparação com o servidor | tabela de 5 linhas | 5 |
| Níveis no servidor | histograma | 4 |

O resultado foram três linhas (progressão, como joga e servidor) numa tela só, com divisões diferentes em cada linha.

## 2. Largura: o conteúdo manda

- **Referência de largura** (grade de 12 colunas):
  - números compactos e listas curtas: 3 colunas;
  - roscas, histogramas e o anel com dados: 4;
  - gráficos de linha, tabelas e listas com barras: 5 ou 6.
- **As linhas não precisam repetir a mesma divisão.** Uma linha pode ter 2 cards e a de baixo 4. As divisas não precisam coincidir. Coincidir é bom quando sai naturalmente, nunca à custa de card esticado ou apertado.
- **Card largo com pouco conteúdo é defeito.** Uma rosca com duas linhas de legenda num card de 546 px fica um vazio à direita.
- **Conteúdo menor que o card fica centrado** (`Centered`), nunca encostado num canto com o resto vazio.

## 3. Altura e telas

- **Cada linha tem a altura natural do conteúdo** (`ScreenPage`).
- **Uma tela só quando cabe.** Se três linhas cabem na altura natural, não criar uma segunda seção para separar assuntos curtos.
- **A seção só enche a tela quando tem conteúdo para isso**, ou quando há outra seção embaixo. Fora disso, os cards ficam na altura natural.
- **Nunca mostrar uma tela cortada.** A última tela, se for mais baixa que a janela, começa no topo e sobra espaço embaixo (já automático no `ScreenPage`).
- **Em 1366** as linhas passam sozinhas para a tela seguinte quando não cabem.

## 4. Sem rolagem dentro do card (1920×1080)

- **Listas mostram só o que cabe** (`FitList`), ordenadas pelo que importa: o mais recente ou o mais forte primeiro.
- **Lista longa ganha páginas** no título do card ("1–10 de 27 ‹ ›"). A seta de voltar volta uma página inteira, não um item.
- **Rolagem dentro do card só como último recurso**, em 1366.
- A frase de lista vazia não conta como item (`AsPlaceholder`).

## 5. Nada vazio, nada esticado, nada cortado

- **Texto cortado ou sobreposto é defeito**, mesmo que só aconteça numa das resoluções.
  - Rótulos de gráfico ficam dentro do gráfico.
  - Valores não usam reticências sem largura mínima.
- **Card esticado só para cobrir a tela é defeito.**
- **Faltou conteúdo num card?** Completar com algo que faça sentido, como foi a jornada do dia na Ocupação. Uma barra que nem o criador consegue ler não serve.
- **Não há card vazio na Visão geral.** O que depende de sistemas futuros vai para uma aba com o marco ("Combate · M3").
- **Card que quase sempre fica vazio vira parte de outro.** Exemplo: "Mudanças desde o cadastro", quase sempre "nada mudou", entrou em Personalidade e traços.
- **Lista que costuma ter um item mostra o quadro inteiro.** Exemplo: Tédio por atividade lista todos os tipos jogados, com 0% nos que não cansaram.
- **Sem dado agora, um card só com a frase.** Nunca tabela e gráfico vazios lado a lado (missão que ninguém está fazendo).

## 6. Cada dado num lugar

- **O cabeçalho já conta.** Se o tipo, as regras, o tamanho e a contagem estão nas etiquetas do cabeçalho, nenhum card repete. A página de área perdeu dois cards por isso.
- **Numa mesma página, um dado aparece uma vez.** Se aparece em dois cards, um deles vira legenda ou atalho. Exemplo: "Zonas conhecidas" saiu do card de nível e virou "11 de 15 conhecidas" no título de Zonas frequentadas.
- **Abas que tratam do mesmo assunto viram uma.** Progressão e Estatísticas foram juntadas.
- **O mesmo número em dois lugares usa a mesma definição.**

## 7. Visão geral é o dossiê completo

- Nunca um resumo magro. O criador quer ver a pessoa inteira sem trocar de aba.
- A primeira linha é Situação atual, Atributos e Personalidade e traços.
- Os cards com aba de detalhe levam o atalho › no título.

## 8. Gráficos

- **Escolher o gráfico pela pergunta:**
  - série no tempo: linha suave, com os pontos nos valores reais;
  - comparação por categoria: barras sólidas;
  - parte de um todo: rosca ou barra única;
  - diferença para uma referência: barra divergente (verde acima, vermelho abaixo).
- **Texto mínimo dentro do gráfico.** O detalhe vai no hover e no ⓘ.
- **A mesma cor tem o mesmo sentido** na tela inteira.
- **O que já deu errado:**
  - agenda em faixas de 24 h, que parece Gantt;
  - barras com trilho vazio ou com uma série dentro da outra;
  - agrupar dias que podem ser diferentes ("seg–sex");
  - a barra de 168 h da semana, que ninguém conseguia ler.

## 9. Identidade do jogo

- **Retrato da pessoa é um brasão** gerado da semente, com o ícone da classe. Nunca rosto desenhado nem nada com cara de feito por IA.
- **Ícones do jogo** (`g-…`) para o que é do mundo: classes, zonas, áreas, missões e acontecimentos. **Lucide** para ações e dados.
- **Moldura única dos emblemas** (`EmblemBox`): degradê na cor do tipo e faixa no topo.
- **Cartões do mapa seguem o padrão das páginas:** cabeçalho com emblema, etiquetas com rótulo e valor, seções com título.
  - São compactos, mas com o essencial: do jogador, o personagem e a sessão.
  - Arrastam pelo cabeçalho e podem ser fixados.

## 10. Conferência antes de mostrar

- [ ] Inventário feito: cada card com a sua pergunta e a sua largura.
- [ ] Capturas em 1920×1080 e em 1366×768, com pelo menos dois jogadores diferentes; no fechamento de etapa, três.
- [ ] Recortes ampliados lidos: texto cortado, valores truncados, rótulo na borda.
- [ ] `.rolagem.txt` vazio em 1920. Em 1366, só rolagem pequena e justificada.
- [ ] Nenhum card largo ou alto com fundo vazio; conteúdo pequeno centrado.
- [ ] Rolar até a última tela: nada cortado em cima.
- [ ] Nenhum dado repetido na mesma página, nem com o cabeçalho.
- [ ] Todo texto em português: nenhum código cru (`session_delayed`) na tela; tipo sem texto mostra uma frase genérica.
- [ ] Plural certo com 1 ("1 jogador", não "1 jogadores").
- [ ] `tools/perf.ps1 -Quick` **uma vez, no fim da entrega**, salvo pedido em contrário.
- [ ] Mostrar ao criador com capturas antes de seguir.

## Erros que já cometemos

| O que foi feito | O que o criador apontou | A regra que ficou |
|---|---|---|
| Visão geral virou um resumo e os atributos foram para uma aba | "parece vazia" | Visão geral é o dossiê completo (seção 7) |
| Cards com rolagem interna em 1920 | não quer rolar dentro do card | `FitList`, ordem certa e páginas (seção 4) |
| Seções esticadas até cobrir a tela | "fica totalmente estranho e sem sentido" | Altura natural; enche só quando precisa (seção 3) |
| Seta ‹ voltando um item por vez | "bugadíssima" | Volta a página inteira (`PreviousOffset`) |
| Retratos com rosto desenhado | "parece coisa feita da IA" | Brasão com o ícone da classe (seção 9) |
| Cartões do mapa pequenos e sem padrão | "básicos e feinhos" | Padrão das páginas, com mais dado útil (seção 9) |
| 3 \| 6 \| 3 em todas as linhas, para alinhar bordas | "para quê? fica estranho" | Largura pelo conteúdo; linhas diferentes (seção 2) |
| Estatísticas em duas telas, com a de cima cortada ao rolar | "fica cortado" | Uma tela quando cabe; última tela no topo (seção 3) |
| Conferência final (etapa 6): eventos crus em inglês, cards quase vazios, área repetindo o cabeçalho, rolagem pesada em 1366 | achado na revisão pelo guia | Itens da lista de conferência (seção 10) e seções 3 e 6 |
