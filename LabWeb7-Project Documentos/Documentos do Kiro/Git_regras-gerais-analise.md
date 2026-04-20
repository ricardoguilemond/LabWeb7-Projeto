# Git — Análise do regras_gerais.md

**Data:** 19/04/2026
**Arquivo:** `.kiro/steering/regras_gerais.md`

---

## 1. Histórico de Commits

O arquivo `regras_gerais.md` foi criado durante a sessão com o Kiro
e commitado uma única vez:

| Commit   | Data                     | Mensagem                                    |
|----------|--------------------------|---------------------------------------------|
| 54ddead6 | 2026-04-16 21:55:35 -03  | Todas as alterações e otimizações com IA     |

Não há histórico anterior de versões deste arquivo.

---

## 2. Estado Atual do Arquivo

O arquivo contém 3 seções:
- **Conduta** — 5 regras de comportamento do Kiro
- **Restrições de arquivos** — lista de arquivos protegidos
- **Projeto** — informações técnicas do stack

---

## 3. Problemas Identificados

### 3.1 Acentuação inconsistente

O arquivo mistura texto sem acento e texto com acento:
- Sem acento: "informacoes", "dubias", "duvidas", "execucao"
- Com acento: "NÃO", "autorização", "explícita", "executá-las"

**Recomendação:** padronizar tudo com acentuação correta em PT-BR,
conforme o steering `encoding-acentuacao-ptbr.md`.

### 3.2 Front-matter com `inclusion: auto`

O valor `auto` não é um tipo válido de inclusão para steerings do Kiro.
Os valores válidos são: `always` (padrão), `fileMatch` ou `manual`.

**Recomendação:** alterar para `inclusion: always`.

### 3.3 Regra de Git ambígua — RESOLVIDO

A regra foi atualizada para uma seção própria `## Git` no steering,
com as seguintes definições claras:

- Pode consultar histórico (commits, branches, diffs) livremente.
- Pode propor operações Git.
- Deve **sempre perguntar** antes de executar qualquer operação Git.
- Nunca executar push, commit, merge, rebase ou checkout sem confirmação.
- Não alterar `.gitignore` sem autorização.

### 3.4 Faltam regras aprendidas nas sessões anteriores

Com base no histórico de problemas, as seguintes regras deveriam
ser adicionadas:

- Não usar `TransactionScope` com PostgreSQL/Npgsql sem
  `TransactionScopeAsyncFlowOption.Enabled`
- Preferir `_db.Database.BeginTransactionAsync()` (EF Core nativo)
- Não remover `@page` de views sem verificar o encoding resultante
- Não usar scripts PowerShell em lote sem validar encoding depois
- Todas as conexões devem ser PostgreSQL (não SQL Server)
- Não alterar `ValidacaoGenerica` para passar model às Views

---

## 4. Proposta de Atualização

Seções sugeridas para adicionar ao `regras_gerais.md`:

### Banco de Dados
- Banco exclusivo: PostgreSQL (Npgsql)
- Não usar pacotes ou código SQL Server
- Preferir transações EF Core nativas (`BeginTransactionAsync`)
- Não usar `TransactionScope` sem `TransactionScopeAsyncFlowOption.Enabled`

### Controllers e Views
- `ValidacaoGenerica` deve retornar `View()` sem model (dados via ViewBag)
- Não alterar a assinatura ou retorno de métodos do `GeralController`
- Views MVC não devem ter diretiva `@page` (exclusiva de Razor Pages)

### Encoding
- Referência ao steering `encoding-acentuacao-ptbr.md`

---

## 5. Ação Recomendada

Atualizar o `regras_gerais.md` com:
1. Corrigir acentuação para PT-BR completo
2. Alterar `inclusion: auto` para `inclusion: always`
3. Adicionar seções de Banco de Dados e Controllers/Views
4. Manter a regra de Git com autorização explícita
