# Diagnóstico de Relacionamento — 5 Tabelas de Exames

**Data:** 07/06/2025
**Escopo:** ExamesRealizados, ItensExamesRealizados, Requisitar,
ExamesRealizadosAM, ItensExamesRealizadosAM
**Objetivo:** Mapear estrutura, relacionamentos e inconsistências
entre modelos C#, Fluent API (EF Core) e DDL PostgreSQL.

---

## Seção 1: Estrutura Atual das Tabelas

### 1.1 ExamesRealizados (Header do exame)

**Arquivo modelo:** `ModeloDeDados/Models/ExamesRealizados.cs`

| Propriedade        | Tipo       | Nullable | Observação          |
|--------------------|------------|----------|---------------------|
| Id                 | int        | Não      | PK (SERIAL)         |
| PacienteId         | int        | Não      | FK → Pacientes      |
| TabelaExamesId     | int        | Não      | FK → TabelaExames   |
| InstituicaoId      | int        | Não      | FK → Instituicao    |
| PostoId            | int        | Não      | FK → Postos         |
| MedicoId           | int        | Não      | FK → Medicos        |
| ClasseExamesId     | int        | Não      | FK → ClasseExames   |
| Sequencial         | int        | Não      |                     |
| LaboratorioApoio   | string?    | Sim      | varchar(20)         |
| ControleApoio      | string     | Não      | varchar(20)         |
| HistoricoClinico   | string?    | Sim      | varchar(2000)       |
| ExameColado        | string?    | Sim      | varchar(250)        |
| ExameColadoImagens | string?    | Sim      | varchar(250)        |
| TravaColado        | int        | Não      |                     |
| DataIni            | DateTime   | Não      | TIMESTAMP no DDL    |
| DataFim            | DateTime?  | Sim      | TIMESTAMP no DDL    |
| Liberacao          | int        | Não      |                     |
| DataExame          | DateTime?  | Sim      | TIMESTAMP no DDL    |
| DataColeta         | string?    | Sim      | varchar(10)         |
| DataEntrega        | DateTime?  | Sim      | TIMESTAMP no DDL    |
| Baixado            | int        | Não      |                     |
| EnviarEmail        | int        | Não      |                     |
| Situacao           | int        | Não      |                     |
| TotalImpresso      | int        | Não      |                     |

**PK:** `Id` (constraint `iExamesRealizados1`)
**ExameRealizadoId existe?** Não (esta é a tabela header)

**Navegações:**
- `ClasseExames` → ClasseExames (1:N)
- `Instituicao` → Instituicao (1:N)
- `Medicos` → Medicos (1:N)
- `Pacientes` → Pacientes (1:N)
- `Postos` → Postos (1:N)
- `TabelaExames` → TabelaExames (1:N)
- `ItensExamesRealizados` → coleção (1:N)
- `ExamesExportados` → coleção (1:N)
- `FichasInternas` → coleção (1:N)
- `FichasLotes` → coleção (1:N)
- `FichasPlanilhas` → coleção (1:N)

⚠️ **INCONSISTÊNCIA:** `ClasseExamesId` existe no modelo C# e no
Fluent API, mas **NÃO existe** no DDL PostgreSQL.

---

### 1.2 ItensExamesRealizados (Itens/detalhes do exame)

**Arquivo modelo:** `ModeloDeDados/Models/ItensExamesRealizados.cs`

| Propriedade        | Tipo       | Nullable | Observação          |
|--------------------|------------|----------|---------------------|
| Id                 | int        | Não      | PK (SERIAL)         |
| PacienteId         | int        | Não      | FK → Pacientes      |
| ClasseExamesId     | int        | Não      | FK → ClasseExames   |
| ClasseExamesNome   | string     | Não      | varchar(50)         |
| ExameRealizadoId   | int        | Não      | FK → ExamesRealiz.  |
| TabelaExamesId     | int        | Não      | FK → TabelaExames   |
| OrdemItem          | int        | Não      |                     |
| RefExame           | string     | Não      | varchar(50)         |
| RefItem            | string     | Não      | varchar(50)         |
| ContaExame         | string     | Não      | varchar(11)         |
| CitoTituloFolha    | int        | Não      |                     |
| CitoTituloExame    | int        | Não      |                     |
| CitoRefItem        | int        | Não      |                     |
| InstituicaoId      | int        | Não      | FK → Instituicao    |
| Sequencial         | int        | Não      |                     |
| LaboratorioApoio   | string?    | Sim      | varchar(20)         |
| ControleApoio      | string?    | Sim      | varchar(20)         |
| LaboratorioExterno | string?    | Sim      | varchar(20)         |
| MaterialSaida      | string?    | Sim      | varchar(16)         |
| MaterialRetorno    | string?    | Sim      | varchar(16)         |
| Descricao          | string?    | Sim      | varchar(50)         |
| CitoDescricao      | string?    | Sim      | varchar(2000)       |
| Resultado          | string?    | Sim      | varchar(30)         |
| UnidadeMedida      | string?    | Sim      | varchar(20)         |
| Referencia         | string?    | Sim      | varchar(60)         |
| ValorItem          | decimal?   | Sim      | DECIMAL(18,4)       |
| Laudo              | byte[]?    | Sim      | BYTEA               |
| Etiquetas          | int        | Não      |                     |
| DataEntregaParcial | DateTime?  | Sim      | TIMESTAMP no DDL    |
| Liberado           | int        | Não      |                     |
| Baixado            | int        | Não      |                     |

**PK:** `Id` (constraint `iItensExamesRealizados1`)
**ExameRealizadoId existe?** ✅ SIM — FK → ExamesRealizados(Id)

**Navegações:**
- `ClasseExames` → ClasseExames (1:N)
- `ExamesRealizados` → ExamesRealizados (N:1)
- `Instituicao` → Instituicao (1:N)
- `Pacientes` → Pacientes (1:N)
- `TabelaExames` → TabelaExames (1:N)

---

### 1.3 Requisitar (Cópia/backup operacional)

**Arquivos modelo:**
- `ModeloDeDados/Models/Requisitar.cs` (modelo principal)
- `LabWebMvc.MVC/Models/Requisitar.cs` (partial class MVC)

| Propriedade        | Tipo       | Nullable | ModeloDeDados | MVC   |
|--------------------|------------|----------|---------------|-------|
| Id                 | int        | Não      | ✅            | ✅    |
| PacienteId         | int        | Não      | ✅            | ✅    |
| ClasseExamesId     | int        | Não      | ✅            | ✅    |
| ClasseExamesNome   | string     | Não      | ✅            | ✅    |
| ExameId            | int        | Não      | ✅            | ✅    |
| OrdemItem          | int        | Não      | ✅            | ✅    |
| RefExame           | string?    | Sim      | ✅            | ✅    |
| RefItem            | string?    | Sim      | ✅            | ✅    |
| ContaExame         | string     | Não      | ✅            | ✅    |
| InstituicaoId      | int        | Não      | ✅            | ✅    |
| PostoId            | int?       | —        | ❌ AUSENTE    | ✅    |
| TabelaExamesId     | int        | Não      | ✅            | ✅    |
| MedicoId           | int        | Não      | ✅            | ✅    |
| LaboratorioApoio   | string?    | Sim      | ✅            | ✅    |
| ControleApoio      | string?    | Sim      | ✅            | ✅    |
| LaboratorioExterno | string?    | Sim      | ✅            | ✅    |
| MaterialSaida      | string?    | Sim      | ✅            | ✅    |
| MaterialRetorno    | string?    | Sim      | ✅            | ✅    |
| Descricao          | string?    | Sim      | ✅            | ✅    |
| Resultado          | string?    | Sim      | ✅            | ✅    |
| UnidadeMedida      | string?    | Sim      | ✅            | ✅    |
| Referencia         | string?    | Sim      | ✅            | ✅    |
| ValorItem          | decimal?   | Sim      | ✅            | ✅    |
| Laudo              | byte[]?    | Sim      | ✅            | ✅    |
| Etiquetas          | int        | Não      | ✅            | ✅    |
| DataIni            | DateTime   | Não      | ✅            | ✅    |
| DataEntregaParcial | DateTime?  | Sim      | ✅            | ✅    |
| Liberado           | int        | Não      | ✅            | ✅    |
| Baixado            | int        | Não      | ✅            | ✅    |

**PK:** `Id` (constraint `iRequisitar1`)
**ExameRealizadoId existe?** ❌ NÃO — Requisitar não tem FK para
ExamesRealizados.

**Navegações (ModeloDeDados):**
- `ClasseExames`, `Instituicao`, `Medicos`, `Pacientes`,
  `TabelaExames`
- ❌ Sem `Postos` (PostoId ausente)

**Navegações (MVC partial):**
- `ClasseExames`, `Instituicao`, `Posto` (nullable),
  `Medicos`, `Pacientes`, `TabelaExames`

⚠️ **INCONSISTÊNCIA:** `PostoId` existe no DDL e no MVC partial,
mas **NÃO existe** no ModeloDeDados/Models/Requisitar.cs.

---

### 1.4 ExamesRealizadosAM (Header — Amostra)

**Arquivo modelo:** `ModeloDeDados/Models/Examesrealizadosam.cs`

| Propriedade        | Tipo       | Nullable | Observação          |
|--------------------|------------|----------|---------------------|
| Id                 | int        | Não      | PK (SERIAL)         |
| OrigemId           | int        | Não      | Sem FK no DDL       |
| PacienteId         | int        | Não      | FK → Pacientes      |
| TabelaExamesId     | int        | Não      | FK → TabelaExames   |
| InstituicaoId      | int        | Não      | FK → Instituicao    |
| PostoId            | int        | Não      | FK → Postos         |
| MedicoId           | int        | Não      | FK → Medicos        |
| ClasseExamesId     | int        | Não      | FK → ClasseExames   |
| Sequencial         | int        | Não      |                     |
| LaboratorioApoio   | string?    | Sim      | varchar(20)         |
| ControleApoio      | string     | Não      | varchar(20)         |
| HistoricoClinico   | string?    | Sim      | varchar(2000)       |
| ExameColado        | string?    | Sim      | varchar(250)        |
| ExameColadoImagens | string?    | Sim      | varchar(250)        |
| TravaColado        | int        | Não      |                     |
| DataIni            | DateTime   | Não      | TIMESTAMP no DDL    |
| DataFim            | DateTime?  | Sim      | TIMESTAMP no DDL    |
| Liberacao          | int        | Não      |                     |
| DataExame          | DateTime?  | Sim      | TIMESTAMP no DDL    |
| DataColeta         | string?    | Sim      | varchar(10)         |
| DataEntrega        | DateTime?  | Sim      | TIMESTAMP no DDL    |
| Baixado            | int        | Não      |                     |
| EnviarEmail        | int        | Não      |                     |
| Situacao           | int        | Não      |                     |
| TotalImpresso      | int        | Não      |                     |

**PK:** `Id` (constraint `iExamesRealizadosAM1`)
**ExameRealizadoId existe?** Não (esta é a tabela header AM)

**Navegações:**
- `ClasseExames`, `Instituicao`, `Medicos`, `Pacientes`,
  `Postos`, `TabelaExames`
- `ItensExamesRealizadosAM` → coleção (1:N)

⚠️ **INCONSISTÊNCIA:** `ClasseExamesId` existe no modelo C# e no
Fluent API, mas **NÃO existe** no DDL PostgreSQL.

---

### 1.5 ItensExamesRealizadosAM (Itens — Amostra)

**Arquivo modelo:** `ModeloDeDados/Models/Itensexamesrealizadosam.cs`

| Propriedade        | Tipo       | Nullable | Observação          |
|--------------------|------------|----------|---------------------|
| Id                 | int        | Não      | PK (SERIAL)         |
| OrigemAmid         | int        | Não      | Coluna: OrigemAMId  |
| PacienteId         | int        | Não      | FK → Pacientes      |
| ClasseExamesId     | int        | Não      | FK → ClasseExames   |
| ClasseExamesNome   | string     | Não      | varchar(50)         |
| ExameRealizadoAMId | int        | Não      | FK → ExamesRealAM   |
| TabelaExamesId     | int        | Não      | FK → TabelaExames   |
| OrdemItem          | int        | Não      |                     |
| RefExame           | string     | Não      | varchar(50)         |
| RefItem            | string     | Não      | varchar(50)         |
| ContaExame         | string     | Não      | varchar(11)         |
| CitoTituloFolha    | int        | Não      |                     |
| CitoTituloExame    | int        | Não      |                     |
| CitoRefItem        | int        | Não      |                     |
| InstituicaoId      | int        | Não      | FK → Instituicao    |
| Sequencial         | int        | Não      |                     |
| LaboratorioApoio   | string?    | Sim      | varchar(20)         |
| ControleApoio      | string?    | Sim      | varchar(20)         |
| LaboratorioExterno | string?    | Sim      | varchar(20)         |
| MaterialSaida      | string?    | Sim      | varchar(16)         |
| MaterialRetorno    | string?    | Sim      | varchar(16)         |
| Descricao          | string?    | Sim      | varchar(50)         |
| CitoDescricao      | string?    | Sim      | varchar(2000)       |
| Resultado          | string?    | Sim      | varchar(30)         |
| UnidadeMedida      | string?    | Sim      | varchar(20)         |
| Referencia         | string?    | Sim      | varchar(60)         |
| ValorItem          | decimal?   | Sim      | DECIMAL(18,4)       |
| Laudo              | byte[]?    | Sim      | BYTEA               |
| Etiquetas          | int        | Não      |                     |
| DataEntregaParcial | DateTime?  | Sim      | TIMESTAMP no DDL    |
| Liberado           | int        | Não      |                     |
| Baixado            | int        | Não      |                     |

**PK:** `Id` (constraint `iItensExamesRealizadosAM1`)
**ExameRealizadoAMId existe?** ✅ SIM — FK → ExamesRealizadosAM(Id)

**Navegações:**
- `ClasseExames`, `ExamesRealizadosAM`, `Instituicao`,
  `Pacientes`, `TabelaExames`

---

## Seção 2: Relacionamentos Configurados no EF Core

### 2.1 Fluent API — ModeloDeDados/Models/db.cs

#### ExamesRealizados (linhas 807–865)

```
HasKey(e => e.Id).HasName("iExamesRealizados1")
HasOne(ClasseExames)  → FK: ClasseExamesId  → constraint: iExamesRealizados_ClasseExames
HasOne(Instituicao)   → FK: InstituicaoId   → constraint: iExamesRealizados_Instituicao
HasOne(Medicos)       → FK: MedicoId        → constraint: iExamesRealizados_Medicos
HasOne(Pacientes)     → FK: PacienteId      → constraint: iExamesRealizados_Pacientes
HasOne(Postos)        → FK: PostoId         → constraint: iExamesRealizados_Postos
HasOne(TabelaExames)  → FK: TabelaExamesId  → constraint: iExamesRealizados_TabelaExames
```

⚠️ `ClasseExamesId` configurado no Fluent API mas **ausente no DDL**.

#### ExamesRealizadosAM (linhas 866–925)

```
HasKey(e => e.Id).HasName("iExamesRealizadosAM1")
HasOne(ClasseExames)  → FK: ClasseExamesId  → constraint: iExamesRealizadosAM_ClasseExames
HasOne(Instituicao)   → FK: InstituicaoId   → constraint: iExamesRealizadosAM_Instituicao
HasOne(Medicos)       → FK: MedicoId        → constraint: iExamesRealizadosAM_Medicos
HasOne(Pacientes)     → FK: PacienteId      → constraint: iExamesRealizadosAM_Pacientes
HasOne(Postos)        → FK: PostoId         → constraint: iExamesRealizadosAM_Postos
HasOne(TabelaExames)  → FK: TabelaExamesId  → constraint: iExamesRealizadosAM_TabelaExames
```

⚠️ `ClasseExamesId` configurado no Fluent API mas **ausente no DDL**.

#### ItensExamesRealizados (linhas 1371–1445)

```
HasKey(e => e.Id).HasName("iItensExamesRealizados1")
HasOne(ClasseExames)      → FK: ClasseExamesId   → constraint: iItensExamesRealizados_ClasseExames
HasOne(ExamesRealizados)  → FK: ExameRealizadoId  → constraint: iItensExamesRealizados_ExamesRealizados
HasOne(Instituicao)       → FK: InstituicaoId     → constraint: iItensExamesRealizados_Instituicao
HasOne(Pacientes)         → FK: PacienteId        → constraint: iItensExamesRealizados_Pacientes
HasOne(TabelaExames)      → FK: TabelaExamesId    → constraint: iItensExamesRealizados_TabelaExames
```

✅ FK `ExameRealizadoId → ExamesRealizados(Id)` configurada
corretamente. DDL inclui `ON DELETE CASCADE ON UPDATE CASCADE`.

#### ItensExamesRealizadosAM (linhas 1446–1522)

```
HasKey(e => e.Id).HasName("iItensExamesRealizadosAM1")
HasOne(ClasseExames)        → FK: ClasseExamesId     → constraint: iItensExamesRealizadosAM1_ClasseExames
HasOne(ExamesRealizadosAM)  → FK: ExameRealizadoAMId  → constraint: iItensExamesRealizadosAM1_ExamesRealizados
HasOne(Instituicao)         → FK: InstituicaoId       → constraint: iItensExamesRealizadosAM1_Instituicao
HasOne(Pacientes)           → FK: PacienteId          → constraint: iItensExamesRealizadosAM1_Pacientes
HasOne(TabelaExames)        → FK: TabelaExamesId      → constraint: iItensExamesRealizadosAM1_TabelaExames
```

✅ FK `ExameRealizadoAMId → ExamesRealizadosAM(Id)` configurada
corretamente. DDL inclui `ON DELETE CASCADE ON UPDATE CASCADE`.

Nota: `ExameRealizadoAMId` tem mapeamento explícito de coluna:
`entity.Property(e => e.ExameRealizadoAMId).HasColumnName("ExameRealizadoAMId")`

#### Requisitar (linhas 1835–1907)

```
HasKey(e => e.Id).HasName("iRequisitar1")
HasOne(ClasseExames)  → FK: ClasseExamesId  → constraint: iRequisitar_ClasseExames
HasOne(Instituicao)   → FK: InstituicaoId   → constraint: iRequisitar_Instituicao
HasOne(Medicos)       → FK: MedicoId        → constraint: iRequisitar_Medicos
HasOne(Pacientes)     → FK: PacienteId      → constraint: iRequisitar_Pacientes
HasOne(TabelaExames)  → FK: TabelaExamesId  → constraint: iRequisitar_TabelaExames
```

⚠️ **Sem FK para Postos** no ModeloDeDados db.cs.
⚠️ **Sem FK para ExamesRealizados** — Requisitar é tabela
independente (backup operacional), sem vínculo direto com
ExamesRealizados.

### 2.2 Fluent API — LabWebMvc.MVC/Models/db.cs

A configuração do MVC db.cs é similar ao ModeloDeDados, com
as seguintes diferenças para Requisitar (linhas 2132–2213):

```
HasOne(Posto)  → FK: PostoId  → constraint: iRequisitar_Postos
               → IsRequired(false)
               → OnDelete(ClientSetNull)
```

Também inclui:
```
entity.Property(e => e.PostoId).IsRequired(false);
entity.Property(e => e.DataIni).HasColumnType("timestamp with time zone");
entity.Property(e => e.DataEntregaParcial).HasColumnType("timestamp with time zone");
```

⚠️ O MVC db.cs tem `PostoId` como nullable e FK para Postos,
enquanto o ModeloDeDados db.cs **não tem**.

### 2.3 Relacionamentos Ausentes

| Relacionamento                          | Status           |
|-----------------------------------------|------------------|
| Requisitar → ExamesRealizados           | ❌ NÃO EXISTE    |
| Requisitar → Postos (ModeloDeDados)     | ❌ NÃO EXISTE    |
| ExamesRealizados.ClasseExamesId (DDL)   | ❌ COLUNA AUSENTE|
| ExamesRealizadosAM.ClasseExamesId (DDL) | ❌ COLUNA AUSENTE|

---

## Seção 3: Relacionamentos no DDL (PostgreSQL)

**Arquivo:** `Biblioteca PostgreSql/Tabelas_Vazias.sql`

### 3.1 ExamesRealizados (linhas 260–291)

| Constraint                          | Tipo | Referência         |
|-------------------------------------|------|--------------------|
| iExamesRealizados1                  | PK   | (Id)               |
| iExamesRealizados_Pacientes         | FK   | Pacientes(Id)      |
| iExamesRealizados_TabelaExames      | FK   | TabelaExames(Id)   |
| iExamesRealizados_Instituicao       | FK   | Instituicao(Id)    |
| iExamesRealizados_Medicos           | FK   | Medicos(Id)        |
| iExamesRealizados_Postos            | FK   | Postos(Id)         |

❌ **AUSENTE:** Coluna `ClasseExamesId` e FK para ClasseExames.
O modelo C# e o Fluent API declaram essa coluna e FK, mas o DDL
não a possui. Isso pode causar erro em runtime se o EF Core
tentar inserir/consultar essa coluna.

### 3.2 ExamesRealizadosAM (linhas 293–326)

| Constraint                          | Tipo | Referência         |
|-------------------------------------|------|--------------------|
| iExamesRealizadosAM1                | PK   | (Id)               |
| iExamesRealizadosAM_Pacientes       | FK   | Pacientes(Id)      |
| iExamesRealizadosAM_TabelaExames    | FK   | TabelaExames(Id)   |
| iExamesRealizadosAM_Instituicao     | FK   | Instituicao(Id)    |
| iExamesRealizadosAM_Medicos         | FK   | Medicos(Id)        |
| iExamesRealizadosAM_Postos          | FK   | Postos(Id)         |

❌ **AUSENTE:** Coluna `ClasseExamesId` e FK para ClasseExames.
Mesma inconsistência de ExamesRealizados.

### 3.3 ItensExamesRealizados (linhas 491–531)

| Constraint                                    | Tipo | Referência              | Cascade |
|-----------------------------------------------|------|-------------------------|---------|
| iItensExamesRealizados1                       | PK   | (Id)                    | —       |
| iItensExamesRealizados_ExamesRealizados       | FK   | ExamesRealizados(Id)    | CASCADE |
| iItensExamesRealizados_Pacientes              | FK   | Pacientes(Id)           | —       |
| iItensExamesRealizados_ClasseExames           | FK   | ClasseExames(Id)        | —       |
| iItensExamesRealizados_TabelaExames           | FK   | TabelaExames(Id)        | —       |
| iItensExamesRealizados_Instituicao            | FK   | Instituicao(Id)         | —       |

✅ FK `ExameRealizadoId → ExamesRealizados(Id)` existe no DDL
com `ON DELETE CASCADE ON UPDATE CASCADE`.

### 3.4 ItensExamesRealizadosAM (linhas 532–572)

| Constraint                                    | Tipo | Referência              | Cascade |
|-----------------------------------------------|------|-------------------------|---------|
| iItensExamesRealizadosAM1                     | PK   | (Id)                    | —       |
| iItensExamesRealizadosAM1_ExamesRealizados    | FK   | ExamesRealizadosAM(Id)  | CASCADE |
| iItensExamesRealizadosAM1_Pacientes           | FK   | Pacientes(Id)           | —       |
| iItensExamesRealizadosAM1_ClasseExames        | FK   | ClasseExames(Id)        | —       |
| iItensExamesRealizadosAM1_TabelaExames        | FK   | TabelaExames(Id)        | —       |
| iItensExamesRealizadosAM1_Instituicao         | FK   | Instituicao(Id)         | —       |

✅ FK `ExameRealizadoAMId → ExamesRealizadosAM(Id)` existe no DDL
com `ON DELETE CASCADE ON UPDATE CASCADE`.

### 3.5 Requisitar (linhas 648–687)

| Constraint                    | Tipo | Referência         |
|-------------------------------|------|--------------------|
| iRequisitar1                  | PK   | (Id)               |
| iRequisitar_Pacientes         | FK   | Pacientes(Id)      |
| iRequisitar_TabelaExames      | FK   | TabelaExames(Id)   |
| iRequisitar_Instituicao       | FK   | Instituicao(Id)    |
| iRequisitar_Medicos           | FK   | Medicos(Id)        |
| iRequisitar_Postos            | FK   | Postos(Id)         |
| iRequisitar_ClasseExames      | FK   | ClasseExames(Id)   |

✅ DDL tem `PostoId` e FK `iRequisitar_Postos`.
❌ **Sem FK para ExamesRealizados** — Requisitar é independente.

### 3.6 Constraints Ausentes no DDL

| Tabela              | Constraint Ausente                    | Impacto              |
|---------------------|---------------------------------------|----------------------|
| ExamesRealizados    | ClasseExamesId (coluna + FK)          | CRÍTICO              |
| ExamesRealizadosAM  | ClasseExamesId (coluna + FK)          | CRÍTICO              |
| Requisitar          | FK → ExamesRealizados                 | Design intencional   |

---

## Seção 4: Uso de ExameRealizadoId no Código

| Arquivo                                       | Linha | Contexto                                          | Operação |
|-----------------------------------------------|-------|---------------------------------------------------|----------|
| ModeloDeDados/Models/ItensExamesRealizados.cs | 16    | `public int ExameRealizadoId { get; set; }`       | MODELO   |
| LabWebMvc.MVC/Models/Itensexamesrealizados.cs | 13    | `public int ExameRealizadoId { get; set; }`       | MODELO   |
| ModeloDeDados/Models/db.cs                    | 1427  | `.HasForeignKey(d => d.ExameRealizadoId)`         | CONFIG   |
| LabWebMvc.MVC/Models/db.cs                   | 1696  | `.HasForeignKey(d => d.ExameRealizadoId)`         | CONFIG   |
| RequisitarController.cs                       | 492   | `ExameRealizadoId = exame.Id,`                    | INSERT   |
| RequisitarController.cs                       | 613   | `.Where(i => idsExames.Contains(i.ExameRealizadoId))` | SELECT   |

**Resumo:** `ExameRealizadoId` é usado exclusivamente na tabela
`ItensExamesRealizados` como FK para `ExamesRealizados.Id`.
É atribuído no INSERT (linha 492) após salvar o header
`ExamesRealizados` e usado no SELECT (linha 613) para buscar
itens a excluir durante edição.

---

## Seção 5: Uso de Requisitar.Id como Código do Exame

| Arquivo                   | Linha | Contexto                                          | Problema                    |
|---------------------------|-------|---------------------------------------------------|-----------------------------|
| RequisitarController.cs   | 1371  | `string codigoExame = exames.FirstOrDefault()?.Id` | Usa Requisitar.Id como código|
| RequisitarController.cs   | 1419  | `$"CÓDIGO DE EXAME Nº {codigoExame}"`             | Imprime no cupom            |
| RequisitarController.cs   | 1457  | `servico.Executar(codigoExame)`                   | Passa para impressão        |
| _PartialRequisitar.cshtml | 166   | `{ data: 'id', width: '50px' }`                   | Exibe Id no grid            |
| RequisitarController.cs   | 1508  | `Id = r.Id` (GetLancamentosHoje)                  | Retorna Id ao grid          |

**Análise do problema:**

O `Requisitar.Id` é um SERIAL auto-incremento da tabela
`Requisitar`. Ele **NÃO** é o mesmo que `ExamesRealizados.Id`.
Quando o cupom imprime "CÓDIGO DE EXAME Nº X", o X é o Id da
tabela Requisitar, não o Id do exame realizado.

Isso significa que:
1. O "código do exame" no cupom é o Id da requisição, não do
   exame propriamente dito.
2. Se o paciente buscar o resultado pelo "código" impresso no
   cupom, esse código não corresponde ao `ExamesRealizados.Id`.
3. O grid de requisições do dia exibe `Requisitar.Id` na
   primeira coluna, que é o Id da requisição.

---

## Seção 6: Diagnóstico

### 6.1 Problemas Encontrados

#### PROBLEMA 1 — ClasseExamesId ausente no DDL de ExamesRealizados
- **Gravidade:** CRÍTICA
- **Onde:** DDL `Tabelas_Vazias.sql` linhas 260–291
- **O que:** A coluna `ClasseExamesId` existe no modelo C#
  (`ExamesRealizados.cs` linha 19) e no Fluent API
  (`db.cs` linha 837), mas **NÃO existe** no DDL PostgreSQL.
- **Impacto:** Se o banco foi criado pelo DDL, a coluna não
  existe fisicamente. O EF Core tentará fazer INSERT/SELECT
  nessa coluna e receberá erro do PostgreSQL. Porém, o código
  de criação em `SalvarExameRealizadoAsync` (linha 459) **não
  atribui** `ClasseExamesId` ao criar `ExamesRealizados`,
  o que sugere que o campo pode ter sido adicionado ao modelo
  mas nunca ao DDL.
- **Risco:** Erro em runtime em qualquer query que inclua
  `ClasseExamesId` de `ExamesRealizados` (ex: Include,
  Where, OrderBy).

#### PROBLEMA 2 — ClasseExamesId ausente no DDL de ExamesRealizadosAM
- **Gravidade:** CRÍTICA
- **Onde:** DDL `Tabelas_Vazias.sql` linhas 293–326
- **O que:** Mesma situação do Problema 1, mas para a tabela AM.
- **Impacto:** Idêntico ao Problema 1.

#### PROBLEMA 3 — PostoId ausente no ModeloDeDados Requisitar.cs
- **Gravidade:** ALTA
- **Onde:** `ModeloDeDados/Models/Requisitar.cs`
- **O que:** A propriedade `PostoId` existe no DDL (linha 662),
  no MVC partial (`LabWebMvc.MVC/Models/Requisitar.cs` linha 11),
  e no MVC Fluent API (linha 2207), mas **NÃO existe** no
  `ModeloDeDados/Models/Requisitar.cs`.
- **Impacto:** Divergência entre os dois modelos partial.
  O ModeloDeDados é o modelo base usado pelo scaffold. Se
  algum código referenciar `PostoId` via o modelo base, não
  compilará. O MVC partial resolve isso adicionando a
  propriedade, mas cria inconsistência de manutenção.

#### PROBLEMA 4 — Requisitar.Id usado como "Código do Exame"
- **Gravidade:** MÉDIA
- **Onde:** `RequisitarController.cs` linhas 1371, 1419, 1457
- **O que:** O cupom imprime `Requisitar.Id` como "CÓDIGO DE
  EXAME", mas esse Id é da tabela Requisitar, não de
  ExamesRealizados. São tabelas diferentes com Ids diferentes.
- **Impacto:** O paciente recebe um "código de exame" que não
  corresponde ao Id real do exame na tabela ExamesRealizados.
  Isso pode causar confusão em consultas futuras de resultado.

#### PROBLEMA 5 — Requisitar sem vínculo com ExamesRealizados
- **Gravidade:** MÉDIA (design intencional, mas com risco)
- **Onde:** Modelo e DDL de Requisitar
- **O que:** A tabela Requisitar não possui FK para
  ExamesRealizados. São tabelas independentes que são
  gravadas na mesma transação, mas sem vínculo referencial.
- **Impacto:** Não há garantia de integridade referencial
  entre Requisitar e ExamesRealizados. Se um ExamesRealizados
  for excluído, os registros de Requisitar correspondentes
  permanecem órfãos (e vice-versa). A exclusão em
  `ExcluirRequisicao` (linha 1283) remove apenas registros
  de Requisitar, sem tocar em ExamesRealizados ou
  ItensExamesRealizados.

#### PROBLEMA 6 — Divergência entre dois arquivos db.cs
- **Gravidade:** ALTA
- **Onde:** `ModeloDeDados/Models/db.cs` vs
  `LabWebMvc.MVC/Models/db.cs`
- **O que:** Existem dois DbContext com configurações
  diferentes. O MVC db.cs tem:
  - `PostoId` nullable em Requisitar
  - FK `iRequisitar_Postos` para Requisitar
  - `HasColumnType("timestamp with time zone")` para datas
  - `ValueGeneratedOnAdd()` para Ids
  O ModeloDeDados db.cs usa `HasColumnType("datetime")` e
  não tem PostoId em Requisitar.
- **Impacto:** Dependendo de qual DbContext é usado em
  runtime, o comportamento pode variar. Configurações de
  timestamp divergentes podem causar erros com Npgsql 8.x.

### 6.2 Resumo de Inconsistências

| # | Tabela              | Problema                        | Modelo | Fluent | DDL  |
|---|---------------------|---------------------------------|--------|--------|------|
| 1 | ExamesRealizados    | ClasseExamesId                  | ✅     | ✅     | ❌   |
| 2 | ExamesRealizadosAM  | ClasseExamesId                  | ✅     | ✅     | ❌   |
| 3 | Requisitar          | PostoId (ModeloDeDados)         | ❌     | ❌     | ✅   |
| 4 | Requisitar          | FK → ExamesRealizados           | ❌     | ❌     | ❌   |
| 5 | ExamesRealizados    | ClasseExamesId não é atribuído  | —      | —      | —    |

---

## Seção 7: Plano de Correção

### Etapa 1 — Adicionar ClasseExamesId ao DDL (CRÍTICO)

**Arquivos:** `Biblioteca PostgreSql/Tabelas_Vazias.sql`

Adicionar a coluna `ClasseExamesId` e a FK correspondente nas
tabelas `ExamesRealizados` e `ExamesRealizadosAM`:

```sql
-- ExamesRealizados: adicionar coluna e FK
ALTER TABLE ExamesRealizados
  ADD COLUMN ClasseExamesId int NOT NULL DEFAULT 0;

ALTER TABLE ExamesRealizados
  ADD CONSTRAINT iExamesRealizados_ClasseExames
  FOREIGN KEY (ClasseExamesId) REFERENCES ClasseExames(Id);

-- ExamesRealizadosAM: adicionar coluna e FK
ALTER TABLE ExamesRealizadosAM
  ADD COLUMN ClasseExamesId int NOT NULL DEFAULT 0;

ALTER TABLE ExamesRealizadosAM
  ADD CONSTRAINT iExamesRealizadosAM_ClasseExames
  FOREIGN KEY (ClasseExamesId) REFERENCES ClasseExames(Id);
```

Também atualizar o DDL base (`Tabelas_Vazias.sql`) para incluir
a coluna e FK nas definições de CREATE TABLE.

**Risco:** Se já existem registros no banco, o DEFAULT 0 pode
violar a FK se não existir ClasseExames com Id=0. Verificar
antes de executar.

### Etapa 2 — Sincronizar PostoId no ModeloDeDados Requisitar

**Arquivo:** `ModeloDeDados/Models/Requisitar.cs`

Adicionar `PostoId` como propriedade nullable e a navegação
para Postos, alinhando com o MVC partial:

```csharp
public int? PostoId { get; set; }
public virtual Postos? Posto { get; set; }
```

**Arquivo:** `ModeloDeDados/Models/db.cs`

Adicionar a configuração Fluent API para PostoId em Requisitar:

```csharp
entity.Property(e => e.PostoId).IsRequired(false);
entity.HasOne(d => d.Posto).WithMany(p => p.Requisitar)
    .HasForeignKey(d => d.PostoId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.ClientSetNull)
    .HasConstraintName("iRequisitar_Postos");
```

### Etapa 3 — Atribuir ClasseExamesId ao criar ExamesRealizados

**Arquivo:** `RequisitarController.cs` (linha ~459)

No método `SalvarExameRealizadoAsync`, adicionar a atribuição
de `ClasseExamesId` ao criar o objeto `ExamesRealizados`:

```csharp
var exame = new ExamesRealizados
{
    // ... campos existentes ...
    ClasseExamesId = primeiroRequisitar.ClasseExamesId,
};
```

### Etapa 4 — Avaliar o uso de Requisitar.Id como código do exame

**Arquivo:** `RequisitarController.cs` (linhas 1371, 1419)

Avaliar se o "CÓDIGO DE EXAME" no cupom deve ser:
- `Requisitar.Id` (atual — Id da requisição)
- `ExamesRealizados.Id` (Id real do exame)

Se deve ser `ExamesRealizados.Id`, alterar a lógica do cupom
para buscar o Id correspondente na tabela ExamesRealizados
usando PacienteId + DataIni + TabelaExamesId.

### Etapa 5 — Unificar os dois db.cs

**Arquivos:**
- `ModeloDeDados/Models/db.cs`
- `LabWebMvc.MVC/Models/db.cs`

Avaliar qual é o DbContext efetivamente usado em runtime e
garantir que ambos estejam sincronizados. Idealmente, manter
apenas um como fonte da verdade.

Pontos de atenção:
- `HasColumnType("datetime")` vs `HasColumnType("timestamp with time zone")`
- `PostoId` em Requisitar
- `ValueGeneratedOnAdd()` para Ids

### Etapa 6 — Avaliar FK entre Requisitar e ExamesRealizados

Decisão de design: se Requisitar deve ter FK para
ExamesRealizados, adicionar:

```sql
ALTER TABLE Requisitar
  ADD COLUMN ExameRealizadoId int;

ALTER TABLE Requisitar
  ADD CONSTRAINT iRequisitar_ExamesRealizados
  FOREIGN KEY (ExameRealizadoId)
  REFERENCES ExamesRealizados(Id);
```

E no modelo C#:
```csharp
public int? ExameRealizadoId { get; set; }
public virtual ExamesRealizados? ExamesRealizados { get; set; }
```

**Nota:** Esta etapa é opcional e depende da decisão de design.
O Requisitar foi projetado como backup operacional independente.
Adicionar FK criaria acoplamento que pode não ser desejado.

### Ordem de Execução Recomendada

| Ordem | Etapa | Prioridade | Risco    |
|-------|-------|------------|----------|
| 1     | DDL ClasseExamesId (Etapa 1)       | CRÍTICA  | Médio  |
| 2     | Atribuir ClasseExamesId (Etapa 3)  | CRÍTICA  | Baixo  |
| 3     | Sincronizar PostoId (Etapa 2)      | ALTA     | Baixo  |
| 4     | Unificar db.cs (Etapa 5)           | ALTA     | Alto   |
| 5     | Código do exame no cupom (Etapa 4) | MÉDIA    | Baixo  |
| 6     | FK Requisitar→Exames (Etapa 6)     | BAIXA    | Médio  |

---

*Documento gerado por análise direta do código-fonte.*
*Nenhum arquivo foi modificado durante esta análise.*
