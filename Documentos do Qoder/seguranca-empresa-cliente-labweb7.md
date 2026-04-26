# Análise de Segurança — Tabela `EmpresaCliente` (LABWEB7Empresas)

**Data:** 25/04/2026  
**Autor:** Qoder AI  
**Escopo:** Sugestões e possibilidades para proteger a tabela `EmpresaCliente` e o banco `LABWEB7Empresas` contra acesso não autorizado, garantindo que somente o desenvolvedor do sistema possa modificá-la, que apenas o projeto LABWEB7 tenha visibilidade, e que acessos fora do Brasil sejam bloqueados.  
**Status:** Documento de análise — **nenhuma alteração de código foi realizada**.

---

## 0. Contexto: A Tabela `EmpresaCliente`

A tabela `EmpresaCliente` no banco `LABWEB7Empresas` é a **única tabela do sistema que exige preenchimento manual**. 
Ela contém dados críticos para o funcionamento de todo o sistema multi-tenant:

| Campo            | Tipo PostgreSQL             | Descrição                                       | Sensibilidade                   |
|------------------|-----------------------------|-------------------------------------------------|---------------------------------|
| `Id`             | integer                     | PK auto-incremento                              | Baixa                           |
| `CNPJ`           | character varying(14)       | CNPJ da empresa (14 dígitos,<br>sem formatação) | **Alta** — identifica cliente   |
| `Email`          | character varying(500)      | E-mail do OWNER/ADM da empresa                  | **Alta** — roteamento login     |
| `StringConexao`  | character varying(4000)     | Connection string do banco<br>do cliente        | **Crítica** — expõe credenciais |
| `LimiteUsuarios` | integer                     | Limite de usuários simultâneos                  | **Média** — controle licença    |
| `DataExpira`     | timestamp without time zone | Data de expiração da licença                    | **Média** — controle licença    |
| `DataCadastro`   | timestamp without time zone | Data/hora de cadastro do registro               | Baixa — auditoria               |

**Dados atuais na tabela (confirmação visual via pgAdmin):**

| Id | CNPJ           | Email                        | StringConexao                       | Limite | DataExpira | DataCadastro     |
|----|----------------|------------------------------|-------------------------------------|--------|------------|------------------|
| 1  | 00000000000100 | rguilemond@gmail.com         | Host=...;Database=LABWEB7;...       | 1      | 2026-10-05 | 2025-10-05 12:38 |
| 2  | 02557289000170 | ricardoguilemond@outlook.com | Host=...;Database=LABWEB7Barros;... | 2      | 2026-10-05 | 2025-10-05 12:38 |

> ⚠️ **Observação:** O campo `StringConexao` contém credenciais PostgreSQL em **texto plano** (Username + Password visíveis). 
Isso é o ativo mais sensível de todo o sistema.

**Tabelas relacionadas em `LABWEB7Empresas`:**
- `Emails` — Cache de roteamento e-mail → empresa (exposto ao login)
- `EmpresaLogin{CNPJ}` — Perfis de controle de acesso por empresa

### Por que esta tabela é o "coração" do sistema?

1. **Roteamento de Login:** Sem ela, é impossível saber qual banco de dados pertence a cada empresa
2. **Credenciais Expostas:** O campo `StringConexao` contém `Username` e `Password` do PostgreSQL
3. **Licenciamento:** `LimiteUsuarios` e `DataExpira` controlam o acesso comercial ao sistema
4. **Sem Backup Adequado:** Se esta tabela for corrompida ou deletada, **TODOS os clientes perdem acesso**

---

## 1. Requisito: Somente o Desenvolvedor Pode Modificar a Tabela

### 1.1 Situação Atual

- Qualquer usuário autenticado com `Administrador = 1` na tabela `Senhas` do banco do cliente **não tem acesso** ao banco `LABWEB7Empresas` — este é acessado apenas durante o fluxo de login via `EmpresaClienteRepository`
- A tabela `EmpresaCliente` **não tem interface CRUD** no sistema web — é preenchida manualmente via SQL no pgAdmin
- O usuário PostgreSQL `sistema` (usado pela aplicação) tem permissão total sobre a tabela

### 1.2 Problemas Identificados

| # | Problema                                                                                                      | Severidade  |
|---|---------------------------------------------------------------------------------------------------------------|-------------|
| 1 | O email do desenvolvedor está **hardcoded** no código (verificações manuais)                                  | Média       |
| 2 | O usuário PostgreSQL `sistema` tem `GRANT ALL` — qualquer código que use esta conexão pode modificar a tabela | **Crítica** |
| 3 | Não há **auditoria** de modificações na tabela `EmpresaCliente`                                               | Alta        |
| 4 | Não há **triggers** no PostgreSQL para proteger contra modificações diretas                                   | Alta        |
| 5 | Se o desenvolvedor mudar de email, não há mecanismo configurável                                              | Média       |

### 1.3 Sugestões

#### 1.3.1 Criar Seção de Configuração do Super-Admin no `appsettings.json`

**Conceito:** Em vez de hardcoded, o email do desenvolvedor fica em configuração.

```json
// appsettings.json (ou appsettings.Production.json)
"SuperAdmin": {
  "Email": "rguilemond@gmail.com",
  "Nome": "Ricardo Guilemond",
  "Descricao": "Desenvolvedor do sistema — único autorizado a modificar EmpresaCliente"
}
```

**Vantagens:**
- Se o email mudar, basta alterar o `appsettings.json` (sem recompilar)
- Pode ser externalizado para Azure Key Vault em produção
- Permite validação programática em tempo de execução

**Implementação sugerida:**
```csharp
// Classe de configuração fortemente tipada
public class SuperAdminSettings
{
    public string Email { get; set; } = "";
    public string Nome { get; set; } = "";
}
```

```csharp
// Em Startup.cs → ConfigureServices:
services.Configure<SuperAdminSettings>(Configuration.GetSection("SuperAdmin"));
```

#### 1.3.2 Implementar Row-Level Security (RLS) no PostgreSQL

**Conceito:** Criar uma política no PostgreSQL que **bloqueia INSERT/UPDATE/DELETE** na tabela `EmpresaCliente` para o usuário `sistema`, permitindo apenas SELECT. Criar um usuário PostgreSQL dedicado (`superadmin`) para modificações.

```sql
-- Passo 1: Revogar permissões de modificação do usuário "sistema"
REVOKE INSERT, UPDATE, DELETE ON "EmpresaCliente" FROM sistema;

-- Passo 2: Criar um usuário PostgreSQL dedicado para o desenvolvedor
CREATE ROLE superadmin WITH LOGIN PASSWORD 'SenhaForteAleatoria!2026';

-- Passo 3: Conceder permissões totais apenas ao superadmin
GRANT SELECT, INSERT, UPDATE, DELETE ON "EmpresaCliente" TO superadmin;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO superadmin;

-- Passo 4: (Opcional) RLS para visibilidade
ALTER TABLE "EmpresaCliente" ENABLE ROW LEVEL SECURITY;

-- O usuário "sistema" pode apenas LER (para login), não modificar:
CREATE POLICY "EmpresaCliente_ReadOnly_sistema" ON "EmpresaCliente"
  FOR SELECT TO sistema
  USING (true);

-- O superadmin tem acesso total:
CREATE POLICY "EmpresaCliente_FullAccess_superadmin" ON "EmpresaCliente"
  FOR ALL TO superadmin
  USING (true)
  WITH CHECK (true);
```

**Vantagens:**
- Proteção em **nível de banco de dados** — mesmo que o código seja comprometido, o `sistema` não pode modificar a tabela
- O desenvolvedor usa o `superadmin` no pgAdmin para manutenção
- Conforme com o princípio de menor privilégio

**Desvantagens:**
- Requer conexão separada para manutenção (não pela aplicação web)
- O `superadmin` deve ter senha forte e ser armazenado com segurança

#### 1.3.3 Criar Trigger de Auditoria no PostgreSQL

**Conceito:** Registrar TODA modificação na tabela `EmpresaCliente` em uma tabela de log.

```sql
-- Tabela de auditoria
CREATE TABLE "EmpresaCliente_Auditoria" (
  "Id" serial PRIMARY KEY,
  "Operacao" varchar(10) NOT NULL,  -- INSERT, UPDATE, DELETE
  "RegistroId" int NOT NULL,
  "DadosAnteriores" jsonb,
  "DadosNovos" jsonb,
  "UsuarioPostgreSQL" varchar(100) NOT NULL,
  "IpOrigem" inet,
  "DataOperacao" timestamp NOT NULL DEFAULT now()
);

-- Trigger function
CREATE OR REPLACE FUNCTION audit_empresa_cliente()
RETURNS trigger AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    INSERT INTO "EmpresaCliente_Auditoria" ("Operacao", "RegistroId", "DadosNovos", "UsuarioPostgreSQL")
    VALUES ('INSERT', NEW."Id", to_jsonb(NEW), current_user);
    RETURN NEW;
  ELSIF TG_OP = 'UPDATE' THEN
    INSERT INTO "EmpresaCliente_Auditoria" ("Operacao", "RegistroId", "DadosAnteriores", "DadosNovos", "UsuarioPostgreSQL")
    VALUES ('UPDATE', NEW."Id", to_jsonb(OLD), to_jsonb(NEW), current_user);
    RETURN NEW;
  ELSIF TG_OP = 'DELETE' THEN
    INSERT INTO "EmpresaCliente_Auditoria" ("Operacao", "RegistroId", "DadosAnteriores", "UsuarioPostgreSQL")
    VALUES ('DELETE', OLD."Id", to_jsonb(OLD), current_user);
    RETURN OLD;
  END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Trigger
CREATE TRIGGER trg_audit_empresa_cliente
  AFTER INSERT OR UPDATE OR DELETE ON "EmpresaCliente"
  FOR EACH ROW EXECUTE FUNCTION audit_empresa_cliente();
```

**Vantagens:**
- Rastro completo de quem modificou, quando e o que foi alterado
- Funciona mesmo para modificações diretas via pgAdmin
- Permite recuperação forense em caso de modificação indevida

#### 1.3.4 Criar Trigger de Proteção (Guard) no PostgreSQL

**Conceito:** Impedir modificações fora de horário comercial ou por usuários não autorizados diretamente no banco.

```sql
-- Exemplo: Somente permitir modificações pelo usuário "superadmin"
CREATE OR REPLACE FUNCTION guard_empresa_cliente()
RETURNS trigger AS $$
BEGIN
  IF current_user <> 'superadmin' THEN
    RAISE EXCEPTION 'Acesso negado: somente o superadmin pode modificar EmpresaCliente. Tentativa por: %', current_user;
  END IF;
  RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

CREATE TRIGGER trg_guard_empresa_cliente
  BEFORE INSERT OR UPDATE OR DELETE ON "EmpresaCliente"
  FOR EACH ROW EXECUTE FUNCTION guard_empresa_cliente();
```

**Vantagens:**
- Última linha de defesa — funciona mesmo que as permissões GRANT sejam alteradas acidentalmente
- Registra o usuário que tentou a modificação (no log do PostgreSQL)

#### 1.3.5 Interface Administrativa Protegida (Opcional — Futuro)

**Conceito:** Criar uma interface web para manutenção da tabela `EmpresaCliente`, acessível **apenas** pelo Super-Admin.

**Requisitos:**
- Rota dedicada: `/SuperAdmin/EmpresaCliente`
- Verificação de email logado contra `SuperAdmin:Email` do `appsettings.json`
- MFA (Multi-Factor Authentication) obrigatório
- Sessão com timeout curto (5 minutos de inatividade)
- Log de todas as operações

**Não recomendado agora** — o preenchimento manual via pgAdmin é mais seguro por enquanto, pois não cria superfície de ataque web.

---

## 2. Requisito: Somente o Projeto LABWEB7 Pode Enxergar Esta Tabela

### 2.1 Situação Atual

- O banco `LABWEB7Empresas` é acessado pela aplicação usando `PSQLConnectionStringEmpresas` do `appsettings.json`
- O usuário PostgreSQL `sistema` tem acesso a **todas** as tabelas do banco
- ~~Não há nenhuma proteção de rede~~ — ✅ **CORRIGIDO 21/04/2026:** `pg_hba.conf` restritivo e `listen_addresses = 'localhost'` aplicados
- As tabelas `Emails` e `EmpresaLogin{CNPJ}` também estão no mesmo banco
- A `StringConexao` (com credenciais) é retornada pelo `EmpresaClienteRepository` durante o login

### 2.2 Problemas Identificados

| # | Problema                                                                | Severidade          | Status            |
|---|-------------------------------------------------------------------------|---------------------|-------------------|
| 1 | O PostgreSQL aceita conexões de qualquer IP (padrão `pg_hba.conf`)      | ✅ **SEM PROBLEMA**| ✅ CORRIGIDO      |
| 2 | O usuário `sistema` pode ser usado por q.q app que saiba as credenciais | **Crítica**         | ⏳ Pendente (RLS/superadmin)|
| 3 | A `StringConexao` trafega pelo `EmpresaClienteRepository` em texto plano| Alta                | ⏳ Pendente (AES) |
| 4 | Não há separação de esquema entre dados de roteamento e dados de login  | Média               | ⏳ Pendente       |
| 5 | O `DevSenhasReset` em modo DEBUG lê `EmpresaCliente` diretamente        | Média               | ⏳ Pendente       |
| 6 | O `EmpresaClienteRepository` constrói SQL com concatenação (Injection)  | ✅ **SEM PROBLEMA** | ✅ CORRIGIDO     |

### 2.3 Sugestões

#### 2.3.1 Configurar `pg_hba.conf` para Restringir Acesso ao Banco

**Conceito:** Limitar quais IPs/host podem se conectar ao PostgreSQL, e a quais bancos.

```conf
# pg_hba.conf — Configuração restritiva para LABWEB7Empresas

# TYPE  DATABASE            USER        ADDRESS         METHOD

# --- Acesso ao banco LABWEB7Empresas ---
# Somente o servidor da aplicação (localhost ou IP do servidor web)
host    LABWEB7Empresas     sistema     127.0.0.1/32     md5
host    LABWEB7Empresas     sistema     ::1/128          md5

# Super-admin: acesso de IPs específicos (casa, escritório, VPN)
host    LABWEB7Empresas     superadmin  192.168.1.0/24   md5
host    LABWEB7Empresas     superadmin  <IP_CASA>/32     md5

# --- Acesso aos bancos de clientes ---
# Somente o servidor da aplicação
host    LABWEB7             sistema     127.0.0.1/32    md5
host    LABWEB7Barros       sistema     127.0.0.1/32    md5
host    LABWEB7%            sistema     127.0.0.1/32    md5

# --- Bloquear todo o resto ---
# Qualquer outra tentativa de conexão é rejeitada
host    all                 all         0.0.0.0/0       reject
```

**Vantagens:**
- Proteção em nível de rede — mesmo que as credenciais vazem, só IPs autorizados podem conectar
- Simples de implementar
- Nativo do PostgreSQL

#### 2.3.2 Criar Esquema Dedicado para Dados Sensíveis no PostgreSQL

**Conceito:** Separar a tabela `EmpresaCliente` em um esquema (`schema`) com permissões diferenciadas.

```sql
-- Criar esquema dedicado
CREATE SCHEMA admin;

-- Mover tabela para o esquema admin
ALTER TABLE "EmpresaCliente" SET SCHEMA admin;

-- Revogar acesso do "sistema" ao esquema admin
REVOKE ALL ON SCHEMA admin FROM sistema;

-- Criar função (role) intermediária para SELECT apenas
CREATE ROLE empresa_reader WITH LOGIN PASSWORD 'SenhaReader!2026';
GRANT USAGE ON SCHEMA admin TO empresa_reader;
GRANT SELECT ON admin."EmpresaCliente" TO empresa_reader;

-- O "sistema" usa a aplicação com "empresa_reader" para acessar apenas SELECT
-- durante o fluxo de login
```

**Ajuste na aplicação:**
```json
// appsettings.json — Connection string para leitura (login)
"ConexaoPostgreSQL": {
  "PSQLConnectionStringEmpresas": "Host=...;Database=LABWEB7Empresas;Username=empresa_reader;Password=...;SSL Mode=prefer;"
}
```

**Vantagens:**
- Separação clara de responsabilidades (SoC)
- O usuário da aplicação web nunca pode modificar `EmpresaCliente`
- Mesmo que um atacante comprometa a aplicação, não pode alterar a tabela

**Desvantagens:**
- Requer migração de esquema
- A query em `EmpresaClienteRepository` precisa ser ajustada para incluir o esquema: `admin."EmpresaCliente"`

#### 2.3.3 Criptografar o Campo `StringConexao` no Banco de Dados

**Conceito:** Armazenar a connection string criptografada (AES-256) em vez de texto plano.

**Estado atual:**
```
StringConexao = 'Host=GUILEMOND-ACER;Port=5432;Database=LABWEB7Barros;Username=sistema;Password=Acer@105;SSL Mode=prefer;'
```

**Estado proposto:**
```
StringConexao = 'ENC:AES256:WklJbmRERnRVRkJXY...='  (texto criptografado)
```

**Implementação sugerida:**
```csharp
// Serviço de criptografia para StringConexao
public class ConnectionStringProtector
{
    private readonly byte[] _key;  // Do appsettings.json → Key Vault
    private readonly byte[] _iv;   // Do appsettings.json → Key Vault

    public string Encrypt(string connectionString) { /* AES-256 */ }
    public string Decrypt(string encryptedConnectionString) { /* AES-256 */ }
}
```

**Ajuste no `EmpresaClienteRepository`:**
```csharp
// Ao ler do banco:
cliente.StringConexao = _protector.Decrypt(reader["StringConexao"].ToString());

// Ao gravar (somente superadmin):
string encrypted = _protector.Encryption(connectionStringNova);
```

**Vantagens:**
- Mesmo que alguém tenha acesso ao banco, não pode ler as connection strings
- Alinhado com LGPD (criptografia de dados sensíveis em repouso)

**Desvantagens:**
- Adiciona complexidade ao fluxo de login (descriptografia em cada acesso)
- Se a chave de criptografia for perdida, as connection strings ficam irrecuperáveis
- Manutenção de duas versões (criptografada e não) durante a migração

#### 2.3.4 Corrigir SQL Injection no `EmpresaClienteRepository`

**Problema atual (linha 27-33):**
```csharp
// VULNERÁVEL a SQL Injection:
SQL = "SELECT TOP 1 * FROM Emails WHERE Email = '" + emailCliente + "'";
SQL = "SELECT * FROM \"Emails\" WHERE \"Email\" = '" + emailCliente + "' LIMIT 1";
```

**Correção sugerida:**
```csharp
// SEGURO — usando parâmetros:
SQL = "SELECT * FROM \"Emails\" WHERE \"Email\" = @Email LIMIT 1";
comando.Parameters.AddWithValue("@Email", emailCliente);
```

> ✅ **CORRIGIDO (25/04/2026):** O SQL Injection foi eliminado. O `EmpresaClienteRepository` agora usa parâmetros `NpgsqlParameter[]` em todos os métodos. Este item não representa mais um risco.

#### 2.3.5 Remover Acesso Externo ao PostgreSQL

**Conceito:** Garantir que o PostgreSQL **nunca** seja exposto à internet.

**Ações:**
1. **Firewall do servidor:** Bloquear porta 5432 para acesso externo
2. **`postgresql.conf`:** `listen_addresses = 'localhost'` (se aplicação no mesmo servidor)
3. **Se a aplicação estiver em servidor diferente:** Usar VPN ou SSH tunnel
4. **Cloud:** Usar Security Groups / VPC para restringir acesso

```conf
# postgresql.conf
listen_addresses = 'localhost'   # ou IP interno da VPC
port = 5432
ssl = on
ssl_cert_file = '/etc/ssl/certs/server.crt'
ssl_key_file = '/etc/ssl/private/server.key'
```

#### 2.3.6 Ocultar Tabelas Internas de Consultas por Parte do Cliente

**Conceito:** Garantir que os bancos de clientes (`LABWEB7`, `LABWEB7Barros`, etc.) **não possam** acessar o banco `LABWEB7Empresas`.

**Ação:**
- O usuário PostgreSQL que conecta ao banco do cliente **deve ser diferente** do usuário que acessa `LABWEB7Empresas`
- Ou usar o mesmo usuário `sistema`, mas com `pg_hba.conf` restringindo quais bancos cada conexão pode acessar

```sql
-- No banco do cliente (e.g., LABWEB7Barros):
-- Revogar qualquer acesso ao banco LABWEB7Empresas
REVOKE ALL ON DATABASE "LABWEB7Empresas" FROM PUBLIC;
```

---

## 3. Requisito: Acesso Somente por IPs do Brasil

### 3.1 Situação Atual

- ~~O PostgreSQL aceita conexões de **qualquer IP**~~ — ✅ **CORRIGIDO 21/04/2026:** `pg_hba.conf` restritivo + `listen_addresses = 'localhost'`
- Não há nenhum middleware de geolocalização na aplicação
- Não há regras de firewall baseadas em geolocalização
- ~~O `AllowedHosts` no `appsettings.json` está como `"*"`~~ — ✅ **CORRIGIDO 25/04/2026:** `AllowedHosts` corrigido para `localhost;localhost:5000;localhost:5001;localhost:56013`

### 3.2 Camadas de Proteção Possíveis

A proteção por geolocalização pode ser implementada em **múltiplas camadas**. Cada camada oferece um nível diferente de segurança e complexidade:

```
+--------------------------------------------------+
| CAMADA 1: DNS/CDN (Cloudflare, AWS Route 53)     |  ← Bloqueio antes de chegar ao servidor
+--------------------------------------------------+
| CAMADA 2: Firewall do Servidor (iptables/UFW)    |  ← Bloqueio no nível de rede
+--------------------------------------------------+
| CAMADA 3: PostgreSQL (pg_hba.conf)               |  ← Bloqueio no nível do banco
+--------------------------------------------------+
| CAMADA 4: Middleware ASP.NET Core                |  ← Bloqueio no nível da aplicação
+--------------------------------------------------+
| CAMADA 5: Rate Limiting + Fail2Ban               |  ← Proteção contra abuso
+--------------------------------------------------+
```

### 3.3 Sugestões por Camada

#### 3.3.1 Camada 1 — Bloqueio GeoIP via CDN/Cloudflare (RECOMENDADO)

**Conceito:** Usar o Cloudflare (ou AWS CloudFront) como proxy reverso com regras de geolocalização.

**Cloudflare (Plano Gratuito):**
- Regras de firewall baseadas em país
- Bloqueio de todos os países exceto Brasil
- Proteção DDoS inclusa
- Certificado SSL/TLS inclusa

**Configuração no Cloudflare:**
```
Regra de Firewall:
  Se: [País do visitante] ≠ [Brasil]
  Então: [Bloquear]

Regra adicional:
  Se: [País do visitante] = [Brasil] E [URI Path] contém "/SuperAdmin"
  Então: [Desafio Managed] (reCAPTCHA)
```

**Vantagens:**
- **Mais efetivo** — o tráfego malicioso nunca chega ao servidor
- Sem modificação de código
- Proteção DDoS inclusa
- Log de tentativas de acesso bloqueadas
- Plano gratuito do Cloudflare já oferece essa funcionalidade

**Desvantagens:**
- Requer configuração de DNS apontando para o Cloudflare
- Pode adicionar latência mínima (~10ms)
- Se o Cloudflare for bypassado (IP direto do servidor), a proteção é contornada

**Mitigação:** Combinar com Camada 2 (firewall do servidor aceita apenas IPs do Cloudflare)

#### 3.3.2 Camada 2 — Firewall do Servidor (iptables/UFW no Linux)

**Conceito:** No servidor Linux que hospeda o PostgreSQL, configurar regras de firewall para aceitar conexões apenas de faixas de IP brasileiras.

**Abordagem A — Whitelist de IPs brasileiros (complexo):**
```bash
# Baixar lista de IPs brasileiros (atualizada mensalmente)
# Fonte: https://www.ipdeny.com/ipblocks/ ou https://ip2location.com
wget -O /tmp/br.zone https://www.ipdeny.com/ipblocks/data/countries/br.zone

# Aplicar regras iptables
while read ip_range; do
  iptables -A INPUT -p tcp --dport 443 -s $ip_range -j ACCEPT
done < /tmp/br.zone

# Bloquear todo o resto
iptables -A INPUT -p tcp --dport 443 -j DROP
```

**Abordagem B — Cloudflare-only (mais simples):**
```bash
# Aceitar conexões HTTPS APENAS dos IPs do Cloudflare
# Lista atualizada: https://www.cloudflare.com/ips/
for ip in $(curl -s https://www.cloudflare.com/ips-v4); do
  iptables -A INPUT -p tcp --dport 443 -s $ip -j ACCEPT
done
iptables -A INPUT -p tcp --dport 443 -j DROP
```

**Vantagens:**
- Proteção no nível do SO — não depende da aplicação
- Difícil de contornar sem acesso ao servidor

**Desvantagens:**
- Manutenção da lista de IPs (atualizações mensais)
- No Windows Server: usar Windows Defender Firewall com regras similares

#### 3.3.3 Camada 3 — PostgreSQL `pg_hba.conf`

**Conceito:** Restringir quais IPs podem se conectar ao PostgreSQL.

**Para o banco LABWEB7Empresas:**
```conf
# pg_hba.conf — Acesso ao banco central de empresas
# Somente localhost (aplicação no mesmo servidor)
host    LABWEB7Empresas     sistema        127.0.0.1/32    md5
host    LABWEB7Empresas     empresa_reader 127.0.0.1/32    md5
host    LABWEB7Empresas     superadmin     127.0.0.1/32    md5

# Se a aplicação estiver em servidor diferente, usar IP específico
host    LABWEB7Empresas     sistema        10.0.0.0/8      md5    # rede interna apenas
```

**Para bancos de clientes:**
```conf
# Somente a aplicação pode acessar os bancos de clientes
host    LABWEB7%            sistema        127.0.0.1/32    md5
```

**Vantagens:**
- Simples e nativo do PostgreSQL
- Proteção no nível do banco

**Desvantagens:**
- Não faz geolocalização — apenas filtragem por IP/rede
- Precisa ser combinado com Camada 1 ou 2 para proteção baseada em país

#### 3.3.4 Camada 4 — Middleware ASP.NET Core (GeoIP)

**Conceito:** Criar um middleware que verifica o IP do visitante e bloqueia acessos fora do Brasil.

**Opção A — Usar API de GeoIP (ex: ip-api.com):**
```csharp
public class GeoIpMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GeoIpMiddleware> _logger;
    
    // Faixas de IP brasileiras conhecidas (simplificado)
    private static readonly string[] BrazilianIpRanges = new[]
    {
        "177.", "179.", "187.", "189.", "190.",     // Exemplos — lista real é muito maior
        "191.", "200.", "201.", "186.", "177."
    };

    public GeoIpMiddleware(RequestDelegate next, ILogger<GeoIpMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        
        if (!string.IsNullOrEmpty(remoteIp))
        {
            // Ignorar localhost (desenvolvimento)
            if (remoteIp is "127.0.0.1" or "::1")
            {
                await _next(context);
                return;
            }

            // Verificar se o IP é brasileiro
            if (!IsBrazilianIp(remoteIp))
            {
                _logger.LogWarning("Acesso bloqueado — IP fora do Brasil: {Ip}", remoteIp);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Acesso não autorizado nesta região.");
                return;
            }
        }

        await _next(context);
    }

    private bool IsBrazilianIp(string ip)
    {
        // Opção 1: Verificar prefixos (simplificado, não 100% preciso)
        // Opção 2: Usar banco de dados GeoIP (MaxMind GeoLite2)
        // Opção 3: Usar API externa (ip-api.com)
        return BrazilianIpRanges.Any(prefix => ip.StartsWith(prefix));
    }
}
```

**Opção B — Usar MaxMind GeoLite2 (mais preciso):**
```csharp
// Instalar pacote NuGet: MaxMind.GeoIP2
public class GeoIpMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DatabaseReader _geoIpReader;

    public GeoIpMiddleware(RequestDelegate _next, IWebHostEnvironment env)
    {
        this._next = _next;
        // Baixar GeoLite2-Country.mmdb de https://dev.maxmind.com/geoip/geolite2-free-geolocation-data
        _geoIpReader = new DatabaseReader(Path.Combine(env.ContentRootPath, "GeoLite2-Country.mmdb"));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        
        if (remoteIp != null && !IPAddress.IsLoopback(remoteIp))
        {
            if (_geoIpReader.TryCountry(remoteIp, out var country))
            {
                if (country?.IsoCode != "BR")
                {
                    context.Response.StatusCode = 403;
                    return;
                }
            }
        }

        await _next(context);
    }
}
```

**Configuração no `Startup.cs`:**
```csharp
// ANTES de UseAuthentication e UseAuthorization
app.UseMiddleware<GeoIpMiddleware>();
```

**Vantagens:**
- Funciona no nível da aplicação
- Pode ser configurado com rotas específicas (ex: bloquear apenas `/SuperAdmin`)
- Log de tentativas de acesso
- Banco de dados GeoLite2 é **gratuito** (requer cadastro na MaxMind)

**Desvantagens:**
- Adiciona latência em cada requisição (verificação de IP)
- Se o Cloudflare estiver na frente, precisa ler o header `CF-IPCountry` em vez do IP remoto
- Banco GeoLite2 precisa ser atualizado mensalmente

#### 3.3.5 Camada 5 — Rate Limiting + Fail2Ban

**Conceito:** Limitar tentativas de login e bloquear IPs que tentam acessar indevidamente.

**Rate Limiting no ASP.NET Core (.NET 7+):**
```csharp
// Startup.cs → ConfigureServices
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.PermitLimit = 5;          // 5 tentativas
        opt.Window = TimeSpan.FromMinutes(15);  // por 15 minutos
        opt.QueueLimit = 0;           // sem fila
    });
});

// Startup.cs → Configure
app.UseRateLimiter();

// No controller de login:
[EnableRateLimiting("LoginPolicy")]
public async Task<IActionResult> Login(LoginViewModel model) { ... }
```

**Fail2Ban (Linux):**
```ini
# /etc/fail2ban/jail.d/labweb7.conf
[labweb7-login]
enabled = true
port = 443
filter = labweb7
logpath = /var/log/nginx/access.log
maxretry = 5
findtime = 600
bantime = 3600
```

---

## 4. Matriz de Decisão — O Que Implementar

### 4.1 Priorização por Esforço x Impacto

| #  | Sugestão                                           | Esforço    | Impacto  | Prioridade  | Viabilidade |
|----|----------------------------------------------------|------------|----------|-------------|-------------|
| 1  | Corrigir SQL Injection no<br>`EmpresaClienteRepo`  | Baixo (1h) | ✅ SEM PROBLEMA | ✅ FEITO 25/04/2026 | 100%        |
| 2  | Configurar `pg_hba.conf` restritivo                | Baixo (2h) | ✅ SEM PROBLEMA | ✅ FEITO 21/04/2026 | 100%        |
| 3  | Cloudflare com bloqueio GeoIP                      | Baixo (4h) | Alto     | CURTO PRAZO | 95%         |
| 4  | Seção `SuperAdmin` no `appsettings.json`           | Baixo (2h) | Médio    | CURTO PRAZO | 100%        |
| 5  | Trigger de auditoria no PostgreSQL                 | Médio (4h) | Alto     | CURTO PRAZO | 100%        |
| 6  | Revogar GRANT INSERT/UPDATE/DELETE<br>do `sistema` | Médio (4h) | Alto     | CURTO PRAZO | 100%        |
| 7  | Criptografar `StringConexao` no banco              | Médio (8h) | Alto     | MÉDIO PRAZO | 90%         |
| 8  | Middleware GeoIP (MaxMind)                         | Médio (8h) | Médio    | MÉDIO PRAZO | 85%         |
| 9  | Trigger de proteção (guard)<br>no PostgreSQL       | Baixo (2h) | Médio    | MÉDIO PRAZO | 100%        |
| 10 | Esquema dedicado (`admin`)                         | Alto (16h) | Alto     | LONGO PRAZO | 80%         |
| 11 | Interface web de Super-Admin                       | Alto (40h) | Médio    | LONGO PRAZO | 60%         |
| 12 | Rate Limiting                                      | Médio (4h) | Médio    | MÉDIO PRAZO | 100%        |
| 13 | Firewall do servidor (iptables)                    | Médio (4h) | Alto     | CURTO PRAZO | 90%         |

### 4.2 Roadmap Sugerido

```
SEMANA 1 (IMEDIATO):
├── 1. Corrigir SQL Injection no EmpresaClienteRepository
├── 2. Configurar pg_hba.conf restritivo (localhost apenas)
└── 3. Configurar postgresql.conf (listen_addresses = 'localhost')

SEMANA 2-3 (CURTO PRAZO):
├── 4. Adicionar seção SuperAdmin no appsettings.json
├── 5. Criar trigger de auditoria no PostgreSQL
├── 6. Revogar GRANT de modificação do usuário "sistema"
├── 7. Configurar Cloudflare com bloqueio GeoIP (Brasil)
└── 8. Firewall do servidor (iptables/Windows Firewall)

MÊS 2 (MÉDIO PRAZO):
├── 9. Criptografar campo StringConexao no banco
├── 10. Implementar middleware GeoIP (MaxMind)
├── 11. Implementar Rate Limiting no login
└── 12. Trigger de proteção (guard) no PostgreSQL

MÊS 3+ (LONGO PRAZO):
├── 13. Migrar EmpresaCliente para esquema dedicado (admin)
└── 14. (Opcional) Interface web de Super-Admin com MFA
```

---

## 5. Considerações Importantes

### 5.1 Sobre a Mudança de Email do Desenvolvedor

O requisito menciona que "se um dia eu mudar o email, isso deve ser considerado". As abordagens possíveis são:

| Abordagem                                                      | Descrição                    | Vantagens                | Desvantagens              |
|----------------------------------------------------------------|------------------------------|--------------------------|---------------------------|
| `appsettings.json`                                             | Email em configuração        | Simples, sem recompilar  | Precisa acessar servidor  |
| Azure Key Vault                                                | Email como segredo           | Criptografado, auditável | Custo, complexidade       |
| Variável de ambiente                                           | `SUPERADMIN_EMAIL=...`       | Sem arquivo              | Precisa reiniciar a app   |
| Tabela no banco                                                | Tabela `ConfiguracaoSistema` | Alterável via SQL        | Precisa acesso ao banco   |
| **Recomendado:** `appsettings.json`<br>+ Key Vault em produção | Config local + nuvem         | Flexível + seguro        | Configuração inicial      |

**Implementação recomendada:**
```json
// appsettings.Production.json
"SuperAdmin": {
  "Email": "rguilemond@gmail.com",  // Alterar quando necessário
  "EmailSecundario": "",             // Backup (opcional)
  "Descricao": "Desenvolvedor do sistema"
}
```

### 5.2 Sobre IPs Aprovados para Uso com LABWEB7

O requisito menciona "IPs que estão aprovados para uso com o Sistema LABWEB7". Isso sugere uma **whitelist de IPs** além da restrição por país:

```json
// appsettings.Production.json
"Seguranca": {
  "IpWhitelist": {
    "BrasilApenas": true,
    "IPsEspecificos": [
      "189.0.0.0/8",     // Faixa brasileira (exemplo)
      "177.0.0.0/8",     // Faixa brasileira (exemplo)
      "200.0.0.0/8"      // Faixa brasileira (exemplo)
    ],
    "IPsAvulsos": [
      "189.100.50.25",   // Escritório
      "179.200.100.30"   // Desenvolvedor (casa)
    ]
  }
}
```

**Nota:** A lista real de IPs brasileiros deve ser obtida de fontes como:
- [IPdeny](https://www.ipdeny.com/ipblocks/) — Listas gratuitas por país
- [MaxMind GeoLite2](https://dev.maxmind.com/geoip/geolite2-free-geolocation-data) — Banco de dados gratuito
- [RIPE NCC](https://www.ripe.net/) — Registro regional de IPs europeus (contém faixas brasileiras via LACNIC)
- [LACNIC](https://lacnic.net/) — Registro regional de IPs da América Latina e Caribe

### 5.3 Sobre VPN como Alternativa Complementar

Para acesso remoto seguro (manutenção, suporte), considere:

**Opção A — WireGuard VPN (recomendado):**
- Leve, moderno, fácil de configurar
- O desenvolvedor conecta via VPN e acessa o banco diretamente
- PostgreSQL aceita conexões apenas da VPN (10.0.0.0/24)

**Opção B — Tailscale:**
- Zero-config mesh VPN
- Gratuito para uso pessoal
- Funciona sobre NAT e firewalls

**Opção C — Cloudflare Tunnel:**
- Sem necessidade de abrir portas
- Acesso seguro ao banco via túnel criptografado
- Integrado com o Cloudflare (mesma plataforma do GeoIP)

### 5.4 Sobre o `AllowedHosts`

✅ **CORRIGIDO (25/04/2026):** O `AllowedHosts` foi configurado corretamente em ambos os `appsettings`:

```json
// appsettings.json e appsettings.Development.json
"AllowedHosts": "localhost;localhost:5000;localhost:5001;localhost:56013"
```

> **Importante:** Em produção, substituir pelos domínios reais:
```json
"AllowedHosts": "labweb7.com.br;www.labweb7.com.br"
```

> **Atenção arquitetural:** O `Program.cs` usa lógica própria de carga de configuração (ignora `appsettings.Development.json`). Qualquer configuração crítica deve estar no `appsettings.json`.

---

## 6. Resumo das Ações Recomendadas (Sem Modificação de Código)

> **Legenda de status:**
> - ✅ CORRIGIDO — aplicado no código/configuração
> - 📄 PENDENTE MANUAL — arquivo gerado, aguarda aplicação manual pelo desenvolvedor
> - ⏳ PENDENTE — ainda não iniciado

### Ações Imediatas (Configuração de Infraestrutura)

1. ✅ **CORRIGIDO** — **Configurar `pg_hba.conf`** para aceitar conexões ao `LABWEB7Empresas` apenas do servidor da aplicação
   > Aplicado diretamente em: `C:\Program Files\PostgreSQL\18\data\pg_hba.conf` em 21/04/2026
   > Regras adicionadas: `LABWEB7Empresas` restrito a `127.0.0.1/32` e `::1/128` com `scram-sha-256`
   > Vulnerabilidade removida: `fe80::/10 trust` (IPv6 link-local sem senha) — comentada
   > Corrigido: `postgres` adicionado para TCP localhost (pgAdmin)
   > Verificado em produção: PostgreSQL reiniciado e conectando normalmente
2. ✅ **CORRIGIDO** — **Configurar `postgresql.conf`** com `listen_addresses` restrito
   > Aplicado diretamente em: `C:\Program Files\PostgreSQL\18\data\postgresql.conf` em 21/04/2026
   > Linha 60: `listen_addresses = 'localhost'  # LABWEB7-SEGURANCA: era '*'`
   > Verificado: `SHOW listen_addresses;` retornou `localhost` ✅
3. ✅ **CORRIGIDO** — **Ativar SSL** no PostgreSQL (`ssl = on`)
   > `ssl = on` aplicado em `postgresql.conf` (linha 109) em 21/04/2026
   > `server.crt` gerado via .NET 8 (`System.Security.Cryptography`) — 1.191 bytes — válido 10 anos
   > `server.key` gerado via .NET 8 (RSA 2048, PEM sem senha) — 1.678 bytes
   > Ambos em: `C:\Program Files\PostgreSQL\18\data\`
   > Verificado: `SHOW ssl;` retornou `on` ✅
4. ✅ **CORRIGIDO** — **Alterar `AllowedHosts`** de `"*"` para os hosts reais
   > `appsettings.Development.json`: `"AllowedHosts": "localhost;localhost:5000;localhost:5001;localhost:56013"` — corrigido em 25/04/2026
   > `appsettings.json`: `"AllowedHosts": "localhost;localhost:5000;localhost:5001;localhost:56013"` — corrigido em 25/04/2026
   > **Em produção:** substituir pelos domínios reais (ex: `"labweb7.com.br;www.labweb7.com.br"`)
   > Causa raíz identificada: `Program.cs` usa lógica própria de carga — só lê `appsettings.json` e `appsettings.Windows.json`; o `appsettings.Development.json` **não é lido automaticamente**

### Ações de Curto Prazo (1-3 semanas)

5. ✅ **CORRIGIDO** — **SQL Injection** no `EmpresaClienteRepository` (usar parâmetros)
   > `RetornaSelectEmails` e `RetornaSelectEmpresaCliente` agora retornam `(string SQL, NpgsqlParameter[] Parametros)`
   > 3 pontos de uso corrigidos em `ValidacoesDeSenhas.cs` (linhas 107, 230 e 361)
   > `ObterEmpresaCliente` interno também corrigido
   > Build verificado: **0 erros**
6. **Adicionar `SuperAdmin`** no `appsettings.json`
7. **Criar trigger de auditoria** no PostgreSQL para `EmpresaCliente`
8. **Configurar Cloudflare** com bloqueio GeoIP (Brasil apenas)
9. **Revogar GRANT INSERT/UPDATE/DELETE** do usuário `sistema` na tabela `EmpresaCliente`
10. **Criar usuário `superadmin`** no PostgreSQL para manutenção

### Ações de Médio Prazo (1-3 meses)

11. **Criptografar campo `StringConexao`** no banco de dados
12. **Implementar middleware GeoIP** (MaxMind GeoLite2)
13. **Implementar Rate Limiting** no login
14. **Adicionar trigger de proteção** (guard) no PostgreSQL

### Ações de Longo Prazo (3-6 meses)

15. **Migrar `EmpresaCliente`** para esquema dedicado (`admin`)
16. **Mover connection strings** para Azure Key Vault ou equivalente
17. **Implementar MFA** para acesso administrativo
18. **Configurar VPN** (WireGuard/Tailscale) para acesso remoto seguro

---

## 7. Diagrama da Arquitetura de Segurança Proposta

```
                        INTERNET
                           |
                    +------+------+
                    | Cloudflare  |  ← Camada 1: GeoIP (Bloqueio por país)
                    | (CDN/Proxy) |
                    +------+------+
                           |
                    +------+------+
                    |   Nginx /   |  ← Camada 2: Proxy reverso + Rate Limiting
                    |   IIS       |
                    +------+------+
                           |
                    +------+------+
                    | ASP.NET Core|  ← Camada 4: Middleware GeoIP + Auth
                    | (LabWeb7)   |
                    +------+------+
                           |
              +------------+------------+
              |                         |
    +---------+---------+    +----------+---------+
    | LABWEB7Empresas    |    | LABWEB7Barros      |
    | (Banco Central)    |    | (Banco do Cliente) |
    +--------------------+    +---------------------+
    | Schema: admin      |    | Acesso: sistema     |
    |  - EmpresaCliente  |    |  (SELECT/INSERT/    |
    |  (somente SELECT   |    |   UPDATE/DELETE)    |
    |   para "sistema")  |    +---------------------+
    |  - Auditoria       |
    |  - Emails          |
    | Acesso:            |
    |  - sistema (SELECT)|
    |  - superadmin (ALL)|
    +--------------------+
              ↑
        pg_hba.conf:    ← Camada 3: IP filtering
        127.0.0.1/32 only
```

---

**Documento elaborado por:** Qoder AI  
**Data:** 25/04/2026  
**Versão:** 1.0  
**Status:** Análise — nenhuma alteração de código foi realizada  
**Próximo passo:** Decidir quais ações implementar e em que ordem
