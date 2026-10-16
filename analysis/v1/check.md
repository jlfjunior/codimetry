# Conferência contra published_v1.json

## Divergências não explicadas

Nenhuma.

## Erratas conhecidas da versão publicada

- per_participant.p6.predicted: calculado Senior, publicado Mid-Level — a regra em estágios aplicada aos próprios valores publicados da linha dá Senior; a matriz de confusão publicada só fecha com Senior
- per_participant.p6.correct: calculado False, publicado True — consequência da errata de p6.predicted
- per_participant.p6.discrepancy: calculado Adjacente, publicado - — consequência da errata de p6.predicted
- per_participant.p10.predicted: calculado Mid-Level, publicado Senior — a regra aplicada aos valores publicados dá Mid-Level; a própria linha publicada marca 'Não / Adjacente'
- per_participant.p13.NOC: calculado 95.2, publicado 97.6 — divergência de uma classe; não altera o nível previsto
- per_participant.p18.CWC: calculado 57.1, publicado 73.1 — 73,1% não é k/21 para nenhum k (p18 tem 21 classes); o valor calculado é 57,1% (12/21)
- per_participant.p19.LCOM: calculado 68.9, publicado 60.0 — o valor publicado repete o WMC da linha (60,0); p19 é o único participante em que %Normal de LCOM difere do de WMC
- per_participant.p19.AC: calculado 73.3, publicado 68.9 — divergência de uma ou duas classes; não altera o nível previsto
- per_participant.p22.NOC: calculado 95.2, publicado 90.5 — divergência de uma classe; não altera o nível previsto
- per_participant.p28.AC: calculado 63.2, publicado 63.9 — 63,9% não é k/57 para nenhum k (p28 tem 57 classes); o valor calculado é 63,2% (36/57)
- per_participant.p40.AC: calculado 65.0, publicado 47.5 — 47,5 é o valor do CWC da mesma linha; com o valor calculado (65,0) a regra dá o Senior publicado
- per_participant.p41.predicted: calculado Mid-Level, publicado Senior — a regra aplicada aos valores publicados dá Mid-Level; a matriz de confusão publicada só fecha com Mid-Level
- per_participant.p41.correct: calculado False, publicado True — consequência da errata de p41.predicted
- per_participant.p41.discrepancy: calculado Adjacente, publicado - — consequência da errata de p41.predicted
- per_participant.p45.predicted: calculado Senior, publicado Mid-Level — a regra aplicada aos valores publicados dá Senior; a matriz de confusão publicada só fecha com Senior
- per_participant.p47.MC: calculado 56.5, publicado 55.3 — divergência de um método; não altera o nível previsto
