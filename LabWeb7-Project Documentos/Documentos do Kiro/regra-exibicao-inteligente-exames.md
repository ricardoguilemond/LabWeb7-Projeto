# Regra de Negócio — Exibição Inteligente de Exames do Paciente

## Objetivo

Implementar uma regra adaptativa e inteligente para exibição de exames
realizados do paciente, baseada em recência temporal + quantidade
mínima/máxima, preservando contexto clínico recente sem poluir a interface.

## Escopo Atual

- Tela de Pacientes (`Views/Pacientes/Index.cshtml`)
- Endpoint: `PacientesController.ObterExamesPaciente`

## Regra Matemática

```
ExamesExibidos = MAX(últimos 4 exames, exames nos últimos 90 dias) LIMIT 8
```

## Parâmetros

| Parâmetro | Valor | Descrição |
|-----------|-------|-----------|
| Mínimo | 4 | Sempre exibir no mínimo os 4 últimos exames |
| Janela temporal | 90 dias | Expandir para exames dentro deste período |
| Máximo | 8 | Nunca ultrapassar 8 exames visíveis |

## Regras Funcionais

### 1. Quantidade mínima obrigatória

Sempre exibir no mínimo os **4 últimos exames** realizados, ordenados
por `DataIni DESC`. Se o paciente possuir menos de 4, exibir todos.

### 2. Expansão inteligente por período recente

Após obter os 4 exames mínimos, verificar exames realizados dentro
dos últimos 90 dias. Se houver exames adicionais dentro deste período,
incluí-los automaticamente.

### 3. Limite máximo visual

Para evitar poluição do grid: máximo de **8 exames visíveis**.
Se existirem mais exames elegíveis, exibir apenas os 8 mais recentes.

### 4. Indicador de exames ocultos

Se houver mais exames além do limite visual, exibir indicador:
```
+N exames anteriores (total: X)
```

## Implementação Backend (C#)

```csharp
const int minimoExames = 4;
const int maximoExames = 8;
const int diasJanela = 90;

// 1. Buscar os últimos 8 exames do paciente (limite máximo no banco)
var ultimos8 = await _db.ExamesRealizados
    .AsNoTracking()
    .Where(e => e.PacienteId == pacienteId)
    .OrderByDescending(e => e.DataIni)
    .Take(maximoExames)
    .Include(...)
    .ToListAsync();

// 2. Aplicar regra adaptativa em memória (sobre max 8 registros)
DateTime dataLimite90Dias = DateTime.UtcNow.AddDays(-diasJanela);
var examesDentro90Dias = ultimos8
    .Where(e => e.DataIni >= dataLimite90Dias).ToList();

// MAX(últimos 4, exames nos últimos 90 dias) — já limitado a 8
List<ExamesRealizados> examesExibidos = examesDentro90Dias.Count >= minimoExames
    ? examesDentro90Dias
    : ultimos8.Take(minimoExames).ToList();

// 3. Contar total para indicador de ocultos
int totalExames = await _db.ExamesRealizados
    .Where(e => e.PacienteId == pacienteId)
    .CountAsync();

int examesOcultos = totalExames - examesExibidos.Count;
```

## Retorno JSON

```json
{
  "sucesso": true,
  "exames": [...],
  "totalExames": 12,
  "examesOcultos": 4
}
```

## Cenários Esperados

| Cenário | Exames | Resultado |
|---------|--------|-----------|
| Poucos exames (3) | 3 total | Mostra 3 |
| Histórico espaçado (Mai, Abr, Jan, Nov) | 4 total | Mostra 4 (mínimo) |
| Recente intenso (6 em 90 dias + 1 antigo) | 7 total | Mostra 6 recentes |
| Muito frequente (15 em 60 dias) | 15 total | Mostra 8, indica "+7" |

## Performance

| Aspecto | Solução |
|---------|---------|
| Query principal | `Take(8)` no banco — evita trazer todos |
| Contagem total | `CountAsync` separado (sem Include) |
| Índice utilizado | `iExamesRealizados2 (PacienteId, Id)` |
| N+1 | Evitado — Includes na mesma query |
| Filtragem 90 dias | Em memória sobre max 8 registros |

## Arquivos Impactados

- `LabWebMvc.MVC/Areas/Controllers/PacientesController.cs`
  - Método: `ObterExamesPaciente(int pacienteId)`
- `LabWebMvc.MVC/Views/Pacientes/Index.cshtml`
  - Funções: `carregarExamesPaciente`, `renderizarExamesPaciente`

## Data de Implementação

17/05/2026
