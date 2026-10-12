# Comparação entre passos

## Números principais

| Número | results-v1 | 00-head-231192a | 01-identidade | 02-sobrecargas | 03-vg-else-if | 04c-nuget-transitivo | 05-chamadas |
|---|---:|---:|---:|---:|---:|---:|---:|
| Classes | 2657 | 2657 | 3004 | 3004 | 3004 | 3004 | 3004 |
| Métodos (MC) | 6839 | 6839 | 6831 | 6831 | 6831 | 6831 | 6831 |
| LCOM = WMC(WMC−1)/2 (%) | 99,40 | 69,48 | 71,87 | 71,87 | 71,87 | 71,87 | 71,87 |
| Acurácia (%) | 51,1 | 46,8 | 46,8 | 44,7 | 44,7 | 44,7 | 46,8 |
| Linha de base (%) | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 |
| ρ regra em estágios | 0,286 | 0,177 | 0,198 | 0,160 | 0,160 | 0,160 | 0,198 |
| ρ média do % Normal | 0,363 | 0,348 | 0,305 | 0,308 | 0,308 | 0,325 | 0,333 |
| Erros (adjacentes / ≥ 2) | 24 (20 / 4) | 26 (24 / 2) | 26 (24 / 2) | 27 (25 / 2) | 27 (25 / 2) | 27 (25 / 2) | 26 (24 / 2) |
| P50 / P90 MC | 9,0 / 35,0 | 10,0 / 38,0 | 10,0 / 38,0 | 10,0 / 38,0 | 10,0 / 38,0 | 10,0 / 38,0 | 10,0 / 38,0 |
| P50 / P90 AC | 2,0 / 7,0 | 2,0 / 7,0 | 2,0 / 6,0 | 2,0 / 6,0 | 2,0 / 6,0 | 2,0 / 6,0 | 2,0 / 6,0 |
| P50 / P90 CWC | 21,0 / 159,0 | 20,0 / 157,4 | 18,0 / 136,7 | 18,0 / 136,0 | 18,0 / 136,0 | 18,0 / 136,0 | 18,0 / 140,4 |
| P50 / P90 LCOM | 0,0 / 10,0 | 0,0 / 3,0 | 0,0 / 3,0 | 0,0 / 3,0 | 0,0 / 3,0 | 0,0 / 3,0 | 0,0 / 3,0 |
| WMC médio — Mid-Level | 2,50 | 2,50 | 2,25 | 2,25 | 2,25 | 2,25 | 2,25 |
| LCOM médio — Mid-Level | 5,98 | 1,85 | 1,62 | 1,62 | 1,62 | 1,62 | 1,62 |
| CWC/classe médio — Mid-Level | 62,00 | 60,83 | 53,78 | 53,78 | 53,78 | 53,78 | 60,88 |
| WMC médio — Senior | 2,44 | 2,44 | 2,19 | 2,19 | 2,19 | 2,19 | 2,19 |
| LCOM médio — Senior | 7,12 | 2,21 | 1,89 | 1,89 | 1,89 | 1,89 | 1,89 |
| CWC/classe médio — Senior | 67,83 | 66,54 | 59,46 | 59,53 | 59,53 | 59,53 | 63,05 |
| WMC médio — Expert | 1,58 | 1,58 | 1,44 | 1,44 | 1,44 | 1,44 | 1,44 |
| LCOM médio — Expert | 2,15 | 1,01 | 0,84 | 0,84 | 0,84 | 0,84 | 0,84 |
| CWC/classe médio — Expert | 44,39 | 43,84 | 39,93 | 39,94 | 39,94 | 39,94 | 41,66 |

## results-v1 → 00-head-231192a

Classes pareadas: 2657 · só em results-v1: 0 · só em 00-head-231192a: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 5753 | 5753 |
| DIT | 0 | 2827 | 2827 |
| NOC | 0 | 152 | 152 |
| CBO | 0 | 940 | 940 |
| RFC | 5 | 23091 | 23096 |
| LCOM | 806 | 14285 | 4923 |
| AC | 0 | 6924 | 6924 |
| CLC | 767 | 111395 | 116439 |
| CWC | 858 | 166857 | 162616 |

| Participante | Nível conhecido | Antes | Depois |
|---|---|---|---|
| p3 | Expert | Mid-Level | Senior |
| p4 | Expert | Expert | Senior |
| p5 | Mid-Level | Mid-Level | Senior |
| p7 | Senior | Mid-Level | Senior |
| p12 | Mid-Level | Mid-Level | Senior |
| p13 | Senior | Senior | Expert |
| p16 | Expert | Senior | Expert |
| p20 | Mid-Level | Mid-Level | Senior |
| p22 | Mid-Level | Beginner | Mid-Level |
| p23 | Mid-Level | Senior | Expert |
| p26 | Senior | Senior | Expert |
| p27 | Mid-Level | Mid-Level | Senior |
| p28 | Mid-Level | Mid-Level | Senior |
| p32 | Senior | Mid-Level | Senior |
| p38 | Senior | Beginner | Mid-Level |
| p41 | Senior | Mid-Level | Senior |
| p43 | Expert | Mid-Level | Senior |
| p48 | Senior | Mid-Level | Senior |

## 00-head-231192a → 01-identidade

Classes pareadas: 2617 · só em 00-head-231192a: 40 · só em 01-identidade: 387

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 110 | 5606 | 5344 |
| DIT | 9 | 2787 | 2782 |
| NOC | 7 | 152 | 143 |
| CBO | 4 | 939 | 941 |
| RFC | 196 | 22171 | 21221 |
| LCOM | 76 | 4689 | 4267 |
| AC | 30 | 6919 | 6837 |
| CLC | 105 | 106975 | 104697 |
| CWC | 267 | 146798 | 137192 |

| Participante | Nível conhecido | Antes | Depois |
|---|---|---|---|
| p2 | Expert | Senior | Expert |
| p10 | Senior | Mid-Level | Senior |
| p25 | Senior | Senior | Expert |
| p47 | Mid-Level | Mid-Level | Senior |

## 01-identidade → 02-sobrecargas

Classes pareadas: 3004 · só em 01-identidade: 0 · só em 02-sobrecargas: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 5745 | 5745 |
| DIT | 0 | 3179 | 3179 |
| NOC | 0 | 160 | 160 |
| CBO | 0 | 1001 | 1001 |
| RFC | 0 | 24487 | 24487 |
| LCOM | 0 | 4478 | 4478 |
| AC | 0 | 7403 | 7403 |
| CLC | 55 | 123419 | 123833 |
| CWC | 35 | 162249 | 162354 |

| Participante | Nível conhecido | Antes | Depois |
|---|---|---|---|
| p1 | Mid-Level | Mid-Level | Senior |

## 02-sobrecargas → 03-vg-else-if

Classes pareadas: 3004 · só em 02-sobrecargas: 0 · só em 03-vg-else-if: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 5745 | 5745 |
| DIT | 0 | 3179 | 3179 |
| NOC | 0 | 160 | 160 |
| CBO | 0 | 1001 | 1001 |
| RFC | 0 | 24487 | 24487 |
| LCOM | 0 | 4478 | 4478 |
| AC | 0 | 7403 | 7403 |
| CLC | 0 | 123833 | 123833 |
| CWC | 0 | 162354 | 162354 |

Nenhum participante mudou de nível previsto.

## 03-vg-else-if → 04c-nuget-transitivo

Classes pareadas: 3004 · só em 03-vg-else-if: 0 · só em 04c-nuget-transitivo: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 5745 | 5745 |
| DIT | 3 | 3179 | 3182 |
| NOC | 1 | 160 | 163 |
| CBO | 0 | 1001 | 1001 |
| RFC | 0 | 24487 | 24487 |
| LCOM | 0 | 4478 | 4478 |
| AC | 0 | 7403 | 7403 |
| CLC | 0 | 123833 | 123833 |
| CWC | 0 | 162354 | 162354 |

Nenhum participante mudou de nível previsto.

## 04c-nuget-transitivo → 05-chamadas

Classes pareadas: 3004 · só em 04c-nuget-transitivo: 0 · só em 05-chamadas: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 5745 | 5745 |
| DIT | 0 | 3182 | 3182 |
| NOC | 0 | 163 | 163 |
| CBO | 0 | 1001 | 1001 |
| RFC | 822 | 24487 | 24999 |
| LCOM | 0 | 4478 | 4478 |
| AC | 0 | 7403 | 7403 |
| CLC | 0 | 123833 | 123833 |
| CWC | 967 | 162354 | 171894 |

| Participante | Nível conhecido | Antes | Depois |
|---|---|---|---|
| p12 | Mid-Level | Senior | Mid-Level |
