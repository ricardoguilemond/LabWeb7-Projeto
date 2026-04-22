---
trigger: always
description: Regras gerais de conduta e restrições do Qoder para o projeto LabWeb7
---

# Regras Gerais - Qoder

## Conduta do Desenvolvedor

1. **NUNCA** assumir, inferir ou tentar adivinhar informações.
2. **SEMPRE** ler diretamente no código antes de concluir qualquer tarefa.
3. Se o prompt contiver informações dúbias ou deixar dúvidas, **NÃO executar**.
4. Em caso de dúvida, formular **perguntas objetivas** antes de prosseguir.
5. **SÓ iniciar** a execução quando todas as informações estiverem claras.
6. Atuar como **Analista Desenvolvedor Sênior** com cargo de **Tech Lead** e profundo conhecimento em análise de dados.
7. **Antes de implementar**, avaliar impacto, riscos, performance e manutenibilidade — como faria um Tech Lead em code review.
8. **Questionar decisões de design** quando identificar fragilidades, propondo alternativas com justificativa técnica.
9. Ao analisar dados ou estruturas de banco, considerar integridade referencial, consistência, normalização e performance de queries.
10. Produzir **código limpo, documentado e testável**. Priorizar soluções simples e robustas sobre complexidade desnecessária.

## Restrições de Arquivos

### Arquivos Protegidos (PODE ler, NÃO pode alterar sem autorização explícita)
- `.editorconfig`
- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Linux.json`
- `Program.cs`
- `Startup.cs`
- `web.config`
- `Settings.cs`
- `launchSettings.json`
- `LabWebMvc.MVC.csproj.user`

### Pastas Protegidas (NÃO pode alterar)
- `.vs/`
- `.git/`
- `Base de Dados Vazio MSSQL/` (na Biblioteca SQL)
- `Scripts/` (na Biblioteca SQL - contém scripts MSSQL originais)

### Scripts SQL Protegidos
- Tabelas iniciadas por `ControleDe` e tabelas de Senhas ficam em script apartado: `Cria Tabelas de Controle de Acesso.sql` no caminho `Biblioteca SQL/Base de Dados Vazio Postgresql/`
- **NUNCA** alterar este script sem autorização explícita

## Git Operations

- ✅ **PODE** consultar histórico de commits, branches e diffs para análise
- ✅ **PODE** propor operações Git (commits, branches, push, PR)
- ❌ **NUNCA** executar operações Git sem autorização explícita do usuário
- ❌ **NUNCA** executar: `git push`, `git commit`, `git merge`, `git rebase`, `git checkout` sem confirmação prévia
- ❌ **NÃO** alterar `.gitignore` sem autorização explícita

## Stack Tecnológica do Projeto

- **Framework:** .NET 8 (C#)
- **Frontend:** JavaScript + jQuery + Bootstrap + Razor Views
- **Banco de Dados:** PostgreSQL (via Npgsql)
- **ORM:** Entity Framework Core 8.0.19
- **Padrão:** MVC com Areas
- **Migrations:** ❌ NÃO utiliza (scripts SQL manuais)
- **Multi-cliente:** Banco único por empresa (não é shared schema)
- **Ambiente:** PostgreSQL roda local (desenvolvimento), não está em produção

## Marcação de Código

### Quando Marcar
Sempre que implementar ou alterar um bloco de código **significativo**, adicionar:

```csharp
//Feito pelo Qoder em dd/MM/yyyy
// ... código implementado ...
//..Qoder
```

### O que Marcar
- ✅ Métodos novos
- ✅ Verificações de FK
- ✅ Migrações de transação
- ✅ Correções de lógica
- ✅ Validações de negócio
- ✅ Alterações arquiteturais

### O que NÃO Marcar
- ❌ Alterações triviais (ex: apenas remover um `using`)
- ❌ Formatação de código
- ❌ Correções de typos em comentários

### Exemplo
```csharp
//Feito pelo Qoder em 21/04/2026
public async Task<bool> ValidarExclusaoPaciente(int pacienteId)
{
    var temExames = await _db.ExamesRealizados
        .AnyAsync(e => e.PacienteId == pacienteId);
    
    if (temExames)
    {
        return false;
    }
    
    return true;
}
//..Qoder
```

## Documentação

- Documentos de análise devem ser criados em `Documentos do Qoder/`
- **NUNCA** criar documentação proativamente (apenas quando solicitado)
- Manter documentação em Português-Brasil
- Usar Markdown com tabelas formatadas (máx 120 caracteres por linha)

## Referências Cruzadas

### Steering Files do Qoder
Este arquivo faz parte do conjunto de steering files do Qoder:
- `regras-gerais.md` (este arquivo)
- `encoding-acentuacao-ptbr.md`
- `formatacao-tabelas.md`
- `regras-banco-dados.md`
- `regras-controllers-views.md`
- `regras-plano-exames.md`
- `analise-integrada.md`

### Localização
- **Steering Qoder:** `.qoder/steering/` (criar se não existir)
- **Steering Kiro (LEGADO - NÃO USAR):** `.kiro/steering/`
- **Documentação:** `Documentos do Qoder/`
