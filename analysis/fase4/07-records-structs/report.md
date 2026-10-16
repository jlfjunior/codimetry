# Análise CBTAM — 07-records-structs

Participantes: 48 · classes: 3373 (de 3376 linhas, um registro por arquivo + classe) · métodos: 6876

## tab:per_class_averages

| Nível | N | WMC | LCOM | MC/classe | AC/classe | CWC/classe |
|---|---:|---:|---:|---:|---:|---:|
| Beginner | 1 | 1,86 (1,86) | 0,29 (0,29) | 2,24 (2,24) | 0,86 (0,86) | 25,19 (25,19) |
| Mid-Level | 13 | 2,25 (2,35) | 1,42 (0,58) | 3,58 (3,53) | 2,00 (2,02) | 39,24 (37,80) |
| Senior | 24 | 2,18 (1,99) | 1,51 (0,66) | 4,49 (3,37) | 1,78 (1,77) | 39,75 (36,82) |
| Expert | 10 | 1,67 (1,62) | 0,56 (0,52) | 2,80 (2,48) | 1,59 (1,55) | 30,18 (29,89) |

## tab:codebase_size

| Nível | N | LOC média (mediana) | Classes média (mediana) |
|---|---:|---:|---:|
| Beginner | 1 | 254 (254) | 21,0 (21,0) |
| Mid-Level | 13 | 991 (914) | 53,9 (48,0) |
| Senior | 24 | 1161 (937) | 69,0 (57,0) |
| Expert | 10 | 1419 (1255) | 99,5 (84,5) |

## metric_redundancy

Pearson entre participantes:

- clc_total × loc: r = 0,913
- cwc_total × loc: r = 0,926
- coc × loc: r = 0,952
- cyclomatic × loc: r = 0,964
- clc_total × classes: r = 0,816
- cwc_total × classes: r = 0,845
- coc × classes: r = 0,866
- cyclomatic × classes: r = 0,913
- cwc_total × cyclomatic: r = 0,844
- clc_total × cyclomatic: r = 0,906
- cwc_total × coc: r = 0,988
- clc_total × coc: r = 0,913
- mc_total × clc_total: r = 0,986
- wmc_avg × lcom_avg: r = 0,769
- cbo_avg × loc: r = 0,001
- cbo_avg × classes: r = -0,072
- cbo_avg × classes_raw: r = -0,072
- dit_avg × noc_avg: r = 0,983
- DIT × NOC por classe (n = 3373): r = 0,020

- Classes com DIT = 1: 94,4%
- Classes com NOC = 0: 97,4%
- Amplitude da LCOM média por participante: 0,00 – 12,38

## position_analysis

| Cargo | N | LOC média | WMC média | LCOM média |
|---|---:|---:|---:|---:|
| Architect | 27 | 1293 | 2,20 | 1,50 |
| Developer | 21 | 965 | 1,94 | 0,96 |

## empirical_application

Limiares absolutos (% Normal sobre todas as classes): WMC 99,8%, DIT 100,0%, NOC 100,0%

LCOM = n(n−1)/2 (n = métodos da LCOM; WMC na v1) em 82,42% das classes.

Tipos medidos por Kind: class 3004, record 358, record struct 9, struct 2.

Limiares relativos (amostra combinada):

| Métrica | População | P50 | P90 |
|---|---:|---:|---:|
| WMC | 3373 | 1,00 | 5,00 |
| DIT | 3373 | 1,00 | 1,00 |
| NOC | 3373 | 0,00 | 0,00 |
| LCOM | 3373 | 0,00 | 1,00 |
| AC | 3373 | 1,00 | 5,00 |
| CWC | 3373 | 12,00 | 93,00 |
| MC | 6876 | 1,00 | 3,00 |

## tab:confusion_matrix

| Conhecido \ Previsto | Beginner | Mid-Level | Senior | Expert |
|---|---:|---:|---:|---:|
| Beginner (n=1) | 0 | 0 | 0 | 1 |
| Mid-Level (n=13) | 0 | 2 | 9 | 2 |
| Senior (n=24) | 0 | 2 | 18 | 4 |
| Expert (n=10) | 0 | 0 | 7 | 3 |

- Acurácia (sem Beginner): 23/47 = 48,9%
- Linha de base (sempre Senior): 24/47 = 51,1%
- ρ de Spearman (regra em estágios × nível conhecido, n = 48): 0,109
- ρ de Spearman (média simples do % Normal × nível conhecido): 0,354
- Erros: 25 (22 adjacentes, 3 a dois ou mais níveis)

## tab:per_participant_results

| ID | Nível | WMC | DIT | NOC | LCOM | AC | CWC | MC | Previsto | Corr. | Discrepância |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| p1 | Mid-Level | 41,0 | 100,0 | 100,0 | 94,9 | 48,7 | 53,8 | 73,7 | Senior | Não | Adjacente |
| p2 | Expert | 59,3 | 100,0 | 100,0 | 96,3 | 51,9 | 81,5 | 100,0 | Senior | Não | Adjacente |
| p3 | Expert | 47,4 | 100,0 | 100,0 | 84,2 | 78,9 | 36,8 | 90,0 | Senior | Não | Adjacente |
| p4 | Expert | 57,1 | 91,8 | 95,9 | 89,8 | 69,4 | 54,1 | 79,4 | Senior | Não | Adjacente |
| p5 | Mid-Level | 33,3 | 95,2 | 95,2 | 85,7 | 47,6 | 57,1 | 83,3 | Senior | Não | Adjacente |
| p6 | Mid-Level | 54,5 | 100,0 | 100,0 | 87,3 | 48,2 | 54,5 | 81,2 | Senior | Não | Adjacente |
| p7 | Senior | 53,9 | 98,9 | 98,9 | 93,3 | 40,4 | 64,0 | 92,9 | Senior | Sim | - |
| p8 | Senior | 54,2 | 100,0 | 100,0 | 91,6 | 53,0 | 49,4 | 80,9 | Senior | Sim | - |
| p9 | Senior | 74,4 | 100,0 | 100,0 | 94,4 | 43,3 | 54,4 | 77,3 | Senior | Sim | - |
| p10 | Senior | 60,5 | 100,0 | 100,0 | 86,0 | 55,8 | 41,9 | 78,2 | Senior | Sim | - |
| p11 | Mid-Level | 56,4 | 90,9 | 90,9 | 90,9 | 47,3 | 47,3 | 87,2 | Senior | Não | Adjacente |
| p12 | Mid-Level | 46,7 | 97,8 | 97,8 | 75,6 | 42,2 | 37,8 | 86,3 | Mid-Level | Sim | - |
| p13 | Senior | 42,9 | 95,2 | 95,2 | 100,0 | 71,4 | 81,0 | 86,8 | Expert | Não | Adjacente |
| p14 | Senior | 63,2 | 94,7 | 94,7 | 94,7 | 57,9 | 63,2 | 88,0 | Expert | Não | Adjacente |
| p15 | Expert | 75,5 | 93,6 | 95,7 | 97,9 | 45,7 | 58,5 | 75,2 | Senior | Não | Adjacente |
| p16 | Expert | 55,7 | 87,1 | 94,8 | 84,5 | 70,1 | 64,4 | 88,2 | Expert | Sim | - |
| p17 | Expert | 62,7 | 100,0 | 100,0 | 94,7 | 69,3 | 57,3 | 70,8 | Expert | Sim | - |
| p18 | Beginner | 61,9 | 90,5 | 95,2 | 90,5 | 76,2 | 61,9 | 89,7 | Expert | Não | Discrepante (+3) |
| p19 | Mid-Level | 62,9 | 87,1 | 96,8 | 88,7 | 54,8 | 48,4 | 80,0 | Senior | Não | Adjacente |
| p20 | Mid-Level | 48,1 | 88,9 | 94,4 | 90,7 | 50,0 | 55,6 | 79,4 | Senior | Não | Adjacente |
| p21 | Senior | 57,1 | 100,0 | 100,0 | 100,0 | 57,1 | 50,0 | 82,4 | Senior | Sim | - |
| p22 | Mid-Level | 42,3 | 92,3 | 96,2 | 80,8 | 26,9 | 38,5 | 82,0 | Mid-Level | Sim | - |
| p23 | Mid-Level | 39,6 | 95,8 | 97,9 | 81,2 | 70,8 | 66,7 | 89,2 | Expert | Não | Discrepante (+2) |
| p24 | Senior | 54,2 | 96,7 | 99,2 | 81,7 | 55,8 | 65,0 | 79,1 | Senior | Sim | - |
| p25 | Senior | 58,7 | 93,5 | 96,7 | 89,1 | 75,0 | 65,2 | 92,8 | Expert | Não | Adjacente |
| p26 | Senior | 44,4 | 87,0 | 92,6 | 77,8 | 64,8 | 55,6 | 84,1 | Senior | Sim | - |
| p27 | Mid-Level | 60,4 | 81,2 | 93,8 | 85,4 | 41,7 | 72,9 | 84,9 | Expert | Não | Discrepante (+2) |
| p28 | Mid-Level | 37,7 | 86,9 | 96,7 | 83,6 | 62,3 | 44,3 | 89,9 | Senior | Não | Adjacente |
| p29 | Senior | 58,5 | 89,9 | 94,1 | 85,1 | 71,8 | 52,1 | 80,8 | Senior | Sim | - |
| p30 | Senior | 51,6 | 93,8 | 96,9 | 90,6 | 64,1 | 43,8 | 82,8 | Senior | Sim | - |
| p31 | Senior | 35,0 | 100,0 | 100,0 | 80,0 | 45,0 | 30,0 | 92,6 | Mid-Level | Não | Adjacente |
| p32 | Senior | 45,9 | 100,0 | 100,0 | 73,0 | 45,9 | 59,5 | 77,5 | Senior | Sim | - |
| p33 | Senior | 58,5 | 89,9 | 94,1 | 85,1 | 71,8 | 52,1 | 80,8 | Senior | Sim | - |
| p34 | Senior | 68,9 | 95,6 | 97,8 | 88,9 | 55,6 | 44,4 | 82,5 | Senior | Sim | - |
| p35 | Senior | 17,1 | 100,0 | 100,0 | 94,3 | 45,7 | 31,4 | 88,6 | Mid-Level | Não | Adjacente |
| p36 | Senior | 35,9 | 100,0 | 100,0 | 89,7 | 51,3 | 35,9 | 91,1 | Senior | Sim | - |
| p37 | Senior | 59,3 | 100,0 | 100,0 | 87,2 | 70,9 | 44,2 | 87,5 | Senior | Sim | - |
| p38 | Senior | 44,1 | 94,9 | 97,5 | 93,2 | 50,0 | 40,7 | 86,1 | Senior | Sim | - |
| p39 | Mid-Level | 53,8 | 100,0 | 100,0 | 87,5 | 64,4 | 51,9 | 86,4 | Senior | Não | Adjacente |
| p40 | Expert | 68,6 | 82,4 | 94,1 | 90,2 | 62,7 | 43,1 | 93,7 | Expert | Sim | - |
| p41 | Senior | 40,9 | 88,6 | 93,2 | 72,7 | 61,4 | 43,2 | 88,3 | Senior | Sim | - |
| p42 | Senior | 70,0 | 100,0 | 100,0 | 83,3 | 70,0 | 56,7 | 78,2 | Expert | Não | Adjacente |
| p43 | Expert | 57,4 | 97,9 | 97,9 | 93,6 | 68,1 | 46,8 | 83,1 | Senior | Não | Adjacente |
| p44 | Senior | 36,7 | 100,0 | 100,0 | 90,0 | 60,0 | 46,7 | 87,3 | Senior | Sim | - |
| p45 | Expert | 54,9 | 91,2 | 95,6 | 77,0 | 59,3 | 60,2 | 75,7 | Senior | Não | Adjacente |
| p46 | Expert | 51,6 | 100,0 | 100,0 | 83,4 | 58,1 | 49,1 | 62,8 | Senior | Não | Adjacente |
| p47 | Mid-Level | 35,7 | 100,0 | 100,0 | 89,3 | 25,0 | 53,6 | 73,9 | Senior | Não | Adjacente |
| p48 | Senior | 43,3 | 100,0 | 100,0 | 94,0 | 59,7 | 56,7 | 75,5 | Senior | Sim | - |
