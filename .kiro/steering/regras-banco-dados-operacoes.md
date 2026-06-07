---
inclusion: always
description: Regras obrigatórias para operações de banco de dados PostgreSQL
---

# Steering — Operações de Banco de Dados PostgreSQL

## Conexão

- Usuário: `sistema`
- Host: `127.0.0.1`
- Porta: `5432`
- Banco principal: `LABWEB7`
- Caminho psql: `C:\Program Files\PostgreSQL\18\bin\psql.exe`

## Regra de Falha de Conexão

Se a conexão falhar: **PARAR IMEDIATAMENTE**.

É proibido:
- Continuar parcialmente
- Improvisar SQL
- Gerar query especulativa
- Inferir schema
- Responder baseado em dedução

Resposta obrigatória:
> Não foi possível validar a conexão com o banco de dados do projeto.
> Operação interrompida para evitar implementação incorreta baseada
> em inferência.

## Investigação de Schema

Antes de qualquer operação de banco, investigar via:
- `information_schema.tables`
- `information_schema.columns`
- `pg_constraint`
- `pg_indexes`
- `pg_class`
- `pg_attribute`

## Regra para Query e Script SQL

Nenhum SQL pode ser produzido sem validação anterior.

Antes de entregar qualquer script:
- Validar sintaxe
- Validar tabelas
- Validar campos
- Validar relacionamentos
- Validar impacto
- Validar índices
- Validar performance básica
- Validar compatibilidade com EF Core
- Validar coerência com DbContext

## Regra Obrigatória para UPDATE e DELETE

Antes de qualquer alteração:

```sql
-- 1. Validar registros afetados
SELECT * FROM "Tabela" WHERE Condicao;

-- 2. Somente após validação
UPDATE "Tabela" SET "Campo" = Valor WHERE Condicao;
```

Para DELETE:
```sql
-- Validar impacto
SELECT * FROM "Tabela" WHERE Condicao;

-- Somente depois
DELETE FROM "Tabela" WHERE Condicao;
```

Nunca executar alteração sem pré-validação.

## Regra Obrigatória para LINQ / Entity Framework

Ao gerar LINQ ou EF Core, é obrigatório validar:
- Entidade real
- Navigation properties
- Include/ThenInclude válidos
- FK real
- Lazy loading vs eager loading
- Performance
- Possibilidade de N+1
- Comportamento do tracking
- Coerência com DbContext

Nunca gerar LINQ assumindo estrutura.

## Proibições Absolutas

É estritamente proibido:
- Inferir estrutura
- Deduzir schema
- Imaginar relacionamentos
- Inventar joins
- Assumir FK
- Assumir cardinalidade
- Assumir nomes de colunas
- Assumir nullable
- Assumir tipo de dado
- Assumir convenções
- Responder com "provavelmente"
- Responder com "deve existir"
- Responder com "aparentemente"
- Criar SQL especulativo

Toda conclusão deve ter origem em:
- Código real (DbContext, Entities, Fluent API)
- Banco real
- Metadata real
- Validação de conexão

## Ordem de Execução Obrigatória

A sequência correta é irrevogável:
1. Investigar código do sistema
2. Validar DbContext
3. Validar entities
4. Validar relacionamentos ORM
5. Conectar ao banco
6. Validar conexão
7. Investigar schema real
8. Validar dados
9. Somente então gerar SQL/LINQ/script
10. Validar impacto
11. Executar

Nunca inverter esta ordem.

## Regra Final

Em qualquer incerteza técnica relacionada ao banco:
**NUNCA inferir. Sempre investigar.**

Se não houver evidência concreta no código ou no banco:
**PARAR e informar a limitação.**

Resposta obrigatória:
> Não foi possível validar com segurança a estrutura real do
> sistema e/ou banco de dados. Operação interrompida para evitar
> implementação incorreta baseada em inferência.
