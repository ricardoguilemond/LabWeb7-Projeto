---
inclusion: manual
description: Pipeline de análise integrada entre PostgreSQL, modelos C# e scripts DDL
---

# Steering — Análise Integrada: Banco × Modelos × Scripts

## Objetivo

Manter consistência entre três fontes de verdade do projeto:

1. **Base de dados PostgreSQL** (estado real em execução)
2. **Modelos C# e mapeamentos EF Core** (código da aplicação)
3. **Scripts DDL** (arquivos SQL de criação/manutenção)

## Quando usar este steering

Acionar manualmente (`#analise-integrada-banco-modelo-scripts`)
quando precisar:

- Verificar se um campo foi adicionado no banco mas não no modelo
- Confirmar se os scripts DDL refletem o estado atual do banco
- Auditar divergências após alterações manuais no banco
- Validar consistência antes de deploy ou entrega

---

## Pipeline de Análise

### Etapa 1 — Extrair metadados da base PostgreSQL

Consultar `information_schema` e `pg_catalog` para obter:

```sql
-- Tabelas
SELECT table_name
FROM   information_schema.tables
WHERE  table_schema = 'public'
  AND  table_type = 'BASE TABLE'
ORDER BY table_name;

-- Colunas com tipos e constraints
SELECT c.table_name,
       c.column_name,
       c.data_type,
       c.character_maximum_length,
       c.is_nullable,
       c.column_default
FROM   information_schema.columns c
WHERE  c.table_schema = 'public'
ORDER BY c.table_name, c.ordinal_position;

-- Chaves primárias
SELECT tc.table_name,
       kcu.column_name,
       tc.constraint_name
FROM   information_schema.table_constraints tc
JOIN   information_schema.key_column_usage kcu
       ON tc.constraint_name = kcu.constraint_name
WHERE  tc.constraint_type = 'PRIMARY KEY'
  AND  tc.table_schema = 'public'
ORDER BY tc.table_name;

-- Chaves estrangeiras
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

-- Índices
SELECT tablename,
       indexname,
       indexdef
FROM   pg_indexes
WHERE  schemaname = 'public'
ORDER BY tablename, indexname;
```

### Etapa 2 — Extrair metadados dos modelos C#

Ler os seguintes arquivos do projeto:

**Modelos (entidades):**
- `LabWebMvc.MVC/Models/*.cs` — classes de entidade
- `ModeloDeDados/Models/*.cs` — classes de entidade (referência)

**Mapeamento EF Core (OnModelCreating):**
- `LabWebMvc.MVC/Models/db.cs` — método `OnModelCreating`

**ViewModels:**
- `LabWebMvc.MVC/ViewModel/vm*.cs` — validações e atributos

Para cada entidade, extrair:
- Nome da classe → nome da tabela
- Propriedades → colunas
- Tipos C# → tipos PostgreSQL esperados
- Atributos `[StringLength]`, `[Required]` → constraints
- `HasMaxLength()`, `HasColumnType()` → mapeamento explícito
- `HasKey()`, `HasIndex()` → chaves e índices
- `HasOne/HasMany` → relacionamentos

**Mapeamento de tipos C# → PostgreSQL:**

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

### Etapa 3 — Extrair metadados dos scripts DDL

Ler os arquivos SQL das pastas:

- `Biblioteca SQL/Base de Dados Vazio Postgresql/*.sql`
- `Biblioteca PostgreSql/Scripts Tabelas por Banco de Dados/**/*.sql`

Para cada `CREATE TABLE`, extrair:
- Nome da tabela
- Colunas, tipos e tamanhos
- Constraints (PK, FK, UNIQUE)
- Valores default

### Etapa 4 — Normalizar e comparar

Estrutura comum para comparação (conceitual):

```
Tabela: "Medicos"
├── Fonte: Banco PostgreSQL
│   ├── Id: integer, serial, PK
│   ├── NomeMedico: varchar(100), NOT NULL
│   ├── Especialidade: varchar(100), NULL
│   ├── CRM: varchar(15), NOT NULL
│   ├── Telefone: varchar(15), NULL
│   └── Email: varchar(100), NULL
├── Fonte: Modelo C# (Medicos.cs + db.cs)
│   ├── Id: int, [Key]
│   ├── NomeMedico: string, NOT NULL, HasMaxLength(100)
│   ├── Especialidade: string?, HasMaxLength(100)
│   ├── CRM: string, NOT NULL
│   ├── Telefone: string?, HasMaxLength(15)
│   └── Email: string?, HasMaxLength(100)
├── Fonte: Script DDL (Tabelas_Vazias.sql)
│   ├── Id: SERIAL, PK
│   ├── NomeMedico: varchar(100), NOT NULL
│   ├── Especialidade: varchar(100)
│   ├── CRM: varchar(15), NOT NULL
│   ├── Telefone: varchar(15)
│   └── Email: varchar(100)
└── Divergências: nenhuma
```

### Etapa 5 — Gerar relatórios

Produzir relatório em Markdown na pasta `Documentos do Kiro/`
com o nome `analise-consistencia-banco-modelo-scripts.md`:

**Seções do relatório:**

1. Resumo executivo (total de tabelas, divergências encontradas)
2. Tabelas presentes no banco mas ausentes nos modelos
3. Tabelas presentes nos modelos mas ausentes no banco
4. Divergências de tipos (banco × modelo)
5. Divergências de tamanhos (ex: varchar(20) no script vs varchar(100) no banco)
6. Campos faltando no modelo ou no banco
7. Scripts DDL desatualizados
8. Sugestões de correção

---

## Regras de Execução

1. **Nunca alterar o banco de dados** sem autorização explícita.
   Apenas ler metadados via `information_schema`.
2. **Nunca alterar scripts da pasta MSSQL** (`Base de Dados Vazio MSSQL`).
3. **Nunca alterar a pasta `Scripts/`** (contém scripts MSSQL originais).
4. Ao encontrar divergências, **propor correções** mas não executar
   sem confirmação do usuário.
5. Prioridade de correção:
   - Primeiro: atualizar modelos C# e mapeamentos EF Core
   - Segundo: atualizar scripts DDL PostgreSQL
   - Terceiro: propor ALTER TABLE para o banco (com SQL pronto)
6. O projeto **não usa Migrations** — todas as alterações de schema
   são feitas via scripts SQL manuais.

---

## Exemplo de Uso

Quando o usuário solicitar análise de consistência:

```
Usuário: "Verifique se o banco está consistente com os modelos"

Kiro:
1. Lê information_schema do PostgreSQL (se acessível)
2. Lê todos os Models/*.cs e db.cs (OnModelCreating)
3. Lê os scripts DDL da Biblioteca PostgreSQL
4. Compara as 3 fontes
5. Gera relatório com divergências e sugestões
```

Se o banco não estiver acessível (ex: PostgreSQL não rodando),
a análise pode ser feita apenas entre modelos C# e scripts DDL.
