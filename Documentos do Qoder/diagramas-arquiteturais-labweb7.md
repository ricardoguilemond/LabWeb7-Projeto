# Diagramas Arquiteturais - LabWeb7

**Data:** 21/04/2026

---

## 1. DIAGRAMA DE DEPENDÊNCIAS DA SOLUTION

```
+-------------------+     +-------------------+     +-------------------+
| LabWebMvc.MVC     | --> | BLL               | --> | ExtensionsMethods |
| Web Application   |     | Business Logic    |     | Utilities         |
+-------------------+     +-------------------+     +-------------------+
         |                         |
         v                         v
+-------------------+     +-------------------+
| ExtensionsMethods | <-- | ModeloDeDados     |
| Utilities         |     | Reference Models  |
+-------------------+     +-------------------+
         |
         v
+-------------------+
| ServicoExportacao |
| Worker Service    |
+-------------------+

+-------------------+ --> LabWebMvc.MVC
| WindowsService    | --> ExtensionsMethods
| Windows Service   | --> ServicoExportacao
+-------------------+
```

---

## 2. ARQUITETURA EM CAMADAS

```
CAMADA DE APRESENTACAO
+-------------------------------------------+
| Views .cshtml (Razor + Bootstrap)         |
| Controllers (Areas/Controllers)           |
| ViewModels (Validation)                   |
+-------------------------------------------+
         |
         v
CAMADA DE APLICACAO
+-------------------------------------------+
| GeralController (General Methods)         |
| ExclusaoService (Deletion Service)        |
| Imagem Service (Image Processing)         |
| ConcorrenciaService (Concurrency Control) |
+-------------------------------------------+
         |
         v
CAMADA DE NEGOCIO
+-------------------------------------------+
| BLL Classes (PartBLL, UtilBLL)            |
| PDF Converters (ConversoresPdf)           |
| Time Services (TempoLocal/TempoServidor)  |
+-------------------------------------------+
         |
         v
CAMADA DE DADOS
+-------------------------------------------+
| DbContext db.cs (EF Core)                 |
| Repositorio<T> (Generic Repository)       |
| DbFactory (Connection Factory)            |
+-------------------------------------------+
         |
         v
+-------------------------------------------+
| PostgreSQL (51 Tabelas)                   |
+-------------------------------------------+

CROSS-CUTTING CONCERNS
+-------------------------------------------+
| ExtensionsMethods (Genericos, Validations) |
| EventLogHelper (Logging)                  |
| Storage Services (AWS S3, Azure)          |
| ValidadorDeSessao (Session Validation)     |
+-------------------------------------------+
```

---

## 3. BANCO DE DADOS - RELACIONAMENTOS PRINCIPAIS

```
+-------------------+       +----------------------+
| Pacientes         |1----*| ExamesRealizados    |
| Id PK, NomePacien |       | Id PK, PacienteId FK|
| CPF, Nascimento   |       | MedicoId FK, InstitId|
| Sexo              |       | PostoId FK, TabelaId |
+-------------------+       | DataIni, Liberacao  |
   |1     |1     |1     +----------------------+
   |      |      |              |1           |1
   v      v      v              v            v
+------++------++------++----------------------+
| Requi|| Exame|| Exame|| ItensExamesRealizad |
| sitar|| Pendi|| Expor|| os                   |
|      || entes|| tados|+----------------------+
+------+ +-----+ +-----+

+-------------------+       +----------------------+
| Medicos           |1----*| ExamesRealizados    |
| Id PK, NomeMedico |       | Requisitar          |
| CRM, Especialidade|       | ExamesPendentes     |
+-------------------+       +----------------------+

+-------------------+       +-------------------+
| Instituicao       |1----*| Postos            |
| Id PK, Sigla      |       | ExamesRealizados  |
| Nome, CNPJ        |       | Requisitar        |
+-------------------+       +-------------------+
   |1                         |1
   v                          v
+-------------------+  +-------------------+
| TabelaExames      |  | ExamesRealizados  |
| Requisitar        |  | Requisitar        |
+-------------------+  +-------------------+
   |1
   v
+-------------------+
| PlanoExames       |
| ExamesRealizados  |
| Requisitar        |
+-------------------+

+-------------------+
| ClasseExames      |1----* Requisitar
|                   |1----* ExamesPendentes
+-------------------+

+-------------------+
| ControleDeAcesso  |1----* ControleDePerfil
+-------------------+
```

---

## 4. FLUXO: REQUISIÇÃO DE EXAMES

```
Usuario --> RequisitarController: Seleciona Instituicao, Posto, Medico, Paciente
RequisitarController --> MedicosController: Carrega/Seleciona Medico
MedicosController --> DbContext: Salva Medico (se novo)
DbContext --> MedicosController: Medico salvo (FORA da transacao)

RequisitarController --> PacientesController: Carrega/Seleciona Paciente
PacientesController --> DbContext: Salva Paciente (se novo)
DbContext --> PacientesController: Paciente salvo (FORA da transacao)

Usuario --> RequisitarController: Seleciona Exames do Plano
RequisitarController --> DbContext: Busca PlanoExames (filtro: TabelaExamesId)
DbContext --> RequisitarController: Retorna exames disponiveis

RequisitarController --> Transaction: BeginTransactionAsync()
RequisitarController --> DbContext: Adiciona itens em Requisitar
DbContext --> DbContext: Valida dados

   [Sucesso]
   DbContext --> RequisitarController: OK
   RequisitarController --> Transaction: CommitAsync()
   Transaction --> RequisitarController: Transacao completada
   RequisitarController --> Usuario: Exames requisitados com sucesso

   [Erro]
   DbContext --> RequisitarController: Erro de validacao
   RequisitarController --> Transaction: RollbackAsync()
   Transaction --> RequisitarController: Rollback completado
   RequisitarController --> Usuario: Erro: exames NAO salvos / Medico+Paciente permanecem salvos

NOTA: Medico e Paciente salvos NAO sao afetados pelo rollback
```

---

## 5. FLUXO: ALTERAÇÃO DE PLANO DE EXAMES (SUS MODEL)

```
Usuario --> PlanoExamesController: Altera item do Plano SUS (ExameId=1)
PlanoExamesController --> DbContext: Atualiza item SUS
PlanoExamesController --> PlanoExamesController: Verifica: E modelo SUS?

   [ExameId == 1 (SUS)]
   PlanoExamesController --> DbContext: Busca todas as TabelaExamesId
   DbContext --> PlanoExamesController: Lista de instituicoes
   loop: Para cada TabelaExamesId
      PlanoExamesController --> DbContext: Replica alteracoes (mesmo ContaExame)
      DbContext --> DbContext: Update PlanoExames WHERE ContaExame = X AND TabelaExamesId = Y
   end loop
   PlanoExamesController --> Usuario: Alteracao replicada para TODAS as instituicoes

   [ExameId != 1]
   PlanoExamesController --> DbContext: Atualiza apenas esta instituicao
   PlanoExamesController --> Usuario: Alteracao aplicada apenas nesta instituicao
```

---

## 6. FLUXO: EXCLUSÃO COM VALIDAÇÃO DE FK

```
+---------------------------+
| Iniciar Exclusao          |
+---------------------------+
   |
   v
+---------------------------+
| Existem registros em      |
| tabelas filhas?           |
+---------------------------+
   |              |
 SIM:          NAO:
   |              |
   v              v
+------------------+ +---------------------------+
| BLOQUEAR Exclusao| | Executar DELETE           |
+------------------+ +---------------------------+
   |                        |
   v                        v
+------------------+ +---------------------------+
| Retornar msg:    | | Iniciar Transacao         |
| "Registro possui |
| vinculos e nao   |
| pode ser excluido"|
+------------------+ |
   |                     +---------------------------+
   v                     | Executar DeleteOrphans?   |
+------------------+    +---------------------------+
| FIM - NAO excluido|       |              |
                        SIM:          NAO:
                           |              |
                           v              |
                  +--------------------+  |
                  | Remove registros   |  |
                  | orfaos            |  |
                  +--------------------+  |
                           |              |
                           v              v
                        +---------------------------+
                        | SaveChanges               |
                        +---------------------------+
                           |
                           v
                        +---------------------------+
                        | Sucesso?                  |
                        +---------------------------+
                           |              |
                        SIM:          NAO:
                           |              |
                           v              v
                  +------------------+ +----------------------+
                  | Commit           | | Rollback             |
                  | Transaction      | | Transaction          |
                  +------------------+ +----------------------+
                     |                      |
                     v                      v
              +------------------+ +----------------------+
              | FIM - Excluido   | | FIM - Erro, nada     |
              | com sucesso      | | excluido             |
              +------------------+ +----------------------+
```

---

## 7. MULTI-TENANT - TROCA DE CONEXÃO

```
HTTP Request --> ValidadorDeSessao: Request com sessao
ValidadorDeSessao --> ValidadorDeSessao: Valida sessao do usuario
ValidadorDeSessao --> ValidadorDeSessao: Obtem EmpresaId da sessao
ValidadorDeSessao --> ConnectionService: SetConnectionString(empresaId)
ConnectionService --> ConnectionService: Busca string de conexao da empresa no appsettings
ConnectionService --> ValidadorDeSessao: ConnectionString retornada
ValidadorDeSessao --> DbFactory: Create()
DbFactory --> DbContext: new DbContext(options) com ConnectionString especifica
DbContext --> ValidadorDeSessao: DbContext criado
ValidadorDeSessao --> Controller: Controller com _db pronto
Controller --> DbContext: Query/Insert/Update
DbContext --> PostgreSQL: Executa no banco da empresa
PostgreSQL --> DbContext: Resultado
DbContext --> Controller: Dados retornados
Controller --> HTTP Request: Response

NOTA: Cada empresa tem seu proprio banco de dados
```

---

## 8. SAVECHANGES - REUTILIZAÇÃO DE IDs

```
+-------------------------------------+
| SaveChangesWithSyncAsync            |
+-------------------------------------+
   |
   v
+-------------------------------------+
| Existem entidades Added?            |
+-------------------------------------+
   |              |
 NAO:            SIM:
   |              |
   v              v
+---------------------------+ +----------------------------+
| Chama base.SaveChangeAsync| | LOCK TABLE EXCLUSIVE MODE  |
+---------------------------+ +----------------------------+
                                |
                                v
                         +----------------------------+
                         | Busca todos os IDs em uso  |
                         +----------------------------+
                                |
                                v
                         +----------------------------+
                         | Procura gap na sequencia   |
                         | Range 1..limite            |
                         +----------------------------+
                                |
                                v
                         +----------------------------+
                         | Encontrou gap?             |
                         +----------------------------+
                            |              |
                         SIM:            NAO:
                            |              |
                            v              v
                  +------------------+ +-------------------------+
                  | Usa primeiro ID  | | Proximo ID <= limite?   |
                  | vago             | +-------------------------+
                  +------------------+    |              |
                                      SIM:            NAO:
                                         |              |
                                         v              v
                                +----------------+ +-------------------+
                                | Proximo ID =   | | ERRO: Limite     |
                                | Max + 1        | | atingido         |
                                +----------------+ +-------------------+
                                       |                     |
                                       v                     v
                                +------------------+ +-------------------+
                                | Atribui ID a    | | Throw Exception   |
                                | entidade        | | FALHA            |
                                +------------------+ +-------------------+
                                       |
                                       v
                                +------------------+
                                | Atualiza usedIds |
                                +------------------+
                                       |
                                       v
                                +------------------+
                                | Mais entidades   |
                                | Added?           |
                                +------------------+
                                   |           |
                                SIM:         NAO:
                                   |           |
                                   v           v
                     (volta a Procura gap) +------------------+
                                           | Sincroniza?      |
                                           +------------------+
                                              |           |
                                           SIM:         NAO:
                                              |           |
                                              v           |
                                     +------------------+  |
                                     | DeleteOrphans    |  |
                                     +------------------+  |
                                              |           |
                                              +-----+-----+
                                                    |
                                                    v
                                           +------------------+
                                           | Chama base.Save  |
                                           +------------------+
                                                    |
                                                    v
                                           +------------------+
                                           | Commit no banco  |
                                           +------------------+
                                                    |
                                                    v
                                           +------------------+
                                           | Sincroniza       |
                                           | Sequence PostgreSQL (setval) |
                                           +------------------+
                                                    |
                                                    v
                                           +------------------+
                                           | SUCESSO          |
                                           +------------------+
```

---

## 9. CONTAEXAME - HIERARQUIA DE VALIDAÇÃO

```
ContaExame: 11.01.001.0005

+-------+  +-------+  +-------------+  +-------+
| 11    |->| 01    |->| 001         |->| 0005  |
| Tipo  |  | Folha |  | Conta       |  | Item  |
+-------+  +-------+  + Principal  |  +-------+
                        +-------------+

Validacao por Prefixo (7 digitos): 11.01.001

   +-- StartsWith --> 11.01.001.0001
   |
   +-- StartsWith --> 11.01.001.0002
   |
   +-- StartsWith --> 11.01.001.0003
   |
   +-- StartsWith --> 11.01.001.0004
   |
   +-- StartsWith --> 11.01.001.0005

Verificar FKs em:
   +-- Validar --> ItensExamesRealizados
   +-- Validar --> ItensExamesRealizadosAM
   +-- Validar --> Requisitar
```

---

## 10. DEPLOY - MODOS DE EXECUÇÃO

```
DESENVOLVIMENTO
+---------------------------+
| Windows Local             |
| PostgreSQL Local          |
| Kestrel/IIS Express       |
+---------------------------+
         |
         v
PRODUCAO WINDOWS
+---------------------------+
| Windows Service           |
| PostgreSQL Remote         |
| IIS Server                |
+---------------------------+
         |
         v
PRODUCAO LINUX
+---------------------------+
| systemd Service           |
| PostgreSQL Remote         |
| Kestrel                   |
| Nginx Reverse Proxy       |
+---------------------------+

WORKER SERVICES
+---------------------------+
| ServicoExportacao         |
| (Exportacao)              |
+---------------------------+
+---------------------------+
| WindowsService            |
| (File Write)              |
+---------------------------+
```

---

## 11. AUTENTICAÇÃO E AUTORIZAÇÃO

```
Usuario --> Controller: Acessa URL protegida
Controller --> ValidadorDeSessao: ValidarSessao()
ValidadorDeSessao --> ValidadorDeSessao: Verifica Cookie Identity
ValidadorDeSessao --> ValidadorDeSessao: Extrai UsuarioId
ValidadorDeSessao --> ControleDeAcesso: Busca usuario
ControleDeAcesso --> ValidadorDeSessao: ControleDeAcesso encontrado?

   [Usuario existe]
   ValidadorDeSessao --> ControleDePerfil: Busca perfil do usuario
   ControleDePerfil --> ValidadorDeSessao: Perfil retornado
   ValidadorDeSessao --> ControleDePerfilMenu: Busca menus permitidos
   ControleDePerfilMenu --> ValidadorDeSessao: Lista de menus
   ValidadorDeSessao --> Menu: Constroi menu dinamico
   Menu --> ValidadorDeSessao: Menu HTML
   ValidadorDeSessao --> Controller: Sessao valida + Menu
   Controller --> Usuario: Pagina renderizada com menu

   [Usuario nao existe]
   ValidadorDeSessao --> Controller: Sessao invalida
   Controller --> Usuario: Redirect para Login
```

---

## 12. INTEGRAÇÕES - EXPORTAÇÃO (STRATEGY PATTERN)

```
TRIGGER
+---------------------------+
| Scheduler | Manual        |
+---------------------------+
         |
         v
FACTORY
+---------------------------+
| ExportacaoFactory         |
| Create Strategy           |
+---------------------------+
         |
         v
STRATEGIES
+---------------------------+
| AWS S3 Strategy           |
| Azure Blob Strategy       |
| FTP Strategy              |
+---------------------------+
   |          |          |
   v          v          v
FORMATS
+-----------+ +----------+ +----------+
| JSON      | | XML      | | CSV      |
+-----------+ +----------+ +----------+
         |
         v
EXECUTION
+---------------------------+
| Query Dados               |
| Transforma Dados          |
| Valida Dados              |
| Upload Arquivo            |
| Log Execucao             |
+---------------------------+
         |
         v
STORAGE
+-----------+ +-----------+ +-----------+
| AWS S3    | | Azure Blob| | FTP Server|
+-----------+ +-----------+ +-----------+
```

---

**Diagramas gerados por Qoder AI - 21/04/2026**
