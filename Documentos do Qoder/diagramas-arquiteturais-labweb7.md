# Diagramas Arquiteturais - LabWeb7

**Data:** 21/04/2026

---

## 1. DIAGRAMA DE DEPENDÊNCIAS DA SOLUTION

```mermaid
graph TB
    subgraph Solution["LabWebMvc.sln"]
        MVC[LabWebMvc.MVC<br/>Web Application]
        BLL[BLL<br/>Business Logic]
        EXT[ExtensionsMethods<br/>Utilities]
        MODELS[ModeloDeDados<br/>Reference Models]
        WORKER[ServicoExportacao<br/>Worker Service]
        WINSVC[WindowsService<br/>Windows Service]
    end
    
    MVC --> BLL
    MVC --> EXT
    BLL --> EXT
    MODELS --> EXT
    WORKER --> MVC
    WINSVC --> EXT
    WINSVC --> MVC
    WINSVC --> WORKER
    
    style MVC fill:#4CAF50,color:#fff
    style BLL fill:#2196F3,color:#fff
    style EXT fill:#FF9800,color:#fff
    style WORKER fill:#9C27B0,color:#fff
```

---

## 2. ARQUITETURA EM CAMADAS

```mermaid
graph TB
    subgraph Presentation["Presentation Layer"]
        VIEWS[Views .cshtml<br/>Razor + Bootstrap]
        CTRL[Controllers<br/>Areas/Controllers]
        VM[ViewModels<br/>Validation]
    end
    
    subgraph Application["Application Layer"]
        GC[GeralController<br/>General Methods]
        ES[ExclusaoService<br/>Deletion Service]
        IMG[Imagem Service<br/>Image Processing]
        CONC[ConcorrenciaService<br/>Concurrency Control]
    end
    
    subgraph Business["Business Layer"]
        BLL_CLASSES[BLL Classes<br/>PartBLL, UtilBLL]
        CONVERTERS[PDF Converters<br/>ConversoresPdf]
        TIME[Time Services<br/>TempoLocal/TempoServidorMSSQL]
    end
    
    subgraph Data["Data Access Layer"]
        DB[DbContext db.cs<br/>EF Core]
        REPO[Repositorio&lt;T&gt;<br/>Generic Repository]
        FACTORY[DbFactory<br/>Connection Factory]
    end
    
    subgraph Database["Database"]
        PSQL[(PostgreSQL<br/>51 Tables)]
    end
    
    subgraph CrossCutting["Cross-Cutting Concerns"]
        EXTENSIONS[ExtensionsMethods<br/>Genericos, Validations]
        LOGGING[EventLogHelper<br/>Logging]
        STORAGE[Storage Services<br/>AWS S3, Azure]
        VALIDATOR[ValidadorDeSessao<br/>Session Validation]
    end
    
    VIEWS --> CTRL
    CTRL --> VM
    CTRL --> GC
    CTRL --> ES
    CTRL --> CONC
    CTRL --> DB
    
    GC --> BLL_CLASSES
    ES --> DB
    DB --> FACTORY
    DB --> REPO
    FACTORY --> PSQL
    REPO --> PSQL
    
    CTRL --> EXTENSIONS
    CTRL --> LOGGING
    CTRL --> VALIDATOR
    ES --> CONC
    
    BLL_CLASSES --> CONVERTERS
    BLL_CLASSES --> TIME
    
    style Presentation fill:#E3F2FD
    style Application fill:#FFF3E0
    style Business fill:#F3E5F5
    style Data fill:#E8F5E9
    style Database fill:#FFEBEE
    style CrossCutting fill:#F5F5F5
```

---

## 3. BANCO DE DADOS - RELACIONAMENTOS PRINCIPAIS

```mermaid
erDiagram
    Pacientes ||--o{ ExamesRealizados : has
    Pacientes ||--o{ Requisitar : has
    Pacientes ||--o{ ExamesPendentes : has
    Pacientes ||--o{ ExamesExportados : has
    Pacientes ||--o{ ItensExamesRealizados : has
    
    Medicos ||--o{ ExamesRealizados : performs
    Medicos ||--o{ Requisitar : performs
    Medicos ||--o{ ExamesPendentes : performs
    
    Instituicao ||--o{ Postos : contains
    Instituicao ||--o{ ExamesRealizados : has
    Instituicao ||--o{ Requisitar : has
    Instituicao ||--o{ TabelaExames : has
    
    Postos ||--o{ ExamesRealizados : at
    Postos ||--o{ Requisitar : at
    
    TabelaExames ||--o{ PlanoExames : defines
    TabelaExames ||--o{ ExamesRealizados : references
    TabelaExames ||--o{ Requisitar : references
    
    ClasseExames ||--o{ Requisitar : categorizes
    ClasseExames ||--o{ ExamesPendentes : categorizes
    
    ExamesRealizados ||--o{ ItensExamesRealizados : contains
    ExamesRealizados ||--o{ ExamesExportados : exported
    
    ControleDeAcesso ||--o{ ControleDePerfil : has
    
    Pacientes {
        int Id PK
        string NomePaciente
        string CPF
        DateTime Nascimento
        string Sexo
    }
    
    Medicos {
        int Id PK
        string NomeMedico
        string CRM
        string Especialidade
    }
    
    Instituicao {
        int Id PK
        string Sigla
        string Nome
        string CNPJ
    }
    
    ExamesRealizados {
        int Id PK
        int PacienteId FK
        int MedicoId FK
        int InstituicaoId FK
        int PostoId FK
        int TabelaExamesId FK
        DateTime DataIni
        int Liberacao
    }
    
    Requisitar {
        int Id PK
        int PacienteId FK
        int MedicoId FK
        int ClasseExamesId FK
        int InstituicaoId FK
        string ContaExame
    }
    
    PlanoExames {
        int Id PK
        int ExameId
        int TabelaExamesId
        string ContaExame
        string Descricao
        decimal ValorItem
    }
```

---

## 4. FLUXO: REQUISIÇÃO DE EXAMES

```mermaid
sequenceDiagram
    participant U as Usuário
    participant VC as RequisitarController
    participant MC as MedicosController
    participant PC as PacientesController
    participant DB as DbContext (db.cs)
    participant TX as Transaction
    
    U->>VC: Seleciona Instituição, Posto, Médico, Paciente
    VC->>MC: Carrega/Seleciona Médico
    MC->>DB: Salva Médico (se novo)
    DB-->>MC: Médico salvo (FORA da transação)
    
    VC->>PC: Carrega/Seleciona Paciente
    PC->>DB: Salva Paciente (se novo)
    DB-->>PC: Paciente salvo (FORA da transação)
    
    U->>VC: Seleciona Exames do Plano
    VC->>DB: Busca PlanoExames (filtro: TabelaExamesId)
    DB-->>VC: Retorna exames disponíveis
    
    VC->>TX: BeginTransactionAsync()
    VC->>DB: Adiciona itens em Requisitar
    DB->>DB: Valida dados
    
    alt Sucesso
        DB-->>VC: OK
        VC->>TX: CommitAsync()
        TX-->>VC: Transação completada
        VC-->>U: Exames requisitados com sucesso
    else Erro
        DB-->>VC: Erro de validação
        VC->>TX: RollbackAsync()
        TX-->>VC: Rollback completado
        VC-->>U: Erro: exames NÃO salvos<br/>Médico/Paciente permanecem salvos
    end
    
    Note over MC,PC: Médico e Paciente salvos<br/>NÃO são afetados pelo rollback
```

---

## 5. FLUXO: ALTERAÇÃO DE PLANO DE EXAMES (SUS MODEL)

```mermaid
sequenceDiagram
    participant U as Usuário
    participant PEC as PlanoExamesController
    participant DB as DbContext
    participant TE as TabelaExames (Todas)
    
    U->>PEC: Altera item do Plano SUS (ExameId=1)
    PEC->>DB: Atualiza item SUS
    
    PEC->>PEC: Verifica: É modelo SUS?
    
    alt ExameId == 1 (SUS)
        PEC->>DB: Busca todas as TabelaExamesId
        DB-->>PEC: Lista de instituições
        
        loop Para cada TabelaExamesId
            PEC->>DB: Replica alterações<br/>(mesmo ContaExame)
            DB->>DB: Update PlanoExames<br/>WHERE ContaExame = X<br/>AND TabelaExamesId = Y
        end
        
        PEC-->>U: Alteração replicada para<br/>TODAS as instituições
    else ExameId != 1
        PEC->>DB: Atualiza apenas esta instituição
        PEC-->>U: Alteração aplicada apenas<br/>nesta instituição
    end
```

---

## 6. FLUXO: EXCLUSÃO COM VALIDAÇÃO DE FK

```mermaid
flowchart TD
    START[Iniciar Exclusão] --> CHECK{Existem registros<br/>em tabelas filhas?}
    
    CHECK -->|Sim| BLOCK[❌ Bloquear Exclusão]
    CHECK -->|Não| DELETE[✅ Executar DELETE]
    
    BLOCK --> MSG[Retornar mensagem assertiva:<br/>"Registro possui vínculos e<br/>não pode ser excluído"]
    MSG --> END1[Fim - NÃO excluído]
    
    DELETE --> TX[Iniciar Transação]
    TX --> ORPHAN{Executar<br/>DeleteOrphans?}
    
    ORPHAN -->|Sim| DEL_ORPH[Remove registros órfãos]
    ORPHAN -->|Não| SAVE[SaveChanges]
    
    DEL_ORPH --> SAVE
    SAVE --> COMMIT{Sucesso?}
    
    COMMIT -->|Sim| COMMIT_TX[Commit Transaction]
    COMMIT -->|Não| ROLLBACK[Rollback Transaction]
    
    COMMIT_TX --> END2[Fim - Excluído com sucesso]
    ROLLBACK --> END3[Fim - Erro, nada excluído]
    
    style BLOCK fill:#F44336,color:#fff
    style DELETE fill:#4CAF50,color:#fff
    style MSG fill:#FF9800,color:#fff
```

---

## 7. MULTI-TENANT - TROCA DE CONEXÃO

```mermaid
sequenceDiagram
    participant REQ HTTP Request
    participant VS ValidadorDeSessao
    participant CS ConnectionService
    participant DBF DbFactory
    participant DB DbContext
    participant PSQL PostgreSQL
    
    REQ->>VS: Request com sessão
    VS->>VS: Valida sessão do usuário
    VS->>VS: Obtém EmpresaId da sessão
    
    VS->>CS: SetConnectionString(empresaId)
    CS->>CS: Busca string de conexão<br/>da empresa no appsettings
    CS-->>VS: ConnectionString retornada
    
    VS->>DBF: Create()
    DBF->>DB: new DbContext(options)<br/>com ConnectionString específica
    DB-->>VS: DbContext criado
    
    VS->>CTRL: Controller com _db pronto
    CTRL->>DB: Query/Insert/Update
    DB->>PSQL: Executa no banco da empresa
    PSQL-->>DB: Resultado
    DB-->>CTRL: Dados retornados
    CTRL-->>REQ: Response
    
    Note over REQ,PSQL: Cada empresa tem seu<br/>próprio banco de dados
```

---

## 8. SAVECHANGES - REUTILIZAÇÃO DE IDs

```mermaid
flowchart TD
    START[SaveChangesWithSyncAsync] --> ADDED{Existem entidades<br/>Added?}
    
    ADDED -->|Não| BASE[Chama base.SaveChangesAsync]
    ADDED -->|Sim| LOCK[LOCK TABLE EXCLUSIVE MODE]
    
    LOCK --> GET_IDS[Busca todos os IDs em uso]
    GET_IDS --> FIND_GAP[Procura gap na sequência<br/>Range 1..limite]
    
    FIND_GAP --> HAS_GAP{Encontrou gap?}
    
    HAS_GAP -->|Sim| USE_GAP[Usa primeiro ID vago]
    HAS_GAP -->|Não| CHECK_LIMIT{Próximo ID<br/><= limite?}
    
    CHECK_LIMIT -->|Sim| NEXT_ID[Próximo ID = Max + 1]
    CHECK_LIMIT -->|Não| ERROR[❌ Erro: Limite atingido]
    
    USE_GAP --> ASSIGN[Atribui ID à entidade]
    NEXT_ID --> ASSIGN
    
    ASSIGN --> UPDATE_LIST[Atualiza usedIds]
    UPDATE_LIST --> MORE{Mais entidades<br/>Added?}
    
    MORE -->|Sim| FIND_GAP
    MORE -->|Não| SYNC{sincroniza?}
    
    SYNC -->|Sim| ORPHAN[DeleteOrphans]
    SYNC -->|Não| BASE
    ORPHAN --> BASE
    
    BASE --> COMMIT[Commit no banco]
    COMMIT --> SEQ[Sincroniza Sequence PostgreSQL<br/>setval]
    SEQ --> END[✅ Sucesso]
    
    ERROR --> THROW[Throw Exception]
    THROW --> FAIL[❌ Falha]
    
    style USE_GAP fill:#4CAF50,color:#fff
    style NEXT_ID fill:#4CAF50,color:#fff
    style ERROR fill:#F44336,color:#fff
    style LOCK fill:#FF9800,color:#fff
```

---

## 9. CONTAEXAME - HIERARQUIA DE VALIDAÇÃO

```mermaid
graph LR
    subgraph ContaExame["ContaExame: 11.01.001.0005"]
        TIPO[11<br/>Tipo]
        FOLHA[01<br/>Folha]
        CONTA[001<br/>Conta Principal]
        ITEM[0005<br/>Item]
    end
    
    TIPO -. 2 dígitos .-> FOLHA
    FOLHA -. 2 dígitos .-> CONTA
    CONTA -. 3 dígitos .-> ITEM
    
    subgraph Validação["Validação por Prefixo (7 dígitos)"]
        PREFIXO[11.01.001<br/>Prefixo]
        
        ITEM1[11.01.001.0001]
        ITEM2[11.01.001.0002]
        ITEM3[11.01.001.0003]
        ITEM4[11.01.001.0004]
        ITEM5[11.01.001.0005]
    end
    
    PREFIXO -. StartsWith .-> ITEM1
    PREFIXO -. StartsWith .-> ITEM2
    PREFIXO -. StartsWith .-> ITEM3
    PREFIXO -. StartsWith .-> ITEM4
    PREFIXO -. StartsWith .-> ITEM5
    
    subgraph Verificação["Verificar FKs em:"]
        IER[ItensExamesRealizados]
        IERAM[ItensExamesRealizadosAM]
        REQ[Requisitar]
    end
    
    PREFIXO -. Validar .-> IER
    PREFIXO -. Validar .-> IERAM
    PREFIXO -. Validar .-> REQ
    
    style PREFIXO fill:#FF9800,color:#fff
    style ITEM5 fill:#4CAF50,color:#fff
```

---

## 10. DEPLOY - MODOS DE EXECUÇÃO

```mermaid
graph TB
    subgraph Dev["Desenvolvimento"]
        WIN_DEV[Windows Local]
        PSQL_DEV[(PostgreSQL Local)]
        KESTREL_DEV[Kestrel/IIS Express]
    end
    
    subgraph Prod_Win["Produção Windows"]
        WIN_SVC[Windows Service]
        PSQL_WIN[(PostgreSQL Remote)]
        IIS[IIS Server]
    end
    
    subgraph Prod_Linux["Produção Linux"]
        SYSCTL[systemd Service]
        PSQL_LNX[(PostgreSQL Remote)]
        KESTREL_LNX[Kestrel]
        NGINX[Nginx Reverse Proxy]
    end
    
    subgraph Workers["Worker Services"]
        EXPORT[ServicoExportacao<br/>Exportação]
        FILE[WindowsService<br/>File Write]
    end
    
    KESTREL_DEV --> PSQL_DEV
    IIS --> PSQL_WIN
    WIN_SVC --> PSQL_WIN
    KESTREL_LNX --> PSQL_LNX
    NGINX --> KESTREL_LNX
    
    EXPORT --> PSQL_WIN
    FILE --> PSQL_WIN
    
    style Dev fill:#E3F2FD
    style Prod_Win fill:#E8F5E9
    style Prod_Linux fill:#FFF3E0
    style Workers fill:#F3E5F5
```

---

## 11. AUTENTICAÇÃO E AUTORIZAÇÃO

```mermaid
sequenceDiagram
    participant USER Usuário
    participant CTRL Controller
    participant VALID ValidadorDeSessao
    participant CA ControleDeAcesso
    participant CP ControleDePerfil
    participant CPM ControleDePerfilMenu
    participant MENU Menu
    
    USER->>CTRL: Acessa URL protegida
    CTRL->>VALID: ValidarSessao()
    
    VALID->>VALID: Verifica Cookie Identity
    VALID->>VALID: Extrai UsuarioId
    
    VALID->>CA: Busca usuário
    CA-->>VALID: ControleDeAcesso encontrado?
    
    alt Usuário existe
        VALID->>CP: Busca perfil do usuário
        CP-->>VALID: Perfil retornado
        
        VALID->>CPM: Busca menus permitidos
        CPM-->>VALID: Lista de menus
        
        VALID->>MENU: Constrói menu dinâmico
        MENU-->>VALID: Menu HTML
        
        VALID-->>CTRL: Sessão válida + Menu
        CTRL-->>USER: Página renderizada com menu
    else Usuário não existe
        VALID-->>CTRL: Sessão inválida
        CTRL-->>USER: Redirect para Login
    end
```

---

## 12. INTEGRAÇÕES - EXPORTAÇÃO (STRATEGY PATTERN)

```mermaid
graph TB
    subgraph Trigger["Trigger"]
        SCHED[Scheduler]
        MANUAL[Manual]
    end
    
    subgraph Factory["Factory"]
        EF[ExportacaoFactory<br/>Create Strategy]
    end
    
    subgraph Strategies["Strategies"]
        AWS[AWS S3 Strategy]
        AZURE[Azure Blob Strategy]
        FTP[FTP Strategy]
        JSON[JSON Format]
        XML[XML Format]
        CSV[CSV Format]
    end
    
    subgraph Execution["Execution"]
        QUERY[Query Dados]
        TRANSFORM[Transforma Dados]
        VALIDATE[Valida Dados]
        UPLOAD[Upload Arquivo]
        LOG[Log Execução]
    end
    
    subgraph Storage["Storage"]
        S3[(AWS S3)]
        BLOB[(Azure Blob)]
        FTPSRV[(FTP Server)]
    end
    
    SCHED --> EF
    MANUAL --> EF
    
    EF --> AWS
    EF --> AZURE
    EF --> FTP
    
    AWS --> JSON
    AWS --> XML
    AZURE --> CSV
    FTP --> JSON
    
    JSON --> QUERY
    XML --> QUERY
    CSV --> QUERY
    
    QUERY --> TRANSFORM
    TRANSFORM --> VALIDATE
    VALIDATE --> UPLOAD
    
    UPLOAD --> S3
    UPLOAD --> BLOB
    UPLOAD --> FTPSRV
    
    UPLOAD --> LOG
    
    style Factory fill:#FF9800,color:#fff
    style Strategies fill:#4CAF50,color:#fff
    style Execution fill:#2196F3,color:#fff
    style Storage fill:#9C27B0,color:#fff
```

---

**Diagramas gerados por Qoder AI - 21/04/2026**
