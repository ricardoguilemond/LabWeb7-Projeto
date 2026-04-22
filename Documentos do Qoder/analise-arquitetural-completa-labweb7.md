# Análise Arquitetural Completa - LabWeb7 (LabWebMvc.MVC)

**Data da Análise:** 21/04/2026  
**Versão do Projeto:** .NET 8  
**Analista:** Qoder AI

---

## 1. VISÃO GERAL DO PROJETO

### 1.1 Descrição
LabWeb7 é um sistema web multi-cliente para gerenciamento de laboratórios de análises clínicas, com suporte a múltiplas instituições, controle de exames, pacientes, médicos e integrações com sistemas externos.

### 1.2 Stack Tecnológica
- **Framework:** .NET 8 (C#)
- **Padrão Arquitetural:** MVC (Model-View-Controller) com Razor Views
- **Frontend:** JavaScript + jQuery + Bootstrap
- **Banco de Dados:** PostgreSQL (via Npgsql)
- **ORM:** Entity Framework Core 8.0.19
- **Servidor:** Kestrel (pode rodar como Windows Service ou Linux)

---

## 2. ESTRUTURA DE PROJETOS DA SOLUTION

### 2.1 Diagrama de Dependências

```
LabWebMvc.sln
│
├── LabWebMvc.MVC (Projeto Principal - Web Application)
│   ├── Dependencies → BLL
│   └── Dependencies → ExtensionsMethods
│
├── BLL (Business Logic Layer)
│   └── FrameworkReference → Microsoft.AspNetCore.App
│
├── ExtensionsMethods (Utilitários e Extensões)
│   └── Dependencies → BLL
│
├── ModeloDeDados (Modelos de Referência)
│   └── Dependencies → ExtensionsMethods
│
├── ServicoExportacao (Worker Service - Exportação de Dados)
│   └── Dependencies → LabWebMvc.MVC
│
└── WindowsService (Serviço Windows)
    ├── Dependencies → ExtensionsMethods
    ├── Dependencies → LabWebMvc.MVC
    └── Dependencies → ServicoExportacao
```

### 2.2 Descrição de Cada Projeto

#### **LabWebMvc.MVC** (Projeto Principal)
- **Tipo:** ASP.NET Core Web Application (SDK: Microsoft.NET.Sdk.Web)
- **Responsabilidade:** Application layer, Controllers, Views, UI logic
- **Features:**
  - Controllers em Areas (padrão modular)
  - Razor Views com Bootstrap
  - EF Core DbContext (db.cs)
  - ViewModels para validação
  - Integrations (AWS S3, Azure Blobs, Google reCAPTCHA)
  - PDF generation (iText, PdfSharpCore)
  - Image processing (SixLabors.ImageSharp)

#### **BLL** (Business Logic Layer)
- **Tipo:** Class Library (SDK: Microsoft.NET.Sdk)
- **Responsabilidade:** Business rules, utilities, converters
- **Principais Arquivos:**
  - `PartBLL.cs` - Lógica de negócio de exames
  - `TempoLocal.cs` / `TempoServidorMSSQL.cs` - Serviços de data/hora
  - `ConversoresPdf.cs` / `WkConverterPdf.cs` - Conversão PDF
  - `PathHelper.cs` - Manipulação de caminhos
  - `UtilBLL.cs` - Utilitários gerais

#### **ExtensionsMethods** (Biblioteca de Extensões)
- **Tipo:** Class Library
- **Responsabilidade:** Cross-cutting concerns, utilities, helpers
- **Módulos:**
  - `Genericos/` - Utilitários gerais, validações, matemática
  - `Enumerations/` - Enums compartilhados (ex: Recaptcha)
  - `ParametrosGenericos/` - Parâmetros de configuração
  - `MensagemEmail/` - Serviços de email e SMS
  - `Storages/` - LocalStorageService
  - `ValidadorDeSessao/` - Validação de sessão de usuário
  - `EventViewerHelper/` - Logs no Event Viewer do Windows
  - `ControleDeAcesso/` - Controle de menu por perfil

#### **ModeloDeDados** (Modelos de Referência)
- **Tipo:** Class Library
- **Responsabilidade:** Entity models de referência (parece ser um projeto legado/espelho)
- **Observação:** Contém modelos similares ao LabWebMvc.MVC/Models

#### **ServicoExportacao** (Worker Service)
- **Tipo:** .NET Worker (SDK: Microsoft.NET.Sdk.Worker)
- **Responsabilidade:** Exportação assíncrona de dados para sistemas externos
- **Features:**
  - Background hosted service
  - Integrações com AWS S3, Azure Storage
  - Pode rodar como Windows Service

#### **WindowsService** (Serviço Windows)
- **Tipo:** Console Application (SDK: Microsoft.NET.Sdk)
- **Responsabilidade:** Host para rodar serviços em background no Windows
- **Features:**
  - FileWriteService.cs - Serviço de escrita em arquivos
  - Integração com ServicoExportacao

---

## 3. BANCO DE DADOS POSTGRESQL

### 3.1 Configuração de Acesso
- **Provider:** Npgsql.EntityFrameworkCore.PostgreSQL v8.0.4
- **Connection Strings:** `ConexaoPostgreSQL` / `PSQLConnectionString` (no appsettings.json)
- **Multi-tenant:** Banco único por empresa (não é shared schema)
- **Migrations:** NÃO utiliza EF Migrations - scripts SQL manuais

### 3.2 Modelo de Dados (DbContext - db.cs)

**Localização:** `LabWebMvc.MVC/Models/db.cs` (2386 linhas)

**Características Especiais:**
1. **Factory Pattern:** DbContext criado via `IDbFactory` para troca dinâmica de banco
2. **Custom SaveChanges:** 
   - `SaveChanges()` / `SaveChangesAsync()` - padrão
   - `SaveChangesWithSyncAsync()` - reutiliza IDs excluídos (limite 99 IDs)
   - `DeleteOrphans()` - remove registros órfãos automaticamente
3. **Table Locking:** Usa `LOCK TABLE ... IN EXCLUSIVE MODE` para controle de concorrência
4. **Sequence Synchronization:** Sincroniza sequências PostgreSQL automaticamente

### 3.3 Tabelas Principais do Sistema

#### **Entidades Core (Domínio Principal)**

| Tabela         | Primary Key  | Descrição                                |
|----------------|--------------|------------------------------------------|
| Pacientes      | Id (serial)  | Cadastro de pacientes                    |
| Medicos        | Id (serial)  | Cadastro de médicos                      |
| Instituicao    | Id (serial)  | Instituições/clientes (multi-tenant)     |
| Postos         | Id (serial)  | Postos de coleta vinculados a instituições|
| TabelaExames   | Id (serial)  | Tabelas de preços por instituição        |
| PlanoExames    | Id (serial)  | Itens do plano de exames (preços)        |
| ClasseExames   | Id (serial)  | Classes/grupos de exames                 |
| Requisitar     | Id (serial)  | Requisições de exames (pending)          |

#### **Entidades de Exames (Operacional)**

| Tabela                  | Primary Key  | Descrição                               |
|-------------------------|--------------|-----------------------------------------|
| ExamesRealizados        | Id (serial)  | Exames concluídos                       |
| ExamesRealizadosAM      | Id (serial)  | Exames concluídos (AM = ?)              |
| ItensExamesRealizados   | Id (serial)  | Itens dos exames realizados             |
| ItensExamesRealizadosAM | Id (serial)  | Itens dos exames realizados (AM)        |
| ExamesPendentes         | Id (serial)  | Exames pendentes de execução            |
| ExamesExportados        | Id (serial)  | Exames exportados para sistemas externos|
| ExamesImpressos         | Id (serial)  | Controle de impressões de exames        |

#### **Entidades Administrativas**

| Tabela               | Primary Key  | Descrição                            |
|----------------------|--------------|--------------------------------------|
| Empresa              | Id (serial)  | Dados da empresa (matriz/filial)     |
| Senhas               | Id (serial)  | Gerenciamento de senhas (fila)       |
| ControleDeAcesso     | Id (serial)  | Controle de acesso de usuários       |
| ControleDePerfil     | Id (serial)  | Perfis de acesso e permissões        |
| ControleDePerfilMenu | Id (serial)  | Menus por perfil                     |
| Configuracoes        | Id (serial)  | Configurações do sistema             |
| Assinaturas          | Id (serial)  | Assinaturas digitais (CRBio)         |

#### **Entidades Auxiliares**

| Tabela          | Primary Key  | Descrição                        |
|-----------------|--------------|----------------------------------|
| FichasInternas  | Id (serial)  | Fichas internas de controle      |
| FichasLotes     | Id (serial)  | Lotes de fichas                  |
| FichasPlanilhas | Id (serial)  | Planilhas de fichas              |
| Rastreamentos   | Id (serial)  | Rastreamento de operações        |
| LogArquivos     | Id (serial)  | Log de arquivos                  |
| MemoAuxiliar    | Id (serial)  | Memos auxiliares                 |
| TextosProntos   | Id (serial)  | Textos pré-definidos             |

#### **Entidades de Integração**

| Tabela                         | Primary Key  | Descrição                      |
|--------------------------------|--------------|--------------------------------|
| IntegracaoDadosConfiguracao    | Id (serial)  | Configuração de integrações    |
| IntegracaoDadosLayout          | Id (serial)  | Layouts de integração          |
| IntegracaoDadosExecucao        | Id (serial)  | Execuções de integração        |
| IntegracaoDadosExecucaoArquivo | Id (serial)  | Arquivos de integração         |
| IntegracaoDadosArmazenamento   | Id (serial)  | Armazenamento de integração    |
| IntegracaoDadosPeriodicidade   | Id (serial)  | Periodicidade de integração    |

#### **Tabelas de Domínio (Lookup)**

| Tabela          | Primary Key  | Descrição                |
|-----------------|--------------|--------------------------|
| Sexo            | Id (serial)  | Tipos de sexo            |
| EstadoCivil     | Id (serial)  | Estados civis            |
| Cor             | Id (serial)  | Cores/raças              |
| TipoSanguineo   | Id (serial)  | Tipos sanguíneos         |
| UF              | Id (serial)  | Unidades federativas     |
| Logradouro      | Id (serial)  | Tipos de logradouro      |
| SituacaoExames  | Id (serial)  | Situações de exames      |
| TituloExames    | Id (serial)  | Títulos de exames        |

#### **Tabelas de Monitoramento**

| Tabela                 | Primary Key       | Descrição                    |
|------------------------|-------------------|------------------------------|
| ControleConcorrencia   | Processo (varchar)| Controle de concorrência     |
| ReCaptchaMonitoramento | Id (serial)       | Monitoramento de reCAPTCHA   |
| ERTemporario           | Id (serial)       | ERs temporários              |

### 3.4 Relacionamentos de FK (Mapeados no EF Core)

#### **Pacientes (1) → (N)**
- ExamesRealizados (PacienteId)
- ExamesRealizadosAM (PacienteId)
- Requisitar (PacienteId)
- ExamesPendentes (PacienteId)
- ExamesExportados (PacienteId)
- ExamesImpressos (PacienteId)
- ItensExamesRealizados (PacienteId)
- ItensExamesRealizadosAM (PacienteId)
- FichasInternas (PacienteId)
- FichasLotes (PacienteId)
- FichasPlanilhas (PacienteId)

#### **Instituicao (1) → (N)**
- ExamesRealizados (InstituicaoId)
- ExamesRealizadosAM (InstituicaoId)
- Requisitar (InstituicaoId)
- ExamesPendentes (InstituicaoId)
- ExamesExportados (InstituicaoId)
- ExamesImpressos (InstituicaoId)
- ItensExamesRealizados (InstituicaoId)
- ItensExamesRealizadosAM (InstituicaoId)
- Postos (via relacionamento implícito)

#### **Medicos (1) → (N)**
- ExamesRealizados (MedicoId)
- ExamesRealizadosAM (MedicoId)
- Requisitar (MedicoId)
- ExamesPendentes (MedicoId)
- ExamesExportados (MedicoId)

#### **TabelaExames (1) → (N)**
- ExamesRealizados (TabelaExamesId)
- ExamesRealizadosAM (TabelaExamesId)
- Requisitar (TabelaExamesId)
- ExamesPendentes (TabelaExamesId)
- ExamesExportados (TabelaExamesId)
- ExamesImpressos (TabelaExamesId)
- ItensExamesRealizados (TabelaExamesId)
- ItensExamesRealizadosAM (TabelaExamesId)
- PlanoExames (TabelaExamesId)

#### **Postos (1) → (N)**
- ExamesRealizados (PostoId)
- ExamesRealizadosAM (PostoId)
- Requisitar (PostoId)
- ExamesPendentes (PostoId)

#### **ClasseExames (1) → (N)**
- ExamesPendentes (ClasseExamesId)
- Requisitar (ClasseExamesId)

#### **ExamesRealizados (1) → (N)**
- ItensExamesRealizados (ExameId - implícito)
- ExamesExportados (ExameId)
- FichasInternas (ExameId - implícito)
- FichasLotes (ExameId - implícito)
- FichasPlanilhas (ExameId - implícito)

#### **ControleDeAcesso (1) → (N)**
- ControleDePerfil (ControleDeAcessoId)

### 3.5 Relacionamentos APENAS EM CÓDIGO (FK não declaradas no Banco)

**IMPORTANTE:** Estes relacionamentos são mantidos apenas na aplicação (código C#), sem constraints FK no PostgreSQL:

1. **PlanoExames → TabelaExames**
   - Campo: `TabelaExamesId`
   - Tipo: Relacionamento lógico (sem FK no banco)
   - Uso: Filtra planos por instituição

2. **PlanoExames → ClasseExames**
   - Campo: `ExameId` (referencia ClasseExames.Id)
   - Tipo: Relacionamento lógico
   - Uso: SUS Model (ExameId = 1) replica para todas instituições

3. **Requisitar → ClasseExames**
   - Campo: `ClasseExamesId`
   - Tipo: Relacionamento lógico
   - Uso: Vincula requisição à classe de exame

4. **ItensExamesRealizados → PlanoExames**
   - Campo: `ContaExame` (string de 11 dígitos)
   - Tipo: Relacionamento por código (não por ID)
   - Uso: Hierarquia de contas (folha → conta principal → itens)

5. **ItensExamesRealizadosAM → PlanoExames**
   - Campo: `ContaExame` (string de 11 dígitos)
   - Tipo: Relacionamento por código

6. **FichasInternas/Lotes/Planilhas → Entidades relacionadas**
   - Múltiplos campos `Coluna1` a `Coluna18` (varchar 6)
   - Tipo: Relacionamento flexível por código
   - Uso: Fichas de controle interno com estrutura dinâmica

---

## 4. PADRÕES DE NOMENCLATURA

### 4.1 Models/Entities
- **Classe:** PascalCase (ex: `Pacientes`, `ExamesRealizados`)
- **Nome da Tabela:** Igual ao nome da classe (ex: `Pacientes`, `ExamesRealizados`)
- **Propriedades:** PascalCase (ex: `NomePaciente`, `DataNascimento`)
- **Colunas:** Misto - algumas CamelCase, outras com underscore
- **PK:** Sempre `Id` (int serial)
- **FKs:** Sufixo `Id` (ex: `PacienteId`, `InstituicaoId`)

### 4.2 Controllers
- **Nome:** `{Entidade}Controller.cs` (ex: `PacientesController`, `MedicosController`)
- **Base:** Herdam de `BaseController`
- **Namespace:** `LabWebMvc.MVC.Areas.Controllers`
- **Localização:** `LabWebMvc.MVC/Areas/Controllers/`

### 4.3 ViewModels
- **Prefixo:** `vm` ou `VM` (ex: `vmPacientes`, `VMGeral`)
- **Localização:** `LabWebMvc.MVC/ViewModel/`
- **Propósito:** Validação de formulários, transporte de dados para views

### 4.4 Views
- **Extensão:** `.cshtml`
- **Localização:** `LabWebMvc.MVC/Views/{Controller}/{Action}.cshtml`
- **Padrão:** Razor syntax com Bootstrap
- **Helpers:** `LabWebMvc.MVC/HtmlHelpers/`

### 4.5 Services/Helpers
- **Interfaces:** Prefixo `I` (ex: `IEventLogHelper`, `IValidadorDeSessao`)
- **Implementações:** Nome direto (ex: `EventLogHelper`, `ValidadorDeSessao`)
- **Services:** Sufixo `Service` (ex: `ExclusaoService`, `ReCaptchaService`)

### 4.6 Banco de Dados
- **Tabelas:** PascalCase (ex: `ExamesRealizados`, `ControleDePerfil`)
- **Indexes:** Prefixo `i` + nome da tabela + número (ex: `iPacientes1`, `iPacientes2`)
- **Constraints FK:** Prefixo `i` + tabela_origem + tabela_destino (ex: `iExamesRealizados_Pacientes`)
- **Sequences:** `{tabela}_id_seq` (padrão PostgreSQL)

---

## 5. PADRÕES ARQUITETURAIS

### 5.1 Injeção de Dependência (DI)
Registrado em `Startup.cs`:

```csharp
// Factory Pattern para DbContext
services.AddScoped<IDbFactory, DbFactory>();
services.AddScoped<Db>(sp => { /* criação dinâmica */ });

// Repositório Genérico
services.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));

// Services
services.AddScoped<GeralController>();
services.AddScoped<ExclusaoService>();
services.AddScoped<Imagem>();
services.AddScoped<IValidadorDeSessao, ValidadorDeSessao>();
services.AddSingleton<IEventLogHelper, EventLogHelper>();
```

### 5.2 Controller Base Pattern
Todos controllers herdam de `BaseController`:

```csharp
public abstract class BaseController : Controller
{
    protected readonly IDbFactory _dbFactory;
    protected readonly IValidadorDeSessao _validador;
    protected readonly GeralController _geralController;
    protected readonly IEventLogHelper _eventLogHelper;
    protected readonly Imagem _imagem;
    protected readonly ExclusaoService _exclusaoService;
    protected Db _db;
}
```

**Vantagens:**
- DbContext criado por request via factory
- Validação de sessão centralizada
- Log de eventos disponível em todos controllers
- Serviços de imagem e exclusão injetados

### 5.3 Multi-Tenant via Connection String
- Cada empresa/instituição pode ter banco separado
- `IConnectionService` retorna connection string dinâmica
- `DbFactory` cria DbContext com connection string correta
- Troca de banco em runtime (sem restart)

### 5.4 Controle de Concorrência
- **Tabela:** `ControleConcorrencia` (Processo, DataHora)
- **Service:** `ConcorrenciaService`
- **ExclusaoService:** Valida concorrência antes de deletar
- **Lock de Tabela:** `LOCK TABLE ... IN EXCLUSIVE MODE` no SaveChanges

### 5.5 Reutilização de IDs
- `SaveChangesWithSyncAsync()` recupera IDs excluídos
- Limite configurável de registros (default 99)
- Sincroniza sequência PostgreSQL automaticamente
- Evita gaps em tabelas com limite de registros

---

## 6. BIBLIOTECAS E DEPENDÊNCIAS

### 6.1 Core Framework

| Pacote                                   | Versão | Uso                              |
|------------------------------------------|--------|----------------------------------|
| Npgsql.EntityFrameworkCore               | 8.0.4  | Provider PostgreSQL para EF Core |
| Microsoft.EntityFrameworkCore            | 8.0.19 | ORM Entity Framework Core        |
| Microsoft.EntityFrameworkCore.Relational | 8.0.19 | Suporte a bancos relacionais     |
| Microsoft.EntityFrameworkCore.Tools      | 8.0.19 | Ferramentas de scaffolding       |
| Microsoft.AspNetCore.Identity.UI         | 8.0.19 | Identity UI para autenticação    |

### 6.2 Cloud Storage

| Pacote               | Versão  | Uso                        |
|----------------------|---------|----------------------------|
| AWSSDK.S3            | 4.0.6.2 | Amazon S3 storage          |
| Azure.Storage.Blobs  | 12.25.0 | Azure Blob Storage         |

### 6.3 Google Cloud

| Pacote                              | Versão | Uso                   |
|-------------------------------------|--------|-----------------------|
| Google.Cloud.RecaptchaEnterprise.V1 | 2.18.0 | reCAPTCHA Enterprise  |
| Google.Api.Gax                      | 4.11.0 | Google API Extensions |

### 6.4 PDF Generation

| Pacote              | Versão  | Uso                            |
|---------------------|---------|--------------------------------|
| itext               | 9.3.0   | iText PDF library (comercial)  |
| itext7.licensekey   | 3.1.6   | License key para iText         |
| PdfSharpCore        | 1.3.67  | PDF generation (open source)   |

### 6.5 Image Processing

| Pacote                        | Versão  | Uso                          |
|-------------------------------|---------|------------------------------|
| SixLabors.ImageSharp          | 3.1.11  | Manipulação de imagens       |
| SixLabors.ImageSharp.Drawing  | 2.1.7   | Drawing em imagens           |
| SixLabors.Fonts               | 2.1.3   | Fontes para ImageSharp       |

### 6.6 Utilities

| Pacote                                       | Versão | Uso                      |
|----------------------------------------------|--------|--------------------------|
| Newtonsoft.Json                              | 13.0.4 | JSON serialization       |
| RecaptchaNet                                 | 3.1.0  | reCAPTCHA validation     |
| System.Configuration.ConfigurationManager    | 9.0.10 | Configuration management |
| Microsoft.Extensions.Hosting.WindowsServices | 9.0.0  | Windows Service hosting  |
| System.ServiceProcess.ServiceController      | 9.0.10 | Service control          |
| System.Security.Cryptography.Xml             | 9.0.10 | XML cryptography         |
| System.Drawing.Common                        | 9.0.10 | GDI+ drawing (Windows)   |

---

## 7. ÁREAS FUNCIONAIS (MODULARIZAÇÃO)

### 7.1 Estrutura de Areas
```
LabWebMvc.MVC/Areas/
├── Controllers/        # Todos os controllers principais
├── Concorrencias/      # Controle de concorrência
├── Connections/        # Gerenciamento de conexões
├── ControlGeral/       # Controles gerais
├── ControleDeImagens/  # Manipulação de imagens
├── ExpressionCombiner/ # Combinador de expressões LINQ
├── Identity/           # Autenticação e autorização
├── Impressoras/        # Serviços de impressão
├── Middleware/         # Middlewares customizados
├── ServicosDatabase/   # Serviços de banco de dados
├── Strategy/           # Pattern Strategy (integrações)
├── Utils/              # Utilitários
└── Validations/        # Validações customizadas
```

### 7.2 Controllers Principais (21 controllers)

| Controller                     | Responsabilidade                      |
|--------------------------------|---------------------------------------|
| HomeController                 | Dashboard, página inicial             |
| PacientesController            | CRUD de pacientes                     |
| MedicosController              | CRUD de médicos                       |
| InstituicoesController         | CRUD de instituições                  |
| PostosController               | CRUD de postos de coleta              |
| RequisitarController           | Requisições de exames                 |
| PlanoExamesController          | Gestão de planos de exames            |
| PlanoExamesItensController     | Itens do plano de exames              |
| ClasseExamesController         | Classes de exames                     |
| SenhasController               | Fila de senhas                        |
| ConfiguracoesController        | Configurações do sistema              |
| GraficosController             | Gráficos e indicadores                |
| MensagemController             | Mensagens e notificações              |
| MenuController                 | Gerenciamento de menu                 |
| ConnectionController           | Troca de conexão (banco)              |
| ReCaptchaTrackerController     | Monitoramento reCAPTCHA               |
| ReleaseController              | Release notes                         |
| ImplantacaoController          | Implantação inicial                   |
| BaseController                 | Base abstrata para controllers        |

---

## 8. REGRAS DE NEGÓCIO CRÍTICAS

### 8.1 Plano de Exames - Modelo SUS
- `ExameId = 1` (SUS) é o modelo base
- Alterações no SUS replicam para TODAS as instituições
- `ContaExame` estrutura: `XX.XX.XXX.XXXX` (11 dígitos)
  - Posição 1-2: Tipo (11=crédito, fixo)
  - Posição 3-4: Folha (01-99)
  - Posição 5-7: Conta principal
  - Posição 8-11: Item específico
- Validação por prefixo de 7 dígitos (StartsWith)

### 8.2 Transações de Requisição
- Médico e Paciente salvos FORA da transação de exames
- Rollback de exames NÃO afeta cadastro de médico/paciente
- Validação de FK antes de DELETE (não confiar em exception do banco)

### 8.3 Data/Hora
- **NUNCA** usar `DateTime.Now` ou `DateTime.UtcNow` para gravar no banco
- Usar `_geralController.ObterDataHoraServidor()` ou `SELECT NOW()`
- PostgreSQL: `timestamp without time zone` → `DateTime` com `Kind=Unspecified`
- PostgreSQL: `timestamp with time zone` → `DateTime` com `Kind=UTC`

### 8.4 Exclusão de Registros
- Verificar FKs em tabelas filhas antes de deletar
- Mensagem assertiva se houver vínculos
- `ExclusaoService` com controle de concorrência
- `DeleteOrphans()` remove órfãos automaticamente

---

## 9. INTEGRAÇÕES

### 9.1 Exportação de Dados
- **Strategy Pattern:** `ExportacaoFactory` cria estratégia por tipo
- **Destinos:** AWS S3, Azure Blob Storage, FTP
- **Formatos:** JSON, XML, CSV
- **Agendamento:** Via `ServicoExportacao` (Worker Service)

### 9.2 Importação de Dados
- Layouts configuráveis via `IntegracaoDadosLayout`
- Validação de dados antes de inserir
- Log de execuções em `IntegracaoDadosExecucao`

### 9.3 reCAPTCHA
- Google reCAPTCHA Enterprise v1
- Monitoramento de tentativas em `ReCaptchaMonitoramento`
- Validação via `ReCaptchaService`

### 9.4 Armazenamento de Arquivos
- **Local:** File system (desenvolvimento)
- **Cloud:** AWS S3 ou Azure Blob Storage (produção)
- **Tipos:** Laudos PDF, imagens, timbres, logomarcas

---

## 10. PADRÕES DE FRONTEND

### 10.1 Views Razor
- Bootstrap (versão original do LabWeb7)
- jQuery + JavaScript vanilla
- DataTables para grids
- Inputmask para formulários
- site.js carregado 2x no _Layout.cshtml (head + body)

### 10.2 JavaScript
- **Arquivo Principal:** `wwwroot/js/site.js`
- **Bibliotecas:**
  - jQuery
  - Bootstrap
  - DataTables
  - Inputmask 5.x
- **NÃO** adicionar terceira referência ao site.js

### 10.3 CSS
- Bootstrap customizado
- DataTables styles
- Custom styles em `wwwroot/css/`

---

## 11. SEGURANÇA E AUTENTICAÇÃO

### 11.1 Autenticação
- ASP.NET Core Identity (Cookie-based)
- `ControleDeAcesso` - Login/senha customizado
- `ControleDePerfil` - Perfis de acesso
- `ControleDePerfilMenu` - Menus por perfil

### 11.2 Criptografia
- Senhas criptografadas (ver `Extensions/Criptografias/`)
- `CriptoDecripto.cs` - Criptografia simétrica
- `Senhas.cs` model - Gerenciamento de senhas

### 11.3 Autorização
- Perfis de acesso com níveis de menu
- Validação de sessão via `IValidadorDeSessao`
- Controle de concorrência por usuário

---

## 12. LOGGING E MONITORAMENTO

### 12.1 Event Viewer (Windows)
- `IEventLogHelper` - Interface singleton
- Logs de erro do EF Core
- Logs de operações críticas
- Formato: `LABWEB7 ::: {mensagem}`

### 12.2 Log de Arquivos
- `LogArquivos` model - Registro de arquivos processados
- `LoggerFile.cs` - Logger em arquivo físico

### 12.3 Rastreamento
- `Rastreamentos` model - Rastreamento de operações
- `ControleConcorrencia` - Controle de processos concorrentes

---

## 13. CONVENÇÕES DE DESENVOLVIMENTO

### 13.1 Code Marking
```csharp
//Feito pelo Qoder em 21/04/2026
// ... código ...
//..Qoder
```

### 13.2 Encoding
- `.cs`, `.cshtml`, `.csproj`: UTF-8 com BOM
- `.js`: Manter existente (não adicionar/remover BOM)
- `.json`, `.css`, `.md`: UTF-8 sem BOM
- Português-Brasil com acentuação correta

### 13.3 Arquivos Protegidos (NÃO modificar sem autorização)
- `.editorconfig`, `appsettings.json`, `appsettings.Development.json`
- `Program.cs`, `Startup.cs`, `web.config`, `Settings.cs`
- Pastas: `.vs/`, `.git/`
- Scripts MSSQL originais

### 13.4 Git
- SEMPRE perguntar antes de executar operações Git
- NUNCA executar `git push`, `git commit`, `git merge` sem autorização
- Pode consultar histórico, branches, diffs

---

## 14. DEPLOY E EXECUÇÃO

### 14.1 Ambientes
- **Desenvolvimento:** PostgreSQL local, Windows
- **Produção:** Linux ou Windows, PostgreSQL remoto

### 14.2 Configurações por Ambiente
- `appsettings.json` - Configurações gerais
- `appsettings.Development.json` - Desenvolvimento
- `appsettings.Linux.json` - Linux production

### 14.3 Modos de Execução
1. **IIS/Kestrel:** Web application padrão
2. **Windows Service:** Via `WindowsService` project
3. **Linux Service:** systemd service

### 14.4 Worker Services
- `ServicoExportacao` - Exportação assíncrona
- Pode rodar como serviço separado ou integrado

---

## 15. PONTOS DE ATENÇÃO E DÍVIDAS TÉCNICAS

### 15.1 ModeloDeDados Projeto
- Parece ser um projeto espelho/legado
- Modelos duplicados com LabWebMvc.MVC/Models
- **Recomendação:** Verificar se ainda é necessário

### 15.2 Migrations
- Projeto NÃO usa EF Migrations
- Scripts SQL manuais para schema changes
- **Risco:** Divergência entre modelo e banco
- **Mitigação:** Usar steering de análise integrada

### 15.3 Relacionamento por Código (ContaExame)
- `ItensExamesRealizados` → `PlanoExames` via string `ContaExame`
- Não é FK tradicional, é relacionamento lógico
- **Complexidade:** Validação requer substring matching (7 dígitos)

### 15.4 Multi-Tenant por Banco
- Cada empresa tem banco próprio
- Troca de conexão em runtime
- **Desafio:** Manter schemas sincronizados entre bancos

### 15.5 Reutilização de IDs
- `SaveChangesWithSyncAsync()` reutiliza IDs excluídos
- Limite de 99 registros (configurável)
- **Risco:** Concorrência na atribuição de IDs
- **Mitigação:** Table locking com `EXCLUSIVE MODE`

### 15.6 Hardcoded Values
- `ExameId = 1` para SUS model (usar `IdPadrao.SUS` enum)
- Strings mágicas em validações
- **Recomendação:** Centralizar em constantes/enums

---

## 16. FLUXOS CRÍTICOS DO SISTEMA

### 16.1 Fluxo de Requisição de Exames
```
1. Usuário seleciona Instituição + Posto + Médico + Paciente
2. Salva Médico/Paciente (FORA da transação)
3. Seleciona exames do PlanoExames (filtrado por TabelaExamesId)
4. Cria registros em Requisitar (DENTRO da transação)
5. Se falhar: rollback apenas dos exames, mantém médico/paciente
```

### 16.2 Fluxo de Realização de Exames
```
1. Requisitar → ItensExamesRealizados (mover dados)
2. Resultado + Laudo (byte[] PDF)
3. ExamesRealizados (registro principal)
4. Liberar exame (Liberacao = 1)
5. Imprimir/Exportar (ExamesImpressos/ExamesExportados)
```

### 16.3 Fluxo de Alteração de Plano de Exames
```
1. Alterar item no SUS (ExameId = 1)
2. Replicar para TODAS as instituições (mesmo ContaExame)
3. Cenário 1: Preço individual (SalvarItemGrid) - só a instituição
4. Cenário 2: Preço em massa (SalvarAlteracaoPlanoExamesItens) - todas
5. Validar FKs antes de excluir (ItensExamesRealizados, Requisitar)
```

---

## 17. ENUMS E CONSTANTES IMPORTANTES

### 17.1 IdPadrao (provável)
```csharp
public enum IdPadrao
{
    SUS = 1  // Modelo base de exames
}
```

### 17.2 Recaptcha (ExtensionsMethods/Enumerations)
```csharp
public enum Recaptcha
{
    // Configurações de reCAPTCHA
}
```

### 17.3 Status de Exame
- `Liberacao`: 0 = Pendente, 1 = Liberado
- `Baixado`: 0 = Não, 1 = Sim
- `Situacao`: Referência a `SituacaoExames` tabela
- `EnviarEmail`: 0 = Não, 1 = Sim

---

## 18. RECURSOS AVANÇADOS

### 18.1 Expression Combiner
- `Areas/ExpressionCombiner/` - Combina expressões LINQ dinamicamente
- Uso: Filtros complexos de pesquisa

### 18.2 Strategy Pattern para Integrações
- `Areas/Strategy/` - Strategies de exportação/importação
- Factory: `ExportacaoFactory`
- Extensível para novos formatos

### 18.3 Middleware Customizados
- `Areas/Middleware/` - Middlewares de request/response
- Validações, logging, tratamento de erros

### 18.4 Impressão
- `Areas/Impressoras/` - Abstração de impressão
- Windows: `ImpressoraWindows`
- Linux: `ImpressoraLinux`
- Interface: `IImpressoraCupom`

---

## 19. TABELAS DE CONTROLE DE ACESSO

**Nota:** Tabelas `ControleDe*` ficam em script separado:
`Biblioteca SQL/Base de Dados Vazio Postgresql/Cria Tabelas de Controle de Acesso.sql`

### Estrutura:
- `ControleDeAcesso` - Usuários (login, senha, perfil)
- `ControleDePerfil` - Perfis (permissões de menu)
- `ControleDePerfilMenu` - Menu items por perfil
- `ControleDePerfilModelo` - Modelos de perfil
- `ControleDePerfilTipo` - Tipos de perfil

---

## 20. CONCLUSÃO E RECOMENDAÇÕES

### 20.1 Pontos Fortes
✅ Arquitetura bem organizada com separação de concerns  
✅ DI configurado corretamente  
✅ Multi-tenant com isolamento por banco  
✅ Controle de concorrência robusto  
✅ Validações de FK antes de DELETE  
✅ Server-side datetime handling  
✅ Logging centralizado no Event Viewer  

### 20.2 Melhorias Recomendadas
🔧 Implementar EF Migrations para controle de schema  
🔧 Centralizar hardcoded values em constantes/enums  
🔧 Documentar relacionamentos lógicos (ContaExame)  
🔧 Unificar projetos de modelo (ModeloDeDados vs LabWebMvc.MVC/Models)  
🔧 Adicionar testes unitários para BLL  
🔧 Implementar health checks para monitoramento  
🔧 Migrar para async/await em todas as operações de I/O  

### 20.3 Riscos Identificados
⚠️ Sem migrations: risco de divergência modelo/banco  
⚠️ Relacionamento por código (ContaExame): frágil a mudanças  
⚠️ Reutilização de IDs: possível race condition sem locking adequado  
⚠️ Hardcoded ExameId=1: vulnerável a refatoração  
⚠️ Múltiplos projetos com dependências circulares potenciais  

---

**Fim da Análise Arquitetural**  
*Documento gerado automaticamente por Qoder AI em 21/04/2026*
