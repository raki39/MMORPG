# Sorteio de dedicação (experimento)

**Situação:** mecanismo aceito (D-053), com influência de 0,45 como referência inicial. Os valores candidatos não foram adotados. O preset principal continua no formato antigo (versão A) até a separação entre Perfil e Preferências. Com o conteúdo antigo, o código novo dá exatamente o mesmo hash final.

A dedicação de uma pessoa sai de três atributos: **importância do jogo**, **sessão normal** e **hábito**. As horas jogadas não são parâmetro; elas saem desses atributos, das janelas livres e das decisões da rotina (`docs/rotina.md`), que este experimento não altera.

## 1. Formato triangular (mínimo, moda, máximo)

No conteúdo, um atributo pode ser escrito de duas formas:

```json
"gameImportance": { "mean": 350, "spread": 150 }              // formato antigo
"gameImportance": { "min": 150, "mode": 300, "max": 800 }     // triangular
```

- **Formato antigo:** média de três sorteios uniformes em [média − dispersão, média + dispersão]. É simétrico, e o desvio padrão fica perto de um terço da dispersão.
- **Triangular:** a densidade sobe em linha reta do mínimo até a moda e desce em linha reta até o máximo. Com essa forma fixada, os três números definem a distribuição por completo.
  - **Moda** é o valor mais comum, o pico da densidade.
  - **Mediana** é o valor que deixa metade das pessoas abaixo e metade acima. Numa triangular assimétrica, as duas são diferentes.
  - Exemplo (150 · 300 · 800): moda 300, mediana 397.

Com `R = máx − mín`, a acumulada é `F(x) = (x − mín)² / (R·(moda − mín))` até a moda e `1 − (máx − x)² / (R·(máx − moda))` depois dela.

O gerador usa a inversa, um valor por posição `u` entre 0 e 1:
- `x = mín + √(u·R·(moda − mín))` se `u < (moda − mín)/R`;
- `x = máx − √((1 − u)·R·(máx − moda))` caso contrário.

Mediana: `mín + √(R·(moda − mín)/2)` quando a moda está na metade de cima, e `máx − √(R·(máx − moda)/2)` quando está na metade de baixo.

**Detalhes do gerador:**
- **Só inteiros:** a posição vai de 0 a 10⁹ − 1, a raiz é inteira (método de Newton) e o valor é arredondado ao inteiro mais próximo.
- **Resolução no topo:** na última posição, o valor fica no máximo a `√(R·(máx − moda)/10⁹)` abaixo do máximo. Isso é zero em todas as faixas de dedicação, porque R não passa de 1.000.
- **Mesmos sorteios do formato antigo:** cada atributo triangular consome três sorteios do fluxo da pessoa (três números de 0 a 999 formam a posição). Assim, trocar o formato não muda nenhum outro atributo de ninguém.
- **Pessoa de referência do cenário controlado:** fica na **mediana**. No formato antigo, ela fica na média, que nesse caso é também a mediana.

## 2. Dedicação compartilhada

Sem ela, os três atributos são sorteados de forma independente. Com ela:

1. Cada pessoa sorteia uma **posição de dedicação** `d`, uniforme entre 0 e 1, num fluxo próprio (`population.dedication`), sem mexer no fluxo dos demais atributos.
2. Cada atributo de dedicação tem a sua posição própria `p`, os três sorteios do item 1.
3. As duas se misturam com o peso `w` da influência (`dedication.influencePermille`, de 0 a 1000): `s = w·d + (1 − w)·p`.
4. `s` não é uniforme: a soma de duas uniformes de larguras `w` e `1 − w` tem distribuição **trapezoidal**. A transformação integral de probabilidade, ou seja aplicar a acumulada `F` do trapézio, devolve uma posição uniforme. Com `a = mín(w, 1−w)` e `b = máx(w, 1−w)`:

   | Trecho | F(s) |
   |---|---|
   | s < a | s² / (2ab) |
   | a ≤ s ≤ b | (s − a/2) / b |
   | s > b | 1 − (1 − s)² / (2ab) |

5. A posição corrigida entra na inversa da triangular do atributo.

**Consequências:**
- **Distribuição de cada atributo:** continua exatamente a triangular declarada, para qualquer influência. A influência só muda como os três se combinam.
- **Influência 0:** a posição é a própria. Os sorteios são independentes, como antes.
- **Influência 1000:** os três atributos ficam na mesma posição, com correlação de postos igual a 1.
- **Influência intermediária:** a dedicação puxa os três na mesma direção, mas cada um mantém uma parte própria. Interesse alto com sessão curta, ou com hábito irregular, continua possível; só fica menos comum que no sorteio independente.

Correlação entre dois atributos e uma combinação "alta num, baixa no outro" (terço superior de um com terço inferior do outro), em 60.000 posições sorteadas:

| Influência | Correlação entre dois atributos | Correlação com a dedicação | Alto num e baixo no outro |
|---|---|---|---|
| 0 | 0,00 | — | 11,0% (independente: 11,1%) |
| 0,40 | 0,28 | 0,53 | 7,3% |
| 0,45 (candidata) | 0,38 | 0,61 | 5,8% |
| 0,50 | 0,49 | 0,70 | 4,2% |
| 0,60 | 0,70 | 0,84 | 1,3% |
| 1 | 1,00 | 1,00 | 0% |

**Regras do validador:**
- Formato triangular: `min ≤ mode ≤ max`, dentro dos limites do atributo e sem `mean`/`spread` junto.
- Influência de 0 a 1000. Acima de 0, os três atributos de dedicação de todos os perfis precisam do formato triangular.
- `routine.slackMinutes` só aceita o formato antigo.

## 3. O que os testes conferem (`Simulation.Tests/TraitDrawTests.cs`)

Os testes estatísticos usam amostra fixa (seed fixa, resultado determinístico) e tolerância de 5 erros padrão da proporção, `√(q(1−q)/N)`. Uma implementação correta passa com probabilidade de cerca de 1 − 6·10⁻⁷ por conferência; um desvio maior é erro real. Não há igualdade exata de frequências.

| Teste | O que confere | Tipo |
|---|---|---|
| Combinação: extremos, continuidade, monotonia e simetria | influências de 0 a 1000; `F(0) = 0`, `F(½) = ½`, `F(1 − s) = 1 − F(s)`, sem salto nas junções do trapézio | exato |
| Influência 0 e 1000 | devolvem exatamente a posição própria e a compartilhada | exato |
| Posição continua uniforme | 200.000 pares, 11 influências, quantis de 0,1% a 99,9% (inclui as caudas) | estatístico |
| Correlação e combinações | mede, sem exigir que andem juntos: correlação perto de 0 com influência 0 e igual a 1 com influência 1000; com 0,45, "alto num e baixo no outro" fica entre 3% e o valor independente | estatístico |
| Triangular: mínimo, máximo, moda, mediana e monotonia | inclui moda num dos extremos e faixa degenerada | exato |
| Triangular segue a acumulada | 19 pontos por distribuição, contando o arredondamento | estatístico |
| Formato não muda os demais atributos | 3.000 pessoas, influências 0 e 450: todos os outros atributos iguais aos da versão A | exato |
| Dentro da faixa, determinístico e mediana na referência | gerador completo | exato |
| Validador | casos válidos e inválidos | exato |

## 4. O experimento

| Versão | Formato | Dedicação compartilhada | Valores |
|---|---|---|---|
| A | antigo | — | atuais (referência) |
| B0 | triangular | não | equivalentes aos atuais |
| B | triangular | 0,45 | equivalentes aos atuais |
| C0 | triangular | não | candidatos |
| C | triangular | 0,45 | candidatos |

- **Valores equivalentes:** mesma moda (= média atual) e mesmo desvio padrão do formato antigo. A meia largura é 0,816 × dispersão, porque o desvio da triangular simétrica é a meia largura/√6 e o do formato antigo é a dispersão/3.
- **B0 mede a troca de formato.** Mesmo com p10, mediana e p90 parecidos, a forma muda: com o mesmo desvio, a triangular tem faixa total menor (±0,816 × dispersão contra ± dispersão) e caudas mais curtas. Na importância do casual (350 ±150), as duas coincidem de p5 a p95, mas o p99,9 é 467 na triangular contra 482 no formato antigo, e o p0,1 é 234 contra 218. Por isso B0 é medido, e não presumido igual a A.
- **Valores candidatos:** as quatro vocações (social, explorador, comerciante, competitivo) com a mesma distribuição; o competitivo não sobe pelo estilo.

  | Atributo | Casual | Vocações | Hardcore | Devoto |
  |---|---|---|---|---|
  | Importância | 150 · 300 · 800 | 250 · 600 · 950 | 500 · 900 · 1000 | 800 · 970 · 1000 |
  | Sessão normal (min) | 30 · 70 · 240 | 50 · 130 · 330 | 90 · 260 · 420 | 180 · 360 · 600 |
  | Hábito | 100 · 350 · 850 | 200 · 600 · 950 | 400 · 850 · 1000 | 650 · 950 · 1000 |

- **Como foi medido:** 1.000 pessoas por execução, 90 dias com 7 de aquecimento, seeds 1 a 15 por versão.
  - As seeds 1 a 5 são a validação padrão, com as regras do cenário.
  - As seeds 6 a 15 só acrescentam dados por pessoa.
  - O tamanho da população é o mesmo da validação, para não mudar a lotação das zonas.
- **Resultados:** `docs/baselines/exp-dedicacao/` (comparação em `comparacao.md`; por perfil e ocupação em `perfil-ocupacao.csv`).
  - **A → B0:** sem diferença de horas fora do ruído.
  - **B0 → B:** medianas iguais, caudas um pouco mais longas.
  - **C0 e C:** a variedade pedida apareceu, mas com mais horas e participação; regras do cenário 6 de 9.
