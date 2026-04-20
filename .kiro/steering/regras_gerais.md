---
inclusion: always
description: Regras gerais de conduta e restrições do Kiro para o projeto LabWeb7
---

# Regras Gerais

## Conduta

1. Jamais supor, inferir ou tentar adivinhar informações.
2. Sempre ler diretamente no código antes de concluir qualquer tarefa.
3. Se o prompt contiver informações dúbias ou deixar dúvidas, não executar.
4. Em caso de dúvida, formular perguntas objetivas antes de prosseguir.
5. Só iniciar a execução quando todas as informações estiverem claras.

## Restrições de Arquivos

- Pode ler mas NÃO pode alterar (salvo autorização explícita):
  `.editorconfig`, `appsettings.json`, `appsettings.Development.json`,
  `Program.cs`, `Startup.cs`, `web.config`, `Settings.cs`, `launchSettings.json`
- Pode ler mas NÃO pode alterar pastas: `.vs/`, `.git/`
- Nunca alterar a pasta `Base de Dados Vazio MSSQL` na Biblioteca SQL
- Nunca alterar a pasta `Scripts/` na Biblioteca SQL (contém scripts MSSQL originais)
- Tabelas iniciadas por `ControleDe` e tabelas de Senhas ficam em
  script apartado: `Cria Tabelas de Controle de Acesso.sql`
  no caminho `Biblioteca SQL/Base de Dados Vazio Postgresql/`

## Git

- Pode consultar histórico de commits, branches e diffs para análise.
- Pode propor operações Git (commits, branches, push, PR).
- Antes de executar qualquer operação Git, **sempre perguntar** ao
  usuário e aguardar autorização explícita.
- Nunca executar `git push`, `git commit`, `git merge`, `git rebase`
  ou `git checkout` sem confirmação prévia do usuário.
- Não alterar `.gitignore` sem autorização explícita.

## Projeto

- Framework: .NET 8 (C#), Frontend: JavaScript + jQuery + Razor, Banco: PostgreSQL
- Não utiliza Migrations
- Multi-cliente, banco único por empresa
- PostgreSQL roda local (desenvolvimento), não está em produção

## Banco de Dados

- Banco exclusivo: **PostgreSQL** (Npgsql)
- Não usar pacotes, código ou sintaxe SQL Server
- Preferir transações EF Core nativas (`_db.Database.BeginTransactionAsync()`)
- Não usar `TransactionScope` sem `TransactionScopeAsyncFlowOption.Enabled`
- Connection strings devem apontar para `ConexaoPostgreSQL` / `PSQLConnectionString`
- Funções de data/hora: usar `NOW()` do PostgreSQL (não `GETDATE()` ou `SYSDATETIME()`)

## Data e Hora

- Nunca usar `DateTime.UtcNow`, `DateTime.Now` ou `DateTime.Today` para gravar no banco
- Sempre obter data/hora do servidor via `_geralController.ObterDataHoraServidor()`
  ou via query `SELECT NOW()` no PostgreSQL
- Para colunas `timestamp without time zone`: usar `DateTime` com `Kind=Unspecified`
  ou `Kind=Local` (nunca `Kind=UTC`)
- Para colunas `timestamp with time zone`: usar `DateTime` com `Kind=UTC`
  ou `DateTimeOffset`
- Nunca confiar na data/hora do computador cliente
- Em logs e EventLog, `DateTime.UtcNow` é aceitável (não grava no banco)

## Controllers e Views

- `ValidacaoGenerica` deve retornar `View()` sem model (dados via ViewBag)
- Não alterar a assinatura ou retorno de métodos do `GeralController`
  sem autorização explícita
- Views MVC (pasta `Views/`) não devem ter diretiva `@page`
  (`@page` é exclusiva de Razor Pages na pasta `Pages/`)
- O `site.js` é carregado duas vezes no `_Layout.cshtml` (no head e no body)
  — não adicionar uma terceira referência

## Regras de Negócio — Requisição de Exames

- No formulário de Requisição, o salvamento de Médico e Paciente
  deve ser **fora da transação** do lançamento de exames.
- Motivo: se o lançamento do exame falhar e houver rollback,
  o médico e o paciente devem continuar salvos no banco.
- Nunca envolver o cadastro de Médico/Paciente dentro da mesma
  transação que processa os itens de exame na Requisição.

## Regras de Negócio — Exclusão de Registros

- Antes de excluir qualquer registro, verificar se existem
  dados relacionados em tabelas filhas (FKs).
- Se houver vínculos, retornar mensagem assertiva informando
  o motivo (ex: "Paciente possui exames vinculados e não pode
  ser excluído").
- Nunca deixar a exceção de FK do PostgreSQL ser a única
  proteção — sempre validar no controller antes do DELETE.
- Documentos de análise devem ser criados em `Documentos do Kiro/`.

## Encoding

- Seguir integralmente o steering `encoding-acentuacao-ptbr.md`
- Todos os textos em Português-Brasil com acentuação correta
- Não usar scripts PowerShell em lote sem validar encoding depois
