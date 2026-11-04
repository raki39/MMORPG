# Atributos do jogador (D-036)

Os atributos são da **pessoa** (o jogador), não do personagem. O personagem tem classe, nível, itens e status. No combate (M3), os dois entram juntos: dois personagens do mesmo nível e equipamento se saem diferente conforme os atributos de quem joga.

Cada atributo vai de 0 a 1000 no Core e aparece de 1 a 20 na interface. Eles mudam **devagar**, como nos jogos de gestão esportiva: um jogador ativo ganha cerca de 1 ponto da escala por temporada de 2 a 3 meses. Mudanças visíveis geram evento com a causa (`trait_changed`).

## Talento (traço do jogador, fixo)

O talento vem do perfil no conteúdo (`population.json`, campo `talent`), com variação individual. As faixas são:

| Faixa | Talento |
|---|---|
| Noob | < 150 |
| Inexperiente | < 350 |
| Padrão | < 600 |
| Experiente | < 800 |
| Prodígio | < 930 |
| Gênio | ≥ 930 |

Ele define, só para as **capacidades** (Mecânica, Conhecimento, Coordenação):

- **Potencial (teto):** base + 50 + talento × 350 / 1000. Um Noob quase não passa de onde começou; um Gênio pode subir quase 400.
- **Velocidade de aprender:** o custo de prática vai de 0,6× (Gênio) a 1,4× (Noob).

O talento não afeta gostos nem personalidade, que mudam pela experiência e não por talento.

## Limites e ritmo

- **Piso:** base − faixa. A faixa é 250 nas capacidades, 200 nos gostos e 100 na personalidade.
- **Teto:**
  - capacidades: o potencial dado pelo talento;
  - gostos e personalidade: base + faixa.
- **Desaceleração perto do teto:** quanto mais perto do potencial, mais cara a prática, até 3×.
- **Inatividade:**
  - mais de 7 dias sem jogar: −1 por dia na Mecânica;
  - mais de 14 dias sem jogar: −1 por dia no Conhecimento.
  - Isso dá cerca de 1 ponto da escala a cada 50 dias parado. Com as atualizações (M7), o Conhecimento passa a cair por atualização perdida.

## O que cada atributo faz hoje e como muda

| Atributo | Influencia (simulação) | Sobe com | Cai com |
|---|---|---|---|
| PvE | Afinidade com caça e chefe; parte do gosto por missões; peso em objetivos de nível | Caçar com bom encaixe de nível (1 ponto / 360 min) | Repetir muito sem paciência |
| PvP | Afinidade com patrulha e emboscada; no M3, desempenho em PvP | Patrulha e emboscada com gosto; abate de jogador (+3, M3) | Repetição sem paciência |
| Comércio | Afinidade com coleta e comércio; objetivos de praticar | Coleta e comércio com gosto | Repetição sem paciência |
| Exploração | Afinidade com explorar e missões; objetivos de descobrir zonas | Explorar com gosto; 1 ponto a cada 4 zonas descobertas | Repetição sem paciência |
| Socializar | Afinidade com taverna; companhia em outras atividades | Socializar com gosto | Repetição sem paciência |
| Sociabilidade | Valor da companhia na taverna; classe de suporte | Tempo na taverna ou em grupo (1 / 600 min) | — |
| Agressividade | Gosto por desafio acima do nível (fator "desafio"); emboscada; objetivos de nível; classe tanque | Abate de jogador (M3) | — |
| Paciência | Tolera repetição; comprometimento com objetivos; dois objetivos ao mesmo tempo (≥ 450); coleta | — (personalidade) | — |
| Lealdade | Prefere a zona onde estava (fator "zona onde já estava"); comprometimento com objetivos; patrulha; classe tanque | — (personalidade) | — |
| Aversão a risco | Pesa contra risco, PvP aberto e conteúdo acima do nível | — | Enfrentar risco sem perder (1 / 600 min); mortes (M3) a farão subir |
| Mecânica | XP em combate (×0,88–1,12); menos medo do risco; no M3, execução | Combate (1 / 150 min, ajustado por talento e teto) | Mais de 7 dias sem jogar |
| Conhecimento | XP geral (×0,95–1,05); deslocamento pesa menos; decisões mais consistentes (menos ruído); vontade de treinar quando baixo | Jogar (1 / 400 min), explorar (1 / 90 min), zona descoberta (+2), primeira vez num tipo de atividade (+3), missão concluída (+1) | Mais de 14 dias sem jogar |
| Coordenação | Lugares cheios pesam menos na caça e na coleta; chefe; classe tanque; grupos no M6 | Tempo em grupo ou taverna (1 / 240 min) | — |
| Trapaça | Sem efeito até os incidentes (M9) | — | — |
| Oportunismo | Foge mais de lugares disputados; emboscada; incidentes no M9 | — | — |

Os números são hipóteses de calibração e ficam em `TraitTuning` (`Simulation.Core/Behavior/TraitDynamics.cs`).

## Objetivos pessoais

Ao conectar, a pessoa pode adotar um objetivo se tiver vaga: um para todos, dois para quem tem paciência ≥ 450. A chance é de 35% a 65% conforme a paciência. A escolha é ponderada pelos traços (que vêm do perfil):

| Objetivo | Peso | Alvo |
|---|---|---|
| Chegar a um nível | 150 + PvE·0,3 + Agressividade·0,2 + (1000 − Paciência)·0,1 | 1 a 3 níveis acima |
| Descobrir uma zona | 100 + Exploração·0,9 | Uma zona vizinha das conhecidas, dentro do nível (agressivos aceitam zonas mais altas) |
| Concluir uma missão | 120 + (PvE + Exploração)·0,2 + Paciência·0,2 + Conhecimento·0,1 | A principal disponível; senão, uma secundária |
| Praticar a atividade preferida | 100 + gosto·0,5 + Paciência·0,2 | 120 a 420 minutos |

- **Influência:** vira o fator "objetivo pessoal" na decisão: até 200 × comprometimento (400 + Paciência·0,3 + Lealdade·0,3, em permille). O ruído, o gosto, o risco e a distância ainda podem vencer, então não é regra.
- **Abandono:** depois de 3 a 12 horas de jogo sem progresso (conforme a paciência), ou ao conectar, com 4% de chance, se a paciência for < 300.
- **Conclusão:** reforça o traço que motivou o objetivo (+1).
- Tudo registra evento (`goal_set`, `goal_completed`, `goal_abandoned`) com o motivo, e o estado entra no hash.
