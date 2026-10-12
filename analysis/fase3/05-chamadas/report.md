# Análise CBTAM — 05-chamadas

Participantes: 48 · classes: 3004 (de 3004 linhas, um registro por arquivo + classe) · métodos: 6831

## tab:per_class_averages

| Nível | N | WMC | LCOM | MC/classe | AC/classe | CWC/classe |
|---|---:|---:|---:|---:|---:|---:|
| Beginner | 1 | 1,86 (1,86) | 0,29 (0,29) | 22,48 (22,48) | 1,29 (1,29) | 27,14 (27,14) |
| Mid-Level | 13 | 2,25 (2,19) | 1,62 (0,73) | 40,01 (38,21) | 3,03 (2,95) | 60,88 (61,36) |
| Senior | 24 | 2,19 (1,85) | 1,89 (0,77) | 41,60 (39,02) | 2,46 (2,63) | 63,05 (56,26) |
| Expert | 10 | 1,44 (1,44) | 0,84 (0,77) | 30,11 (33,61) | 2,16 (2,14) | 41,66 (46,71) |

## tab:codebase_size

| Nível | N | LOC média (mediana) | Classes média (mediana) |
|---|---:|---:|---:|
| Beginner | 1 | 405 (405) | 21,0 (21,0) |
| Mid-Level | 13 | 1559 (1233) | 49,2 (48,0) |
| Senior | 24 | 1905 (1477) | 60,5 (52,0) |
| Expert | 10 | 2670 (2275) | 89,0 (79,5) |

## metric_redundancy

Pearson entre participantes:

- clc_total × loc: r = 0,933
- cwc_total × loc: r = 0,871
- coc × loc: r = 0,914
- cyclomatic × loc: r = 0,962
- clc_total × classes: r = 0,871
- cwc_total × classes: r = 0,795
- coc × classes: r = 0,849
- cyclomatic × classes: r = 0,916
- cwc_total × cyclomatic: r = 0,850
- clc_total × cyclomatic: r = 0,912
- cwc_total × coc: r = 0,986
- clc_total × coc: r = 0,996
- mc_total × clc_total: r = 0,999
- wmc_avg × lcom_avg: r = 0,728
- cbo_avg × loc: r = -0,112
- cbo_avg × classes: r = -0,155
- cbo_avg × classes_raw: r = -0,155
- dit_avg × noc_avg: r = 0,989
- DIT × NOC por classe (n = 3004): r = 0,018

- Classes com DIT = 1: 94,6%
- Classes com NOC = 0: 97,3%
- Amplitude da LCOM média por participante: 0,00 – 14,58

## position_analysis

| Cargo | N | LOC média | WMC média | LCOM média |
|---|---:|---:|---:|---:|
| Architect | 27 | 2239 | 2,18 | 1,90 |
| Developer | 21 | 1553 | 1,86 | 1,14 |

## empirical_application

Limiares absolutos (% Normal sobre todas as classes): WMC 99,8%, DIT 100,0%, NOC 100,0%

LCOM = WMC(WMC−1)/2 em 71,87% das classes.

Limiares relativos (amostra combinada):

| Métrica | População | P50 | P90 |
|---|---:|---:|---:|
| WMC | 3004 | 1,00 | 5,00 |
| DIT | 3004 | 1,00 | 1,00 |
| NOC | 3004 | 0,00 | 0,00 |
| LCOM | 3004 | 0,00 | 3,00 |
| AC | 3004 | 2,00 | 6,00 |
| CWC | 3004 | 18,00 | 140,40 |
| MC | 6831 | 10,00 | 38,00 |

## tab:confusion_matrix

| Conhecido \ Previsto | Beginner | Mid-Level | Senior | Expert |
|---|---:|---:|---:|---:|
| Beginner (n=1) | 0 | 0 | 0 | 1 |
| Mid-Level (n=13) | 0 | 2 | 10 | 1 |
| Senior (n=24) | 0 | 4 | 16 | 4 |
| Expert (n=10) | 0 | 0 | 6 | 4 |

- Acurácia (sem Beginner): 22/47 = 46,8%
- Linha de base (sempre Senior): 24/47 = 51,1%
- ρ de Spearman (regra em estágios × nível conhecido, n = 48): 0,198
- ρ de Spearman (média simples do % Normal × nível conhecido): 0,333
- Erros: 26 (24 adjacentes, 2 a dois ou mais níveis)

## tab:per_participant_results

| ID | Nível | WMC | DIT | NOC | LCOM | AC | CWC | MC | Previsto | Corr. | Discrepância |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| p1 | Mid-Level | 14,8 | 100,0 | 100,0 | 85,2 | 63,0 | 29,6 | 50,5 | Senior | Não | Adjacente |
| p2 | Expert | 63,0 | 100,0 | 100,0 | 96,3 | 51,9 | 88,9 | 82,9 | Expert | Sim | - |
| p3 | Expert | 47,4 | 100,0 | 100,0 | 73,7 | 84,2 | 36,8 | 62,5 | Senior | Não | Adjacente |
| p4 | Expert | 69,0 | 90,5 | 95,2 | 86,9 | 76,2 | 51,2 | 48,3 | Senior | Não | Adjacente |
| p5 | Mid-Level | 38,1 | 95,2 | 95,2 | 81,0 | 47,6 | 57,1 | 63,3 | Senior | Não | Adjacente |
| p6 | Mid-Level | 52,4 | 100,0 | 100,0 | 82,5 | 51,5 | 54,4 | 45,3 | Senior | Não | Adjacente |
| p7 | Senior | 47,8 | 100,0 | 100,0 | 92,5 | 38,8 | 53,7 | 54,1 | Senior | Sim | - |
| p8 | Senior | 59,0 | 100,0 | 100,0 | 89,2 | 54,2 | 51,8 | 48,7 | Senior | Sim | - |
| p9 | Senior | 78,7 | 100,0 | 100,0 | 93,3 | 43,8 | 52,8 | 36,1 | Senior | Sim | - |
| p10 | Senior | 56,4 | 100,0 | 100,0 | 84,6 | 69,2 | 38,5 | 29,9 | Senior | Sim | - |
| p11 | Mid-Level | 60,0 | 90,9 | 90,9 | 90,9 | 54,5 | 49,1 | 42,9 | Senior | Não | Adjacente |
| p12 | Mid-Level | 47,7 | 97,7 | 97,7 | 75,0 | 45,5 | 45,5 | 52,9 | Mid-Level | Sim | - |
| p13 | Senior | 47,6 | 95,2 | 95,2 | 90,5 | 76,2 | 90,5 | 89,5 | Expert | Não | Adjacente |
| p14 | Senior | 73,7 | 94,7 | 94,7 | 89,5 | 63,2 | 63,2 | 68,0 | Expert | Não | Adjacente |
| p15 | Expert | 77,7 | 93,6 | 95,7 | 91,5 | 71,3 | 63,8 | 41,4 | Expert | Sim | - |
| p16 | Expert | 58,0 | 86,2 | 94,5 | 77,3 | 68,5 | 65,2 | 64,7 | Expert | Sim | - |
| p17 | Expert | 66,7 | 100,0 | 100,0 | 96,0 | 72,0 | 68,0 | 57,5 | Expert | Sim | - |
| p18 | Beginner | 61,9 | 90,5 | 95,2 | 90,5 | 85,7 | 57,1 | 71,8 | Expert | Não | Discrepante (+3) |
| p19 | Mid-Level | 60,4 | 84,9 | 96,2 | 79,2 | 75,5 | 45,3 | 43,3 | Senior | Não | Adjacente |
| p20 | Mid-Level | 49,0 | 88,2 | 94,1 | 82,4 | 49,0 | 56,9 | 71,5 | Senior | Não | Adjacente |
| p21 | Senior | 57,1 | 100,0 | 100,0 | 100,0 | 57,1 | 50,0 | 58,8 | Senior | Sim | - |
| p22 | Mid-Level | 34,8 | 91,3 | 95,7 | 78,3 | 39,1 | 30,4 | 48,3 | Mid-Level | Sim | - |
| p23 | Mid-Level | 52,1 | 95,8 | 97,9 | 83,3 | 70,8 | 68,8 | 74,2 | Expert | Não | Discrepante (+2) |
| p24 | Senior | 51,0 | 96,0 | 99,0 | 76,0 | 58,0 | 55,0 | 60,9 | Senior | Sim | - |
| p25 | Senior | 65,2 | 93,5 | 96,7 | 87,0 | 77,2 | 65,2 | 71,9 | Expert | Não | Adjacente |
| p26 | Senior | 55,6 | 87,0 | 92,6 | 74,1 | 64,8 | 63,0 | 70,8 | Expert | Não | Adjacente |
| p27 | Mid-Level | 52,8 | 83,3 | 94,4 | 80,6 | 44,4 | 61,1 | 78,2 | Senior | Não | Adjacente |
| p28 | Mid-Level | 39,7 | 86,2 | 96,6 | 82,8 | 63,8 | 48,3 | 53,8 | Senior | Não | Adjacente |
| p29 | Senior | 65,0 | 90,2 | 94,5 | 76,1 | 82,2 | 45,4 | 49,9 | Senior | Sim | - |
| p30 | Senior | 64,2 | 92,5 | 96,2 | 79,2 | 75,5 | 37,7 | 36,1 | Senior | Sim | - |
| p31 | Senior | 25,5 | 100,0 | 100,0 | 76,5 | 51,0 | 19,6 | 47,9 | Mid-Level | Não | Adjacente |
| p32 | Senior | 16,7 | 100,0 | 100,0 | 62,5 | 70,8 | 33,3 | 50,7 | Senior | Sim | - |
| p33 | Senior | 65,0 | 90,2 | 94,5 | 76,1 | 82,2 | 45,4 | 49,9 | Senior | Sim | - |
| p34 | Senior | 71,1 | 94,7 | 97,4 | 84,2 | 76,3 | 34,2 | 14,4 | Senior | Sim | - |
| p35 | Senior | 14,7 | 100,0 | 100,0 | 88,2 | 47,1 | 26,5 | 46,6 | Mid-Level | Não | Adjacente |
| p36 | Senior | 22,6 | 100,0 | 100,0 | 71,0 | 58,1 | 22,6 | 46,5 | Mid-Level | Não | Adjacente |
| p37 | Senior | 68,4 | 100,0 | 100,0 | 78,9 | 81,6 | 44,7 | 43,1 | Senior | Sim | - |
| p38 | Senior | 28,1 | 93,3 | 96,6 | 83,1 | 52,8 | 24,7 | 40,6 | Mid-Level | Não | Adjacente |
| p39 | Mid-Level | 61,3 | 100,0 | 100,0 | 82,8 | 61,3 | 50,5 | 50,7 | Senior | Não | Adjacente |
| p40 | Expert | 80,5 | 87,8 | 97,6 | 87,8 | 65,9 | 31,7 | 42,4 | Senior | Não | Adjacente |
| p41 | Senior | 40,0 | 87,5 | 92,5 | 55,0 | 75,0 | 40,0 | 54,9 | Senior | Sim | - |
| p42 | Senior | 66,7 | 100,0 | 100,0 | 70,8 | 83,3 | 37,5 | 45,5 | Senior | Sim | - |
| p43 | Expert | 51,3 | 97,4 | 97,4 | 84,6 | 71,8 | 43,6 | 51,4 | Senior | Não | Adjacente |
| p44 | Senior | 36,7 | 100,0 | 100,0 | 83,3 | 60,0 | 56,7 | 69,8 | Senior | Sim | - |
| p45 | Expert | 58,0 | 89,8 | 95,5 | 63,6 | 73,9 | 51,1 | 58,6 | Senior | Não | Adjacente |
| p46 | Expert | 64,9 | 100,0 | 100,0 | 74,4 | 64,0 | 46,7 | 46,4 | Senior | Não | Adjacente |
| p47 | Mid-Level | 39,3 | 100,0 | 100,0 | 78,6 | 25,0 | 53,6 | 55,1 | Senior | Não | Adjacente |
| p48 | Senior | 47,5 | 100,0 | 100,0 | 86,4 | 62,7 | 57,6 | 56,1 | Senior | Sim | - |
