# Análise Arquitetural — Sistema de Gestão Laboratorial (LIMS)

**Projeto de Estudo:** Aplicação web completa para laboratórios de análises clínicas  
**Stack:** ASP.NET Core 8 · C# 12 · PostgreSQL · Entity Framework Core · SignalR  
**Autor:** Ricardo Guilemond

---

## Resumo do Projeto

Sistema de gestão laboratorial (LIMS) construído em ASP.NET Core 8 MVC,
destinado a laboratórios de análises clínicas. Opera em modelo **multi-tenant**
(um banco PostgreSQL por empresa-cliente), com autenticação robusta, integração
com Google Cloud, serviços Windows para processos agendados, e migração
completa de um sistema legado Delphi/Firebird.

A solução compreende 5 projetos, 45+ entidades no ORM, 30+ controllers, e
uma camada de frontend com jQuery + DataTables 2.x + Bootstrap 5.

**Domínio:** requisição de exames, lançamento de resultados, cadastros
(pacientes, médicos, instituições), planos de exames hierárquicos, geração
de laudos, integração/exportação de dados, e controle financeiro.

---

## 1. Arquitetura da Solução

### Projetos

| Projeto            | Tipo                 | Responsabilidade                         |
|--------------------|----------------------|------------------------------------------|
| Web MVC            | ASP.NET Core MVC     | Aplicação principal (UI + API + negócio) |
| BLL                | Class Library        | Utilidades, PDF, tempo do servidor       |
| ExtensionsMethods  | Class Library        | Helpers: logging, sessão, email, crypto  |
| ServicoExportacao  | Worker Service       | BackgroundService para integração        |
| WindowsService     | Windows Service      | Serviço Windows para integração cíclica  |

### Padrão Arquitetural

**MVC monolítico** com Repository Pattern genérico + Service Layer:

- Controllers — orquestração de fluxo e roteamento
- Services — lógica de negócio e concorrência
- Repository genérico (`IRepositorio<T>`) com DbContext
- Entidades EF Core com relacionamentos 1:N e 1:1 (Fluent API)
- ViewModels como DTOs para as Views
- BLL isolada (tempo, PDF, helpers de infraestrutura)

### Dependências entre Projetos

```
Web MVC ──────────► BLL
    │                 ▲
    └──────────────► ExtensionsMethods ──► BLL

WindowsService ───► Web MVC + ExtensionsMethods
    └─────────────► ServicoExportacao ──► Web MVC
```

---

## 2. Tecnologias Utilizadas

| Aspecto                   | Tecnologia                                          |
|---------------------------|-----------------------------------------------------|
| Framework                 | ASP.NET Core 8.0 (C# 12)                            |
| ORM                       | Entity Framework Core 8.0                           |
| Banco de dados            | PostgreSQL (Npgsql)                                 |
| Banco legado (migração)   | Firebird 2.5.x (via ODBC)                           |
| Frontend                  | jQuery + Bootstrap 5 + DataTables 2.x + SweetAlert2 |
| Autenticação              | Cookie Authentication + Session + BCrypt            |
| Proteção anti-bot         | Google reCAPTCHA Enterprise                         |
| PDF                       | PdfSharpCore                                        |
| Imagens                   | SixLabors.ImageSharp                                |
| Cloud Storage             | AWS S3, Azure Blob Storage                          |
| Comunicação real-time     | ASP.NET SignalR                                     |
| Logging                   | Windows Event Log + arquivo (cross-platform)        |
| Documentos                | DocumentFormat.OpenXml                              |

---

## 3. Multi-Tenancy

O sistema implementa **isolamento completo por banco de dados**:

1. Banco administrativo central com registro de empresas-clientes
2. Cada empresa possui seu próprio banco PostgreSQL dedicado
3. No login, o sistema identifica a empresa e carrega a conexão correta
4. A conexão do tenant é restaurada automaticamente em cada requisição
5. Troca dinâmica de DbContext garante isolamento total entre clientes

Este padrão garante que dados de um cliente nunca sejam acessíveis
por outro, mesmo em cenários de falha.

---

## 4. Fluxo de uma Requisição HTTP

```
Browser → Kestrel/IIS → Middleware Pipeline:
  → HTTPS Redirect
  → Static Files
  → Routing
  → Localization (pt-BR)
  → Session
  → Authentication
  → Authorization
  → Controller (restaura tenant)
    → Validação de sessão
    → Service / Repository / DbContext
    → PostgreSQL (banco do tenant)
  → View (Razor) → Browser
```

---

## 5. Recursos do C# 12 Utilizados

| Recurso                  | Aplicação no Projeto                 |
|--------------------------|--------------------------------------|
| `async/await`            | Controllers, Services, DB operations |
| LINQ                     | Queries EF Core, filtros dinâmicos   |
| Nullable reference types | Habilitado em toda a solução         |
| Records                  | DTOs internos de API                 |
| Pattern matching         | Switch expressions em services       |
| Generics                 | Repository Pattern genérico          |
| Expression trees         | Filtros dinâmicos em services        |
| Reflection               | Mecanismo de sincronização de IDs    |
| Collection expressions   | Inicialização de coleções            |
| Static local functions   | Submétodos no fluxo de login         |
| CancellationToken        | Timeouts em operações assíncronas    |
| Tuples                   | Ranges UTC para queries              |
| Extension methods        | Filtros e formatações reutilizáveis  |

---

## 6. Banco de Dados

### Bancos Suportados

| Banco         | Uso                                       | Status     |
|---------------|-------------------------------------------|------------|
| PostgreSQL    | Banco principal (produção e dev)          | ATIVO      |
| Firebird 2.5  | Fonte de migração (sistema legado Delphi) | MIGRAÇÃO   |

### Estratégia de Acesso a Dados

- **EF Core** para operações CRUD (via Repository genérico)
- **SQL direto** (FromSqlRaw) para locks pessimistas e operações críticas
- **Npgsql direto** para operações multi-banco e importação
- **Transações EF Core nativas** para operações compostas
- **Sem Migrations** — schema gerenciado via DDL scripts
- **Fluent API** para mapeamento de entidades e relacionamentos

### Entidades Principais (por domínio)

**Cadastros:** Pacientes, Médicos, Instituições, Postos, Empresa

**Exames:** ExamesRealizados (header), ItensExamesRealizados (detalhe),
PlanoExames (hierarquia de preços), ClasseExames, TabelaExames

**Controle de Acesso:** Senhas, Usuários, Perfis, Menus por perfil

**Financeiro:** Catálogo de Recebimentos, Formas de Pagamento

**Integração:** Configurações, Layouts, Execuções, Periodicidades

---

## 7. Relacionamentos entre Entidades

```
Pacientes ──1:N──► ExamesRealizados
Médicos ──1:N──► ExamesRealizados
Instituição ──1:N──► ExamesRealizados
Postos ──1:N──► ExamesRealizados (nullable)
TabelaExames ──1:N──► ExamesRealizados

ExamesRealizados ──1:N──► ItensExamesRealizados
ExamesRealizados ──1:N──► ExamesExportados

Senhas ──1:1──► UsuáriosWeb
Senhas ──1:N──► ControleDeAcesso ──1:N──► ControleDePerfil

Instituição ──1:N──► Postos
```

Todas as FKs usam `DeleteBehavior.ClientSetNull` — exclusão em cascata
é controlada pelo código com validação prévia de vínculos ativos.

---

## 8. Controllers e Services

### Controllers Principais

| Controller             | Responsabilidade               | Complexidade |
|------------------------|--------------------------------|--------------|
| Requisição de Exames   | Fluxo completo de requisição   | ALTÍSSIMA    |
| Resultado de Exames    | Lançamento de resultados       | ALTA         |
| Login/Home             | Autenticação e dashboard       | ALTA         |
| Carga de Dados         | Importação Firebird→PostgreSQL | ALTÍSSIMA    |
| Gestão de Senhas       | CRUD de usuários               | ALTA         |
| Pacientes              | CRUD pacientes                 | MÉDIA        |
| Plano de Exames        | Hierarquia ContaExame          | ALTA         |
| Consulta de Exames     | Consulta e liberação           | MÉDIA        |

### Services

| Serviço              | Responsabilidade                               |
|----------------------|------------------------------------------------|
| Validação de Login   | Autenticação + roteamento multi-DB             |
| Integração           | Exportação agendada                            |
| Exclusão             | Exclusão genérica com controle de concorrência |
| Concorrência         | Lock pessimista via tabela de controle         |
| reCAPTCHA            | Validação Google Enterprise                    |
| Importação Firebird  | ODBC + reconexão + schema + encoding           |
| Tempo do Servidor    | Data/hora UTC do PostgreSQL                    |

---

## 9. Processos Operacionais

### Requisição de Exames (fluxo completo)

```
Entrada: Paciente + Médico + Instituição + Tabela + Exames
  ↓
1. Validação de dados
  ↓
2. Salvar/Atualizar Paciente (fora da transação)
  ↓
3. Salvar/Localizar Médico (fora da transação)
  ↓
4. INÍCIO TRANSAÇÃO
  ↓
5. Gera Sequencial (lock pessimista por instituição)
   Cria header de exame
   Expande Principais → Sub-itens automaticamente
   Insere itens detalhados
  ↓
6. COMMIT
  ↓
Saída: Requisição salva + Cupom impresso
```

### Importação Firebird → PostgreSQL

```
1. Schema comparison (NETProvider managed)
  ↓
2. Decisão do usuário via SignalR (real-time)
  ↓
3. Importação via ODBC (Charset=NONE):
   - Reconexão por tabela (estabilidade)
   - Preservação de encoding WIN1252→UTF-8
   - Batch INSERT com savepoints
  ↓
4. Pós-importação:
   - Criação de registros hierárquicos ausentes
   - Deduplicação (pacientes + médicos)
  ↓
5. Progresso em tempo real (SignalR Hub)
```

---

## 10. Segurança (implementações)

| Mecanismo                | Implementação                              |
|--------------------------|--------------------------------------------|
| Hash de senhas           | BCrypt (work factor 11)                    |
| Migração de senhas       | AES legado → BCrypt automático no login    |
| Proteção anti-bot        | Google reCAPTCHA Enterprise (score-based)  |
| Autenticação             | Cookie Authentication + sliding expiration |
| Validação de sessão      | Filter customizado em toda requisição      |
| Controle de acesso       | Perfis + menu dinâmico por permissão       |
| Parametrização de SQL    | EF Core + NpgsqlParameter (anti-injection) |
| Cookies                  | HttpOnly + SameSite                        |
| Criptografia de dados    | AES-128 para dados sensíveis no banco      |

---

## 11. Tratamento de Data/Hora (UTC)

Implementação rigorosa de timezone:

- **Fonte canônica:** PostgreSQL via `SELECT NOW()` (retorna UTC)
- **Fallback:** `DateTime.UtcNow` do servidor de aplicação
- **Armazenamento:** UTC no banco (colunas `timestamptz`)
- **Exibição:** conversão UTC → America/Sao_Paulo apenas na UI
- **Métodos dedicados:** `ObterDataHoraUtc()`, `ObterRangeDiaUtc()`,
  `ConverterLocalParaUtc()`, `ConverterDataLocalParaRangeUtc()`
- **Compatibilidade:** Npgsql 8.x strict mode (Kind=Utc obrigatório)

---

## 12. Concorrência e Processamento Assíncrono

| Mecanismo                    | Uso                                    |
|------------------------------|----------------------------------------|
| FOR UPDATE (PostgreSQL)      | Geração de sequencial por instituição  |
| LOCK TABLE EXCLUSIVE         | Sincronização de IDs (gap-fill)        |
| Tabela de concorrência       | Lock lógico para exclusões             |
| CancellationToken            | Timeout em operações críticas          |
| async/await                  | Toda a camada de persistência          |
| BackgroundService            | Integrações agendadas                  |
| Windows Service              | Processamento cíclico em background    |
| SignalR                      | Progresso em tempo real (importação)   |

---

## 13. Frontend

### Stack

| Biblioteca     | Finalidade                             |
|----------------|----------------------------------------|
| jQuery 3.x     | Manipulação DOM, AJAX, delegação       |
| DataTables 2.x | Grids com paginação, filtro, ordenação |
| Bootstrap 5    | Layout responsivo, componentes UI      |
| SweetAlert2    | Modais de confirmação/alerta           |
| Chart.js       | Gráficos e indicadores                 |
| Font Awesome 6 | Ícones                                 |
| InputMask      | Máscaras (CPF, telefone, datas)        |
| SignalR Client | Progresso real-time na importação      |

### Princípios de Frontend

- CSS padrão sobre soluções JavaScript para layout
- JavaScript puro preferido sobre plugins adicionais
- Colunas fixas em grids via `position: sticky` (sem plugins)
- Handlers com namespace para evitar acúmulo em partials AJAX
- DataTables com `scrollX`, idioma pt-BR, layout customizado

---

## 14. Integrações Externas

| Integração               | Protocolo   | Status    |
|--------------------------|-------------|-----------|
| Google reCAPTCHA Enterp. | HTTPS/gRPC  | ATIVO     |
| Google Cloud Monitoring  | HTTPS/gRPC  | ATIVO     |
| AWS S3                   | HTTPS       | PREPARADO |
| Azure Blob Storage       | HTTPS       | PREPARADO |
| Firebird (migração)      | ODBC        | ATIVO     |
| SMTP (Email)             | SMTP/TLS    | PARCIAL   |

---

## 15. Classificação de Complexidade

| Componente                  | Complexidade | Justificativa                       |
|-----------------------------|--------------|-------------------------------------|
| Sincronização de IDs        | CRÍTICA      | Reflection + lock + concorrência    |
| Login multi-tenant          | CRÍTICA      | Roteamento dinâmico entre bancos    |
| Requisição de exames        | ALTÍSSIMA    | Orquestra 5+ entidades + transação  |
| Importação Firebird         | ALTÍSSIMA    | ODBC + encoding + reconexão + batch |
| Remoção de órfãos           | ALTA         | Reflection sobre todos os DbSets    |
| Troca de tenant             | ALTA         | DbContext dinâmico por requisição   |
| Exclusão com concorrência   | MÉDIA        | Lock genérico com validação de FK   |
| Criptografia                | MÉDIA        | BCrypt + AES + migração automática  |
| Tempo do servidor           | BAIXA        | SELECT NOW() + fallback             |
| Repository genérico         | BAIXA        | CRUD padrão                         |

---

## 16. Mapa de Dependências

```
[Browser] → jQuery / DataTables / SweetAlert2
    ↓ HTTP/AJAX
[Kestrel/IIS] → Middleware Pipeline
    ↓
[Controller] → Service Layer → Repository
    ↓                              ↓
[ViewModel]              [Tempo UTC (PostgreSQL)]
    ↓
[DbContext] → [Npgsql] → [PostgreSQL]
    ↓
[ConnectionService] ← [Session: tenant routing]
    ↓
[Banco Empresas] → [Banco do Cliente]

[Windows Service] → [Integração] → [DbContext] → [PostgreSQL]
[Carga de Dados] → [ODBC] → [Firebird 2.5.x]
                 → [SignalR] → [Browser]
```

---

## 17. Pontos Fortes da Arquitetura

1. **Multi-tenancy por banco separado** — isolamento real entre clientes
2. **Tratamento de timezone rigoroso** — UTC + conversão controlada
3. **BCrypt com migração automática** — segurança sem quebrar legado
4. **Concorrência controlada** — locks pessimistas onde necessário
5. **Importação Firebird validada** — encoding preservado (WIN1252→UTF-8)
6. **Repository genérico** — consistência no acesso a dados
7. **SignalR para progresso** — UX durante processos longos
8. **Código documentado** — comentários detalhados e rastreáveis
9. **Cross-platform** — Windows + Linux (logging adaptável)
10. **Expansão automática de exames** — hierarquia Principal→Sub-itens

---

## 18. Métricas do Projeto

| Métrica                   | Valor  |
|---------------------------|--------|
| Projetos na solução       | 5      |
| Entidades (DbSets)        | 45+    |
| Controllers               | 30+    |
| ViewModels                | 30+    |
| Views (pastas)            | 24     |
| Pacotes NuGet (total)     | ~50    |
| Componentes CRÍTICOS      | 5      |
| Componentes ALTÍSSIMOS    | 4      |
| Integrações externas      | 6      |

---

## 19. Competências Técnicas Demonstradas

- Arquitetura multi-tenant com isolamento por banco de dados
- Entity Framework Core 8 (Fluent API, transações, raw SQL)
- PostgreSQL avançado (timestamptz, FOR UPDATE, sequences)
- ASP.NET Core 8 (DI, Middleware Pipeline, Cookie Auth)
- C# 12 (async/await, records, generics, reflection, expressions)
- Migração de sistema legado (Delphi/Firebird → .NET/PostgreSQL)
- Segurança (BCrypt, reCAPTCHA Enterprise, session management)
- Real-time (SignalR para progresso de operações longas)
- Cloud (AWS S3, Azure Blob, Google Cloud Monitoring)
- Windows Services e BackgroundService (.NET Worker)
- Frontend (jQuery, DataTables 2.x, Bootstrap 5, Chart.js)
- Controle de concorrência (locks pessimistas, gap-fill de IDs)
- Encoding e internacionalização (WIN1252→UTF-8, pt-BR, timezone)

---

*Projeto em desenvolvimento ativo. Arquitetura analisada em agosto/2026.*
