---
inclusion: always
description: Regras definitivas de tratamento de datas — DateTime, timestamptz, UTC
---

# Steering — Tratamento de Datas (DateTime / timestamptz)

## Padrão Definitivo

- **Banco:** Todas as colunas de data são `TIMESTAMPTZ` (UTC)
- **Backend (.NET):** `DateTime` com `Kind=Utc` para persistência
- **Frontend:** Conversão UTC → pt-BR somente na apresentação
- **Fonte canônica:** PostgreSQL via `SELECT NOW()` (retorna UTC)
- **Fallback:** `DateTime.UtcNow` (nunca `DateTime.Now`)

## Proibições

- ❌ Nunca usar `DateTimeKind.Unspecified` em queries ou
  parâmetros para colunas `timestamptz`
- ❌ Nunca usar `DateTime.Now` ou `DateTime.Today` para gravar
- ❌ Nunca usar `ToFormataData()` para gerar valores de
  persistência (retorna `Kind=Unspecified`)
- ❌ Nunca confiar na data/hora do computador cliente
- ❌ Nunca fazer conversões implícitas de timezone

## Métodos Obrigatórios

### Para persistência (gravar no banco)

```csharp
// Data/hora atual UTC do servidor PostgreSQL
DateTime dataUtc = _geralController.ObterDataHoraUtc();

// Converter data local (do cliente) para UTC antes de gravar
DateTime dataUtc = _geralController.ConverterLocalParaUtc(dataLocal);
```

### Para filtros/queries (ler do banco)

```csharp
// Range do dia atual em UTC (para "registros de hoje")
var (inicioUtc, fimUtc) = _geralController.ObterRangeDiaUtc();

// Range de uma data específica em UTC
var (inicioUtc, fimUtc) = _geralController
    .ConverterDataLocalParaRangeUtc(dataLocal);

// Usar em queries:
.Where(r => r.DataIni >= inicioUtc && r.DataIni <= fimUtc)
```

### Para exibição (mostrar ao usuário)

```csharp
// Converter UTC para string pt-BR
string dataLocal = _tempoService
    .FormatarUtcParaLocal(dataUtc);
// Resultado: "03/05/2026 14:30:00"
```

## Métodos Legacy (em desuso)

Os métodos abaixo são mantidos para compatibilidade mas
**não devem ser usados em código novo**:

- `ObterDataHoraServidor()` → retorna string, usar
  `ObterDataHoraUtc()` em vez disso
- `ObterDataHoraServidorAsync()` → retorna `DateTime?`
  com `Kind=Unspecified`, usar `ObterDataHoraUtcAsync()`
- `ToFormataData()` → retorna `Kind=Unspecified`, não
  usar para persistência

## DDL PostgreSQL

Todas as colunas de data no DDL devem usar `TIMESTAMPTZ`:

```sql
"DataIni" TIMESTAMPTZ NOT NULL,
"DataEntrega" TIMESTAMPTZ,
"Nascimento" TIMESTAMPTZ NOT NULL,
```

Nunca usar `TIMESTAMP` (sem timezone) para novas colunas.

## Npgsql 8.x — Regras Estritas

O Npgsql 8.x aplica regras estritas de `DateTimeKind`:

| Coluna PostgreSQL | Kind aceito | Kind rejeitado |
|-------------------|-------------|----------------|
| `timestamptz` | `Utc` | `Unspecified`, `Local` |
| `timestamp` | `Unspecified`, `Local` | `Utc` |

O projeto **não usa** `EnableLegacyTimestampBehavior`.
Todas as datas devem ser `Kind=Utc` antes de gravar.

## Governança

- Qualquer alteração nas regras de datas requer análise
  completa de impacto (steering `regras-analise-antes-de-alterar`)
- Proibido introduzir `DateTimeKind.Unspecified` em código novo
- Proibido aplicar conversões automáticas sem controle de contexto
- Proibido alterar o `TempoServidorPostgreSQL` sem aprovação
