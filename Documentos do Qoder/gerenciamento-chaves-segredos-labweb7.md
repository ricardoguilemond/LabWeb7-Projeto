# Gerenciamento de Chaves e Segredos - LabWeb7

**Data:** 24/04/2026
**Versão:** 1.0
**Projeto:** LabWebMvc.MVC (.NET 8)

---

## 1. Visão Geral

Este documento descreve a estratégia de gerenciamento de chaves, senhas e segredos do projeto LabWeb7, cobrindo desde o desenvolvimento local até a publicação em produção.

### Princípios Fundamentais

1. **NUNCA** colocar chaves reais no código-fonte (`.cs`)
2. **NUNCA** commitar segredos de produção no Git
3. Cada ambiente (desenvolvimento, homologação, produção) tem suas próprias chaves
4. O código-fonte deve funcionar sem chaves hardcoded - todas vêm de configuração

---

## 2. Chaves Gerenciadas

### 2.1 Chaves de Criptografia

| Chave | Seção no appsettings | Uso | Crítica? |
|-------|---------------------|-----|----------|
| `myVetorDeCifras` | `Secrets:myVetorDeCifras` | Vetor de cifras AES para dados internos | 🔴 SIM |
| `mySecretKeyGoogle` | `Secrets:mySecretKeyGoogle` | Chave pública Google reCAPTCHA v3 | 🟡 Médio |
| `mySecretKeyLEGADA` | `Secrets:mySecretKeyLEGADA` | Chave legada para terceiros | 🟡 Médio |
| `mySecretKeyPublic` | `Secrets:mySecretKeyPublic` | Chave SITE do reCAPTCHA (HTML) | 🟡 Médio |
| `mySecretKeyPrivate` | `Secrets:mySecretKeyPrivate` | Chave SECRETA do reCAPTCHA (backend) | 🔴 SIM |

### 2.2 URLs de Serviço

| Chave | Seção no appsettings | Uso |
|-------|---------------------|-----|
| `GatewayUrl` | `Secrets:GatewayUrl` | URL do serviço de integração |

### 2.3 Credenciais de Banco de Dados

| Chave | Seção no appsettings | Uso |
|-------|---------------------|-----|
| `PSQLConnectionStringEmpresas` | `ConexaoPostgreSQL` | String de conexão do banco de empresas |
| `PSQLConnectionString` | `ConexaoPostgreSQL` | String de conexão do banco do cliente |

### 2.4 Credenciais de Email

| Chave | Seção no appsettings | Uso |
|-------|---------------------|-----|
| `SmtpPassword` | `EmailConfiguration` | Senha do servidor SMTP |
| `SmtpSenhaApp` | `EmailConfiguration` | Senha de App do Google |
| `PopPassword` | `EmailConfiguration` | Senha do servidor POP/IMAP |

### 2.5 Credenciais Google reCAPTCHA

| Chave | Seção no appsettings | Uso |
|-------|---------------------|-----|
| `SecretKey` | `GoogleReCaptcha:SecretKey` | Chave secreta do reCAPTCHA |
| `ProjectId` | `GoogleReCaptcha:ProjectId` | ID do projeto Google Cloud |

---

## 3. Estratégia por Ambiente

### 3.1 Desenvolvimento Local (User Secrets)

O .NET Secret Manager armazena chaves **fora do projeto**, em:

```
%APPDATA%\Microsoft\UserSecrets\1bde491a-f64f-4d6b-8052-d8b97d86202e\secrets.json
```

O ID `1bde491a-...` está configurado no `LabWebMvc.MVC.csproj`:
```xml
<UserSecretsId>1bde491a-f64f-4d6b-8052-d8b97d86202e</UserSecretsId>
```

#### Comandos Básicos

```bash
# Listar todas as chaves
dotnet user-secrets list

# Definir uma chave
dotnet user-secrets set "Secrets:myVetorDeCifras" "valor_aqui"

# Remover uma chave
dotnet user-secrets remove "Secrets:myVetorDeCifras"

# Limpar todas as chaves
dotnet user-secrets clear
```

#### Configuração Inicial (primeira vez)

```bash
cd "f:\Projetos dotNet\Web-Project\LabWeb7-Projeto\LabWebMvc.MVC"

dotnet user-secrets set "Secrets:myVetorDeCifras" "Ara5SSan5Yan1966"
dotnet user-secrets set "Secrets:mySecretKeyGoogle" "6LdSfoErAAAAAOF9vDLSXPOUgGdKssnlLNLEFinL"
dotnet user-secrets set "Secrets:mySecretKeyLEGADA" "6LdSfoErAAAAABHPZ6IIjs94ZFlvc4EvNZEgubyN"
dotnet user-secrets set "Secrets:mySecretKeyPublic" "6Le_QQYiAAAAANuFhenHQ5DpfJCGIfa2X1O51ltB"
dotnet user-secrets set "Secrets:mySecretKeyPrivate" "6Le_QQYiAAAAAAF7jG4PZalVceazfJZnlbJVKodL"
dotnet user-secrets set "Secrets:GatewayUrl" "http://localhost:10000"
```

#### Editar Diretamente o Arquivo

```bash
notepad "%APPDATA%\Microsoft\UserSecrets\1bde491a-f64f-4d6b-8052-d8b97d86202e\secrets.json"
```

Conteúdo do arquivo `secrets.json`:
```json
{
  "Secrets:myVetorDeCifras": "Ara5SSan5Yan1966",
  "Secrets:mySecretKeyGoogle": "6LdSfoErAAAAAOF9vDLSXPOUgGdKssnlLNLEFinL",
  "Secrets:mySecretKeyLEGADA": "6LdSfoErAAAAABHPZ6IIjs94ZFlvc4EvNZEgubyN",
  "Secrets:mySecretKeyPublic": "6Le_QQYiAAAAANuFhenHQ5DpfJCGIfa2X1O51ltB",
  "Secrets:mySecretKeyPrivate": "6Le_QQYiAAAAAAF7jG4PZalVceazfJZnlbJVKodL",
  "Secrets:GatewayUrl": "http://localhost:10000"
}
```

### 3.2 Homologação (appsettings ou Variáveis de Ambiente)

#### Opção A: Arquivo de configuração específico

Criar `appsettings.Homologacao.json` (NÃO commitar no Git):

```json
{
  "Secrets": {
    "myVetorDeCifras": "valor_homologacao",
    "mySecretKeyGoogle": "chave_homologacao",
    "mySecretKeyLEGADA": "chave_homologacao",
    "mySecretKeyPublic": "chave_homologacao",
    "mySecretKeyPrivate": "chave_homologacao",
    "GatewayUrl": "http://servidor-homolog:10000"
  },
  "ConexaoPostgreSQL": {
    "PSQLConnectionStringEmpresas": "Host=servidor-homolog;...",
    "PSQLConnectionString": "Host=servidor-homolog;..."
  }
}
```

Adicionar ao `.gitignore`:
```
appsettings.Homologacao.json
```

#### Opção B: Variáveis de Ambiente

```bash
# Linux:
export Secrets__myVetorDeCifras="valor_homologacao"
export Secrets__mySecretKeyGoogle="chave_homologacao"
export Secrets__GatewayUrl="http://servidor-homolog:10000"

# Windows (PowerShell - nível máquina):
[System.Environment]::SetEnvironmentVariable("Secrets__myVetorDeCifras", "valor_homologacao", "Machine")
```

**Nota:** Variáveis de ambiente usam `__` (duplo underscore) no lugar de `:` (dois pontos).

### 3.3 Produção

#### Opção 1: Variáveis de Ambiente (VPS própria) - GRÁTIS

Para VPS Linux (Hetzner, Contabo, DigitalOcean, etc.):

**Configuração via systemd:**
```ini
# /etc/systemd/system/labweb7.service
[Unit]
Description=LabWeb7 Application
After=network.target

[Service]
WorkingDirectory=/var/www/labweb7
ExecStart=/usr/bin/dotnet LabWebMvc.MVC.dll
Environment=Secrets__myVetorDeCifras=valor_producao
Environment=Secrets__mySecretKeyGoogle=chave_producao
Environment=Secrets__mySecretKeyPrivate=chave_producao
Environment=Secrets__GatewayUrl=http://gateway:10000
Environment=ConexaoPostgreSQL__PSQLConnectionStringEmpresas=Host=prod-db;...
Environment=ASPNETCORE_ENVIRONMENT=Production
Restart=always

[Install]
WantedBy=multi-user.target
```

**Configuração via arquivo de ambiente:**
```bash
# /etc/labweb7/environment
Secrets__myVetorDeCifras=valor_producao
Secrets__mySecretKeyGoogle=chave_producao
Secrets__mySecretKeyPrivate=chave_producao
Secrets__GatewayUrl=http://gateway:10000
ConexaoPostgreSQL__PSQLConnectionStringEmpresas=Host=prod-db;...
```

**Segurança do servidor:**
```bash
# Proteger o arquivo de ambiente
sudo chmod 600 /etc/labweb7/environment
sudo chown labweb7:labweb7 /etc/labweb7/environment
```

#### Opção 2: Azure Key Vault - RECOMENDADO para Azure

**1. Criar o Key Vault:**
```bash
az keyvault create --name labweb7-vault --resource-group MeuGrupo --location brazilsouth
```

**2. Adicionar segredos:**
```bash
az keyvault secret set --vault-name labweb7-vault --name "Secrets--myVetorDeCifras" --value "valor_producao"
az keyvault secret set --vault-name labweb7-vault --name "Secrets--mySecretKeyGoogle" --value "chave_producao"
az keyvault secret set --vault-name labweb7-vault --name "Secrets--mySecretKeyPrivate" --value "chave_producao"
az keyvault secret set --vault-name labweb7-vault --name "Secrets--GatewayUrl" --value "http://gateway:10000"
az keyvault secret set --vault-name labweb7-vault --name "ConexaoPostgreSQL--PSQLConnectionStringEmpresas" --value "Host=prod-db;..."
```

**3. Configurar no `Program.cs`:**
```csharp
// Adicionar ANTES de builder.Build():
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddAzureKeyVault(
        new Uri("https://labweb7-vault.vault.azure.net/"),
        new DefaultAzureCredential());
}
```

**4. Instalar os pacotes:**
```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

**Custo:** ~R$ 3-10/mês

#### Opção 3: AWS Secrets Manager

**1. Criar o segredo:**
```bash
aws secretsmanager create-secret --name LabWeb7/Secrets --secret-string '{
  "myVetorDeCifras": "valor_producao",
  "mySecretKeyGoogle": "chave_producao",
  "mySecretKeyPrivate": "chave_producao",
  "GatewayUrl": "http://gateway:10000"
}'
```

**2. Configurar no `Program.cs`:**
```csharp
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddSecretsManager(
        configurator: options =>
        {
            options.SecretFilter = s => s.Name.StartsWith("LabWeb7/");
        });
}
```

**3. Instalar o pacote:**
```bash
dotnet add package Amazon.Extensions.Configuration.SystemsManager
```

**Custo:** ~R$ 0,60/secreto/mês + R$ 0,02/10.000 chamadas

#### Opção 4: Docker / Kubernetes

**Docker Compose:**
```yaml
services:
  labweb7:
    image: labweb7:latest
    environment:
      - Secrets__myVetorDeCifras=valor_producao
      - Secrets__mySecretKeyGoogle=chave_producao
      - Secrets__mySecretKeyPrivate=chave_producao
      - Secrets__GatewayUrl=http://gateway:10000
      - ConexaoPostgreSQL__PSQLConnectionStringEmpresas=Host=prod-db;...
```

**Docker Secrets (mais seguro):**
```yaml
secrets:
  db_connection:
    file: ./secrets/db_connection.txt
  encryption_key:
    file: ./secrets/encryption_key.txt
```

**Kubernetes Secrets:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: labweb7-secrets
type: Opaque
stringData:
  myVetorDeCifras: valor_producao
  mySecretKeyGoogle: chave_producao
  mySecretKeyPrivate: chave_producao
  GatewayUrl: "http://gateway:10000"
```

---

## 4. Precedência de Configuração

O .NET carrega as configurações nesta ordem (a **última sobrescreve** as anteriores):

```
1. appsettings.json                 ← valores padrão (ENTRA no Git)
2. appsettings.{Environment}.json   ← sobrescreve o anterior (ENTRA no Git)
3. User Secrets (dev apenas)        ← sobrescreve tudo acima (NÃO entra no Git)
4. Variáveis de ambiente            ← sobrescreve tudo acima (NÃO entra no Git)
5. Azure Key Vault / AWS SM         ← sobrescreve tudo (NÃO entra no Git, CLOUD)
```

**Exemplo prático:**

Se a chave `Secrets:myVetorDeCifras` existir em:
- `appsettings.json` = `"valor_padrao"`
- `appsettings.Development.json` = `"valor_dev"`
- User Secrets = `"valor_user_secrets"`

O .NET usará: `"valor_user_secrets"` (prioridade maior)

---

## 5. Comparativo de Soluções

| Critério | Variáveis de Ambiente | Azure Key Vault | AWS Secrets Manager | Docker Secrets |
|----------|----------------------|-----------------|---------------------|----------------|
| **Custo** | ✅ Grátis | ~R$ 3-10/mês | ~R$ 0,60/segredo/mês | ✅ Grátis |
| **Configuração** | ✅ Nenhuma | Média | Média | Baixa |
| **Segurança** | 🟡 Média | ✅ Alta (HSM) | ✅ Alta (KMS) | 🟡 Média |
| **Auditoria** | ❌ Nenhuma | ✅ Log de acesso | ✅ CloudTrail | ❌ Nenhuma |
| **Rotação automática** | ❌ Manual | ✅ Automática | ✅ Automática | ❌ Manual |
| **Dependência** | ✅ Nenhuma | 🟡 Azure | 🟡 AWS | 🟡 Docker |
| **Ideal para** | VPS própria | Azure App Service | AWS ECS/Lambda | Containers |

---

## 6. Checklist de Produção

Antes de publicar em produção, verifique:

- [ ] Nenhuma chave hardcoded no código-fonte (`.cs`)
- [ ] `appsettings.json` sem senhas reais de produção
- [ ] Strings de conexão usando variáveis de ambiente ou Key Vault
- [ ] Senhas de email em local seguro (não no Git)
- [ ] HTTPS obrigatório (`CookieSecurePolicy.Always`)
- [ ] `ASPNETCORE_ENVIRONMENT=Production` configurado
- [ ] Logs não expondo dados sensíveis
- [ ] Firewall configurado no servidor

---

## 7. Rotação de Chaves

### Quando trocar chaves

| Evento | Ação |
|--------|------|
| Vazamento suspeito | Trocar IMEDIATAMENTE |
| Funcionário sai da equipe | Trocar em até 24h |
| A cada 90 dias | Trocar senhas de banco e email |
| A cada 180 dias | Trocar chaves de API |
| Nunca trocar | `myVetorDeCifras` (perde dados criptografados) |

### Como trocar a chave AES (myVetorDeCifras)

**⚠️ ATENÇÃO:** Trocar esta chave torna todos os dados criptografados com ela **inacessíveis**!

```
1. Descriptografar TODOS os dados com a chave antiga
2. Trocar a chave no Key Vault / variável de ambiente
3. Recriptografar TODOS os dados com a nova chave
4. Testar exaustivamente
```

**Recomendação:** NÃO trocar esta chave a menos que haja vazamento confirmado.

---

## 8. Referências

- [Safe storage of app secrets in development](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [Azure Key Vault configuration provider](https://docs.microsoft.com/aspnet/core/security/key-vault-configuration)
- [AWS Secrets Manager for .NET](https://docs.aws.amazon.com/secretsmanager/latest/userguide/manage_secrets-with-dotnet.html)
- [Configuration in ASP.NET Core](https://docs.microsoft.com/aspnet/core/fundamentals/configuration)
