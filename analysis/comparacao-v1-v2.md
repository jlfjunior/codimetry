# Comparação v1 × v2 (Fase 6)

> Gerado em 15/10/2026. v1 = `results-v1/` (`0bc0f89`, a coleta anterior às correções); v2 = `results-v2/` (`0ff7d31`, opções padrão; mesmo código da tag `v1` da ferramenta). A numeração v1/v2 das coletas é controle interno: a dissertação cita só a tag `v1`. Artefatos `artefatos-mestrado@40a6044`, .NET SDK 10.0.101, nos dois casos.
> Escopo igual ao da v1 (decisões da seção 4 do protocolo): 48 participantes com p29 e p33, código de teste e código gerado incluídos.
> Waterfall completo, passo a passo: [fase4/comparacao.md](fase4/comparacao.md); Fase 3: [fase3/comparacao.md](fase3/comparacao.md).

## 1. Números do Cap. 4

| Número | v1 | v2 | Observação |
|---|---:|---:|---|
| Classes | 2.657 | 3.373 | +347 homônimas separadas (Fase 3, passo 01); +369 records e structs (INC-13) |
| Métodos (MC) | 6.839 | 6.876 | |
| Acurácia (sem Beginner) | 51,1% | 46,8% | linha de base (sempre Senior) 51,1% nas duas |
| ρ, regra em estágios | 0,286 | 0,111 | |
| ρ, média do % Normal | 0,363 | 0,360 | |
| Erros (adjacentes / ≥ 2 níveis) | 24 (20 / 4) | 26 (23 / 3) | |
| Classes com DIT = 1 | 94,2% | 20,1% | v2: DIT = 0 em 78,9% (`System.Object` não conta, INC-12) |
| Classes com NOC = 0 | 97,3% | 97,4% | |
| P50 / P90 MC | 9 / 35 | 3 / 43 | composição da Eq. 23; chamada ao código = 2 + MC |
| P50 / P90 CWC | 21 / 159 | 0 / 41,8 | só chamadas a outros tipos do código (INC-19) |
| r CLC total × LOC | 0,932 | 0,843 | |
| r CoC × LOC | 0,807 | 0,539 | CoC segue a herança (INC-20) |
| r CBO médio × classes | −0,110 | 0,267 | CBO com todas as referências (INC-11) |

Sem p33 (duplicata de p29): acurácia 45,7% contra linha de base de 50,0%; ρ = 0,112 e 0,357.

## 2. Redundância LCOM ≡ WMC

O indicador "LCOM = n(n−1)/2" do relatório (82,42% na v2) é inflado por classes com 0 ou 1 método, em que LCOM = 0 = n(n−1)/2 trivialmente. Restrito às classes com pelo menos 2 métodos na LCOM:

| | v1 | v2 |
|---|---:|---:|
| Classes com n ≥ 2 | 1.278 | 936 |
| LCOM = n(n−1)/2 | 98,7% | 36,6% |
| r(n(n−1)/2, LCOM) | 0,999 | 0,662 |
| r(WMC, LCOM), todas as classes | 0,787 | 0,495 |

A queda vem de INC-07 a INC-09 (passo 02) e já tinha começado na Fase 3 com a correção de `054fc08`.

## 3. Médias por nível (`tab:per_class_averages`)

| Nível | WMC v1 → v2 | LCOM v1 → v2 | CWC/classe v1 → v2 |
|---|---|---|---|
| Mid-Level | 2,50 → 2,25 | 5,98 → 1,42 | 62,00 → 30,23 |
| Senior | 2,44 → 2,18 | 7,12 → 1,51 | 67,83 → 22,39 |
| Expert | 1,58 → 1,67 | 2,15 → 0,56 | 44,39 → 17,48 |

## 4. Veredito das conclusões da dissertação

| Conclusão | Veredito | Por quê |
|---|---|---|
| LCOM é redundante com WMC | **Revertida** | Era efeito do defeito: 98,7% → 36,6% das classes com LCOM máxima; r 0,999 → 0,662 |
| Expert tem WMC, LCOM e CWC/classe menores que Mid-Level e Senior | **Mantida** | Expert continua o menor nas três. Mid-Level e Senior trocam de ordem na CWC/classe (Mid > Senior na v2) |
| A regra em estágios acerta 51,1%, igual à linha de base | **Enfraquecida** | 46,8%, abaixo da linha de base de 51,1% |
| ρ = 0,286 entre previsão e nível | **Enfraquecida** | 0,111 com a regra em estágios; a média do % Normal fica estável (0,360) |
| DIT e NOC quase não variam | **Mantida**, com outro número | DIT concentra-se em 0 (78,9%), não em 1; NOC = 0 em 97,4% |
| CLC e CoC totais são dominados pelo tamanho | **Mantida para CLC, enfraquecida para CoC** | r com LOC: CLC 0,843; CoC 0,539, porque a CoC agora multiplica pela herança |
| CBO não se correlaciona com o tamanho | **Enfraquecida** | r com nº de classes passa de −0,11 a 0,27 |

## 5. Ressalvas

- O passo `40c2ac4` (composição da MC, INC-14) não compila sozinho: usa `MethodComplexityCalculator`, que só entra em `45d7e9f` (AC, INC-18). Por isso o waterfall tem um passo só para os dois (`04-mc-composicao-ac`) e não separa seus efeitos.
- A partir do passo 05, o CSV exporta `LcomMethods`, e o indicador LCOM = n(n−1)/2 do relatório passa a usá-lo no lugar do WMC; a mudança de 66% para 80% nesse passo é do indicador, não da LCOM.
- Validação G3 (LCOM e CBO à mão em 5 classes reais) e revisão com o orientador ainda pendentes; a tag `v1` existe só localmente e só deve ser publicada depois delas.
