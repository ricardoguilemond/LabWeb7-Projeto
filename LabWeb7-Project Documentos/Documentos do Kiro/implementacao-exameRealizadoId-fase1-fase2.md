# Implementação ExameRealizadoId — Fases 1 e 2

**Data:** 03/05/2026
**Autor:** Kiro
**Escopo:** Relacionamento lógico entre Requisitar e
ExamesRealizados via ExameRealizadoId

---

## Contexto

O sistema apresentava inconsistência no "Código do Exame":
- O cupom imprimia `Requisitar.Id` (primeiro item)
- O grid mostrava `Requisitar.Id` (último item via `Max`)
- A cada edição, novos Ids eram gerados (DELETE + INSERT)
- Não havia vínculo entre `Requisitar` e `ExamesRealizados`

---

## Regra Central Implementada

O código do exame é **sempre** `ExamesRealizados.Id`,
propagado para `Requisitar.ExameRealizadoId` e
`ItensExamesRealizados.ExameRealizadoId`.

- `Requisitar.Id` = chave interna, nunca exibida ao usuário
- `ExameRealizadoId` = código do exame, exibido no grid e cupom
- Decisão inclusão vs edição: exclusivamente por
  `ExameRealizadoId` (> 0 = edição, 0 = inclusão nova)

---

## Fase 1 — Preparação Estrutural

### Banco de Dados

**DDL atualizado:** `Tabelas_Vazias.sql`
- Adicionada coluna `"ExameRealizadoId" INT` na tabela
  `Requisitar` (nullable, sem FK, sem constraint)

**Script de migração criado:**
`add_ExameRealizadoId_Requisitar.sql`
- `ALTER TABLE "Requisitar" ADD COLUMN "ExameRealizadoId" INT`
- Idempotente (verifica existência antes de adicionar)
- Executar uma vez por banco de cliente

### Modelos C#

| Arquivo | Alteração |
|---------|-----------|
| `ModeloDeDados/Models/Requisitar.cs` | `int? ExameRealizadoId` |
| `LabWebMvc.MVC/Models/Requisitar.cs` | `int? ExameRealizadoId` |

### Fluent API (EF Core)

| Arquivo | Alteração |
|---------|-----------|
| `ModeloDeDados/Models/db.cs` | `entity.Property(e => e.ExameRealizadoId).IsRequired(false)` |
| `LabWebMvc.MVC/Models/db.cs` | `entity.Property(e => e.ExameRealizadoId).IsRequired(false)` |

**Confirmação:** Nenhuma FK física criada. Nenhuma navegação
adicionada. O EF Core trata como coluna simples INT NULL.

### ViewModels

| Arquivo | Alteração |
|---------|-----------|
| `vmRequisitarSimplificado.cs` | `int? ExameRealizadoId` |
| `vmRequisitar.cs` | `int ExameRealizadoId` |

### Frontend

| Arquivo | Alteração |
|---------|-----------|
| `_PartialExames.cshtml` | Campo hidden `exameRealizadoId` |
| `_PartialRequisitar.cshtml` | Grid: coluna "Id" → "Cód. Exame" |
| `_PartialRequisitar.cshtml` | `editarRequisicao` preenche `exameRealizadoId` |
| `Index.cshtml` | Reseta `exameRealizadoId` após salvamento |

---

## Fase 2 — Correção do Fluxo

### Arquitetura: Ponto Único de Autoridade

`SalvarRequisicao` = **orquestrador** que controla:
- Transação única
- Decisão inclusão vs edição (por `ExameRealizadoId`)
- Exclusão de itens antigos
- Inserção de novos itens
- Propagação de `ExameRealizadoId`

`SalvarExameRealizadoAsync` = **apenas inclusão nova**:
- Cria header `ExamesRealizados` via `Add`
- Cria `ItensExamesRealizados` vinculados ao novo header
- Retorna `ExameRealizadoId` gerado

### Fluxo de Inclusão Nova (ExameRealizadoId == 0)

```
1. CriarRequisicoes(vm) → lista de Requisitar
2. SalvarExameRealizadoAsync → cria header + itens → retorna Id
3. Propagar ExameRealizadoId a todos os Requisitar
4. PersistirDadosRequisitarAsync → INSERT Requisitar
5. Commit
```

### Fluxo de Edição (ExameRealizadoId > 0)

```
1. Validar: buscar header por ExameRealizadoId
2. Se não existir → abortar com erro
3. Atualizar header (UPDATE, sem recriar)
4. Excluir ItensExamesRealizados WHERE ExameRealizadoId = @id
5. Excluir Requisitar WHERE ExameRealizadoId = @id
6. Inserir novos ItensExamesRealizados com mesmo ExameRealizadoId
7. Inserir novos Requisitar com mesmo ExameRealizadoId
8. Commit
```

### Regras de Exclusão (Edição)

- Exclusão **somente** por `WHERE ExameRealizadoId = @id`
- **Nunca** por `PacienteId + DataIni + TabelaExamesId`
- **Nunca** excluir `ExamesRealizados` (header)
- Ponto único: toda exclusão no orquestrador
- `SalvarExameRealizadoAsync` **não exclui** nada

### Cupom

| Antes | Depois |
|-------|--------|
| `Requisitar.FirstOrDefault()?.Id` | `Requisitar.FirstOrDefault()?.ExameRealizadoId` |
| Código diferente entre cupom e grid | Mesmo código em ambos |

### Grid (GetLancamentosHoje)

| Antes | Depois |
|-------|--------|
| `GroupBy(PacienteId).Max(Id)` | `GroupBy(ExameRealizadoId)` |
| Coluna "Id" com `Requisitar.Id` | Coluna "Cód. Exame" com `ExameRealizadoId` |
| Dados antigos: Id numérico | Dados antigos: "-" (NULL) |

---

## Arquivos Alterados (Fase 2)

| Arquivo | Método/Trecho | Alteração |
|---------|---------------|-----------|
| `RequisitarController.cs` | `SalvarRequisicao` | Orquestrador com edição vs inclusão |
| `RequisitarController.cs` | `SalvarExameRealizadoAsync` | Apenas inclusão nova |
| `RequisitarController.cs` | `CarregarRequisicaoParaEdicao` | Retorna `exameRealizadoId` |
| `RequisitarController.cs` | `CupomRequisicao` | Usa `ExameRealizadoId` como código |
| `RequisitarController.cs` | `GetLancamentosHoje` | Agrupa por `ExameRealizadoId` |
| `_PartialRequisitar.cshtml` | `editarRequisicao` | Preenche `exameRealizadoId` |
| `_PartialRequisitar.cshtml` | Grid DataTables | Coluna "Cód. Exame" |
| `_PartialExames.cshtml` | Campo hidden | `exameRealizadoId` |
| `Index.cshtml` | Salvamento | Reseta `exameRealizadoId` |
| `vmRequisitar.cs` | Propriedade | `ExameRealizadoId` |
| `vmRequisitarSimplificado.cs` | Propriedade | `ExameRealizadoId` |

---

## Relacionamentos Confirmados

### Físicos (FK no banco)

| Tabela | Campo | Referência | Cascade |
|--------|-------|------------|---------|
| ItensExamesRealizados | ExameRealizadoId | ExamesRealizados(Id) | CASCADE |
| ItensExamesRealizadosAM | ExameRealizadoAMId | ExamesRealizadosAM(Id) | CASCADE |

### Lógicos (sem FK, orientado por código)

| Tabela | Campo | Referência | Tipo |
|--------|-------|------------|------|
| Requisitar | ExameRealizadoId | ExamesRealizados(Id) | INT NULL |

### Confirmação

- ✅ Requisitar **não tem** FK física com ExamesRealizados
- ✅ ItensExamesRealizados **tem** FK física com ExamesRealizados
- ✅ ItensExamesRealizadosAM **tem** FK física com ExamesRealizadosAM

---

## Compatibilidade com Dados Antigos

- `ExameRealizadoId = NULL` para registros existentes
- Grid exibe "-" para registros sem código
- Edição de registros antigos sem `ExameRealizadoId`:
  bloqueada com mensagem controlada
- Nenhuma tentativa de reconstrução automática

---

## Scripts SQL

| Script | Propósito | Localização |
|--------|-----------|-------------|
| `add_ExameRealizadoId_Requisitar.sql` | Migração bancos existentes | `Biblioteca PostgreSql/.../LABWEB7Empresas/` |
| `Tabelas_Vazias.sql` | DDL atualizado para novos bancos | `Biblioteca PostgreSql/.../LABWEB7Empresas/` |

---

## Build

```
Compilação com êxito.
    0 Aviso(s)
    0 Erro(s)
```

---

## Pendente para Teste

1. Executar `add_ExameRealizadoId_Requisitar.sql` no banco
2. Criar nova requisição → verificar `ExameRealizadoId` propagado
3. Editar requisição → verificar `ExameRealizadoId` preservado
4. Cupom → verificar código = `ExameRealizadoId`
5. Grid → verificar coluna "Cód. Exame" = `ExameRealizadoId`
6. Registro antigo → verificar "-" no grid e bloqueio na edição
