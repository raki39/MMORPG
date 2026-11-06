# Diagnóstico das metas de missão e do objetivo da campanha (D-065)

| Arquivo | O que é |
|---|---|
| `diagnostico.cs` | ferramenta (projeto à parte com AssemblyName `Simulation.Tests`): intercepta o fim de cada meta e registra o estado da missão no instante (etapa, minutos, vagas, nível, zona conhecida, vínculos), mais os eventos relevantes |
| `antes.md`, `depois.md` | análise (`analise.py`) de 5 seeds × 1.000 × 90 dias antes e depois das correções: metas por tipo, estado no abandono, continuidade, grupos e objetivo da campanha |
| `comparacao.md` | antes × depois lado a lado (`comparar.py`) |
| `sem_avanco_antes_seed1.txt` | "etapa disponível sem avanço" (seed 1, antes): se a pessoa escolheu a atividade da etapa e o que fez no lugar (`analise2.py`) |
| `trajetorias_antes_seed1.md` | trajetórias individuais (seed 1, antes): quem abandonou o objetivo da campanha e uma missão antiga readotada 20 vezes (`trajetorias.py`) |

Os CSVs completos (centenas de MB) não ficam no repositório; a ferramenta os gera de novo com as mesmas seeds.
