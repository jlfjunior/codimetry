# Análise CBTAM — results-v1

Participantes: 48 · classes: 2657 (de 3052 linhas, um registro por arquivo + classe) · métodos: 6839

## tab:per_class_averages

| Nível | N | WMC | LCOM | MC/classe | AC/classe | CWC/classe |
|---|---:|---:|---:|---:|---:|---:|
| Beginner | 1 | 1,86 (1,86) | 4,10 (4,10) | 21,48 (21,48) | 1,29 (1,29) | 30,52 (30,52) |
| Mid-Level | 13 | 2,50 (2,31) | 5,98 (4,33) | 41,51 (45,73) | 3,11 (3,09) | 62,00 (65,35) |
| Senior | 24 | 2,44 (2,18) | 7,12 (3,62) | 44,96 (44,87) | 2,60 (2,74) | 67,83 (66,54) |
| Expert | 10 | 1,58 (1,57) | 2,15 (2,06) | 31,61 (33,59) | 2,33 (2,31) | 44,39 (48,32) |

## tab:codebase_size

| Nível | N | LOC média (mediana) | Classes média (mediana) |
|---|---:|---:|---:|
| Beginner | 1 | 405 (405) | 21,0 (21,0) |
| Mid-Level | 13 | 1559 (1233) | 43,9 (44,0) |
| Senior | 24 | 1905 (1477) | 52,4 (46,5) |
| Expert | 10 | 2670 (2275) | 80,7 (70,0) |

## metric_redundancy

Pearson entre participantes:

- clc_total × loc: r = 0,932
- cwc_total × loc: r = 0,883
- coc × loc: r = 0,807
- cyclomatic × loc: r = 0,962
- clc_total × classes: r = 0,870
- cwc_total × classes: r = 0,821
- coc × classes: r = 0,852
- cyclomatic × classes: r = 0,935
- cwc_total × cyclomatic: r = 0,858
- clc_total × cyclomatic: r = 0,908
- cwc_total × coc: r = 0,919
- clc_total × coc: r = 0,920
- mc_total × clc_total: r = 0,997
- wmc_avg × lcom_avg: r = 0,762
- cbo_avg × loc: r = -0,101
- cbo_avg × classes: r = -0,110
- cbo_avg × classes_raw: r = -0,132
- dit_avg × noc_avg: r = 0,940
- DIT × NOC por classe (n = 2657): r = 0,019

- Classes com DIT = 1: 94,2%
- Classes com NOC = 0: 97,3%
- Amplitude da LCOM média por participante: 0,42 – 57,92

## position_analysis

| Cargo | N | LOC média | WMC média | LCOM média |
|---|---:|---:|---:|---:|
| Architect | 27 | 2239 | 2,41 | 6,72 |
| Developer | 21 | 1553 | 2,07 | 4,42 |

## empirical_application

Limiares absolutos (% Normal sobre todas as classes): WMC 99,7%, DIT 100,0%, NOC 100,0%

LCOM = WMC(WMC−1)/2 em 99,40% das classes.

Limiares relativos (amostra combinada):

| Métrica | População | P50 | P90 |
|---|---:|---:|---:|
| WMC | 2657 | 1,00 | 5,00 |
| DIT | 2657 | 1,00 | 1,00 |
| NOC | 2657 | 0,00 | 0,00 |
| LCOM | 2657 | 0,00 | 10,00 |
| AC | 2657 | 2,00 | 7,00 |
| CWC | 2657 | 21,00 | 159,00 |
| MC | 6839 | 9,00 | 35,00 |

## tab:confusion_matrix

| Conhecido \ Previsto | Beginner | Mid-Level | Senior | Expert |
|---|---:|---:|---:|---:|
| Beginner (n=1) | 0 | 0 | 0 | 1 |
| Mid-Level (n=13) | 1 | 7 | 5 | 0 |
| Senior (n=24) | 1 | 8 | 14 | 1 |
| Expert (n=10) | 0 | 2 | 5 | 3 |

- Acurácia (sem Beginner): 24/47 = 51,1%
- Linha de base (sempre Senior): 24/47 = 51,1%
- ρ de Spearman (regra em estágios × nível conhecido, n = 48): 0,286
- ρ de Spearman (média simples do % Normal × nível conhecido): 0,363
- Erros: 24 (20 adjacentes, 4 a dois ou mais níveis)

## tab:per_participant_results

| ID | Nível | WMC | DIT | NOC | LCOM | AC | CWC | MC | Previsto | Corr. | Discrepância |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| p1 | Mid-Level | 14,8 | 100,0 | 100,0 | 14,8 | 63,0 | 40,7 | 48,4 | Mid-Level | Sim | - |
| p2 | Expert | 58,3 | 100,0 | 100,0 | 58,3 | 45,8 | 95,8 | 74,3 | Senior | Não | Adjacente |
| p3 | Expert | 41,2 | 100,0 | 100,0 | 41,2 | 82,4 | 41,2 | 60,0 | Mid-Level | Não | Discrepante (-2) |
| p4 | Expert | 65,3 | 89,3 | 94,7 | 65,3 | 74,7 | 60,0 | 46,9 | Expert | Sim | - |
| p5 | Mid-Level | 30,0 | 95,0 | 95,0 | 30,0 | 45,0 | 70,0 | 63,3 | Mid-Level | Sim | - |
| p6 | Mid-Level | 51,1 | 100,0 | 100,0 | 51,1 | 54,3 | 48,9 | 44,1 | Senior | Não | Adjacente |
| p7 | Senior | 42,0 | 94,0 | 98,0 | 42,0 | 32,0 | 52,0 | 49,5 | Mid-Level | Não | Adjacente |
| p8 | Senior | 53,6 | 100,0 | 100,0 | 53,6 | 52,2 | 50,7 | 49,6 | Senior | Sim | - |
| p9 | Senior | 69,3 | 100,0 | 100,0 | 69,3 | 41,3 | 56,0 | 37,1 | Senior | Sim | - |
| p10 | Senior | 47,1 | 100,0 | 100,0 | 47,1 | 67,6 | 41,2 | 29,9 | Mid-Level | Não | Adjacente |
| p11 | Mid-Level | 51,1 | 95,7 | 93,6 | 51,1 | 55,3 | 40,4 | 44,4 | Senior | Não | Adjacente |
| p12 | Mid-Level | 47,7 | 97,7 | 97,7 | 47,7 | 45,5 | 45,5 | 54,4 | Mid-Level | Sim | - |
| p13 | Senior | 47,6 | 95,2 | 95,2 | 47,6 | 76,2 | 90,5 | 84,2 | Senior | Sim | - |
| p14 | Senior | 73,7 | 94,7 | 94,7 | 73,7 | 63,2 | 68,4 | 68,0 | Expert | Não | Adjacente |
| p15 | Expert | 75,8 | 93,4 | 95,6 | 75,8 | 70,3 | 65,9 | 42,8 | Expert | Sim | - |
| p16 | Expert | 51,7 | 83,7 | 94,6 | 51,7 | 63,9 | 61,9 | 64,4 | Senior | Não | Adjacente |
| p17 | Expert | 61,5 | 100,0 | 100,0 | 61,5 | 67,7 | 66,2 | 58,3 | Expert | Sim | - |
| p18 | Beginner | 61,9 | 90,5 | 95,2 | 61,9 | 85,7 | 57,1 | 74,4 | Expert | Não | Discrepante (+3) |
| p19 | Mid-Level | 60,0 | 82,2 | 97,8 | 68,9 | 73,3 | 44,4 | 37,8 | Senior | Não | Adjacente |
| p20 | Mid-Level | 39,4 | 90,9 | 93,9 | 39,4 | 57,6 | 42,4 | 70,7 | Mid-Level | Sim | - |
| p21 | Senior | 61,5 | 100,0 | 100,0 | 61,5 | 53,8 | 69,2 | 58,8 | Senior | Sim | - |
| p22 | Mid-Level | 23,8 | 90,5 | 95,2 | 23,8 | 33,3 | 28,6 | 43,3 | Beginner | Não | Adjacente |
| p23 | Mid-Level | 52,1 | 95,8 | 97,9 | 52,1 | 70,8 | 77,1 | 69,9 | Senior | Não | Adjacente |
| p24 | Senior | 50,5 | 96,0 | 99,0 | 50,5 | 57,6 | 58,6 | 60,8 | Senior | Sim | - |
| p25 | Senior | 51,4 | 94,3 | 97,1 | 51,4 | 72,9 | 55,7 | 69,3 | Senior | Sim | - |
| p26 | Senior | 52,0 | 86,0 | 92,0 | 52,0 | 62,0 | 62,0 | 73,5 | Senior | Sim | - |
| p27 | Mid-Level | 43,8 | 84,4 | 93,8 | 43,8 | 37,5 | 56,2 | 80,5 | Mid-Level | Sim | - |
| p28 | Mid-Level | 40,4 | 86,0 | 96,5 | 40,4 | 63,2 | 49,1 | 54,4 | Mid-Level | Sim | - |
| p29 | Senior | 54,8 | 88,1 | 93,3 | 54,8 | 78,5 | 48,1 | 49,1 | Senior | Sim | - |
| p30 | Senior | 60,9 | 93,5 | 95,7 | 60,9 | 76,1 | 34,8 | 36,9 | Senior | Sim | - |
| p31 | Senior | 24,5 | 100,0 | 100,0 | 24,5 | 51,0 | 14,3 | 46,1 | Mid-Level | Não | Adjacente |
| p32 | Senior | 13,0 | 100,0 | 100,0 | 13,0 | 69,6 | 34,8 | 50,7 | Mid-Level | Não | Adjacente |
| p33 | Senior | 54,8 | 88,1 | 93,3 | 54,8 | 78,5 | 48,1 | 49,1 | Senior | Sim | - |
| p34 | Senior | 70,3 | 94,6 | 97,3 | 70,3 | 75,7 | 37,8 | 13,4 | Senior | Sim | - |
| p35 | Senior | 10,3 | 100,0 | 100,0 | 10,3 | 44,8 | 41,4 | 46,6 | Mid-Level | Não | Adjacente |
| p36 | Senior | 23,3 | 100,0 | 100,0 | 23,3 | 56,7 | 26,7 | 41,6 | Mid-Level | Não | Adjacente |
| p37 | Senior | 59,0 | 100,0 | 100,0 | 59,0 | 78,7 | 34,4 | 36,9 | Senior | Sim | - |
| p38 | Senior | 22,8 | 92,4 | 96,2 | 22,8 | 53,2 | 32,9 | 39,0 | Beginner | Não | Discrepante (-2) |
| p39 | Mid-Level | 55,7 | 100,0 | 100,0 | 55,7 | 60,8 | 49,4 | 52,2 | Senior | Não | Adjacente |
| p40 | Expert | 80,0 | 87,5 | 97,5 | 80,0 | 65,0 | 47,5 | 42,4 | Senior | Não | Adjacente |
| p41 | Senior | 38,2 | 85,3 | 94,1 | 38,2 | 70,6 | 47,1 | 53,1 | Mid-Level | Não | Adjacente |
| p42 | Senior | 65,2 | 100,0 | 100,0 | 65,2 | 82,6 | 30,4 | 45,5 | Senior | Sim | - |
| p43 | Expert | 45,7 | 97,1 | 97,1 | 45,7 | 68,6 | 42,9 | 47,2 | Mid-Level | Não | Discrepante (-2) |
| p44 | Senior | 36,7 | 100,0 | 100,0 | 36,7 | 60,0 | 60,0 | 68,3 | Senior | Sim | - |
| p45 | Expert | 57,5 | 89,7 | 95,4 | 57,5 | 73,6 | 51,7 | 54,3 | Senior | Não | Adjacente |
| p46 | Expert | 64,2 | 100,0 | 100,0 | 64,2 | 63,3 | 46,0 | 47,5 | Senior | Não | Adjacente |
| p47 | Mid-Level | 38,5 | 100,0 | 100,0 | 38,5 | 23,1 | 42,3 | 56,5 | Mid-Level | Sim | - |
| p48 | Senior | 31,9 | 100,0 | 100,0 | 31,9 | 55,3 | 48,9 | 52,9 | Mid-Level | Não | Adjacente |
