# Plano de recoleta dos dados do estudo de caso (após finalizar a dissertação)

## Contexto

Os números do Cap. 4 (tabelas por nível, correlações, matriz de confusão, 51,1% de acurácia, ρ = 0,286, LCOM = WMC(WMC−1)/2 em 99,40% das classes) foram gerados em 21/08/2026. A versão da ferramenta usada era anterior às correções de **LCOM** (`054fc08`) e de **MC/CWC** (`017cfa0`), ambas de 07/10/2026. O próprio guia de reprodução avisa que LCOM, MC, CLC, CWC e CC mudam com a versão atual (`net-metrics-collector/docs/research/reproducing-dissertation.md:5`). O achado "LCOM redundante com WMC" provavelmente é efeito do defeito.

Hoje existe o script de coleta e o `scripts/generate_index.py`, que gera o resumo descritivo. **Não existe script versionado da análise CBTAM**: limiares por percentil, regra em estágios, matriz de confusão e ρ.

O objetivo, depois da defesa, é revisar o cálculo de cada métrica na ferramenta, ajustá-la às definições do Cap. 2, recoletar com a versão revisada, recalcular tudo de forma reproduzível e levar os resultados para a versão final (correções da banca) e/ou para o artigo.

Repositórios envolvidos (vizinhos em `~/Documents/github/`):
- `net-metrics-collector`: ferramenta, `results/`, `scripts/`, `tests/`
- `artefatos-mestrado`: os 48 artefatos `p{n}-{cargo}-{nível}`, privado
- `master-thesis`: o texto

---

## Etapa 0 — Congelar a linha de base (≈ 0,5 dia)

1. Identificar o commit da ferramenta usado na coleta de 21/08. O candidato é `0bc0f89` (07/05/2026, o último commit de código antes da coleta; `a8564cf` só mexe em docs). Criar a tag `coleta-v1-dissertacao`.
2. Arquivar os CSVs atuais: `results/` → `results-v1/`, versionados. Registrar o commit de `artefatos-mestrado` (`40a6044`) e a versão do .NET SDK.
3. Criar a tag `defesa-2026-11-27` em `master-thesis` para congelar o texto defendido.

## Etapa 1 — Decisões de escopo, antes de rodar (≈ 0,5 dia)

Registrar cada decisão em `docs/research/protocolo-recoleta.md`, no repositório da ferramenta:
- **Código de teste:** hoje entram classes de `tests/` (ex.: `LancamentoTests` em `p1-dev-pl`). Recomendação: **excluir** os projetos de teste, porque o CBTAM avalia decisões de projeto do código de produção. Rodar também com os testes incluídos, como análise de sensibilidade.
- **Código gerado:** excluir `Migrations/`, `*.Designer.cs`, `obj/` e `bin/`.
- **Classes parciais e duplicadas:** manter a regra do Cap. 4 (consolidar um registro por classe) e implementá-la no script, não à mão.
- **Tipos analisados:** hoje só as declarações `class` são medidas. Decidir se entram também `record class` (comum em C# moderno para DTOs e entidades) e `struct`/`record struct`. Recomendação: incluir `record class` e manter `struct` fora.

## Etapa 2 — Revisão dos cálculos das métricas e ajustes na ferramenta (≈ 3–5 dias)

Antes de recoletar, comparar o cálculo de cada métrica na ferramenta (seções "How this tool computes it" em `net-metrics-collector/docs/*.md`) com a definição do Cap. 2 da dissertação e com a fonte original (CK 1994; Misra et al. 2018, `docs/anexos/`). Para cada divergência, escolher uma de três saídas:
- **Ajustar** a ferramenta para seguir a definição;
- **Parametrizar**, com uma opção de linha de comando que permita calcular as duas variantes (útil para comparar com a v1);
- **Documentar** a divergência como adaptação, no Cap. 2/4 e em `docs/*.md`.

### 2.1 Divergências já identificadas

| Métrica | Definição (Cap. 2 / fonte) | Cálculo atual da ferramenta | Ajuste proposto |
|---|---|---|---|
| **LOC** | LOC física e lógica (Park, 1992); usada para o tamanho dos projetos | Linhas não vazias e sem comentário **só no corpo de métodos e construtores**; ignora campos, propriedades e declarações de classe | Acrescentar uma coluna de LOC por arquivo inteiro (sem linhas em branco e sem comentários) para medir o tamanho do projeto; manter a LOC por método |
| **WMC** | Exemplo do Cap. 2 pondera pela complexidade ciclomática ($c_i$ = V(G)) | $c_i$ = 1; conta só métodos não estáticos e construtores | **Parametrizar** (`c_i = 1` × `c_i = V(G)`) e alinhar a escolha com o Cap. 2; decidir se métodos estáticos entram |
| **RFC** | Métodos da classe ∪ métodos chamados diretamente | Casa as chamadas **pelo nome simples** (`a.Save()` e `b.Save()` contam uma vez); inclui chamadas ao framework | Usar o modelo semântico do Roslyn para identificar o método chamado (símbolo completo) |
| **LCOM** | LCOM1 (P − Q, mínimo 0) | LCOM1 sobre métodos não estáticos e construtores; campos e propriedades como atributos | Validar a correção de `054fc08`; decidir se **construtores** entram, porque eles costumam acessar todos os campos e reduzem a LCOM artificialmente |
| **CBO** | Acoplamento com qualquer classe usada (CK 1994) | Só tipos de campos, propriedades, retornos e parâmetros de métodos; **ignora** variáveis locais, `new`, chamadas estáticas e **parâmetros de construtor primário** | Incluir parâmetros de construtor (inclusive primário, muito usado em injeção de dependência no .NET 8+), tipos instanciados com `new` e chamadas estáticas a tipos do projeto |
| **DIT** | Caminho até a raiz considerando **só herança explícita**; `System.Object` não conta, e uma classe sem herança explícita tem DIT = 0 (Cap. 2) | Conta `System.Object` como 1 nível; uma classe base do framework conta 1 nível sem os ancestrais (é um limite inferior) | **Ajustar** para a convenção do Cap. 2 (sem `System.Object`); decidir como tratar bases do framework (contar só a herança declarada no código analisado ou resolver os ancestrais); depois, retirar a frase de deslocamento em `04_Outros.tex` e a nota de rodapé do Cap. 2 |
| **NOC** | Subclasses diretas | Coerente (só o código analisado) | Nenhum |
| **MC** | Pesos BCS (Tabela 1 de Misra); **soma em sequência, produto no aninhamento** (eq. 23); bloco sequencial = 1; `switch` = 3 | Pesos **somados** em qualquer profundidade; **cada instrução** vale 1; `switch` = 2; cada chamada vale 2 | Implementar a eq. 23: bloco linear = 1, multiplicação por camada, `case` = 3, recursão = 2. Manter a variante aditiva como opção (`--mc-additive`) para comparar com a v1 |
| **AC** | Número de atributos (eq. 25: peso 1 por atributo) | Peso 1 para tipos simples e 2 para os demais | **Parametrizar**; o padrão segue Misra (peso 1) |
| **CLC** | AC + Σ MC (eq. 26) | Mesma fórmula, mas com a AC e a MC da ferramenta | Segue os ajustes de AC e MC |
| **CWC** | Σ (2 + MC do método chamado), **só chamadas a outras classes do sistema** (eq. 24) | Chamadas a métodos do código: MC + 2; chamadas a bibliotecas: **3 fixo**; casamento pelo nome simples | Excluir chamadas ao framework (ou parametrizar); identificar o método chamado pelo modelo semântico |
| **CoC** | Produto entre níveis da hierarquia da soma das CLC de cada nível; hierarquias distintas somadas (eq. 27) | ΣCLC + ΣCWC/2, **sem considerar herança** | Implementar a eq. 27 sobre o grafo de herança já montado para DIT/NOC |
| **V(G)** | 1 + nº de decisões (Cap. 2) | Conferir como contam `&&`/`||`, `case`, `catch`, `??` e o ternário | Alinhar com a definição do Cap. 2 e documentar |

### 2.2 Procedimento

1. **Inventário:** para cada métrica, ler a implementação (`src/MetricsExporter.Net/Services/*Calculator*` e `Parsers/*Visitor*`) e confirmar ou atualizar a linha da tabela acima.
2. **Casos de teste antes do ajuste:** escrever testes com os exemplos do Cap. 2 e do artigo de Misra (classes COMPUTER, HARDWARE, SOFTWARE, DESKTOP e NOTEBOOK do apêndice, com os valores publicados: MC(HARDWARE) = 16, CLC(HARDWARE) = 20, CWC(HARDWARE) = 9, CoC = 536), portados para C#. Eles devem falhar onde houver divergência.
3. **Implementar os ajustes** métrica por métrica, um commit por métrica, até os testes passarem.
4. **Manter as variantes antigas** acessíveis por opção quando houver parametrização, para que a comparação v1 × v2 (Etapa 6) separe o efeito de **correção de defeito** do efeito de **mudança de definição**.
5. **Documentar:** atualizar cada seção "How this tool computes it" e `docs/anexos/descricao-ferramenta.md` (§3) na dissertação.
6. **Revisão com o orientador** da tabela final de decisões antes de recoletar.

## Etapa 3 — Validar a ferramenta corrigida (≈ 1–2 dias)

1. `dotnet test` em `tests/MetricsExporter.Net.Tests`, com todos os testes passando.
2. Atualizar `tests/.../Integration/DocumentationExamplesTests.cs` com os exemplos **atuais** do Cap. 2 da dissertação: DIT = 2, LOC lógica = 4, MC = 12, CLC = 6, CWC = 4, CoC = 4. Incluir também os casos do artigo de Misra criados na Etapa 2.
3. Validação cruzada da LCOM: em 3 a 5 classes reais de participantes diferentes, calcular a LCOM1 à mão e comparar com a ferramenta.
4. Atualizar `docs/loc.md`, que ainda diz LOC lógica = 3, e os demais `docs/*.md` conforme as decisões da Etapa 2.
5. Criar a tag `coleta-v2`.

## Etapa 4 — Script de análise versionado (≈ 2–3 dias)

Criar `scripts/analysis/cbtam_analysis.py` (Python + pandas/scipy) no repositório da ferramenta. Ele recebe um diretório de resultados (`--results results-v1|results`) e gera `analysis/<versão>/` com CSVs e um `report.md`. Deve reproduzir **todos** os números do Cap. 4:

| Saída | Seção do Cap. 4 (`Conteudo/04_Outros.tex`) |
|---|---|
| Médias e medianas por classe de WMC, LCOM, MC, AC e CWC por nível | `tab:per_class_averages` |
| LOC e nº de classes por nível | `tab:codebase_size` |
| Pearson entre métricas, LOC e classes; % de DIT = 1 e de NOC = 0 | `metric_redundancy` |
| Desenvolvedor × arquiteto | `position_analysis` |
| % Normal pelos limiares absolutos (WMC, DIT, NOC) | `empirical_application` |
| Checagem LCOM = WMC(WMC−1)/2 | `empirical_application` |
| Classificação por percentis (P50/P90), regra em estágios 40/50/60%, matriz de confusão, acurácia, linha de base, ρ de Spearman, erros em níveis adjacentes | `tab:confusion_matrix`, `per_participant_results`, `experience_summary` |

- Os rótulos vêm do nome da pasta: `jr` → Beginner, `pl` → Mid-Level, `sn` → Senior, `esp` → Expert.
- **Teste de ouro:** rodar sobre `results-v1/` e reproduzir os números publicados (51,1%, ρ = 0,286, 99,40%, 2.657 classes, 6.839 métodos, as tabelas). Se não reproduzir, investigar antes de seguir, porque o script ainda não é confiável.

## Etapa 5 — Recoletar (≈ 0,5 dia)

1. Rodar o script "Running Everything at Once" de `reproducing-dissertation.md` com a tag `coleta-v2`, aplicando os filtros da Etapa 1.
2. Rodar `generate_index.py` e `cbtam_analysis.py --results results`.
3. Conferir que há 48 participantes, que nenhum CSV está vazio e que as contagens de classes e métodos são plausíveis frente à v1.

## Etapa 6 — Comparar v1 × v2 (≈ 1 dia)

Gerar `analysis/comparacao-v1-v2.md` com:
- variação de cada número publicado;
- se a redundância LCOM ≡ WMC desaparece;
- se a ordenação Expert < Mid/Senior se mantém;
- acurácia × linha de base;
- ρ;
- efeito da exclusão dos testes (sensibilidade);
- separação entre efeito de correção de defeitos e efeito de mudança de definição (usando as variantes parametrizadas da Etapa 2).

Classificar cada conclusão da dissertação como **mantida**, **enfraquecida** ou **revertida**.

## Etapa 7 — Atualizar o texto (≈ 2–4 dias, conforme o impacto)

Pontos que dependem dos dados, em `master-thesis`:
- `Pre_Textual/Resumo.tex` e `Abstract.tex`: os 51,1% e a frase sobre a LCOM ser redundante.
- `Conteudo/01_Introducao.tex`: a contribuição "Achados metodológicos" (`sec:intro_contribuicoes`).
- `Conteudo/02_Referencial_Teorico.tex`: o parágrafo final da seção de Misra, sobre as simplificações da ferramenta, conforme as decisões da Etapa 2 (se a ferramenta passar a seguir Misra, o parágrafo vira uma frase de confirmação).
- `Conteudo/04_Outros.tex`:
  - todas as tabelas e números de `results`;
  - `toward_validation` (variante da LCOM);
  - `construct_validity`, citando a tag `coleta-v2`;
  - o protocolo do estudo de caso: filtros, versão e commit.
- `Conteudo/06_Conclusao.tex`: os números, a seção Ferramentas e os Trabalhos Futuros (o item "ferramentas verificadas para LCOM" pode estar resolvido).
- Apêndice (se houver): tabela por participante.

Destino: se ainda estiver no prazo de correções pós-defesa, entra na versão final depositada, com aval do orientador. Se não, vai para o artigo e para uma errata/nota no repositório.

## Verificação

- `dotnet test` passando, com os exemplos do Cap. 2 e do artigo de Misra (CoC = 536) cobertos.
- Cada linha da tabela 2.1 tem uma decisão registrada (ajustar, parametrizar ou documentar) e um commit ou trecho de documentação correspondente.
- `cbtam_analysis.py` sobre `results-v1/` reproduz exatamente os números da versão defendida (teste de ouro).
- `cbtam_analysis.py` sobre `results/` (v2) roda do zero com um único comando, e o `report.md` é igual ao conteúdo das tabelas no `.tex`.
- `grep` em `master-thesis` pelos números antigos (51,1, 0,286, 99,40, 2.657) não acha nada que tenha ficado sem querer.
- O PDF compila sem referências indefinidas.

## Estimativa total

Cerca de 11 a 17 dias de trabalho, a começar depois de 27/11/2026. A Etapa 2 é a mais longa e a que mais pode variar, conforme quantas divergências virarem ajuste.
