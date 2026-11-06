# P4b — traits: medição

validation.csv identico (P2 x P3): True
people.csv: 5000 x 5000 linhas; linhas com alguma coluna comum diferente: 0

== Quantidade de traits por pessoa (n = 5000, 5 seeds x 1.000) ==
| Traits | Tabela | Medido |
|---|---|---|
| 0 | 8% | 7.8% |
| 1 | 25% | 24.0% |
| 2 | 50% | 50.6% |
| 3 | 10% | 10.5% |
| 4 | 4% | 3.9% |
| 5 | 2% | 2.2% |
| 6 | 1% | 1.2% |

Traits sorteadas: 9492; negativas 39.5% (chance por vaga: 40%)
Com 2 traits: so positivas 36.9%, uma de cada 47.1%, so negativas 16.0% (esperado 36, 48 e 16%)

== Frequencia de cada trait (% das pessoas) ==
| Trait | +/- | Sistema | % |
|---|---|---|---|
| Estudioso | + | pessoa | 9.5 |
| Saudosista | + | pessoa | 8.9 |
| Observador | + | pessoa | 8.8 |
| Protetor de novatos | + | etapa3 | 8.5 |
| Otimizador | + | pessoa | 8.5 |
| Caçador de barganhas | + | M5 | 8.3 |
| Companheiro fiel | + | M6 | 8.3 |
| Memória firme | + | pessoa | 8.1 |
| Mercador cauteloso | + | M5 | 8.0 |
| Líder nato | + | M6 | 7.9 |
| Competidor honrado | + | etapa3 | 7.9 |
| Resiliente | + | pessoa | 7.8 |
| Colecionador | + | pessoa | 7.7 |
| Provocador | - | etapa3 | 7.3 |
| Rancoroso | - | pessoa | 7.3 |
| Indeciso | - | pessoa | 7.2 |
| Procrastinador | - | pessoa | 7.1 |
| Pavio curto | - | pessoa | 7.1 |
| Acumulador | - | M5 | 7.1 |
| Persistente | + | pessoa | 6.8 |
| Lobo solitário | - | M6 | 6.6 |
| Maratonista | - | pessoa | 6.5 |
| Impulsivo | - | pessoa | 6.5 |
| Esquecido | - | pessoa | 6.4 |
| Gastador compulsivo | - | M5 | 5.8 |

== Coerencia (taxa da trait no grupo, %) ==
- Pavio curto: Agressivo 16.6; Pacifico 4.7; todos 7.1
- Provocador: Agressivo 19.1; Pacifico (excluido) 0.0; todos 7.3
- Maratonista: Devoto 17.5; Hardcore 11.9; Regular 6.6; Casual 4.3
- Lobo solitário: Reservado 13.4; Sociavel (excluido) 0.0; todos 6.6
- Observador: pref. Exploracao >= 700 11.9; <= 300 4.7
- Otimizador: Competitividade >= 700 10.7; <= 300 5.4
- Persistente: Paciente 12.2; Leal (excluido) 0.0; Inquieto (excluido) 0.0; todos 6.8

Combinacoes excluidas encontradas: 0
