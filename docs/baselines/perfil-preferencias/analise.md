# Perfil × Preferências × atributos: medição da população nova

Medição antes de adotar o preset. Execuções: seeds 1, 2, 3, 4, 5 × 1.000 pessoas × 90 dias (7 de aquecimento, janela de 83 dias), a validação padrão. Referência: versão A de `docs/baselines/exp-dedicacao/` (mesmas seeds e tamanho).

**Pareamento com A:** 4832 de 5000 pessoas têm a mesma ocupação de A (96.6%); nome e localidade seguem o mesmo fluxo. As diferenças vêm do devoto, que tem ocupações próprias, e do sorteio de perfil com os pesos novos.

## 1. Atributos de dedicação por perfil (p10 · mediana · p90)

| Perfil | n | Importância | Sessão normal (min) | Hábito |
|---|---|---|---|---|
| Casual | 1763 | 228 · 375 · 591 | 57 · 100 · 168 | 230 · 397 · 629 |
| Regular | 2635 | 370 · 546 · 745 | 84 · 145 · 231 | 340 · 535 · 741 |
| Hardcore | 505 | 670 · 836 · 935 | 177 · 260 · 348 | 596 · 781 · 905 |
| Devoto | 97 | 861 · 935 · 970 | 269 · 363 · 484 | 743 · 856 · 951 |

## 2. Horas estimadas (na criação)

p10 · **mediana** · p90 · p99 (máximo), em horas por semana. Na referência A, "Vocações" junta social, explorador, comerciante e competitivo.

| Perfil | n | Nova | Referência A |
|---|---|---|---|
| Casual | 1763 | 3,5 · **8,1** · 17,3 · 27,8 (41,1) | 3,8 · **5,9** · 9,7 · 13,8 (16,8) |
| Regular (A: vocações) | 2635 | 7,1 · **15,3** · 29,8 · 44,9 (58,9) | 8,3 · **13,8** · 23,1 · 32,8 (40,4) |
| Hardcore | 505 | 22,2 · **35,5** · 52,1 · 63,9 (70,3) | 25,6 · **35,4** · 49,5 · 58,9 (63,1) |
| Devoto | 97 | 46,2 · **55,0** · 69,0 · 74,5 (74,5) | 48,5 · **55,3** · 69,9 · 73,9 (79,2) |
| **População** | 5000 | 5,1 · **13,6** · 34,8 · 60,7 (74,5) | 4,8 · **11,5** · 30,1 · 56,8 (79,2) |

## 3. Horas reais

p10 · **mediana** · p90 · p99 (máximo), em horas por semana. Na referência A, "Vocações" junta social, explorador, comerciante e competitivo.

| Perfil | n | Nova | Referência A |
|---|---|---|---|
| Casual | 1763 | 3,5 · **8,9** · 18,6 · 29,5 (38,4) | 3,8 · **6,7** · 11,0 · 15,7 (18,0) |
| Regular (A: vocações) | 2635 | 7,6 · **16,5** · 30,9 · 44,7 (54,9) | 9,0 · **15,4** · 25,1 · 34,2 (40,0) |
| Hardcore | 505 | 22,6 · **35,9** · 50,4 · 63,4 (71,4) | 26,1 · **36,6** · 49,1 · 57,6 (61,4) |
| Devoto | 97 | 46,2 · **54,8** · 69,0 · 74,5 (75,1) | 48,8 · **54,4** · 68,8 · 72,9 (73,9) |
| **População** | 5000 | 5,5 · **14,6** · 35,6 · 59,3 (75,1) | 5,2 · **12,5** · 31,8 · 55,8 (73,9) |

**Sobreposição (horas reais):**
- p90 do casual 18,6 h contra mediana do regular 16,5 h;
- p10 do hardcore 22,6 h contra mediana do regular 16,5 h e p90 do regular 30,9 h;
- p10 do devoto 46,2 h contra p90 do hardcore 50,4 h.

## 4. Horas reais por perfil × ocupação

p10 · mediana · p90; p99 só com 300 pessoas ou mais na célula. Todas as células e o máximo em `perfil-ocupacao.csv`.

| Ocupação | Tempo livre (mediana) | Casual | Regular | Hardcore | Devoto |
|---|---|---|---|---|---|
| Aposentado | 107,3 | 8,8 · **14,9** · 27,8 (n=42) | 11,3 · **27,0** · 41,5 (n=48) | 47,7 · **58,0** · 62,1 (n=11) | — |
| Autônomo / freelancer | 75,5 | 4,5 · **10,8** · 19,4 (n=85) | 11,5 · **20,1** · 35,1 (n=108) | 36,2 · **43,2** · 50,1 (n=19) | — |
| Comércio e serviços em escala 5x2 | 50,2 | 2,8 · **6,3** · 13,2 (n=155) | 6,9 · **12,1** · 23,1 (n=220) | 23,0 · **30,4** · 35,6 (n=37) | — |
| Comércio em escala 6x1 | 44,0 | 3,0 · **7,7** · 13,6 (n=72) | 5,7 · **10,5** · 20,3 (n=96) | 18,0 · **29,2** · 34,5 (n=17) | — |
| Cuidando da casa e da família | 59,0 | 3,0 · **7,2** · 16,3 (n=39) | 8,5 · **16,4** · 27,2 (n=96) | 27,2 · **37,8** · 41,8 (n=15) | — |
| Estudante (manhã) | 73,6 | 6,1 · **10,8** · 20,9 (n=191) | 10,7 · **20,8** · 34,0 (n=276) | 30,7 · **45,5** · 51,1 (n=49) | 48,8 · **54,8** · 59,2 (n=12) |
| Estudante (tarde) | 76,2 | 6,0 · **9,3** · 20,8 (n=74) | 10,7 · **18,5** · 33,9 (n=115) | 25,2 · **37,4** · 49,7 (n=26) | 45,1 · **49,9** · 52,7 (n=12) |
| Home office (tecnologia) | 65,0 | 4,0 · **8,5** · 18,3 (n=146) | 9,5 · **17,2** · 30,3 (n=205) | 27,5 · **39,4** · 46,9 (n=51) | — |
| Meio período (manhã) | 79,2 | 5,8 · **9,3** · 20,8 (n=55) | 12,6 · **21,2** · 36,8 (n=79) | 31,2 · **54,1** · 56,0 (n=16) | — |
| Meio período (tarde) | 79,0 | 5,7 · **9,9** · 18,3 (n=45) | 12,1 · **19,3** · 30,5 (n=70) | 34,1 · **41,9** · 52,0 (n=11) | — |
| Sem ocupação | 107,1 | 8,7 · **15,8** · 29,0 (n=126) | 14,6 · **26,8** · 40,4 (n=209) | 39,8 · **54,3** · 63,6 (n=34) | 57,0 · **65,0** · 71,2 (n=41) |
| Trabalha de dia e estuda à noite | 34,7 | 0,5 · **4,1** · 8,7 (n=102) | 2,4 · **7,1** · 13,3 (n=144) | 8,7 · **13,7** · 20,0 (n=33) | — |
| Trabalho em horário comercial | 54,1 | 3,1 · **7,8** · 15,7 (n=393) | 7,6 · **14,4** · 24,8 (n=627) | 23,4 · **32,4** · 39,4 (n=123) | — |
| Turno noturno | 59,7 | 3,7 · **8,1** · 16,8 (n=54) | 6,6 · **14,2** · 26,3 (n=91) | 27,3 · **35,0** · 45,6 (n=13) | — |
| Universitário (diurno) | 67,9 | 5,4 · **9,3** · 18,3 (n=184) | 9,5 · **17,8** · 31,6 (n=251) | 28,6 · **40,2** · 48,9 (n=50) | 44,4 · **48,9** · 53,7 (n=32) |

## 5. Preferências: distribuição e variedade

| Preferência | p10 | mediana | p90 | forte (≥ 700) | predominante |
|---|---|---|---|---|---|
| PvE | 253 | 577 | 825 | 27.9% | 25.7% |
| PvP | 54 | 287 | 681 | 9.1% | 7.2% |
| Crafting | 191 | 424 | 735 | 13.4% | 11.0% |
| Comércio | 126 | 347 | 703 | 10.3% | 8.3% |
| Coleta | 192 | 434 | 739 | 13.5% | 11.1% |
| Exploração | 236 | 514 | 778 | 18.7% | 16.6% |
| Social | 250 | 553 | 803 | 23.4% | 20.0% |

Preferências fortes por pessoa: 0: 27% · 1: 40% · 2: 23% · 3: 8% · 4 ou mais: 2%.

**Independência do perfil** (mediana de cada preferência por perfil):

| Perfil | PvE | PvP | Crafting | Comércio | Coleta | Exploração | Social |
|---|---|---|---|---|---|---|---|
| Casual | 572 | 280 | 418 | 348 | 434 | 523 | 544 |
| Regular | 577 | 288 | 430 | 347 | 433 | 513 | 557 |
| Hardcore | 600 | 304 | 416 | 343 | 433 | 503 | 563 |
| Devoto | 559 | 275 | 459 | 339 | 449 | 488 | 536 |

**Ligações (correlação de postos medida):**

| Par | Correlação |
|---|---|
| Social × Sociabilidade (0,35) | 0,47 |
| PvP × Agressividade (0,30) | 0,37 |
| Exploração × Variedade (0,30) | 0,38 |
| Preferência PvE × especialidade PvE (0,50) | 0,73 |
| Social × especialidade Socializar (0,50) | 0,70 |
| PvP × Competitividade (sem ligação) | -0,00 |
| Competitividade × Agressividade (sem ligação) | -0,01 |
| PvE × importância do jogo (sem ligação) | 0,03 |
| Crafting × Comércio (sem ligação) | 0,01 |
| Coleta × Comércio (sem ligação) | 0,01 |

## 6. Influência real das preferências nas escolhas

Parcela do tempo em atividades de cada tipo (toda a campanha), pelo terço da preferência correspondente. Se a preferência pesa, o terço de cima passa mais tempo no tipo; se fosse obrigação, o de baixo não faria nada dele.

| Tipo | Preferência | Terço de baixo | Terço do meio | Terço de cima | Correlação (pessoa) |
|---|---|---|---|---|---|
| Caça e chefe | PvE | 10,9% | 33,0% | 66,7% | 0,80 |
| Coleta | Coleta | 2,1% | 6,8% | 36,2% | 0,73 |
| Comércio | Comércio | 0,4% | 2,1% | 19,6% | 0,77 |
| Exploração | Exploração | 2,4% | 7,0% | 25,6% | 0,62 |
| Taverna | Social | 2,6% | 14,9% | 48,1% | 0,77 |
| Patrulha e emboscada | PvP | 1,0% | 2,1% | 6,1% | 0,28 |
| Coleta e comércio, pelo Crafting (deve ficar igual) | Crafting | 22,9% | 22,6% | 21,7% | -0,02 |

**Especialidade (reforço mínimo):** o fator "especialidade" vale no máximo `teto × peso da afinidade` = 50 × 450 / 1000 = **22 pontos**, contra até **450 pontos** do gosto (preferência): no máximo 5% do termo de afinidade. Comparar tempo de caça por nível de especialidade não isola o reforço, porque a especialidade cresce com a prática e está ligada à preferência (0,5).

**Mistura de atividades** (parcela do tempo conectado, média das seeds; faixa provisória da validação):

| Medida | Referência A | Nova |
|---|---|---|
| Tempo em atividades de combate | 42,1% | 31,3% |
| Tempo explorando | 6,9% | 8,5% |
| Tempo na cidade | 22,3% | 26,8% |
| Tempo andando ou esperando | 21,3% | 20,2% |
| Tempo em missões | 2,2% | 1,9% |
| Tempo em coleta | 5,2% | 11,4% |

## 7. Competitividade e agressividade

| Medida | Correlação com Competitividade | Correlação com Agressividade |
|---|---|---|
| Nível ao fim | 0,02 | 0,01 |
| Horas por semana | 0,01 | 0,02 |
| Minutos de patrulha e emboscada | 0,00 | 0,11 |
