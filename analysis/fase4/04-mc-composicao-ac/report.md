# Análise CBTAM — 04-mc-composicao-ac

Participantes: 48 · classes: 3004 (de 3004 linhas, um registro por arquivo + classe) · métodos: 6804

## tab:per_class_averages

| Nível | N | WMC | LCOM | MC/classe | AC/classe | CWC/classe |
|---|---:|---:|---:|---:|---:|---:|
| Beginner | 1 | 1,86 (1,86) | 0,29 (0,29) | 2,24 (2,24) | 0,86 (0,86) | 25,19 (25,19) |
| Mid-Level | 13 | 2,24 (2,19) | 1,55 (0,65) | 3,91 (3,60) | 1,88 (1,81) | 43,02 (41,39) |
| Senior | 24 | 2,19 (1,85) | 1,77 (0,73) | 5,18 (3,71) | 1,48 (1,41) | 45,82 (42,76) |
| Expert | 10 | 1,44 (1,44) | 0,66 (0,57) | 3,08 (2,70) | 1,35 (1,29) | 33,64 (35,63) |

## tab:codebase_size

| Nível | N | LOC média (mediana) | Classes média (mediana) |
|---|---:|---:|---:|
| Beginner | 1 | 254 (254) | 21,0 (21,0) |
| Mid-Level | 13 | 991 (914) | 49,2 (48,0) |
| Senior | 24 | 1161 (937) | 60,5 (52,0) |
| Expert | 10 | 1419 (1255) | 89,0 (79,5) |

## metric_redundancy

Pearson entre participantes:

- clc_total × loc: r = 0,900
- cwc_total × loc: r = 0,924
- coc × loc: r = 0,949
- cyclomatic × loc: r = 0,962
- clc_total × classes: r = 0,789
- cwc_total × classes: r = 0,829
- coc × classes: r = 0,847
- cyclomatic × classes: r = 0,916
- cwc_total × cyclomatic: r = 0,848
- clc_total × cyclomatic: r = 0,900
- cwc_total × coc: r = 0,988
- clc_total × coc: r = 0,903
- mc_total × clc_total: r = 0,989
- wmc_avg × lcom_avg: r = 0,734
- cbo_avg × loc: r = -0,097
- cbo_avg × classes: r = -0,155
- cbo_avg × classes_raw: r = -0,155
- dit_avg × noc_avg: r = 0,989
- DIT × NOC por classe (n = 3004): r = 0,018

- Classes com DIT = 1: 94,6%
- Classes com NOC = 0: 97,3%
- Amplitude da LCOM média por participante: 0,00 – 14,66

## position_analysis

| Cargo | N | LOC média | WMC média | LCOM média |
|---|---:|---:|---:|---:|
| Architect | 27 | 1293 | 2,18 | 1,77 |
| Developer | 21 | 965 | 1,86 | 1,03 |

## empirical_application

Limiares absolutos (% Normal sobre todas as classes): WMC 99,8%, DIT 100,0%, NOC 100,0%

LCOM = n(n−1)/2 (n = métodos da LCOM; WMC na v1) em 66,15% das classes.

Tipos medidos por Kind: class 3004.

Limiares relativos (amostra combinada):

| Métrica | População | P50 | P90 |
|---|---:|---:|---:|
| WMC | 3004 | 1,00 | 5,00 |
| DIT | 3004 | 1,00 | 1,00 |
| NOC | 3004 | 0,00 | 0,00 |
| LCOM | 3004 | 0,00 | 1,00 |
| AC | 3004 | 1,00 | 4,00 |
| CWC | 3004 | 15,00 | 105,00 |
| MC | 6804 | 1,00 | 3,00 |

## tab:confusion_matrix

| Conhecido \ Previsto | Beginner | Mid-Level | Senior | Expert |
|---|---:|---:|---:|---:|
| Beginner (n=1) | 0 | 0 | 0 | 1 |
| Mid-Level (n=13) | 0 | 2 | 9 | 2 |
| Senior (n=24) | 0 | 1 | 12 | 11 |
| Expert (n=10) | 0 | 0 | 3 | 7 |

- Acurácia (sem Beginner): 21/47 = 44,7%
- Linha de base (sempre Senior): 24/47 = 51,1%
- ρ de Spearman (regra em estágios × nível conhecido, n = 48): 0,349
- ρ de Spearman (média simples do % Normal × nível conhecido): 0,355
- Erros: 27 (24 adjacentes, 3 a dois ou mais níveis)

## tab:per_participant_results

| ID | Nível | WMC | DIT | NOC | LCOM | AC | CWC | MC | Previsto | Corr. | Discrepância |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| p1 | Mid-Level | 14,8 | 100,0 | 100,0 | 92,6 | 63,0 | 40,7 | 73,7 | Senior | Não | Adjacente |
| p2 | Expert | 63,0 | 100,0 | 100,0 | 96,3 | 51,9 | 85,2 | 100,0 | Expert | Sim | - |
| p3 | Expert | 47,4 | 100,0 | 100,0 | 84,2 | 78,9 | 36,8 | 90,0 | Senior | Não | Adjacente |
| p4 | Expert | 69,0 | 90,5 | 95,2 | 88,1 | 77,4 | 52,4 | 79,4 | Expert | Sim | - |
| p5 | Mid-Level | 38,1 | 95,2 | 95,2 | 85,7 | 47,6 | 57,1 | 83,3 | Senior | Não | Adjacente |
| p6 | Mid-Level | 52,4 | 100,0 | 100,0 | 86,4 | 48,5 | 53,4 | 81,2 | Senior | Não | Adjacente |
| p7 | Senior | 47,8 | 100,0 | 100,0 | 91,0 | 44,8 | 59,7 | 93,6 | Senior | Sim | - |
| p8 | Senior | 59,0 | 100,0 | 100,0 | 91,6 | 53,0 | 50,6 | 80,9 | Senior | Sim | - |
| p9 | Senior | 78,7 | 100,0 | 100,0 | 94,4 | 43,8 | 56,2 | 77,3 | Senior | Sim | - |
| p10 | Senior | 56,4 | 100,0 | 100,0 | 84,6 | 59,0 | 41,0 | 77,9 | Senior | Sim | - |
| p11 | Mid-Level | 60,0 | 90,9 | 90,9 | 90,9 | 47,3 | 49,1 | 87,2 | Senior | Não | Adjacente |
| p12 | Mid-Level | 47,7 | 97,7 | 97,7 | 75,0 | 43,2 | 40,9 | 86,0 | Mid-Level | Sim | - |
| p13 | Senior | 47,6 | 95,2 | 95,2 | 100,0 | 71,4 | 90,5 | 86,8 | Expert | Não | Adjacente |
| p14 | Senior | 73,7 | 94,7 | 94,7 | 94,7 | 57,9 | 68,4 | 88,0 | Expert | Não | Adjacente |
| p15 | Expert | 77,7 | 93,6 | 95,7 | 97,9 | 45,7 | 64,9 | 75,2 | Expert | Sim | - |
| p16 | Expert | 58,0 | 86,2 | 94,5 | 83,4 | 71,8 | 65,7 | 88,2 | Expert | Sim | - |
| p17 | Expert | 66,7 | 100,0 | 100,0 | 94,7 | 69,3 | 62,7 | 70,8 | Expert | Sim | - |
| p18 | Beginner | 61,9 | 90,5 | 95,2 | 90,5 | 76,2 | 61,9 | 89,7 | Expert | Não | Discrepante (+3) |
| p19 | Mid-Level | 60,4 | 84,9 | 96,2 | 86,8 | 54,7 | 45,3 | 80,0 | Senior | Não | Adjacente |
| p20 | Mid-Level | 49,0 | 88,2 | 94,1 | 92,2 | 47,1 | 56,9 | 80,5 | Senior | Não | Adjacente |
| p21 | Senior | 57,1 | 100,0 | 100,0 | 100,0 | 57,1 | 64,3 | 82,4 | Senior | Sim | - |
| p22 | Mid-Level | 34,8 | 91,3 | 95,7 | 78,3 | 30,4 | 30,4 | 81,7 | Mid-Level | Sim | - |
| p23 | Mid-Level | 52,1 | 95,8 | 97,9 | 81,2 | 70,8 | 75,0 | 89,2 | Expert | Não | Discrepante (+2) |
| p24 | Senior | 51,0 | 96,0 | 99,0 | 79,0 | 62,0 | 64,0 | 78,7 | Expert | Não | Adjacente |
| p25 | Senior | 65,2 | 93,5 | 96,7 | 89,1 | 75,0 | 67,4 | 92,8 | Expert | Não | Adjacente |
| p26 | Senior | 55,6 | 87,0 | 92,6 | 77,8 | 64,8 | 70,4 | 84,1 | Expert | Não | Adjacente |
| p27 | Mid-Level | 52,8 | 83,3 | 94,4 | 80,6 | 50,0 | 63,9 | 84,9 | Senior | Não | Adjacente |
| p28 | Mid-Level | 39,7 | 86,2 | 96,6 | 82,8 | 63,8 | 48,3 | 89,5 | Senior | Não | Adjacente |
| p29 | Senior | 65,0 | 90,2 | 94,5 | 82,8 | 79,1 | 49,7 | 80,7 | Expert | Não | Adjacente |
| p30 | Senior | 64,2 | 92,5 | 96,2 | 88,7 | 71,7 | 37,7 | 82,8 | Expert | Não | Adjacente |
| p31 | Senior | 25,5 | 100,0 | 100,0 | 76,5 | 52,9 | 23,5 | 92,9 | Senior | Sim | - |
| p32 | Senior | 16,7 | 100,0 | 100,0 | 58,3 | 70,8 | 37,5 | 77,5 | Senior | Sim | - |
| p33 | Senior | 65,0 | 90,2 | 94,5 | 82,8 | 79,1 | 49,7 | 80,7 | Expert | Não | Adjacente |
| p34 | Senior | 71,1 | 94,7 | 97,4 | 86,8 | 65,8 | 36,8 | 82,5 | Expert | Não | Adjacente |
| p35 | Senior | 14,7 | 100,0 | 100,0 | 94,1 | 47,1 | 41,2 | 88,6 | Mid-Level | Não | Adjacente |
| p36 | Senior | 22,6 | 100,0 | 100,0 | 87,1 | 61,3 | 19,4 | 91,1 | Senior | Sim | - |
| p37 | Senior | 68,4 | 100,0 | 100,0 | 85,5 | 77,6 | 42,1 | 87,5 | Expert | Não | Adjacente |
| p38 | Senior | 28,1 | 93,3 | 96,6 | 91,0 | 64,0 | 24,7 | 86,1 | Senior | Sim | - |
| p39 | Mid-Level | 61,3 | 100,0 | 100,0 | 86,0 | 67,7 | 49,5 | 87,6 | Expert | Não | Discrepante (+2) |
| p40 | Expert | 80,5 | 87,8 | 97,6 | 87,8 | 70,7 | 34,1 | 93,2 | Expert | Sim | - |
| p41 | Senior | 40,0 | 87,5 | 92,5 | 70,0 | 67,5 | 42,5 | 88,3 | Senior | Sim | - |
| p42 | Senior | 66,7 | 100,0 | 100,0 | 79,2 | 79,2 | 45,8 | 78,2 | Expert | Não | Adjacente |
| p43 | Expert | 51,3 | 97,4 | 97,4 | 92,3 | 79,5 | 38,5 | 84,7 | Senior | Não | Adjacente |
| p44 | Senior | 36,7 | 100,0 | 100,0 | 90,0 | 60,0 | 53,3 | 87,3 | Senior | Sim | - |
| p45 | Expert | 58,0 | 89,8 | 95,5 | 70,5 | 72,7 | 55,7 | 75,2 | Senior | Não | Adjacente |
| p46 | Expert | 65,3 | 100,0 | 100,0 | 81,0 | 66,1 | 45,5 | 62,1 | Expert | Sim | - |
| p47 | Mid-Level | 39,3 | 100,0 | 100,0 | 89,3 | 25,0 | 57,1 | 73,9 | Senior | Não | Adjacente |
| p48 | Senior | 47,5 | 100,0 | 100,0 | 93,2 | 67,8 | 54,2 | 75,5 | Senior | Sim | - |
