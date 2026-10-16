# Comparação entre passos

## Números principais

| Número | results-v1 | 00-fim-fase3 | 01-loc | 02-lcom | 04-mc-composicao-ac | 05-wmc | 06-vg-operadores | 07-records-structs | 08-vg-agregada | 09-mc-chamadas-lambdas-switch | 10-cwc | 11-dit | 12-cbo | 13-rfc | 14-coc-v2 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Classes | 2657 | 3004 | 3004 | 3004 | 3004 | 3004 | 3004 | 3373 | 3373 | 3373 | 3373 | 3373 | 3373 | 3373 | 3373 |
| Métodos (MC) | 6839 | 6831 | 6831 | 6831 | 6804 | 6804 | 6804 | 6876 | 6876 | 6876 | 6876 | 6876 | 6876 | 6876 | 6876 |
| LCOM = n(n−1)/2 (%) | 99,40 | 71,87 | 71,87 | 66,15 | 66,15 | 80,39 | 80,39 | 82,42 | 82,42 | 82,42 | 82,42 | 82,42 | 82,42 | 82,42 | 82,42 |
| Acurácia (%) | 51,1 | 46,8 | 46,8 | 46,8 | 44,7 | 40,4 | 40,4 | 48,9 | 48,9 | 55,3 | 48,9 | 46,8 | 46,8 | 46,8 | 46,8 |
| Linha de base (%) | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 | 51,1 |
| ρ regra em estágios | 0,286 | 0,198 | 0,198 | 0,198 | 0,349 | 0,313 | 0,313 | 0,109 | 0,109 | 0,185 | 0,167 | 0,111 | 0,111 | 0,111 | 0,111 |
| ρ média do % Normal | 0,363 | 0,333 | 0,333 | 0,345 | 0,355 | 0,297 | 0,297 | 0,354 | 0,354 | 0,295 | 0,346 | 0,360 | 0,360 | 0,360 | 0,360 |
| Erros (adjacentes / ≥ 2) | 24 (20 / 4) | 26 (24 / 2) | 26 (24 / 2) | 26 (24 / 2) | 27 (24 / 3) | 29 (24 / 5) | 29 (24 / 5) | 25 (22 / 3) | 25 (22 / 3) | 22 (19 / 3) | 25 (22 / 3) | 26 (23 / 3) | 26 (23 / 3) | 26 (23 / 3) | 26 (23 / 3) |
| P50 / P90 MC | 9,0 / 35,0 | 10,0 / 38,0 | 10,0 / 38,0 | 10,0 / 38,0 | 1,0 / 3,0 | 1,0 / 3,0 | 1,0 / 3,0 | 1,0 / 3,0 | 1,0 / 3,0 | 3,0 / 43,0 | 3,0 / 43,0 | 3,0 / 43,0 | 3,0 / 43,0 | 3,0 / 43,0 | 3,0 / 43,0 |
| P50 / P90 AC | 2,0 / 7,0 | 2,0 / 6,0 | 2,0 / 6,0 | 2,0 / 6,0 | 1,0 / 4,0 | 1,0 / 4,0 | 1,0 / 4,0 | 1,0 / 5,0 | 1,0 / 5,0 | 1,0 / 5,0 | 1,0 / 5,0 | 1,0 / 5,0 | 1,0 / 5,0 | 1,0 / 5,0 | 1,0 / 5,0 |
| P50 / P90 CWC | 21,0 / 159,0 | 18,0 / 140,4 | 18,0 / 140,4 | 18,0 / 140,4 | 15,0 / 105,0 | 15,0 / 105,0 | 15,0 / 105,0 | 12,0 / 93,0 | 12,0 / 93,0 | 12,0 / 132,0 | 0,0 / 41,8 | 0,0 / 41,8 | 0,0 / 41,8 | 0,0 / 41,8 | 0,0 / 41,8 |
| P50 / P90 LCOM | 0,0 / 10,0 | 0,0 / 3,0 | 0,0 / 3,0 | 0,0 / 1,0 | 0,0 / 1,0 | 0,0 / 1,0 | 0,0 / 1,0 | 0,0 / 1,0 | 0,0 / 1,0 | 0,0 / 1,0 | 0,0 / 1,0 | 0,0 / 1,0 | 0,0 / 1,0 | 0,0 / 1,0 | 0,0 / 1,0 |
| WMC médio — Mid-Level | 2,50 | 2,25 | 2,25 | 2,24 | 2,24 | 2,44 | 2,44 | 2,25 | 2,25 | 2,25 | 2,25 | 2,25 | 2,25 | 2,25 | 2,25 |
| LCOM médio — Mid-Level | 5,98 | 1,62 | 1,62 | 1,55 | 1,55 | 1,55 | 1,55 | 1,42 | 1,42 | 1,42 | 1,42 | 1,42 | 1,42 | 1,42 | 1,42 |
| CWC/classe médio — Mid-Level | 62,00 | 60,88 | 60,88 | 60,88 | 43,02 | 43,02 | 43,02 | 39,24 | 39,24 | 62,25 | 30,23 | 30,23 | 30,23 | 30,23 | 30,23 |
| WMC médio — Senior | 2,44 | 2,19 | 2,19 | 2,19 | 2,19 | 2,49 | 2,49 | 2,18 | 2,18 | 2,18 | 2,18 | 2,18 | 2,18 | 2,18 | 2,18 |
| LCOM médio — Senior | 7,12 | 1,89 | 1,89 | 1,77 | 1,77 | 1,77 | 1,77 | 1,51 | 1,51 | 1,51 | 1,51 | 1,51 | 1,51 | 1,51 | 1,51 |
| CWC/classe médio — Senior | 67,83 | 63,05 | 63,05 | 63,05 | 45,82 | 45,82 | 45,82 | 39,75 | 39,75 | 55,44 | 22,39 | 22,39 | 22,39 | 22,39 | 22,39 |
| WMC médio — Expert | 1,58 | 1,44 | 1,44 | 1,44 | 1,44 | 1,82 | 1,82 | 1,67 | 1,67 | 1,67 | 1,67 | 1,67 | 1,67 | 1,67 | 1,67 |
| LCOM médio — Expert | 2,15 | 0,84 | 0,84 | 0,66 | 0,66 | 0,66 | 0,66 | 0,56 | 0,56 | 0,56 | 0,56 | 0,56 | 0,56 | 0,56 | 0,56 |
| CWC/classe médio — Expert | 44,39 | 41,66 | 41,66 | 41,66 | 33,64 | 33,64 | 33,64 | 30,18 | 30,18 | 44,33 | 17,48 | 17,48 | 17,48 | 17,48 | 17,48 |

## results-v1 → 00-fim-fase3

Classes pareadas: 2617 · só em results-v1: 40 · só em 00-fim-fase3: 387

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 110 | 5606 | 5344 |
| DIT | 6 | 2787 | 2785 |
| NOC | 6 | 152 | 146 |
| CBO | 4 | 939 | 941 |
| RFC | 863 | 22166 | 21653 |
| LCOM | 862 | 14048 | 4267 |
| AC | 30 | 6919 | 6837 |
| CLC | 848 | 101931 | 105085 |
| CWC | 1213 | 151039 | 147072 |

| Participante | Nível conhecido | Antes | Depois |
|---|---|---|---|
| p1 | Mid-Level | Mid-Level | Senior |
| p2 | Expert | Senior | Expert |
| p3 | Expert | Mid-Level | Senior |
| p4 | Expert | Expert | Senior |
| p5 | Mid-Level | Mid-Level | Senior |
| p7 | Senior | Mid-Level | Senior |
| p10 | Senior | Mid-Level | Senior |
| p13 | Senior | Senior | Expert |
| p16 | Expert | Senior | Expert |
| p20 | Mid-Level | Mid-Level | Senior |
| p22 | Mid-Level | Beginner | Mid-Level |
| p23 | Mid-Level | Senior | Expert |
| p25 | Senior | Senior | Expert |
| p26 | Senior | Senior | Expert |
| p27 | Mid-Level | Mid-Level | Senior |
| p28 | Mid-Level | Mid-Level | Senior |
| p32 | Senior | Mid-Level | Senior |
| p38 | Senior | Beginner | Mid-Level |
| p41 | Senior | Mid-Level | Senior |
| p43 | Expert | Mid-Level | Senior |
| p47 | Mid-Level | Mid-Level | Senior |
| p48 | Senior | Mid-Level | Senior |

## 00-fim-fase3 → 01-loc

Classes pareadas: 3004 · só em 00-fim-fase3: 0 · só em 01-loc: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 5745 | 5745 |
| DIT | 0 | 3182 | 3182 |
| NOC | 0 | 163 | 163 |
| CBO | 0 | 1001 | 1001 |
| RFC | 0 | 24999 | 24999 |
| LCOM | 0 | 4478 | 4478 |
| AC | 0 | 7403 | 7403 |
| CLC | 0 | 123833 | 123833 |
| CWC | 0 | 171894 | 171894 |

Nenhum participante mudou de nível previsto.

## 01-loc → 02-lcom

Classes pareadas: 3004 · só em 01-loc: 0 · só em 02-lcom: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 4 | 5745 | 5741 |
| DIT | 0 | 3182 | 3182 |
| NOC | 0 | 163 | 163 |
| CBO | 0 | 1001 | 1001 |
| RFC | 0 | 24999 | 24999 |
| LCOM | 260 | 4478 | 4071 |
| AC | 0 | 7403 | 7403 |
| CLC | 0 | 123833 | 123833 |
| CWC | 0 | 171894 | 171894 |

Nenhum participante mudou de nível previsto.

## 02-lcom → 04-mc-composicao-ac

Classes pareadas: 3004 · só em 02-lcom: 0 · só em 04-mc-composicao-ac: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 5741 | 5741 |
| DIT | 0 | 3182 | 3182 |
| NOC | 0 | 163 | 163 |
| CBO | 0 | 1001 | 1001 |
| RFC | 0 | 24999 | 24999 |
| LCOM | 0 | 4071 | 4071 |
| AC | 1613 | 7403 | 4479 |
| CLC | 2595 | 123833 | 17482 |
| CWC | 897 | 171894 | 127917 |

| Participante | Nível conhecido | Antes | Depois |
|---|---|---|---|
| p4 | Expert | Senior | Expert |
| p24 | Senior | Senior | Expert |
| p29 | Senior | Senior | Expert |
| p30 | Senior | Senior | Expert |
| p31 | Senior | Mid-Level | Senior |
| p33 | Senior | Senior | Expert |
| p34 | Senior | Senior | Expert |
| p36 | Senior | Mid-Level | Senior |
| p37 | Senior | Senior | Expert |
| p38 | Senior | Mid-Level | Senior |
| p39 | Mid-Level | Senior | Expert |
| p40 | Expert | Senior | Expert |
| p42 | Senior | Senior | Expert |
| p46 | Expert | Senior | Expert |

## 04-mc-composicao-ac → 05-wmc

Classes pareadas: 3004 · só em 04-mc-composicao-ac: 0 · só em 05-wmc: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 685 | 5741 | 6831 |
| DIT | 0 | 3182 | 3182 |
| NOC | 0 | 163 | 163 |
| CBO | 0 | 1001 | 1001 |
| RFC | 0 | 24999 | 24999 |
| LCOM | 0 | 4071 | 4071 |
| AC | 0 | 4479 | 4479 |
| CLC | 0 | 17482 | 17482 |
| CWC | 0 | 127917 | 127917 |

| Participante | Nível conhecido | Antes | Depois |
|---|---|---|---|
| p12 | Mid-Level | Mid-Level | Senior |
| p21 | Senior | Senior | Expert |
| p27 | Mid-Level | Senior | Expert |
| p28 | Mid-Level | Senior | Expert |
| p35 | Senior | Mid-Level | Senior |
| p43 | Expert | Senior | Expert |
| p44 | Senior | Senior | Expert |
| p48 | Senior | Senior | Expert |

## 05-wmc → 06-vg-operadores

Classes pareadas: 3004 · só em 05-wmc: 0 · só em 06-vg-operadores: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 6831 | 6831 |
| DIT | 0 | 3182 | 3182 |
| NOC | 0 | 163 | 163 |
| CBO | 0 | 1001 | 1001 |
| RFC | 0 | 24999 | 24999 |
| LCOM | 0 | 4071 | 4071 |
| AC | 0 | 4479 | 4479 |
| CLC | 0 | 17482 | 17482 |
| CWC | 0 | 127917 | 127917 |

Nenhum participante mudou de nível previsto.

## 06-vg-operadores → 07-records-structs

Classes pareadas: 3004 · só em 06-vg-operadores: 0 · só em 07-records-structs: 369

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 6831 | 6831 |
| DIT | 0 | 3182 | 3182 |
| NOC | 0 | 163 | 163 |
| CBO | 265 | 1001 | 1323 |
| RFC | 0 | 24999 | 24999 |
| LCOM | 0 | 4071 | 4071 |
| AC | 0 | 4479 | 4479 |
| CLC | 0 | 17482 | 17482 |
| CWC | 4 | 127917 | 127945 |

| Participante | Nível conhecido | Antes | Depois |
|---|---|---|---|
| p2 | Expert | Expert | Senior |
| p4 | Expert | Expert | Senior |
| p12 | Mid-Level | Senior | Mid-Level |
| p15 | Expert | Expert | Senior |
| p21 | Senior | Expert | Senior |
| p24 | Senior | Expert | Senior |
| p26 | Senior | Expert | Senior |
| p28 | Mid-Level | Expert | Senior |
| p29 | Senior | Expert | Senior |
| p30 | Senior | Expert | Senior |
| p31 | Senior | Senior | Mid-Level |
| p33 | Senior | Expert | Senior |
| p34 | Senior | Expert | Senior |
| p35 | Senior | Senior | Mid-Level |
| p37 | Senior | Expert | Senior |
| p39 | Mid-Level | Expert | Senior |
| p43 | Expert | Expert | Senior |
| p44 | Senior | Expert | Senior |
| p46 | Expert | Expert | Senior |
| p48 | Senior | Expert | Senior |

## 07-records-structs → 08-vg-agregada

Classes pareadas: 3373 · só em 07-records-structs: 0 · só em 08-vg-agregada: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 6903 | 6903 |
| DIT | 0 | 3556 | 3556 |
| NOC | 0 | 176 | 176 |
| CBO | 0 | 1355 | 1355 |
| RFC | 0 | 25105 | 25105 |
| LCOM | 0 | 4075 | 4075 |
| AC | 0 | 5893 | 5893 |
| CLC | 0 | 19004 | 19004 |
| CWC | 0 | 128162 | 128162 |

Nenhum participante mudou de nível previsto.

## 08-vg-agregada → 09-mc-chamadas-lambdas-switch

Classes pareadas: 3373 · só em 08-vg-agregada: 0 · só em 09-mc-chamadas-lambdas-switch: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 6903 | 6903 |
| DIT | 0 | 3556 | 3556 |
| NOC | 0 | 176 | 176 |
| CBO | 0 | 1355 | 1355 |
| RFC | 0 | 25105 | 25105 |
| LCOM | 0 | 4075 | 4075 |
| AC | 0 | 5893 | 5893 |
| CLC | 1510 | 19004 | 152462 |
| CWC | 607 | 128162 | 205589 |

| Participante | Nível conhecido | Antes | Depois |
|---|---|---|---|
| p11 | Mid-Level | Senior | Mid-Level |
| p14 | Senior | Expert | Senior |
| p28 | Mid-Level | Senior | Mid-Level |
| p38 | Senior | Senior | Mid-Level |
| p40 | Expert | Expert | Senior |
| p42 | Senior | Expert | Senior |
| p47 | Mid-Level | Senior | Mid-Level |

## 09-mc-chamadas-lambdas-switch → 10-cwc

Classes pareadas: 3373 · só em 09-mc-chamadas-lambdas-switch: 0 · só em 10-cwc: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 6903 | 6903 |
| DIT | 0 | 3556 | 3556 |
| NOC | 0 | 176 | 176 |
| CBO | 0 | 1355 | 1355 |
| RFC | 0 | 25105 | 25105 |
| LCOM | 0 | 4075 | 4075 |
| AC | 0 | 5893 | 5893 |
| CLC | 0 | 152462 | 152462 |
| CWC | 2155 | 205589 | 98398 |

| Participante | Nível conhecido | Antes | Depois |
|---|---|---|---|
| p3 | Expert | Senior | Expert |
| p11 | Mid-Level | Mid-Level | Senior |
| p14 | Senior | Senior | Expert |
| p22 | Mid-Level | Mid-Level | Senior |
| p28 | Mid-Level | Mid-Level | Senior |
| p38 | Senior | Mid-Level | Senior |
| p40 | Expert | Senior | Expert |
| p42 | Senior | Senior | Expert |
| p44 | Senior | Senior | Expert |

## 10-cwc → 11-dit

Classes pareadas: 3373 · só em 10-cwc: 0 · só em 11-dit: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 6903 | 6903 |
| DIT | 2801 | 3556 | 755 |
| NOC | 0 | 176 | 176 |
| CBO | 0 | 1355 | 1355 |
| RFC | 0 | 25105 | 25105 |
| LCOM | 0 | 4075 | 4075 |
| AC | 0 | 5893 | 5893 |
| CLC | 0 | 152462 | 152462 |
| CWC | 0 | 98398 | 98398 |

| Participante | Nível conhecido | Antes | Depois |
|---|---|---|---|
| p3 | Expert | Expert | Senior |

## 11-dit → 12-cbo

Classes pareadas: 3373 · só em 11-dit: 0 · só em 12-cbo: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 6903 | 6903 |
| DIT | 0 | 755 | 755 |
| NOC | 0 | 176 | 176 |
| CBO | 2265 | 1355 | 8050 |
| RFC | 0 | 25105 | 25105 |
| LCOM | 0 | 4075 | 4075 |
| AC | 0 | 5893 | 5893 |
| CLC | 0 | 152462 | 152462 |
| CWC | 0 | 98398 | 98398 |

Nenhum participante mudou de nível previsto.

## 12-cbo → 13-rfc

Classes pareadas: 3373 · só em 12-cbo: 0 · só em 13-rfc: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 6903 | 6903 |
| DIT | 0 | 755 | 755 |
| NOC | 0 | 176 | 176 |
| CBO | 0 | 8050 | 8050 |
| RFC | 1476 | 25105 | 28581 |
| LCOM | 0 | 4075 | 4075 |
| AC | 0 | 5893 | 5893 |
| CLC | 0 | 152462 | 152462 |
| CWC | 0 | 98398 | 98398 |

Nenhum participante mudou de nível previsto.

## 13-rfc → 14-coc-v2

Classes pareadas: 3373 · só em 13-rfc: 0 · só em 14-coc-v2: 0

| Métrica | Classes que mudaram | Soma antes | Soma depois |
|---|---:|---:|---:|
| WMC | 0 | 6903 | 6903 |
| DIT | 0 | 755 | 755 |
| NOC | 0 | 176 | 176 |
| CBO | 0 | 8050 | 8050 |
| RFC | 0 | 28581 | 28581 |
| LCOM | 0 | 4075 | 4075 |
| AC | 0 | 5893 | 5893 |
| CLC | 0 | 152462 | 152462 |
| CWC | 0 | 98398 | 98398 |

Nenhum participante mudou de nível previsto.
