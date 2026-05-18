# Relatório de Investigação — Consultar Exames do Paciente (Master/Detail)

**Data:** 17/05/2026
**Spec:** `.kiro/specs/consultar-exames/`
**Requisitos:** 13.1, 13.2, 13.3, 13.4, 13.5

---

## 1. Resumo Executivo

Investigação completa dos arquivos necessários para implementar a consulta
de exames na tela de Pacientes com detail inline (expand/collapse).
Todos os relacionamentos, endpoints de referência, extension methods e
padrões AJAX foram confirmados diretamente no código-fonte.

A implementação é viável sem alterações estruturais no projeto.

---

## 2. Investigação Técnica

### 2.1 PacientesController

| Item          | Valor                                                        |
|---------------|--------------------------------------------------------------|
| Projeto       | LabWebMvc.MVC                                                |
| Arquivo       | Areas/Controllers/PacientesController.cs                     |
| Namespace     | LabWebMvc.MVC.Areas.Controllers                              |
| Classe        | PacientesController : BaseController                         |
| Linhas        | ~510 linhas                                                  |

**Construtor:**
```csharp
public PacientesController(
    IDbFactory dbFactory,
    IValidadorDeSessao validador,
    GeralController geralController,
    IEventLogHelper eventLogHelper,
    Imagem imagem,
    ExclusaoService exclusaoService,
    IConnectionService connectionService)
    : base(dbFactory, validador, geralController, eventLogHelper,
           imagem, exclusaoService, connectionService)
```

**Campos herdados de BaseController:**
- `_db` (Db — DbContext, Npgsql)
- `_geralController` (GeralController)
- `_eventLogHelper` (IEventLogHelper)
- `_exclusaoService` (ExclusaoService)

**Métodos existentes:**

| Método                    | Verbo    | Rota                   |
|---------------------------|----------|------------------------|
| Index                     | HttpGet  | Pacientes              |
| IncluirPaciente           | HttpGet  | IncluirPaciente        |
| SalvarPaciente            | HttpPost | IncluirPaciente        |
| AlterarPaciente           | HttpGet  | AlterarPaciente        |
| SalvarAlteracaoPaciente   | HttpPost | AlterarPaciente        |
| ExcluirPaciente           | HttpGet  | ExcluirPaciente        |
| ConsultarPaciente         | HttpGet  | ConsultarPaciente      |
| ConverterPdf              | —        | —                      |

**Assinatura do Index:**
```csharp
[TypeFilter(typeof(SessionFilter))]
[HttpGet]
[Route("Pacientes")]
public async Task<IActionResult> Index(string? Conteudo, int registros = 50)
```

**Imports relevantes:**
- `using BLL;`
- `using static BLL.UtilBLL;`
- `using LabWebMvc.MVC.Models;`
- `using Microsoft.EntityFrameworkCore;`
- `using LabWebMvc.MVC.Areas.ExpressionCombiner;`

**Observações:**
- O Index usa `FiltrarPorConteudo()` para busca genérica.
- Quando `Conteudo` está vazio, retorna os últimos 100 registros.
- Já usa `ConverterDataLocalParaRangeUtc()` para busca por data de nascimento.
- Retorna via `_geralController.ValidacaoGenerica(vmResposta)`.

---

### 2.2 Views/Pacientes/Index.cshtml

| Item          | Valor                                                        |
|---------------|--------------------------------------------------------------|
| Arquivo       | Views/Pacientes/Index.cshtml                                 |
| Model         | vmPacientes                                                  |
| Partial Menu  | Partials/_PartialMenuPacientes.cshtml                        |

**Estrutura HTML:**
- `<partial name='Partials/_PartialMenuPacientes' />`
- `<div class="table-responsive">`
- `<table id="modeloTable">` com classes `display compact order-column
  stripe table-hover nowrap`
- `data-order='[[ 0, "desc" ]]'`
- 9 colunas: Id, Id Externo, Nome Paciente, DT Nascimento, Sexo,
  Documento, Tipo.Doc, Telefone, Opções

**DataTables config:**
- Usa `_PartialDatatables` (shared) que chama `configTable()` de
  `mydatatables.js`
- `configTable()` inicializa com: footer search inputs, fixedColumns,
  autoWidth, responsive, layout com pageLength/search/info/paging,
  language pt-BR

**Scripts existentes:**
- `clickConsulta(x)` — redireciona para ConsultarPaciente
- `clickExames(x)` — **alert("Em desenvolvimento")** ← ponto de
  integração para o detail inline
- `clickAlterar(x)` — redireciona para AlterarPaciente
- `clickDelete(x)` — usa `clickConfirm` para ExcluirPaciente

**Coluna de opções:**
- 4 ícones: Consultar, Exames Realizados, Alterar, Excluir
- Classe: `grid_fundo_opcoes`
- Cada ícone usa `onclick` com `id=item.Id`

---

### 2.3 Models — Relacionamentos Confirmados

#### Pacientes (ModeloDeDados/Models/Pacientes.cs)

| Propriedade           | Tipo                                  | Relação         |
|-----------------------|---------------------------------------|-----------------|
| Id                    | int (PK)                              | —               |
| NomePaciente          | string                                | —               |
| Nascimento            | DateTime                              | —               |
| ExamesRealizados      | ICollection\<ExamesRealizados\>       | 1:N (FK)        |
| ItensExamesRealizados | ICollection\<ItensExamesRealizados\>  | 1:N (FK)        |
| Requisitar            | ICollection\<Requisitar\>             | 1:N (FK)        |

#### ExamesRealizados (ModeloDeDados/Models/ExamesRealizados.cs)

| Propriedade      | Tipo                                   | Relação         |
|------------------|----------------------------------------|-----------------|
| Id               | int (PK)                               | —               |
| PacienteId       | int (FK)                               | N:1 Pacientes   |
| ClasseExamesId   | int (FK)                               | N:1 ClasseExames|
| InstituicaoId    | int (FK)                               | N:1 Instituicao |
| PostoId          | int (FK)                               | N:1 Postos      |
| MedicoId         | int (FK)                               | N:1 Medicos     |
| TabelaExamesId   | int (FK)                               | N:1 TabelaExames|
| DataIni          | DateTime                               | timestamptz     |
| DataFim          | DateTime?                              | timestamptz     |
| Sequencial       | int                                    | —               |
| ClasseExames     | virtual ClasseExames                   | Navigation      |
| Instituicao      | virtual Instituicao                    | Navigation      |
| Postos           | virtual Postos                         | Navigation      |
| Pacientes        | virtual Pacientes                      | Navigation      |
| ItensExamesRealizados | ICollection\<ItensExamesRealizados\> | 1:N           |

#### ItensExamesRealizados (ModeloDeDados/Models/ItensExamesRealizados.cs)

| Propriedade       | Tipo                    | Relação              |
|-------------------|-------------------------|----------------------|
| Id                | int (PK)                | —                    |
| ExameRealizadoId  | int (FK)                | N:1 ExamesRealizados |
| PacienteId        | int (FK)                | N:1 Pacientes        |
| ClasseExamesId    | int (FK)                | N:1 ClasseExames     |
| TabelaExamesId    | int (FK)                | N:1 TabelaExames     |
| InstituicaoId     | int (FK)                | N:1 Instituicao      |
| OrdemItem         | int                     | Ordenação            |
| RefExame          | string                  | —                    |
| RefItem           | string                  | —                    |
| ContaExame        | string                  | —                    |
| Descricao         | string?                 | —                    |
| ValorItem         | decimal?                | —                    |
| ExamesRealizados  | virtual ExamesRealizados| Navigation           |

#### ClasseExames (ModeloDeDados/Models/Classeexames.cs)

| Propriedade      | Tipo                                   | Relação         |
|------------------|----------------------------------------|-----------------|
| Id               | int (PK)                               | —               |
| RefExame         | string?                                | Nome da folha   |
| ExamesRealizados | ICollection\<ExamesRealizados\>        | 1:N             |
| ItensExamesRealizados | ICollection\<ItensExamesRealizados\> | 1:N           |

#### Instituicao (ModeloDeDados/Models/Instituicao.cs)

| Propriedade | Tipo   | Observação                |
|-------------|--------|---------------------------|
| Id          | int    | PK                        |
| Sigla       | string | Usado no detail           |
| Nome        | string | Nome completo             |

#### Postos (ModeloDeDados/Models/Postos.cs)

| Propriedade | Tipo   | Observação                |
|-------------|--------|---------------------------|
| Id          | int    | PK                        |
| NomePosto   | string | Será abreviado (max 12)   |

---

### 2.4 Padrão AJAX — ConsultarExames/Index.cshtml (Referência)

| Item          | Valor                                                        |
|---------------|--------------------------------------------------------------|
| Arquivo       | Views/ConsultarExames/Index.cshtml                           |
| Controller    | ConsultarExamesController.cs                                 |

**Padrão de detail inline confirmado:**

1. **Handler de clique** delegado no `tbody`:
   ```javascript
   $('#modeloTable tbody').on('click', 'tr', function (e) { ... })
   ```
2. **Ignora** cliques em `.grid_fundo_opcoes`, `.detail-row`,
   `.detail-header-row`
3. **Remove** detail anterior antes de abrir novo
4. **Destaca** linha pai com classe `.detail-parent-highlight`
5. **Chamada AJAX** via `$.ajax` GET para endpoint que retorna JSON
6. **Renderização** via injeção de TRs após a linha clicada:
   - TR de header (`.detail-header-row`) com títulos das colunas
   - TRs de dados (`.detail-row`) com os itens
7. **CSS** no bloco `<style>` da própria view

**Endpoint de referência:**
```csharp
[Route("ConsultarExames/ObterItensExame")]
public async Task<IActionResult> ObterItensExame(int exameRealizadoId)
```
- Retorna `Json(new { sucesso = true, itens })`
- Usa `AsNoTracking()`, `Where`, `OrderBy`, `Select` com projeção anônima
- Aplica `FormatarContaExameSem11()` na projeção

**Padrão de filtros backend (ConsultarExamesController.Index):**
- Parâmetros opcionais na assinatura do Index
- Query com `Include()` para navigation properties
- Filtros condicionais com `Where()` encadeados
- Sem filtro: `Take(100)` com `OrderByDescending`
- Retorna via `_geralController.ValidacaoGenerica(vmResposta)`

---

### 2.5 GeralController — ConverterDataLocalParaRangeUtc

| Item          | Valor                                                        |
|---------------|--------------------------------------------------------------|
| Arquivo       | Areas/Controllers/GeralController.cs                         |
| Método        | ConverterDataLocalParaRangeUtc                               |
| Linha         | ~272                                                         |

**Assinatura:**
```csharp
public (DateTime inicioUtc, DateTime fimUtc)
    ConverterDataLocalParaRangeUtc(DateTime dataLocal)
```

**Comportamento:**
- Se `Kind=UTC`: converte para local (America/Sao_Paulo), extrai `.Date`,
  calcula range UTC do dia
- Se `Kind=Unspecified` ou `Kind=Local`: trata como horário de Brasília,
  extrai `.Date`, calcula range UTC do dia
- Retorna tupla `(inicioUtc, fimUtc)` representando 00:00:00 até
  23:59:59.9999999 do dia em UTC

**Uso no PacientesController.Index (existente):**
```csharp
var (inicioUtc, fimUtc) = _geralController
    .ConverterDataLocalParaRangeUtc(dataBusca);
query = query.Where(l => l.Nascimento >= inicioUtc &&
                         l.Nascimento <= fimUtc);
```

---

### 2.6 Extension Methods

#### ToLocalString (BLL/UtilBLL.cs, linha ~298)

```csharp
public static string ToLocalString(
    this DateTime utc,
    string formato = "dd/MM/yyyy HH:mm:ss",
    string timezoneId = "America/Sao_Paulo")
```
- Converte UTC → local (America/Sao_Paulo)
- Se `Kind != UTC`, força `SpecifyKind(utc, DateTimeKind.Utc)`
- Overload para `DateTime?` retorna `string.Empty` se nulo

**Uso no ConsultarExames/Index.cshtml:**
```razor
((DateTime)item.DataIni).ToLocalString("dd/MM/yyyy")
```

#### FormatarContaExameSem11 (BLL/UtilBLL.cs, linha ~1007)

```csharp
public static string FormatarContaExameSem11(this string? conta)
```
- Se `null` → retorna `string.Empty`
- Se não vazio: PadLeft(11, '0'), Substring(2, 9), formata como
  `##.###.####`
- Usado no ConsultarExamesController.ObterItensExame na projeção

**Namespace:** `BLL` (classe `UtilBLL`, métodos estáticos de extensão)
**Import necessário:** `using static BLL.UtilBLL;` (já presente no
PacientesController)

---

## 3. Fluxo de Execução Proposto

### Endpoint ObterFolhasExame
1. Recebe GET sem parâmetros
2. Query: `_db.ClasseExames.AsNoTracking().OrderBy(c => c.RefExame)
   .Select(c => new { c.Id, c.RefExame })`
3. Retorna JSON `{ sucesso: true, folhas: [...] }`

### Endpoint ObterExamesPaciente
1. Recebe GET com `pacienteId`
2. Query: `_db.ExamesRealizados.AsNoTracking()
   .Include(e => e.Instituicao).Include(e => e.Postos)
   .Include(e => e.ClasseExames)
   .Where(e => e.PacienteId == pacienteId)
   .OrderByDescending(e => e.DataIni)`
3. Projeta: Id, DataIni (dd/MM/yyyy via ToLocalString), DataFim,
   Sigla, NomePosto (abreviado max 12), Folha (RefExame)
4. Retorna JSON `{ sucesso: true, exames: [...] }`

### Endpoint ObterItensExame
1. Recebe GET com `exameRealizadoId`
2. Query: `_db.ItensExamesRealizados.AsNoTracking()
   .Where(i => i.ExameRealizadoId == exameRealizadoId)
   .OrderBy(i => i.OrdemItem)`
3. Projeta: RefExame, RefItem, ContaExame (formatado), Descricao
4. Retorna JSON `{ sucesso: true, itens: [...] }`

### Filtros no Index
1. Novos parâmetros: `dataInicial`, `dataFinal`, `nomePaciente`, `folhaId`
2. Só aplicados quando `Conteudo` está vazio
3. Filtro por período: `ConverterDataLocalParaRangeUtc` para cada data,
   join com ExamesRealizados.DataIni
4. Filtro por nome: `NomePaciente.Contains()` case-insensitive
5. Filtro por folha: pacientes com ExamesRealizados.ClasseExamesId == folhaId

---

## 4. Riscos Identificados

| #  | Risco                                              | Severidade | Mitigação                          |
|----|----------------------------------------------------|-----------|------------------------------------|
| 1  | Filtro por período exige join com ExamesRealizados | Média     | Usar subquery com Any()            |
| 2  | NomePosto pode ser null                            | Baixa     | Null-coalescing na abreviação      |
| 3  | DataFim pode ser null                              | Baixa     | Tratar com ?? "-" na projeção      |
| 4  | Performance em tabelas grandes sem índice em DataIni| Média    | AsNoTracking + Take limitado       |
| 5  | Conflito com configTable() ao injetar TRs no DOM   | Baixa     | TRs injetados não afetam DataTables|
| 6  | Acúmulo de handlers se view recarregada via AJAX   | Baixa     | Usar namespace + off() antes on()  |

---

## 5. Preocupações de Segurança

| Item                          | Análise                                    |
|-------------------------------|--------------------------------------------|
| SQL Injection                 | Sem risco — usa EF Core com LINQ           |
| Autorização                   | SessionFilter aplicado em todos endpoints  |
| Exposição de dados            | Projeção anônima limita campos retornados   |
| Validação de parâmetros       | Tipos int garantem validação pelo binder   |

---

## 6. Oportunidades de Performance

| Item                                    | Impacto | Observação                     |
|-----------------------------------------|---------|--------------------------------|
| Usar `Any()` em vez de `Include()` para | Alto    | Evita materializar ExamesReali-|
| filtro por período/folha no Index       |         | zados inteiros                 |
| Projeção com `Select()` nos endpoints   | Médio   | Já planejado — evita trazer    |
| AJAX                                    |         | colunas desnecessárias         |
| `AsNoTracking()` em todas as queries    | Médio   | Já planejado — somente leitura |

---

## 7. Arquivos e Métodos Impactados

| Arquivo                                          | Ação                          |
|--------------------------------------------------|-------------------------------|
| Areas/Controllers/PacientesController.cs         | Adicionar 3 endpoints + filtros|
| Views/Pacientes/Index.cshtml                     | Adicionar filtros HTML + JS   |
| BLL/UtilBLL.cs                                   | Somente leitura (extension)   |
| Areas/Controllers/GeralController.cs             | Somente leitura (método)      |
| ModeloDeDados/Models/*.cs                        | Somente leitura (models)      |

---

## 8. Plano Técnico de Implementação

### Fase 2 — Backend (Endpoints)
1. Adicionar `ObterFolhasExame` no PacientesController
2. Adicionar `ObterExamesPaciente` no PacientesController
3. Adicionar `ObterItensExame` no PacientesController
4. Todos seguem o padrão do ConsultarExamesController

### Fase 3 — Backend (Filtros no Index)
1. Estender assinatura do Index com parâmetros opcionais
2. Implementar lógica condicional (só quando Conteudo vazio)
3. Usar `Any()` com subquery para filtro por período/folha

### Fase 5 — Frontend (Filtros)
1. Adicionar div de filtros antes do grid (padrão ConsultarExames)
2. Carregar ComboBox de folhas via AJAX ao carregar página
3. Form GET submete para o mesmo Index

### Fase 6-7 — Frontend (Detail Inline)
1. Handler de clique na linha do paciente (namespace)
2. AJAX para ObterExamesPaciente → renderizar TRs de exames
3. Handler de clique na linha do exame → AJAX ObterItensExame
4. CSS no bloco `<style>` da view

### Fase 8-9 — Verificação
1. Build completo com 0 erros e 0 avisos
2. Encoding UTF-8 com BOM preservado
3. Marcação `//Feito pelo Kiro` em todos os blocos

---

## 9. Risco de Regressão

| Área                        | Risco     | Motivo                              |
|-----------------------------|-----------|-------------------------------------|
| Busca existente (Conteudo)  | Nenhum    | Filtros novos só atuam quando vazio |
| Grid DataTables existente   | Baixo     | TRs injetados são removidos antes   |
| Botões de ação existentes   | Nenhum    | Handlers onclick preservados        |
| Rotas existentes            | Nenhum    | Novas rotas não conflitam           |

---

## 10. Conclusão Técnica Final

A implementação é viável e de baixo risco. O projeto já possui:
- Padrão de detail inline funcional (ConsultarExames)
- Extension methods necessários (ToLocalString, FormatarContaExameSem11)
- Método de conversão de datas (ConverterDataLocalParaRangeUtc)
- Navigation properties corretas nos models
- Padrão de filtros backend (ConsultarExamesController)

Não é necessário adicionar pacotes NuGet, alterar models ou criar
novos arquivos além das modificações no controller e na view existentes.

---

## Checklist de Validação

```
[x] O código-fonte real foi inspecionado?
[x] As conclusões são baseadas em evidência?
[x] Os nomes de arquivo foram identificados?
[x] Os nomes de classe foram identificados?
[x] Os métodos foram identificados?
[x] As linhas aproximadas foram identificadas?
[x] O fluxo de execução foi explicado?
[x] As entradas foram explicadas?
[x] As saídas foram explicadas?
[x] As intenções de negócio foram explicadas?
[x] Os cenários de falha foram identificados?
[x] Os bugs potenciais foram identificados?
[x] As fragilidades de segurança foram analisadas?
[x] As oportunidades de performance foram analisadas?
[x] Novas bibliotecas desnecessárias foram evitadas?
[x] A análise é suficiente para implementar a mudança?
[x] Os achados estão agrupados em tópicos contextuais?
```
