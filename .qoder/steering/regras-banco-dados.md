---
trigger: always
description: Regras de Banco de Dados PostgreSQL para LabWeb7
---

# Steering de Banco de Dados - Qoder

## Banco de Dados Exclusivo: PostgreSQL

### Provider e Configuração
- ✅ **Provider:** `Npgsql.EntityFrameworkCore.PostgreSQL` v8.0.4
- ✅ **Connection Strings:** `ConexaoPostgreSQL` ou `PSQLConnectionString` (no appsettings.json)
- ❌ **NUNCA** usar pacotes, código ou sintaxe SQL Server
- ❌ **NUNCA** usar `System.Data.SqlClient` ou `Microsoft.Data.SqlClient`

### Sintaxe PostgreSQL vs SQL Server

| Funcionalidade | PostgreSQL ✅ | SQL Server ❌ |
|----------------|---------------|---------------|
| Data/Hora atual | `NOW()` | `GETDATE()`, `SYSDATETIME()` |
| Concatenação | `\|\|` ou `CONCAT()` | `+` |
| Top N | `LIMIT n` | `TOP n` |
| String length | `LENGTH()` | `LEN()` |
| Identity | `SERIAL` ou `GENERATED ALWAYS AS IDENTITY` | `IDENTITY(1,1)` |
| Boolean | `BOOLEAN` | `BIT` |
| Case-insensitive | `ILIKE` | `LIKE` (com collation) |

## Transações EF Core

### Preferir Transações Nativas
```csharp
// ✅ CORRETO - Transação nativa EF Core
using var transaction = await _db.Database.BeginTransactionAsync();
try
{
    await _db.Pacientes.AddAsync(paciente);
    await _db.SaveChangesAsync();
    
    await _db.ExamesRealizados.AddAsync(exame);
    await _db.SaveChangesAsync();
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### TransactionScope (se necessário)
```csharp
// ✅ CORRETO - Com TransactionScopeAsyncFlowOption.Enabled
using var scope = new TransactionScope(
    TransactionScopeOption.Required,
    new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
    TransactionScopeAsyncFlowOption.Enabled  // OBRIGATÓRIO para async
);

try
{
    // operações
    scope.Complete();
}
catch
{
    // rollback automático
}
```

## Data e Hora - REGRAS CRÍTICAS

### ❌ NUNCA Usar
```csharp
// ERRADO - Usa hora do servidor da aplicação
entity.DataRegistro = DateTime.Now;
entity.DataRegistro = DateTime.UtcNow;
entity.DataRegistro = DateTime.Today;
```

### ✅ SEMPRE Usar
```csharp
// Opção 1: Via GeralController
entity.DataRegistro = await _geralController.ObterDataHoraServidor();

// Opção 2: Via query SQL
var serverTime = await _db.Database
    .SqlQuery<DateTime>("SELECT NOW()")
    .FirstOrDefaultAsync();
entity.DataRegistro = serverTime;
```

### DateTime Kind por Tipo de Coluna

| Tipo PostgreSQL | DateTime.Kind | Exemplo |
|-----------------|---------------|---------|
| `timestamp without time zone` | `Unspecified` ou `Local` | `DateTime.SpecifyKind(dt, DateTimeKind.Unspecified)` |
| `timestamp with time zone` | `UTC` | `DateTime.SpecifyKind(dt, DateTimeKind.Utc)` ou `DateTimeOffset` |

### Exemplo Correto
```csharp
// Para coluna timestamp without time zone
var dataServidor = await _geralController.ObterDataHoraServidor();
entity.DataRegistro = DateTime.SpecifyKind(dataServidor, DateTimeKind.Unspecified);

// Para coluna timestamp with time zone
var dataServidor = await _geralController.ObterDataHoraServidor();
entity.DataRegistro = DateTime.SpecifyKind(dataServidor, DateTimeKind.Utc);
```

### Logs e EventLog
```csharp
// DateTime.UtcNow é aceitável em logs (NÃO grava no banco)
_eventLog.LogEventViewer($"Erro em {DateTime.UtcNow}", "Error");
```

## DbContext Features Especiais

### Factory Pattern
```csharp
// DbContext criado via factory para troca dinâmica de banco
_db = _dbFactory.Create();
```

### SaveChanges Customizado
```csharp
// SaveChanges com reutilização de IDs (limite 99)
await _db.SaveChangesWithSyncAsync(
    sincroniza: true, 
    quantidadeRegistrosMaximo: 99
);

// Remove órfãos automaticamente
_db.DeleteOrphans();
```

### Table Locking
```csharp
// Lock para controle de concorrência
await Database.ExecuteSqlRawAsync(
    $@"LOCK TABLE ""{tableName}"" IN EXCLUSIVE MODE"
);
```

### Sequence Synchronization
```csharp
// Sincroniza sequência PostgreSQL
var sql = $"SELECT setval(pg_get_serial_sequence('\"{tableName}\"', 'Id'), {maxId.Value})";
await Database.ExecuteSqlRawAsync(sql);
```

## Migrations - NÃO USAR

- ❌ O projeto **NÃO utiliza EF Migrations**
- ✅ Todas as alterações de schema são feitas via **scripts SQL manuais**
- ✅ Scripts localizados em: `Biblioteca SQL/Base de Dados Vazio Postgresql/`
- ✅ Manter scripts sincronizados com modelo C#

## Validação de FK Antes de DELETE

### ❌ NÃO Confiar Apenas na Exception do Banco
```csharp
// ERRADO - Deixa o banco lançar exception
try
{
    _db.Pacientes.Remove(paciente);
    await _db.SaveChangesAsync();
}
catch (DbUpdateException ex)
{
    // FK violation pega aqui
}
```

### ✅ Validar Antes no Controller
```csharp
// CORRETO - Verifica FKs antes de deletar
var temExames = await _db.ExamesRealizados
    .AnyAsync(e => e.PacienteId == pacienteId);

if (temExames)
{
    return View("Error", "Paciente possui exames vinculados e não pode ser excluído.");
}

var temRequisicoes = await _db.Requisitar
    .AnyAsync(r => r.PacienteId == pacienteId);

if (temRequisicoes)
{
    return View("Error", "Paciente possui requisições pendentes e não pode ser excluído.");
}

// Só então deleta
_db.Pacientes.Remove(paciente);
await _db.SaveChangesAsync();
```

## Connection Strings

### appsettings.json
```json
{
  "ConnectionStrings": {
    "ConexaoPostgreSQL": "Host=127.0.0.1;Database=db_labweb7;Username=postgres;Password=senha"
  }
}
```

### Multi-Tenant
```csharp
// Cada empresa tem seu próprio banco
_connectionService.SetConnectionString(empresaId);
_db = _dbFactory.Create();
```

## Queries PostgreSQL

### Date Functions
```csharp
// ✅ CORRETO - Usando NOW() do PostgreSQL
var query = _db.ExamesRealizados
    .Where(e => e.DataIni >= EF.Functions.DateDiffDay(EF.Property<DateTime>("NOW()"), e.DataIni));

// SQL direto
var result = await _db.Database
    .SqlQuery<ExamesRealizados>(
        "SELECT * FROM \"ExamesRealizados\" WHERE \"DataIni\" >= NOW() - INTERVAL '30 days'")
    .ToListAsync();
```

### String Operations
```csharp
// ✅ CORRETO - ILIKE para case-insensitive em PostgreSQL
var pacientes = await _db.Pacientes
    .Where(p => EF.Functions.ILike(p.NomePaciente, "%joão%"))
    .ToListAsync();
```

## Performance de Queries

### Boas Práticas
- ✅ Usar `AsNoTracking()` para queries read-only
- ✅ Usar `Select()` para projetar apenas campos necessários
- ✅ Evitar `Include()` desnecessários
- ✅ Usar índices nas colunas de filtro

```csharp
// ✅ Otimizado
var pacientes = await _db.Pacientes
    .AsNoTracking()
    .Where(p => p.Cidade == "São Paulo")
    .Select(p => new { p.Id, p.NomePaciente, p.CPF })
    .ToListAsync();
```

## Checklist de Validação

Antes de executar queries:

```
□ Está usando sintaxe PostgreSQL (não SQL Server)?
□ Data/hora está sendo obtida do servidor (não DateTime.Now)?
□ DateTime.Kind está correto para o tipo de coluna?
□ Transações estão usando BeginTransactionAsync()?
□ FKs estão sendo validadas antes de DELETE?
□ Não está usando Migrations?
□ Connection string aponta para ConexaoPostgreSQL?
```

---

**Steering criado por Qoder - 21/04/2026**  
*Baseado nas melhores práticas do projeto LabWeb7*
