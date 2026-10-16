# Protocolo de recoleta e comparação v1 × v2

> Criado em 12/10/2026 a partir de `docs/PLANO_RECOLETA.md`, depois de conferir a proposta contra o código da ferramenta, a suíte de testes e os CSVs de `results/`.
> Este documento substitui a ordem de etapas do plano original e é o lugar onde se registram as decisões de escopo (seção 4).

## 1. Situação atual verificada

- `dotnet test`: 140 testes passam e 1 está pulado (`CyclomaticComplexityCalculatorTests.ElseIf_CountsOnce`).
- `results/` tem 48 participantes, com 3.052 linhas em `metrics_ck.csv`. Sem as 395 linhas de nomes repetidos dentro do mesmo participante, sobram **2.657**, exatamente o nº de classes do Cap. 4.
- Em 3.031 das 3.052 linhas, LCOM = WMC(WMC−1)/2. Isso confirma que a redundância LCOM ≡ WMC relatada na dissertação é efeito do defeito corrigido em `054fc08`.
- 625 linhas (≈20%) vêm de caminhos de teste e 143 de `Migrations/` ou `obj/`.
- Existem 373 declarações `record` nos artefatos que a ferramenta não mede.
- Estado do repositório: `tests/` está **untracked** e `src/MetricsExporter.Net/CLI/Commands/RootCommandHandler.cs` tem alterações locais.
- Versões: artefatos `artefatos-mestrado@40a6044`, .NET SDK 10.0.101.

## 1.1 Linha de base congelada (Fase 0, 12/10/2026)

| Item | Valor |
|---|---|
| Ferramenta | `0bc0f89` (código idêntico a `a8564cf`, que só altera docs). A tag `coleta-v1-dissertacao` criada nesta fase foi removida em 16/10/2026: a única tag da ferramenta é `v1`, a versão usada na dissertação (seção 3, Fase 4) |
| Artefatos | `artefatos-mestrado@40a6044` |
| SDK | .NET 10.0.101 (macOS arm64) |
| Dados | `results-v1/` (48 participantes × 4 CSVs, `index.md`, `index.pdf`) |
| Comando | `dotnet <build Release de 0bc0f89>/MetricsExporter.Net.dll --path ../artefatos-mestrado/<p>/ --out <saída>/<p>`, executado na raiz deste repositório |
| **G0** | **Aprovado.** Os 192 CSVs gerados são idênticos byte a byte aos de `results-v1/`, inclusive a coluna `FilePath` e a ordem das linhas |

A tag `defesa-2026-11-27` em `master-thesis` fica para o dia da defesa, já que o texto ainda está em revisão.

## 2. Achados que o plano original não cobria

| # | Achado | Evidência | Impacto |
|---|---|---|---|
| A | **p29 e p33 são o mesmo artefato** | `diff -rq artefatos-mestrado/p29-arch-sn artefatos-mestrado/p33-arch-sn` não acusa diferença; os CSVs só diferem no caminho | n = 48 vira 47 participantes distintos, e um Sênior conta duas vezes na acurácia e no ρ |
| B | **Uma classe é identificada só pelo nome simples** | `CkMetricsAnalysisService.cs` e os calculadores filtram por `ContainingClass == className`; o caminho do arquivo é buscado com `FirstOrDefault(c => c.Name == className)` | Classes de mesmo nome em namespaces diferentes (`Program`, `DependencyInjection`…) e classes `partial` somam WMC, RFC, LCOM e CWC entre si, e o caminho do arquivo pode sair errado |
| C | **Sobrecargas de método se sobrescrevem na MC** | Chave `"{Classe}::{Metodo}"` em `MisraSyntaxVisitor.cs` | Só a última sobrecarga entra na MC, CLC e CWC |
| D | **V(G) conta `else if` duas vezes** | Teste `ElseIf_CountsOnce` pulado; `CyclomaticComplexityVisitor.VisitIfStatement` soma +1 no `else` e +1 de novo ao visitar o `if` interno | V(G) inflada em código com cadeias `else if` |
| E | **O modelo semântico só referencia a CoreLib** | `AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))` em `CkMetricsAnalysisService.cs` | Tipos de ASP.NET/EF não se resolvem. Isso **bloqueia** os ajustes de RFC, CWC e CBO "via Roslyn" e o ajuste de DIT com ancestrais do framework. Pré-requisito: carregar os projetos com MSBuildWorkspace depois de `dotnet restore` nos artefatos |
| F | Membros que não são método não são medidos | Propriedades com lógica, accessors, funções locais e lambdas ficam fora de WMC, MC e LOC; `record`, `struct` e `interface` não são visitados | Decisão de escopo (seção 4) |
| G | CBO ignora **todos** os parâmetros de construtor, não só os do construtor primário, e também a classe base | `CkSyntaxVisitor.VisitClassDeclaration` só olha campos, propriedades e `MethodDeclarationSyntax` | Reforça a linha CBO da tabela 2.1 do plano |
| H | Detalhes da MC | `else` pesa 2; `switch` pesa 2 independentemente do nº de `case`; chamadas dentro de lambdas contam | Tratar junto com a implementação da eq. 23 |
| I | O estado do repositório impedia congelar a linha de base | `tests/` untracked; `RootCommandHandler.cs` modificado; projeto de testes duplicado no `.sln` | **Resolvido na Fase 0** (`2ffc580`) |

A tabela 2.1 de `docs/PLANO_RECOLETA.md` continua valendo para as divergências de definição (LOC, WMC, RFC, LCOM, CBO, DIT, NOC, MC, AC, CLC, CWC, CoC, V(G)).

## 3. Fases

A principal mudança em relação ao plano original: **o script de análise e o teste de ouro vêm antes de mexer na ferramenta**, para que o efeito de cada correção possa ser medido isoladamente.

### Fase 0: Linha de base reproduzível (≈1 dia)

1. Versionar `tests/` e decidir o que fazer com o diff de `RootCommandHandler.cs` (commitar ou descartar).
2. Num worktree em `0bc0f89`, rodar a coleta sobre `artefatos-mestrado@40a6044` e comparar com `results/`, ignorando a coluna `FilePath`.
   - **Gate G0:** a saída tem que ser idêntica. Só então criar a tag `coleta-v1-dissertacao`. Se divergir, procurar o commit certo antes de seguir.
3. Mover `results/` para `results-v1/` (versionado) e registrar commit dos artefatos e versão do SDK.
4. Criar a tag `defesa-2026-11-27` em `master-thesis`.

### Fase 1: Script de análise e teste de ouro (≈2–3 dias)

1. Criar `scripts/analysis/cbtam_analysis.py`, que recebe `--results <dir>` e gera `analysis/<versão>/` com CSVs e `report.md`, cobrindo todas as saídas listadas na Etapa 4 do plano original.
2. Consolidar as classes exatamente como no Cap. 4.
3. Gerar também `per_class.csv` com uma chave estável (participante + arquivo + classe), que a Fase 6 usa no pareamento.
4. **Gate G1:** rodando sobre `results-v1/`, o script tem que reproduzir 2.657 classes, 6.839 métodos, 99,40%, 51,1% e ρ = 0,286, além das tabelas do Cap. 4. Se não reproduzir, o script ainda não é confiável.

#### Resultado (12/10/2026): G1 aprovado

- Script só com a biblioteca padrão (o Python do sistema não tem pandas/scipy, e assim o resultado não depende de versões de pacotes). Uso e regras em `scripts/analysis/README.md`.
- `python3 scripts/analysis/cbtam_analysis.py --results results-v1 --out analysis/v1 --check scripts/analysis/published_v1.json` termina com 0 divergências não explicadas.
- **Reproduzidos exatamente:** contagens (48 / 2.657 / 6.839), `tab:per_class_averages`, `tab:codebase_size`, `position_analysis`, 94,2% DIT = 1, 97,3% NOC = 0, amplitude da LCOM 0,42–57,92, limiares absolutos (99,7 / 100 / 100), 99,40%, correlações citadas, `tab:confusion_matrix`, 51,1% contra linha de base de 51,1%, ρ = 0,286, ρ = 0,363 e 24 erros (20 adjacentes, 4 a dois ou mais níveis).
- `scripts/generate_index.py` passou a aceitar o diretório de resultados como argumento e reproduz `results-v1/index.md` (só a data muda).

#### Regras implícitas descobertas (não descritas no Cap. 4)

| Regra | Como foi identificada |
|---|---|
| Classes consolidadas por (arquivo, classe) mantendo o **último** registro, não por nome | Só essa combinação dá 2.657 e explica DIT/NOC de p11, p15, p20, p25 e p41 |
| Métodos (MC) **sem** deduplicação | 6.839 = todas as linhas de método |
| Percentis com interpolação linear; Normal ≤ P50 | P50/P90 resultantes: WMC 1/5, DIT 1/1, NOC 0/0, LCOM 0/10, AC 2/7, CWC 21/159, MC 9/35 |
| **DIT × NOC (r = 0,02) é por classe**, não por participante | Por participante, r = 0,94 (consolidado) ou 0,98 (bruto) |
| CBO × classes (r = −0,13) usa o nº de classes **antes** da consolidação | Com o nº consolidado, r = −0,11 |

#### Achados para a Fase 7 (texto)

1. **A tabela `tab:per_participant_results` tem erros de transcrição.** Aplicando a regra em estágios aos próprios valores publicados, 5 linhas (p6, p10, p40, p41, p45) dão um nível diferente da coluna "Previsto", e essa coluna geraria outra matriz de confusão (27/47 = 57,4%). A matriz, a acurácia e o ρ publicados estão corretos; a tabela não. Há ainda 8 células de % Normal divergentes (p13, p18, p19 ×2, p22, p28, p40, p47), duas delas impossíveis com o nº de classes do participante. A lista completa, com justificativas, está na seção `errata` de `scripts/analysis/published_v1.json` e em `analysis/v1/check.md`.
2. **A frase sobre DIT × NOC está errada.** O texto diz que a correlação "0,98 em n = 36" desapareceu em n = 48. Na verdade, 0,98 é uma correlação por participante e 0,02 é por classe; por participante, em n = 48, ela continua em 0,94–0,98. A mudança vem da granularidade, não do tamanho da amostra.
3. **Sensibilidade p33:** sem a duplicata, a acurácia fica em 23/46 = 50,0%, igual à linha de base (também 50,0%), e ρ = 0,285. As conclusões não mudam.

### Fase 2: Decisões de escopo (≈0,5 dia)

Preencher a seção 4 deste documento antes de alterar a ferramenta.

### Fase 3: Infraestrutura e correções de defeito (≈2–3 dias)

São erros de implementação, não mudanças de definição. Cada item tem seu commit e seu teste, escrito antes da correção:

1. Identidade da classe por símbolo (FQN), com `partial` consolidado na própria ferramenta (achado B).
2. Chave de método pela assinatura completa (achado C).
3. `else if` na V(G); reativar `ElseIf_CountsOnce` (achado D).
4. Compilação com referências reais via MSBuildWorkspace (achado E).
5. Filtros de escopo decididos na Fase 2 (testes, código gerado, tipos analisados).

#### Resultado (12/10/2026): itens 1–4 concluídos e identificação de chamadas por símbolo (achado do item 5 da revisão)

Branch `recoleta/fase-3`; cada commit tem testes escritos antes da correção (162 testes, nenhum pulado). O item 5 (filtros de escopo) depende da Fase 2 e ainda não foi feito.

| Passo | Commit | O que muda |
|---|---|---|
| 01 | `bf45bac` | Classe identificada por projeto + nome completo (achado B). Uma compilação por `.csproj`, ligada pelos `<ProjectReference>`. `partial` vira uma classe só; métodos de interface/struct/record aninhados deixam de contar para a classe externa |
| 02 | `570929b` | Chave do método inclui a lista de parâmetros (achado C) |
| 03 | `df14394` | V(G) sem `else if` em dobro (achado D) |
| — | `38dd36c` | Log da taxa de chamadas resolvidas (não muda métricas) |
| 04 | `1a0a5a8`, `bdb1d71` | Referências reais (achado E): targeting packs do SDK, usings implícitos por SDK, pacotes NuGet do `obj/project.assets.json` e `ProjectReference` transitivo. Em vez do MSBuildWorkspace, os arquivos de projeto são lidos diretamente; o MSBuild só roda no `dotnet restore` |
| 05 | `ba447d1` | RFC e CWC identificam o método chamado pelo símbolo (achado do casamento por nome) |

**Taxa de chamadas resolvidas** (42.799 invocações em classes): 8,0% só com a CoreLib → 19,4% com usings implícitos → 34,5% com o framework → **98,1%** com os pacotes NuGet e as referências transitivas.

**Impacto de cada passo** (detalhes em `analysis/fase3/comparacao.md`):

| Número | v1 | 00 (HEAD `231192a`) | 01 identidade | 02 sobrecargas | 03 V(G) | 04 referências | 05 chamadas |
|---|---:|---:|---:|---:|---:|---:|---:|
| Classes | 2.657 | 2.657 | 3.004 | 3.004 | 3.004 | 3.004 | 3.004 |
| LCOM = WMC(WMC−1)/2 | 99,40% | 69,48% | 71,87% | = | = | = | = |
| Acurácia (linha de base 51,1%) | 51,1% | 46,8% | 46,8% | 44,7% | 44,7% | 44,7% | 46,8% |
| ρ regra em estágios | 0,286 | 0,177 | 0,198 | 0,160 | 0,160 | 0,160 | 0,198 |
| ρ média do % Normal | 0,363 | 0,348 | 0,305 | 0,308 | 0,308 | 0,325 | 0,333 |

- 01 é o passo com mais efeito estrutural: +347 classes (homônimas separadas), WMC muda em 110 classes, RFC em 196, CWC em 267.
- 02 muda CLC em 55 classes e CWC em 35; 03 muda a V(G) de 25 métodos (−0,2%); 04 muda só DIT (3 classes) e NOC (1), pelas referências transitivas; 05 muda RFC em 822 classes (+2%) e CWC em 967 (+6%).
- Exemplo verificado à mão no passo 05: em p12, `EntryService` tem métodos `GetItem` e `GetAll`, e por isso as chamadas `_entryRepository.GetItem()` e `GetAll()` eram ignoradas como "chamadas à própria classe".

**Preparação necessária para reproduzir a coleta a partir do passo 04:**

1. `dotnet restore` em cada `.csproj` de `artefatos-mestrado`. Os projetos de p39 precisam de `-p:NuGetAudit=false`, porque tratam alertas de vulnerabilidade como erro. O restore só grava em `obj/`, que é ignorado pelo git, mas **atualiza `p34-arch-sn/src/AppHost/CashFlow.AppHost/CashFlow.AppHost.AppHost/packages.lock.json`**, que deve ser revertido com `git checkout` depois.
2. Referências usadas: Microsoft.NETCore.App.Ref 10.0.1 e Microsoft.AspNetCore.App.Ref 10.0.1, registradas no log de cada coleta.

**Limitações conhecidas:**

- p7 depende de pacotes privados do participante no GitHub Packages (`CashFlow.Domain.Shared`, `StreamTail`), que devolvem 401: 65% das chamadas de p7 se resolvem.
- `p4-arch-esp/src/CashFlow.Jobs` não restaura: usa gerenciamento central de pacotes sem declarar as versões (defeito do artefato).
- Pacotes com versões flutuantes podem resolver versões diferentes em datas diferentes; a data do restore desta coleta é 12/10/2026.

### Fase 4: Alinhamento às definições (≈3–5 dias)

> Plano detalhado, com as decisões pendentes e os exemplos de referência: [plano-correcao-metricas.md](plano-correcao-metricas.md).

1. Seguir a tabela 2.1 de `docs/PLANO_RECOLETA.md`, métrica por métrica, com os testes escritos primeiro:
   - exemplos do Cap. 2: DIT = 2, LOC lógica = 4, MC = 12, CLC = 6, CWC = 4, CoC = 4;
   - apêndice de Misra et al. (2018), portado para C#: MC(HARDWARE) = 16, CLC(HARDWARE) = 20, CWC(HARDWARE) = 9, CoC = 536.
2. Sempre que a decisão for "parametrizar", a variante antiga continua disponível por opção de linha de comando (ex.: `--mc-additive`).
3. Atualizar as seções "How this tool computes it" de `docs/*.md` (incluindo `docs/loc.md`, que ainda diz LOC lógica = 3) e `docs/research/descricao-ferramenta.md`.
4. **Gate G2:** `dotnet test` sem falhas e sem testes pulados.
5. **Gate G3:** LCOM1 e CBO calculados à mão em 5 classes reais de participantes diferentes, conferidos contra a ferramenta.
6. Revisão da tabela de decisões com o orientador. A versão final da ferramenta tem a tag `v1` (16/10/2026, ainda só local); se G3 ou a revisão mudarem a ferramenta, a tag é movida antes do push. A numeração v1/v2 das coletas é só controle interno das correções: a dissertação cita apenas a tag `v1`.

### Fase 5: Recoleta incremental (≈1 dia)

Em vez de uma única coleta v2, coletar um snapshot depois de cada grupo de commits, sempre sobre `artefatos-mestrado@40a6044`:

```
v1 → +escopo → +identidade/overloads → +V(G) → +semântica → +LCOM → +CBO/RFC → +DIT → +MC/AC/CWC/CoC = v2
```

1. Para cada snapshot: rodar a coleta ("Running Everything at Once" de `reproducing-dissertation.md`), `generate_index.py` e `cbtam_analysis.py`.
2. Guardar cada snapshot em `results-snapshots/<passo>/` e a análise em `analysis/<passo>/`.
3. **Gate G4:** 47 ou 48 participantes (conforme a decisão sobre p29/p33), nenhum CSV vazio, nº de classes e métodos coerente com a v1 menos o que os filtros removem.

### Fase 6: Comparação v1 × v2 (≈1–2 dias)

Gerar `analysis/comparacao-v1-v2.md` com:

- **Números publicados:** tabela com antes, depois e Δ para cada número do Cap. 4.
- **Nível de classe:** distribuição das diferenças pareadas por métrica (pela chave de `per_class.csv`) e correlação de Spearman entre v1 e v2, para mostrar se a métrica mudou de escala ou de ordenação.
- **Nível de participante:** matriz de transição entre o rótulo previsto na v1 e na v2, e quantos participantes mudaram de classificação.
- **Waterfall** da Fase 5: quanto da mudança em acurácia, ρ e redundância LCOM vem de cada passo. Isso separa o efeito de correção de defeito do efeito de mudança de definição.
- **Sensibilidades:** testes incluídos ou não, p33 incluído ou não, `record` incluído ou não.
- **Veredito** para cada conclusão da dissertação: **mantida**, **enfraquecida** ou **revertida**. As principais são:
  - LCOM redundante com WMC;
  - ordenação Expert < Mid/Sênior;
  - acurácia (51,1%) contra a linha de base;
  - ρ de Spearman (0,286).

### Fase 7: Atualização do texto (≈2–4 dias)

Igual à Etapa 7 de `docs/PLANO_RECOLETA.md`. Acrescentar ao protocolo do estudo de caso no Cap. 4 o tratamento de p29/p33, se a decisão mudar o n, e citar a tag `v1`.

Independentemente da recoleta, corrigir na versão defendida os achados da Fase 1: as erratas de `tab:per_participant_results` e a frase sobre DIT × NOC em `metric_redundancy` e em `experience_summary`.

## 4. Decisões de escopo

| Decisão | Opções | Recomendação | Decisão tomada |
|---|---|---|---|
| p29/p33 idênticos | Manter os dois; manter só um; investigar a origem | Investigar a origem; se for duplicata, manter um e rodar os dois como sensibilidade | **Manter os dois** (n = 48, como na v1); a duplicata é declarada no Cap. 4 e a sensibilidade sem p33 continua no relatório (15/10/2026) |
| Código de teste | Incluir; excluir | Excluir; incluir como sensibilidade | **Incluir**, como na v1 (15/10/2026) |
| Código gerado (`Migrations/`, `*.Designer.cs`, `obj/`, `bin/`) | Incluir; excluir | Excluir | **Incluir**, como na v1 (15/10/2026) |
| Classes parciais e homônimas | Consolidar no script; consolidar na ferramenta | Consolidar na ferramenta por FQN | **Na ferramenta**, por projeto + nome completo (`bf45bac`) |
| Tipos analisados | `class`; + `record class`; + `struct`/`record struct` | `class` + `record class` | **`class`, `record`, `struct` e `record struct`** (INC-13, D19, `d1cab02`); `--classes-only` reproduz a v1 |
| Propriedades com lógica e funções locais | Ignorar; contar como métodos | _a discutir_ | **Decidido pelas INCs:** acessores com corpo entram na V(G) agregada (D18, INC-04), mas não no WMC; lambdas e funções locais somam na V(G) e na MC do método que as contém (D21, D11; INC-04, INC-17) |
| Construtores em WMC e LCOM | Incluir; excluir; parametrizar | Parametrizar | **WMC inclui** (D3, INC-05; `--wmc-exclude-static` reproduz a v1); **LCOM exclui**, sem opção (D4, INC-09) |
| Destino dos resultados | Versão final depositada; artigo + errata | Depende do prazo pós-defesa | _pendente_ |

## 5. Gates de validação

| Gate | Critério | Fase |
|---|---|---|
| G0 | Coleta com `0bc0f89` idêntica a `results-v1/` — **aprovado em 12/10/2026** | 0 |
| G1 | `cbtam_analysis.py` sobre `results-v1/` reproduz os números da versão defendida — **aprovado em 12/10/2026** (com erratas da tabela por participante documentadas) | 1 |
| G2 | `dotnet test` sem falhas e sem testes pulados, cobrindo os exemplos do Cap. 2 e de Misra (CoC = 536) | 4 |
| G3 | LCOM1 e CBO à mão em 5 classes batem com a ferramenta | 4 |
| G4 | Contagens da v2 plausíveis frente à v1 e aos filtros | 5 |
| G5 | `grep` em `master-thesis` pelos números antigos (51,1, 0,286, 99,40, 2.657) sem ocorrências esquecidas; PDF compila sem referências indefinidas | 7 |

## 6. Estimativa

Cerca de 13 a 20 dias de trabalho, a partir de 27/11/2026. O aumento em relação ao plano original vem da migração para MSBuildWorkspace (achado E) e da recoleta incremental. A Fase 0 e a investigação de p29/p33 podem ser feitas antes da defesa, porque não alteram a ferramenta.
