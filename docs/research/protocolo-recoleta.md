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
| I | O estado do repositório impede congelar a linha de base | `tests/` untracked; `RootCommandHandler.cs` modificado | Versionar antes de criar qualquer tag |

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

1. Criar `scripts/analysis/cbtam_analysis.py` (pandas/scipy), que recebe `--results <dir>` e gera `analysis/<versão>/` com CSVs e `report.md`, cobrindo todas as saídas listadas na Etapa 4 do plano original.
2. Consolidar as classes por (participante, nome), exatamente como no Cap. 4.
3. Gerar também `per_class.csv` com uma chave estável (participante + arquivo + classe), que a Fase 6 usa no pareamento.
4. **Gate G1:** rodando sobre `results-v1/`, o script tem que reproduzir 2.657 classes, 6.839 métodos, 99,40%, 51,1% e ρ = 0,286, além das tabelas do Cap. 4. Se não reproduzir, o script ainda não é confiável.

### Fase 2: Decisões de escopo (≈0,5 dia)

Preencher a seção 4 deste documento antes de alterar a ferramenta.

### Fase 3: Infraestrutura e correções de defeito (≈2–3 dias)

São erros de implementação, não mudanças de definição. Cada item tem seu commit e seu teste, escrito antes da correção:

1. Identidade da classe por símbolo (FQN), com `partial` consolidado na própria ferramenta (achado B).
2. Chave de método pela assinatura completa (achado C).
3. `else if` na V(G); reativar `ElseIf_CountsOnce` (achado D).
4. Compilação com referências reais via MSBuildWorkspace (achado E).
5. Filtros de escopo decididos na Fase 2 (testes, código gerado, tipos analisados).

### Fase 4: Alinhamento às definições (≈3–5 dias)

1. Seguir a tabela 2.1 de `docs/PLANO_RECOLETA.md`, métrica por métrica, com os testes escritos primeiro:
   - exemplos do Cap. 2: DIT = 2, LOC lógica = 4, MC = 12, CLC = 6, CWC = 4, CoC = 4;
   - apêndice de Misra et al. (2018), portado para C#: MC(HARDWARE) = 16, CLC(HARDWARE) = 20, CWC(HARDWARE) = 9, CoC = 536.
2. Sempre que a decisão for "parametrizar", a variante antiga continua disponível por opção de linha de comando (ex.: `--mc-additive`).
3. Atualizar as seções "How this tool computes it" de `docs/*.md` (incluindo `docs/loc.md`, que ainda diz LOC lógica = 3) e `docs/research/descricao-ferramenta.md`.
4. **Gate G2:** `dotnet test` sem falhas e sem testes pulados.
5. **Gate G3:** LCOM1 e CBO calculados à mão em 5 classes reais de participantes diferentes, conferidos contra a ferramenta.
6. Revisão da tabela de decisões com o orientador; depois, tag `coleta-v2`.

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

Igual à Etapa 7 de `docs/PLANO_RECOLETA.md`. Acrescentar ao protocolo do estudo de caso no Cap. 4 o tratamento de p29/p33, se a decisão mudar o n, e citar a tag `coleta-v2`.

## 4. Decisões de escopo

| Decisão | Opções | Recomendação | Decisão tomada |
|---|---|---|---|
| p29/p33 idênticos | Manter os dois; manter só um; investigar a origem | Investigar a origem; se for duplicata, manter um e rodar os dois como sensibilidade | _pendente_ |
| Código de teste | Incluir; excluir | Excluir; incluir como sensibilidade | _pendente_ |
| Código gerado (`Migrations/`, `*.Designer.cs`, `obj/`, `bin/`) | Incluir; excluir | Excluir | _pendente_ |
| Classes parciais e homônimas | Consolidar no script; consolidar na ferramenta | Consolidar na ferramenta por FQN | _pendente_ |
| Tipos analisados | `class`; + `record class`; + `struct`/`record struct` | `class` + `record class` | _pendente_ |
| Propriedades com lógica e funções locais | Ignorar; contar como métodos | _a discutir_ | _pendente_ |
| Construtores em WMC e LCOM | Incluir; excluir; parametrizar | Parametrizar | _pendente_ |
| Destino dos resultados | Versão final depositada; artigo + errata | Depende do prazo pós-defesa | _pendente_ |

## 5. Gates de validação

| Gate | Critério | Fase |
|---|---|---|
| G0 | Coleta com `0bc0f89` idêntica a `results/` (sem a coluna de caminho) | 0 |
| G1 | `cbtam_analysis.py` sobre `results-v1/` reproduz os números da versão defendida | 1 |
| G2 | `dotnet test` sem falhas e sem testes pulados, cobrindo os exemplos do Cap. 2 e de Misra (CoC = 536) | 4 |
| G3 | LCOM1 e CBO à mão em 5 classes batem com a ferramenta | 4 |
| G4 | Contagens da v2 plausíveis frente à v1 e aos filtros | 5 |
| G5 | `grep` em `master-thesis` pelos números antigos (51,1, 0,286, 99,40, 2.657) sem ocorrências esquecidas; PDF compila sem referências indefinidas | 7 |

## 6. Estimativa

Cerca de 13 a 20 dias de trabalho, a partir de 27/11/2026. O aumento em relação ao plano original vem da migração para MSBuildWorkspace (achado E) e da recoleta incremental. A Fase 0 e a investigação de p29/p33 podem ser feitas antes da defesa, porque não alteram a ferramenta.
