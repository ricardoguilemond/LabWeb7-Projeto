# Infraestrutura LabWeb7 — Custo Zero Permanente

**Data:** 19/04/2026
**Stack:** .NET 8 (C#) + Razor + jQuery + PostgreSQL
**Modelo:** Multi-cliente, banco unico por empresa
**Objetivo:** Custo $0/mes agora e no futuro, sem dependencia de trials ou free tiers temporarios

---

## Principio

Nenhum servico com gratuidade temporaria (AWS Free Tier, Azure trial, etc.).
Apenas planos gratuitos permanentes (free forever) ou ferramentas open source locais.

---

## Visao Geral da Arquitetura

```
DESENVOLVIMENTO / HOMOLOGACAO (local)
======================================
  Windows + .NET 8 SDK + PostgreSQL local
  Docker Desktop (simulacao de producao)

AMBIENTE ONLINE (gratuito permanente)
======================================
  [GitHub] --push--> [GitHub Actions CI/CD]
                          |
                    dotnet publish
                          |
                    [Render.com]         <-- Aplicacao .NET 8
                          |
                    [Supabase]           <-- PostgreSQL gratuito
                          
  Backups: local + Supabase Storage ou Google Drive
```

---

## 1. Ambiente Local (Desenvolvimento e Homologacao)

Nenhum custo. Tudo roda na maquina do desenvolvedor.

### Requisitos
- Windows 10/11
- .NET 8 SDK
- PostgreSQL 15+ (instalado local)
- Docker Desktop (opcional, para simular producao)
- Git

### Docker Compose para simulacao local

```yaml
# docker-compose.yml
version: "3.8"

services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5001
      - ConnectionStrings__PSQLConnectionString=Host=db;Port=5432;Database=labweb7;Username=labweb7user;Password=labweb7pass;SSL Mode=prefer;
    depends_on:
      db:
        condition: service_healthy

  db:
    image: postgres:15-alpine
    ports:
      - "5433:5432"
    environment:
      POSTGRES_DB: labweb7
      POSTGRES_USER: labweb7user
      POSTGRES_PASSWORD: labweb7pass
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U labweb7user -d labweb7"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  pgdata:
```

### Dockerfile

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LabWebMvc.sln .
COPY LabWebMvc.MVC/LabWebMvc.MVC.csproj LabWebMvc.MVC/
COPY BLL/BLL.csproj BLL/
COPY ExtensionsMethods/ExtensionsMethods.csproj ExtensionsMethods/
COPY Extensions/Extensions.csproj Extensions/
COPY ModeloDeDados/ModeloDeDados.csproj ModeloDeDados/
COPY ServicosDatabase/ServicosDatabase.csproj ServicosDatabase/
COPY ServicoExportacao/ServicoExportacao.csproj ServicoExportacao/
COPY WindowsService/WindowsService.csproj WindowsService/

RUN dotnet restore LabWebMvc.sln

COPY . .
RUN dotnet publish LabWebMvc.MVC -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Dependencias para System.Drawing (impressao)
RUN apt-get update && apt-get install -y libgdiplus && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

EXPOSE 5001
ENTRYPOINT ["dotnet", "LabWebMvc.MVC.dll"]
```

### Comandos Docker

```bash
# Subir ambiente completo
docker-compose up -d

# Ver logs
docker-compose logs -f app

# Parar
docker-compose down

# Rebuild apos alteracoes
docker-compose up -d --build app
```

---

## 2. Banco de Dados Online — Supabase (Gratuito Permanente)

Supabase oferece PostgreSQL gerenciado com plano gratuito sem expiracao.

### Plano Free (permanente)
- 500 MB de banco de dados
- 1 GB de storage
- 50.000 requisicoes de autenticacao/mes
- 2 projetos gratuitos
- Sem limite de tempo

### Configuracao

1. Criar conta em [supabase.com](https://supabase.com)
2. Criar novo projeto (regiao: South America East 1)
3. Anotar as credenciais em Project Settings > Database:
   - Host: `db.xxxxx.supabase.co`
   - Port: `5432`
   - Database: `postgres`
   - User: `postgres`
   - Password: (definida na criacao)

### Connection String para o LabWeb7

```
Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=SUA_SENHA;SSL Mode=require;Trust Server Certificate=true;
```

### Criacao das tabelas

Usar o SQL Editor do Supabase ou conectar via pgAdmin/DBeaver local
e executar os scripts de criacao de tabelas do projeto.

### Limitacoes
- 500 MB de storage (suficiente para desenvolvimento/estudo)
- Projeto pode ser pausado apos 7 dias de inatividade (reativa com um clique)
- Para manter ativo: configurar um health check periodico via GitHub Actions

### Health Check para evitar pausa (opcional)

```yaml
# .github/workflows/keep-alive.yml
name: Keep Supabase Alive

on:
  schedule:
    - cron: "0 8 */3 * *"  # A cada 3 dias as 8h UTC

jobs:
  ping:
    runs-on: ubuntu-latest
    steps:
      - name: Ping Supabase
        run: |
          curl -s -o /dev/null -w "%{http_code}" \
            "https://xxxxx.supabase.co/rest/v1/" \
            -H "apikey: ${{ secrets.SUPABASE_ANON_KEY }}"
```

---

## 3. Aplicacao Online — Render.com (Gratuito Permanente)

Render.com oferece hospedagem de aplicacoes web com plano gratuito permanente.

### Plano Free (permanente)
- 750 horas/mes de execucao
- Deploy automatico via GitHub
- HTTPS automatico (Let's Encrypt)
- Dominio gratuito: `seu-app.onrender.com`
- 512 MB RAM

### Limitacoes
- Instancia dorme apos 15 minutos de inatividade
- Cold start de ~30 segundos ao acordar
- 512 MB RAM (suficiente para .NET 8 em modo producao)

### Configuracao

1. Criar conta em [render.com](https://render.com)
2. Conectar repositorio GitHub
3. Criar novo Web Service:
   - **Environment:** Docker
   - **Region:** Oregon (US West) ou mais proximo disponivel
   - **Instance Type:** Free
   - **Branch:** main

### Variaveis de Ambiente no Render

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:10000
ConnectionStrings__PSQLConnectionString=Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=SUA_SENHA;SSL Mode=require;Trust Server Certificate=true;
```

Nota: Render usa porta 10000 por padrao para plano free.

### render.yaml (opcional, Infrastructure as Code)

```yaml
services:
  - type: web
    name: labweb7
    runtime: docker
    dockerfilePath: ./Dockerfile
    plan: free
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ASPNETCORE_URLS
        value: http://+:10000
      - key: ConnectionStrings__PSQLConnectionString
        sync: false  # definir manualmente no dashboard (contem senha)
```

---

## 4. CI/CD — GitHub Actions (Gratuito Permanente)

GitHub Actions oferece 2.000 minutos/mes gratuitos para repositorios publicos
e privados.

### Pipeline de Build e Deploy

```yaml
# .github/workflows/deploy.yml
name: Build and Deploy

on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"

      - name: Restore
        run: dotnet restore LabWebMvc.sln

      - name: Build
        run: dotnet build LabWebMvc.sln -c Release --no-restore

      # Deploy no Render e automatico via webhook do GitHub
      # Nao precisa de step adicional - o Render detecta o push e faz deploy
```

### Deploy automatico

O Render.com detecta pushes no branch `main` e faz deploy automaticamente.
Nao e necessario configurar deploy manual no GitHub Actions.

Se preferir controle manual, usar o Deploy Hook do Render:

```yaml
      - name: Trigger Render Deploy
        if: github.ref == 'refs/heads/main'
        run: |
          curl -X POST "${{ secrets.RENDER_DEPLOY_HOOK }}"
```

---

## 5. Backups

### Estrategia de Backup (custo zero)

| Metodo              | Destino                    | Frequencia | Custo |
|---------------------|----------------------------|------------|-------|
| pg_dump local       | Pasta local                | Diario     | $0    |
| Supabase Dashboard  | Export manual              | Semanal    | $0    |
| Google Drive        | Upload manual ou via rclone| Semanal    | $0    |

### Script de backup local

```bash
#!/bin/bash
# backup-local.sh
# Executa backup do PostgreSQL local para pasta de backups

DATA=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="$HOME/backups/labweb7"
mkdir -p "$BACKUP_DIR"

pg_dump -U labweb7user -h localhost -d labweb7 | gzip > "$BACKUP_DIR/labweb7_$DATA.sql.gz"

# Remove backups com mais de 30 dias
find "$BACKUP_DIR" -name "*.sql.gz" -mtime +30 -delete

echo "Backup concluido: labweb7_$DATA.sql.gz"
```

### Backup do Supabase (remoto)

```bash
#!/bin/bash
# backup-supabase.sh
# Executa backup do PostgreSQL no Supabase

DATA=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="$HOME/backups/labweb7-supabase"
mkdir -p "$BACKUP_DIR"

PGPASSWORD="SUA_SENHA" pg_dump \
  -h db.xxxxx.supabase.co \
  -p 5432 \
  -U postgres \
  -d postgres \
  --no-owner \
  --no-privileges \
  | gzip > "$BACKUP_DIR/supabase_$DATA.sql.gz"

echo "Backup Supabase concluido: supabase_$DATA.sql.gz"
```

---

## 6. Resumo de Custos

| Servico                  | Plano                  | Custo     | Expira?       |
|--------------------------|------------------------|-----------|---------------|
| .NET 8 SDK               | Open source            | $0        | Nunca         |
| PostgreSQL local         | Open source            | $0        | Nunca         |
| Docker Desktop           | Gratuito (uso pessoal) | $0        | Nunca         |
| GitHub (repositorio)     | Free                   | $0        | Nunca         |
| GitHub Actions (CI/CD)   | Free (2000 min/mes)    | $0        | Nunca         |
| Supabase (PostgreSQL)    | Free                   | $0        | Nunca         |
| Render.com (aplicacao)   | Free                   | $0        | Nunca         |
| Let's Encrypt (HTTPS)    | Gratuito (via Render)  | $0        | Nunca         |
| **Total**                |                        | **$0/mes**| **Permanente**|

---

## 7. Fluxo de Trabalho Completo

```
1. Desenvolve local (Windows + .NET 8 + PostgreSQL local)
         |
2. Testa com Docker (docker-compose up)
         |
3. Commit + Push para GitHub (branch main)
         |
4. GitHub Actions executa build automatico
         |
5. Render.com detecta push e faz deploy automatico
         |
6. Aplicacao online em https://labweb7.onrender.com
         |
7. Conecta ao Supabase (PostgreSQL remoto)
```

---

## 8. Evolucao Futura (quando houver orcamento)

Quando o projeto precisar ir para producao real com SLA:

| Necessidade      | Solucao                                    | Custo estimado |
|------------------|--------------------------------------------|----------------|
| Sem cold start   | Render Starter ($7/mes) ou Railway ($5/mes)| $5-7/mes       |
| Mais storage DB  | Supabase Pro ($25/mes) ou Railway Postgres | $25/mes        |
| Dominio proprio  | Registro .com.br (~R$40/ano)               | ~$8/ano        |
| Monitoramento    | UptimeRobot (gratuito) ou Better Stack     | $0             |

A migracao e simples: alterar a connection string e o destino de deploy.
O codigo da aplicacao nao precisa mudar.
