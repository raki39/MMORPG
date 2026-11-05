# Perfil, Preferências e atributos

**Situação:** adotado (D-054), com os parâmetros candidatos de Casual, Regular, Hardcore e Devoto. Os valores abaixo continuam candidatos: ponto de partida medido, sujeito a ajuste quando houver novas evidências ou sistemas. A estrutura anterior, com sete perfis, fica como referência em `baselines/exp-dedicacao/A`.

## 1. O que é cada coisa

| Camada | O que representa | Onde fica | O que decide |
|---|---|---|---|
| **Perfil** (Casual, Regular, Hardcore, Devoto) | dedicação | campo da pessoa | gera importância do jogo, hábito e sessão normal (D-053: triangular e dedicação compartilhada 0,45) |
| **Preferências** (PvE, PvP, Crafting, Comércio, Coleta, Exploração, Social) | interesses pessoais | campo da pessoa, ao lado do Perfil | o gosto principal nas escolhas, sem obrigar |
| **Especialidade** (PvE, PvP, Comércio, Exploração, Socializar) | afinidade desenvolvida e familiaridade | atributos | reforço mínimo e limitado no gosto; cresce com a prática |
| **Personalidade** (Sociabilidade, Agressividade, Paciência, Lealdade, Aversão a risco, Variedade) | como a pessoa é | atributos | relação com os outros, confronto, tédio, persistência, risco |
| **Conduta** (Competitividade, Trapaça, Oportunismo) | como a pessoa age diante dos outros | atributos | Competitividade: disputar, superar e ser reconhecido |
| **Capacidades** (Mecânica, Conhecimento, Coordenação) e Talento | execução | atributos | rendimento, risco percebido, consistência |
| Rotina individual (horário preferido, tendência a prolongar, autocontrole) | características individuais | atributos | efeitos na rotina iguais aos de antes |

**Princípios:**
- **Sem regra pelo nome:** nenhuma regra de comportamento trata o perfil pelo nome (há um teste que confere).
- **Sem teto de horas por perfil:** as horas saem da disponibilidade e das decisões.
- **A personalidade descritiva** (Pacífico, Agressivo etc.) continua sendo só um rótulo derivado dos atributos.
- **Traits (P4)** serão outro sistema.

## 2. Geração

1. **Perfil:** sorteado pelo peso.
2. **Fluxo principal da pessoa:** os atributos saem na mesma ordem de antes, com os mesmos três sorteios por atributo. Por isso nome, localidade e ocupação de cada pessoa continuam os da estrutura anterior (comparação pareada).
3. **Fluxo próprio** (`population.interests`): as sete preferências e a Competitividade.
4. **Valores:**
   - Os atributos de dedicação usam a posição de dedicação (D-053).
   - As ligações configuráveis (`links`) misturam a posição de uma âncora com a própria. É a mesma transformação da dedicação compartilhada: a distribuição de cada atributo não muda, só a combinação.
5. **Atributos da população** (`traits`): mesma distribuição para todos os perfis.
   - **Valores candidatos:** partem da média e do desvio que a população inteira tinha na estrutura anterior (mistura dos sete perfis). Só some a dependência do perfil.
   - **Formato:** triangular para os que entram em ligações (simétrica: moda = média = mediana, salvo quando o limite corta a faixa); os demais, no formato antigo (média e dispersão).
   - **Corte:** na Agressividade (0 · 320 · 700), o corte em 0 deixa a média em 340 e a mediana em 335, contra 320 antes.

**Ligações candidatas** (influência de 0 a 1000; 0 desliga):

| Alvo | Âncora | Influência | Por quê |
|---|---|---|---|
| Preferência Social | Sociabilidade | 350 | quem é sociável tende a gostar de atividades sociais, sem obrigar |
| Preferência PvP | Agressividade | 300 | o gosto por confronto puxa o PvP |
| Preferência Exploração | Variedade | 300 | quem precisa variar tende a gostar de explorar |
| Especialidade (cada uma) | Preferência do mesmo tema | 500 | a familiaridade costuma acompanhar o gosto |

Competitividade sem ligação: independente do Perfil e das Preferências.

**Preferências candidatas** (triangular de 0 a 1000). **Moda não é média:** a moda é o valor mais comum. Os candidatos foram escolhidos para que a **média** de cada triangular, `(mín + moda + máx) / 3`, ficasse perto do gosto médio que a população tinha. A mediana fica entre as duas.

| Preferência | Moda | Média | Mediana | Média do gosto antes |
|---|---:|---:|---:|---:|
| PvE | 680 | 560 | 583 | 558 |
| PvP | 10 | 337 | 296 | 329 |
| Crafting | 350 | 450 | 430 | — (não existia) |
| Comércio | 170 | 390 | 356 | 384 |
| Coleta | 350 | 450 | 430 | 449 (mistura de comércio 50%, paciência 30%, exploração 20%) |
| Exploração | 520 | 507 | 510 | 497 |
| Social | 610 | 537 | 552 | 528 (Socializar) |

- **Outros triangulares:** especialidade 0 · 250 · 700 (média 317, mediana 303); Competitividade 0 · 250 · 950 (média 400, mediana 373).
- **Dispersão:** é a de uma triangular inteira de 0 a 1000. Antes, gostos compostos (coleta, por exemplo) eram médias de vários atributos e variavam menos. Isso explica parte da mudança na mistura de atividades (seção 4).
- **São parâmetros candidatos.** Esta correção de texto não mudou nenhuma distribuição.

**Perfis candidatos** (mínimo · moda · máximo):

| Perfil | Peso | Importância | Sessão normal (min) | Hábito |
|---|---|---|---|---|
| Casual | 35 | 120 · 280 · 750 | 30 · 65 · 220 | 100 · 320 · 800 |
| Regular | 53 | 220 · 540 · 900 | 45 · 115 · 300 | 180 · 540 · 900 |
| Hardcore | 10 | 550 · 900 · 1000 | 100 · 260 · 420 | 450 · 850 · 1000 |
| Devoto | 2 | 800 · 970 · 1000 | 180 · 360 · 600 | 650 · 950 · 1000 |

O Devoto mantém as ocupações próprias (D-052).

## 3. Dependências migradas

| Onde | Antes | Agora |
|---|---|---|
| Gosto pela atividade (`ActivityScoring.Affinity`) | atributos pve, pvp, trade, exploration e socializing; a coleta misturava comércio, paciência e exploração; o chefe misturava coordenação | preferência do tipo; coleta pela Coleta; chefe pela PvE; no PvP, personalidade e conduta completam (como antes); no treino, a necessidade de aprender (como antes) |
| Reforço da especialidade | — | fator próprio "especialidade": 5% do atributo, teto de 50 na escala de 0 a 1000 da afinidade (`specialtyAffinityPermille`, `specialtyAffinityCap`); só no termo de afinidade |
| Companhia fora da taverna | socializing | preferência Social (na taverna continua a Sociabilidade) |
| Gosto por desafio | agressividade 40% | Competitividade 40% (mecânica e aversão a risco iguais) |
| Classe | dano: pvp e pve; suporte: socializing | dano: preferências PvP e PvE; suporte: preferência Social; tanque igual (agressividade, coordenação, lealdade) |
| Metas | nível: pve + agressividade; nível das zonas aceitas: agressividade; descobrir: exploration; missão: pve + exploration; praticar: pve/trade | preferência PvE + Competitividade; zonas: Competitividade; preferências Exploração e PvE; praticar pela preferência do tipo (motivo `pref_x`) |
| Meta cumprida | reforçava o gosto do motivo | reforça a especialidade do tema (a preferência é estável); coleta e fabricação não têm especialidade |
| Experiência da sessão | diversão: gosto; companhia: socializing; progresso: pve + agressividade + pvp; expectativa: agressividade; novidade: exploration | diversão e companhia pelas preferências; progresso: PvE + Competitividade + PvP; expectativa: Competitividade; novidade: preferência Exploração |
| Tédio | gosto alivia | preferência alivia (mesma regra) |
| Descoberta | curiosidade pela exploration | preferência Exploração |
| Prática | coleta e comércio faziam crescer o comércio | comércio faz crescer o comércio; a coleta não tem especialidade |

**A agressividade continua existindo**, com o efeito de confronto: emboscada, tanque e ligação com a preferência PvP (PK no futuro).

**Competitividade: direção e limites atuais.**
- **Direção:** a vontade de disputar, superar outros e ser reconhecido deve **acompanhar os interesses da pessoa**.
  - Um comerciante competitivo quer ser o melhor no comércio; não precisa buscar nível alto.
  - Quem prefere crafting pode querer ser o melhor artesão.
  - Quem prefere PvP pode lutar sem ligar para rankings.
- **Hoje os efeitos são parciais e só na progressão:** gosto por desafio acima do nível, metas de nível e zonas mais altas aceitas, expectativa de progresso e cuidado com o progresso.
- **Limitação conhecida:** esses efeitos valem para qualquer competitivo, mesmo quem não liga para progressão.
- **O resto entra com os sistemas:** rankings e mercado (M5), fabricação, PvP e chefes (Etapa 3), guildas (M6). A competição passará a seguir o tema das preferências da pessoa.
- **O que ela não decide:** agressividade, PK, habilidade nem horas jogadas.

**Especialidades: só o reforço nas escolhas, por enquanto.**
- **Benefícios de execução ainda não existem.** Hoje a especialidade só dá o pequeno reforço no gosto (fator "especialidade", até 22 pontos contra até 450 da preferência) e cresce com a prática.
- **Benefícios futuros:** serão propostos com o combate, a economia e os grupos, sem repetir o que as capacidades já fazem e sem contar a mesma eficiência de novo na recompensa esperada.

**Sem efeito por enquanto:** Crafting (não há fabricação), benefícios de execução da especialidade e a Competitividade fora da progressão.

## 4. Medição (primeira, com os candidatos)

**Execução e arquivos:**
- 5 seeds × 1.000 pessoas × 90 dias, as mesmas da referência A (`baselines/exp-dedicacao/A`).
- Detalhes em `baselines/perfil-preferencias/analise.md`.
- **A comparação com A não é perfeitamente pareada.**
  - **Iguais por id:** nome e localidade (mesmo fluxo de sorteio).
  - **Ocupação:** 96,6% mantiveram a mesma. Os outros 3,4% viraram ou deixaram de ser devoto, que tem ocupações próprias, com os pesos novos de perfil.
  - **Diferentes para todos:** perfil, dedicação, preferências e atributos, porque a estrutura mudou.
  - **Como ler:** a comparação é entre distribuições com rotina de vida quase igual, não pessoa a pessoa.

**Horas reais por semana na campanha completa, com humor** (p10 · mediana · p90; p99):

| Perfil | Nova | Referência A |
|---|---|---|
| Casual | 3,5 · 8,9 · 18,6; p99 29,5 | 3,8 · 6,7 · 11,0; p99 15,7 |
| Regular (A: as quatro vocações) | 7,6 · 16,5 · 30,9; p99 44,7 | 9,0 · 15,4 · 25,1; p99 34,2 |
| Hardcore | 22,6 · 35,9 · 50,4; p99 63,4 | 26,1 · 36,6 · 49,1; p99 57,6 |
| Devoto | 46,2 · 54,8 · 69,0 | 48,8 · 54,4 · 68,8 |
| População | 5,5 · 14,6 · 35,6 | 5,2 · 12,5 · 31,8 |

- **Sobreposição:** o p90 do casual (18,6 h) passa a mediana do regular (16,5 h); o p10 do hardcore (22,6 h) fica abaixo do p90 do regular (30,9 h).
- **Horas do casual, média × mediana:** são duas medidas e duas rodadas diferentes.

  | Medida | Rodada | Nova | Referência A |
  |---|---|---|---|
  | **Média** por pessoa (meta da validação) | controlada, humor neutralizado (regras do cenário, D-039a) | **9,8 h** (seeds 9,4 a 10,3) | 6,7 h |
  | Média por pessoa | campanha completa, com humor | 10,3 h (seeds 9,8 a 10,8) | 7,1 h |
  | **Mediana** por pessoa (tabela acima) | campanha completa, com humor | **8,9 h** (seeds 8,6 a 9,3) | 6,7 h |

  - A média fica acima da mediana porque a cauda de cima ficou mais longa (p90 18,6 h, p99 29,5 h), e a média sente mais essa cauda.
  - **A meta de 3 a 9 h compara a média da rodada neutra.**
- **Ocupação ainda pesa:** casual em horário comercial vai de 3,1 a 15,7 h; casual sem ocupação, de 8,7 a 29,0 h.

**Preferências:**
- **Fortes (≥ 700) por pessoa:** nenhuma 27%, uma 40%, duas 23%, três ou mais 10%.
- **Predominante:** PvE 26%, Social 20%, Exploração 17%, Coleta 11%, Crafting 11%, Comércio 8%, PvP 7%.
- **Independentes do perfil:** as medianas de cada preferência ficam a até 30 pontos entre casual, regular e hardcore. No devoto, com só 97 pessoas, ficam a até 43, dentro do ruído da amostra.
- **Ligações medidas** (correlação de postos): Social × Sociabilidade 0,47; PvP × Agressividade 0,37; Exploração × Variedade 0,38; especialidade × preferência 0,70 a 0,73 (inclui a prática). Sem ligação, perto de 0.
  - A influência configurada não é a correlação: com 0,35, a correlação entre âncora e alvo fica perto de 0,47.

**Influência real nas escolhas** (parcela do tempo no tipo, pelo terço da preferência; baixo · meio · cima):
- caça e chefe 11% · 33% · 67%;
- coleta 2% · 7% · 36%;
- comércio 0,4% · 2% · 20%;
- exploração 2% · 7% · 26%;
- taverna 3% · 15% · 48%;
- patrulha e emboscada 1% · 2% · 6%.
- **Crafting**, sem efeito, mantém a parcela de coleta e comércio igual nos três terços (23% · 23% · 22%).
- **Especialidade:** vale no máximo 22 pontos na nota, contra até 450 do gosto.

**Efeitos em outras medidas** (contra A):
- **Mistura de atividades:** combate de 42% para 31%; coleta de 5% para 11%; cidade de 22% para 27%; exploração de 7% para 8,5%.
  - **Causa provável:** as preferências têm a dispersão inteira de um gosto. Antes, gostos compostos, como o da coleta, eram médias de três atributos e variavam menos.
- **Evolução mais lenta:** nível 20 em 77 h (A: 70 h); nível 30 em 195 h (A: 173 h).
  - **Causa provável:** o hardcore não nasce mais com conhecimento e mecânica altos, e há menos combate.
- **Regras do cenário: 7 de 9** (A: 9 de 9). As duas divergências, com as medidas por seed, estão em `validacao.md` (seção "D-054"). As metas não foram mudadas; os valores atuais ficam como referência de acompanhamento.
  - Horas do casual: média de 9,8 h na rodada neutra, acima da faixa de 3 a 9 h.
  - Fim de semana ÷ dia útil: instável em 1,3, com uma seed em 1,299.
- **Já estavam fora em A e continuam:** tempo em missões, casuais no nível 10, horas até o nível 20, inativos e desistentes.
- **Competitividade:** sem correlação com nível nem horas. A agressividade correlaciona 0,11 com os minutos de PvP (emboscada e ligação com a preferência PvP).
