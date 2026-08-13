# Steering Rules - Qoder (LabWeb7)

**Criado por:** Qoder AI  
**Data:** 21/04/2026  
**Status:** ✅ ATIVO - Usar estes steering files (NÃO os do Kiro)

---

## 📋 Visão Geral

Esta pasta contém os **steering files** do Qoder para o projeto LabWeb7. Estas regras **SUBSTITUEM** os steering files do Kiro (`.kiro/steering/`).

---

## 📁 Steering Files Disponíveis

### 1. **regras-gerais.md** 
**Trigger:** `always` (sempre ativo)  
**Conteúdo:**
- ✅ Conduta do desenvolvedor (10 regras)
- ✅ Restrições de arquivos e pastas
- ✅ Git operations rules
- ✅ Stack tecnológica do projeto
- ✅ Marcação de código (`//Feito pelo Qoder em dd/MM/yyyy`)
- ✅ Documentação

**Uso:** Aplicado em TODAS as operações

---

### 2. **encoding-acentuacao-ptbr.md**
**Trigger:** `always` (sempre ativo)  
**Conteúdo:**
- ✅ Padrão de encoding por tipo de arquivo
- ✅ UTF-8 com/sem BOM
- ✅ Preservação de acentuação PT-BR
- ✅ PowerShell manipulation segura
- ✅ Validação obrigatória
- ✅ Proibições

**Uso:** Aplicado ao criar/alterar QUALQUER arquivo

---

### 3. **regras-banco-dados.md**
**Trigger:** `always` (sempre ativo)  
**Conteúdo:**
- ✅ PostgreSQL exclusivo (NÃO SQL Server)
- ✅ Transações EF Core nativas
- ✅ Data/hora do servidor (NUNCA DateTime.Now)
- ✅ DateTime.Kind por tipo de coluna
- ✅ DbContext features (Factory, SaveChanges, Locking)
- ✅ Migrations: NÃO USAR
- ✅ Validação de FK antes de DELETE
- ✅ Performance de queries

**Uso:** Aplicado em TODAS as operações de banco de dados

---

### 4. **regras-controllers-views.md**
**Trigger:** `always` (sempre ativo)  
**Conteúdo:**
- ✅ BaseController pattern
- ✅ Serviços disponíveis via DI
- ✅ GeralController: NÃO alterar
- ✅ ValidacaoGenerica: retornar View() sem model
- ✅ Views MVC: NÃO usar @page
- ✅ site.js: carregamento duplo (NÃO adicionar terceiro)
- ✅ ViewModels: padrão de nomenclatura
- ✅ CRUD patterns
- ✅ JavaScript e jQuery

**Uso:** Aplicado ao criar/alterar controllers e views

---

### 5. **regras-plano-exames.md**
**Trigger:** `manual` (acionar quando necessário)  
**Conteúdo:**
- ✅ Modelo SUS (ExameId = 1) como template
- ✅ Estrutura da ContaExame (11 dígitos)
- ✅ Validação por prefixo (7 dígitos)
- ✅ Regras de exclusão (folha, conta principal, itens)
- ✅ Regras de inclusão (gap detection)
- ✅ Regras por instituição (TabelaExamesId)
- ✅ Tratamento de preços (Cenário 1 vs Cenário 2)

**Uso:** Acionar ao trabalhar com Plano de Exames

### 6. **regras-tela-requisitar.md**
**Trigger:** `always` (sempre ativo)  
**Conteúdo:**
- ❌ Proteção contra alterações não autorizadas na tela Requisitar
- ✅ Funcionalidades críticas que não devem ser alteradas
- ✅ Problemas históricos conhecidos e como evitá-los
- ✅ Checklist obrigatório antes de alterar a tela
- ✅ Lista de arquivos protegidos

**Uso:** Aplicado em TODAS as operações — leitura obrigatória antes de tocar na tela Requisitar

---

### 6. **analise-integrada.md**
**Trigger:** `manual` (acionar quando necessário)  
**Conteúdo:**
- ✅ Pipeline de análise: Banco × Modelos × Scripts
- ✅ Etapa 1: Extrair metadados PostgreSQL
- ✅ Etapa 2: Extrair metadados modelos C#
- ✅ Etapa 3: Extrair metadados scripts DDL
- ✅ Etapa 4: Normalizar e comparar
- ✅ Etapa 5: Gerar relatórios
- ✅ Regras de execução
- ✅ Problemas comuns e soluções

**Uso:** Acionar ao validar consistência do banco de dados

---

### 7. **formatacao-tabelas.md**
**Trigger:** `always` (sempre ativo)  
**Conteúdo:**
- ✅ Limite de 120 caracteres por linha
- ✅ Dimensionamento de células
- ✅ Quebra de texto longo
- ✅ Separadores e alinhamento
- ✅ Validação final obrigatória
- ✅ Exemplos práticos
- ✅ Erros comuns

**Uso:** Aplicado ao criar/editar tabelas Markdown

---

## 🔄 Steering Kiro vs Qoder

### ❌ NÃO USAR (LEGADO)
```
.kiro/steering/
├── regras_gerais.md
├── encoding-acentuacao-ptbr.md
├── formatacao-tabelas.md
├── regras-plano-exames.md
└── analise-integrada-banco-modelo-scripts.md
```

### ✅ USAR SEMPRE (ATIVO)
```
.qoder/steering/
├── regras-gerais.md
├── encoding-acentuacao-ptbr.md
├── formatacao-tabelas.md
├── regras-banco-dados.md
├── regras-controllers-views.md
├── regras-plano-exames.md
├── regras-tela-requisitar.md
└── analise-integrada.md
```

---

## 🎯 Quando Usar Cada Steering

### Desenvolvimento Diário (Sempre Ativos)
1. ✅ `regras-gerais.md` - Conduta e restrições
2. ✅ `encoding-acentuacao-ptbr.md` - Encoding e acentuação
3. ✅ `regras-banco-dados.md` - Banco de dados
4. ✅ `regras-controllers-views.md` - Controllers e Views
5. ✅ `formatacao-tabelas.md` - Tabelas Markdown

### Funcionalidades Específicas (Acionar Manualmente)
6. 🔧 `regras-plano-exames.md` - Ao trabalhar com Plano de Exames
7. ⚠️ `regras-tela-requisitar.md` - Proteção permanente da tela Requisitar (sempre consultar)
8. 🔧 `analise-integrada.md` - Ao validar consistência do banco

---

## 📊 Resumo das Regras

### Conduta
- NUNCA assumir/inferir informações
- SEMPRE ler código diretamente
- Atuar como Tech Lead Sênior
- Avaliar impacto antes de implementar

### Banco de Dados
- PostgreSQL exclusivo (NÃO SQL Server)
- Data/hora do servidor (NUNCA DateTime.Now)
- Validar FKs antes de DELETE
- NÃO usar Migrations

### Controllers/Views
- Herdar de BaseController
- ValidacaoGenerica: View() sem model
- Views MVC: NÃO usar @page
- site.js: NÃO adicionar terceira referência

### Plano de Exames
- SUS (ExameId=1) é template base
- ContaExame: 11 dígitos, validar por prefixo 7 dígitos
- Gap detection para inclusão
- Dois cenários de preço (individual vs em massa)

### Encoding
- .cs, .cshtml: UTF-8 com BOM
- .js: Manter existente
- .json, .css: UTF-8 sem BOM
- Português-Brasil com acentuação correta

### Tabelas Markdown
- Máximo 120 caracteres por linha
- Colunas alinhadas pelo maior texto
- Texto longo: quebrar ou resumir

---

## 📝 Marcação de Código

### Padrão Qoder
```csharp
//Feito pelo Qoder em dd/MM/yyyy
// ... código ...
//..Qoder
```

### Quando Marcar
- ✅ Métodos novos
- ✅ Verificações de FK
- ✅ Migrações de transação
- ✅ Correções de lógica
- ✅ Validações de negócio

### Quando NÃO Marcar
- ❌ Alterações triviais
- ❌ Formatação de código
- ❌ Correções de typos

---

## 🔗 Links Relacionados

### Documentação
- **Análise Arquitetural:** `Documentos do Qoder/analise-arquitetural-completa-labweb7.md`
- **Quick Reference:** `Documentos do Qoder/quick-reference-labweb7.md`
- **Diagramas:** `Documentos do Qoder/diagramas-arquiteturais-labweb7.md`

### Steering Kiro (LEGADO - NÃO USAR)
- `.kiro/steering/`

---

## ⚠️ Notas Importantes

1. **Estes steering files SUBSTITUEM os do Kiro**
2. **Sempre consultar estes arquivos antes de implementar**
3. **Em caso de dúvida, seguir as regras aqui documentadas**
4. **Se regra não cobrir situação, perguntar ao usuário**

---

## 🚀 Manutenção

### Adicionar Nova Regra
1. Criar arquivo `.md` nesta pasta
2. Adicionar frontmatter com `trigger` e `description`
3. Documentar regras com exemplos
4. Atualizar este README

### Atualizar Regra Existente
1. Editar arquivo correspondente
2. Manter histórico de alterações
3. Atualizar este README se necessário

---

**Steering files criados por Qoder AI - 21/04/2026**  
*Baseados nas melhores práticas e regras do projeto LabWeb7*
