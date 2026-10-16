# Codimetry: descrição da ferramenta para a dissertação

> Este documento reúne o material de apoio para descrever, na dissertação, a ferramenta construída para extrair as métricas do estudo de caso. Ele está organizado em quatro partes:
>
> 1. Motivação, ou seja, por que foi preciso construir uma ferramenta.
> 2. Descrição técnica.
> 3. Pontos de atenção, que precisam ficar coerentes com o texto da dissertação.
> 4. Parágrafos prontos para adaptar, ao final.

---

## 1. Motivação: a escassez de ferramentas abertas para o ecossistema .NET

O estudo de caso da dissertação analisa artefatos .NET/C# de 48 desenvolvedores. Para isso, ele precisa de três conjuntos de métricas em nível de classe e de método, coletadas de forma **automatizada, reprodutível e com definição conhecida**:

- as seis métricas CK (WMC, RFC, LCOM, CBO, DIT, NOC);
- as cinco métricas de complexidade cognitiva de Misra et al. (2018) (MC, AC, CLC, CWC, CoC);
- complexidade ciclomática e LOC.

Nenhuma ferramenta disponível para C# atendia a esses três requisitos ao mesmo tempo:

| Ferramenta | Ecossistema | Licença | Métricas relevantes | Limitação para o estudo |
|---|---|---|---|---|
| *ck* (Aniche) | Java | Aberta | Suíte CK completa | Não analisa C# |
| Code Metrics do Visual Studio / pacote `Microsoft.CodeAnalysis.Metrics` | .NET | Aberta (pacote) / IDE proprietária | Índice de manutenibilidade, complexidade ciclomática, profundidade de herança, acoplamento de classes, LOC | Não oferece WMC, RFC, LCOM nem NOC; não oferece as métricas de Misra |
| SonarQube / SonarAnalyzer for C# | .NET (multilinguagem) | Analisador aberto; plataforma com edições comerciais | Complexidade ciclomática e cognitiva (Campbell) | Não implementa a suíte CK nem a de Misra; orientado a regras e *quality gates*, não a exportar métricas por classe |
| NDepend | .NET | Comercial | Ampla cobertura de métricas OO, incluindo variantes de LCOM | Proprietária e paga; as definições internas não podem ser inspecionadas nem alteradas |
| Understand (SciTools) | Multilinguagem, incluindo C# | Comercial | Métricas OO incluindo CK | Proprietária e paga |

Em síntese, as ferramentas abertas para .NET cobrem só parte das métricas CK e nenhuma das métricas de Misra. As ferramentas mais completas são comerciais e de código fechado. Isso impede verificar **qual formulação** de cada métrica é usada, um ponto crítico porque a própria literatura registra várias variantes concorrentes, por exemplo LCOM1 a LCOM5 e a escolha de $c_i$ no WMC. Por isso, decidiu-se construir uma ferramenta própria, aberta e documentada.

> ⚠️ **Antes de citar:** confira a tabela acima contra a documentação atual de cada ferramenta e acrescente as referências (sites e versões consultadas, com data de acesso). O conteúdo reflete o conhecimento geral sobre essas ferramentas e não foi verificado nesta sessão.

---

## 2. Descrição técnica

| Item | Valor |
|---|---|
| Nome | Codimetry (*code* + *-metry*, a medição de código); até 16/10/2026, MetricsExporter.Net |
| Repositório | https://github.com/jlfjunior/codimetry |
| Licença | MIT |
| Plataforma | .NET 10, aplicação de linha de comando multiplataforma (Windows, Linux, macOS) |
| Análise de código | Roslyn (`Microsoft.CodeAnalysis.CSharp` 4.9.2): árvore sintática e modelo semântico, com uma compilação por projeto |
| Tamanho | 37 arquivos C#, 6.550 linhas físicas e 2.303 linhas lógicas (medidas pela própria ferramenta no commit `0ff7d31`, sem os arquivos gerados em `obj/`) |
| Desenvolvimento | Iniciado em novembro de 2025 |

### Funcionamento

1. **Entrada.** A ferramenta aceita um diretório (`--path`, com busca recursiva), uma *solution* (`--solution`) ou um projeto (`--project`).
2. **Análise.** Os arquivos `.cs` são agrupados pelo `.csproj` mais próximo, e cada projeto vira uma compilação Roslyn com as referências do .NET, os *usings* implícitos do SDK, os pacotes NuGet restaurados e as referências entre projetos (o MSBuild não é executado). Cada arquivo é percorrido por *visitors* especializados, um por família de métricas. O modelo semântico identifica cada tipo e cada método chamado pelo símbolo, não pelo nome, de modo que classes homônimas e sobrecargas ficam separadas. O `run.log` registra a regra usada para cada métrica e a taxa de chamadas e criações de objeto que o modelo semântico resolveu.
3. **Cálculo em duas fases para métricas relacionais.** As métricas que dependem de relações entre classes (DIT, NOC, CBO, RFC, CWC, CoC) e o MC, que soma o MC dos métodos chamados, são calculados em duas etapas. Primeiro, a ferramenta monta um grafo com todas as classes, suas relações de herança e as chamadas entre métodos. Depois, calcula as métricas consultando esse grafo.
4. **Saída.** A ferramenta gera um arquivo CSV por família de métricas, em formato tabular pronto para análise estatística (Python, R, planilhas):

   | Arquivo | Métricas | Granularidade |
   |---|---|---|
   | `metrics_loc.csv` | LOC | método, classe, arquivo |
   | `metrics_cyclomatic.csv` | Complexidade ciclomática | método, classe, arquivo |
   | `metrics_ck.csv` | WMC, DIT, NOC, CBO, RFC, LCOM | classe |
   | `metrics_misra.csv` | MC, AC, CLC, CWC, CoC | método, classe, sistema |
   | `metrics_coc.csv` | CoC | hierarquia de herança |

### Arquitetura

A arquitetura é organizada em camadas simples:

- `Parsers`: leitura de arquivos, *solutions* e projetos, e *visitors* Roslyn que extraem as informações sintáticas;
- `Services`: cálculo das métricas e orquestração da análise;
- `Exporters`: geração dos CSV;
- `CLI`: interface de linha de comando.

Para acrescentar uma nova métrica, basta criar um *visitor*, uma calculadora e um exportador, sem alterar o restante da ferramenta.

### Documentação

Cada métrica tem uma página no repositório (`docs/loc.md`, `docs/cyclomatic.md`, `docs/ck.md`, `docs/misra.md`). Cada página traz a definição formal, um exemplo em C# e uma seção que descreve **exatamente como a ferramenta calcula o valor**. Essa transparência foi um dos motivos para construir a ferramenta.

---

## 3. Pontos de atenção para a dissertação

Os itens abaixo afetam o que a dissertação pode afirmar sobre a ferramenta e sobre os resultados. Verifique cada um antes de fechar o texto.

1. **Formulações adotadas.** O texto da dissertação deve descrever o que a ferramenta efetivamente calcula:
   - **Unidade de análise** (INC-13, D19): "classe" inclui `record`, `record struct` e `struct`. Records herdam como classes (entram em DIT, NOC e CoC); structs têm DIT = NOC = 0. Declare isso no Cap. 4 e, se relevante, a contagem por `Kind`. `--classes-only` reproduz o escopo da v1.
   - **V(G)** segue a lista de pontos de decisão da §2.4 (INC-02, INC-03): `default` e o padrão curinga (`_`, `var x` sem `when`) não contam, como o `else`; `goto` não conta; `??`, `??=` e `?.` só contam com `--cc-null-operators` (D16). Duas extensões precisam ser declaradas na §2.4: os combinadores de padrão `or`/`and` contam como `||`/`&&` (D17, C# 9+), e `&&`/`||` contam em qualquer expressão, não só "dentro de uma expressão condicional". `--cc-legacy-operators` reproduz a regra de operadores da v1.
   - **V(G) agregada** (INC-04, D18, D21): a soma por classe e por projeto cobre métodos, construtores, destrutores, operadores e acessores **com corpo**; acessores automáticos não entram. Lambdas e funções locais somam no método que as contém. Declare no Cap. 4 que o total do projeto exclui acessores automáticos; `--cc-include-auto-accessors` reproduz a soma da v1. O WMC com cᵢ = V(G) continua só sobre métodos e construtores.
   - **WMC** usa $c_i = 1$ por padrão e $c_i = V(G)$ com `--wmc-weight cyclomatic` (D2, INC-05). Entram os métodos e construtores declarados na classe, **estáticos inclusive** (D3); acessores de propriedade não entram. A §2.5.1 afirma que se adota a complexidade ciclomática como $c_i$: declare no Cap. 4 qual variante foi usada e que estáticos e construtores entram. `--wmc-exclude-static` reproduz a v1.
   - **LCOM** usa a formulação original LCOM1, o que é coerente com a §2.5.3, sobre os **métodos de instância** da classe, **sem construtores** (D4, INC-09). A §2.5.3 fala em "pares de métodos da classe" sem declarar a escolha: acrescente que os construtores ficam de fora porque costumam inicializar todos os campos e, por isso, compartilham atributo com quase todo método, o que reduziria a LCOM independentemente da coesão. Cite a referência que justifica a exclusão (ainda a conferir; ver §2 do INC-09).
   - **CBO** (INC-10, INC-11, D8, D9) conta os **outros tipos do código analisado** que a classe usa, por qualquer referência de tipo: herança, interfaces implementadas, campos, propriedades, eventos, parâmetros (inclusive do construtor primário), retornos, variáveis locais, `new`, `typeof`, conversões, padrões, `catch`, atributos, argumentos genéricos, receptores de membros estáticos e o uso de métodos e atributos declarados em outro tipo. Classes, records, structs e **interfaces** do código são alvos (D8: em .NET, a injeção de dependência acopla por interface); tipos do framework e de pacotes não contam, nem membros herdados dos ancestores da própria classe. Os tipos são identificados pelo símbolo, de modo que `Task` não casa com `BackgroundTask` (defeito da v1). Por padrão, só o acoplamento de **saída** (D9); `--cbo-bidirectional` soma os tipos que usam a classe, como em Chidamber e Kemerer (1994). Declare no Cap. 2 a direção e a inclusão de interfaces. `--cbo-declared-types-only` reproduz a regra da v1 (campos, propriedades e assinaturas de métodos), sem o casamento por sufixo.
   - **RFC** (INC-06, D20): $M$ são os métodos e construtores declarados na classe; $R$, os métodos e **construtores** chamados diretamente por eles e que não estão em $M$, identificados pelo símbolo (sobrecargas são métodos distintos). `new X()`, `new()` e `: base(...)` entram em $R$ pelo construtor chamado, declarado ou implícito; acessores de propriedade não entram, porque em C# são acessos a atributo. `--rfc-exclude-constructors` reproduz a v1.
   - **DIT** conta as arestas de herança **declaradas** (INC-12, D7a): `System.Object` não conta, e classe sem base explícita tem DIT = 0, como diz a §2.5.5 ("`System.Object` não é contabilizada"). Uma base externa (`Exception`, `ControllerBase`) conta 1. A nota de rodapé do Cap. 2 ("valores uma unidade maiores") deve ser removida, e os DIT do Cap. 4 recalculados: as faixas e o `dit_eq_1_pct` mudam. `--dit-resolve-framework` conta também os ancestrais das bases externas (D7b); `--dit-count-object` reproduz a v1.
   - **MC** usa os **pesos cognitivos das estruturas básicas de controle** (sequência 1, if-then-else e ternário 2, *case* — `switch` e `switch` por expressão — 3, iteração 3; Wang e Shao, 2003), compostos pela Eq. 23 de Misra et al. (soma na camada, produto no aninhamento; INC-14). `else if` é um `if` aninhado no `else` (D12); a instrução que contém um ternário é o próprio if-then-else, sem sequência a mais (D23, INC-15). **Chamadas** (INC-16, D10, D14, D20): só métodos do código analisado somam 2 + MC do chamado; recursão direta ou mútua soma 2; chamada via interface ou método abstrato usa o MC da implementação única (senão 2 + 1); chamadas de biblioteca não somam (`--mc-library-call-weight` muda isso); `new X()` não é chamada. Explicite no Cap. 2 que só métodos do sistema somam 2 + MC do chamado. **Lambdas e funções locais** (INC-17, D11): o corpo é uma camada aninhada onde está escrito (peso 1 × camada), somada às estruturas da camada; chamada à função local não soma. É uma adaptação da Eq. 23 para C# e precisa ser declarada no Cap. 2; `--mc-lambdas calls|ignore` dá as alternativas. A §2.6.1 descreve o MC pelas regras de Campbell, com incremento por aninhamento: é preciso alinhar as duas descrições.
   - **AC** conta 1 por atributo de instância, qualquer que seja o tipo, como a §2.6.2 (INC-18); `static` e `const` não contam. `--ac-type-weights` reproduz a ponderação por tipo da v1.
   - **CWC** segue a Eq. 24 (INC-19, D13, D14): soma 2 + MC do método chamado para cada chamada a método de **outro tipo do código analisado**, inclusive métodos herdados de uma classe base; via interface ou método abstrato, o MC da implementação única (senão 2 + 1). Chamadas a bibliotecas e delegados não contam, nem as não resolvidas (registradas no log). Explicite no Cap. 2 "outras classes do sistema". `--cwc-library-weight 3` reproduz a regra de biblioteca da v1.
   - **CoC** (INC-20, D1) segue a hierarquia de herança: por padrão, $CoC(c) = CLC(c)$ para uma classe sem subclasses e $CLC(c) \times \sum CoC(\text{subclasses})$ nas demais; a CoC do sistema é a soma das CoC das raízes (classes cuja base não está no código, como na DIT). É a regra que reproduz o exemplo de Misra et al. (2018), CoC = 536. A Eq. 27 lida nível a nível daria 816 no mesmo exemplo; `--coc-formula levels` usa essa leitura. A §2.6.5 precisa registrar essa contradição e a fórmula adotada. A CWC não entra. O valor sai na coluna `CoC` (a coluna `CC` repete o valor por uma versão), e `metrics_coc.csv` traz a CoC de cada hierarquia. Com CLC na casa das centenas e hierarquias de 3–4 níveis, a CoC chega a $10^8$–$10^{10}$: compare participantes em escala logarítmica e declare isso no Cap. 4. `--coc-legacy` reproduz a v1 ($\sum CLC + \sum CWC / 2$, sem herança).
2. **Correções que alteram resultados.** Depois da coleta, foram corrigidos defeitos no cálculo do **LCOM** (commit `054fc08`) e do **MC/CWC**: estruturas aninhadas eram ignoradas e chamadas internas contavam como acoplamento. Duas consequências:
   - A conclusão de que "a métrica LCOM mostrou-se, para a ferramenta de extração utilizada, redundante com a métrica WMC" (Resumo; §4.3.2) provavelmente **decorre do defeito**. Antes da correção, o LCOM tendia a $n(n-1)/2$, uma função direta do número de métodos.
   - Recoletar os dados com a versão corrigida e atualizar os resultados é o caminho mais sólido. Se isso não for possível, registre o defeito como ameaça à validade de construto (§4.4.1) e informe o commit usado na coleta.
3. **Validação.** A ferramenta tem uma suíte de **testes automatizados** (xUnit, em `tests/Codimetry.Tests`), com 410 casos, 94% de cobertura de linhas e 89% de ramos (coverlet, commit `0ff7d31`). A suíte inclui:
   - testes unitários de cada parser e calculadora;
   - testes de integração que executam a ferramenta sobre os exemplos de código da documentação (`docs/*.md`), do Cap. 2 e do artigo de Misra et al. (2018), e conferem os valores esperados;
   - um teste de aceite por critério de cada spec de correção (`docs/research/specs/INC-*.md`), incluindo as opções que reproduzem a v1;
   - testes de regressão para os defeitos de LCOM e MC/CWC citados no item 2. Foi verificado que eles falham com as versões defeituosas.

   **Atenção:** a suíte foi criada **depois** da coleta de dados do estudo de caso. Se os dados não forem recoletados, a dissertação não pode afirmar que a coleta foi feita com uma versão testada. Nenhum caso fica ignorado: o defeito do `else if` contado em dobro na complexidade ciclomática foi corrigido (commit `df14394`).
4. **Escopo da análise.** A análise é **estática**, com modelo semântico:
   - chamadas e tipos são identificados pelo símbolo; o que o modelo semântico não resolve (referência ausente, código que não compila) cai para o nome simples exato e é contado no `run.log`;
   - `class`, `record`, `record struct` e `struct` são medidos (coluna `Kind`); interfaces não geram linhas, mas são alvos de acoplamento do CBO;
   - CBO, RFC, CWC e CoC consideram apenas os tipos declarados no código analisado; chamadas a bibliotecas entram na RFC, mas não na CWC nem no MC (salvo pelas opções de compatibilidade);
   - o que só existe em tempo de execução (reflexão, injeção de dependência configurada por convenção, despacho virtual além da implementação única) não é visto.

---

## 4. Parágrafos sugeridos

### Versão curta (um parágrafo)

> Diante da escassez de ferramentas de código aberto capazes de extrair, para o ecossistema .NET, o conjunto completo de métricas adotado neste trabalho, foi desenvolvida a ferramenta **Codimetry**, disponibilizada publicamente sob licença MIT[^repo]. As alternativas identificadas ou cobrem apenas parte da suíte CK — caso do pacote de métricas de código da Microsoft, que não fornece WMC, RFC, LCOM e NOC —, ou não implementam as métricas de complexidade cognitiva de Misra et al. (MISRA et al., 2018), ou ainda são produtos comerciais de código fechado, o que impede verificar qual formulação de cada métrica é efetivamente calculada — aspecto crítico diante das múltiplas variantes registradas na literatura, como as de LCOM. A ferramenta, implementada em .NET 10 sobre a plataforma de compilação Roslyn, percorre a árvore sintática de diretórios, *solutions* ou projetos C# e exporta, em arquivos CSV, as métricas de linhas de código, complexidade ciclomática (MCCABE, 1976), CK (CHIDAMBER; KEMERER, 1994) e Misra, nos níveis de método, classe, arquivo e sistema, acompanhadas de documentação que descreve a formulação adotada para cada métrica.

[^repo]: Disponível em: <https://github.com/jlfjunior/codimetry>. Acesso em: [data].

### Versão estendida (para uma seção ou subseção de "Ferramentas")

> A coleta das métricas exigiu uma ferramenta capaz de extrair, de artefatos C#, as métricas de tamanho (LOC), de complexidade ciclomática, a suíte CK e a suíte de complexidade cognitiva de Misra et al. (2018), de forma automatizada e reprodutível. No ecossistema Java, ferramentas abertas como a *ck* cumprem esse papel para a suíte CK; no ecossistema .NET, contudo, não foi identificada alternativa equivalente: as métricas de código da Microsoft oferecem apenas índice de manutenibilidade, complexidade ciclomática, profundidade de herança, acoplamento e linhas de código; o SonarQube concentra-se em complexidade ciclomática e cognitiva, sem as suítes CK e Misra; e ferramentas abrangentes como o NDepend são comerciais e de código fechado, impossibilitando a inspeção das formulações utilizadas.
>
> Para suprir essa lacuna, foi desenvolvida a ferramenta de linha de comando **Codimetry**, de código aberto (licença MIT). Implementada em .NET 10, a ferramenta utiliza a plataforma de compilação Roslyn para construir a árvore sintática de cada arquivo-fonte e percorrê-la por meio de *visitors* especializados, um por família de métricas. As métricas que dependem de relações entre classes (DIT, NOC, CBO e CWC) são calculadas em duas fases — construção de um grafo de classes e posterior cálculo sobre esse grafo —, e os resultados são exportados em arquivos CSV nos níveis de método, classe, arquivo e sistema, prontos para análise estatística. Para identificar tipos e métodos pelo símbolo, e não pelo nome, a ferramenta monta uma compilação por projeto, com as referências do .NET e dos pacotes NuGet. O repositório documenta, para cada métrica, sua definição formal, um exemplo em C# e a formulação efetivamente implementada — por exemplo, WMC com peso unitário por método, LCOM em sua formulação original (LCOM1), MC composto pelos pesos cognitivos das estruturas básicas de controle e CoC calculado por subárvore da hierarquia de herança —, de modo a permitir a replicação dos resultados e a comparação com outros estudos.
>
> A corretude da ferramenta é verificada por uma suíte de testes automatizados (xUnit), que cobre 94% das linhas de código e inclui testes de integração que executam a ferramenta sobre os exemplos de código documentados — como os apresentados no Capítulo 2 e o exemplo de Misra et al. (2018) — e conferem os valores esperados para cada métrica; os limites da análise estática, como chamadas que o modelo semântico não resolve e o comportamento que só existe em tempo de execução, são discutidos como ameaça à validade na Seção 4.4.

---

## Referências citadas neste documento

- CHIDAMBER, S. R.; KEMERER, C. F. A metrics suite for object oriented design. *IEEE Transactions on Software Engineering*, v. 20, n. 6, p. 476–493, 1994.
- MCCABE, T. J. A complexity measure. *IEEE Transactions on Software Engineering*, n. 4, p. 308–320, 1976.
- MISRA, S. et al. A suite of object oriented cognitive complexity metrics. *IEEE Access*, v. 6, p. 8782–8796, 2018.
- WANG, Y.; SHAO, J. A new measure of software complexity based on cognitive weights. *Canadian Journal of Electrical and Computer Engineering*, v. 28, n. 2, p. 69–74, 2003.
- ANICHE, M. *Java code metrics calculator (CK)*. Disponível em: <https://github.com/mauricioaniche/ck>.
