# Cenário controlado

Simulador 0.1.0+94819226f74c4413078507c4d07b6576ca560bdb · seeds 1, 2, 3, 4, 5 · 60 dias (7 de aquecimento) · 50 pessoas por célula.

Cada célula tem pessoas iguais: perfil e ocupação fixos, traços e rotina na média do conteúdo, residência na localidade do servidor. Humor neutro: sem extensões, inatividade nem desistência. O que muda entre as pessoas é só o sorteio do dia a dia, então as diferenças entre células e entre versões vêm das regras, não da mistura da população.

| Perfil | Ocupação | h/semana | no trabalho | dias com jogo | dias com oportunidade | 2+ sessões | tempo livre usado | sessão p50 | p90 | máx. | planejado dia livre | planejado dia útil |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| casual | sem_ocupacao | 8,6 | 0,0 | 59,2% | 100,0% | 0,0% | 7,8% | 126 | 145 | 159 | 122 | — |
| casual | escala_5x2 | 4,2 | 0,0 | 32,8% | 100,0% | 0,0% | 9,2% | 109 | 139 | 158 | 120 | 93 |
| casual | meio_periodo_manha | 6,8 | 0,0 | 46,8% | 100,0% | 0,0% | 8,4% | 125 | 144 | 161 | 122 | 119 |
| casual | home_office | 5,6 | 0,4 | 43,5% | 100,0% | 0,0% | 8,6% | 117 | 139 | 159 | 120 | 105 |
| casual | turno_noturno | 4,8 | 0,0 | 36,7% | 100,0% | 0,0% | 8,4% | 113 | 137 | 159 | 116 | 105 |
| casual | trabalho_estudo | 2,5 | 0,0 | 18,9% | 46,7% | 0,0% | 7,5% | 118 | 141 | 156 | 116 | 86 |
| social | sem_ocupacao | 15,5 | 0,0 | 72,0% | 100,0% | 0,0% | 14,1% | 185 | 214 | 231 | 181 | — |
| social | escala_5x2 | 8,1 | 0,0 | 44,4% | 100,0% | 0,0% | 17,5% | 153 | 204 | 230 | 179 | 132 |
| social | meio_periodo_manha | 12,8 | 0,0 | 60,3% | 100,0% | 0,0% | 16,1% | 183 | 211 | 230 | 181 | 176 |
| social | home_office | 11,0 | 0,7 | 59,3% | 100,0% | 0,0% | 17,2% | 169 | 204 | 230 | 183 | 155 |
| social | turno_noturno | 9,5 | 0,0 | 49,6% | 100,0% | 0,0% | 16,5% | 164 | 200 | 230 | 173 | 154 |
| social | trabalho_estudo | 4,5 | 0,0 | 23,8% | 46,7% | 0,0% | 13,8% | 174 | 207 | 231 | 175 | 127 |
| comerciante | sem_ocupacao | 15,3 | 0,0 | 81,8% | 100,0% | 0,0% | 13,8% | 161 | 185 | 200 | 155 | — |
| comerciante | escala_5x2 | 8,6 | 0,0 | 55,6% | 100,0% | 0,0% | 18,5% | 129 | 175 | 200 | 153 | 110 |
| comerciante | meio_periodo_manha | 13,2 | 0,0 | 71,3% | 100,0% | 0,0% | 16,4% | 158 | 183 | 200 | 155 | 151 |
| comerciante | home_office | 11,5 | 0,7 | 68,8% | 100,0% | 0,0% | 17,5% | 145 | 175 | 199 | 153 | 130 |
| comerciante | turno_noturno | 10,0 | 0,0 | 59,8% | 100,0% | 0,0% | 17,1% | 142 | 174 | 199 | 148 | 131 |
| comerciante | trabalho_estudo | 4,3 | 0,0 | 26,2% | 46,7% | 0,0% | 13,1% | 149 | 178 | 199 | 148 | 107 |
| explorador | sem_ocupacao | 18,0 | 0,0 | 74,8% | 100,0% | 0,0% | 16,1% | 205 | 238 | 257 | 202 | — |
| explorador | escala_5x2 | 10,0 | 0,0 | 50,7% | 100,0% | 0,0% | 21,8% | 167 | 224 | 255 | 202 | 147 |
| explorador | meio_periodo_manha | 15,7 | 0,0 | 66,6% | 100,0% | 0,0% | 19,6% | 203 | 235 | 257 | 202 | 198 |
| explorador | home_office | 13,2 | 0,8 | 63,2% | 100,0% | 0,0% | 20,3% | 187 | 224 | 255 | 201 | 173 |
| explorador | turno_noturno | 11,5 | 0,0 | 54,4% | 100,0% | 0,0% | 19,8% | 180 | 222 | 254 | 190 | 170 |
| explorador | trabalho_estudo | 5,3 | 0,0 | 25,8% | 46,7% | 0,0% | 16,4% | 192 | 230 | 253 | 194 | 139 |
| competitivo | sem_ocupacao | 26,6 | 0,0 | 84,8% | 100,0% | 0,0% | 24,0% | 270 | 312 | 331 | 265 | — |
| competitivo | escala_5x2 | 15,5 | 0,0 | 61,6% | 100,0% | 0,0% | 33,2% | 203 | 290 | 329 | 261 | 186 |
| competitivo | meio_periodo_manha | 23,3 | 0,0 | 75,0% | 100,0% | 0,0% | 28,9% | 265 | 307 | 331 | 263 | 259 |
| competitivo | home_office | 20,1 | 0,9 | 73,6% | 100,0% | 0,0% | 30,6% | 241 | 294 | 332 | 263 | 222 |
| competitivo | turno_noturno | 18,4 | 0,0 | 66,1% | 100,0% | 0,0% | 31,4% | 235 | 290 | 332 | 251 | 221 |
| competitivo | trabalho_estudo | 7,6 | 0,0 | 28,4% | 46,7% | 0,0% | 23,4% | 249 | 302 | 329 | 254 | 180 |
| hardcore | sem_ocupacao | 53,4 | 0,0 | 100,0% | 100,0% | 0,1% | 48,0% | 458 | 534 | 562 | 455 | — |
| hardcore | escala_5x2 | 28,5 | 0,0 | 87,3% | 100,0% | 0,2% | 61,6% | 208 | 488 | 560 | 457 | 200 |
| hardcore | meio_periodo_manha | 51,4 | 0,0 | 97,6% | 100,0% | 0,1% | 63,9% | 450 | 525 | 561 | 453 | 446 |
| hardcore | home_office | 42,2 | 1,2 | 94,2% | 100,0% | 5,7% | 65,4% | 348 | 490 | 558 | 455 | 345 |
| hardcore | turno_noturno | 39,6 | 0,0 | 91,1% | 100,0% | 0,1% | 68,2% | 330 | 486 | 561 | 430 | 330 |
| hardcore | trabalho_estudo | 14,7 | 0,0 | 33,2% | 46,7% | 0,4% | 45,4% | 418 | 509 | 560 | 433 | 302 |

h/semana: todo o tempo jogado por pessoa (inclui pausa e escapada do trabalho, coluna à parte). Dias e tempo livre: os últimos 30 dias da agenda. Sessão: só as sessões normais, em minutos. Planejado: mediana do total planejado nos dias com jogo.
