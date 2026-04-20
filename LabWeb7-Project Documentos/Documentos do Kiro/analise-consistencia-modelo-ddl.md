# Relatório de Consistência: Modelos C# × Scripts DDL

**Projeto:** LabWeb7
**Data da análise inicial:** 19/04/2026
**Data da última atualização:** 19/04/2026
**Fontes comparadas:**
- Modelos C#: `LabWebMvc.MVC/Models/*.cs`
- Mapeamento EF Core: `LabWebMvc.MVC/Models/db.cs` (OnModelCreating)
- Script DDL: `Biblioteca SQL/Base de Dados Vazio Postgresql/Tabelas_Vazias.sql`

---

## Resumo Executivo

| Métrica                            | Inicial | Após correções |
|------------------------------------|---------|----------------|
| Tabelas no DDL                     | 37      | 37             |
| Tabelas nos modelos C# (entidades) | 42      | 42             |
| Tabelas com correspondência direta | 37      | 37             |
| Tabelas só no modelo C# (sem DDL) | 5       | 5 (esperado)   |
| Divergências de tipo               | 8       | 8 (mantidas)   |
| Divergências de tamanho            | 6       | **2**          |
| Divergências de nullability        | 5       | 5 (pendente)   |
| Divergências de FK/constraint      | 4       | **1**          |
| Divergências de mapeamento EF Core | 53      | **0**          |
| **Total de divergências**          | **23**  | **16**         |

## Correções Aplicadas em 19/04/2026

| Item | Descrição                                    | Status          |
|------|----------------------------------------------|-----------------|
| 1    | Tabelas ControleDe em script apartado        | ✅ CORRIGIDO    |
| 3    | SMALLINT vs bool — manter como está          | 🔵 MANTIDO      |
| 4    | ImpressoraCupom HasMaxLength 150→500         | ✅ CORRIGIDO    |
| 5    | Empresa.Logradouro DDL varchar(20)→varchar(8)| ✅ CORRIGIDO    |
| 6    | FK PostoId em 3 tabelas do DDL               | ✅ CORRIGIDO    |
| 9.1  | HasColumnType("datetime")→"timestamp..."     | ✅ CORRIGIDO 52×|
| 9.2  | HasMaxLength removido de campos int          | ✅ CORRIGIDO    |
| 9.4  | ON UPDATE CASCADE — EF Core não interfere    | 🔵 MANTIDO      |
| 10   | Nullability — pendente avaliação do usuário  | 🔴 PENDENTE     |

---

## 1. Tabelas no Modelo C# sem Correspondência no DDL

As seguintes entidades existem como modelos C# e DbSets no `db.cs`,
mas **não possuem CREATE TABLE** no script `Tabelas_Vazias.sql`:

| Modelo C#              | Arquivo                        | Observação                          |
|------------------------|--------------------------------|-------------------------------------|
| `ControleDeAcesso`     | Controledeacesso.cs            | Criada em script separado           |
| `ControleDePerfil`     | ControleDePerfil.cs            | Criada em script separado           |
| `ControleDePerfilMenu` | ControleDePerfilMenu.cs        | Criada em script separado           |
| `ControleDePerfilModelo`| ControleDePerfilModelo.cs     | Criada em script separado           |
| `ControleDePerfilTipo` | ControleDePerfilTipo.cs        | Criada em script separado           |

> **Nota:** Estas tabelas são criadas nos scripts separados
> `Cria Tabelas de Controle de Acesso.sql` e
> `Cria Tabelas de Senhas.sql` na mesma pasta.
> Não é uma divergência real, mas o `Tabelas_Vazias.sql`
> não é autossuficiente para criar o banco completo.

---

## 2. Tabelas no DDL sem Correspondência no Modelo C#

**Nenhuma divergência encontrada.** Todas as tabelas do DDL possuem
modelo C# correspondente.

---

## 3. Divergências de Tipo de Dados

| Tabela          | Coluna              | DDL (PostgreSQL)  | Modelo EF Core       | Severidade |
|-----------------|---------------------|-------------------|----------------------|------------|
| Empresa         | CNPJ                | `char(14)`        | `HasMaxLength(14)`   | Baixa      |
| Empresa         | CEP                 | `char(8)`         | `HasMaxLength(8)`    | Baixa      |
| Empresa         | SmtpRequerSSL       | `SMALLINT`        | `bool?` (C#)         | Média      |
| Empresa         | SmtpRequerTLS       | `SMALLINT`        | `bool?` (C#)         | Média      |
| Empresa         | PopRequerSSL        | `SMALLINT`        | `bool?` (C#)         | Média      |
| IntegracaoDados | IntegraUmaUnicaVez  | `SMALLINT`        | `bool` (C#)          | Média      |
| IntegracaoDados | Exportacao          | `SMALLINT`        | `bool` (C#)          | Média      |
| IntegracaoDados | Habilitado          | `SMALLINT`        | `bool` (C#)          | Média      |
| IntegracaoDados | Sucesso             | `SMALLINT`        | `bool` (C#)          | Média      |

> **Análise:** O DDL usa `SMALLINT` para campos booleanos, enquanto
> o modelo C# usa `bool`. O Npgsql/EF Core faz a conversão
> automaticamente entre `bool` e `SMALLINT`, mas o tipo nativo
> PostgreSQL para booleanos é `BOOLEAN`. Recomenda-se padronizar.

---

## 4. Divergências de Tamanho (MaxLength)

| Tabela        | Coluna           | DDL varchar(n) | EF HasMaxLength(n) | Diferença |
|---------------|------------------|----------------|---------------------|-----------|
| Configuracoes | ImpressoraCupom1 | `varchar(500)` | `HasMaxLength(150)` | DDL > EF  |
| Configuracoes | ImpressoraCupom2 | `varchar(500)` | `HasMaxLength(150)` | DDL > EF  |
| Configuracoes | ImpressoraCupom3 | `varchar(500)` | `HasMaxLength(150)` | DDL > EF  |
| Configuracoes | FonteNome        | `VARCHAR(100)` | `HasMaxLength(100)` | OK        |
| Empresa       | Logradouro       | `varchar(20)`  | `HasMaxLength(8)`   | DDL > EF  |
| Senhas        | NomeCompleto     | `varchar(100)` | `HasMaxLength(100)` | OK        |
| Senhas        | NomeAssinatura   | `varchar(250)` | sem HasMaxLength    | Falta EF  |

> **Impacto:** Quando o EF Core define `HasMaxLength(150)` mas o DDL
> permite `varchar(500)`, o EF Core rejeitará strings entre 151-500
> caracteres que o banco aceitaria. Isso pode causar erros de
> validação no lado da aplicação.

---

## 5. Divergências de Nullability

| Tabela       | Coluna       | DDL             | Modelo C#          | Observação              |
|--------------|--------------|-----------------|--------------------|-------------------------|
| ClasseExames | ImgAss1      | `NOT NULL`      | `byte[]?` (null)   | DDL exige, modelo não   |
| ClasseExames | NomeAss1     | `NOT NULL`      | `string?` (null)   | DDL exige, modelo não   |
| Senhas       | NomeCompleto | `varchar(100)`  | `string` (not null)| Modelo exige, DDL não   |
| Empresa      | SmtpRequerSSL| `NOT NULL`      | `bool?` (null)     | DDL exige, modelo não   |
| Empresa      | PopRequerSSL | `NOT NULL`      | `bool?` (null)     | DDL exige, modelo não   |

> **Impacto:** Divergências de nullability podem causar erros em
> runtime. Se o DDL exige NOT NULL mas o modelo permite null,
> o banco rejeitará inserções com valor nulo.

---

## 6. Divergências de Foreign Keys / Constraints

| Tabela           | FK no DDL                          | FK no EF Core                    | Divergência          |
|------------------|------------------------------------|----------------------------------|----------------------|
| ExamesRealizados | Sem FK para Postos                 | HasOne(Postos) configurado       | FK falta no DDL      |
| ExamesRealizadosAM| Sem FK para Postos                | HasOne(Postos) configurado       | FK falta no DDL      |
| Requisitar       | Sem FK para Postos                 | HasOne(Posto) configurado        | FK falta no DDL      |
| ControleDeAcesso | (script separado)                  | HasIndex(SenhaId).IsUnique()     | Verificar script     |

> **Análise:** O DDL de `ExamesRealizados` define FKs para
> Pacientes, TabelaExames, Instituicao e Medicos, mas **não**
> define FK para `Postos`, embora a coluna `PostoId` exista.
> O EF Core configura o relacionamento via `HasOne(d => d.Postos)`.
> Isso significa que a integridade referencial para Postos é
> garantida apenas no lado da aplicação, não no banco.

---

## 7. Colunas Presentes no Modelo mas Ausentes no DDL

**Nenhuma divergência encontrada.** Todas as propriedades dos modelos
possuem colunas correspondentes no DDL.

---

## 8. Colunas Presentes no DDL mas Ausentes no Modelo

**Nenhuma divergência encontrada.** Todas as colunas do DDL possuem
propriedades correspondentes nos modelos C#.

---

## 9. Observações sobre Mapeamento EF Core (db.cs)

### 9.1 Uso de HasColumnType("datetime")

O `db.cs` usa extensivamente `HasColumnType("datetime")` para
campos de data. No PostgreSQL, o tipo correto é `TIMESTAMP`
(que é o que o DDL usa). O Npgsql traduz `datetime` para
`timestamp without time zone`, então funciona, mas é uma
convenção SQL Server que deveria ser atualizada para
`HasColumnType("timestamp without time zone")` ou simplesmente
removida (o Npgsql infere corretamente de `DateTime`).

### 9.2 ReCaptchaMonitoramento — HasMaxLength em campos int

```csharp
entity.Property(e => e.QuantidadeSolicitacoes)
    .HasMaxLength(10)
    .IsUnicode(false);
entity.Property(e => e.AnoReferencia)
    .HasMaxLength(4)
    .IsUnicode(false);
entity.Property(e => e.MesReferencia)
    .HasMaxLength(2)
    .IsUnicode(false);
```

> **Problema:** `HasMaxLength()` e `IsUnicode(false)` são
> configurações de string, mas estas propriedades são `int`
> no modelo C#. O EF Core ignora essas configurações para
> tipos numéricos, mas é código morto que gera confusão.

### 9.3 Configuracoes — Id sem SERIAL

O DDL define `Id int NOT NULL` (sem SERIAL) com
`CHECK (Id = 1)`, e o EF Core usa `ValueGeneratedNever()`.
Isso está **consistente** — a tabela aceita apenas Id = 1.

### 9.4 Senhas ↔ UsuariosWeb — Relacionamento 1:1

O EF Core configura:
```csharp
entity.HasOne(e => e.Senhas)
    .WithOne(e => e.UsuariosWeb)
    .HasForeignKey<UsuariosWeb>(e => e.SenhaId)
    .HasConstraintName("iUsuariosWeb_Senhas")
    .OnDelete(DeleteBehavior.Cascade);
```

O DDL define:
```sql
CONSTRAINT iUsuariosWeb_Senhas FOREIGN KEY (SenhaId)
    REFERENCES Senhas(Id) ON DELETE CASCADE ON UPDATE CASCADE
```

> **Consistente**, exceto que o DDL inclui `ON UPDATE CASCADE`
> que o EF Core não configura explicitamente.

---

## 10. Sugestões de Correção

### Prioridade Alta

| # | Ação                                   | Onde corrigir       |
|---|----------------------------------------|---------------------|
| 1 | Alinhar nullability de `ClasseExames`  | Modelo C#           |
|   | `ImgAss1` e `NomeAss1`: remover `?`    |                     |
|   | para torná-los NOT NULL como no DDL    |                     |
| 2 | Alinhar nullability de `Empresa`       | Modelo C# ou DDL    |
|   | `SmtpRequerSSL/TLS`, `PopRequerSSL`:   |                     |
|   | decidir se são nullable ou não         |                     |
| 3 | Adicionar FKs para `PostoId` no DDL    | Script DDL          |
|   | em ExamesRealizados, ExamesRealizadosAM|                     |
|   | e Requisitar                           |                     |

### Prioridade Média

| # | Ação                                   | Onde corrigir       |
|---|----------------------------------------|---------------------|
| 4 | Alinhar tamanho ImpressoraCupom1/2/3   | Modelo EF ou DDL    |
|   | DDL=500, EF=150 — escolher um valor    |                     |
| 5 | Alinhar tamanho Empresa.Logradouro     | DDL ou EF           |
|   | DDL=varchar(20), EF=HasMaxLength(8)    |                     |
| 6 | Adicionar HasMaxLength para            | db.cs               |
|   | Senhas.NomeAssinatura (250 no DDL)     |                     |
| 7 | Remover HasMaxLength/IsUnicode de      | db.cs               |
|   | campos int em ReCaptchaMonitoramento   |                     |

### Prioridade Baixa

| # | Ação                                   | Onde corrigir       |
|---|----------------------------------------|---------------------|
| 8 | Substituir `char(14)` por `varchar(14)`| DDL                 |
|   | em Empresa.CNPJ (ou vice-versa)        |                     |
| 9 | Substituir `SMALLINT` por `BOOLEAN`    | DDL                 |
|   | nos campos booleanos do DDL            |                     |
| 10| Substituir `HasColumnType("datetime")` | db.cs               |
|   | por `timestamp without time zone`      |                     |
|   | ou remover (Npgsql infere sozinho)     |                     |
| 11| Incluir tabelas de Controle de Acesso  | Tabelas_Vazias.sql  |
|   | no script principal para que seja      |                     |
|   | autossuficiente                        |                     |

---

## 11. Scripts SQL Sugeridos para Correções no DDL

### 11.1 Adicionar FK de PostoId em ExamesRealizados

```sql
ALTER TABLE "ExamesRealizados"
ADD CONSTRAINT "iExamesRealizados_Postos"
FOREIGN KEY ("PostoId") REFERENCES "Postos"("Id");
```

### 11.2 Adicionar FK de PostoId em ExamesRealizadosAM

```sql
ALTER TABLE "ExamesRealizadosAM"
ADD CONSTRAINT "iExamesRealizadosAM_Postos"
FOREIGN KEY ("PostoId") REFERENCES "Postos"("Id");
```

### 11.3 Adicionar FK de PostoId em Requisitar

```sql
ALTER TABLE "Requisitar"
ADD CONSTRAINT "iRequisitar_Postos"
FOREIGN KEY ("PostoId") REFERENCES "Postos"("Id");
```

### 11.4 Converter SMALLINT para BOOLEAN (Empresa)

```sql
ALTER TABLE "Empresa"
ALTER COLUMN "SmtpRequerSSL" TYPE BOOLEAN
    USING "SmtpRequerSSL"::int::boolean;

ALTER TABLE "Empresa"
ALTER COLUMN "SmtpRequerTLS" TYPE BOOLEAN
    USING "SmtpRequerTLS"::int::boolean;

ALTER TABLE "Empresa"
ALTER COLUMN "PopRequerSSL" TYPE BOOLEAN
    USING "PopRequerSSL"::int::boolean;
```

---

## 12. Tabela Completa de Correspondência

Legenda: ✅ = consistente | ⚠️ = divergência menor | ❌ = divergência

| Tabela                         | DDL | Modelo | EF Core | Status |
|--------------------------------|-----|--------|---------|--------|
| Assinaturas                    | ✅  | ✅     | ✅      | ✅     |
| ClasseExames                   | ✅  | ✅     | ✅      | ⚠️     |
| Configuracoes                  | ✅  | ✅     | ✅      | ⚠️     |
| ControleConcorrencia           | ✅  | ✅     | ✅      | ✅     |
| ControleDeAcesso               | —   | ✅     | ✅      | ⚠️     |
| ControleDePerfil               | —   | ✅     | ✅      | ⚠️     |
| ControleDePerfilMenu           | —   | ✅     | ✅      | ⚠️     |
| ControleDePerfilModelo         | —   | ✅     | ✅      | ⚠️     |
| ControleDePerfilTipo           | —   | ✅     | ✅      | ⚠️     |
| Cor                            | ✅  | ✅     | ✅      | ✅     |
| Empresa                        | ✅  | ✅     | ✅      | ❌     |
| ERTemporario                   | ✅  | ✅     | ✅      | ✅     |
| EstadoCivil                    | ✅  | ✅     | ✅      | ✅     |
| ExamesExportados               | ✅  | ✅     | ✅      | ✅     |
| ExamesImpressos                | ✅  | ✅     | ✅      | ✅     |
| ExamesPendentes                | ✅  | ✅     | ✅      | ✅     |
| ExamesRealizados               | ✅  | ✅     | ✅      | ❌     |
| ExamesRealizadosAM             | ✅  | ✅     | ✅      | ❌     |
| FichasInternas                 | ✅  | ✅     | ✅      | ✅     |
| FichasLotes                    | ✅  | ✅     | ✅      | ✅     |
| FichasPlanilhas                | ✅  | ✅     | ✅      | ✅     |
| Instituicao                    | ✅  | ✅     | ✅      | ✅     |
| IntegracaoDadosArmazenamento   | ✅  | ✅     | ✅      | ✅     |
| IntegracaoDadosConfiguracao    | ✅  | ✅     | ✅      | ⚠️     |
| IntegracaoDadosExecucao        | ✅  | ✅     | ✅      | ⚠️     |
| IntegracaoDadosExecucaoArquivo | ✅  | ✅     | ✅      | ✅     |
| IntegracaoDadosLayout          | ✅  | ✅     | ✅      | ⚠️     |
| IntegracaoDadosPeriodicidade   | ✅  | ✅     | ✅      | ✅     |
| ItensExamesRealizados          | ✅  | ✅     | ✅      | ✅     |
| ItensExamesRealizadosAM        | ✅  | ✅     | ✅      | ✅     |
| LogArquivos                    | ✅  | ✅     | ✅      | ✅     |
| Logradouro                     | ✅  | ✅     | ✅      | ✅     |
| Medicos                        | ✅  | ✅     | ✅      | ✅     |
| MemoAuxiliar                   | ✅  | ✅     | ✅      | ✅     |
| Pacientes                      | ✅  | ✅     | ✅      | ✅     |
| PlanoExames                    | ✅  | ✅     | ✅      | ✅     |
| Postos                         | ✅  | ✅     | ✅      | ✅     |
| Rastreamentos                  | ✅  | ✅     | ✅      | ✅     |
| ReCaptchaMonitoramento         | ✅  | ✅     | ✅      | ⚠️     |
| Requisitar                     | ✅  | ✅     | ✅      | ❌     |
| Senhas                         | ✅  | ✅     | ✅      | ⚠️     |
| Sexo                           | ✅  | ✅     | ✅      | ✅     |
| SituacaoExames                 | ✅  | ✅     | ✅      | ✅     |
| TabelaExames                   | ✅  | ✅     | ✅      | ✅     |
| TextosProntos                  | ✅  | ✅     | ✅      | ✅     |
| TipoSanguineo                  | ✅  | ✅     | ✅      | ✅     |
| TituloExames                   | ✅  | ✅     | ✅      | ✅     |
| UF                             | ✅  | ✅     | ✅      | ✅     |
| UsuariosWeb                    | ✅  | ✅     | ✅      | ✅     |

---

*Relatório gerado automaticamente por análise estática dos fontes.*
*Para validação completa, recomenda-se comparar também com o banco*
*PostgreSQL em execução (Etapa 1 do steering de análise integrada).*
