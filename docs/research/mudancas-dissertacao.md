# Mudanças na dissertação após a recoleta (v2)

> Elaborado em 16/10/2026, a partir da coleta v2 (`results-v2/`, ferramenta em `0ff7d31`) e da comparação em [`analysis/comparacao-v1-v2.md`](../../analysis/comparacao-v1-v2.md).
> Repositório da dissertação: `master-thesis` (branch `main`, último commit `601391b`). As linhas citadas são desse commit.
> Escopo da v2 igual ao da v1 (seção 4 do [protocolo](protocolo-recoleta.md)): 48 participantes, p29 e p33 incluídos, código de teste e código gerado incluídos.

## 0. Antes de editar

1. **Validação G3**: calcular à mão LCOM1 e CBO em 5 classes reais de participantes diferentes e conferir com `results-v2/`.
2. **Revisão com o orientador**, principalmente do item 4.3: com a v2, a regra em estágios fica **abaixo** da linha de base, e o Resumo, o Cap. 4 e a Conclusão dizem hoje que ela a iguala.
3. Tag `coleta-v2` na ferramenta (no commit usado para `results-v2/`) e citar a tag no Cap. 4.
4. Fazer todas as mudanças abaixo **num único commit** de `master-thesis`, para que o texto nunca misture v1 e v2.

Fonte de todos os números novos: [`analysis/fase4/14-coc-v2/report.md`](../../analysis/fase4/14-coc-v2/report.md) (tabelas no formato do Cap. 4) e [`analysis/comparacao-v1-v2.md`](../../analysis/comparacao-v1-v2.md).

## 1. Resumo das mudanças de resultado

| Número | v1 (texto atual) | v2 |
|---|---:|---:|
| Classes / métodos | 2.657 / 6.839 | 3.373 / 6.876 |
| Acurácia da regra em estágios | 51,1% (24/47) | 46,8% (22/47) |
| Linha de base (sempre *Senior*) | 51,1% | 51,1% |
| ρ, regra em estágios | 0,286 | 0,111 |
| ρ, média do % Normal | 0,363 | 0,360 |
| Erros (adjacentes / ≥ 2 níveis) | 24 (20 / 4) | 26 (23 / 3) |
| Classes com LCOM máxima, entre as com 2 ou mais métodos | 98,7% (texto: 99,40% de todas) | 36,6% |
| Classes com DIT = 1 | 94,2% | 20,1% (DIT = 0 em 78,9%) |
| Classes com NOC = 0 | 97,3% | 97,4% |
| r WMC × LCOM (média por participante) | 0,76 | 0,77 |
| r CBO × LOC / × classes | −0,10 / −0,13 | 0,39 / 0,27 |
| r CoC × LOC | 0,81 | 0,54 |

| Conclusão | Veredito |
|---|---|
| LCOM redundante com WMC | **Revertida** (era efeito do defeito) |
| *Expert* com menor WMC, LCOM, MC e CWC por classe | **Mantida** |
| Acurácia igual à linha de base | **Enfraquecida**: fica abaixo |
| ρ = 0,286 | **Enfraquecida** na regra em estágios; estável na média do % Normal |
| DIT e NOC quase não variam | **Mantida**, com outros números |
| Totais de Misra dominados pelo tamanho | **Mantida para CLC e CWC; enfraquecida para CoC** |
| CBO não se correlaciona com o tamanho | **Enfraquecida** |

---

## 2. `Pre_Textual/Resumo.tex` e `Pre_Textual/Abstract.tex`

| Linha | Hoje | Mudança |
|---|---|---|
| Resumo 28–29; Abstract 26–27 | "uma regra de classificação em estágios atinge 51,1% de acurácia (24 de 47 participantes…), igual à de uma linha de base ingênua" | "atinge 46,8% de acurácia (22 de 47…), abaixo da linha de base ingênua de sempre prever o nível mais frequente (51,1%)" |
| Resumo 26–27; Abstract 24–25 | "participantes *Expert* exibiram consistentemente menor complexidade estrutural e cognitiva por classe" | Mantém (confirmado na v2) |
| Resumo 23; Abstract 21 | "conjunto compacto e não redundante de sete métricas" | Mantém. Com a v2, a LCOM deixa de ser redundante com a WMC, o que **reforça** a afirmação |

Se o Resumo citar a LCOM como achado metodológico, trocar por: verificar a formulação implementada pela ferramenta mudou a conclusão sobre a LCOM.

## 3. Cap. 1 — `Conteudo/01_Introducao.tex`

| Linha | Hoje | Mudança |
|---|---|---|
| 74 | Achado metodológico: "é necessário verificar a formulação implementada pelas ferramentas de extração, como evidenciado pelo caso da métrica LCOM" | Mantém o achado e muda a evidência: a redundância LCOM ≡ WMC da coleta original vinha de um defeito de implementação e desaparece na versão corrigida (98,7% → 36,6% das classes com LCOM máxima) |

## 4. Cap. 2 — `Conteudo/02_Referencial_Teorico.tex`

O capítulo deixa de descrever as simplificações da v1 e passa a declarar só as adaptações da ferramenta corrigida.

| Linha | Seção | Mudança |
|---|---|---|
| 120 | V(G), pontos de decisão | Declarar duas extensões: os combinadores de padrão `or`/`and` (C# 9+) contam como `\|\|`/`&&` (D17), e `&&`/`\|\|` contam em qualquer expressão, não só "dentro de uma expressão condicional". `default`, o padrão curinga `_` e `goto` não contam; `??`, `??=` e `?.` também não (D16) |
| 184–191 | WMC | A remissão à Seção `case_study_protocol` continua; ali passa a constar $c_i = 1$, com métodos estáticos e construtores (D2, D3) |
| 221 | RFC | Declarar que construtores chamados (`new`, `: base(...)`) entram em $R$, por serem métodos executados em resposta à mensagem (D20), e que acessores de propriedade não entram |
| 256 | LCOM | Depois de "adota-se LCOM1", acrescentar que entram os métodos de instância **sem construtores** (D4), com a justificativa: construtores inicializam quase todos os atributos e reduziriam a LCOM sem relação com a coesão. Citar a referência ainda pendente (§2 do spec INC-09) |
| 293 | CBO | Declarar a direção (só saída, D9) e que interfaces do sistema contam como acoplamento (D8: em .NET, a injeção de dependência acopla por interface). Tipos do framework e de pacotes não contam |
| **318** | DIT, nota de rodapé | **Remover** a nota "os valores de DIT relatados naquele capítulo são uma unidade maiores": a v2 segue a convenção do texto. Opcional: uma frase dizendo que uma classe base externa (`Exception`, `ControllerBase`) conta 1 nível, sem seguir seus ancestrais |
| 388–448 | MC | Declarar as decisões de aplicação a C#: (i) só chamadas a métodos do próprio sistema somam 2 + MC do método chamado; chamadas de biblioteca não somam (D10); (ii) lambdas e funções locais são uma camada aninhada onde estão escritas (D11); (iii) `else if` é um `if` aninhado no `else` (D12); (iv) as instruções simples de uma camada formam **uma** sequência (D22), a única regra que reproduz MC = 12 em `check_supported_sw`; (v) o ternário equivale a if-then-else (D23); (vi) `switch` e `switch` por expressão têm peso 3 (*case*). Registrar também que a CC de Campbell (linhas 364–368) é contexto e não é a métrica coletada |
| 501 | CWC | Trocar "métodos externos" / "outra classe" por "métodos de **outras classes do sistema**" (Eq. 24): chamadas a bibliotecas e delegados não contam (D13); via interface ou método abstrato, conta o MC da implementação única ou 1 (D14) |
| 532–537 | CoC | Registrar a **contradição** entre a Eq. 27 e o exemplo de Misra et al. (2018): a Eq. 27 lida nível a nível daria 4 × (14 + 20) × (3 + 3) = 816, mas o artigo obtém 4 × (14 + 20 × (3 + 3)) = 536, multiplicando cada classe só pelas próprias subclasses. Declarar a fórmula adotada (por subárvore, D1) e que hierarquias distintas e classes isoladas se somam. O exemplo `Usuario`/`Cliente` (= 4) vale nas duas leituras |
| **569** | Parágrafo de simplificações da ferramenta | **Reescrever.** Hoje lista as simplificações da v1 (pesos somados, cada instrução contada, AC ponderado por tipo, CoC = ΣCLC + ΣCWC/2). Passa a dizer que a ferramenta implementa as definições desta seção, com as adaptações acima, e remete à Tabela `tab:formulas_coleta` |

## 5. Cap. 3 — `Conteudo/04_Outros.tex` (CBTAM)

| Linha | Mudança |
|---|---|
| 82, 212 | Nenhuma: as sete métricas continuam. Se a seleção (`metric_selection`) usar a redundância LCOM ≡ WMC como argumento, retirá-lo |
| 194 | Limiar de LCOM "≤ 0,5; dependente de variante": conferir se a nota ainda se aplica, porque a LCOM coletada é LCOM1 em contagem, não normalizada |

## 6. Cap. 4 — `Conteudo/05_Customizacao.tex` (resultados)

### 6.1 Protocolo (`case_study_protocol`)

| Linha | Hoje | Mudança |
|---|---|---|
| 4 | "métricas CK (WMC, DIT, NOC, CBO, RFC, LCOM)" | Mantém |
| 13 | n = 48 (21 desenvolvedores, 27 arquitetos) | Mantém. Acrescentar que **p29 e p33 são o mesmo artefato** (diretórios idênticos) e que ambos foram mantidos, como na coleta original; sem p33, a acurácia fica em 45,7% contra 50,0% de linha de base e ρ = 0,112 |
| 15 | "coleta executada em 21 de agosto de 2026 com a versão `coleta-v1-dissertacao` (*commit* `0bc0f89`)" | Nova data (15/10/2026), tag `coleta-v2` e commit. Acrescentar a preparação: `dotnet restore` dos artefatos, uma compilação por projeto com as referências do .NET e dos pacotes NuGet (98% das chamadas resolvidas) e as limitações: pacotes privados de p7 e o projeto de p4 que não restaura (protocolo, Fase 3). Manter uma frase sobre a v1 e o arquivo `results-v1/` para rastreabilidade |
| 21–42 | `tab:formulas_coleta` (formulações da v1) | **Substituir** pelas formulações da v2 (tabela 6.2 abaixo) |
| 43 | "Essas formulações diferem, em alguns casos, das definições originais…" | Passa a dizer que as formulações seguem as definições do Cap. 2, com as adaptações declaradas lá |
| 45 | "totalizando 2.657 classes e 6.839 métodos" | 3.373 classes (3.004 `class`, 358 `record`, 9 `record struct`, 2 `struct`) e 6.876 métodos. Declarar que **records e structs são medidos** (D19) e que classes homônimas em namespaces diferentes deixaram de ser fundidas |

### 6.2 Nova `tab:formulas_coleta`

| Métrica | Formulação implementada (v2) |
|---|---|
| LOC | LOC lógica (Park, 1992): declarações e instruções, sem chaves, linhas em branco e comentários (D15) |
| V(G) | 1 + pontos de decisão (lista do Cap. 2); `or`/`and` contam; `default`, `_`, `goto`, `??` e `?.` não |
| WMC | $c_i = 1$: métodos e construtores declarados na classe, estáticos inclusive |
| LCOM | LCOM1, $\max(0, \|P\| - \|Q\|)$, sobre os métodos de instância, sem construtores; atributos identificados pelo símbolo |
| DIT | Arestas de herança declaradas; `System.Object` não conta; base externa conta 1 |
| NOC | Subclasses diretas no código analisado |
| CBO | Outros tipos do código (classes, records, structs e interfaces) referenciados pela classe, por qualquer forma; só saída |
| RFC | \|M ∪ R\|, com métodos e construtores identificados pelo símbolo |
| MC | Eq. 23: pesos de Wang e Shao somados na camada e multiplicados no aninhamento; chamada ao sistema = 2 + MC do chamado; biblioteca = 0 |
| AC | 1 por atributo de instância (Eq. 25); `static` e `const` não contam |
| CLC | AC + Σ MC |
| CWC | Σ (2 + MC do chamado) sobre chamadas a métodos de outros tipos do sistema (Eq. 24) |
| CoC | Por subárvore de herança: CLC(c) × Σ CoC(subclasses); soma das hierarquias |

### 6.3 Tabelas e números

Todas as tabelas saem de `analysis/fase4/14-coc-v2/report.md`, já no formato do capítulo:

| Tabela / seção | Linha | O que muda |
|---|---|---|
| `tab:per_class_averages` | 63–66 | Todas as células (tabela pronta na seção 6.6). Ex.: *Expert* WMC 1,58 → 1,67; LCOM 2,15 → 0,56; MC/classe 31,61 → 27,61; CWC/classe 44,39 → 17,48 |
| Texto após a tabela | 74 | "o menor LCOM médio (2,15, contra 5,98–7,12…)" → 0,56 contra 1,42–1,51. A conclusão (*Expert* menor) se mantém. Na CWC/classe, *Mid-Level* (30,23) passa a ficar acima de *Senior* (22,39) |
| `tab:codebase_size` | 91–94 | Tabela pronta na seção 6.6. LOC muda de definição (LOC lógica): *Expert* 2.670 → 1.419 de média. Classes: *Expert* 80,7 → 99,5 (entram records e homônimas) |
| `metric_redundancy` | 101 | Correlações: CLC × LOC 0,84; CWC × LOC 0,81; CoC × LOC 0,54; V(G) × LOC 0,97; CWC × CoC 0,55; CLC × CoC 0,53; MC × CLC 1,00. Trocar a sigla `CC` por `CoC` ("CWC--CC" → "CWC--CoC"). A conclusão vale para CLC, CWC e V(G); a CoC fica menos dependente do tamanho porque passa a seguir a herança |
| `metric_redundancy` | 103 | WMC × LCOM por participante: 0,76 → 0,77 (mantém). CBO × LOC e × classes: −0,10 / −0,13 → 0,39 / 0,27; reescrever "sem indicar relação linear apreciável" como uma correlação positiva fraca a moderada |
| `metric_redundancy` | 105 | "94,2% das classes tinham DIT exatamente igual a 1,0 … na convenção da ferramenta" → 78,9% com DIT = 0 e 20,1% com DIT = 1 (amplitude 0–3), já na convenção do Cap. 2; remover a explicação da convenção. NOC = 0 em 97,4%. DIT × NOC por classe: r = 0,005. Atenção: o r = 0,02 é **por classe**; por participante a correlação é 0,52 (achado da Fase 1 do protocolo) |
| `relating_thresholds` | 113–123 | Limiares absolutos: % Normal WMC 99,8%, DIT 100%, NOC 100% (antes 99,7 / 100 / 100). O achado se mantém |
| `toward_validation` / `empirical_application` | 138, 142, 144 | "2.657 classes e 6.839 métodos" → 3.373 / 6.876. **Remover** "em 99,40% das 2.657 classes, o valor de LCOM é igual a WMC(WMC−1)/2" e a afirmação de que a LCOM se mostrou redundante; substituir por: entre as classes com 2 ou mais métodos, a LCOM atinge o máximo em 36,6%, o que a torna informativa |
| Limiares relativos (P50 / P90) | — | WMC 1 / 5; DIT 0 / 1; NOC 0 / 0; LCOM 0 / 1; AC 1 / 5; CWC 0 / 41,8; MC 3 / 43 (antes: WMC 1/5, DIT 1/1, LCOM 0/10, AC 2/7, CWC 21/159, MC 9/35) |
| `tab:confusion_matrix` | 162–165 | Tabela pronta na seção 6.6. Nova matriz: *Mid-Level* 0/2/9/2; *Senior* 0/2/17/5; *Expert* 0/0/7/3; *Beginner* → *Expert* |
| Texto da classificação | 170 | "classifica corretamente 24 dos 47 (51,1%) … idêntico à linha de base" → 22 de 47 (46,8%), **abaixo** da linha de base (51,1%). ρ = 0,286 → 0,111 (regra em estágios); 0,360 com a média simples do % Normal (antes 0,363) |
| Texto | 172 | Retirar "uma terceira (LCOM) se mostrou redundante com a métrica WMC para esta ferramenta" |
| Texto | 174 | "das 24 classificações incorretas, 20 envolvem a confusão entre níveis adjacentes" → 26 incorretas, 23 adjacentes, 3 a dois ou mais níveis |
| `tab:per_participant_results` | 197–244 | Substituir pela tabela da v2 (seção 6.6): o % Normal das sete métricas para os 48 participantes, o nível previsto e a discrepância. 24 participantes mudam de nível previsto. Isso também elimina as erratas da tabela atual (5 linhas com "Previsto" inconsistente e 8 células de % Normal; ver `analysis/v1/check.md`) |
| `experience_summary` | 252 | 51,1% → 46,8% e "comparável à linha de base" → "abaixo da linha de base". Retirar "LCOM … redundante com WMC em mais de 99% das classes". Corrigir também a frase sobre DIT × NOC, se repetida aqui |

### 6.4 Ameaças à validade (`construct_validity`, linha 274)

**Reescrever o parágrafo.** Hoje ele descreve os defeitos da v1 (LCOM pelos tipos, MC sem aninhamento, CWC com chamadas internas), diz que a análise é "essencialmente sintática", com chamadas e tipos casados pelo nome simples e só `class` medida, e pede replicação com a versão corrigida.

Na v2, o parágrafo passa a dizer:
- os resultados foram recalculados com a versão corrigida, e a coleta original fica arquivada (`results-v1/`) para comparação;
- a análise usa o modelo semântico, com 98% das chamadas resolvidas, e o que fica de fora: pacotes privados de p7, projeto de p4 que não restaura, reflexão e injeção de dependência resolvidas em tempo de execução;
- a suíte de testes tem 410 casos, 94% de cobertura de linhas, e testes de aceite com os exemplos do Cap. 2 e do artigo de Misra;
- adaptações às definições que podem afetar a comparação com outros estudos: CBO só de saída, CoC por subárvore, MC sem chamadas de biblioteca.

Em `internal_validity` ou `reliability`: mencionar p29 = p33 e a sensibilidade sem p33.

### 6.5 Discussão e considerações finais

| Linha | Mudança |
|---|---|
| 298, 324 | "conjunto compacto e não redundante": mantém |
| 326 | "alcançou 51,1% de acurácia (24 de 47 …), igual à linha de base" → 46,8% (22 de 47), abaixo da linha de base de 51,1%; ρ 0,286 → 0,111 |

### 6.6 Tabelas completas da v2

Geradas de `analysis/fase4/14-coc-v2/` (`per_participant_results.csv` e `report.md`). Cada tabela aparece em Markdown, para conferência, e com o corpo em LaTeX no formato atual do capítulo, pronto para colar. Os números de linha são de `05_Customizacao.tex` no commit `601391b`.

#### `tab:per_participant_results` (v2)

% Normal de cada métrica por participante (limiares P50/P90 da amostra combinada da v2) e nível previsto pela regra em estágios. A coluna **Previsto v1** não entra na dissertação: mostra a previsão da coleta original (`analysis/v1/per_participant_results.csv`, recalculada pelo script; difere da tabela publicada nas 5 linhas com errata) para conferência; **24 de 48** participantes mudam de nível previsto.

| ID | Nível | WMC | DIT | NOC | LCOM | AC | CWC | MC | Previsto | Corr. | Discrepância | Previsto v1 |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|---|---|---|
| p1 | Mid-Level | 41,0 | 84,6 | 100,0 | 94,9 | 48,7 | 66,7 | 64,2 | Senior | Não | Adjacente | **Mid-Level** |
| p2 | Expert | 59,3 | 88,9 | 100,0 | 96,3 | 51,9 | 77,8 | 82,9 | Senior | Não | Adjacente | Senior |
| p3 | Expert | 47,4 | 52,6 | 100,0 | 84,2 | 78,9 | 84,2 | 60,0 | Senior | Não | Adjacente | **Mid-Level** |
| p4 | Expert | 57,1 | 81,6 | 95,9 | 89,8 | 69,4 | 68,4 | 52,2 | Senior | Não | Adjacente | **Expert** |
| p5 | Mid-Level | 33,3 | 81,0 | 95,2 | 85,7 | 47,6 | 61,9 | 64,6 | Senior | Não | Adjacente | **Mid-Level** |
| p6 | Mid-Level | 54,5 | 82,7 | 100,0 | 87,3 | 48,2 | 77,3 | 64,7 | Senior | Não | Adjacente | Senior |
| p7 | Senior | 53,9 | 78,7 | 98,9 | 93,3 | 40,4 | 79,8 | 49,1 | Senior | Sim | - | **Mid-Level** |
| p8 | Senior | 54,2 | 92,8 | 100,0 | 91,6 | 53,0 | 73,5 | 50,4 | Senior | Sim | - | Senior |
| p9 | Senior | 74,4 | 85,6 | 100,0 | 94,4 | 43,3 | 74,4 | 50,5 | Senior | Sim | - | Senior |
| p10 | Senior | 60,5 | 76,7 | 100,0 | 86,0 | 55,8 | 69,8 | 42,3 | Senior | Sim | - | **Mid-Level** |
| p11 | Mid-Level | 56,4 | 70,9 | 90,9 | 90,9 | 47,3 | 67,3 | 46,6 | Senior | Não | Adjacente | Senior |
| p12 | Mid-Level | 46,7 | 84,4 | 97,8 | 75,6 | 42,2 | 64,4 | 43,9 | Mid-Level | Sim | - | Mid-Level |
| p13 | Senior | 42,9 | 81,0 | 95,2 | 100,0 | 71,4 | 76,2 | 81,6 | Expert | Não | Adjacente | **Senior** |
| p14 | Senior | 63,2 | 68,4 | 94,7 | 94,7 | 57,9 | 89,5 | 80,0 | Expert | Não | Adjacente | Expert |
| p15 | Expert | 75,5 | 75,5 | 95,7 | 97,9 | 45,7 | 70,2 | 49,0 | Senior | Não | Adjacente | **Expert** |
| p16 | Expert | 55,7 | 74,2 | 94,8 | 84,5 | 70,1 | 75,3 | 68,4 | Expert | Sim | - | **Senior** |
| p17 | Expert | 62,7 | 72,0 | 100,0 | 94,7 | 69,3 | 80,0 | 63,3 | Expert | Sim | - | Expert |
| p18 | Beginner | 61,9 | 52,4 | 95,2 | 90,5 | 76,2 | 85,7 | 66,7 | Expert | Não | Discrepante (+3) | Expert |
| p19 | Mid-Level | 62,9 | 62,9 | 96,8 | 88,7 | 54,8 | 69,4 | 52,2 | Senior | Não | Adjacente | Senior |
| p20 | Mid-Level | 48,1 | 70,4 | 94,4 | 90,7 | 50,0 | 77,8 | 64,1 | Senior | Não | Adjacente | **Mid-Level** |
| p21 | Senior | 57,1 | 64,3 | 100,0 | 100,0 | 57,1 | 71,4 | 58,8 | Senior | Sim | - | Senior |
| p22 | Mid-Level | 42,3 | 61,5 | 96,2 | 80,8 | 26,9 | 69,2 | 59,0 | Senior | Não | Adjacente | **Beginner** |
| p23 | Mid-Level | 39,6 | 70,8 | 97,9 | 81,2 | 70,8 | 68,8 | 66,7 | Expert | Não | Discrepante (+2) | **Senior** |
| p24 | Senior | 54,2 | 79,2 | 99,2 | 81,7 | 55,8 | 65,8 | 44,9 | Senior | Sim | - | Senior |
| p25 | Senior | 58,7 | 77,2 | 96,7 | 89,1 | 75,0 | 78,3 | 63,2 | Expert | Não | Adjacente | **Senior** |
| p26 | Senior | 44,4 | 63,0 | 92,6 | 77,8 | 64,8 | 77,8 | 59,3 | Senior | Sim | - | Senior |
| p27 | Mid-Level | 60,4 | 75,0 | 93,8 | 85,4 | 41,7 | 81,2 | 68,6 | Expert | Não | Discrepante (+2) | **Mid-Level** |
| p28 | Mid-Level | 37,7 | 72,1 | 96,7 | 83,6 | 62,3 | 59,0 | 47,8 | Senior | Não | Adjacente | **Mid-Level** |
| p29 | Senior | 58,5 | 77,1 | 94,1 | 85,1 | 71,8 | 70,2 | 48,8 | Senior | Sim | - | Senior |
| p30 | Senior | 51,6 | 78,1 | 96,9 | 90,6 | 64,1 | 68,8 | 55,7 | Senior | Sim | - | Senior |
| p31 | Senior | 35,0 | 76,7 | 100,0 | 80,0 | 45,0 | 56,7 | 33,8 | Mid-Level | Não | Adjacente | Mid-Level |
| p32 | Senior | 45,9 | 83,8 | 100,0 | 73,0 | 45,9 | 78,4 | 54,9 | Senior | Sim | - | **Mid-Level** |
| p33 | Senior | 58,5 | 77,1 | 94,1 | 85,1 | 71,8 | 70,2 | 48,8 | Senior | Sim | - | Senior |
| p34 | Senior | 68,9 | 75,6 | 97,8 | 88,9 | 55,6 | 80,0 | 56,2 | Senior | Sim | - | Senior |
| p35 | Senior | 17,1 | 82,9 | 100,0 | 94,3 | 45,7 | 37,1 | 46,6 | Mid-Level | Não | Adjacente | Mid-Level |
| p36 | Senior | 35,9 | 74,4 | 100,0 | 89,7 | 51,3 | 71,8 | 55,4 | Senior | Sim | - | **Mid-Level** |
| p37 | Senior | 59,3 | 84,9 | 100,0 | 87,2 | 70,9 | 75,6 | 56,2 | Senior | Sim | - | Senior |
| p38 | Senior | 44,1 | 75,4 | 97,5 | 93,2 | 50,0 | 61,0 | 43,2 | Senior | Sim | - | **Beginner** |
| p39 | Mid-Level | 53,8 | 86,5 | 100,0 | 87,5 | 64,4 | 79,8 | 55,9 | Senior | Não | Adjacente | Senior |
| p40 | Expert | 68,6 | 68,6 | 94,1 | 90,2 | 62,7 | 78,4 | 58,7 | Expert | Sim | - | **Senior** |
| p41 | Senior | 40,9 | 61,4 | 93,2 | 72,7 | 61,4 | 65,9 | 52,3 | Senior | Sim | - | **Mid-Level** |
| p42 | Senior | 70,0 | 73,3 | 100,0 | 83,3 | 70,0 | 73,3 | 50,9 | Expert | Não | Adjacente | **Senior** |
| p43 | Expert | 57,4 | 87,2 | 97,9 | 93,6 | 68,1 | 74,5 | 58,4 | Senior | Não | Adjacente | **Mid-Level** |
| p44 | Senior | 36,7 | 73,3 | 100,0 | 90,0 | 60,0 | 70,0 | 66,7 | Expert | Não | Adjacente | **Senior** |
| p45 | Expert | 54,9 | 79,6 | 95,6 | 77,0 | 59,3 | 64,6 | 34,3 | Senior | Não | Adjacente | Senior |
| p46 | Expert | 51,6 | 95,3 | 100,0 | 83,4 | 58,1 | 66,1 | 51,7 | Senior | Não | Adjacente | Senior |
| p47 | Mid-Level | 35,7 | 85,7 | 100,0 | 89,3 | 25,0 | 60,7 | 47,8 | Mid-Level | Sim | - | Mid-Level |
| p48 | Senior | 43,3 | 88,1 | 100,0 | 94,0 | 59,7 | 71,6 | 50,3 | Senior | Sim | - | **Mid-Level** |

Corpo LaTeX (substitui as linhas 197–244 de `05_Customizacao.tex`; cabeçalho, legenda e formato de colunas não mudam):

```latex
p1 & \textit{Mid-Level} & 41,0 & 84,6 & 100,0 & 94,9 & 48,7 & 66,7 & 64,2 & \textit{Senior} & Não & Adjacente \\
p2 & \textit{Expert} & 59,3 & 88,9 & 100,0 & 96,3 & 51,9 & 77,8 & 82,9 & \textit{Senior} & Não & Adjacente \\
p3 & \textit{Expert} & 47,4 & 52,6 & 100,0 & 84,2 & 78,9 & 84,2 & 60,0 & \textit{Senior} & Não & Adjacente \\
p4 & \textit{Expert} & 57,1 & 81,6 & 95,9 & 89,8 & 69,4 & 68,4 & 52,2 & \textit{Senior} & Não & Adjacente \\
p5 & \textit{Mid-Level} & 33,3 & 81,0 & 95,2 & 85,7 & 47,6 & 61,9 & 64,6 & \textit{Senior} & Não & Adjacente \\
p6 & \textit{Mid-Level} & 54,5 & 82,7 & 100,0 & 87,3 & 48,2 & 77,3 & 64,7 & \textit{Senior} & Não & Adjacente \\
p7 & \textit{Senior} & 53,9 & 78,7 & 98,9 & 93,3 & 40,4 & 79,8 & 49,1 & \textit{Senior} & Sim & - \\
p8 & \textit{Senior} & 54,2 & 92,8 & 100,0 & 91,6 & 53,0 & 73,5 & 50,4 & \textit{Senior} & Sim & - \\
p9 & \textit{Senior} & 74,4 & 85,6 & 100,0 & 94,4 & 43,3 & 74,4 & 50,5 & \textit{Senior} & Sim & - \\
p10 & \textit{Senior} & 60,5 & 76,7 & 100,0 & 86,0 & 55,8 & 69,8 & 42,3 & \textit{Senior} & Sim & - \\
p11 & \textit{Mid-Level} & 56,4 & 70,9 & 90,9 & 90,9 & 47,3 & 67,3 & 46,6 & \textit{Senior} & Não & Adjacente \\
p12 & \textit{Mid-Level} & 46,7 & 84,4 & 97,8 & 75,6 & 42,2 & 64,4 & 43,9 & \textit{Mid-Level} & Sim & - \\
p13 & \textit{Senior} & 42,9 & 81,0 & 95,2 & 100,0 & 71,4 & 76,2 & 81,6 & \textit{Expert} & Não & Adjacente \\
p14 & \textit{Senior} & 63,2 & 68,4 & 94,7 & 94,7 & 57,9 & 89,5 & 80,0 & \textit{Expert} & Não & Adjacente \\
p15 & \textit{Expert} & 75,5 & 75,5 & 95,7 & 97,9 & 45,7 & 70,2 & 49,0 & \textit{Senior} & Não & Adjacente \\
p16 & \textit{Expert} & 55,7 & 74,2 & 94,8 & 84,5 & 70,1 & 75,3 & 68,4 & \textit{Expert} & Sim & - \\
p17 & \textit{Expert} & 62,7 & 72,0 & 100,0 & 94,7 & 69,3 & 80,0 & 63,3 & \textit{Expert} & Sim & - \\
p18 & \textit{Beginner} & 61,9 & 52,4 & 95,2 & 90,5 & 76,2 & 85,7 & 66,7 & \textit{Expert} & Não & Discrepante (+3) \\
p19 & \textit{Mid-Level} & 62,9 & 62,9 & 96,8 & 88,7 & 54,8 & 69,4 & 52,2 & \textit{Senior} & Não & Adjacente \\
p20 & \textit{Mid-Level} & 48,1 & 70,4 & 94,4 & 90,7 & 50,0 & 77,8 & 64,1 & \textit{Senior} & Não & Adjacente \\
p21 & \textit{Senior} & 57,1 & 64,3 & 100,0 & 100,0 & 57,1 & 71,4 & 58,8 & \textit{Senior} & Sim & - \\
p22 & \textit{Mid-Level} & 42,3 & 61,5 & 96,2 & 80,8 & 26,9 & 69,2 & 59,0 & \textit{Senior} & Não & Adjacente \\
p23 & \textit{Mid-Level} & 39,6 & 70,8 & 97,9 & 81,2 & 70,8 & 68,8 & 66,7 & \textit{Expert} & Não & Discrepante (+2) \\
p24 & \textit{Senior} & 54,2 & 79,2 & 99,2 & 81,7 & 55,8 & 65,8 & 44,9 & \textit{Senior} & Sim & - \\
p25 & \textit{Senior} & 58,7 & 77,2 & 96,7 & 89,1 & 75,0 & 78,3 & 63,2 & \textit{Expert} & Não & Adjacente \\
p26 & \textit{Senior} & 44,4 & 63,0 & 92,6 & 77,8 & 64,8 & 77,8 & 59,3 & \textit{Senior} & Sim & - \\
p27 & \textit{Mid-Level} & 60,4 & 75,0 & 93,8 & 85,4 & 41,7 & 81,2 & 68,6 & \textit{Expert} & Não & Discrepante (+2) \\
p28 & \textit{Mid-Level} & 37,7 & 72,1 & 96,7 & 83,6 & 62,3 & 59,0 & 47,8 & \textit{Senior} & Não & Adjacente \\
p29 & \textit{Senior} & 58,5 & 77,1 & 94,1 & 85,1 & 71,8 & 70,2 & 48,8 & \textit{Senior} & Sim & - \\
p30 & \textit{Senior} & 51,6 & 78,1 & 96,9 & 90,6 & 64,1 & 68,8 & 55,7 & \textit{Senior} & Sim & - \\
p31 & \textit{Senior} & 35,0 & 76,7 & 100,0 & 80,0 & 45,0 & 56,7 & 33,8 & \textit{Mid-Level} & Não & Adjacente \\
p32 & \textit{Senior} & 45,9 & 83,8 & 100,0 & 73,0 & 45,9 & 78,4 & 54,9 & \textit{Senior} & Sim & - \\
p33 & \textit{Senior} & 58,5 & 77,1 & 94,1 & 85,1 & 71,8 & 70,2 & 48,8 & \textit{Senior} & Sim & - \\
p34 & \textit{Senior} & 68,9 & 75,6 & 97,8 & 88,9 & 55,6 & 80,0 & 56,2 & \textit{Senior} & Sim & - \\
p35 & \textit{Senior} & 17,1 & 82,9 & 100,0 & 94,3 & 45,7 & 37,1 & 46,6 & \textit{Mid-Level} & Não & Adjacente \\
p36 & \textit{Senior} & 35,9 & 74,4 & 100,0 & 89,7 & 51,3 & 71,8 & 55,4 & \textit{Senior} & Sim & - \\
p37 & \textit{Senior} & 59,3 & 84,9 & 100,0 & 87,2 & 70,9 & 75,6 & 56,2 & \textit{Senior} & Sim & - \\
p38 & \textit{Senior} & 44,1 & 75,4 & 97,5 & 93,2 & 50,0 & 61,0 & 43,2 & \textit{Senior} & Sim & - \\
p39 & \textit{Mid-Level} & 53,8 & 86,5 & 100,0 & 87,5 & 64,4 & 79,8 & 55,9 & \textit{Senior} & Não & Adjacente \\
p40 & \textit{Expert} & 68,6 & 68,6 & 94,1 & 90,2 & 62,7 & 78,4 & 58,7 & \textit{Expert} & Sim & - \\
p41 & \textit{Senior} & 40,9 & 61,4 & 93,2 & 72,7 & 61,4 & 65,9 & 52,3 & \textit{Senior} & Sim & - \\
p42 & \textit{Senior} & 70,0 & 73,3 & 100,0 & 83,3 & 70,0 & 73,3 & 50,9 & \textit{Expert} & Não & Adjacente \\
p43 & \textit{Expert} & 57,4 & 87,2 & 97,9 & 93,6 & 68,1 & 74,5 & 58,4 & \textit{Senior} & Não & Adjacente \\
p44 & \textit{Senior} & 36,7 & 73,3 & 100,0 & 90,0 & 60,0 & 70,0 & 66,7 & \textit{Expert} & Não & Adjacente \\
p45 & \textit{Expert} & 54,9 & 79,6 & 95,6 & 77,0 & 59,3 & 64,6 & 34,3 & \textit{Senior} & Não & Adjacente \\
p46 & \textit{Expert} & 51,6 & 95,3 & 100,0 & 83,4 & 58,1 & 66,1 & 51,7 & \textit{Senior} & Não & Adjacente \\
p47 & \textit{Mid-Level} & 35,7 & 85,7 & 100,0 & 89,3 & 25,0 & 60,7 & 47,8 & \textit{Mid-Level} & Sim & - \\
p48 & \textit{Senior} & 43,3 & 88,1 & 100,0 & 94,0 & 59,7 & 71,6 & 50,3 & \textit{Senior} & Sim & - \\
```


#### `tab:per_class_averages` (v2)

| Nível | N | WMC | LCOM | MC/classe | AC/classe | CWC/classe |
|---|---|---|---|---|---|---|
| Beginner | 1 | 1,86 (1,86) | 0,29 (0,29) | 9,86 (9,86) | 0,86 (0,86) | 1,95 (1,95) |
| Mid-Level | 13 | 2,25 (2,35) | 1,42 (0,58) | 38,72 (23,04) | 2,00 (2,02) | 30,23 (12,43) |
| Senior | 24 | 2,18 (1,99) | 1,51 (0,66) | 36,52 (31,63) | 1,78 (1,77) | 22,39 (18,94) |
| Expert | 10 | 1,67 (1,62) | 0,56 (0,52) | 27,61 (11,72) | 1,59 (1,55) | 17,48 (6,03) |

Corpo LaTeX (substitui as linhas 63–66):

```latex
\textit{Beginner} & 1 & 1,86 (1,86) & 0,29 (0,29) & 9,86 (9,86) & 0,86 (0,86) & 1,95 (1,95) \\
\textit{Mid-Level} & 13 & 2,25 (2,35) & 1,42 (0,58) & 38,72 (23,04) & 2,00 (2,02) & 30,23 (12,43) \\
\textit{Senior} & 24 & 2,18 (1,99) & 1,51 (0,66) & 36,52 (31,63) & 1,78 (1,77) & 22,39 (18,94) \\
\textit{Expert} & 10 & 1,67 (1,62) & 0,56 (0,52) & 27,61 (11,72) & 1,59 (1,55) & 17,48 (6,03) \\
```

#### `tab:codebase_size` (v2)

LOC = LOC lógica (Park, 1992), coluna `LLOC`. A v1 usava a contagem antiga (linhas não vazias no corpo dos métodos), por isso os valores caem; o rótulo da coluna deve passar a dizer "LOC lógica".

| Nível | N | LOC média (mediana) | Classes média (mediana) |
|---|---|---|---|
| Beginner | 1 | 254 (254) | 21,0 (21,0) |
| Mid-Level | 13 | 991 (914) | 53,9 (48,0) |
| Senior | 24 | 1161 (937) | 69,0 (57,0) |
| Expert | 10 | 1419 (1255) | 99,5 (84,5) |

Corpo LaTeX (substitui as linhas 91–94):

```latex
\textit{Beginner} & 1 & 254 (254) & 21,0 (21,0) \\
\textit{Mid-Level} & 13 & 991 (914) & 53,9 (48,0) \\
\textit{Senior} & 24 & 1161 (937) & 69,0 (57,0) \\
\textit{Expert} & 10 & 1419 (1255) & 99,5 (84,5) \\
```

#### `tab:confusion_matrix` (v2)

| Conhecido \ Previsto | Beginner | Mid-Level | Senior | Expert |
|---|---|---|---|---|
| Beginner (n=1) | 0 | 0 | 0 | 1 |
| Mid-Level (n=13) | 0 | 2 | 9 | 2 |
| Senior (n=24) | 0 | 2 | 17 | 5 |
| Expert (n=10) | 0 | 0 | 7 | 3 |

Corpo LaTeX (substitui as linhas 162–165):

```latex
\textit{Beginner} ($n=1$) & 0 & 0 & 0 & 1 \\
\textit{Mid-Level} ($n=13$) & 0 & 2 & 9 & 2 \\
\textit{Senior} ($n=24$) & 0 & 2 & 17 & 5 \\
\textit{Expert} ($n=10$) & 0 & 0 & 7 & 3 \\
```

## 7. Cap. 5 — `Conteudo/06_Conclusao.tex`

| Linha | Hoje | Mudança |
|---|---|---|
| 13 | Hipótese 1: totais "fortemente correlacionados entre si" | Mantém para CLC, CWC e V(G); ressalvar a CoC (r com LOC 0,54) |
| 15 | Hipótese 2: "acurácia … de 51,1% … não superou uma linha de base" | 46,8%, **abaixo** da linha de base; o suporte à hipótese fica mais fraco |
| 31 | "48 participantes, 2.657 classes" | 48 participantes, 3.373 classes |
| 36 | "94,2% das classes têm DIT igual a 1 na convenção da ferramenta, e 97,3% têm NOC igual a 0"; "a CBO não se correlacionou com o tamanho"; "a LCOM … não forneceu informação independente da WMC" | 78,9% com DIT = 0 e 97,4% com NOC = 0; CBO com correlação positiva fraca com o tamanho (r = 0,27 com o número de classes); a LCOM passa a fornecer informação independente da WMC na versão corrigida (36,6% das classes com 2 ou mais métodos na LCOM máxima, contra 98,7% na coleta original) |
| 51 | Achado metodológico: "(99,7% a 100% das classes classificadas como Normal…)" e "como evidenciado pelo caso da LCOM" | 99,8% a 100%. Manter o caso da LCOM como evidência, agora com o desfecho: o defeito mudava a conclusão |
| 61–63 | Transparência da ferramenta; "os valores obtidos para os 48 participantes podem ser interpretados à luz da definição" | Mantém; citar a tag `coleta-v2` |
| 92 | Trabalho futuro: "implementações verificadas das métricas, particularmente da LCOM" | Reformular: a verificação foi feita nesta versão (testes de aceite com os exemplos do Cap. 2 e do artigo). O trabalho futuro passa a ser a validação independente das adaptações (CoC por subárvore, CBO só de saída, MC sem bibliotecas) |

## 8. Itens independentes da recoleta

Valem mesmo que a v2 não seja adotada (Fase 1 do protocolo):

- A frase sobre DIT × NOC ("0,98 em n = 36" que "desapareceu" em n = 48) mistura granularidades: 0,98 é por participante, 0,02 é por classe. Corrigir em `metric_redundancy` e `experience_summary`.
- Erratas de `tab:per_participant_results` na v1: listadas em `analysis/v1/check.md` e em `scripts/analysis/published_v1.json`.

## 9. Conferência final

- [ ] `grep` em `master-thesis/Conteudo` e `Pre_Textual` não encontra mais: `51,1\% de acurácia` como valor do modelo, `0{,}286`, `2.657`, `6.839`, `94,2`, `99,40`, `uma unidade maiores`, `metade da soma`, `CWC--CC`, `0bc0f89` (exceto onde citado como v1).
- [ ] Resumo e Abstract dizem o mesmo que o Cap. 4 e a Conclusão.
- [ ] `tab:formulas_coleta` e o Cap. 2 descrevem a mesma regra para cada métrica.
- [ ] O PDF compila (`make`) sem referências quebradas.
