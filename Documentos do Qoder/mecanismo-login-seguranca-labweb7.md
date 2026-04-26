# Mecanismo de Login e Segurança — LabWeb7

**Versão:** 2.0  
**Data:** 25/04/2026  
**Autor:** Qoder  
**Escopo:** Arquitetura multi-tenant, fluxo completo de autenticação, modelos de segurança, ordem de execução, passos manuais vs. automáticos, exemplos práticos e checklist operacional.

---

## 1. Visão Geral da Arquitetura Multi-Tenant

O LabWeb7 utiliza uma arquitetura **multi-tenant com bancos de dados separados por empresa**. Cada empresa-cliente possui seu próprio banco PostgreSQL isolado. Um banco central (`LABWEB7Empresas`) atua como roteador de autenticação.

### 1.1 Bancos de Dados

```
+--------------------+-------------------------------------------------+----------------------------------+
|Banco               |Papel                                            |Quem usa                          |
+--------------------+-------------------------------------------------+----------------------------------+
|`LABWEB7Empresas`   |Roteamento central de login e licenciamento      |Aplicacao (apenas durante o login)|
+--------------------+-------------------------------------------------+----------------------------------+
|`LABWEB7`           |Base da empresa de testes (CNPJ `00000000000100`)|Usuarios da empresa teste         |
+--------------------+-------------------------------------------------+----------------------------------+
|`LABWEB7Barros`     |Base da empresa Barros (CNPJ `02557289000170`)   |Usuarios da empresa Barros        |
+--------------------+-------------------------------------------------+----------------------------------+
|`LABWEB7NomeCliente`|Base de qualquer empresa adicional               |Usuarios dessa empresa            |
+--------------------+-------------------------------------------------+----------------------------------+
```

> Cada novo cliente recebe um banco isolado. O `LABWEB7Empresas` **nunca armazena senhas de usuários**.

### 1.2 Diagrama da Arquitetura Multi-Tenant

```
+------------------------------------------------------------------+
|                    Aplicacao LabWeb7                             |
+------------------------------------------------------------------+
                             |
                             v
+------------------------------------------------------------------+
|                  LABWEB7Empresas                                 |
|            (Banco Central de Roteamento)                         |
+------------------------------------------------------------------+
   |                    |               |                |
   v                    v               v                v
+----------------+ +---------+ +----------------+ +-----------------------+
| EmpresaCliente | | Emails  | | EmpresaLogin   | | EmpresaLogin          |
|                | | Cache:  | | 00000000000100 | | 02557289000170        |
| StringConexao/ | | email ->| | Perfis empresa | | Perfis empresa Barros |
| empresa        | | empresa | | teste          | |                       |
+----------------+ +---------+ +----------------+ +-----------------------+
   | StringConexao                 | StringConexao
   v                               v
+---------------------------+  +------------------------+
| LABWEB7                   |  | LABWEB7Barros          |
| Empresa Teste             |  | Empresa Barros         |
| CNPJ 00000000000100       |  | CNPJ 02557289000170    |
+---------------------------+  +------------------------+
   |              |         |  |            |         |
   v              v         v  v            v         v
+--------+ +------------+ +--------+ +------------+ +--------+
| Senhas | | UsuariosWeb| | Empresa| | UsuariosWeb| | Senhas |
+--------+ +------------+ +--------+ +------------+ +--------+
```

### 1.3 Usuário de Serviço PostgreSQL

Toda conexão da aplicação usa o usuário PostgreSQL `sistema`, cujas credenciais estão em `Settings.cs → BasePadrao`:

```
+---------------------+----------------------------------+-----------------------------------------+
|Propriedade          |Valor (dev)                       |Uso                                      |
+---------------------+----------------------------------+-----------------------------------------+
|`BasePadrao.UserId`  |"sistema"                         |Substitui placeholder "usubanco" na conn.|
+---------------------+----------------------------------+-----------------------------------------+
|`BasePadrao.Password`|"Acer@105"                        |Substitui placeholder "ususenha" na conn.|
+---------------------+----------------------------------+-----------------------------------------+
|`BasePadrao.Chave`   |"sEabc777cioR5g7RioRo3m1988ar2602"|Chave AES legada (apenas para migracao)  |
+---------------------+----------------------------------+-----------------------------------------+
```

As connection strings em `appsettings.json` usam os placeholders `usubanco` e `ususenha`, que são substituídos em tempo de execução por `BasePadrao`.

> ✅ **CORRIGIDO (24/04/2026):** As credenciais `BasePadrao.Password` e chaves AES foram migradas para `appsettings.json` (Seção `Secrets`). O código-fonte não contém mais credenciais hardcoded. Consulte [gerenciamento-chaves-segredos-labweb7.md](gerenciamento-chaves-segredos-labweb7.md).

---

## 2. Estrutura de Tabelas Envolvidas no Login

### 2.1 Tabelas em `LABWEB7Empresas`

#### `EmpresaCliente` — Registro das empresas licenciadas

```
+----------------+--------+-----------------------------------------------------+
|Campo           |Tipo    |Descricao                                            |
+----------------+--------+-----------------------------------------------------+
|`Id`            |int     |PK auto-incremento                                   |
+----------------+--------+-----------------------------------------------------+
|`CNPJ`          |string  |CNPJ da empresa (14 digitos, sem formatacao)         |
+----------------+--------+-----------------------------------------------------+
|`Email`         |string  |E-mail do OWNER/ADM da empresa                       |
+----------------+--------+-----------------------------------------------------+
|`StringConexao` |string  |Connection string completa para o banco desta empresa|
+----------------+--------+-----------------------------------------------------+
|`LimiteUsuarios`|string  |Limite de usuarios simultaneos                       |
+----------------+--------+-----------------------------------------------------+
|`DataExpira`    |DateTime|Data de expiracao da licenca                         |
+----------------+--------+-----------------------------------------------------+
```

**Exemplo de registro:**
```
Id=2, CNPJ='02557289000170', Email='ricardoguilemond@outlook.com',
StringConexao='Host=localhost;Port=5432;Database=LABWEB7Barros;Username=sistema;Password=Acer@105;SSL Mode=prefer;'
```

> ⚠️ O campo `StringConexao` **deve usar formato Npgsql** (`Host=`, `Username=`). O formato SQL Server (`Server=`, `User ID=`) **não funciona** e causa fallback silencioso para o banco padrão.

#### `Emails` — Cache de roteamento e-mail → empresa

```
+------------------+--------+--------------------------------+
|Campo             |Tipo    |Descricao                       |
+------------------+--------+--------------------------------+
|`Email`           |string  |E-mail do usuario (ADM ou comum)|
+------------------+--------+--------------------------------+
|`EmpresaClienteId`|int     |FK para `EmpresaCliente.Id`     |
+------------------+--------+--------------------------------+
|`DataCadastro`    |DateTime|Data de inclusao                |
+------------------+--------+--------------------------------+
```

**Exemplo:**
```
Email='ricardoguilemond@outlook.com', EmpresaClienteId=2
Email='rguilemond@gmail.com',         EmpresaClienteId=1
```

#### `EmpresaLogin{CNPJ}` — Perfis de acesso por empresa

Uma tabela por empresa, nomeada com o CNPJ (sem formatação):
- `EmpresaLogin00000000000100` → empresa LABWEB7 (testes)
- `EmpresaLogin02557289000170` → empresa LABWEB7Barros

```
+--------+------+------------------------------------------+
|Campo   |Tipo  |Descricao                                 |
+--------+------+------------------------------------------+
|`Id`    |int   |PK auto-incremento                        |
+--------+------+------------------------------------------+
|`Email` |string|E-mail do usuario registrado nesta empresa|
+--------+------+------------------------------------------+
|`Perfil`|int   |`1` = Administrador, `2+` = perfis comuns |
+--------+------+------------------------------------------+
```

---

### 2.2 Tabelas em cada banco de cliente (`LABWEB7`, `LABWEB7Barros`, etc.)

#### `Senhas` — Credenciais e identidade do usuário

```
+-----------------+---------+--------------------------------------------------------------+
|Campo            |Tipo     |Descricao                                                     |
+-----------------+---------+--------------------------------------------------------------+
|`Id`             |int      |PK auto-incremento                                            |
+-----------------+---------+--------------------------------------------------------------+
|`LoginUsuario`   |string   |E-mail do usuario (chave de busca no login, convencao atual)  |
+-----------------+---------+--------------------------------------------------------------+
|`NomeUsuario`    |string   |Nome de usuario em maiusculas                                 |
+-----------------+---------+--------------------------------------------------------------+
|`NomeCompleto`   |string   |Nome completo em maiusculas                                   |
+-----------------+---------+--------------------------------------------------------------+
|`SenhaUsuario`   |string   |Hash BCrypt (formato `$2a$11$...`) ou AES legado (Base64 puro)|
+-----------------+---------+--------------------------------------------------------------+
|`Email`          |string   |E-mail real do usuario                                        |
+-----------------+---------+--------------------------------------------------------------+
|`CNPJEmpresa`    |string   |CNPJ da empresa a qual pertence                               |
+-----------------+---------+--------------------------------------------------------------+
|`Bloqueado`      |int      |`0` = ativo, `1` = bloqueado                                  |
+-----------------+---------+--------------------------------------------------------------+
|`EmailConfirmado`|int      |`0` = pendente, `1` = confirmado                              |
+-----------------+---------+--------------------------------------------------------------+
|`Administrador`  |int      |`0` = usuario comum, `1` = administrador                      |
+-----------------+---------+--------------------------------------------------------------+
|`DataExpira`     |DateTime?|Data de expiracao da conta (`null` = sem expiracao)           |
+-----------------+---------+--------------------------------------------------------------+
```

#### `UsuariosWeb` — Dados complementares do usuário

```
+-----------------------+--------+--------------------------------------+
|Campo                  |Tipo    |Descricao                             |
+-----------------------+--------+--------------------------------------+
|`SenhaId`              |int     |FK real para `Senhas.Id` (via EF Core)|
+-----------------------+--------+--------------------------------------+
|`CPFUsuario`           |string  |CPF sem formatacao (11 digitos)       |
+-----------------------+--------+--------------------------------------+
|`DataNascimentoUsuario`|DateTime|Data de nascimento                    |
+-----------------------+--------+--------------------------------------+
|`CNPJEmpresa`          |string  |CNPJ da empresa                       |
+-----------------------+--------+--------------------------------------+
```

#### `Empresa` — Dados cadastrais da empresa no banco do cliente

```
+--------------+------+--------------------------------------------------+
|Campo         |Tipo  |Descricao                                         |
+--------------+------+--------------------------------------------------+
|`Id`          |int   |PK                                                |
+--------------+------+--------------------------------------------------+
|`CNPJ`        |string|CNPJ desta empresa (deve bater com EmpresaCliente)|
+--------------+------+--------------------------------------------------+
|`NomeFantasia`|string|Nome exibido no rodape e sessao                   |
+--------------+------+--------------------------------------------------+
|`UF`          |string|Estado sede da empresa                            |
+--------------+------+--------------------------------------------------+
```

> ⚠️ **Atenção:** A tabela `Empresa` de cada banco pode ter múltiplos registros (dados de teste). 
O sistema filtra pelo CNPJ correto da empresa logada via `_db.Empresa.FirstOrDefault(e => e.CNPJ == cnpjEmpresaLogada)`.

---

## 3. O "Falso Relacionamento" entre Bancos

Este é o ponto mais **complexo** da arquitetura (intencional, viável, não precisa ser corrigido):

```
LABWEB7Empresas.EmpresaLogin00000000000100.Email  ←→  LABWEB7.Senhas.LoginUsuario
LABWEB7Empresas.EmpresaLogin02557289000170.Email  ←→  LABWEB7Barros.Senhas.LoginUsuario
```

**Por que é "falso"?**
- Não existe FK de banco de dados (são instâncias PostgreSQL diferentes)
- O EF Core não conhece esse relacionamento
- É mantido **somente por convenção de código** — escolha arquitetural intencional
- O campo `Senhas.LoginUsuario` armazena o **e-mail** do usuário (convenção desde a versão atual)
- FK cross-database via `postgres_fdw` é possível mas **não viável** (latência, isolamento, disponibilidade)
- Constraints locais (`CHECK LoginUsuario = Email`, `UNIQUE Email`) são a alternativa recomendada

**Regra de ouro:**
> `Senhas.LoginUsuario` = `Senhas.Email` = e-mail digitado no login

Registros criados antes desta convenção podem ter `LoginUsuario` com um Id numérico. 
Esses registros precisam de correção manual (ver seção 7.1).

---

## 4. Tipos de Usuário e Onde São Cadastrados

```
+-----------------+--------------------------------------------------+------------------------+
|Tipo             |LABWEB7Empresas                                   |Banco do Cliente        |
+-----------------+--------------------------------------------------+------------------------+
|OWNER/ADM licenca|`EmpresaCliente` + `Emails` + `EmpresaLogin{CNPJ}`|`Senhas` + `UsuariosWeb`|
+-----------------+--------------------------------------------------+------------------------+
|Usuario comum    |Apenas `Emails`                                   |`Senhas` + `UsuariosWeb`|
+-----------------+--------------------------------------------------+------------------------+
```

**Regras:**
- Usuários comuns **nunca** estão em `EmpresaCliente`
- OWNER/ADM estão em `EmpresaCliente` **e** na base do cliente
- Todo usuário (ADM ou comum) **sempre** tem entrada em `Emails` para o roteamento funcionar

---

## 5. Fluxo Completo de Login — Ordem de Execução

### Legenda
- 🤖 **Automático** — executado pelo Sistema sem intervenção
- 👤 **Manual** — requer ação do operador/DBA
- 🔑 **Segurança** — etapa crítica de segurança

---

### 5.1 Diagrama de Fluxo (ASCII)

```
+------------------------------------+
| Inicio: Usuario digita email+senha |
+------------------------------------+
   |
   v
+-------------------------------+
| HomeController.ContinuarLogin |
+-------------------------------+
   |
   v
+------------------------------------------+
| ValidacoesDeSenhas.RetornaValidacaoLogin |
+------------------------------------------+
   |
   v
+--------------------------------+
| Busca email em LABWEB7Empresas |
+--------------------------------+
   |
   v
+-------------------+
| Email encontrado? |
+-------------------+
   |
   +==> NAO: Busca em EmpresaCliente pelo email
   |         |
   |         v
   |     +---------------------------+
   |     | OWNER encontrado?         |
   |     +---------------------------+
   |        |              |
   |     NAO:           SIM:
   |        |              |
   |        v              v
   |  +----------------+ +-------------------------------------+
   |  | ERRO: Acesso   | | Cria Emails + EmpresaLogin         |
   |  | nao autorizado | | + Senhas com BCrypt                |
   |  +----------------+ +-------------------------------------+
   |                              |
   |                              v
   +==========================> +--------------------+
                               | Obtem StringConexao |
                               +--------------------+
                                        |
                                        v
                               +--------------------------------+
                               | SELECT Senhas WHERE            |
                               | LoginUsuario = email           |
                               +--------------------------------+
                                        |
                                        v
                               +--------------------+
                               | Usuario encontrado?|
                               +--------------------+
                                  |            |
                               NAO:          SIM:
                                  |            |
                                  v            v
                          +---------------+ +-------------------------+
                          | ERRO: Login   | | VerificaSenhaComMigracao|
                          | invalido      | +-------------------------+
                          +---------------+          |
                                                     v
                                            +----------------+
                                            | Formato BCrypt?|
                                            +----------------+
                                               |         |
                                            SIM:      NAO:
                                               |         |
                                               v         v
                                    +--------------+ +----------------------------+
                                    | BCrypt.Verify| | Descriptografa AES         |
                                    +--------------+ | + Migra para BCrypt        |
                                           |         +----------------------------+
                                           |               |
                                           +-------+-------+
                                                   |
                                                   v
                                          +----------------+
                                          | Senha valida?  |
                                          +----------------+
                                             |         |
                                          NAO:      SIM:
                                             |         |
                                             v         v
                                    +----------------+ +--------------------------------+
                                    | ERRO: Senha    | | Valida Bloqueado +             |
                                    | incorreta      | | EmailConfirmado + DataExpira   |
                                    +----------------+ +--------------------------------+
                                                              |
                                                              v
                                                     +---------------------------+
                                                     | Grava Sessao + Cookie     |
                                                     +---------------------------+
                                                              |
                                                              v
                                                     +---------------------------+
                                                     | Redirect para Home        |
                                                     | BaseController restaura   |
                                                     | conexao por requisicao    |
                                                     +---------------------------+
```

---

### 5.3 Detalhe de Cada Etapa

```
+------------+------------+-----------------------------------------------------------------------------------------+
|Etapa       |Quem executa|O que faz                                                                                |
+------------+------------+-----------------------------------------------------------------------------------------+
|1           |Sistema     |Conecta em `LABWEB7Empresas` usando `PSQLConnectionStringEmpresas` do `appsettings.json` |
+------------+------------+-----------------------------------------------------------------------------------------+
|2           |Sistema     |SELECT * FROM "Emails" WHERE "Email" = email                                             |
+------------+------------+-----------------------------------------------------------------------------------------+
|3a (novo)   |Sistema     |Se e-mail nao existe: primeiro acesso OWNER. Cria registros automaticamente.             |
+------------+------------+-----------------------------------------------------------------------------------------+
|3b (retorno)|Sistema     |Se e-mail existe: le EmpresaClienteId, busca EmpresaCliente, obtem StringConexao         |
+------------+------------+-----------------------------------------------------------------------------------------+
|4           |Sistema     |Troca contexto do banco para o banco do cliente correto                                  |
+------------+------------+-----------------------------------------------------------------------------------------+
|5           |Sistema     |SELECT * FROM "Senhas" WHERE "LoginUsuario" = email no banco do cliente                  |
+------------+------------+-----------------------------------------------------------------------------------------+
|6           |Sistema     |Verifica hash BCrypt ou migra AES para BCrypt automaticamente                            |
+------------+------------+-----------------------------------------------------------------------------------------+
|7           |Sistema     |Valida se conta esta ativa, e-mail confirmado, nao expirada                              |
+------------+------------+-----------------------------------------------------------------------------------------+
|8           |Sistema     |Monta vmSenhas com StringDeConexao e CNPJEmpresa pelo CNPJ correto                       |
+------------+------------+-----------------------------------------------------------------------------------------+
|9           |Sistema     |HomeController persiste StringConexao na sessao HTTP                                     |
+------------+------------+-----------------------------------------------------------------------------------------+
|10          |Sistema     |Grava todas as variaveis de sessao e cria cookie de autenticacao                         |
+------------+------------+-----------------------------------------------------------------------------------------+
|11          |Sistema     |Redireciona para a Home                                                                  |
+------------+------------+-----------------------------------------------------------------------------------------+
|12          |Sistema     |`BaseController.OnActionExecuting()` restaura a conexao do tenant em cada nova requisicao|
+------------+------------+-----------------------------------------------------------------------------------------+
```

---

## 6. Exemplo Prático Completo

### Cenário: Login de `ricardoguilemond@outlook.com` (empresa Barros)

```
1. Usuário acessa /Home/Login e digita:
   Email: ricardoguilemond@outlook.com
   Senha: 12345

2. Sistema conecta em LABWEB7Empresas e executa:
   SELECT * FROM "Emails" WHERE "Email" = 'ricardoguilemond@outlook.com'
   → Retorna: EmpresaClienteId = 2

3. Sistema busca EmpresaCliente:
   SELECT * FROM "EmpresaCliente" WHERE "Id" = '2'
   → Retorna: CNPJ='02557289000170',
              StringConexao='Host=localhost;Port=5432;Database=LABWEB7Barros;...'

4. Sistema troca conexão:
   _connectionService.SetConnectionString('Host=...LABWEB7Barros...')
   _db = new Db(UseNpgsql('Host=...LABWEB7Barros...'))

5. Sistema executa em LABWEB7Barros:
   SELECT * FROM "Senhas" WHERE "LoginUsuario" = 'ricardoguilemond@outlook.com'
   → Retorna: SenhaUsuario = '$2a$11$...' (hash BCrypt)

6. BCrypt.Verify("12345", "$2a$11$...") → true ✅

7. Validações: Bloqueado=0 ✅, EmailConfirmado=1 ✅, DataExpira=null ✅

8. Busca empresa pelo CNPJ correto em LABWEB7Barros:
   SELECT * FROM "Empresa" WHERE "CNPJ" = '02557289000170'
   → Retorna: NomeFantasia='LABORATÓRIO BARROS', UF='MG'

9. Sessão gravada:
   SessionEmail      = 'ricardoguilemond@outlook.com'
   SessionCNPJEmpresa= '02557289000170'
   SessionNomeEmpresa= 'LABORATÓRIO BARROS'
   SessionStringConexao = 'Host=...LABWEB7Barros...'

10. Cookie de autenticação criado. Redirect para /Home

11. Cada requisição subsequente:
    OnActionExecuting() lê SessionStringConexao → recria _db → LABWEB7Barros ✅
```

---

## 7. Modelo de Segurança Adotado

### 7.1 Isolamento de Dados por Tenant (Database-per-Tenant)

**Modelo:** Cada empresa possui seu próprio banco PostgreSQL isolado.

**Benefícios:**
- Vazamento de dados de um cliente **não compromete** outros
- Backup, restore e manutenção por empresa de forma independente
- Conformidade com LGPD: dados de clientes fisicamente separados
- Possibilidade de hospedar em servidores diferentes por cliente

**Funcionamento:** O `IConnectionService` (Scoped) armazena a connection string ativa para a requisição. O `BaseController.OnActionExecuting()` restaura a conexão correta da sessão em cada nova requisição HTTP.

---

### 7.2 BCrypt para Armazenamento de Senhas

**Modelo:** Senhas armazenadas como hash irreversível via `BCrypt.Net-Next` com work factor `$2a$11$` (2.048 iterações).

**Benefícios:**
- Hash **irreversível** — impossível recuperar a senha original
- **Salt automático** — cada hash é único mesmo para senhas iguais (evita rainbow tables)
- **Work factor ajustável** — pode ser aumentado sem quebrar compatibilidade
- Resistente a ataques de força bruta e dicionário

**Comparação de formatos:**

```
+----------+------------------------------+---------+-----------+
|Formato   |Exemplo                       |Seguranca|Status     |
+----------+------------------------------+---------+-----------+
|BCrypt    |`$2a$11$cfSNMo7i82RP...`      |Alta     |Atual      |
+----------+------------------------------+---------+-----------+
|AES-CBC   |`BAHImD+dYlY+zWRF...` (Base64)|Baixa    |Legado     |
+----------+------------------------------+---------+-----------+
|Texto puro|`12345`                       |Nenhuma  |Nunca usado|
+----------+------------------------------+---------+-----------+
```

---

### 7.3 Migração Transparente AES → BCrypt

**Modelo:** Na primeira autenticação bem-sucedida com senha AES legada, o sistema migra automaticamente para BCrypt sem intervenção do usuário.

**Fluxo de migração:**

```
+-----------------------------+
| Login com senha AES        |
+-----------------------------+
   |
   v
+-----------------------------+
| VerificaSenhaComMigracao   |
+-----------------------------+
   |
   v
+-----------------------------+
| Comeca com $2?             |
+-----------------------------+
   |              |
 SIM:          NAO:
   |              |
   v              v
+----------------+ +--------------------------------+
| BCrypt.Verify  | | Descriptografa AES-CBC        |
| vs hash        | | usando BasePadrao.Chave       |
+----------------+ +--------------------------------+
   |                     |
   v                     v
+-----------+     +-------------+
| Valida?   |     | Senha       |
+-----------+     | correta?    |
   |    |          +-------------+
 SIM:  NAO:          |        |
   |    |         SIM:      NAO:
   |    |          |         |
   |    v          v         v
   |  +------------------+ +------------------------+
   |  | ERRO: Senha      | | Gera BCrypt.HashPassword|
   |  | incorreta        | | workFactor=11           |
   |  +------------------+ +------------------------+
   |                             |
   |                             v
   |                    +----------------------------------+
   |                    | UPDATE Senhas SET SenhaUsuario   |
   |                    | = novo hash BCrypt              |
   |                    +----------------------------------+
   |                             |
   v                             v
+------------------------------------------+
| Login bem-sucedido                       |
| Proximo login usa BCrypt                 |
+------------------------------------------+
```

**Benefício:** Zero downtime na migração de segurança. Usuários não percebem a mudança.

---

### 7.4 DevSenhasReset (somente DEBUG)

**Modelo:** Método de startup que faz reset em massa de senhas legadas para BCrypt.

**Fluxo automático ao iniciar em DEBUG:**
```
Startup.cs → DevSenhasReset()
    ↓
Conecta em LABWEB7Empresas
    ↓
SELECT DISTINCT "StringConexao" FROM "EmpresaCliente"
    ↓
Para cada banco de cliente:
    SELECT * FROM "Senhas" WHERE "SenhaUsuario" NOT LIKE '$2%'
    ↓
    UPDATE SET "SenhaUsuario" = BCrypt(config["LoginPadraoSistema:Senha"])
    ↓
    Log no EventViewer
```

**Senha padrão:** definida em `appsettings.Development.json → LoginPadraoSistema:Senha` (padrão: `"12345"`).

---

### 7.5 Autenticação por Cookie com Claims

**Modelo:** Após login bem-sucedido, o sistema emite um cookie de autenticação com Claims.

**Claims gravados:**
- `ClaimTypes.Name` → LoginUsuario (e-mail)
- `ClaimTypes.Email` → e-mail
- `"NomeCompleto"` → nome completo
- `"Empresa"` → nome da empresa

**Benefício:** O cookie é `HttpOnly` e `SameSite=Lax`, protegido contra XSS e CSRF básico.

---

### 7.6 Sessão HTTP para Persistência do Tenant

**Modelo:** A `StringConexao` da empresa logada é persistida na sessão HTTP (`SessionStringConexao`) e restaurada automaticamente a cada requisição pelo `BaseController.OnActionExecuting()`.

**Por que é necessário:** O `IConnectionService` é `Scoped` (uma instância por requisição HTTP). Sem a restauração via sessão, cada requisição iniciaria com o banco padrão (`LABWEB7`) em vez do banco correto do usuário logado.

```
Requisição 1 (login):       SetConnectionString(LABWEB7Barros) + Session["SessionStringConexao"]
Requisição 2 (qualquer):    OnActionExecuting() → lê sessão → SetConnectionString(LABWEB7Barros)
Requisição N:               Mesmo comportamento ↑
```

---

## 8. Passos Manuais vs. Automáticos — Resumo

### 8.1 Passos 100% Automáticos (Sistema)

```
+--+--------------------+----------------------------------------------------------------------------+
|# |Quando              |O que o Sistema faz                                                         |
+--+--------------------+----------------------------------------------------------------------------+
|A1|Cada login          |Roteamento e-mail para banco correto via `LABWEB7Empresas.Emails`           |
+--+--------------------+----------------------------------------------------------------------------+
|A2|Primeiro login OWNER|Cria `Emails`, `EmpresaLogin{CNPJ}`, `Senhas`, `UsuariosWeb` automaticamente|
+--+--------------------+----------------------------------------------------------------------------+
|A3|Login com senha AES |Migra para BCrypt transparentemente                                         |
+--+--------------------+----------------------------------------------------------------------------+
|A4|Startup em DEBUG    |`DevSenhasReset` migra todas as senhas legadas                              |
+--+--------------------+----------------------------------------------------------------------------+
|A5|Cada requisicao     |`OnActionExecuting()` restaura conexao do tenant da sessao                  |
+--+--------------------+----------------------------------------------------------------------------+
|A6|Login bem-sucedido  |Cria cookie de autenticacao com Claims                                      |
+--+--------------------+----------------------------------------------------------------------------+
```

### 8.2 Passos Manuais (Operador/DBA)

```
+-----+----------------------+---------------------------------------------------------+-------------------------------------+
|#    |Quando                |O que deve ser feito manualmente                         |Por que                              |
+-----+----------------------+---------------------------------------------------------+-------------------------------------+
|M1   |Novo banco de cliente |Executar `GRANT` para o usuario `sistema`                |PostgreSQL nao herda permissoes      |
|     |criado                |                                                         |automaticamente                      |
+-----+----------------------+---------------------------------------------------------+-------------------------------------+
|M2   |Novo banco de cliente |Registrar empresa em `EmpresaCliente` com `StringConexao`|O roteamento depende desse registro  |
|     |                      |formato Npgsql                                           |                                     |
+-----+----------------------+---------------------------------------------------------+-------------------------------------+
|M3   |Migracao de banco     |Corrigir registros com `LoginUsuario` = Id numerico para |Sistema de login usa e-mail como     |
|     |legado                |e-mail                                                   |chave de busca                       |
+-----+----------------------+---------------------------------------------------------+-------------------------------------+
|M4   |Novo banco de cliente |Registrar `Empresa` no banco do cliente com o CNPJ       |O CNPJ exibido no rodape vem desta   |
|     |                      |correto                                                  |tabela                               |
+-----+----------------------+---------------------------------------------------------+-------------------------------------+
|M5   |Producao              |Migrar `BasePadrao.Password` para User Secrets           |Seguranca: senha hardcoded no codigo-|
|     |                      |                                                         |fonte                                |
+-----+----------------------+---------------------------------------------------------+-------------------------------------+
```

---

## 9. Ordem de Execução para Novo Cliente

Ao adicionar uma nova empresa-cliente ao sistema, seguir **exatamente** esta ordem:

### Passo 1 — MANUAL: Criar banco PostgreSQL
```sql
-- Executado pelo DBA como superusuário postgres
CREATE DATABASE "LABWEB7NomeCliente";
```

### Passo 2 — MANUAL: Executar script de criação das tabelas
```
Executar o script SQL completo de estrutura do LabWeb7 no novo banco.
```

### Passo 3 — MANUAL: Conceder permissões ao usuário `sistema`
```sql
-- Conectado ao novo banco LABWEB7NomeCliente:
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO sistema;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO sistema;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO sistema;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO sistema;
```

### Passo 4 — MANUAL: Cadastrar empresa em `LABWEB7Empresas`
```sql
-- Conectado ao banco LABWEB7Empresas:
INSERT INTO "EmpresaCliente" ("CNPJ", "Email", "StringConexao", "LimiteUsuarios", "DataExpira")
VALUES (
    '12345678000199',
    'owner@empresa.com.br',
    'Host=SERVIDOR;Port=5432;Database=LABWEB7NomeCliente;Username=sistema;Password=Acer@105;SSL Mode=prefer;',
    '10',
    '2027-12-31'
);
```

> ⚠️ A `StringConexao` **deve usar formato Npgsql** (`Host=`, `Username=`). Nunca usar `Server=` ou `User ID=`.

### Passo 5 — MANUAL: Cadastrar `Empresa` no banco do cliente
```sql
-- Conectado ao banco LABWEB7NomeCliente:
INSERT INTO "Empresa" ("CNPJ", "NomeFantasia", "UF")
VALUES ('12345678000199', 'NOME DA EMPRESA', 'SP');
```

### Passo 6 — AUTOMÁTICO: Primeiro login do OWNER
```
O OWNER acessa o sistema e digita seu e-mail + senha.
O Sistema automaticamente:
  → Cria registro em LABWEB7Empresas.Emails
  → Cria registro em LABWEB7Empresas.EmpresaLogin12345678000199
  → Cria Senhas + UsuariosWeb em LABWEB7NomeCliente com senha BCrypt("12345")
O OWNER faz login com a senha padrão "12345" e a troca em seguida.
```

### Passo 7 — OPCIONAL: Reiniciar em DEBUG
```
Se necessário resetar senhas legadas, reiniciar a aplicação em modo DEBUG.
O DevSenhasReset automaticamente cobre o novo banco.
```

---

## 10. Problemas Conhecidos e Correções

### 10.1 Registros legados com `LoginUsuario` = Id numérico

**Causa:** Registros criados antes da convenção `LoginUsuario = email`.  
**Sintoma:** Login retorna "Login inválido" mesmo com senha correta.  
**Correção manual:**
```sql
-- Executar na base do cliente afetado (ex: LABWEB7Barros):
UPDATE "Senhas"
SET "LoginUsuario" = 'email@dominio.com',
    "SenhaUsuario" = '$2a$11$cfSNMo7i82RPp7ylFWOgJuIohFUGF.nKKKcM0qUTHAhVt8q1Vt6Ti'
WHERE "Email" = 'email@dominio.com';
-- O hash acima corresponde à senha "12345"
```

### 10.2 `GRANT` ausente em banco de cliente

**Causa:** Banco criado sem conceder permissões ao usuário `sistema`.  
**Sintoma:** `42501: permissão negada para tabela Senhas`.  
**Correção:** Executar o bloco de `GRANT` do Passo 3 acima na base afetada.

### 10.3 `StringConexao` em formato SQL Server (`Server=`)

**Causa:** Registro em `EmpresaCliente.StringConexao` usando formato MSSQL.  
**Sintoma:** Login roteado silenciosamente para o banco padrão (outra empresa).  
**Correção:**
```sql
-- Conectado a LABWEB7Empresas:
UPDATE "EmpresaCliente"
SET "StringConexao" = 'Host=SERVIDOR;Port=5432;Database=BANCO;Username=sistema;Password=SENHA;SSL Mode=prefer;'
WHERE "CNPJ" = '02557289000170';
```

### 10.4 CNPJ errado exibido no rodapé

**Causa:** Tabela `Empresa` do banco do cliente tem múltiplos registros e o `Id=1` tem CNPJ incorreto.  
**Sintoma:** Rodapé exibe CNPJ da empresa de teste.  
**Correção:** O sistema agora filtra por `WHERE "CNPJ" = cnpjEmpresaLogada`. Garantir que o registro com o CNPJ correto exista na tabela `Empresa`.

### 10.5 `IConnectionService` não persistido entre requisições

**Causa arquitetural (já corrigida):** `IConnectionService` é Scoped; cada requisição nova iniciava com o banco padrão.  
**Correção implementada:** `SessionStringConexao` gravado na sessão durante o login. `BaseController.OnActionExecuting()` restaura a conexão em cada requisição.

---

## 11. Checklist Operacional

### Para novo banco de cliente:
- [ ] Banco PostgreSQL criado
- [ ] Script de tabelas executado
- [ ] `GRANT` para `sistema` executado (Passo 3)
- [ ] Registro em `EmpresaCliente` com `StringConexao` no formato Npgsql (Passo 4)
- [ ] Registro em `Empresa` do banco do cliente com CNPJ correto (Passo 5)
- [ ] Primeiro login do OWNER realizado com sucesso
- [ ] OWNER trocou a senha padrão `12345`

### Para migração de banco legado:
- [ ] Verificar registros com `LoginUsuario` numérico e corrigir (seção 10.1)
- [ ] Verificar `StringConexao` em formato Npgsql (seção 10.3)
- [ ] Verificar `GRANT` para `sistema` (seção 10.2)
- [ ] Verificar tabela `Empresa` com CNPJ correto (seção 10.4)

---

**Documento atualizado por Qoder — 25/04/2026**  
**Versão 2.0 — Revisão completa com todos os fluxos, modelos de segurança e exemplos práticos**
