# Cenário controlado

Simulador 0.1.0+753b9186d2182e960d6aec31a76cecb5286dded9 · seeds 1, 2, 3, 4, 5 · 60 dias (7 de aquecimento) · 50 pessoas por célula.

Cada célula tem pessoas iguais: perfil e ocupação fixos, traços e rotina na média do conteúdo, residência na localidade do servidor. Humor neutro: sem extensões, inatividade nem desistência. O que muda entre as pessoas é só o sorteio do dia a dia, então as diferenças entre células e entre versões vêm das regras, não da mistura da população.

| Perfil | Ocupação | h/semana | no trabalho | dias com jogo | dias com oportunidade | 2+ sessões | tempo livre usado | sessão p50 | p90 | máx. | planejado dia livre | planejado dia útil |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| casual | sem_ocupacao | 8,5 | 0,0 | 57,8% | 100,0% | 0,0% | 7,5% | 124 | 136 | 155 | 120 | — |
| casual | escala_5x2 | 4,1 | 0,0 | 32,4% | 100,0% | 0,0% | 8,9% | 108 | 131 | 152 | 120 | 93 |
| casual | meio_periodo_manha | 6,7 | 0,0 | 47,3% | 100,0% | 0,0% | 8,5% | 123 | 135 | 151 | 120 | 118 |
| casual | home_office | 5,5 | 0,4 | 43,2% | 100,0% | 0,0% | 8,3% | 115 | 132 | 153 | 121 | 104 |
| casual | turno_noturno | 4,8 | 0,0 | 36,5% | 100,0% | 0,0% | 8,3% | 113 | 130 | 155 | 115 | 104 |
| casual | trabalho_estudo | 2,6 | 0,0 | 19,8% | 46,7% | 0,0% | 7,8% | 119 | 133 | 151 | 116 | 86 |
| social | sem_ocupacao | 14,1 | 0,0 | 71,0% | 100,0% | 0,0% | 12,7% | 170 | 186 | 207 | 165 | — |
| social | escala_5x2 | 7,6 | 0,0 | 45,0% | 100,0% | 0,0% | 16,6% | 142 | 177 | 204 | 165 | 128 |
| social | meio_periodo_manha | 11,9 | 0,0 | 61,1% | 100,0% | 0,0% | 15,0% | 168 | 184 | 207 | 165 | 162 |
| social | home_office | 9,8 | 0,7 | 57,3% | 100,0% | 0,0% | 14,9% | 156 | 178 | 208 | 165 | 142 |
| social | turno_noturno | 8,8 | 0,0 | 48,5% | 100,0% | 0,0% | 15,0% | 154 | 176 | 204 | 159 | 143 |
| social | trabalho_estudo | 4,2 | 0,0 | 24,8% | 46,7% | 0,0% | 13,1% | 160 | 180 | 202 | 160 | 119 |
| comerciante | sem_ocupacao | 13,3 | 0,0 | 81,6% | 100,0% | 0,0% | 12,1% | 141 | 154 | 172 | 135 | — |
| comerciante | escala_5x2 | 7,9 | 0,0 | 56,8% | 100,0% | 0,0% | 17,2% | 116 | 147 | 173 | 135 | 104 |
| comerciante | meio_periodo_manha | 11,6 | 0,0 | 71,1% | 100,0% | 0,0% | 14,4% | 139 | 152 | 172 | 135 | 132 |
| comerciante | home_office | 9,9 | 0,7 | 70,5% | 100,0% | 0,0% | 15,5% | 128 | 147 | 172 | 135 | 115 |
| comerciante | turno_noturno | 9,0 | 0,0 | 59,4% | 100,0% | 0,0% | 15,2% | 127 | 147 | 170 | 130 | 116 |
| comerciante | trabalho_estudo | 4,1 | 0,0 | 29,0% | 46,7% | 0,0% | 12,5% | 134 | 149 | 170 | 131 | 97 |
| explorador | sem_ocupacao | 16,3 | 0,0 | 75,6% | 100,0% | 0,1% | 14,7% | 185 | 201 | 224 | 180 | — |
| explorador | escala_5x2 | 9,2 | 0,0 | 49,4% | 100,0% | 0,0% | 19,6% | 153 | 192 | 223 | 179 | 139 |
| explorador | meio_periodo_manha | 13,6 | 0,0 | 63,9% | 100,0% | 0,0% | 16,9% | 182 | 199 | 224 | 181 | 177 |
| explorador | home_office | 11,7 | 0,8 | 62,5% | 100,0% | 0,0% | 17,8% | 169 | 194 | 226 | 181 | 155 |
| explorador | turno_noturno | 10,5 | 0,0 | 54,4% | 100,0% | 0,0% | 18,1% | 165 | 190 | 223 | 173 | 156 |
| explorador | trabalho_estudo | 4,9 | 0,0 | 27,2% | 46,7% | 0,0% | 15,5% | 175 | 196 | 223 | 175 | 130 |
| competitivo | sem_ocupacao | 23,0 | 0,0 | 85,3% | 100,0% | 0,1% | 20,6% | 230 | 251 | 280 | 226 | — |
| competitivo | escala_5x2 | 14,2 | 0,0 | 62,8% | 100,0% | 0,0% | 30,8% | 188 | 238 | 273 | 225 | 174 |
| competitivo | meio_periodo_manha | 20,2 | 0,0 | 76,0% | 100,0% | 0,0% | 25,1% | 228 | 248 | 279 | 226 | 221 |
| competitivo | home_office | 17,3 | 0,9 | 74,0% | 100,0% | 0,0% | 26,5% | 209 | 240 | 276 | 225 | 193 |
| competitivo | turno_noturno | 15,9 | 0,0 | 64,8% | 100,0% | 0,0% | 27,1% | 207 | 239 | 276 | 217 | 195 |
| competitivo | trabalho_estudo | 7,1 | 0,0 | 31,4% | 46,7% | 0,0% | 22,0% | 216 | 243 | 277 | 217 | 162 |
| hardcore | sem_ocupacao | 41,8 | 0,0 | 98,5% | 100,0% | 0,4% | 37,6% | 364 | 396 | 440 | 360 | — |
| hardcore | escala_5x2 | 25,5 | 0,0 | 87,6% | 100,0% | 0,1% | 55,3% | 208 | 373 | 436 | 360 | 200 |
| hardcore | meio_periodo_manha | 40,3 | 0,0 | 96,4% | 100,0% | 0,1% | 50,2% | 359 | 391 | 442 | 360 | 355 |
| hardcore | home_office | 35,8 | 1,2 | 93,9% | 100,0% | 0,1% | 55,1% | 325 | 376 | 433 | 360 | 306 |
| hardcore | turno_noturno | 34,7 | 0,0 | 90,9% | 100,0% | 0,0% | 59,3% | 324 | 375 | 442 | 346 | 314 |
| hardcore | trabalho_estudo | 13,6 | 0,0 | 39,2% | 46,7% | 0,5% | 42,7% | 339 | 382 | 430 | 346 | 259 |

h/semana: todo o tempo jogado por pessoa (inclui pausa e escapada do trabalho, coluna à parte). Dias e tempo livre: os últimos 30 dias da agenda. Sessão: só as sessões normais, em minutos. Planejado: mediana do total planejado nos dias com jogo.
