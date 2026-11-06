# Validação da simulação

Este documento registra o que foi medido na simulação, contra quais metas e o que os números mostram. Os relatórios completos ficam em `reports/` (fora do Git). Aqui ficam só as conclusões.

## Como rodar

```bash
dotnet run --project Simulation.Runner -- --days 90 --population 1000 --seeds 1-5 --out reports/validacao
```

- As seeds rodam em paralelo.
- O comando gera `validation.md` (metas, perfis, uso do tempo e atividades pouco usadas, com diagnóstico) e `validation.csv` (valor de cada meta em cada seed).
- Uma execução simples (`--seed`) também gera `validation.md` quando o mundo tem metas.

## Metas

As metas ficam em [`Content/presets/inicial/targets.json`](../Content/presets/inicial/targets.json).

- **São hipóteses de design, não regras do mundo.** O Claude propôs as faixas em 04/11/2026, antes de rodar as execuções grandes, para não ajustar a meta ao resultado. O criador pode e deve ajustá-las.
- **Uma meta só conta como "dentro" quando todas as seeds caem na faixa.**
  - Se a média está na faixa mas alguma seed escapa, a meta fica "instável".
- **Medidas semanais ignoram a semana de lançamento** (`warmupDays`).

## Resultado de 04/11/2026

Rodada de 5 seeds × 1.000 pessoas × 90 dias. **9 de 16 metas ficaram dentro da faixa.** A variação entre seeds é pequena (menos de 2 pontos na maioria das medidas), então os desvios são do modelo, não do acaso.

### O que está validado: rotina e calendário

| Medida | Meta | Resultado |
|---|---|---|
| Sessões por semana, casual | 2–4 | 3,1 |
| Sessões por semana, hardcore | 5–7 | 5,4 |
| Horas por semana, casual | 2–6 h | 4,3 h |
| Horas por semana, hardcore | 15–45 h (D-045) | 25,1 h |
| Ativos em dia útil ÷ cadastrados | 30–55% | 52% |
| Ativos no fim de semana ÷ dia útil | 1,3–2,0× | 1,6× |
| Pico online ÷ cadastrados | 15–35% | 29% |

**Teste estatístico do "trabalhador 19h–23h"** (plano 19.4, `ValidationTests`):
- **Configuração:** um perfil com hora preferida 21 ± 2 e variação de ± 75 min.
- **Proporção esperada:** a proporção exata de inícios de sessão entre 19h e 23h, calculada a partir dessa regra, é **96,08%**.
- **Resultado observado:** **96,07%**, em 1.000 pessoas e 8.798 sessões de dia útil (5 seeds).
- **Tolerância:** 4 erros-padrão contados por pessoa, ou seja, ±2,45 pontos.
  - Conta por pessoa e não por sessão porque a hora preferida é fixa para cada pessoa.

Conclusão: quem joga, quando e por quanto tempo segue o que os perfis configuram.

### O que está fora, e por quê

**1. A progressão está cerca de 3 vezes mais lenta que a meta.**
- **Números:** nível 5 com 9,9 h jogadas (meta 2–6 h) e nível 10 com 65 h (meta 10–25 h). Só 27% dos casuais chegam ao nível 10 em 90 dias (meta 40–80%).
- **Causa principal: só 57% do tempo conectado dá XP.**
  - deslocamento: 27%;
  - taverna e comércio, que não dão XP: 15%.
- **Causa secundária: a curva de XP** (60n² + 140n).
- Não é bug; são as duas alavancas para a conferência da fórmula.

**2. Deslocamento ocupa 27% do tempo** (meta até 20%).
- **Zonas grandes:** têm raio de 1,2 km. Ir de uma zona à vizinha leva cerca de 15 minutos a pé (9 km/h).
- **Atividades curtas:** duram de 15 a 30 minutos.
- **Troca frequente:** a penalidade de repetição empurra a pessoa para outra atividade, muitas vezes em outra zona.
- **Alavancas:** atividades mais longas na mesma área, penalidade de repetição menor dentro da mesma zona, ou montaria e velocidade maior (decisão de design).

**3. O quadro de missões da cidade domina as escolhas** (35% das escolhas, 16% do tempo).
- A cidade tem faixa de nível 1–60. Por isso, a missão sempre tem "adequação ao nível" máxima, não exige viagem e não tem risco.
- **Exemplo:** acima do nível 8, caçar na Floresta rende menos XP por minuto (8,4) que pegar missão na cidade (10).
- É falha da fórmula: a missão deveria ter nível, como as zonas.

**4. Pouco tempo em combate** (21%, meta 35–55%).
- Caça contra criaturas: 11%.
- **PvP sem combate de verdade:** emboscada e patrulha somam 10% do tempo, mas ainda não existe combate entre jogadores. Hoje são "XP com risco declarado" sem nenhuma consequência.
- Isso só fica realista com o combate (M3).

**5. A exploração não cansa** (15%, no limite da faixa).
- Explorar continua atraente depois que a pessoa já conhece o mapa inteiro.
- Falta um termo de novidade na nota.

**6. Pouca coleta** (4,5%, meta 8–20%).
- Coleta dá pouco XP (5 por minuto) e o recurso coletado ainda não vale nada, porque a economia só chega no M5.

### Atividades pouco usadas

| Atividade | Escolhas | Diagnóstico |
|---|---|---|
| Enfrentar o Rei Goblin (Covil) | 0% | Exige grupo de 5 e nível 15. Grupos só existem no M6, então hoje ninguém pode fazer. **Não é bug.** |
| Recrutar para grupo (Vila) | 0% | Depende do sistema de grupos (M6). |
| Coletar cristais sombrios (Ruínas) | 0% | Exige nível 10, que só 48% alcançam, numa zona de PvP com perda total. |
| Consertar equipamento (Vila) | 0,3% | Ainda não existe desgaste de equipamento nem economia, então não há motivo para consertar. |

### Bug encontrado durante a validação

A primeira rodada mostrou 10,5% do tempo em "espera sem opção viável". Investigando, ninguém ficava de fato sem opção. O problema era a contagem nova de tempo: quando a atividade terminava junto com a sessão, o tempo era somado duas vezes.

Foi corrigido, e um teste (`TempoJogado_FechaComAsSessoesEAsAtividades`) agora confere que a soma fecha com as sessões.

## Resultado após a progressão no conteúdo (D-027, 04/11/2026)

Mesma rodada: 5 seeds × 1.000 pessoas × 90 dias. **11 de 17 metas dentro da faixa.**

As metas de progressão agora seguem o ritmo A escolhido pelo criador, medido em horas conectadas.

| Medida | Meta | Resultado | Prévia nominal |
|---|---|---|---|
| Horas até o nível 5 | 1–4 h | 2,7 h | 1,9 h |
| Horas até o nível 10 | 8–20 h | 17 h | 10,7 h |
| Horas até o nível 15 | 22–50 h | 41 h (só 42–45% chegaram) | 31 h |
| Casuais no nível 10 ou mais | 60–95% | 96% (acima) | — |

**Por que o resultado fica acima da prévia:**
- A prévia supõe 60% do tempo conectado rendendo XP.
- Na simulação, é menos: a taverna e o quadro de missões não dão mais XP, e o deslocamento é de 23%.

**Divisão do tempo, pior que antes, e por quê:**
- **Taverna com 31% do tempo.** Com o ritmo novo, os personagens passam do conteúdo atual: o nível mediano no fim é 14 para o casual e 19 para o hardcore, mas as caças vão só até o desafio 15.
  - Sem caça adequada, o XP rende pouco, e a taverna vence na nota.
- **Quadro de missões ainda com 23,5% das escolhas,** mesmo sem dar XP. A missão ainda é uma atividade com afinidade alta e sem viagem.
  - O conserto disso é transformá-la em quest estruturada (passo 3 do D-025).
- **Caça com 8,6% e coleta com 2,6%:** os mesmos motivos.

**Conclusão:** o ritmo de progressão está calibrado. O que falta é conteúdo para os níveis que os personagens alcançam e quests de verdade.

## Resultado após o mapa ampliado (D-028, 04/11/2026)

Mesma rodada: 5 seeds × 1.000 pessoas × 90 dias. **13 de 19 metas dentro da faixa.**

**Metas de progressão usadas:**
- Nível 5, 10 e 15: as metas do D-027 (começo mais rápido, pedido do criador).
- Nível 20 e 30: a referência do criador (95 h e 200 h).
- Até a recalibração depois do passo 3, as metas do 10 ao 15 continuam mais rápidas que a referência original de 27 h até o 10.

| Medida | Meta | Resultado | Quem chegou |
|---|---|---|---|
| Horas até o nível 5 | 1–4 h | 2,7 h | 99–100% |
| Horas até o nível 10 | 8–20 h | 14,5 h | 80–84% |
| Horas até o nível 15 | 22–50 h | 36,6 h | 68–70% |
| Horas até o nível 20 | 80–110 h | 73 h (abaixo) | 36–37% |
| Horas até o nível 30 | 170–230 h | 190 h | 12–15% (subestima) |
| Casuais no nível 10 ou mais | 60–95% | 99% (acima) | — |

**Nível mediano no fim, por perfil:**

| Perfil | Nível mediano |
|---|---|
| Hardcore | 30 |
| Competitivo | 28 |
| Explorador | 21 |
| Casual | 16 |
| Comerciante | 15 |
| Social | 7 |

O hardcore chega ao conteúdo final enquanto os casuais ainda ocupam as áreas do meio, como o criador esperava.

**Divisão do tempo:**

| Atividade | Tempo | Situação |
|---|---|---|
| Caça | 28% | Combate total em 38%, dentro da meta |
| Exploração | 18% | Acima: falta o termo de novidade (objetivos, D-026) |
| Taverna | 15% | — |
| Deslocamento | 21% | Pouco acima |
| Missões | 1,7% | Ainda não existem quests de verdade |
| Coleta | 4,5% | O recurso não vale nada até a economia |

**O quadro de missões caiu para menos de 1% das escolhas por cidade** (antes 23,5%).
- Com caça adequada em todos os níveis, a nota neutra do quadro deixou de vencer. Isso confirma o diagnóstico: o quadro ganhava porque as caças erradas para o nível recebiam nota negativa.
- A correção definitiva é o passo 3.

**Atividades sem uso:**
- **Por depender de grupos (M6):** os dois chefes e o recrutamento.
- **Por nível:** coleta e exploração das zonas finais, porque poucos chegam lá.
- **Na Cidadela, os serviços quase não são usados.** Quem está nas zonas finais caça. O sistema de objetivos e as necessidades devem mudar isso.

## Resultado após as quests (D-031, 04/11/2026)

Mesma rodada: 5 seeds × 1.000 pessoas × 90 dias. **15 de 21 metas dentro da faixa.**

| Medida | Meta | Resultado |
|---|---|---|
| XP vindo de quests | 15–35% | 19,6% |
| Quests concluídas por personagem | 4–30 | 21 |
| Horas até o nível 10 | 8–20 h | 14,9 h |
| Horas até o nível 20 | 80–110 h | 73 h (abaixo) |
| Horas até o nível 30 | 170–230 h | 192 h (14–17% chegaram) |
| Tempo em combate | 35–55% | 32,8% (abaixo) |
| Tempo andando ou esperando | 5–20% | 30,2% (acima; antes das quests, 22%) |

**O ciclo da quest anda por natureza:** ir ao quadro, ir à etapa (muitas vezes em outra zona) e voltar para entregar.
- Medimos três calibrações de peso; a escolhida está no meio-termo (ver D-031).
- A redução virá do plano da sessão e da continuidade (D-026): fazer as etapas onde a pessoa já está e entregar quando passar pela cidade.
- Não foi usada compensação artificial.

**Achado da ficha da quest:** com peso fixo de entrega, muitos personagens carregavam quests "prontas para entregar" por dias.
- O peso de entrega passou a crescer com o número de quests prontas.

## Resultado no fechamento do M2 (D-037 a D-039, 04/11/2026)

90 dias × 1.000 pessoas × 5 seeds: **14 de 24 metas** dentro da faixa (3 metas novas de retenção).

- **Continuidade (D-037):**
  - escolhas que continuam a mesma atividade: 50% → 71%;
  - tempo andando: 31% → 24%;
  - combate: 35% → 40%;
  - exploração: 13% → 7%.
- **Progressão:** horas até o 30 caíram para 162 (anda-se menos). A recalibração fica para depois do combate (D-028, escolha a ou b).
- **Humor (D-038):**
  - casuais e exploradores satisfeitos;
  - 98% dos hardcore insatisfeitos no dia 90, com a novidade esgotada (nível 30 e mundo conhecido);
  - competitivos vêm logo atrás.
- **Retenção (D-039):**
  - desistem 2,5% (abaixo da meta de 8–30%), quase todos hardcore e competitivos, no dia mediano 78 e no nível 30, por falta de novidade e tédio;
  - casuais quase não desistem porque ainda faltam as causas: mortes (M3), economia (M5) e social (M6).
- **Seções novas do relatório:** continuidade e motivos de troca, humor e memória por perfil, retenção e motivos de desistência, evolução dos atributos.

## Revisão D-039a (04/11/2026)

- **Metas por classe:**
  - **regras do cenário:** 7 de 7, rotina medida com o humor neutralizado;
  - **provisórias:** 8 de 12;
  - **dependentes de sistemas futuros:** acompanhamento.
- **162 h até o nível 30:** resultado de referência depois do D-037. Não se compensa agora; a recalibração rumo a cerca de 200 h vem depois do combate.
- **Novidade em meses:**
  - no dia 90 ninguém está insatisfeito;
  - em 180 dias a novidade dos hardcore cai a 213, com satisfação de 542;
  - a falta de conteúdo pesa de verdade só depois de 7 a 8 meses sem atualização.

## R5: metas da rotina revistas (D-052, 05/11/2026)

Com a vontade de jogar da v3 (D-051), a pessoa joga, para e volta. A meta antiga "sessões por semana" foi escrita quando havia uma sessão por dia e, na prática, media dias de jogo. Na R5 as medidas foram separadas: **dias de jogo por semana**, **sessões por dia de jogo** e **horas por semana**. Cada faixa foi revista pelo significado da medida, não para acomodar o resultado.

| Meta | Antes (até a R4) | R5 | Por quê |
|---|---|---|---|
| Casual: sessões por semana 2–4 | `sessions_per_week` | **Dias de jogo por semana 2–4** (`play_days_per_week`) | Mesma intenção (cerca de 3 dias); a medida agora conta dias, não voltas |
| Hardcore: sessões por semana 5–7 | `sessions_per_week` | **Dias de jogo por semana 5–7** | Mesma intenção (cerca de 6 dias) |
| — | — | **Casual: sessões por dia de jogo 1,1–2,0** (`sessions_per_play_day`) | Nova. Uma sessão na maioria dos dias e voltas comuns no dia livre; abaixo de 1,1 a volta quase não existiria |
| — | — | **Hardcore: sessões por dia de jogo 1,2–2,5** | Nova. Volta mais quando há tempo; a sessão normal longa limita quantas cabem |
| Casual: horas por semana 2–6 h | "3 sessões de 70 min" | **3–9 h** | Cerca de 3 dias de jogo; no dia livre, 1 ou 2 sessões de 1h30 com pausa (comportamento da v3, que o criador considerou coerente em 05/11/2026; ainda sem avaliação numa experiência jogável) |
| Hardcore: horas por semana 15–45 h | — | sem mudança | — |
| Ativos em dia útil, fim de semana, pico online | — | sem mudança | Medem a população no dia, não as sessões |

**Resultados registrados** (regras do cenário, média das 5 seeds):

| Medida | pré-R4 (R3) | R4 (D-050) | v3 (D-051) | R5 (D-052) |
|---|---:|---:|---:|---:|
| Casual: sessões por semana | 2,8 | 2,8 | 4,2 | 4,2 |
| Casual: dias de jogo por semana | — | — | — | 2,8 |
| Casual: sessões por dia de jogo | — | — | — | 1,49 |
| Casual: horas por semana | 5,5 | 5,4 | 6,8 | 6,7 |
| Hardcore: sessões por semana | 6,5 | 6,6 | 9,5 | 9,4 |
| Hardcore: dias de jogo por semana | — | — | — | 6,2 |
| Hardcore: sessões por dia de jogo | — | — | — | 1,52 |
| Hardcore: horas por semana | 36,8 | 31,9 | 36,8 | 36,5 |
| Regras do cenário dentro | 7 de 7 | 7 de 7 | 4 de 7 | 9 de 9 |

Os relatórios completos de cada versão estão em `docs/baselines/` (`pre-R4`, `pos-R4`, `pos-R4b`, `pos-R5`, `pos-R5-devoto`).

**Com o perfil Devoto** (D-052, cerca de 2% da população): regras do cenário 9 de 9.
- **Outros perfis com pessoas iguais:** idênticos no cenário controlado.
- **Campanha completa:** as horas por perfil ficaram iguais à R5 dentro de 0,1 h.
- **O Devoto:** 6,9 dias, 1,52 sessão por dia de jogo e 57 h por semana. Ainda sem meta própria.

## D-054: Perfil, Preferências e atributos separados (05/11/2026)

A estrutura nova foi adotada com os parâmetros candidatos (`perfil-preferencias.md`). Validação padrão: 5 seeds × 1.000 pessoas × 90 dias. **Regras do cenário: 7 de 9.** Nenhuma meta mudou; os valores abaixo ficam como referência para acompanhamento.

**Divergência 1 — horas por semana do casual** (meta de 3 a 9 h; média por pessoa na rodada controlada com humor neutralizado):

| Seed | 1 | 2 | 3 | 4 | 5 | Média |
|---|---:|---:|---:|---:|---:|---:|
| D-054 | 9,77 | 10,28 | 9,42 | 9,67 | 9,81 | 9,79 |
| Referência A (antes) | 6,70 | 6,77 | 6,75 | 6,66 | 6,64 | 6,70 |

- **Causa:** os candidatos do Casual (importância, hábito e sessão mais altos e mais espalhados) alongaram a cauda de cima. O mesmo efeito apareceu no experimento do D-053 (versões C0 e C).
- **Mediana da campanha completa:** 8,9 h, contra 6,7 h em A.
- **Média da campanha completa:** 10,3 h.

**Divergência 2 — ativos no fim de semana ÷ ativos em dia útil** (meta de 1,3 a 2,0×):

| Seed | 1 | 2 | 3 | 4 | 5 | Média |
|---|---:|---:|---:|---:|---:|---:|
| D-054 | 1,317 | **1,299** | 1,309 | 1,321 | 1,313 | 1,312 |
| Referência A (antes) | 1,346 | 1,322 | 1,313 | 1,324 | 1,329 | 1,327 |

- **Leitura:** já ficava perto do piso em A, e uma seed ficou abaixo de 1,3. Causa provável, não medida à parte: com hábito mais alto em parte da população, os dias úteis ganharam jogadores.

**Outras mudanças registradas** (faixas provisórias, sem mudança de meta):
- **Mistura de atividades:** combate 31,3% (A: 42,1%), coleta 11,4% (5,2%), cidade 26,8% (22,3%), exploração 8,5% (6,9%). A mistura nova fica preservada por decisão do criador.
- **Progressão:** nível 15 em 40,2 h (36,4 h), nível 20 em 77,3 h (69,6 h), nível 30 em 195 h (173 h).
- **Já estavam fora em A e continuam:** tempo em missões, casuais no nível 10, horas até o nível 20, inativos e desistentes.

Relatórios em `docs/baselines/perfil-preferencias/`.

## D-055: progressão inicial e metas de desbloqueio (05/11/2026)

Medição em quatro versões com as mesmas pessoas (`baselines/progressao-inicial/`). Nenhuma meta mudou.

- **Regras do cenário:** 7 de 9 em todas. As duas divergências do D-054 continuam iguais (horas do casual 9,8 h; fim de semana instável em 1,3).
- **Mudanças provisórias (V0 → V3):**
  - horas até o nível 10: 15,8 → 15,2;
  - casuais no nível 10 ou mais: 95,3% → 97,5% (já fora);
  - XP de missões: 19,5% → 17,7%;
  - combate: 31,3% → 32,9%;
  - cidade: 26,8% → 24,4%;
  - andando ou esperando: 20,2% → 19,9%.
- **Com a correção do ciclo das metas (V3b, preset final):**
  - provisórias 8 de 12 ("andando ou esperando" 19,4%, dentro);
  - XP de missões 16,8%;
  - casuais no nível 10 ou mais 96,9%;
  - regras do cenário 7 de 9, sem mudança.

## D-056: personalidade principal (05/11/2026)

Medição com as mesmas pessoas, sem e com as regras (`baselines/personalidade/`). Nenhuma meta mudou.

- **Regras do cenário:** 7 de 9, iguais. As duas divergências do D-054 continuam (horas do casual 9,8 h; fim de semana em 1,3).
- **Provisórias:** 8 de 12 nas duas versões, mas trocaram:
  - **cidade:** 24,3% → 23,1%, entrou na faixa;
  - **andando ou esperando:** 19,4% → 20,3%, saiu (limite de 20%).
- **Divergência registrada, andando ou esperando:**
  - **De onde vem:** Inquieto 21% → 32%, Paciente 19% → 24%, Versátil 21% → 26%. Agressivo, Pacífico e Metódico andam menos.
  - **Por quê:** é o efeito próprio dessas personalidades (trocar de lugar, ir às etapas de missão, começar a sessão em outro lugar), não ciclo vazio.
  - **Decisão do criador:** não enfraquecer personalidades só para a meta provisória passar.
- **Outras:** combate 33,0% → 33,7%; XP de missões 16,8% → 17,5%; horas até o nível 20 77,0 → 78,0 (já fora).
- **Inquieto em minutos (P2):** andando ou esperando 20,3% → 20,2% (ainda fora); o Inquieto anda 28,1% (era 31,9%).

## D-057: traits (05/11/2026)

Com e sem traits, as mesmas pessoas (`baselines/traits/`). Os efeitos ficam desligados até a P4c, então a simulação precisa ser idêntica, e é: `validation.csv` igual e nenhuma coluna comum do `people.csv` diferente. A distribuição das traits está em `traits.md`, seção 5.

## D-058: variedade de horários (05/11/2026)

Antes × depois com as mesmas pessoas (`baselines/horarios/`). Nenhuma meta mudou.

- **Regras do cenário:** 8 de 9 (antes, 7). O fim de semana deixou de ser instável; as horas do casual continuam fora (10,0 h).
- **Provisórias:** 7 de 12. "Tempo na cidade" (23,6%) e "andando ou esperando" (20,0%) ficaram instáveis no limite; os valores praticamente não mudaram.
- **Madrugada:** a presença por hora local está em `horarios.md`, seção 7. Não há meta de presença mínima na madrugada, por decisão do criador.

## D-059: efeitos das traits (05/11/2026)

Efeitos desligados × ligados, com as mesmas pessoas (`baselines/traits-efeitos/`). Nenhuma meta mudou.

- **Regras do cenário:** 8 de 9, iguais.
- **Provisórias:** 8 de 12 (antes, 7):
  - "tempo na cidade" voltou para dentro: 23,6% → 22,8%;
  - "andando ou esperando" foi de 20,0% para 20,5%, pelo Colecionador, pelo Impulsivo e pela espera do Indeciso. Fica registrado, sem enfraquecer as traits.
- **Outras:**
  - XP de missões: 17,4% → 16,6%;
  - horas até o nível 20: 77,9 → 78,4 (já fora);
  - horas por semana do hardcore: 37,4 → 37,1.

## D-060: aquisição e perda de traits (05/11/2026)

Aquisição desligada × ligada, com as mesmas pessoas (`baselines/traits-aquisicao/`). Regras do cenário 8 de 9 e provisórias 8 de 12, iguais; diferenças de até 0,6 ponto. A aquisição desligada é idêntica à rodada da P4c: registrar evidência não muda a simulação.

## Próximos passos

Roteiro por etapas (backlog):
1. Etapa 1 — personagem (classes, atributos do personagem, itens iniciais, inventário e ouro).
2. Etapa 2 — combate PvE (M3) e recalibração da curva com a escolha do criador (D-028: (a) cerca de 200 h com desaceleração suave ou (b) desaceleração forte com 220–250 h).
3. Etapa 3 — PvP e chefe.
4. Revisar com o criador as metas de retenção quando mortes, economia e social existirem.
