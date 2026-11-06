# Variedade de horários (R6, D-058)

| Arquivo | O que é |
|---|---|
| `analise.md` | antes × depois, mesmas pessoas (seeds 1 e 2 × 1.000 × 28 dias; dias 7 a 27): tempo jogado por hora local e por ocupação, conectados por hora do servidor, sono planejado e real, passagens entre trabalho e folga, início das sessões e geração |
| `R6/validation.md`, `.csv`, `people.csv` | validação padrão (5 seeds × 1.000 × 90 dias) com o preset da R6. A versão anterior está em `../traits/P3/` |
| `conferencias.md`, `conferir.py` | as duas conferências pedidas pelo criador: déficit de quem trabalha e estuda à noite e as contas da janela depois do turno |
| `analisar.py` | script da comparação; lê as exportações do diagnóstico (fora do repositório) |
| `exportar_horarios.cs` | programa de diagnóstico que exporta pessoas, sessões e dias (sono planejado e real) |
| `conteudo.txt` | hash do `population.json` (igual ao preset) |
