---
trigger: always
description: Pipeline de Análise Integrada entre PostgreSQL, Modelos C# e Scripts DDL para LabWeb7
---

# Steering de Análise Integrada - Qoder

## Objetivo

Manter **consistência** entre três fontes de verdade do projeto:

1. **Base de dados PostgreSQL** (estado real em execução)
2. **Modelos C# e mapeamentos EF Core** (código da aplicação)
3. **Scripts DDL** (arquivos SQL de criação/manutenção)

## Quando Usar Este Steering

Acionar quando precisar:

- ✅ Verificar se um campo foi adicionado no banco mas **NÃO** no modelo
- ✅ Confirmar se os scripts DDL refletem o estado atual do banco
- ✅ Auditar divergências após alterações manuais no banco
- ✅ Validar consistência antes de deploy ou entrega
- ✅ Investigar erros de mapeamento EF Core

## Pipeline de Análise

### Etapa 1 — Extrair Metadados da Base PostgreSQL

#### Tabelas
```sql
SELECT table_name
FROM   information_schema.tables
WHERE  table_schema = 'public'
  AND  table_type = 'BASE TABLE'
ORDER BY table_name;
```

#### Colunas com Tipos e Constraints
```sql
SELECT c.table_name,
       c.column_name,
       c.data_type,
       c.character_maximum_length,
       c.is_nullable,
       c.column_default
FROM   information_schema.columns c
WHERE  c.table_schema = 'public'
ORDER BY c.table_name, c.ordinal_position;
```

#### Chaves Primárias
```sql
SELECT tc.table_name,
       kcu.column_name,
       tc.constraint_name
FROM   information_schema.table_constraints tc
JOIN   information_schema.key_column_usage kcu
       ON tc.constraint_name = kcu.constraint_name
WHERE  tc.constraint_type = 'PRIMARY KEY'
  AND  tc.table_schema = 'public'
ORDER BY tc.table_name;
```

#### Chaves Estrangeiras
```sql
SELECT tc.table_name       AS tabela_origem,
       kcu.column_name     AS coluna_origem,
       ccu.table_name      AS tabela_destino,
       ccu.column_name     AS coluna_destino,
       tc.constraint_name
FROM   information_schema.table_constraints tc
JOIN   information_schema.key_column_usage kcu
       ON tc.constraint_name = kcu.constraint_name
JOIN   information_schema.constraint_column_usage ccu
       ON tc.constraint_name = ccu.constraint_name
WHERE  tc.constraint_type = 'FOREIGN KEY'
  AND  tc.table_schema = 'public'
ORDER BY tc.table_name;
```

#### Índices
```sql
SELECT tablename,
       indexname,
       indexdef
FROM   pg_indexes
WHERE  schemaname = 'public'
ORDER BY tablename, indexname;
```

### Etapa 2 — Extrair Metadados dos Modelos C#

#### Arquivos para Ler
- **Modelos (entidades):** `LabWebMvc.MVC/Models/*.cs`
- **Mapeamento EF Core:** `LabWebMvc.MVC/Models/db.cs` (método `OnModelCreating`)
- **ViewModels:** `LabWebMvc.MVC/ViewModel/vm*.cs` (validações e atributos)

#### Extrair de Cada Entidade
- ✅ Nome da classe → nome da tabela
- ✅ Propriedades → colunas
- ✅ Tipos C# → tipos PostgreSQL esperados
- ✅ Atributos `[StringLength]`, `[Required]` → constraints
- ✅ `HasMaxLength()`, `HasColumnType()` → mapeamento explícito
- ✅ `HasKey()`, `HasIndex()` → chaves e índices
- ✅ `HasOne/HasMany` → relacionamentos

### Mapeamento de Tipos C# → PostgreSQL

| Tipo C#          | Tipo PostgreSQL          |
|------------------|--------------------------|
| `int`            | `integer` / `serial`     |
| `long`           | `bigint` / `bigserial`   |
| `string`         | `varchar(n)` / `text`    |
| `bool`           | `boolean`                |
| `DateTime`       | `timestamp`              |
| `decimal`        | `numeric` / `decimal`    |
| `byte[]`         | `bytea`                  |
| `double`         | `double precision`       |
| `float`          | `real`                   |

### Etapa 3 — Extrair Metadados dos Scripts DDL

#### Arquivos para Ler
- `Biblioteca SQL/Base de Dados Vazio Postgresql/*.sql`
- `Biblioteca PostgreSql/Scripts Tabelas por Banco de Dados/**/*.sql`

#### Extrair de Cada CREATE TABLE
- ✅ Nome da tabela
- ✅ Colunas, tipos e tamanhos
- ✅ Constraints (PK, FK, UNIQUE)
- ✅ Valores default

### Etapa 4 — Normalizar e Comparar

#### Estrutura de Comparação (Conceitual)

```
Tabela: "Pacientes"
├── Fonte: Banco PostgreSQL
│   ├── Id: integer, serial, PK
│   ├── NomePaciente: varchar(100), NOT NULL
│   ├── CPF: varchar(14), NULL
│   └── DataNascimento: timestamp, NOT NULL
│
├── Fonte: Modelo C# (Pacientes.cs + db.cs)
│   ├── Id: int, [Key]
│   ├── NomePaciente: string, NOT NULL, HasMaxLength(100)
│   ├── CPF: string?, HasMaxLength(14)
│   └── DataNascimento: DateTime
│
├── Fonte: Script DDL (Tabelas_Vazias.sql)
│   ├── Id: SERIAL, PK
│   ├── NomePaciente: varchar(100), NOT NULL
│   ├── CPF: varchar(14)
│   └── DataNascimento: timestamp, NOT NULL
│
└── Divergências: 
    ⚠️ DataNascimento no modelo não tem NOT NULL, mas no banco tem
```

### Etapa 5 — Gerar Relatórios

#### Local do Relatório
- **Pasta:** `Documentos do Qoder/`
- **Nome:** `analise-consistencia-banco-modelo-scripts.md`

#### Seções do Relatório

1. **Resumo Executivo**
   - Total de tabelas
   - Divergências encontradas
   - Severidade (Alta/Média/Baixa)

2. **Tabelas Presentes no Banco mas Ausentes nos Modelos**
   - Lista de tabelas
   - Impacto

3. **Tabelas Presentes nos Modelos mas Ausentes no Banco**
   - Lista de tabelas
   - Impacto

4. **Divergências de Tipos (Banco × Modelo)**
   - Tabela.Coluna: tipo_banco vs tipo_modelo
   - Exemplo: `varchar(20)` vs `varchar(100)`

5. **Divergências de Tamanhos**
   - Exemplo: `varchar(20)` no script vs `varchar(100)` no banco

6. **Campos Faltando no Modelo ou no Banco**
   - Colunas extras
   - Colunas ausentes

7. **Scripts DDL Desatualizados**
   - Scripts que não refletem estado atual

8. **Sugestões de Correção**
   - SQL pronto para ALTER TABLE
   - Código C# para atualizar modelos

## Regras de Execução

### 1. NUNCA Alterar o Banco Sem Autorização
- ✅ Apenas **LER** metadados via `information_schema`
- ❌ **NUNCA** executar ALTER TABLE, DROP, CREATE sem autorização explícita

### 2. NUNCA Alterar Scripts MSSQL
- ❌ **NUNCA** alterar pasta `Base de Dados Vazio MSSQL`
- ❌ **NUNCA** alterar pasta `Scripts/` (contém scripts MSSQL originais)

### 3. Propor Correções, NÃO Executar
- ✅ Ao encontrar divergências, **PROPOR** correções
- ❌ **NÃO executar** sem confirmação do usuário

### 4. Prioridade de Correção

1. **Primeiro:** Atualizar modelos C# e mapeamentos EF Core
2. **Segundo:** Atualizar scripts DDL PostgreSQL
3. **Terceiro:** Propor ALTER TABLE para o banco (com SQL pronto)

### 5. Projeto NÃO Usa Migrations
- ❌ **NÃO** usar `Add-Migration` ou `Update-Database`
- ✅ Todas as alterações de schema são feitas via **scripts SQL manuais**

## Exemplo de Uso

### Cenário: Verificar Consistência

```
Usuário: "Verifique se o banco está consistente com os modelos"

Qoder:
1. ✅ Lê information_schema do PostgreSQL (se acessível)
2. ✅ Lê todos os Models/*.cs e db.cs (OnModelCreating)
3. ✅ Lê os scripts DDL da Biblioteca PostgreSQL
4. ✅ Compara as 3 fontes
5. ✅ Gera relatório com divergências e sugestões
```

### Cenário: Banco Não Acessível

```
Usuário: "Verifique se os modelos estão consistentes com os scripts"

Qoder:
1. ❌ PostgreSQL não está rodando
2. ✅ Lê todos os Models/*.cs e db.cs (OnModelCreating)
3. ✅ Lê os scripts DDL da Biblioteca PostgreSQL
4. ✅ Compara modelos × scripts
5. ✅ Gera relatório parcial (sem validação do banco)
```

## Checklist de Validação

Antes de considerar análise completa:

```
□ Extraiu metadados do banco (se acessível)?
□ Extraiu metadados dos modelos C#?
□ Extraiu metadados dos scripts DDL?
□ Comparou as 3 fontes?
□ Identificou todas as divergências?
□ Gerou relatório em Documentos do Qoder/?
□ Propôs correções com SQL pronto?
□ NÃO executou alterações sem autorização?
```

## Ferramentas Úteis

### Verificar se Banco Está Acessível
```csharp
try
{
    var canConnect = await _db.Database.CanConnectAsync();
    if (canConnect)
    {
        // Banco acessível - análise completa
    }
}
catch
{
    // Banco não acessível - análise parcial
}
```

### Extrair Schema via EF Core
```csharp
var model = _db.Model;
foreach (var entityType in model.GetEntityTypes())
{
    var tableName = entityType.GetTableName();
    var properties = entityType.GetProperties();
    
    foreach (var prop in properties)
    {
        Console.WriteLine($"{tableName}.{prop.Name}: {prop.GetColumnType()}");
    }
}
```

## Problemas Comuns

### 1. Campo no Banco mas Não no Modelo
```
Solução: Adicionar propriedade no model + configurar em OnModelCreating
```

### 2. Campo no Modelo mas Não no Banco
```
Solução: Criar script ALTER TABLE ADD COLUMN
```

### 3. Tipo Diferente (Banco vs Modelo)
```
Solução: 
- Se banco correto: alterar modelo C#
- Se modelo correto: alterar banco via ALTER TABLE ALTER COLUMN
```

### 4. Script DDL Desatualizado
```
Solução: Atualizar script para refletir estado atual do banco
```

---

**Steering criado por Qoder - 21/04/2026**  
*Baseado nas melhores práticas do projeto LabWeb7*
