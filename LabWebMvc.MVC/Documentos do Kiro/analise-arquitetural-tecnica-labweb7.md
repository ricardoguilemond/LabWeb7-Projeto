# Análise Arquitetural Técnica Completa — LabWeb7Mvc.MVC

**Data:** 15/08/2026  
**Autor:** Kiro (análise baseada em investigação direta no código-fonte)  
**Versão:** 1.0

---

## Resumo Executivo

O LabWeb7Mvc é um **sistema de gestão laboratorial (LIMS)** construído em ASP.NET Core 8 MVC,
destinado a laboratórios de análises clínicas. Opera em modelo **multi-tenant** (um banco
PostgreSQL por empresa-cliente), com autenticação baseada em cookies, reCAPTCHA Enterprise do
Google, criptografia BCrypt para senhas, e serviços Windows para integrações agendadas.

O sistema migra de um legado Delphi/Firebird para .NET/PostgreSQL, mantendo compatibilidade
com dados importados do sistema anterior. A solução compreende 5 projetos, 45+ entidades no
DbContext, 30+ controllers, e uma camada de frontend com jQuery + DataTables 2.x + Bootstrap 5.

**Domínio principal:** requisição de exames, lançamento de resultados, cadastros (pacientes,
médicos, instituições), planos de exames com hierarquia ContaExame, geração de laudos,
integração/exportação de dados, e controle financeiro (faturamento/recebimentos).

---

## 1. Visão Geral da Solução

### 1.1 Projetos da Solução

| Projeto            | Tipo                  | Framework | Responsabilidade                          |
|--------------------|-----------------------|-----------|-------------------------------------------|
| LabWebMvc.MVC      | ASP.NET Core MVC Web  | net8.0    | Aplicação principal (UI + API + negócio)  |
| BLL                | Class Library         | net8.0    | Utilidades, PDF, tempo do servidor        |
| ExtensionsMethods  | Class Library         | net8.0    | Helpers: EventLog, sessão, email, crypto  |
| ServicoExportacao  | Worker Service        | net8.0    | BackgroundService para integração         |
| WindowsService     | Console/Win Service   | net8.0    | Serviço Windows para integração cíclica   |

### 1.2 Dependências entre Projetos

```
LabWebMvc.MVC ──────► BLL
     │                  ▲
     └──────────────► ExtensionsMethods ──► BLL
     
WindowsService ─────► LabWebMvc.MVC
     │                 ExtensionsMethods
     └─────────────► ServicoExportacao ──► LabWebMvc.MVC
```

### 1.3 Tecnologias Utilizadas

| Aspecto                    | Tecnologia                                           |
|----------------------------|------------------------------------------------------|
| Framework                  | ASP.NET Core 8.0 (C# 12)                             |
| ORM                        | Entity Framework Core 8.0.19                         |
| Banco de dados             | PostgreSQL (Npgsql 8.0.4)                            |
| Banco legado (importação)  | Firebird 2.5.x (via ODBC)                            |
| Frontend                   | jQuery + Bootstrap 5 + DataTables 2.x + SweetAlert2  |
| Autenticação               | Cookie Authentication + Session                      |
| Hash de senhas             | BCrypt.Net-Next 4.1.0                                |
| reCAPTCHA                  | Google Cloud RecaptchaEnterprise.V1 2.18.0           |
| PDF                        | PdfSharpCore 1.3.67                                  |
| Imagens                    | SixLabors.ImageSharp 3.1.11                          |
| Cloud Storage              | AWS S3 (AWSSDK.S3 4.0.6.2), Azure Blob 12.25.0       |
| Comunicação real-time      | SignalR (importação Firebird)                        |
| Logging                    | Windows Event Log + arquivo (Linux)                  |
| Documentos                 | DocumentFormat.OpenXml 3.1.0                         |

### 1.4 Arquitetura Adotada

**Padrão:** MVC monolítico com Repository Pattern genérico + Services.

**Separação de responsabilidades:**
- **Controllers** — orquestração de fluxo, validação de sessão, roteamento
- **GeralController** — serviço utilitário (data/hora, validações genéricas)
- **Services** — ExclusaoService, ConcorrenciaService, IntegracaoService
- **Repository** — `IRepositorio<T>` genérico com DbContext
- **Models** — entidades EF Core (cada classe mapeia uma tabela PostgreSQL; relacionamentos 1:N e 1:1 entre entidades)
- **ViewModels** — DTOs para Views (vmRequisitar, vmSenhas, etc.)
- **BLL** — lógica de negócio isolada (tempo, PDF, helpers)

### 1.5 Fluxo de uma Requisição HTTP

```
Browser → Kestrel/IIS → Middleware Pipeline:
  → HTTPS Redirect
  → Static Files
  → Cookie Policy
  → Routing
  → Localization (pt-BR)
  → Session (restaura SessionStringConexao)
  → Authentication (Cookie Auth)
  → Authorization
  → Controller (BaseController.OnActionExecuting restaura tenant)
    → GeralController.Validacao (verifica sessão)
    → Service/Repository/DbContext
    → PostgreSQL (banco do tenant)
  → View (Razor) → Browser
```

### 1.6 Multi-Tenancy

O sistema implementa **multi-tenancy por banco de dados separado**:

1. `LABWEB7Empresas` — banco administrativo central com tabela `EmpresaCliente`
2. Cada empresa-cliente tem seu próprio banco (ex: `LABWEB7{CNPJ}`)
3. No login, `ValidacoesDeSenhas.RetornaValidacaoLogin` localiza a empresa pelo email
4. A connection string do tenant é armazenada na Session (`SessionStringConexao`)
5. `BaseController.OnActionExecuting` restaura a conexão em cada requisição
6. `IConnectionService.SetConnectionString` atualiza dinamicamente o DbContext

---

## 2. Estrutura Física e Lógica

### 2.1 Diretórios Principais (LabWebMvc.MVC)

| Diretório                  | Finalidade                                                 |
|----------------------------|------------------------------------------------------------|
| `Areas/Controllers/`       | Todos os controllers do sistema (30+)                      |
| `Areas/Concorrencias/`     | Serviços de controle de concorrência e exclusão            | 
| `Areas/ControleDeImagens/` | Manipulação de imagens (assinaturas, logos)                |
| `Areas/Impressoras/`       | Impressão de cupons (Windows/Linux)                        |
| `Areas/ServicosDatabase/`  | IConnectionService, IDbFactory, DatabaseContextFactory     |
| `Areas/Strategy/`          | Estratégias de exportação                                  |
| `Areas/Utils/`             | Classes utilitárias (Utils, BasePadrao, HttpContextHelper) |
| `Areas/Validations/`       | ValidacoesDeSenhas, SessionFilter                          |
| `IndicadoresGraficos/`     | Geração de gráficos (Chart.js server-side)                 |
| `Integracoes/`             | Importação (Firebird), Exportação, Interfaces              |
| `Interfaces/`              | IRepositorio, Repositorio, Criptografias                   |
| `Mensagens/`               | Mensagens padronizadas (pt-BR)                             |
| `Models/`                  | Entidades EF Core (45+) + DbContext (db.cs)                |
| `ViewModel/`               | ViewModels (30+), incluindo CargaDados/                    |
| `Views/`                   | Razor Views (24 pastas)                                    |
| `wwwroot/`                 | Assets estáticos (js/, css/, lib/, images/)                |

### 2.2 Arquivos de Configuração Críticos

| Arquivo                        | Finalidade                                         |
|--------------------------------|----------------------------------------------------|
| `Program.cs`                   | Host builder com suporte a Windows Service         |
| `Startup.cs`                   | DI, Auth, Session, Pipeline, Routing               |
| `appsettings.json`             | Connection strings, secrets, reCAPTCHA, email      |
| `global.json`                  | Fixa versão do SDK .NET                            |
| `LabWebMvc.MVC.csproj`         | Dependências NuGet, target framework               |
| `web.config`                   | Configuração IIS InProcess hosting                 |

---

## 3. Linguagem e Recursos do C# 12

### Recursos Identificados no Código

| Recurso                   | Uso Real                              | Complexidade |
|---------------------------|---------------------------------------|--------------|
| `async/await`             | Controllers, Services, DB ops         | MÉDIA        |
| LINQ                      | Queries EF Core, filtros dinâmicos    | MÉDIA        |
| Nullable reference types  | Habilitado (`<Nullable>enable`)       | BAIXA        |
| Records                   | `ApiResult` no RequisitarController   | BAIXA        |
| Pattern matching          | Switch expressions em EventLogHelper  | BAIXA        |
| Lambda expressions        | DI registrations, LINQ, delegates     | MÉDIA        |
| Generics                  | `IRepositorio<T>`, `Repositorio<T>`   | MÉDIA        |
| Expression trees          | Filtros dinâmicos em ExclusaoService  | ALTA         |
| Reflection                | `SaveChangesWithSyncAsync` (gap-fill) | ALTÍSSIMA    |
| Collection expressions    | `= []` para inicialização             | BAIXA        |
| Static local functions    | Em ValidacoesDeSenhas                 | MÉDIA        |
| Partial classes           | Entidades EF Core                     | BAIXA        |
| IDisposable               | CriptoDecripto, DbContext             | MÉDIA        |
| CancellationToken         | Operações assíncronas com timeout     | MÉDIA        |
| Tuples                    | `(DateTime, DateTime)` em ranges UTC  | BAIXA        |
| Extension methods         | StringExtensions, FiltrarPorConteudo  | MÉDIA        |
| Raw string literals       | SQL inline                            | BAIXA        |

---

## 4. Classificação de Complexidade

### Componentes por Nível de Complexidade

| Componente              | Localização              | Compl.    | Justificativa              |
|-------------------------|--------------------------|-----------|----------------------------|
| `SaveChangesWithSync`   | Models/db.cs             | CRÍTICA   | Reflection + lock +        |
| `Async`                 |                          |           | gap-fill + concorrência    |
|                         |                          |           |                            |
| `RetornaValidacao`      | Areas/Validations/       | CRÍTICA   | Multi-DB routing +         |
| `Login`                 | ValidacoesDeSenhas       |           | login + primeiro acesso    |
|                         |                          |           |                            |
| `SalvarRequisicao`      | Areas/Controllers/       | ALTÍSSIMA | Orquestra paciente +       |
|                         | Requisitar               |           | médico + exame +           |
|                         |                          |           | itens + edição             |
|                         |                          |           |                            |
| `FirebirdImporter`      | Integracoes/             | ALTÍSSIMA | ODBC + reconexão +         |
|                         | Importacao               |           | schema + batch +           |
|                         |                          |           | encoding                   |
|                         |                          |           |                            |
| `DeleteOrphans`         | Models/db.cs             | ALTA      | Reflection sobre todos     |
|                         |                          |           | DbSets + FK detect         |
|                         |                          |           |                            |
| `OnActionExecuting`     | Areas/Controllers/       | ALTA      | Troca dinâmica de          |
| (BaseController)        | BaseController           |           | DbContext por tenant       |
|                         |                          |           |                            |
| `ExclusaoService`       | Areas/                   | MÉDIA     | Genérico com lock          |
|                         | Concorrencias            |           | de concorrência            |
|                         |                          |           |                            |
| `GeralController`       | Areas/Controllers        | MÉDIA     | Múltiplas sobrecargas      |
|                         |                          |           | + timezone                 |
|                         |                          |           |                            |
| `CriptoDecripto`        | Interfaces/              | MÉDIA     | BCrypt + AES legacy        |
|                         | Criptografias            |           | + cipher customizada       |
|                         |                          |           |                            |
| `TempoServidor`         | BLL                      | BAIXA     | SELECT NOW() +             |
| `PostgreSQL`            |                          |           | fallback                   |
|                         |                          |           |                            |
| `Repositorio<T>`        | Interfaces               | BAIXA     | CRUD genérico padrão       |

---

## 5. Frameworks e Bibliotecas

### Inventário Completo de Dependências (LabWebMvc.MVC)

| Pacote                                  | Versão  | Finalidade                          | Criticidade |
|-----------------------------------------|---------|-------------------------------------|-------------|
| Npgsql.EntityFrameworkCore.PostgreSQL   | 8.0.4   | Provider EF Core PostgreSQL         | CRÍTICA     |
| Npgsql                                  | 8.0.4   | Driver PostgreSQL .NET              | CRÍTICA     |
| Microsoft.EntityFrameworkCore.*         | 8.0.19  | ORM                                 | CRÍTICA     |
| BCrypt.Net-Next                         | 4.1.0   | Hash de senhas                      | CRÍTICA     |
| Microsoft.AspNetCore.Identity.UI        | 8.0.19  | Suporte Identity (parcial)          | ALTA        |
| FirebirdSql.Data.FirebirdClient         | 10.3.2  | Conexão Firebird (schema only)      | ALTA        |
| System.Data.Odbc                        | 8.0.0   | ODBC para importação Firebird       | ALTA        |
| Google.Cloud.RecaptchaEnterprise.V1     | 2.18.0  | reCAPTCHA Enterprise                | ALTA        |
| Google.Cloud.Monitoring.V3              | 3.5.0   | Métricas reCAPTCHA                  | MÉDIA       |
| AWSSDK.S3                               | 4.0.6.2 | Storage AWS (preparado, não ativo)  | BAIXA       |
| Azure.Storage.Blobs                     | 12.25.0 | Storage Azure (preparado, não ativo)| BAIXA       |
| DocumentFormat.OpenXml                  | 3.1.0   | Geração Excel/Word                  | MÉDIA       |
| PdfSharpCore                            | 1.3.67  | Geração de PDF                      | MÉDIA       |
| SixLabors.ImageSharp                    | 3.1.11  | Manipulação de imagens              | MÉDIA       |
| RecaptchaNet                            | 3.1.0   | reCAPTCHA v2/v3 validation          | MÉDIA       |
| Microsoft.Extensions.Hosting.WinServices| 9.0.0   | Suporte a Windows Service           | MÉDIA       |
| System.ServiceProcess.ServiceController | 9.0.10  | Controle de serviços Windows        | BAIXA       |
| System.Configuration.ConfigManager      | 9.0.10  | Configuração legada                 | BAIXA       |

### Frontend

| Biblioteca     | Finalidade                              | Criticidade |
|----------------|-----------------------------------------|-------------|
| jQuery 3.x     | Manipulação DOM, AJAX, delegação        | CRÍTICA     |
| DataTables 2.x | Grids de dados com paginação/filtro     | CRÍTICA     |
| Bootstrap 5    | Layout responsivo, componentes UI       | ALTA        |
| SweetAlert2    | Modais de confirmação/alerta            | ALTA        |
| Chart.js       | Gráficos e indicadores                  | MÉDIA       |
| Font Awesome 6 | Ícones                                  | BAIXA       |
| InputMask      | Máscaras de input (CPF, telefone)       | MÉDIA       |

---

## 6. Banco de Dados

### 6.1 Bancos Utilizados

| Banco              | Uso                                         | Status      |
|--------------------|---------------------------------------------|-------------|
| PostgreSQL         | Banco principal (produção e desenvolvimento)| ATIVO       |
| Firebird 2.5.x     | Fonte de importação (sistema legado Delphi) | IMPORTAÇÃO  |
| SQL Server         | Connection strings presentes mas inativas   | RESERVA     |

### 6.2 DbContext e DbSets (45+ entidades)

**Arquivo:** `LabWebMvc.MVC/Models/db.cs`

Entidades principais (por domínio):

**Cadastros:**
- Pacientes, Medicos, Instituicao, Postos, Empresa, Assinaturas

**Exames:**
- ExamesRealizados, ItensExamesRealizados, ExamesRealizadosAM, ItensExamesRealizadosAM
- PlanoExames, ClasseExames, TabelaExames, ExameReferencia
- ExamesPendentes, ExamesExportados, ExamesImpressos

**Controle de Acesso:**
- Senhas, UsuariosWeb, ControleDeAcesso, ControleDePerfil
- ControleDePerfilMenu, ControleDePerfilModelo, ControleDePerfilTipo

**Financeiro:**
- CatalogoRecebimentos, CatalogoRecebimentosExames, CatalogoRecebimentosFormas
- FormasRecebimento, ContasRecebimento

**Integração:**
- IntegracaoDadosConfiguracao, IntegracaoDadosLayout, IntegracaoDadosExecucao
- IntegracaoDadosExecucaoArquivo, IntegracaoDadosArmazenamento, IntegracaoDadosPeriodicidade

**Auxiliares:**
- Cor, EstadoCivil, Logradouro, Sexo, TipoSanguineo, UF, SituacaoExames
- ERTemporario, MemoAuxiliar, TextosProntos, TituloExames, Configuracoes
- ControleConcorrencia, ReCaptchaMonitoramento, LogArquivos, Rastreamentos

**Fichas e Planilhas:**
- FichasInternas, FichasLotes, FichasPlanilhas

### 6.3 Estratégia de Acesso a Dados

- **EF Core** para operações CRUD (via IRepositorio<T>)
- **SQL direto** (FromSqlRaw) para locks (`FOR UPDATE`) e operações críticas
- **Npgsql direto** (NpgsqlConnection/NpgsqlCommand) para operações multi-banco
  (LABWEB7Empresas), importação, e validações cross-database
- **Transações EF Core nativas** (`BeginTransactionAsync`) para operações compostas
- **Sem Migrations** — schema gerenciado externamente via DDL scripts

---

## 7. Modelos e Entidades

### 7.1 Classificação dos Modelos

| Tipo            | Exemplos                                       | Localização               |
|-----------------|------------------------------------------------|---------------------------|
| Entity/Model    | Pacientes, ExamesRealizados, Senhas            | Models/                   |
| ViewModel       | vmRequisitar, vmSenhas, vmPacientes            | ViewModel/                |
| DTO interno     | DadosItemCupom, ApiResult                      | Inline no Controller      |
| Config/Settings | GoogleReCaptchaSettings                        | Interfaces/Criptografias/ |

### 7.2 Entidade Central: ExamesRealizados

```
ExamesRealizados (Header do exame do paciente)
├── Id (PK, auto-increment)
├── PacienteId (FK → Pacientes)
├── TabelaExamesId (FK → TabelaExames)
├── InstituicaoId (FK → Instituicao)
├── PostoId (FK → Postos, nullable)
├── MedicoId (FK → Medicos)
├── Sequencial (int, gerado por instituição)
├── LaboratorioApoio, ControleApoio
├── DataIni (timestamptz), DataFim, DataExame, DataEntrega
├── Liberacao, Baixado, Situacao, TotalImpresso
├── Faturado, EmCatalogoRecebimentos
└── Navigation Properties:
    ├── Pacientes, Medicos, Instituicao, Postos, TabelaExames
    ├── ItensExamesRealizados (1:N)
    ├── ExamesExportados (1:N)
    ├── FichasInternas, FichasLotes, FichasPlanilhas (1:N)
    └── CatalogoRecebimentosExames (1:N)
```

### 7.3 Modelo vmRequisitar (ViewModel complexo)

O `vmRequisitar` agrega:
- Dados do formulário de requisição
- Sub-ViewModels: VmPacientes, VmInstituicao, VmPostos, VmTabelaExames, VmMedicos, VmPlanoExames
- Listas auxiliares: ListaPacientes, ListaInstituicoes, ListaPostos, ListaTabelas, ListaMedicos
- ListaCupom (exames selecionados)
- Campos informativos (NomePaciente, CRM, SiglaInstituicao, etc.)
- TabelaExamesIdOriginal (para edição com troca de tabela)

---

## 8. Relacionamentos entre Tabelas

### Diagrama Textual dos Principais Relacionamentos

```
Pacientes ──1:N──► ExamesRealizados
Medicos ──1:N──► ExamesRealizados
Instituicao ──1:N──► ExamesRealizados
Postos ──1:N──► ExamesRealizados (nullable)
TabelaExames ──1:N──► ExamesRealizados

ExamesRealizados ──1:N──► ItensExamesRealizados
ExamesRealizados ──1:N──► ExamesExportados
ExamesRealizados ──1:N──► FichasInternas/Lotes/Planilhas

Pacientes ──1:N──► ItensExamesRealizados
Pacientes ──1:N──► ExamesPendentes
Pacientes ──1:N──► ExamesImpressos

ClasseExames ──1:N──► ExamesPendentes
PlanoExames ── relação lógica ── TabelaExames + ClasseExames (ContaExame)

Senhas ──1:1──► UsuariosWeb
Senhas ──1:N──► ControleDeAcesso ──1:N──► ControleDePerfil

Instituicao ──1:N──► Postos

CatalogoRecebimentos ──1:N──► CatalogoRecebimentosExames
CatalogoRecebimentos ──1:N──► CatalogoRecebimentosFormas
```

### Delete Behavior

Todas as FKs usam `DeleteBehavior.ClientSetNull` — exclusão em cascata é **controlada
pelo código**, não pelo banco. Isso exige validação explícita antes de qualquer DELETE.

---

## 9. Controllers

### Controllers Principais e Complexidade

| Controller                    | Responsabilidade                  | Complexidade |
|-------------------------------|-----------------------------------|--------------|
| RequisitarController          | Requisição completa de exames     | ALTÍSSIMA    |
| ResultadoExamesController     | Lançamento de resultados          | ALTA         |
| HomeController                | Login, dashboard, reCAPTCHA       | ALTA         |
| CargaDadosController          | Importação Firebird→PostgreSQL    | ALTÍSSIMA    |
| SenhasController              | Gestão de usuários/senhas         | ALTA         |
| PacientesController           | CRUD pacientes                    | MÉDIA        |
| PlanoExamesItensController    | Hierarquia ContaExame             | ALTA         |
| ConsultarExamesController     | Consulta e liberação de exames    | MÉDIA        |
| CatalogoRecebimentosController| Financeiro/recebimentos           | MÉDIA        |
| GraficosController            | Indicadores gráficos              | MÉDIA        |
| GeralController               | Serviço utilitário (não é MVC)    | MÉDIA        |

### Padrão dos Controllers

Todos herdam de `BaseController` que injeta:
- `IDbFactory` → cria DbContext
- `IValidadorDeSessao` → valida sessão
- `GeralController` → utilitários de data/hora
- `IEventLogHelper` → logging
- `Imagem` → manipulação de imagens
- `ExclusaoService` → exclusão com concorrência
- `IConnectionService` → connection string do tenant

---

## 10. Services e Camada de Negócio

| Serviço                    | Responsabilidade                               | Complexidade |
|----------------------------|------------------------------------------------|--------------|
| ValidacoesDeSenhas         | Login, criação de usuário, multi-DB routing    | CRÍTICA      |
| IntegracaoService          | Integração agendada (exportação)               | ALTA         |
| ExclusaoService            | Exclusão genérica com lock de concorrência     | MÉDIA        |
| ConcorrenciaService        | Lock pessimista via tabela ControleConcorrencia| MÉDIA        |
| ReCaptchaService           | Wrapper para Google reCAPTCHA Enterprise       | MÉDIA        |
| FirebirdImporter           | Importação ODBC Firebird→PostgreSQL            | ALTÍSSIMA    |
| CargaDadosExecutor         | Orquestrador da importação                     | ALTA         |
| SchemaComparer             | Comparação de schema Firebird vs PostgreSQL    | ALTA         |
| ServicoExportacaoPacientes | Exportação de dados de pacientes               | MÉDIA        |
| TempoServidorPostgreSQL    | Data/hora UTC do servidor PostgreSQL           | BAIXA        |

---

## 11. Views, Razor, HTML, CSS e JavaScript

### Estrutura de Views (24 pastas)

Home, Pacientes, Medicos, Requisitar, ResultadoExames, ConsultarExames,
PlanoExames, PlanoExamesItens, CargaDados, Senhas, Configuracoes,
Instituicoes, Postos, ClasseExames, CatalogoRecebimentos, Graficos,
Release, Manutencao, ManutencaoFaturamento, RelatorioFaturamento,
FormasRecebimento, ContasRecebimento, ExameReferencia, Mensagem, Shared

### Layout

- `_Layout.cshtml` — layout master com menu dinâmico por perfil
- Partials: `_PartialRequisitar`, `_PartialLancarExames`, `_PartialCupom`,
  `_PartialFiltro`, `_PartialPlanoContaItem`

### JavaScript Principal

| Arquivo                | Finalidade                                   |
|------------------------|----------------------------------------------|
| `site.js`              | Funções globais (clickAviso, clickConfirm)   |
| `mydatatables.js`      | Configuração padrão DataTables               |
| `requisitar-exames.js` | Lógica de requisição (formulário + cupom)    |
| `grid-navigate.js`     | Navegação ENTER entre campos do grid         |
| `cargadados.js`        | UI de importação (SignalR progress)          |

---

## 12. Processos Operacionais

### Processo: Requisição de Exames (ALTÍSSIMA complexidade)

```
Entrada: Formulário com Paciente + Médico + Instituição + Tabela + Exames
  ↓
1. Validação de dados (ValidarDadosDominio)
  ↓
2. Salvar/Atualizar Paciente (FORA da transação)
  ↓
3. Salvar/Localizar Médico (FORA da transação)
  ↓
4. INÍCIO TRANSAÇÃO
  ↓
5a. INCLUSÃO: SalvarExameRealizadoAsync
    - Gera Sequencial (FOR UPDATE lock)
    - Cria header ExamesRealizados
    - Expande Principais → Sub-itens (PlanoExames)
    - Insere ItensExamesRealizados
  ↓
5b. EDIÇÃO: Atualiza header + Remove itens antigos + Insere novos
  ↓
6. COMMIT
  ↓
7. Limpa Cupom (ListaAcumulativa)
  ↓
Saída: JSON {sucesso, pacienteId, exameRealizadoId}
```

### Processo: Login e Autenticação (CRÍTICA complexidade)

```
Entrada: Email + Senha + Token reCAPTCHA
  ↓
1. Valida reCAPTCHA (IsCaptchaValid / CreateAssessment)
  ↓
2. Localiza empresa pelo email (LABWEB7Empresas → Emails → EmpresaCliente)
  ↓
3. Se primeiro acesso: cria registros em Emails + EmpresaLogin[CNPJ]
  ↓
4. Obtém StringConexao da empresa → SetConnectionString
  ↓
5. Cria novo DbContext com conexão do cliente
  ↓
6. Busca Senhas no banco do cliente (BCrypt verify + migração AES)
  ↓
7. Valida: Bloqueado? EmailConfirmado? DataExpira?
  ↓
8. Armazena em Session: Email, Nome, Token, CNPJ, StringConexao
  ↓
Saída: Cookie .LabWeb7.Auth + Session .LabWeb7.Session
```

### Processo: Importação Firebird → PostgreSQL (ALTÍSSIMA complexidade)

```
Entrada: Connection string Firebird + Configuração de tabelas
  ↓
1. Fase Preparação (NETProvider): Schema comparison + Contagem
  ↓
2. Decisão do usuário via SignalR (RequererDecisao)
  ↓
3. Fase Importação (ODBC Charset=NONE):
   - Para cada tabela: abre conexão ODBC → SELECT → batch INSERT PostgreSQL
   - Reconexão por tabela (evita crash em tabelas grandes)
   - SanitizarStringWin1252 para preservar acentos
  ↓
4. Fase Pós-Importação:
   - Criar Folhas ausentes (PlanoExames)
   - Deduplicação (Pacientes + Médicos)
  ↓
5. Progresso via SignalR Hub → Frontend
  ↓
Saída: Dados migrados em PostgreSQL + Relatório
```

---

## 13. Segurança

### 13.1 Autenticação

| Mecanismo             | Implementação                                  | Status       |
|-----------------------|------------------------------------------------|--------------|
| Cookie Auth           | `.LabWeb7.Auth`, 60 min sliding                | IMPLEMENTADO |
| Session               | `.LabWeb7.Session`, 30 min idle                | IMPLEMENTADO |
| BCrypt                | Work factor 11, migração automática AES→BCrypt | IMPLEMENTADO |
| reCAPTCHA Enterprise  | Google Cloud, score ≥ 0.7 = seguro             | IMPLEMENTADO |
| Timeout de login      | 30s para consulta ao banco                     | IMPLEMENTADO |

### 13.2 Autorização

| Mecanismo            | Implementação                                      | Status       |
|----------------------|----------------------------------------------------|--------------|
| SessionFilter        | TypeFilter que valida sessão ativa                 | IMPLEMENTADO |
| Perfis               | ControleDePerfil + ControleDePerfilMenu            | IMPLEMENTADO |
| Menu dinâmico        | Montado por perfil do usuário                      | IMPLEMENTADO |
| ValidadorDeSessao    | Verifica SessionEmail + SessionNome + SessionToken | IMPLEMENTADO |

### 13.3 Proteção contra Ataques

| Ameaça              | Proteção                                       | Nível         |
|---------------------|------------------------------------------------|---------------|
| SQL Injection       | EF Core parameterizado + NpgsqlParameter       | ALTO          |
| XSS                 | Razor encoding automático                      | MÉDIO         |
| CSRF                | Não identificado AntiForgeryToken explícito    | **AUSENTE**   |
| Brute Force         | reCAPTCHA + log de tentativas                  | MÉDIO         |
| Session Fixation    | Cookie HttpOnly + SameSite=Lax                 | MÉDIO         |

### 13.4 Dados Sensíveis — RISCOS IDENTIFICADOS

| Risco                                      | Severidade | Localização              |
|--------------------------------------------|------------|--------------------------|
| Secrets em appsettings.json (texto plano)  | **ALTA**   | appsettings.json         |
| Senha de email hardcoded                   | **ALTA**   | appsettings.json         |
| Credencial DB com placeholders previsíveis | MÉDIA      | appsettings.json         |
| EnableSensitiveDataLogging em produção     | MÉDIA      | db.cs OnConfiguring      |
| Chave AES estática (vetor de cifras)       | MÉDIA      | CriptoDecripto.cs        |

---

## 14. Logs, Auditoria e Observabilidade

| Mecanismo              | Implementação                               | Destino              |
|------------------------|---------------------------------------------|----------------------|
| EventLogHelper         | Windows Event Log (Application)             | Windows Event Viewer |
| LoggerFile             | Arquivo de log no disco                     | C:\log\ ou /var/log/ |
| EF Core SQL Logging    | LogTo() com filtro em db.cs                 | Event Viewer         |
| reCAPTCHA Monitoring   | Tabela ReCaptchaMonitoramento               | PostgreSQL           |
| ControleConcorrencia   | Tabela com processo + timestamp             | PostgreSQL           |
| Rastreamentos          | Tabela de rastreamento de operações         | PostgreSQL           |

---

## 15. Integrações Externas

| Integração              | Protocolo    | Biblioteca                         | Status     |
|-------------------------|--------------|------------------------------------|------------|
| Google reCAPTCHA Enterp.| HTTPS/gRPC   | Google.Cloud.RecaptchaEnterprise   | ATIVO      |
| Google Cloud Monitoring | HTTPS/gRPC   | Google.Cloud.Monitoring.V3         | ATIVO      |
| AWS S3                  | HTTPS        | AWSSDK.S3                          | PREPARADO  |
| Azure Blob Storage      | HTTPS        | Azure.Storage.Blobs                | PREPARADO  |
| Firebird (importação)   | ODBC         | System.Data.Odbc                   | ATIVO      |
| SMTP (Email)            | SMTP/TLS     | System.Net.Mail                    | PARCIAL    |

---

## 16. Processamento Assíncrono e Concorrência

### Mecanismos de Concorrência

| Mecanismo                       | Uso                                      | Risco             |
|---------------------------------|------------------------------------------|-------------------|
| FOR UPDATE (PostgreSQL)         | GeraSequencialAsync — lock por linha     | Baixo             |
| LOCK TABLE IN EXCLUSIVE MODE    | SaveChangesWithSyncAsync — gap-fill      | **ALTO (timeout)**|
| ControleConcorrencia (tabela)   | ExclusaoService — lock lógico            | Médio             |
| CancellationToken (10s)         | SaveChangesWithSyncAsync                 | Adequado          |
| Timeout de login (30s)          | ValidacoesDeSenhas                       | Adequado          |

### Serviços em Background

- `FileWriteService` (WindowsService): loop infinito com Thread.Sleep
- `SvcExportacao` (BackgroundService): executa uma vez e para

---

## 17. Tratamento de Erros

| Padrão                          | Uso                                    | Adequação     |
|---------------------------------|----------------------------------------|---------------|
| try/catch com rollback          | Transações em controllers              | ADEQUADO      |
| EventLog para exceções          | Todos os services                      | ADEQUADO      |
| JSON com {sucesso, mensagem}    | APIs internas                          | ADEQUADO      |
| DeveloperExceptionPage (dev)    | Startup Configure                      | ADEQUADO      |
| ExceptionHandler (prod)         | /Home/Error                            | ADEQUADO      |
| SaveChanges engolindo exceções  | db.cs SaveChanges (retorna 0)          | **INADEQUADO**|

**Risco:** O `SaveChanges(bool)` síncrono captura exceções e retorna 0 sem propagar.
Isso pode mascarar erros de persistência em código legado.

---

## 18. Configuração e Infraestrutura

### Injeção de Dependência

- **Scoped:** Db, IRepositorio<T>, GeralController, ValidacoesDeSenhas, ExclusaoService,
  IConnectionService, IDbFactory, CargaDados*, ReCaptcha*, Imagem, PathHelper
- **Singleton:** IEventLogHelper, IHttpContextAccessor
- **Condicional:** IImpressoraCupom (Windows vs Linux)

### Ambiente

- Cultura fixa: `pt-BR`
- HTTPS Redirection habilitado
- Session: DistributedMemoryCache (in-process)
- Cookies: HttpOnly, SameSite=Lax, SecurePolicy=SameAsRequest

---

## 19. Fluxos Críticos

| # | Fluxo                       | Impacto de Falha                         |
|---|-----------------------------|------------------------------------------|
| 1 | Login multi-tenant          | Indisponibilidade total                  |
| 2 | Requisição de exames        | Perda de dados de exames                 |
| 3 | Lançamento de resultados    | Resultados incorretos (impacto clínico)  |
| 4 | Importação Firebird         | Perda de dados históricos                |
| 5 | SaveChangesWithSyncAsync    | Deadlock ou corrupção de IDs             |
| 6 | Geração de sequencial       | Duplicação de protocolos                 |
| 7 | Exclusão de requisição      | Perda de dados                           |
| 8 | Liberação de exames         | Laudos incorretos liberados              |
| 9 | reCAPTCHA no login          | Bypass de segurança                      |
| 10| Troca de tenant (session)   | Dados cruzados entre empresas            |

---

## 20. Componentes Críticos

| Componente                            | Classificação | Razão                                    |
|---------------------------------------|---------------|------------------------------------------|
| ValidacoesDeSenhas                    | CRÍTICA       | Gate único de acesso ao sistema          |
| BaseController.OnActionExecuting      | CRÍTICA       | Isolamento de dados entre tenants        |
| SaveChangesWithSyncAsync              | CRÍTICA       | Integridade de IDs + locks               |
| RequisitarController.SalvarRequisicao | ALTÍSSIMA     | Regra de negócio central                 |
| db.cs (DbContext)                     | CRÍTICA       | Fundação de todo acesso a dados          |
| ConnectionService                     | CRÍTICA       | Roteamento multi-tenant                  |
| FirebirdImporter                      | ALTÍSSIMA     | Migração de dados com encoding           |
| GeralController (timezone)            | ALTA          | Consistência de datas em todo o sistema  |
| CriptoDecripto (BCrypt)               | ALTA          | Segurança de credenciais                 |
| ExclusaoService                       | ALTA          | Integridade referencial                  |

---

## 21. Dívida Técnica e Riscos Arquiteturais

### Riscos Identificados (por severidade)

| Risco                                           | Severidade | Evidência                           |
|-------------------------------------------------|------------|-------------------------------------|
| Secrets em appsettings.json                     | ALTA       | Senhas, chaves API em texto plano   |
| SaveChanges síncrono engole exceções            | ALTA       | db.cs retorna 0 sem propagar erro   |
| EnableSensitiveDataLogging em produção          | ALTA       | db.cs OnConfiguring                 |
| Ausência de CSRF protection                     | ALTA       | Nenhum AntiForgeryToken identificado|
| GeralController herda de Controller (MVC)       | MÉDIA      | Deveria ser serviço puro            |
| DeleteOrphans usa reflection pesada             | MÉDIA      | Performance em tabelas grandes      |
| Reflection no SaveChangesWithSyncAsync          | MÉDIA      | Manutenibilidade e debugging        |
| Pacotes duplicados entre projetos               | BAIXA      | Mesmo pacote em MVC, BLL, Ext, etc  |
| WindowsService usa Thread direto (não Task)     | BAIXA      | Padrão obsoleto mas funcional       |
| ViewBag ainda usado em alguns controllers       | BAIXA      | Migração parcial para ViewModel     |

### Dívida Técnica

- **GeralController** é um "serviço" disfarçado de Controller
- **Código legado de criptografia** (Criptografia dos anos 90) mantido para compatibilidade
- **Duplicação de pacotes NuGet** entre projetos (mesma lista em 4 .csproj)
- **Email** tem implementação incompleta (`EnviarEmail` privado não integrado)
- **AWS/Azure Storage** referenciados mas com implementação de exemplo (não produção)

---

## 22. Performance

### Potenciais Gargalos

| Gargalo                                   | Localização                    | Impacto  |
|-------------------------------------------|--------------------------------|----------|
| LOCK TABLE EXCLUSIVE no gap-fill          | db.cs SaveChangesWithSyncAsync | ALTO     |
| DeleteOrphans carrega TODOS os registros  | db.cs                          | ALTO     |
| ToList() antes de filtrar em DeleteOrphans| db.cs                          | ALTO     |
| FOR UPDATE em GeraSequencialAsync         | RequisitarController           | MÉDIO    |
| Include sem paginação em consultas        | Diversos controllers           | MÉDIO    |
| EF Core SQL logging em produção           | db.cs LogTo                    | MÉDIO    |
| SignalR single-connection (não scale-out) | ImportProgressHub              | BAIXO    |

---

## 23. Mapa de Dependências

```
[Browser] → jQuery/DataTables/SweetAlert2
    ↓ HTTP/AJAX
[Kestrel/IIS] → Middleware Pipeline
    ↓
[Controller] → BaseController → GeralController
    ↓                              ↓
[ViewModel]                  [ITempoServidorService]
    ↓                              ↓
[IRepositorio<T>]            [PostgreSQL: SELECT NOW()]
    ↓
[Db (DbContext)] → [Npgsql] → [PostgreSQL]
    ↓
[IConnectionService] ← [Session: tenant routing]
    ↓
[LABWEB7Empresas] → [EmpresaCliente.StringConexao] → [LABWEB7{CNPJ}]

[WindowsService] → [IntegracaoService] → [Db] → [PostgreSQL]
[CargaDados] → [ODBC] → [Firebird 2.5.x]
              → [SignalR] → [Browser progress bar]
```

---

## 24. Matriz Geral da Arquitetura

| Área            | Componente              | Tecnologia        | Criticidade | Complexidade |
|-----------------|-------------------------|-------------------|-------------|--------------|
| Apresentação    | Views Razor             | ASP.NET Core MVC  | ALTA        | MÉDIA        |
| Apresentação    | JavaScript/jQuery       | jQuery 3.x        | ALTA        | MÉDIA        |
| Apresentação    | DataTables              | DataTables 2.x    | ALTA        | MÉDIA        |
| Backend         | Controllers (30+)       | C# 12             | CRÍTICA     | ALTA         |
| Backend         | GeralController         | C# 12             | ALTA        | MÉDIA        |
| Persistência    | DbContext (db.cs)       | EF Core 8.0.19    | CRÍTICA     | ALTÍSSIMA    |
| Persistência    | Repository Pattern      | EF Core           | ALTA        | BAIXA        |
| Banco           | PostgreSQL              | Npgsql 8.0.4      | CRÍTICA     | MÉDIA        |
| Banco           | Multi-tenant routing    | Custom            | CRÍTICA     | ALTA         |
| Segurança       | Autenticação            | Cookie + BCrypt   | CRÍTICA     | ALTA         |
| Segurança       | reCAPTCHA               | Google Enterprise | ALTA        | MÉDIA        |
| Integração      | Firebird import         | ODBC              | ALTA        | ALTÍSSIMA    |
| Integração      | Cloud Storage           | AWS/Azure/GCP     | BAIXA       | BAIXA        |
| Background      | Windows Service         | .NET Hosting      | MÉDIA       | MÉDIA        |
| Logging         | Event Viewer            | Windows EventLog  | ALTA        | BAIXA        |
| Real-time       | SignalR Hub             | ASP.NET SignalR   | MÉDIA       | BAIXA        |

---

## 25. Conclusão Arquitetural

### Qual arquitetura o projeto utiliza?

**MVC monolítico** com Repository Pattern genérico, multi-tenancy por banco separado,
e service layer parcial. A separação é funcional mas não estrita — GeralController
acumula responsabilidades de serviço dentro de uma classe Controller.

### Pontos Fortes

1. **Multi-tenancy robusto** — isolamento completo por banco de dados
2. **Timezone handling** — implementação sólida UTC→local com métodos dedicados
3. **BCrypt com migração** — segurança de senhas com compatibilidade legada
4. **Concorrência controlada** — locks pessimistas e serviço de exclusão
5. **Importação Firebird** — solução definitiva validada para preservar encoding
6. **Repository genérico** — consistência no acesso a dados
7. **SignalR para progresso** — experiência de usuário na importação
8. **Código documentado** — comentários detalhados e marcações de autoria

### Pontos Frágeis

1. **Secrets expostos** — appsettings.json contém dados sensíveis em texto plano
2. **SaveChanges engole exceções** — pode mascarar erros de persistência
3. **DeleteOrphans carrega tudo em memória** — performance crítica em produção
4. **Ausência de CSRF** — vulnerabilidade em formulários POST
5. **LOCK TABLE EXCLUSIVE** — risco de contenção sob carga
6. **GeralController como serviço** — design não convencional

### Nível Geral de Maturidade

**INTERMEDIÁRIO-AVANÇADO** — O sistema demonstra decisões arquiteturais bem
fundamentadas (multi-tenancy, timezone, concorrência), com implementações que
funcionam corretamente em cenário de baixa/média carga. A principal lacuna está
em segurança (secrets, CSRF) e em componentes que usam reflection pesada que
podem não escalar sob carga alta.

### Mapa de Conhecimento (para novo desenvolvedor)

**Ordem de estudo recomendada:**

1. `Startup.cs` — entender toda a DI e pipeline
2. `BaseController.cs` — compreender o multi-tenant
3. `db.cs` — DbSets, SaveChanges, OnModelCreating
4. `GeralController.cs` — métodos de data/hora e validação
5. `ValidacoesDeSenhas.cs` — fluxo de login e routing
6. `RequisitarController.cs` — regra de negócio central
7. `IConnectionService` + `IDbFactory` — troca dinâmica de banco
8. `CriptoDecripto.cs` — segurança de credenciais
9. `ExclusaoService.cs` — padrão de exclusão com concorrência
10. `appsettings.json` — configurações e secrets

---

## Apêndice: Resumo Quantitativo

| Métrica                    | Valor   |
|----------------------------|---------|
| Projetos na solução        | 5       |
| Entidades (DbSets)         | 45+     |
| Controllers                | 30+     |
| ViewModels                 | 30+     |
| Views (pastas)             | 24      |
| Pacotes NuGet (MVC)        | 22      |
| Pacotes NuGet (total)      | ~50     |
| Complexidade CRÍTICA       | 5       |
| Complexidade ALTÍSSIMA     | 4       |
| Complexidade ALTA          | 10+     |
| Riscos de segurança        | 5       |
| Gargalos de performance    | 7       |
