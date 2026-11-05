# Cenário controlado

Simulador 0.1.0+fc3cbb1bc1eb3f5727e09cc0225af07a153bb8e2 · seeds 1, 2, 3, 4, 5 · 60 dias (7 de aquecimento) · 50 pessoas por célula.

Cada célula tem pessoas iguais: perfil e ocupação fixos, traços e rotina na média do conteúdo, residência na localidade do servidor. Humor neutro: sem extensões, inatividade nem desistência. O que muda entre as pessoas é só o sorteio do dia a dia, então as diferenças entre células e entre versões vêm das regras, não da mistura da população.

| Perfil | Ocupação | h/semana | no trabalho | dias com jogo | dias com oportunidade | 2+ sessões | tempo livre usado | sessão p50 | p90 | máx. | planejado dia livre | planejado dia útil |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| casual | sem_ocupacao | 11,3 | 0,0 | 57,7% | 100,0% | 43,7% | 10,0% | 89 | 141 | 290 | 80 | — |
| casual | escala_5x2 | 4,8 | 0,0 | 32,4% | 100,0% | 21,9% | 10,4% | 87 | 123 | 294 | 80 | 80 |
| casual | meio_periodo_manha | 8,7 | 0,0 | 47,3% | 100,0% | 43,3% | 11,1% | 89 | 142 | 320 | 80 | 80 |
| casual | home_office | 6,7 | 0,4 | 43,2% | 100,0% | 37,2% | 10,3% | 87 | 140 | 300 | 80 | 80 |
| casual | turno_noturno | 5,7 | 0,0 | 36,5% | 100,0% | 31,1% | 9,8% | 87 | 140 | 294 | 80 | 80 |
| casual | trabalho_estudo | 3,2 | 0,0 | 19,9% | 46,7% | 37,1% | 9,9% | 88 | 141 | 299 | 80 | 80 |
| social | sem_ocupacao | 20,0 | 0,0 | 71,0% | 100,0% | 51,1% | 18,0% | 122 | 181 | 413 | 110 | — |
| social | escala_5x2 | 9,0 | 0,0 | 45,0% | 100,0% | 22,9% | 19,5% | 120 | 175 | 360 | 110 | 110 |
| social | meio_periodo_manha | 16,4 | 0,0 | 61,1% | 100,0% | 51,3% | 20,7% | 122 | 181 | 386 | 110 | 110 |
| social | home_office | 12,7 | 0,7 | 57,3% | 100,0% | 47,3% | 19,4% | 120 | 177 | 358 | 110 | 110 |
| social | turno_noturno | 10,7 | 0,0 | 48,5% | 100,0% | 38,3% | 18,3% | 119 | 177 | 446 | 110 | 110 |
| social | trabalho_estudo | 5,6 | 0,0 | 24,9% | 46,7% | 41,3% | 17,0% | 121 | 179 | 355 | 110 | 110 |
| comerciante | sem_ocupacao | 22,2 | 0,0 | 81,5% | 100,0% | 56,3% | 20,3% | 104 | 165 | 432 | 90 | — |
| comerciante | escala_5x2 | 10,5 | 0,0 | 56,9% | 100,0% | 26,4% | 22,8% | 102 | 159 | 372 | 90 | 90 |
| comerciante | meio_periodo_manha | 18,6 | 0,0 | 71,0% | 100,0% | 56,3% | 23,0% | 104 | 164 | 399 | 90 | 90 |
| comerciante | home_office | 14,8 | 0,7 | 70,5% | 100,0% | 52,0% | 23,4% | 102 | 161 | 395 | 90 | 90 |
| comerciante | turno_noturno | 13,1 | 0,0 | 59,4% | 100,0% | 49,3% | 22,1% | 102 | 161 | 452 | 90 | 90 |
| comerciante | trabalho_estudo | 6,4 | 0,0 | 28,9% | 46,7% | 50,8% | 19,2% | 102 | 161 | 334 | 90 | 90 |
| explorador | sem_ocupacao | 24,8 | 0,0 | 75,6% | 100,0% | 54,7% | 22,3% | 132 | 193 | 430 | 120 | — |
| explorador | escala_5x2 | 11,1 | 0,0 | 49,4% | 100,0% | 22,2% | 23,8% | 130 | 186 | 454 | 120 | 120 |
| explorador | meio_periodo_manha | 19,6 | 0,0 | 63,8% | 100,0% | 54,6% | 24,5% | 131 | 192 | 411 | 120 | 120 |
| explorador | home_office | 15,5 | 0,8 | 62,5% | 100,0% | 48,4% | 23,5% | 128 | 188 | 519 | 120 | 120 |
| explorador | turno_noturno | 12,4 | 0,0 | 54,4% | 100,0% | 32,1% | 21,4% | 128 | 186 | 494 | 120 | 120 |
| explorador | trabalho_estudo | 7,1 | 0,0 | 27,2% | 46,7% | 49,9% | 21,9% | 130 | 190 | 420 | 120 | 120 |
| competitivo | sem_ocupacao | 32,2 | 0,0 | 85,2% | 100,0% | 55,5% | 28,8% | 164 | 243 | 600 | 150 | — |
| competitivo | escala_5x2 | 15,8 | 0,0 | 62,9% | 100,0% | 18,7% | 34,5% | 161 | 213 | 459 | 150 | 150 |
| competitivo | meio_periodo_manha | 27,2 | 0,0 | 75,8% | 100,0% | 57,0% | 34,1% | 164 | 244 | 576 | 150 | 150 |
| competitivo | home_office | 21,1 | 0,9 | 73,9% | 100,0% | 45,3% | 32,5% | 160 | 240 | 630 | 150 | 150 |
| competitivo | turno_noturno | 18,6 | 0,0 | 64,8% | 100,0% | 40,0% | 31,8% | 161 | 240 | 485 | 150 | 150 |
| competitivo | trabalho_estudo | 9,1 | 0,0 | 31,3% | 46,7% | 45,3% | 27,7% | 161 | 224 | 546 | 150 | 150 |
| hardcore | sem_ocupacao | 54,1 | 0,0 | 98,5% | 100,0% | 64,4% | 49,0% | 270 | 360 | 731 | 240 | — |
| hardcore | escala_5x2 | 28,5 | 0,0 | 87,5% | 100,0% | 18,4% | 62,2% | 209 | 306 | 690 | 240 | 200 |
| hardcore | meio_periodo_manha | 48,0 | 0,0 | 96,3% | 100,0% | 64,1% | 60,0% | 251 | 360 | 729 | 240 | 240 |
| hardcore | home_office | 38,1 | 1,2 | 93,9% | 100,0% | 33,0% | 58,9% | 270 | 341 | 660 | 240 | 240 |
| hardcore | turno_noturno | 34,7 | 0,0 | 90,8% | 100,0% | 23,3% | 59,3% | 270 | 330 | 642 | 240 | 240 |
| hardcore | trabalho_estudo | 15,7 | 0,0 | 39,4% | 46,7% | 44,1% | 48,3% | 253 | 334 | 578 | 240 | 240 |

h/semana: todo o tempo jogado por pessoa (inclui pausa e escapada do trabalho, coluna à parte). Dias e tempo livre: os últimos 30 dias da agenda. Sessão: só as sessões normais, em minutos. Planejado: mediana do total planejado nos dias com jogo.
