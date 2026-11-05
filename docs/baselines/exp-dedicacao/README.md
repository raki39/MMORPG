# Experimento de dedicação (D-053)

Cinco versões do sorteio de dedicação, com as mesmas pessoas por seed. Método em `../../dedicacao.md`.

| Versão | Formato | Dedicação compartilhada | Valores |
|---|---|---|---|
| A | antigo (média e dispersão) | — | os do preset (referência) |
| B0 | triangular | não | equivalentes aos de A |
| B | triangular | 0,45 | equivalentes aos de A |
| C0 | triangular | não | candidatos |
| C | triangular | 0,45 | candidatos |

**Execuções:**
- 5 seeds (1 a 5) × 1.000 pessoas × 90 dias por versão, com 7 de aquecimento: é a validação padrão, com as regras do cenário.
- Código: o do commit que adicionou o formato triangular (D-053), sobre `ebf4e31`. A, B0 e C0 rodaram antes da correção do carregador de JSON (que só afeta o campo `dedication`, ausente nessas três); B e C rodaram depois. O conteúdo de cada versão está identificado em `conteudo.txt`.

| Arquivo | O que é |
|---|---|
| `comparacao.md` | atributos, correlações, horas estimadas e reais (p10, mediana, p90, p99, máximo), diferenças pareadas e topo da distribuição |
| `perfil-ocupacao.csv` | horas por perfil × ocupação em todas as versões |
| `pareado.csv` | diferenças pareadas contra A, com intervalo de 95% |
| `<versão>/validation.md`, `.csv` | relatório da validação |
| `<versão>/people.csv` | uma linha por pessoa e seed |
| `<versão>/population.json` | o conteúdo usado |
| `montar_mundos.py`, `analisar.py` | scripts que montaram as versões e a comparação |

**Regras do cenário:** A 9/9, B0 9/9, B 9/9, C0 6/9, C 6/9.
