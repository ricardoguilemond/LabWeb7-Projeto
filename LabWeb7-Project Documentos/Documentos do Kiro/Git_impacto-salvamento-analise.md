# Git — Análise de Impacto nos Botões de Salvamento

**Data:** 19/04/2026
**Branch atual:** `LabWeb7-Projeto-2025`
**HEAD:** `b7c7630` (16/11/2025)
**Telas afetadas:** Médicos, Instituições, Postos
**Tela funcional:** Pacientes

---

## 1. Linha do Tempo dos Commits

| Commit   | Data             | Descrição                              | Impacto |
|----------|------------------|----------------------------------------|---------|
| 74fd506  | 20/10/2025       | Conversão MSSQL → PostgreSQL           | Nenhum  |
| f108019  | 26/10/2025 20:04 | Melhorias nas Views e controllers      | ALTO    |
| 0d8daff  | 02/11/2025       | ExclusaoService, DeleteOrphans, db.cs  | MÉDIO   |
| 983e5c1  | 15/11/2025       | Git ignore                             | Nenhum  |
| b7c7630  | 16/11/2025       | Exclusão de documento (HEAD atual)     | Nenhum  |
| bd0f43e  | 22/02/2026       | Melhorias CRUD (NÃO no HEAD)           | ALTO    |
| 54ddead  | 16/04/2026       | Otimizações Kiro (NÃO no HEAD)         | BAIXO   |

---

## 2. Commit Crítico: f108019 (26/10/2025 20:04)

Este é o commit que introduziu o problema. Está INCLUÍDO no HEAD atual.

### O que foi alterado

**GeralController.cs — método `ValidacaoGenerica`:**

Antes (funcionava):
```csharp
return string.IsNullOrEmpty(vm.PartialView) ? View() : PartialView(vm.PartialView);
```

Depois (quebrou):
```csharp
return string.IsNullOrEmpty(vm.PartialView)
    ? View(vm.RetornoDeRota, vm)      // PASSA vm COMO MODEL
    : PartialView(vm.PartialView, vm); // PASSA vm COMO MODEL
```

**Criação de `vmListaValidacao.cs`:**

Nova classe genérica para padronizar a passagem de dados
entre controllers e `GeralController.ValidacaoGenerica`.

### Por que isso quebra o salvamento

Quando `View(vm.RetornoDeRota, vm)` é chamado, o ASP.NET Core
interpreta o primeiro parâmetro como nome da View e o segundo
como o Model. A View `Index.cshtml` de Médicos espera `@model`
implícito ou nenhum model, mas recebe `vmListaValidacao<dynamic>`.

Isso causa `InvalidOperationException` em runtime:
> The model item passed into the ViewDataDictionary is of type
> 'vmListaValidacao\`1[System.Object]', but this ViewDataDictionary
> instance requires a model item of type 'vmMedicos'.

O erro ocorre na listagem (Index), não no salvamento direto.
Porém, se a listagem falha após o POST de salvamento, o usuário
não vê confirmação e assume que o salvamento falhou.

---

## 3. Commit Relevante: 0d8daff (02/11/2025)

### O que foi alterado

**db.cs — `SaveChanges()` e `SaveChangesAsync()`:**

- `SaveChanges()` foi refatorado com parâmetro `bool sincroniza`
- `SaveChangesAsync()` foi renomeado internamente para
  `SaveChangesIfChangedAsync()`
- Adicionado `SaveChangesWithSyncAsync()` para controle de IDs
- `DeleteOrphans()` agora só executa se `sincroniza = true`

**Impacto:** O `SaveChanges()` padrão (sem parâmetro) chama
`SaveChanges(false)`, que NÃO executa `DeleteOrphans()`.
Isso é seguro para o salvamento normal. Porém, se algum
controller dependia do `DeleteOrphans()` implícito, pode
haver efeitos colaterais.

**BaseController.cs:**

- Adicionado `ExclusaoService` como dependência injetada
- Todos os controllers receberam o novo parâmetro

**Impacto:** Apenas estrutural, não afeta o salvamento.

---

## 4. Commit bd0f43e (22/02/2026) — NÃO no HEAD

Este commit tentou corrigir o problema do `f108019`:

**GeralController.cs — corrigiu `ValidacaoGenerica`:**

```csharp
// Removeu: View(vm.RetornoDeRota, vm)
// Adicionou:
if (!string.IsNullOrEmpty(vm.PartialView))
    return View(vm.PartialView);
else
    return View();  // SEM model — correto
```

**Porém**, este mesmo commit também migrou os métodos `Index`
e `Incluir` de Médicos, Instituições e Postos para usar
`ValidacaoGenerica` em vez de `Validacao`:

```csharp
// Antes (Validacao):
return _geralController.Validacao("Index", "Cadastro de Médicos",
    totalRegistros, totalTabela, listaGrid);

// Depois (ValidacaoGenerica):
var vmResposta = new vmListaValidacao<dynamic> { ... };
return _geralController.ValidacaoGenerica(vmResposta);
```

E migrou `SalvarMedico` de `TransactionScope` para
`_db.Database.BeginTransactionAsync()` (EF Core nativo).

**Este commit NÃO está no HEAD atual** (`LabWeb7-Projeto-2025`).
Está apenas na branch `main` (origin/main).

---

## 5. Estado Atual do Código em Disco

O código em disco (arquivos no workspace) apresenta uma
**mistura** de estados:

| Arquivo                    | Estado no disco                    | Origem         |
|----------------------------|------------------------------------|----------------|
| GeralController.cs         | `View()` sem model (correto)       | Editado na sessão atual |
| PacientesController.cs     | Usa `ValidacaoGenerica` (do bd0f43e)| Commit bd0f43e |
| MedicosController.cs       | Usa `Validacao` (do 0d8daff)       | HEAD b7c7630   |
| InstituicoesController.cs  | Usa `Validacao` (do 0d8daff)       | HEAD b7c7630   |
| PostosController.cs        | Usa `Validacao` (do 0d8daff)       | HEAD b7c7630   |

Pacientes usa `ValidacaoGenerica` enquanto Médicos, Instituições
e Postos usam `Validacao`. Ambos os caminhos funcionam porque
o `GeralController` atual retorna `View()` sem model em ambos.

---

## 6. Causa Raiz do Problema de Salvamento

Após análise completa dos commits, o problema de salvamento
**NÃO é causado pelo `ValidacaoGenerica`** (que afeta apenas
a listagem/Index). O salvamento (POST) usa métodos separados:
`SalvarMedico`, `SalvarAlteracaoMedico`, etc.

### Diferença real entre Pacientes (funciona) e Médicos (não funciona)

| Aspecto                | Pacientes                  | Médicos/Inst./Postos       |
|------------------------|----------------------------|----------------------------|
| Transação no Save      | `BeginTransactionAsync()`  | `TransactionScope`         |
| Transação no Update    | `BeginTransactionAsync()`  | `TransactionScope`         |
| Async flow             | Nativo EF Core             | Sem `AsyncFlowOption`      |
| Catch de erro          | Retorna JSON de erro       | Loga mas NÃO retorna erro |
| SaveChanges            | `SaveChangesAsync()`       | `SaveChanges()` (síncrono) |

### Hipótese principal: `TransactionScope` sem `AsyncFlowOption`

Os controllers de Médicos, Instituições e Postos usam:

```csharp
using (TransactionScope trans = new(
    TransactionScopeOption.Required,
    new TransactionOptions() {
        IsolationLevel = IsolationLevel.ReadCommitted
    }))
// FALTA: TransactionScopeAsyncFlowOption.Enabled
```

Com Npgsql (PostgreSQL), o `TransactionScope` sem
`TransactionScopeAsyncFlowOption.Enabled` pode causar:

1. A transação não se propaga para operações async
2. `SaveChanges()` executa fora da transação
3. `trans.Complete()` tenta completar uma transação vazia
4. `Dispose()` lança `TransactionAbortedException`
5. O `catch` loga o erro mas **não retorna** — cai no
   `return Json(... sucesso = true)` fora do try/catch

O usuário vê "Médico foi salvo" mas os dados podem não
ter sido persistidos, ou foram persistidos mas a resposta
JSON não chega corretamente ao browser.

### Hipótese secundária: erro silencioso no AJAX

Se o `TransactionAbortedException` causa um erro HTTP 500
em vez de retornar JSON, o callback `error` do AJAX é
acionado. Nesse callback, a variável `actionPos` não está
definida (está no escopo do `success`), causando um erro
JavaScript que impede a mensagem de aparecer.

---

## 7. Recomendações de Correção

### Correção 1 — Migrar para transações EF Core nativas (recomendado)

Substituir `TransactionScope` por `_db.Database.BeginTransactionAsync()`
em `MedicosController`, `InstituicoesController` e `PostosController`,
seguindo o mesmo padrão do `PacientesController`.

### Correção 2 — Adicionar `AsyncFlowOption` (alternativa)

Se preferir manter `TransactionScope`, adicionar o terceiro parâmetro:

```csharp
using (TransactionScope trans = new(
    TransactionScopeOption.Required,
    new TransactionOptions() {
        IsolationLevel = IsolationLevel.ReadCommitted
    },
    TransactionScopeAsyncFlowOption.Enabled))  // ADICIONAR
```

### Correção 3 — Tratar o catch corretamente

Em todos os métodos de salvamento, o `catch (TransactionAbortedException)`
deve retornar JSON de erro em vez de cair no return de sucesso:

```csharp
catch (TransactionAbortedException ex)
{
    _eventLogHelper.LogEventViewer("...", "wError");
    return Json(new {
        titulo = MensagensError_pt_BR.ErroFalhou,
        mensagem = "Erro na transação",
        action = "",
        sucesso = false
    });
}
```

### Correção 4 — Corrigir variável `actionPos` no AJAX

Nos callbacks `error` das views, `actionPos` não está definida:

```javascript
error: function (request, status, error) {
    // actionPos NÃO existe neste escopo!
    clickAviso('Interrompido', 'Falha na execução', 'critica', actionPos);
}
```

Corrigir para:

```javascript
error: function (request, status, error) {
    clickAviso('Interrompido', 'Falha na execução', 'critica', '');
}
```

---

## 8. Prioridade de Execução

| Prioridade | Correção                              | Risco  |
|------------|---------------------------------------|--------|
| 1          | Migrar TransactionScope → EF Core     | Baixo  |
| 2          | Tratar catch com return JSON de erro  | Baixo  |
| 3          | Corrigir actionPos no AJAX das views  | Baixo  |
| 4          | Adicionar AsyncFlowOption (se manter) | Baixo  |
