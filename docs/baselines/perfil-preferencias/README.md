# Perfil × Preferências × atributos: primeira medição (em avaliação)

População da estrutura nova (`../../perfil-preferencias.md`), com os parâmetros candidatos. Ainda não adotada na `main`.

**Execuções:**
- validação padrão: seeds 1 a 5 × 1.000 pessoas × 90 dias, com 7 de aquecimento (janela de 83 dias);
- mesmas seeds e mesmo tamanho da referência A (`../exp-dedicacao/A`);
- código do ramo `perfil-preferencias`, cujo commit está em `conteudo.txt` com o hash do `population.json`.

| Arquivo | O que é |
|---|---|
| `analise.md` | atributos por perfil, horas estimadas e reais contra A, perfil × ocupação, preferências, ligações, influência nas escolhas, mistura de atividades, competitividade |
| `perfil-ocupacao.csv` | horas por perfil × ocupação (com máximo e p99 quando há 300 pessoas ou mais) |
| `validation.md`, `.csv` | relatório da validação (regras do cenário 7 de 9) |
| `people.csv` | uma linha por pessoa e seed, com preferências, especialidades, competitividade e minutos por tipo de atividade |
| `population.json` | o conteúdo medido |
| `analisar.py` | o script da análise |
