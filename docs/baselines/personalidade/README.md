# Personalidade principal (P4a, D-056)

Duas versões do preset, com as mesmas pessoas (seeds 1 a 5 × 1.000 pessoas × 90 dias, validação padrão):

| Versão | Classificação | Regras |
|---|---|---|
| P0 | sim | não (`personality.rulesEnabled` = false) |
| P1 | sim | sim (preset) |

| Arquivo | O que é |
|---|---|
| `analise.md` | distribuição das personalidades, mudanças em 90 dias, mistura de atividades por personalidade e metas da validação. Compara V3b (D-055), P0 e P1 |
| `analise_regras.md` | o Agressivo ainda responde a preferências, risco e metas? Quem anda mais com as regras? |
| `<versão>/validation.md`, `.csv` | relatórios da validação |
| `<versão>/people.csv` | uma linha por pessoa e seed, com personalidade inicial e final, minutos por tipo de atividade, minutos de espera (`min_idle`) e tempo conectado (`min_played`) |
| `conteudo.txt` | hash do `population.json` de cada versão; P1 é igual ao preset |
| `analisar.py`, `analisar_regras.py` | scripts. Leem `reports/p4a/val/<versão>/` e a V3b em `../progressao-inicial/` |

A aversão a risco por pessoa vem da criação refeita com as mesmas seeds (só a geração, sem simular), conferida pela personalidade: 0 diferenças em 349 Agressivos.
