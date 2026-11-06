# D-067 — comparação nas mesmas pessoas (5 seeds × 1.000 × 90 dias)

Base = D-066 (tudo desligado); v1 = defeitos (visita coerente e bloqueio da meta de nível); v12 = + aceite avaliado; v123 = + prazo da diária pronta (preset). Gerado por `d067_comparar.py` sobre as variantes de `d067_variantes.py`.

| Medida | Base (D-066) | v1 defeitos | v12 + aceite avaliado | v123 + prazo da pronta |
|---|---|---|---|---|
| visitas ao quadro | 300580 | 270268 | 154582 | 166200 |
|   aceitar: so aceitou | 97973 | 102234 | 47649 | 47925 |
|   aceitar: troca | 13311 | 20542 | 2949 | 2964 |
|   aceitar: sem resultado | 39458 | 0 | 94 | 97 |
|   aceitar: interrompida | 9026 | 6571 | 2792 | 2788 |
|   entregar: entregou e aceitou | 76815 | 76652 | 55842 | 59008 |
|   entregar: so entregou | 51043 | 51413 | 35278 | 40280 |
|   entregar: interrompida | 11953 | 11890 | 9417 | 10975 |
|   visitas sem resultado logo apos outra sem resultado | 36673 | 0 | 0 | 0 |
| viagem ao quadro (h) | 96719 | 96917 | 60207 | 63910 |
|   para aceitar (h) | 35905 | 35964 | 16619 | 16130 |
|   para entregar (h) | 60756 | 60890 | 43527 | 47452 |
|   das para aceitar: com troca (liberar vaga) (h) | 4917 | 3880 | 1190 | 1201 |
| visitas que so aceitaram diarias | 81895 | 93292 | 20746 | 21726 |
|   viagem delas (h) | 24242 | 24592 | 6182 | 6033 |
| diarias aceitas | 169529 | 169587 | 58409 | 60182 |
|   entregues | 62762 | 62675 | 27305 | 35480 |
|   expiradas sem etapa executada | 76148 | 76064 | 14012 | 14469 |
|   expiradas com a etapa feita (nao entregues) | 24457 | 24740 | 14913 | 7988 |
|   expiradas com trabalho parcial | 4454 | 4459 | 1656 | 1725 |
|   largadas (troca) | 1708 | 1649 | 523 | 520 |
| reaceite da mesma diaria apos expirar intocada | 63930 | 63873 | 3306 | 3415 |
| reaceite apos expirar com progresso | 23101 | 23271 | 12395 | 7090 |
| missoes main concluidas | 41150 | 41147 | 41246 | 40941 |
| missoes secondary concluidas | 55802 | 56140 | 47619 | 48058 |
| missoes daily concluidas | 62762 | 62675 | 27305 | 35480 |
|   secundarias concluidas, etapa de hunt | 34454 | 34666 | 33275 | 33553 |
|   secundarias concluidas, etapa de gather | 21348 | 21474 | 14344 | 14505 |
|   secundarias concluidas, etapa de outro | 0 | 0 | 0 | 0 |
| missoes largadas por troca | 28017 | 24746 | 4384 | 4487 |
| aceites por motivo: sem motivo registrado | 299314 (100%) | 296395 (100%) | — | — |
| aceites aproveitando a viagem de outra missao | 0 | 0 | 35797 | 39082 |
| metas de missao: % concluidas | 86.3 | 86.9 | 88.1 | 87.9 |
| mesmo alvo abandonado 3+ vezes (pares) | 254 | 212 | 231 | 238 |
|   por tipo | complete_quest:1 discover_zone:20 practice_activity:134 reach_level:99 | complete_quest:2 discover_zone:28 practice_activity:132 reach_level:49 unlock_level:1 | discover_zone:54 practice_activity:99 reach_level:76 unlock_level:2 | complete_quest:1 discover_zone:53 practice_activity:108 reach_level:74 unlock_level:2 |
| meta de nivel readotada sem subir de nivel | 163 | 0 | 0 | 0 |
| nivel em 90 dias | 25.2 | 25.2 | 25.0 | 24.8 |
| satisfacao | 600 | 600 | 587 | 588 |
| frustracao | 1.2 | 1.2 | 1.5 | 1.4 |
| engajamento | 707 | 708 | 698 | 699 |
| h jogadas/semana | 18.53 | 18.53 | 18.39 | 18.37 |
| metas abandonadas/pessoa | 1.46 | 1.42 | 1.37 | 1.39 |
| % desistiu do jogo | 0.8 | 0.7 | 0.7 | 0.7 |
| % tempo quest | 2.7 | 2.5 | 1.8 | 1.9 |
| % tempo hunt | 25.9 | 25.9 | 29.3 | 28.9 |
| % tempo gather | 11.6 | 11.5 | 12.3 | 12.1 |
| % tempo explore | 7.9 | 7.9 | 8.9 | 8.7 |
| % tempo socialize | 14.8 | 14.9 | 16.7 | 16.8 |
| % tempo trade | 5.2 | 5.2 | 5.7 | 5.8 |
| % tempo idle | 0.3 | 0.3 | 0.3 | 0.3 |
| % andando ou esperando | 24,8 | 24,9 | 20,1 | 20,7 |
| missoes concluidas por personagem | 31,9 | 32,0 | 23,2 | 24,9 |
| % XP vindo de missoes | 25,8 | 25,8 | 23,5 | 24,0 |
| validacao | Regras do cenário: 8 de 9 · Provisórias: 7 de 12 | Regras do cenário: 8 de 9 · Provisórias: 7 de 12 | Regras do cenário: 8 de 9 · Provisórias: 7 de 12 | Regras do cenário: 8 de 9 · Provisórias: 7 de 12 |
| aceites por motivo: goal | — | — | 9726 (6%) | 9492 (6%) |
| aceites por motivo: reward | — | — | 58125 (37%) | 57991 (36%) |
| aceites por motivo: campaign | — | — | 3700 (2%) | 3948 (2%) |
| aceites por motivo: unlocks | — | — | 2365 (2%) | 2590 (2%) |
| aceites por motivo: practice_goal | — | — | 7314 (5%) | 7838 (5%) |
| aceites por motivo: taste | — | — | 75731 (48%) | 77180 (49%) |

## Ciclos repetidos que restam (v123)

```
## (1) Diarias com a etapa feita que expiraram sem entrega: 7988
  ('sobrou mais de 40 min de sessao', 'sem outra sessao antes do reset'): 2,549
  ('sessao acabou em 15 a 40 min', 'sem outra sessao antes do reset'): 2,445
  ('sessao acabou em ate 15 min', 'sem outra sessao antes do reset'): 2,262
  ('sessao acabou em ate 15 min', 'teve outra sessao antes do reset'): 298
  ('sessao acabou em 15 a 40 min', 'teve outra sessao antes do reset'): 217
  ('sobrou mais de 40 min de sessao', 'teve outra sessao antes do reset'): 214
  ('sem sessao registrada', 'sem outra sessao antes do reset'): 3

## (2) Pares com 3+ abandonos: 238 {'practice_activity': 108, 'discover_zone': 53, 'reach_level': 74, 'unlock_level': 2, 'complete_quest': 1}
  ('complete_quest', 'missao avancou'): 2
  ('discover_zone', 'subiu de nivel'): 148
  ('practice_activity', 'voltou a praticar'): 248
  ('reach_level', 'subiu de nivel'): 170
  ('unlock_level', 'nada registrado'): 4
  practice_activity: abandonos 356; motivos {'no_progress': 306, 'impatience': 50}; dias ate largar (mediana) 3.0; alvos {'trade': 49, 'socialize': 41, 'explore': 9, 'hunt': 6, 'gather': 3}
  reach_level: abandonos 244; motivos {'no_progress': 184, 'impatience': 60}; dias ate largar (mediana) 3.0; alvos {'': 74}
  discover_zone: abandonos 201; motivos {'no_progress': 201}; dias ate largar (mediana) 3.1; alvos {'covil_do_rei_goblin': 52, 'mata_dos_espinhos': 1}

Exemplos:
  ('complete_quest', 'missao avancou') cidadela_principal_1: largou dia 24.6 (nivel 21, no_progress) -> readotou dia 27.5 (2.9 d depois; subiu 1 niveis; escolheu a atividade 0x) -> largou dia 28.6 (nivel 22, no_progress)
  ('complete_quest', 'missao avancou') cidadela_principal_1: largou dia 28.6 (nivel 22, no_progress) -> readotou dia 61.0 (32.4 d depois; subiu 6 niveis; escolheu a atividade 0x) -> largou dia 62.8 (nivel 29, no_progress)
  ('discover_zone', 'subiu de nivel') covil_do_rei_goblin: largou dia 8.7 (nivel 15, no_progress) -> readotou dia 12.0 (3.3 d depois; subiu 2 niveis; escolheu a atividade 0x) -> largou dia 13.7 (nivel 19, no_progress)
  ('discover_zone', 'subiu de nivel') covil_do_rei_goblin: largou dia 13.7 (nivel 19, no_progress) -> readotou dia 14.7 (1.0 d depois; subiu 1 niveis; escolheu a atividade 0x) -> largou dia 15.9 (nivel 22, no_progress)
  ('practice_activity', 'voltou a praticar') trade: largou dia 3.8 (nivel 9, no_progress) -> readotou dia 12.8 (9.0 d depois; subiu 7 niveis; escolheu a atividade 25x) -> largou dia 16.8 (nivel 19, no_progress)
  ('practice_activity', 'voltou a praticar') trade: largou dia 16.8 (nivel 19, no_progress) -> readotou dia 45.6 (28.7 d depois; subiu 9 niveis; escolheu a atividade 49x) -> largou dia 51.8 (nivel 29, no_progress)
  ('reach_level', 'subiu de nivel') : largou dia 18.6 (nivel 11, no_progress) -> readotou dia 41.0 (22.4 d depois; subiu 5 niveis; escolheu a atividade 0x) -> largou dia 53.9 (nivel 16, no_progress)
  ('reach_level', 'subiu de nivel') : largou dia 53.9 (nivel 16, no_progress) -> readotou dia 62.5 (8.6 d depois; subiu 1 niveis; escolheu a atividade 0x) -> largou dia 66.8 (nivel 17, no_progress)
  ('unlock_level', 'nada registrado') planalto_dos_ventos/explorar_planalto: largou dia 36.6 (nivel 13, no_progress) -> readotou dia 39.6 (3.0 d depois; subiu 1 niveis; escolheu a atividade 0x) -> largou dia 46.8 (nivel 14, no_progress)
  ('unlock_level', 'nada registrado') planalto_dos_ventos/explorar_planalto: largou dia 46.8 (nivel 14, no_progress) -> readotou dia 58.7 (11.9 d depois; subiu 1 niveis; escolheu a atividade 0x) -> largou dia 62.7 (nivel 15, no_progress)

## praticar atividade: escolhas da atividade entre largar e readotar, e dias de intervalo
 escolhas: {'4 ou mais': 179, '2 a 3': 52, '1 escolha': 17} ; intervalo (dias) mediana 13.7 p10 3.2

## alcancar nivel readotada sem subir de nivel: intervalo (dias)
```
