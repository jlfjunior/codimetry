# Análise CBTAM — 13-rfc

Participantes: 48 · classes: 3373 (de 3376 linhas, um registro por arquivo + classe) · métodos: 6876

## tab:per_class_averages

| Nível | N | WMC | LCOM | MC/classe | AC/classe | CWC/classe |
|---|---:|---:|---:|---:|---:|---:|
| Beginner | 1 | 1,86 (1,86) | 0,29 (0,29) | 9,86 (9,86) | 0,86 (0,86) | 1,95 (1,95) |
| Mid-Level | 13 | 2,25 (2,35) | 1,42 (0,58) | 38,72 (23,04) | 2,00 (2,02) | 30,23 (12,43) |
| Senior | 24 | 2,18 (1,99) | 1,51 (0,66) | 36,52 (31,63) | 1,78 (1,77) | 22,39 (18,94) |
| Expert | 10 | 1,67 (1,62) | 0,56 (0,52) | 27,61 (11,72) | 1,59 (1,55) | 17,48 (6,03) |

## tab:codebase_size

| Nível | N | LOC média (mediana) | Classes média (mediana) |
|---|---:|---:|---:|
| Beginner | 1 | 254 (254) | 21,0 (21,0) |
| Mid-Level | 13 | 991 (914) | 53,9 (48,0) |
| Senior | 24 | 1161 (937) | 69,0 (57,0) |
| Expert | 10 | 1419 (1255) | 99,5 (84,5) |

## metric_redundancy

Pearson entre participantes:

- clc_total × loc: r = 0,843
- cwc_total × loc: r = 0,811
- coc × loc: r = 0,836
- cyclomatic × loc: r = 0,970
- clc_total × classes: r = 0,746
- cwc_total × classes: r = 0,713
- coc × classes: r = 0,739
- cyclomatic × classes: r = 0,919
- cwc_total × cyclomatic: r = 0,872
- clc_total × cyclomatic: r = 0,905
- cwc_total × coc: r = 0,995
- clc_total × coc: r = 0,999
- mc_total × clc_total: r = 1,000
- wmc_avg × lcom_avg: r = 0,769
- cbo_avg × loc: r = 0,386
- cbo_avg × classes: r = 0,267
- cbo_avg × classes_raw: r = 0,267
- dit_avg × noc_avg: r = 0,524
- DIT × NOC por classe (n = 3373): r = 0,005

- Classes com DIT = 1: 20,1%
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
| DIT | 3373 | 0,00 | 1,00 |
| NOC | 3373 | 0,00 | 0,00 |
| LCOM | 3373 | 0,00 | 1,00 |
| AC | 3373 | 1,00 | 5,00 |
| CWC | 3373 | 0,00 | 41,80 |
| MC | 6876 | 3,00 | 43,00 |

## tab:confusion_matrix

| Conhecido \ Previsto | Beginner | Mid-Level | Senior | Expert |
|---|---:|---:|---:|---:|
| Beginner (n=1) | 0 | 0 | 0 | 1 |
| Mid-Level (n=13) | 0 | 2 | 9 | 2 |
| Senior (n=24) | 0 | 2 | 17 | 5 |
| Expert (n=10) | 0 | 0 | 7 | 3 |

- Acurácia (sem Beginner): 22/47 = 46,8%
- Linha de base (sempre Senior): 24/47 = 51,1%
- ρ de Spearman (regra em estágios × nível conhecido, n = 48): 0,111
- ρ de Spearman (média simples do % Normal × nível conhecido): 0,360
- Erros: 26 (23 adjacentes, 3 a dois ou mais níveis)

## tab:per_participant_results

| ID | Nível | WMC | DIT | NOC | LCOM | AC | CWC | MC | Previsto | Corr. | Discrepância |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| p1 | Mid-Level | 41,0 | 84,6 | 100,0 | 94,9 | 48,7 | 66,7 | 64,2 | Senior | Não | Adjacente |
| p2 | Expert | 59,3 | 88,9 | 100,0 | 96,3 | 51,9 | 77,8 | 82,9 | Senior | Não | Adjacente |
| p3 | Expert | 47,4 | 52,6 | 100,0 | 84,2 | 78,9 | 84,2 | 60,0 | Senior | Não | Adjacente |
| p4 | Expert | 57,1 | 81,6 | 95,9 | 89,8 | 69,4 | 68,4 | 52,2 | Senior | Não | Adjacente |
| p5 | Mid-Level | 33,3 | 81,0 | 95,2 | 85,7 | 47,6 | 61,9 | 64,6 | Senior | Não | Adjacente |
| p6 | Mid-Level | 54,5 | 82,7 | 100,0 | 87,3 | 48,2 | 77,3 | 64,7 | Senior | Não | Adjacente |
| p7 | Senior | 53,9 | 78,7 | 98,9 | 93,3 | 40,4 | 79,8 | 49,1 | Senior | Sim | - |
| p8 | Senior | 54,2 | 92,8 | 100,0 | 91,6 | 53,0 | 73,5 | 50,4 | Senior | Sim | - |
| p9 | Senior | 74,4 | 85,6 | 100,0 | 94,4 | 43,3 | 74,4 | 50,5 | Senior | Sim | - |
| p10 | Senior | 60,5 | 76,7 | 100,0 | 86,0 | 55,8 | 69,8 | 42,3 | Senior | Sim | - |
| p11 | Mid-Level | 56,4 | 70,9 | 90,9 | 90,9 | 47,3 | 67,3 | 46,6 | Senior | Não | Adjacente |
| p12 | Mid-Level | 46,7 | 84,4 | 97,8 | 75,6 | 42,2 | 64,4 | 43,9 | Mid-Level | Sim | - |
| p13 | Senior | 42,9 | 81,0 | 95,2 | 100,0 | 71,4 | 76,2 | 81,6 | Expert | Não | Adjacente |
| p14 | Senior | 63,2 | 68,4 | 94,7 | 94,7 | 57,9 | 89,5 | 80,0 | Expert | Não | Adjacente |
| p15 | Expert | 75,5 | 75,5 | 95,7 | 97,9 | 45,7 | 70,2 | 49,0 | Senior | Não | Adjacente |
| p16 | Expert | 55,7 | 74,2 | 94,8 | 84,5 | 70,1 | 75,3 | 68,4 | Expert | Sim | - |
| p17 | Expert | 62,7 | 72,0 | 100,0 | 94,7 | 69,3 | 80,0 | 63,3 | Expert | Sim | - |
| p18 | Beginner | 61,9 | 52,4 | 95,2 | 90,5 | 76,2 | 85,7 | 66,7 | Expert | Não | Discrepante (+3) |
| p19 | Mid-Level | 62,9 | 62,9 | 96,8 | 88,7 | 54,8 | 69,4 | 52,2 | Senior | Não | Adjacente |
| p20 | Mid-Level | 48,1 | 70,4 | 94,4 | 90,7 | 50,0 | 77,8 | 64,1 | Senior | Não | Adjacente |
| p21 | Senior | 57,1 | 64,3 | 100,0 | 100,0 | 57,1 | 71,4 | 58,8 | Senior | Sim | - |
| p22 | Mid-Level | 42,3 | 61,5 | 96,2 | 80,8 | 26,9 | 69,2 | 59,0 | Senior | Não | Adjacente |
| p23 | Mid-Level | 39,6 | 70,8 | 97,9 | 81,2 | 70,8 | 68,8 | 66,7 | Expert | Não | Discrepante (+2) |
| p24 | Senior | 54,2 | 79,2 | 99,2 | 81,7 | 55,8 | 65,8 | 44,9 | Senior | Sim | - |
| p25 | Senior | 58,7 | 77,2 | 96,7 | 89,1 | 75,0 | 78,3 | 63,2 | Expert | Não | Adjacente |
| p26 | Senior | 44,4 | 63,0 | 92,6 | 77,8 | 64,8 | 77,8 | 59,3 | Senior | Sim | - |
| p27 | Mid-Level | 60,4 | 75,0 | 93,8 | 85,4 | 41,7 | 81,2 | 68,6 | Expert | Não | Discrepante (+2) |
| p28 | Mid-Level | 37,7 | 72,1 | 96,7 | 83,6 | 62,3 | 59,0 | 47,8 | Senior | Não | Adjacente |
| p29 | Senior | 58,5 | 77,1 | 94,1 | 85,1 | 71,8 | 70,2 | 48,8 | Senior | Sim | - |
| p30 | Senior | 51,6 | 78,1 | 96,9 | 90,6 | 64,1 | 68,8 | 55,7 | Senior | Sim | - |
| p31 | Senior | 35,0 | 76,7 | 100,0 | 80,0 | 45,0 | 56,7 | 33,8 | Mid-Level | Não | Adjacente |
| p32 | Senior | 45,9 | 83,8 | 100,0 | 73,0 | 45,9 | 78,4 | 54,9 | Senior | Sim | - |
| p33 | Senior | 58,5 | 77,1 | 94,1 | 85,1 | 71,8 | 70,2 | 48,8 | Senior | Sim | - |
| p34 | Senior | 68,9 | 75,6 | 97,8 | 88,9 | 55,6 | 80,0 | 56,2 | Senior | Sim | - |
| p35 | Senior | 17,1 | 82,9 | 100,0 | 94,3 | 45,7 | 37,1 | 46,6 | Mid-Level | Não | Adjacente |
| p36 | Senior | 35,9 | 74,4 | 100,0 | 89,7 | 51,3 | 71,8 | 55,4 | Senior | Sim | - |
| p37 | Senior | 59,3 | 84,9 | 100,0 | 87,2 | 70,9 | 75,6 | 56,2 | Senior | Sim | - |
| p38 | Senior | 44,1 | 75,4 | 97,5 | 93,2 | 50,0 | 61,0 | 43,2 | Senior | Sim | - |
| p39 | Mid-Level | 53,8 | 86,5 | 100,0 | 87,5 | 64,4 | 79,8 | 55,9 | Senior | Não | Adjacente |
| p40 | Expert | 68,6 | 68,6 | 94,1 | 90,2 | 62,7 | 78,4 | 58,7 | Expert | Sim | - |
| p41 | Senior | 40,9 | 61,4 | 93,2 | 72,7 | 61,4 | 65,9 | 52,3 | Senior | Sim | - |
| p42 | Senior | 70,0 | 73,3 | 100,0 | 83,3 | 70,0 | 73,3 | 50,9 | Expert | Não | Adjacente |
| p43 | Expert | 57,4 | 87,2 | 97,9 | 93,6 | 68,1 | 74,5 | 58,4 | Senior | Não | Adjacente |
| p44 | Senior | 36,7 | 73,3 | 100,0 | 90,0 | 60,0 | 70,0 | 66,7 | Expert | Não | Adjacente |
| p45 | Expert | 54,9 | 79,6 | 95,6 | 77,0 | 59,3 | 64,6 | 34,3 | Senior | Não | Adjacente |
| p46 | Expert | 51,6 | 95,3 | 100,0 | 83,4 | 58,1 | 66,1 | 51,7 | Senior | Não | Adjacente |
| p47 | Mid-Level | 35,7 | 85,7 | 100,0 | 89,3 | 25,0 | 60,7 | 47,8 | Mid-Level | Sim | - |
| p48 | Senior | 43,3 | 88,1 | 100,0 | 94,0 | 59,7 | 71,6 | 50,3 | Senior | Sim | - |
