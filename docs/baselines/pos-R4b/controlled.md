# Cenário controlado

Simulador 0.1.0+c5a4530e0952f1cdde9290672af2f93df9801bf9 · seeds 1, 2, 3, 4, 5 · 60 dias (7 de aquecimento) · 50 pessoas por célula.

Cada célula tem pessoas iguais: perfil e ocupação fixos, traços e rotina na média do conteúdo, residência na localidade do servidor. Humor neutro: sem extensões, inatividade nem desistência. O que muda entre as pessoas é só o sorteio do dia a dia, então as diferenças entre células e entre versões vêm das regras, não da mistura da população.

| Perfil | Ocupação | h/semana | no trabalho | dias com jogo | dias com oportunidade | 2+ sessões | tempo livre usado | sessão p50 | p90 | máx. | planejado dia livre | planejado dia útil |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| casual | sem_ocupacao | 11,5 | 0,0 | 57,7% | 100,0% | 44,9% | 10,3% | 89 | 143 | 360 | 80 | — |
| casual | escala_5x2 | 4,8 | 0,0 | 32,4% | 100,0% | 22,5% | 10,6% | 87 | 125 | 334 | 80 | 80 |
| casual | meio_periodo_manha | 8,9 | 0,0 | 47,3% | 100,0% | 44,1% | 11,2% | 89 | 142 | 325 | 80 | 80 |
| casual | home_office | 6,8 | 0,4 | 43,2% | 100,0% | 38,1% | 10,4% | 88 | 140 | 324 | 80 | 80 |
| casual | turno_noturno | 5,8 | 0,0 | 36,5% | 100,0% | 32,1% | 9,9% | 87 | 140 | 297 | 80 | 80 |
| casual | trabalho_estudo | 3,3 | 0,0 | 19,9% | 46,7% | 37,4% | 10,0% | 88 | 142 | 295 | 80 | 80 |
| social | sem_ocupacao | 20,4 | 0,0 | 71,0% | 100,0% | 52,2% | 18,4% | 122 | 182 | 413 | 110 | — |
| social | escala_5x2 | 9,0 | 0,0 | 45,0% | 100,0% | 22,8% | 19,8% | 121 | 176 | 389 | 110 | 110 |
| social | meio_periodo_manha | 16,6 | 0,0 | 61,1% | 100,0% | 52,5% | 21,0% | 122 | 182 | 418 | 110 | 110 |
| social | home_office | 12,8 | 0,7 | 57,3% | 100,0% | 47,9% | 19,6% | 120 | 178 | 387 | 110 | 110 |
| social | turno_noturno | 10,9 | 0,0 | 48,5% | 100,0% | 38,7% | 18,5% | 120 | 179 | 416 | 110 | 110 |
| social | trabalho_estudo | 5,6 | 0,0 | 24,9% | 46,7% | 42,1% | 17,2% | 121 | 179 | 360 | 110 | 110 |
| comerciante | sem_ocupacao | 22,8 | 0,0 | 81,5% | 100,0% | 57,5% | 20,8% | 105 | 180 | 486 | 90 | — |
| comerciante | escala_5x2 | 10,7 | 0,0 | 56,9% | 100,0% | 26,4% | 23,2% | 103 | 161 | 392 | 90 | 90 |
| comerciante | meio_periodo_manha | 19,0 | 0,0 | 71,0% | 100,0% | 57,6% | 23,5% | 104 | 180 | 512 | 90 | 90 |
| comerciante | home_office | 15,0 | 0,7 | 70,5% | 100,0% | 53,4% | 23,6% | 102 | 161 | 394 | 90 | 90 |
| comerciante | turno_noturno | 13,3 | 0,0 | 59,4% | 100,0% | 50,5% | 22,5% | 102 | 163 | 402 | 90 | 90 |
| comerciante | trabalho_estudo | 6,5 | 0,0 | 28,9% | 46,7% | 51,9% | 19,6% | 103 | 164 | 365 | 90 | 90 |
| explorador | sem_ocupacao | 25,4 | 0,0 | 75,6% | 100,0% | 55,9% | 22,8% | 133 | 210 | 462 | 120 | — |
| explorador | escala_5x2 | 11,3 | 0,0 | 49,4% | 100,0% | 22,8% | 24,2% | 130 | 187 | 428 | 120 | 120 |
| explorador | meio_periodo_manha | 20,0 | 0,0 | 63,8% | 100,0% | 55,8% | 25,0% | 132 | 194 | 432 | 120 | 120 |
| explorador | home_office | 15,7 | 0,8 | 62,5% | 100,0% | 49,4% | 23,9% | 129 | 190 | 434 | 120 | 120 |
| explorador | turno_noturno | 12,5 | 0,0 | 54,4% | 100,0% | 33,0% | 21,6% | 128 | 186 | 390 | 120 | 120 |
| explorador | trabalho_estudo | 7,2 | 0,0 | 27,1% | 46,7% | 51,1% | 22,3% | 131 | 192 | 369 | 120 | 120 |
| competitivo | sem_ocupacao | 32,9 | 0,0 | 85,2% | 100,0% | 56,8% | 29,5% | 170 | 245 | 523 | 150 | — |
| competitivo | escala_5x2 | 16,0 | 0,0 | 62,9% | 100,0% | 19,2% | 34,8% | 162 | 214 | 462 | 150 | 150 |
| competitivo | meio_periodo_manha | 27,7 | 0,0 | 75,9% | 100,0% | 58,6% | 34,6% | 165 | 245 | 477 | 150 | 150 |
| competitivo | home_office | 21,2 | 0,9 | 74,0% | 100,0% | 46,2% | 32,7% | 161 | 240 | 480 | 150 | 150 |
| competitivo | turno_noturno | 18,7 | 0,0 | 64,8% | 100,0% | 40,4% | 31,9% | 162 | 240 | 495 | 150 | 150 |
| competitivo | trabalho_estudo | 9,3 | 0,0 | 31,3% | 46,7% | 46,4% | 28,2% | 162 | 240 | 513 | 150 | 150 |
| hardcore | sem_ocupacao | 55,0 | 0,0 | 98,5% | 100,0% | 65,5% | 49,9% | 270 | 362 | 734 | 240 | — |
| hardcore | escala_5x2 | 28,7 | 0,0 | 87,5% | 100,0% | 18,7% | 62,7% | 209 | 308 | 665 | 240 | 200 |
| hardcore | meio_periodo_manha | 48,4 | 0,0 | 96,2% | 100,0% | 65,4% | 60,6% | 251 | 360 | 694 | 240 | 240 |
| hardcore | home_office | 38,4 | 1,2 | 93,9% | 100,0% | 33,3% | 59,3% | 270 | 345 | 725 | 240 | 240 |
| hardcore | turno_noturno | 35,0 | 0,0 | 90,8% | 100,0% | 23,8% | 59,6% | 270 | 330 | 671 | 240 | 240 |
| hardcore | trabalho_estudo | 15,9 | 0,0 | 39,5% | 46,7% | 44,9% | 49,0% | 254 | 336 | 649 | 240 | 240 |

h/semana: todo o tempo jogado por pessoa (inclui pausa e escapada do trabalho, coluna à parte). Dias e tempo livre: os últimos 30 dias da agenda. Sessão: só as sessões normais, em minutos. Planejado: mediana do total planejado nos dias com jogo.
