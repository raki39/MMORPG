# Aquisição e perda de traits (P4d, D-060)

| Arquivo | O que é |
|---|---|
| `analise.md` | A0 (aquisição desligada) × A1 (ligada), mesmas pessoas: conferência de que A0 é idêntica à P4c, aquisições e perdas por trait, oscilações, exclusões, evidência (oportunidades, a favor, contra, só pelo efeito) e validação |
| `A0/`, `A1/` | validação padrão (5 seeds × 1.000 × 90 dias); o `people.csv` tem `play_days`, `quirk_history` e `quirk_evidence` |
| `diagnostico_memorias.cs` | por que o Rancoroso não tem oportunidade no mundo atual: tipos de memória e impressões dos lugares (seed 1, 1.000 pessoas, 45 dias) |
| `conteudo.txt` | hash dos `traits.json`; A0 tem `acquisition.enabled` = false; A1 é igual ao preset |
| `analisar.py` | script (lê `reports/p4d/val/` e `reports/p4c/val/Q1/`) |
