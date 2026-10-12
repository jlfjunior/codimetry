# MetricsExporter.Net: descrição da ferramenta para a dissertação

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
| Nome | MetricsExporter.Net |
| Repositório | https://github.com/jlfjunior/net-metrics-collector |
| Licença | MIT |
| Plataforma | .NET 10, aplicação de linha de comando multiplataforma (Windows, Linux, macOS) |
| Análise de código | Roslyn (`Microsoft.CodeAnalysis.CSharp` 4.9.2), por meio da árvore sintática, com modelo semântico usado pontualmente |
| Tamanho | 24 arquivos C#, cerca de 2,1 mil linhas de código em métodos (medidas pela própria ferramenta) |
| Desenvolvimento | Iniciado em novembro de 2025 |

### Funcionamento

1. **Entrada.** A ferramenta aceita um diretório (`--path`, com busca recursiva), uma *solution* (`--solution`) ou um projeto (`--project`).
2. **Análise.** Cada arquivo `.cs` é convertido em árvore sintática pelo Roslyn e percorrido por *visitors* especializados, um por família de métricas. Os arquivos são processados em paralelo.
3. **Cálculo em duas fases para métricas relacionais.** As métricas que dependem de relações entre classes (DIT, NOC, CBO, CWC) são calculadas em duas etapas. Primeiro, a ferramenta monta um grafo com todas as classes e suas relações de herança e dependência. Depois, calcula as métricas consultando esse grafo.
4. **Saída.** A ferramenta gera um arquivo CSV por família de métricas, em formato tabular pronto para análise estatística (Python, R, planilhas):

   | Arquivo | Métricas | Granularidade |
   |---|---|---|
   | `metrics_loc.csv` | LOC | método, classe, arquivo |
   | `metrics_cyclomatic.csv` | Complexidade ciclomática | método, classe, arquivo |
   | `metrics_ck.csv` | WMC, DIT, NOC, CBO, RFC, LCOM | classe |
   | `metrics_misra.csv` | MC, AC, CLC, CWC, CoC | método, classe, sistema |

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
   - **WMC** usa $c_i = 1$ (contagem de métodos não estáticos e construtores). A §2.5.1 afirma que se adota a complexidade ciclomática como $c_i$. Ajuste o texto ou use a soma por classe de `metrics_cyclomatic.csv`.
   - **LCOM** usa a formulação original LCOM1, o que é coerente com a §2.5.3.
   - **DIT** conta `System.Object` como um nível, o que é coerente com o exemplo da §2.5.5 (DIT = 3).
   - **MC** usa os **pesos cognitivos das estruturas básicas de controle** (sequência 1, desvio 2, iteração 3, chamada 2; Wang e Shao, 2003), somados **sem incremento por aninhamento**. A §2.6.1 descreve o MC pelas regras de Campbell, com incremento por aninhamento. É preciso alinhar as duas descrições.
   - **AC** pondera os atributos por tipo (1 para tipos simples, 2 para os demais). A §2.6.2 conta 1 por atributo.
   - **CWC** soma (MC do método chamado + 2) para cada chamada externa. Chamadas a bibliotecas recebem o valor fixo 3.
   - **CoC** (coluna `CC` do CSV) é calculado como $\sum CLC + \sum CWC / 2$ e **não** considera a hierarquia de herança. A §2.6.5 descreve uma combinação multiplicativa entre os níveis da hierarquia.
2. **Correções que alteram resultados.** Depois da coleta, foram corrigidos defeitos no cálculo do **LCOM** (commit `054fc08`) e do **MC/CWC**: estruturas aninhadas eram ignoradas e chamadas internas contavam como acoplamento. Duas consequências:
   - A conclusão de que "a métrica LCOM mostrou-se, para a ferramenta de extração utilizada, redundante com a métrica WMC" (Resumo; §4.3.2) provavelmente **decorre do defeito**. Antes da correção, o LCOM tendia a $n(n-1)/2$, uma função direta do número de métodos.
   - Recoletar os dados com a versão corrigida e atualizar os resultados é o caminho mais sólido. Se isso não for possível, registre o defeito como ameaça à validade de construto (§4.4.1) e informe o commit usado na coleta.
3. **Validação.** A ferramenta tem uma suíte de **testes automatizados** (xUnit, em `tests/MetricsExporter.Net.Tests`), com 141 casos e cerca de 92% de cobertura de linhas. A suíte inclui:
   - testes unitários de cada parser e calculadora;
   - testes de integração que executam a ferramenta sobre os exemplos de código da documentação e conferem os valores esperados;
   - testes de regressão para os defeitos de LCOM e MC/CWC citados no item 2. Foi verificado que eles falham com as versões defeituosas.

   **Atenção:** a suíte foi criada **depois** da coleta de dados do estudo de caso. Se os dados não forem recoletados, a dissertação não pode afirmar que a coleta foi feita com uma versão testada. Um caso de teste fica marcado como ignorado: ele documenta um defeito conhecido na complexidade ciclomática, em que `else if` é contado em dobro.
4. **Escopo da análise.** A análise é essencialmente **sintática**:
   - chamadas e tipos são associados pelo nome simples;
   - somente declarações `class` são medidas, ou seja, `record`, `struct` e `interface` não geram linhas;
   - o CBO considera apenas tipos declarados no código analisado.

---

## 4. Parágrafos sugeridos

### Versão curta (um parágrafo)

> Diante da escassez de ferramentas de código aberto capazes de extrair, para o ecossistema .NET, o conjunto completo de métricas adotado neste trabalho, foi desenvolvida a ferramenta **MetricsExporter.Net**, disponibilizada publicamente sob licença MIT[^repo]. As alternativas identificadas ou cobrem apenas parte da suíte CK — caso do pacote de métricas de código da Microsoft, que não fornece WMC, RFC, LCOM e NOC —, ou não implementam as métricas de complexidade cognitiva de Misra et al. (MISRA et al., 2018), ou ainda são produtos comerciais de código fechado, o que impede verificar qual formulação de cada métrica é efetivamente calculada — aspecto crítico diante das múltiplas variantes registradas na literatura, como as de LCOM. A ferramenta, implementada em .NET 10 sobre a plataforma de compilação Roslyn, percorre a árvore sintática de diretórios, *solutions* ou projetos C# e exporta, em arquivos CSV, as métricas de linhas de código, complexidade ciclomática (MCCABE, 1976), CK (CHIDAMBER; KEMERER, 1994) e Misra, nos níveis de método, classe, arquivo e sistema, acompanhadas de documentação que descreve a formulação adotada para cada métrica.

[^repo]: Disponível em: <https://github.com/jlfjunior/net-metrics-collector>. Acesso em: [data].

### Versão estendida (para uma seção ou subseção de "Ferramentas")

> A coleta das métricas exigiu uma ferramenta capaz de extrair, de artefatos C#, as métricas de tamanho (LOC), de complexidade ciclomática, a suíte CK e a suíte de complexidade cognitiva de Misra et al. (2018), de forma automatizada e reprodutível. No ecossistema Java, ferramentas abertas como a *ck* cumprem esse papel para a suíte CK; no ecossistema .NET, contudo, não foi identificada alternativa equivalente: as métricas de código da Microsoft oferecem apenas índice de manutenibilidade, complexidade ciclomática, profundidade de herança, acoplamento e linhas de código; o SonarQube concentra-se em complexidade ciclomática e cognitiva, sem as suítes CK e Misra; e ferramentas abrangentes como o NDepend são comerciais e de código fechado, impossibilitando a inspeção das formulações utilizadas.
>
> Para suprir essa lacuna, foi desenvolvida a ferramenta de linha de comando **MetricsExporter.Net**, de código aberto (licença MIT). Implementada em .NET 10, a ferramenta utiliza a plataforma de compilação Roslyn para construir a árvore sintática de cada arquivo-fonte e percorrê-la por meio de *visitors* especializados, um por família de métricas. As métricas que dependem de relações entre classes (DIT, NOC, CBO e CWC) são calculadas em duas fases — construção de um grafo de classes e posterior cálculo sobre esse grafo —, e os resultados são exportados em arquivos CSV nos níveis de método, classe, arquivo e sistema, prontos para análise estatística. O repositório documenta, para cada métrica, sua definição formal, um exemplo em C# e a formulação efetivamente implementada — por exemplo, WMC com peso unitário por método, LCOM em sua formulação original (LCOM1) e MC baseado nos pesos cognitivos das estruturas básicas de controle —, de modo a permitir a replicação dos resultados e a comparação com outros estudos.
>
> A corretude da ferramenta é verificada por uma suíte de testes automatizados (xUnit), que cobre cerca de 92% das linhas de código e inclui testes de integração que executam a ferramenta sobre os exemplos de código documentados — como os apresentados no Capítulo 2 — e conferem os valores esperados para cada métrica; a natureza essencialmente sintática da análise é discutida como ameaça à validade na Seção 4.4.

---

## Referências citadas neste documento

- CHIDAMBER, S. R.; KEMERER, C. F. A metrics suite for object oriented design. *IEEE Transactions on Software Engineering*, v. 20, n. 6, p. 476–493, 1994.
- MCCABE, T. J. A complexity measure. *IEEE Transactions on Software Engineering*, n. 4, p. 308–320, 1976.
- MISRA, S. et al. A suite of object oriented cognitive complexity metrics. *IEEE Access*, v. 6, p. 8782–8796, 2018.
- WANG, Y.; SHAO, J. A new measure of software complexity based on cognitive weights. *Canadian Journal of Electrical and Computer Engineering*, v. 28, n. 2, p. 69–74, 2003.
- ANICHE, M. *Java code metrics calculator (CK)*. Disponível em: <https://github.com/mauricioaniche/ck>.
