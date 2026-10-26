# Análise CBTAM

`cbtam_analysis.py` aplica o modelo CBTAM a um diretório de resultados da ferramenta (uma pasta por participante, `p{n}-{cargo}-{nível}`) e gera as estatísticas descritivas, a classificação de cada participante e a matriz de confusão. Usa só a biblioteca padrão do Python (≥ 3.9).

```bash
python3 scripts/analysis/cbtam_analysis.py --results results --out analysis \
    --check scripts/analysis/published_v1.json
```

## Saídas (`--out`)

| Arquivo | Conteúdo |
|---|---|
| `report.md` | Todas as tabelas e números da análise |
| `participants.csv` | Resumo por participante (médias, totais, LOC, nº de classes) |
| `per_participant_results.csv` | % Normal por métrica, nível previsto e discrepância |
| `per_class.csv` | Uma linha por classe, com chave participante + arquivo + classe, para parear coletas |
| `check.md` | Só com `--check`: divergências não explicadas e erratas conhecidas |

## Regras da análise

- **Classes:** um registro por (arquivo, classe), mantendo o **último**. A ferramenta emite uma linha por declaração de classe parcial ou homônima, todas com o mesmo `FilePath`.
- **Métodos (MC):** todas as linhas de método do `metrics_misra.csv`, sem deduplicação.
- **Limiares relativos:** P50 e P90 com interpolação linear (padrão do numpy), calculados uma vez sobre a amostra combinada. Normal ≤ P50 < High ≤ P90 < Anomalous.
- **Regra em estágios:** Mid-Level se ≥ 4 de 7 métricas têm ≥ 40% Normal; Senior se ≥ 5 têm ≥ 50%; Expert se ≥ 6 têm ≥ 60%, cumulativamente.
- **Acurácia e linha de base** excluem o participante Beginner; **ρ de Spearman** usa todos os participantes.
- **Correlações de Pearson** são entre participantes, com duas exceções: DIT × NOC é por classe, e CBO × classes usa o nº de classes antes da consolidação (o do `index.md`).

## `published_v1.json`

Números de referência da análise dos 47 participantes de `results/`, gerados com a tag `v1` da ferramenta. Com `--check`, o script compara o resultado com esses números. A seção `errata` lista os valores de referência que não conferem com os dados, cada um com a justificativa; hoje ela está vazia. A conferência falha (exit 1) para qualquer divergência fora dessa lista, ou se uma errata deixar de ser necessária.
