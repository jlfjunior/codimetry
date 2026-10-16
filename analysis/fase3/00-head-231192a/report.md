# Análise CBTAM — 00-head-231192a

Participantes: 48 · classes: 2657 (de 3052 linhas, um registro por arquivo + classe) · métodos: 6839

## tab:per_class_averages

| Nível | N | WMC | LCOM | MC/classe | AC/classe | CWC/classe |
|---|---:|---:|---:|---:|---:|---:|
| Beginner | 1 | 1,86 (1,86) | 0,29 (0,29) | 22,10 (22,10) | 1,29 (1,29) | 28,71 (28,71) |
| Mid-Level | 13 | 2,50 (2,31) | 1,85 (1,19) | 43,69 (46,66) | 3,11 (3,09) | 60,83 (62,52) |
| Senior | 24 | 2,44 (2,18) | 2,21 (1,18) | 46,79 (46,23) | 2,60 (2,74) | 66,54 (63,52) |
| Expert | 10 | 1,58 (1,57) | 1,01 (0,91) | 33,13 (35,83) | 2,33 (2,31) | 43,84 (47,56) |

## tab:codebase_size

| Nível | N | LOC média (mediana) | Classes média (mediana) |
|---|---:|---:|---:|
| Beginner | 1 | 405 (405) | 21,0 (21,0) |
| Mid-Level | 13 | 1559 (1233) | 43,9 (44,0) |
| Senior | 24 | 1905 (1477) | 52,4 (46,5) |
| Expert | 10 | 2670 (2275) | 80,7 (70,0) |

## metric_redundancy

Pearson entre participantes:

- clc_total × loc: r = 0,941
- cwc_total × loc: r = 0,861
- coc × loc: r = 0,809
- cyclomatic × loc: r = 0,962
- clc_total × classes: r = 0,876
- cwc_total × classes: r = 0,796
- coc × classes: r = 0,851
- cyclomatic × classes: r = 0,935
- cwc_total × cyclomatic: r = 0,839
- clc_total × cyclomatic: r = 0,920
- cwc_total × coc: r = 0,916
- clc_total × coc: r = 0,915
- mc_total × clc_total: r = 0,997
- wmc_avg × lcom_avg: r = 0,684
- cbo_avg × loc: r = -0,101
- cbo_avg × classes: r = -0,110
- cbo_avg × classes_raw: r = -0,132
- dit_avg × noc_avg: r = 0,940
- DIT × NOC por classe (n = 2657): r = 0,019

- Classes com DIT = 1: 94,2%
- Classes com NOC = 0: 97,3%
- Amplitude da LCOM média por participante: 0,00 – 14,97

## position_analysis

| Cargo | N | LOC média | WMC média | LCOM média |
|---|---:|---:|---:|---:|
| Architect | 27 | 2239 | 2,41 | 2,19 |
| Developer | 21 | 1553 | 2,07 | 1,34 |

## empirical_application

Limiares absolutos (% Normal sobre todas as classes): WMC 99,7%, DIT 100,0%, NOC 100,0%

LCOM = WMC(WMC−1)/2 em 69,48% das classes.

Limiares relativos (amostra combinada):

| Métrica | População | P50 | P90 |
|---|---:|---:|---:|
| WMC | 2657 | 1,00 | 5,00 |
| DIT | 2657 | 1,00 | 1,00 |
| NOC | 2657 | 0,00 | 0,00 |
| LCOM | 2657 | 0,00 | 3,00 |
| AC | 2657 | 2,00 | 7,00 |
| CWC | 2657 | 20,00 | 157,40 |
| MC | 6839 | 10,00 | 38,00 |

## tab:confusion_matrix

| Conhecido \ Previsto | Beginner | Mid-Level | Senior | Expert |
|---|---:|---:|---:|---:|
| Beginner (n=1) | 0 | 0 | 0 | 1 |
| Mid-Level (n=13) | 0 | 3 | 9 | 1 |
| Senior (n=24) | 0 | 5 | 16 | 3 |
| Expert (n=10) | 0 | 0 | 7 | 3 |

- Acurácia (sem Beginner): 22/47 = 46,8%
- Linha de base (sempre Senior): 24/47 = 51,1%
- ρ de Spearman (regra em estágios × nível conhecido, n = 48): 0,177
- ρ de Spearman (média simples do % Normal × nível conhecido): 0,348
- Erros: 26 (24 adjacentes, 2 a dois ou mais níveis)

## tab:per_participant_results

| ID | Nível | WMC | DIT | NOC | LCOM | AC | CWC | MC | Previsto | Corr. | Discrepância |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| p1 | Mid-Level | 14,8 | 100,0 | 100,0 | 85,2 | 63,0 | 33,3 | 48,4 | Mid-Level | Sim | - |
| p2 | Expert | 58,3 | 100,0 | 100,0 | 95,8 | 45,8 | 91,7 | 82,9 | Senior | Não | Adjacente |
| p3 | Expert | 41,2 | 100,0 | 100,0 | 70,6 | 82,4 | 29,4 | 62,5 | Senior | Não | Adjacente |
| p4 | Expert | 65,3 | 89,3 | 94,7 | 85,3 | 74,7 | 57,3 | 48,3 | Senior | Não | Adjacente |
| p5 | Mid-Level | 30,0 | 95,0 | 95,0 | 75,0 | 45,0 | 60,0 | 63,3 | Senior | Não | Adjacente |
| p6 | Mid-Level | 51,1 | 100,0 | 100,0 | 83,7 | 54,3 | 47,8 | 44,7 | Senior | Não | Adjacente |
| p7 | Senior | 42,0 | 94,0 | 98,0 | 94,0 | 32,0 | 52,0 | 52,3 | Senior | Sim | - |
| p8 | Senior | 53,6 | 100,0 | 100,0 | 87,0 | 52,2 | 49,3 | 50,0 | Senior | Sim | - |
| p9 | Senior | 69,3 | 100,0 | 100,0 | 89,3 | 41,3 | 56,0 | 37,1 | Senior | Sim | - |
| p10 | Senior | 47,1 | 100,0 | 100,0 | 76,5 | 67,6 | 41,2 | 29,9 | Mid-Level | Não | Adjacente |
| p11 | Mid-Level | 51,1 | 95,7 | 93,6 | 87,2 | 55,3 | 38,3 | 42,9 | Senior | Não | Adjacente |
| p12 | Mid-Level | 47,7 | 97,7 | 97,7 | 75,0 | 45,5 | 50,0 | 52,9 | Senior | Não | Adjacente |
| p13 | Senior | 47,6 | 95,2 | 95,2 | 90,5 | 76,2 | 90,5 | 89,5 | Expert | Não | Adjacente |
| p14 | Senior | 73,7 | 94,7 | 94,7 | 89,5 | 63,2 | 63,2 | 68,0 | Expert | Não | Adjacente |
| p15 | Expert | 75,8 | 93,4 | 95,6 | 90,1 | 70,3 | 60,4 | 41,4 | Expert | Sim | - |
| p16 | Expert | 51,7 | 83,7 | 94,6 | 73,5 | 63,9 | 62,6 | 64,4 | Expert | Sim | - |
| p17 | Expert | 61,5 | 100,0 | 100,0 | 95,4 | 67,7 | 64,6 | 58,3 | Expert | Sim | - |
| p18 | Beginner | 61,9 | 90,5 | 95,2 | 90,5 | 85,7 | 57,1 | 74,4 | Expert | Não | Discrepante (+3) |
| p19 | Mid-Level | 60,0 | 82,2 | 97,8 | 80,0 | 73,3 | 44,4 | 44,4 | Senior | Não | Adjacente |
| p20 | Mid-Level | 39,4 | 90,9 | 93,9 | 78,8 | 57,6 | 48,5 | 69,9 | Senior | Não | Adjacente |
| p21 | Senior | 61,5 | 100,0 | 100,0 | 100,0 | 53,8 | 69,2 | 58,8 | Senior | Sim | - |
| p22 | Mid-Level | 23,8 | 90,5 | 95,2 | 71,4 | 33,3 | 28,6 | 46,7 | Mid-Level | Sim | - |
| p23 | Mid-Level | 52,1 | 95,8 | 97,9 | 83,3 | 70,8 | 79,2 | 74,2 | Expert | Não | Discrepante (+2) |
| p24 | Senior | 50,5 | 96,0 | 99,0 | 75,8 | 57,6 | 64,6 | 59,3 | Senior | Sim | - |
| p25 | Senior | 51,4 | 94,3 | 97,1 | 78,6 | 72,9 | 58,6 | 69,9 | Senior | Sim | - |
| p26 | Senior | 52,0 | 86,0 | 92,0 | 72,0 | 62,0 | 62,0 | 72,6 | Expert | Não | Adjacente |
| p27 | Mid-Level | 43,8 | 84,4 | 93,8 | 75,0 | 37,5 | 59,4 | 79,3 | Senior | Não | Adjacente |
| p28 | Mid-Level | 40,4 | 86,0 | 96,5 | 84,2 | 63,2 | 49,1 | 53,8 | Senior | Não | Adjacente |
| p29 | Senior | 54,8 | 88,1 | 93,3 | 68,1 | 78,5 | 50,4 | 50,1 | Senior | Sim | - |
| p30 | Senior | 60,9 | 93,5 | 95,7 | 76,1 | 76,1 | 34,8 | 36,9 | Senior | Sim | - |
| p31 | Senior | 24,5 | 100,0 | 100,0 | 77,6 | 51,0 | 18,4 | 47,6 | Mid-Level | Não | Adjacente |
| p32 | Senior | 13,0 | 100,0 | 100,0 | 60,9 | 69,6 | 30,4 | 50,7 | Senior | Sim | - |
| p33 | Senior | 54,8 | 88,1 | 93,3 | 68,1 | 78,5 | 50,4 | 50,1 | Senior | Sim | - |
| p34 | Senior | 70,3 | 94,6 | 97,3 | 83,8 | 75,7 | 37,8 | 14,4 | Senior | Sim | - |
| p35 | Senior | 10,3 | 100,0 | 100,0 | 79,3 | 44,8 | 41,4 | 46,6 | Mid-Level | Não | Adjacente |
| p36 | Senior | 23,3 | 100,0 | 100,0 | 73,3 | 56,7 | 23,3 | 46,5 | Mid-Level | Não | Adjacente |
| p37 | Senior | 59,0 | 100,0 | 100,0 | 72,1 | 78,7 | 37,7 | 43,1 | Senior | Sim | - |
| p38 | Senior | 22,8 | 92,4 | 96,2 | 78,5 | 53,2 | 31,6 | 40,2 | Mid-Level | Não | Adjacente |
| p39 | Mid-Level | 55,7 | 100,0 | 100,0 | 81,0 | 60,8 | 46,8 | 51,7 | Senior | Não | Adjacente |
| p40 | Expert | 80,0 | 87,5 | 97,5 | 87,5 | 65,0 | 40,0 | 44,1 | Senior | Não | Adjacente |
| p41 | Senior | 38,2 | 85,3 | 94,1 | 55,9 | 70,6 | 44,1 | 54,9 | Senior | Sim | - |
| p42 | Senior | 65,2 | 100,0 | 100,0 | 69,6 | 82,6 | 30,4 | 45,5 | Senior | Sim | - |
| p43 | Expert | 45,7 | 97,1 | 97,1 | 82,9 | 68,6 | 42,9 | 51,4 | Senior | Não | Adjacente |
| p44 | Senior | 36,7 | 100,0 | 100,0 | 83,3 | 60,0 | 56,7 | 69,8 | Senior | Sim | - |
| p45 | Expert | 57,5 | 89,7 | 95,4 | 63,2 | 73,6 | 52,9 | 59,1 | Senior | Não | Adjacente |
| p46 | Expert | 64,2 | 100,0 | 100,0 | 73,5 | 63,3 | 46,0 | 46,4 | Senior | Não | Adjacente |
| p47 | Mid-Level | 38,5 | 100,0 | 100,0 | 76,9 | 23,1 | 46,2 | 55,1 | Mid-Level | Sim | - |
| p48 | Senior | 31,9 | 100,0 | 100,0 | 80,9 | 55,3 | 51,1 | 56,1 | Senior | Sim | - |
