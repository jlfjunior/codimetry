# Análise CBTAM

`cbtam_analysis.py` recalcula todos os números do Cap. 4 da dissertação a partir de um diretório de resultados da ferramenta. Usa só a biblioteca padrão do Python (≥ 3.9).

```bash
python3 scripts/analysis/cbtam_analysis.py --results results --out analysis
```

## Saídas (`--out`)

| Arquivo | Conteúdo |
|---|---|
| `report.md` | Todas as tabelas e números do Cap. 4, com o rótulo da seção do `.tex` |
| `participants.csv` | Resumo por participante (médias, totais, LOC, nº de classes) |
| `per_participant_results.csv` | % Normal por métrica, nível previsto e discrepância |
| `per_class.csv` | Uma linha por classe, com chave participante + arquivo + classe, para o pareamento v1 × v2 |
| `check.md` | Só com `--check`: divergências não explicadas e erratas conhecidas |

## Regras reproduzidas da versão publicada

- **Classes:** um registro por (arquivo, classe), mantendo o **último**. A v1 da ferramenta emite uma linha por declaração de classe parcial ou homônima, todas com o mesmo `FilePath`.
- **Métodos (MC):** todas as linhas de método do `metrics_misra.csv`, sem deduplicação.
- **Limiares relativos:** P50 e P90 com interpolação linear (padrão do numpy), calculados uma vez sobre a amostra combinada. Normal ≤ P50 < High ≤ P90 < Anomalous.
- **Regra em estágios:** Mid-Level se ≥ 4 de 7 métricas têm ≥ 40% Normal; Senior se ≥ 5 têm ≥ 50%; Expert se ≥ 6 têm ≥ 60%, cumulativamente.
- **Acurácia e linha de base** excluem o participante Beginner; **ρ de Spearman** usa os 48 participantes.
- **Correlações de Pearson** são entre participantes, com duas exceções: DIT × NOC é por classe, e CBO × classes usa o nº de classes antes da consolidação (o do `index.md`).

## `published_v1.json`

Números do Cap. 4 copiados literalmente de `master-thesis/Conteudo/04_Outros.tex`. A seção `errata` lista as células da tabela por participante (`tab:per_participant_results`) que não conferem com os dados, cada uma com a justificativa. A conferência falha (exit 1) para qualquer divergência fora dessa lista, ou se uma errata deixar de ser necessária.
