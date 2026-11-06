# Objetivos pessoais (O1, D-062)

| Arquivo | O que é |
|---|---|
| `O1/` | validação padrão (5 seeds × 1.000 × 90 dias) com os objetivos sorteados. O `people.csv` tem a coluna `objectives` no formato `id:parâmetro:g|s importância` |
| `distribuicao.txt` | coerência por preferência e competitividade (terços), ambição por perfil, quantidades e frequência de cada objetivo como geral e como secundário |
| `analisar.py` | script que gera a primeira parte de `distribuicao.txt` a partir de `O1/people.csv` |
| `O2/desligada/`, `O2/ligada/` | O2: as mesmas pessoas e objetivos com a influência desligada × ligada. O `people.csv` tem `objective_group`, `objectives_completed`, `objective_links`, `goals_linked_*`, `goals_prep_set` e `goals_by_type` |
| `O2/analise.md` | comparação por grupo (acionáveis, só preparação, indisponíveis), objetivos concluídos, metas ligadas por objetivo e diferenças por seed |
| `analisar_o2.py`, `analisar_o2_seeds.py` | scripts que geram `O2/analise.md` |

**Conferências:**
- **O1:** `validation.csv` e `people.csv` são idênticos aos da E1a (`reports/e1a/val/`), fora a coluna nova. Os objetivos não mudam o comportamento.
- **O2:** a rodada desligada é idêntica à O1 (`validation.csv` e as colunas antigas de `people.csv`). A ligada mostra o efeito da influência.
