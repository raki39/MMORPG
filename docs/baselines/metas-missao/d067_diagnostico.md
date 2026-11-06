# D-067 — diagnóstico das visitas ao quadro e das diárias (base = D-066, 5 seeds × 1.000 × 90 dias)

Saída de `d067_quadro.py` (visitas, deslocamento, diárias, reaceites) e de `d067_ciclos.py` (diárias prontas que venceram; o que mudou entre abandonos repetidos).

```
eventos lidos: 5,624,002; de pessoas sem personagem no ends: 64

## Visitas ao quadro por finalidade e resultado
| finalidade | resultado | visitas | viagem (h) | viagem media (min) |
|---|---|---|---|---|
| aceitar | so aceitou | 97,973 | 30,788 | 18.9 |
| aceitar | sem resultado | 39,458 | 231 | 0.4 |
| aceitar | troca (largou para aceitar) | 13,311 | 4,887 | 22.0 |
| aceitar | interrompida (sessao acabou antes) | 9,026 | 0 | 0.0 |
| entregar | entregou e aceitou | 76,815 | 37,539 | 29.3 |
| entregar | so entregou | 51,043 | 22,926 | 26.9 |
| entregar | interrompida (sessao acabou antes) | 11,953 | 0 | 0.0 |
| entregar | so aceitou | 585 | 267 | 27.4 |
| entregar | troca (largou para aceitar) | 53 | 24 | 26.8 |
| outro | interrompida (sessao acabou antes) | 246 | 0 | 0.0 |
| outro | entregou e aceitou | 86 | 43 | 29.8 |
| outro | so entregou | 15 | 7 | 29.8 |
| outro | troca (largou para aceitar) | 14 | 7 | 28.5 |
| outro | so aceitou | 2 | 1 | 36.7 |
total de viagem ao quadro: 96,719 h em 300,580 visitas

visitas que so aceitaram diarias: 81,895 (viagem 24,242 h); finalidade: {'aceitar': 81320, 'entregar': 575}
visitas com entrega que tambem aceitaram: {'com entrega': 127959, 'com entrega e aceite': 76901}

## Mesmo quadro de novo na mesma sessao (visita anterior -> esta)
  ('sem resultado', 'sem resultado', 'ate 30 min'): 36,537
  ('so aceitou', 'so entregou', 'mais de 30 min'): 28,640
  ('entregou e aceitou', 'entregou e aceitou', 'mais de 30 min'): 5,861
  ('so aceitou', 'entregou e aceitou', 'mais de 30 min'): 5,647
  ('entregou e aceitou', 'so entregou', 'mais de 30 min'): 4,323
  ('so aceitou', 'interrompida (sessao acabou antes)', 'mais de 30 min'): 2,847
  ('troca (largou para aceitar)', 'entregou e aceitou', 'mais de 30 min'): 2,498
  ('entregou e aceitou', 'interrompida (sessao acabou antes)', 'mais de 30 min'): 2,410
  ('sem resultado', 'interrompida (sessao acabou antes)', 'ate 30 min'): 2,137
  ('so entregou', 'so aceitou', 'mais de 30 min'): 1,465
  ('so entregou', 'so entregou', 'mais de 30 min'): 1,401
  ('troca (largou para aceitar)', 'troca (largou para aceitar)', 'ate 30 min'): 995
  ('entregou e aceitou', 'sem resultado', 'ate 30 min'): 825
  ('so entregou', 'interrompida (sessao acabou antes)', 'mais de 30 min'): 778
  ('so aceitou', 'so aceitou', 'mais de 30 min'): 726
  ('troca (largou para aceitar)', 'interrompida (sessao acabou antes)', 'mais de 30 min'): 469
  ('so aceitou', 'sem resultado', 'ate 30 min'): 464
  ('entregou e aceitou', 'so aceitou', 'mais de 30 min'): 303
  ('so entregou', 'entregou e aceitou', 'mais de 30 min'): 288
  ('troca (largou para aceitar)', 'so entregou', 'mais de 30 min'): 281

## Diarias aceitas
destino: {'expirou': 105059, 'entregue': 62762, 'largada': 1708} total 169529
expiradas: {'etapa feita, nao entregue': 24457, 'nenhuma etapa executada': 76148, 'trabalhou sem concluir': 4454}
expiradas sem etapa: tempo ate o reset no aceite: {'mais de 8 h': 33380, 'ate 8 h': 28717, 'ate 3 h': 11539, 'ate 1 h': 2512}
expiradas sem etapa: sessao restante no aceite: {'mais de 60 min': 50528, 'ate 60 min': 13371, 'sessao acaba em ate 30 min': 12210, 'sessao sem fim registrado': 39}
expiradas sem etapa: visita do aceite: {('com entrega', 'entregar'): 26767, ('so aceite', 'aceitar'): 49030, ('so aceite', 'entregar'): 315, ('com entrega', 'outro'): 36}
entregues: visita do aceite: {('com entrega', 'entregar'): 13979, ('so aceite', 'aceitar'): 48256, ('so aceite', 'entregar'): 512, ('com entrega', 'outro'): 15}

## Reaceite da mesma diaria depois de expirar
  ('anterior com progresso', 'visita para entregar'): 9,678
  ('anterior com progresso', 'visita so para aceitar'): 13,423
  ('anterior sem progresso', 'visita para entregar'): 13,519
  ('anterior sem progresso', 'visita so para aceitar'): 50,411
destino das reaceitas: {'entregue': 20831, 'expirou': 65207, 'largada': 993}
anterior -> destino da reaceita: {('com progresso', 'entregue'): 8292, ('com progresso', 'expirou'): 14618, ('sem progresso', 'expirou'): 50589, ('sem progresso', 'largada'): 802, ('sem progresso', 'entregue'): 12539, ('com progresso', 'largada'): 191}
cadeias de expiracoes sem progresso antes do aceite: {0: 105599, 1: 26212, 2: 10515, 3: 5723, 4: 3635, 5: 2535, 6: 1935, 7: 1504, 8: 1227, 9: 1028, 10: 854, 11: 740, 12: 640, 13: 554, 14: 506, 15: 448, 16: 410, 17: 370, 18: 333, 19: 305, 20: 279, 21: 255, 22: 238, 23: 219, 24: 203, 25: 196, 26: 187, 27: 176, 28: 163, 29: 150, 30: 147, 31: 136, 32: 133, 33: 128, 34: 124, 35: 117, 36: 107, 37: 102, 38: 98, 39: 92, 40: 84, 41: 83, 42: 79, 43: 74, 44: 71, 45: 67, 46: 61, 47: 56, 48: 54, 49: 49, 50: 48, 51: 44, 52: 41, 53: 39, 54: 38, 55: 37, 56: 34, 57: 32, 58: 30, 59: 30, 60: 29, 61: 22, 62: 20, 63: 18, 64: 14, 65: 13, 66: 13, 67: 9, 68: 6, 69: 6, 70: 2, 71: 1, 72: 1, 73: 1}

## (1) Diarias com a etapa feita que expiraram sem entrega: 24457
  ('sobrou mais de 40 min de sessao', 'sem outra sessao antes do reset'): 10,855
  ('sessao acabou em 15 a 40 min', 'sem outra sessao antes do reset'): 5,552
  ('sessao acabou em ate 15 min', 'sem outra sessao antes do reset'): 4,615
  ('sobrou mais de 40 min de sessao', 'teve outra sessao antes do reset'): 1,721
  ('sessao acabou em 15 a 40 min', 'teve outra sessao antes do reset'): 882
  ('sessao acabou em ate 15 min', 'teve outra sessao antes do reset'): 815
  ('sem sessao registrada', 'sem outra sessao antes do reset'): 17

## (2) Pares com 3+ abandonos: 254 {'practice_activity': 134, 'reach_level': 99, 'discover_zone': 20, 'complete_quest': 1}
  ('complete_quest', 'missao avancou'): 2
  ('discover_zone', 'subiu de nivel'): 52
  ('practice_activity', 'voltou a praticar'): 331
  ('reach_level', 'nada registrado'): 54
  ('reach_level', 'subiu de nivel'): 182
  practice_activity: abandonos 465; motivos {'no_progress': 384, 'impatience': 81}; dias ate largar (mediana) 2.8; alvos {'socialize': 46, 'trade': 43, 'explore': 23, 'hunt': 18, 'gather': 4}
  reach_level: abandonos 335; motivos {'no_progress': 258, 'impatience': 77}; dias ate largar (mediana) 4.0; alvos {'': 99}
  discover_zone: abandonos 72; motivos {'no_progress': 72}; dias ate largar (mediana) 3.1; alvos {'covil_do_rei_goblin': 20}

Exemplos:
  ('complete_quest', 'missao avancou') forte_secundaria_teias: largou dia 52.0 (nivel 14, no_progress) -> readotou dia 68.1 (16.1 d depois; subiu 0 niveis; escolheu a atividade 0x) -> largou dia 75.0 (nivel 14, no_progress)
  ('complete_quest', 'missao avancou') forte_secundaria_teias: largou dia 75.0 (nivel 14, no_progress) -> readotou dia 84.0 (9.0 d depois; subiu 0 niveis; escolheu a atividade 0x) -> largou dia 89.7 (nivel 14, no_progress)
  ('discover_zone', 'subiu de nivel') covil_do_rei_goblin: largou dia 14.5 (nivel 15, no_progress) -> readotou dia 45.6 (31.1 d depois; subiu 10 niveis; escolheu a atividade 0x) -> largou dia 47.6 (nivel 26, no_progress)
  ('discover_zone', 'subiu de nivel') covil_do_rei_goblin: largou dia 47.6 (nivel 26, no_progress) -> readotou dia 70.8 (23.2 d depois; subiu 4 niveis; escolheu a atividade 0x) -> largou dia 77.9 (nivel 30, no_progress)
  ('practice_activity', 'voltou a praticar') trade: largou dia 5.5 (nivel 9, no_progress) -> readotou dia 8.8 (3.3 d depois; subiu 5 niveis; escolheu a atividade 5x) -> largou dia 12.0 (nivel 15, no_progress)
  ('practice_activity', 'voltou a praticar') trade: largou dia 12.0 (nivel 15, no_progress) -> readotou dia 26.7 (14.8 d depois; subiu 8 niveis; escolheu a atividade 2x) -> largou dia 27.9 (nivel 24, no_progress)
  ('reach_level', 'nada registrado') : largou dia 7.7 (nivel 8, no_progress) -> readotou dia 7.8 (0.2 d depois; subiu 0 niveis; escolheu a atividade 0x) -> largou dia 13.9 (nivel 9, no_progress)
  ('reach_level', 'nada registrado') : largou dia 13.9 (nivel 9, no_progress) -> readotou dia 16.9 (3.0 d depois; subiu 0 niveis; escolheu a atividade 0x) -> largou dia 19.7 (nivel 10, no_progress)
  ('reach_level', 'subiu de nivel') : largou dia 27.5 (nivel 11, no_progress) -> readotou dia 61.8 (34.3 d depois; subiu 13 niveis; escolheu a atividade 0x) -> largou dia 63.7 (nivel 24, no_progress)
  ('reach_level', 'subiu de nivel') : largou dia 63.7 (nivel 24, no_progress) -> readotou dia 85.5 (21.8 d depois; subiu 1 niveis; escolheu a atividade 0x) -> largou dia 87.5 (nivel 25, no_progress)

## praticar atividade: escolhas da atividade entre largar e readotar, e dias de intervalo
 escolhas: {'4 ou mais': 207, '2 a 3': 82, '1 escolha': 42} ; intervalo (dias) mediana 11.2 p10 2.2

## alcancar nivel readotada sem subir de nivel: intervalo (dias)
 n 54 mediana 2.78 p75 4.1
```
