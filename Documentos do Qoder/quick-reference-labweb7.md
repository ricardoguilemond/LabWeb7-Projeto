# Quick Reference - LabWeb7 (LabWebMvc.MVC)

**Última Atualização:** 21/04/2026

---

## 📁 ESTRUTURA DE PASTAS PRINCIPAIS

```
LabWeb7-Projeto/
├── LabWebMvc.MVC/                 # PROJETO PRINCIPAL (Web App)
│   ├── Areas/
│   │   ├── Controllers/           # 21 controllers (BaseController pattern)
│   │   ├── Concorrencias/         # Controle de concorrência
│   │   ├── ServicosDatabase/      # Factory de conexão
│   │   └── Validations/           # Validações customizadas
│   ├── Models/                    # 52 models + db.cs (EF Core DbContext)
│   ├── ViewModel/                 # 20 ViewModels para validação
│   ├── Views/                     # Razor Views (.cshtml)
│   ├── Integracoes/               # Exportação/Importação
│   └── wwwroot/                   # JS, CSS, imagens, fonts
│
├── BLL/                           # Business Logic Layer
├── ExtensionsMethods/             # Utilitários e extensões
├── ModeloDeDados/                 # Modelos de referência (legado?)
├── ServicoExportacao/             # Worker Service (exportação)
└── WindowsService/                # Windows Service host
```

---

## 🗄️ BANCO DE DADOS (PostgreSQL)

### Connection
- **Provider:** Npgsql.EntityFrameworkCore.PostgreSQL v8.0.4
- **Config:** `ConexaoPostgreSQL` no appsettings.json
- **Migrations:** ❌ NÃO USA (scripts SQL manuais)

### Tabelas Principais (51 tabelas)

**Core:** Pacientes, Medicos, Instituicao, Postos, TabelaExames, PlanoExames, ClasseExames, Requisitar

**Exames:** ExamesRealizados, ExamesRealizadosAM, ItensExamesRealizados, ItensExamesRealizadosAM, ExamesPendentes, ExamesExportados, ExamesImpressos

**Admin:** Empresa, Senhas, ControleDeAcesso, ControleDePerfil, Configuracoes, Assinaturas

**Integração:** IntegracaoDadosConfiguracao, IntegracaoDadosLayout, IntegracaoDadosExecucao, IntegracaoDadosExecucaoArquivo, IntegracaoDadosArmazenamento, IntegracaoDadosPeriodicidade

**Lookup:** Sexo, EstadoCivil, Cor, TipoSanguineo, UF, Logradouro, SituacaoExames, TituloExames

### DbContext: `db.cs`
**Localização:** `LabWebMvc.MVC/Models/db.cs` (2386 linhas)

**Features Especiais:**
```csharp
// Factory Pattern
_db = _dbFactory.Create();

// Save com reutilização de IDs
await SaveChangesWithSyncAsync(sincroniza: true, quantidadeRegistrosMaximo: 99);

// Remove órfãos
DeleteOrphans();

// Lock de tabela para concorrência
LOCK TABLE "Pacientes" IN EXCLUSIVE MODE
```

---

## 🔄 RELACIONAMENTOS FK (Principais)

### Pacientes (1) → (N)
```
Pacientes.Id
  → ExamesRealizados.PacienteId
  → Requisitar.PacienteId
  → ExamesPendentes.PacienteId
  → ExamesExportados.PacienteId
  → ItensExamesRealizados.PacienteId
  → FichasInternas.PacienteId
```

### Instituicao (1) → (N)
```
Instituicao.Id
  → ExamesRealizados.InstituicaoId
  → Postos.InstituicaoId (implícito)
  → TabelaExames.InstituicaoId (implícito)
  → Requisitar.InstituicaoId
```

### TabelaExames (1) → (N)
```
TabelaExames.Id
  → PlanoExames.TabelaExamesId
  → ExamesRealizados.TabelaExamesId
  → Requisitar.TabelaExamesId
```

---

## 🔗 RELACIONAMENTOS APENAS EM CÓDIGO (Sem FK no Banco)

| Origem                | Destino      | Campo              | Tipo           |
|-----------------------|--------------|--------------------|----------------|
| PlanoExames           | TabelaExames | TabelaExamesId     | Lógico         |
| PlanoExames           | ClasseExames | ExameId            | Lógico (SUS=1) |
| ItensExamesRealizados | PlanoExames  | ContaExame (string)| Por código     |
| Requisitar            | ClasseExames | ClasseExamesId     | Lógico         |

**ContaExame Estrutura:** `XX.XX.XXX.XXXX` (11 dígitos)
- Pos 1-2: Tipo (11=crédito)
- Pos 3-4: Folha (01-99)
- Pos 5-7: Conta principal
- Pos 8-11: Item

**Validação:** `ContaExame.Substring(0, 7).StartsWith(prefixo)`

---

## 🏗️ PADRÕES ARQUITETURAIS

### DI Registration (Startup.cs)
```csharp
services.AddScoped<IDbFactory, DbFactory>();
services.AddScoped<Db>(sp => { /* criação dinâmica */ });
services.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));
services.AddScoped<GeralController>();
services.AddScoped<ExclusaoService>();
services.AddSingleton<IEventLogHelper, EventLogHelper>();
```

### BaseController Pattern
```csharp
public class PacientesController : BaseController
{
    // _db já disponível (criado via factory)
    // _geralController para métodos utilitários
    // _validador para validação de sessão
    // _eventLogHelper para logs
}
```

### Multi-Tenant
```csharp
// Troca de banco em runtime
_connectionService.SetConnectionString(novaConnection);
_db = _dbFactory.Create();
```

---

## 📝 NOMENCLATURA

| Tipo          | Padrão                  | Exemplo                           |
|---------------|-------------------------|-----------------------------------|
| Model Class   | PascalCase              | `Pacientes`, `ExamesRealizados`   |
| Controller    | PascalCase + Controller | `PacientesController`             |
| ViewModel     | vm/VM + PascalCase      | `vmPacientes`, `VMGeral`          |
| Tabela BD     | PascalCase              | `Pacientes`, `ExamesRealizados`   |
| Index BD      | i + Tabela + Nº         | `iPacientes1`, `iPacientes2`      |
| FK Constraint | i + Origem + Destino    | `iExamesRealizados_Pacientes`     |
| Interface     | I + PascalCase          | `IEventLogHelper`                 |
| Service       | PascalCase + Service    | `ExclusaoService`                 |

---

## 📦 DEPENDÊNCIAS PRINCIPAIS

### EF Core & Database
```xml
Npgsql.EntityFrameworkCore.PostgreSQL  8.0.4
Microsoft.EntityFrameworkCore        8.0.19
```

### Cloud Storage
```xml
AWSSDK.S3              4.0.6.2
Azure.Storage.Blobs    12.25.0
```

### PDF
```xml
itext                  9.3.0
PdfSharpCore           1.3.67
```

### Images
```xml
SixLabors.ImageSharp           3.1.11
SixLabors.ImageSharp.Drawing   2.1.7
SixLabors.Fonts                2.1.3
```

### Google
```xml
Google.Cloud.RecaptchaEnterprise.V1  2.18.0
```

### Utilities
```xml
Newtonsoft.Json          13.0.4
RecaptchaNet             3.1.0
```

---

## 🎯 REGRAS DE NEGÓCIO CRÍTICAS

### 1. Plano de Exames - SUS Model
```csharp
// SUS é o modelo base (ExameId = 1)
int susId = (int)IdPadrao.SUS;  // = 1

// Alterar SUS = alterar TODAS as instituições
if (plano.ExameId == susId)
{
    // Replicar para todas as TabelaExamesId
}
```

### 2. Transação de Requisição
```csharp
// PASSO 1: Salvar Médico/Paciente (FORA da transação)
await _db.Medicos.AddAsync(medico);
await _db.SaveChangesAsync();

// PASSO 2: Salvar Exames (DENTRO da transação)
using var transaction = await _db.Database.BeginTransactionAsync();
try
{
    await _db.Requisitar.AddRangeAsync(exames);
    await _db.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    // Médico/Paciente permanecem salvos
}
```

### 3. Data/Hora
```csharp
// ❌ ERRADO
entity.DataRegistro = DateTime.Now;
entity.DataRegistro = DateTime.UtcNow;

// ✅ CORRETO
entity.DataRegistro = await _geralController.ObterDataHoraServidor();
// OU
entity.DataRegistro = await _db.Database
    .SqlQuery<DateTime>("SELECT NOW()")
    .FirstOrDefaultAsync();
```

### 4. Exclusão com Validação de FK
```csharp
// ✅ ANTES de deletar, verificar FKs
var temExames = await _db.ExamesRealizados
    .AnyAsync(e => e.PacienteId == pacienteId);

if (temExames)
{
    return View("Error", "Paciente possui exames vinculados e não pode ser excluído.");
}

// SÓ ENTÃO deletar
_db.Pacientes.Remove(paciente);
await _db.SaveChangesAsync();
```

---

## 🔐 ARQUIVOS PROTEGIDOS (NÃO modificar sem autorização)

### Config Files
- `.editorconfig`
- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Linux.json`
- `Program.cs`
- `Startup.cs`
- `web.config`
- `Settings.cs`
- `launchSettings.json`

### Folders
- `.vs/`
- `.git/`
- `Base de Dados Vazio MSSQL/`
- `Scripts/` (MSSQL originais)

---

## 🛠️ COMANDOS ÚTEIS

### EF Core Scaffolding
```bash
# Gerar DbContext do banco
dotnet ef dbcontext scaffold "Server=127.0.0.1;Database=db_labweb7;User Id=postgres;Password=senha" Npgsql.EntityFrameworkCore.PostgreSQL -o Models --context Db --project LabWebMvc.MVC --force
```

### Atualizar EF Tools
```bash
dotnet tool update --global dotnet-ef
```

### Verificar Encoding (PowerShell)
```powershell
$bytes = [System.IO.File]::ReadAllBytes("caminho/arquivo.cs")
$hasBOM = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
Write-Host "Tem BOM: $hasBOM"
```

---

## ⚠️ PONTOS DE ATENÇÃO

### Code Marking
```csharp
//Feito pelo Kiro em 21/04/2026
public void MeuMetodo()
{
    // código
}
//..Kiro
```

### Encoding por Tipo
| Extensão              | Encoding | BOM               |
|-----------------------|----------|-------------------|
| .cs, .cshtml, .csproj | UTF-8    | ✅ Com BOM        |
| .js                   | UTF-8    | 🔵 Manter         |
| .json, .css, .md      | UTF-8    | ❌ Sem BOM        |

### Git Operations
- ❌ NUNCA executar sem autorização: `git push`, `git commit`, `git merge`
- ✅ Pode consultar: histórico, branches, diffs

### ValidacaoGenerica
```csharp
// Deve retornar View() SEM model
public IActionResult ValidacaoGenerica()
{
    ViewBag.Dados = dados;
    return View();  // SEM model
}
```

### site.js
```html
<!-- JÁ carregado 2x no _Layout.cshtml -->
<!-- NÃO adicionar terceira referência -->
```

---

## 📊 FLUXOS CRÍTICOS

### Fluxo: Requisição de Exames
```
1. Selecionar: Instituição + Posto + Médico + Paciente
2. Salvar Médico/Paciente (FORA transação)
3. Selecionar exames (PlanoExames filtrado por TabelaExamesId)
4. Criar Requisitar (DENTRO transação)
5. Se falhar: rollback exames, mantém médico/paciente
```

### Fluxo: Realização de Exames
```
Requisitar → ItensExamesRealizados → ExamesRealizados
  → Adicionar Resultado + Laudo (byte[])
  → Liberar (Liberacao = 1)
  → Imprimir/Exportar
```

### Fluxo: Alteração Plano de Exames
```
1. Alterar item SUS (ExameId = 1)
2. Replicar para TODAS instituições (mesmo ContaExame)
3. Cenário 1: Preço individual (grid inline)
4. Cenário 2: Preço em massa (tela completa)
5. Validar FKs antes de excluir
```

---

## 🔍 TROUBLESHOOTING

### DbContext não encontra tabela
```csharp
// Verificar se está no db.cs
public virtual DbSet<Pacientes> Pacientes { get; set; }

// Verificar OnModelCreating
modelBuilder.Entity<Pacientes>(entity => {
    entity.ToTable("Pacientes");
    entity.HasKey(e => e.Id).HasName("iPacientes1");
});
```

### Erro de Concorrência
```csharp
// Verificar ControleConcorrencia tabela
// Usar ExclusaoService para deleções
// Lock de tabela no SaveChangesWithSyncAsync
```

### Data/Hora Errada
```csharp
// Verificar se está usando DateTime.Now (ERRADO)
// Usar ObterDataHoraServidor() (CORRETO)
// Verificar Kind do DateTime (Unspecified/Local/UTC)
```

### FK Violation
```csharp
// Verificar tabelas filhas antes de deletar
// ItensExamesRealizados, Requisitar, etc.
// Usar ExclusaoService com validação
```

---

## 📞 CONTACTS & RESOURCES

### Documentação
- **Análise Completa:** `Documentos do Qoder/analise-arquitetural-completa-labweb7.md`
- **Steering Rules:** `.kiro/steering/*.md`
- **SQL Scripts:** `Biblioteca SQL/Base de Dados Vazio Postgresql/`

### Pastas de Documentação
- `Documentos do Kiro/` - Análises e documentos do Kiro
- `Documentos do Qoder/` - Análises e documentos do Qoder
- `LabWeb7-Project Documentos/` - Documentos gerais do projeto

---

**Quick Reference gerado por Qoder AI - 21/04/2026**
