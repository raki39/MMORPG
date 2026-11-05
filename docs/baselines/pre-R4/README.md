# Referência pré-R4 (medição da R3)

Estado da simulação depois das correções de contabilidade (R1) e de limites (R2), antes de mudar a vontade de jogar (R4). Serve para comparar a R4 com o comportamento atual já corrigido. Nada foi recalibrado.

| Campo | Valor |
|---|---|
| Etapa | R3 (D-049), depois da R1 (D-047), da R2 (D-048) e da escala 5×2 corrigida |
| Validação | 5 seeds (1–5) × 1.000 pessoas × 90 dias; semana de lançamento excluída |
| Resultado | Regras do cenário 7 de 7; provisórias 8 de 12; futuras 1 de 5 |

O relatório completo (`validation.md`) tem a seção "Distribuições" com:
- duração das sessões;
- horas por perfil × ocupação;
- sessões por dia;
- uso do tempo livre;
- composição do tempo jogado;
- dias sem jogo por motivo;
- efeito separado do tempo livre;
- corte de sono.

O `validation.csv` traz os valores por seed com a precisão completa.

## Cenário controlado (`controlled.md`, `controlled.csv`)

Medido com as mesmas regras, no commit que criou o modo `--controlled` do runner, antes de qualquer mudança da R4:

```bash
dotnet run --project Simulation.Runner -- --controlled "casual,social,comerciante,explorador,competitivo,hardcore:sem_ocupacao,escala_5x2,meio_periodo_manha,home_office,turno_noturno,trabalho_estudo" --seeds 1-5 --days 60 --population 50 --out reports/r4/controlled-pre
```

Em cada célula perfil × ocupação, 50 pessoas iguais: traços e rotina na média, residência na localidade do servidor e humor neutro. As diferenças entre as células, e entre esta versão e a R4, vêm das regras, não da mistura da população.
