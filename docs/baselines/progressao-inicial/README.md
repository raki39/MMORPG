# Progressão inicial e metas de desbloqueio (D-055)

Quatro versões do preset, com as mesmas pessoas (seeds 1 a 5 × 1.000 pessoas × 90 dias, validação padrão):

| Versão | Progressão inicial | Desbloqueio e descoberta |
|---|---|---|
| V0 | não | não (idêntico ao D-054) |
| V1 | sim | não |
| V2 | não | sim |
| V3 | sim | sim (antes da correção do ciclo das metas) |
| V3b | sim | sim (preset final, com a correção) |

| Arquivo | O que é |
|---|---|
| `analise.md` | V0 a V3: tempo até o nível-alvo por perfil e preferência, atividades antes e depois dele, personagens abaixo do alvo, metas, horas e nível final |
| `analise_correcao.md` | V0, V3 e V3b: efeito da correção do ciclo das metas de nível |
| `<versão>/validation.md`, `.csv` | relatórios da validação |
| `V3/people.csv`, `V3b/people.csv` | uma linha por pessoa e seed, antes e depois da correção (horas até o alvo, minutos por tipo antes do alvo, metas). O de V0 é igual ao de `../perfil-preferencias/` nas colunas comuns |
| `conteudo.txt` | hash do `population.json` de cada versão |
| `montar_versoes.py`, `analisar.py` | scripts |
