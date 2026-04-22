---
trigger: always
description: Steering de Formatação de Tabelas Markdown para LabWeb7
---

# Steering de Formatação de Tabelas Markdown - Qoder

## Objetivo

Garantir que todas as tabelas criadas ou editadas pelo Qoder sejam:
- ✅ Legíveis
- ✅ Alinhadas
- ✅ Respeitem o limite de largura do projeto

## Checklist Obrigatório

### 1. Limite de Caracteres por Linha

- ✅ Cada linha da tabela deve ter **NO MÁXIMO 120 caracteres**
- ✅ Se ultrapassar, quebrar o texto dentro da célula em múltiplas linhas

### 2. Dimensionamento das Células

- ✅ Ajustar cada coluna pelo **maior texto** presente naquela coluna (incluindo o cabeçalho)
- ✅ A linha de separadores (`|---|`) deve ter **EXATAMENTE** o mesmo comprimento da coluna
- ✅ **Todas as células da coluna** devem ter o mesmo tamanho (preenchidas com espaços)
- ✅ Nenhuma célula pode ficar desalinhada ou truncada

#### **Regra de Ouro do Alinhamento:**

**PASSO 1: Para CADA coluna, encontre o MAIOR texto em TODAS as linhas (incluindo cabeçalho)**

**PASSO 2: Calcule o tamanho da coluna = maior texto + 2 espaços (um de cada lado)**

**PASSO 3: TODAS as células daquela coluna devem ter exatamente esse tamanho**

**PASSO 4: Todas as linhas da tabela terão EXATAMENTE o mesmo número de caracteres**

Exemplo prático:
```
Tabela:
| Pacote                                   | Versão | Uso                              |
| Npgsql.EntityFrameworkCore               | 8.0.4  | Provider PostgreSQL para EF Core |
| Microsoft.EntityFrameworkCore            | 8.0.19 | ORM Entity Framework Core        |
| Microsoft.EntityFrameworkCore.Relational | 8.0.19 | Suporte a bancos relacionais     |
| Microsoft.EntityFrameworkCore.Tools      | 8.0.19 | Ferramentas de scaffolding       |
| Microsoft.AspNetCore.Identity.UI         | 8.0.19 | Identity UI para autenticação    |

Análise COLUNA 1:
- "Pacote" = 6 chars
- "Npgsql.EntityFrameworkCore" = 26 chars
- "Microsoft.EntityFrameworkCore" = 29 chars
- "Microsoft.EntityFrameworkCore.Relational" = 40 chars ← MAIOR
- "Microsoft.EntityFrameworkCore.Tools" = 35 chars
- "Microsoft.AspNetCore.Identity.UI" = 32 chars
→ Tamanho da coluna: 40 + 2 = 42 chars

Análise COLUNA 2:
- "Versão" = 6 chars
- "8.0.19" = 6 chars ← MAIOR (empate)
→ Tamanho da coluna: 6 + 2 = 8 chars

Análise COLUNA 3:
- "Uso" = 3 chars
- "Provider PostgreSQL para EF Core" = 32 chars ← MAIOR
- "ORM Entity Framework Core" = 25 chars
- "Suporte a bancos relacionais" = 28 chars
- "Ferramentas de scaffolding" = 26 chars
- "Identity UI para autenticação" = 29 chars
→ Tamanho da coluna: 32 + 2 = 34 chars

Total de cada linha:
42 (col1) + 8 (col2) + 34 (col3) + 4 (pipes) = 88 chars

Resultado: TODAS as linhas têm exatamente 88 caracteres! ✅
```

### 3. Quebra de Texto Longo

Se um texto for extenso e fizer a linha ultrapassar 120 caracteres:

#### **Opção 1: Quebrar em Múltiplas Linhas (Recomendado para descrições)**

```markdown
| Recurso | Descrição                                                                 |
|---------|---------------------------------------------------------------------------|
| EF Core | ORM para acesso ao banco de dados.<br/>Suporta PostgreSQL via Npgsql.    |
```

#### **Opção 2: Resumir Conteúdo (Recomendado quando possível)**

```markdown
| Pacote          | Versão | Uso                     |
|-----------------|--------|-------------------------|
| EF Core         | 8.0.19 | ORM                     |
| EF Core Rel.    | 8.0.19 | Suporte relacional      |
```

#### **Opção 3: Aumentar a Coluna (Para nomes que NÃO podem ser quebrados)**

**IMPORTANTE:** Se o nome **NÃO PODE** ser quebrado ou resumido (ex: nome de pacote NuGet, classe C#), a coluna DEVE ser dimensionada para o maior texto, mesmo que fique larga.

```markdown
| Pacote                               | Versão | Uso                            |
|--------------------------------------|--------|--------------------------------|
| Npgsql.EntityFrameworkCore           | 8.0.4  | Provider PostgreSQL para EF Core|
| Microsoft.EntityFrameworkCore        | 8.0.19 | ORM Entity Framework Core      |
| Microsoft.EntityFrameworkCore.Relational | 8.0.19 | Suporte a bancos relacionais|
| Microsoft.EntityFrameworkCore.Tools  | 8.0.19 | Ferramentas de scaffolding     |
```

**Regra de Ouro:** A coluna inteira (TODAS as linhas) deve ter o mesmo tamanho do maior texto. Não importa se outras linhas ficam com espaços extras.

### 3.1 Emojis em Tabelas

**Regra Especial para Emojis (✅ ❌ 🔵 etc.):**

Emojis têm largura visual de **2 caracteres**, mas o Python/Markdown conta como **1 caractere**.

**Fórmula de Compensação:**
```
Quando a tabela TEM emojis:
- Largura visual do emoji = 2 chars
- Largura Python do emoji = 1 char
- Compensação: +1 caractere no cabeçalho e tracejado

Fórmula:
  coluna_com_emoji = maior_texto + 2 + 1  # +1 espaço extra
  cabecalho_coluna = maior_texto + 2 + 1  # +1 espaço extra  
  tracejado_coluna = (maior_texto + 2) + 1  # +1 hífen extra

Resultado:
  - Linhas com emoji: N chars Python = (N+1) visual
  - Header/tracejado: (N+1) chars Python = (N+1) visual
  = TODOS ALINHADOS! ✅
```

**Exemplo Prático:**

```markdown
| Extensão              | Encoding | BOM               |
|-----------------------|----------|-------------------|
| .cs, .cshtml, .csproj | UTF-8    | ✅ Com BOM        |
| .js                   | UTF-8    | 🔵 Manter         |
| .json, .css, .md      | UTF-8    | ❌ Sem BOM        |
```

**Análise:**
- Coluna BOM: maior texto = "Manter existente" (16 chars)
- Coluna com emoji: 16 + 2 = 18 chars no Python
- Header/tracejado: 18 + 1 = **19 chars** no Python
- Linhas com emoji: 18 chars Python = **19 visual** (emoji = +1)
- Header/tracejado: 19 chars Python = **19 visual**
- **Resultado:** Todos com 19 chars visuais = ✅ ALINHADO!

**Nota:** Pode haver uma diferença visual de "meio caractere" em alguns editores, mas a fórmula de +1 caractere é a que produz o melhor alinhamento possível.

### Exemplo Incorreto

```markdown
| Item | Objetivo                                                                                                                              |
|------|-------------------------------------------------------------------------------------------------------------------------------------------|
| 1    | Este texto é muito longo e ultrapassa o limite de 120 caracteres por linha, tornando a tabela difícil de ler no editor e no repositório |
```

### 4. Separadores e Alinhamento

- ✅ Usar `|` como delimitador de colunas
- ✅ **ALINHAR TODAS as colunas**: cada célula deve ter exatamente o mesmo número de caracteres
- ✅ A linha de separadores deve ter **EXATAMENTE** o mesmo comprimento das colunas de dados
- ✅ **UM** espaço antes e depois do conteúdo de cada célula
- ✅ **NENHUM** espaço extra entre o conteúdo e os pipes `|`

#### **Como Verificar Alinhamento Correto:**

```markdown
✅ CORRETO - Todas as células da coluna têm mesmo tamanho:
| Pacote                        | Versão | Uso                         |
|-------------------------------|--------|-----------------------------|
| Npgsql                        | 8.0.4  | Provider                    |
| Microsoft.EntityFrameworkCore | 8.0.19 | ORM                         |

❌ ERRADO - Células com tamanhos diferentes:
| Pacote                        | Versão | Uso                         |
|-------------------------------|--------|-----------------------------|
| Npgsql | 8.0.4 | Provider |
| Microsoft.EntityFrameworkCore | 8.0.19 | ORM |
```

#### **Regra Prática de 5 Passos:**

1. **Para cada coluna**, analise **TODAS as linhas** (incluindo cabeçalho)
2. **Encontre o maior texto** em cada coluna
3. **Calcule o tamanho da coluna** = maior texto + 2 espaços
4. **Preencha TODAS as células** daquela coluna com exatamente esse tamanho
5. **Verifique:** Todas as linhas devem ter EXATAMENTE o mesmo número de caracteres

#### **Como Verificar se Está Correto:**

Use este script Python para verificar:
```python
with open('documento.md', 'r') as f:
    lines = f.readlines()
    for i, line in enumerate(lines):
        if '|' in line:
            print(f'Linha {i}: {len(line.rstrip())} chars')
```

**Se todas as linhas tiverem o mesmo número de caracteres, está correto!** ✅

### Formatação Correta

```markdown
| Coluna1   | Coluna2   | Coluna3   |
|-----------|-----------|-----------|
| Valor 1   | Valor 2   | Valor 3   |
| Valor 4   | Valor 5   | Valor 6   |
```

### Formatação Incorreta

```markdown
| Coluna1|Coluna2|Coluna3|
|-----------|-----------|-----------|
|Valor 1|Valor 2   |Valor 3   |
| Valor 4|   Valor 5| Valor 6|
```

### 5. Tabelas com Texto Longo

#### Técnica 1: Quebrar em Múltiplas Linhas

```markdown
| Recurso | Descrição                                                                 |
|---------|---------------------------------------------------------------------------|
| EF Core | ORM para acesso ao banco de dados.<br/>Suporta PostgreSQL via Npgsql.    |
```

#### Técnica 2: Resumir Conteúdo

```markdown
| Recurso | Versão | Finalidade                    |
|---------|--------|-------------------------------|
| EF Core | 8.0.19 | ORM - Acesso a dados          |
| Npgsql  | 8.0.4  | Provider PostgreSQL           |
```

#### Técnica 3: Usar Lista Dentro da Célula

```markdown
| Tabela | Relacionamentos                                                      |
|--------|----------------------------------------------------------------------|
| Pacientes | - ExamesRealizados<br/>- Requisitar<br/>- ExamesPendentes         |
```

## Validação Final Obrigatória

### Antes de Concluir Qualquer Documento com Tabelas

1. ✅ Verificar se **TODAS** as tabelas seguem o limite de **120 caracteres por linha**
2. ✅ Confirmar que as colunas estão **alinhadas pelo maior texto**
3. ✅ Se alguma tabela estiver fora do padrão, **CORRIGIR IMEDIATAMENTE**

### Script de Validação (Mental)

Para cada linha da tabela:
```
IF length(linha) > 120 THEN
    → Quebrar texto em múltiplas linhas
    → OU resumir conteúdo
END IF
```

## Exemplos Práticos

### Tabela de Dependências

```markdown
| Pacote                           | Versão  | Uso                           |
|----------------------------------|---------|-------------------------------|
| Npgsql.EntityFrameworkCore      | 8.0.4   | Provider PostgreSQL           |
| Microsoft.EntityFrameworkCore   | 8.0.19  | ORM                           |
| AWSSDK.S3                        | 4.0.6.2 | Amazon S3 Storage             |
| Azure.Storage.Blobs              | 12.25.0 | Azure Blob Storage            |
| itext                            | 9.3.0   | Geração de PDF                |
| SixLabors.ImageSharp             | 3.1.11  | Manipulação de imagens        |
```

### Tabela de Relacionamentos FK

```markdown
| Tabela Origem    | Tabela Destino       | FK Coluna    |
|------------------|----------------------|--------------|
| Pacientes        | ExamesRealizados     | PacienteId   |
| Pacientes        | Requisitar           | PacienteId   |
| Medicos          | ExamesRealizados     | MedicoId     |
| Instituicao      | Postos               | InstituicaoId|
| TabelaExames     | PlanoExames          | TabelaExamesId|
```

### Tabela de Configuração de Encoding

```markdown
| Tipo de Arquivo     | Encoding | BOM   |
|---------------------|----------|-------|
| .cs (C#)            | UTF-8    | Com   |
| .cshtml (Razor)     | UTF-8    | Com   |
| .js (JavaScript)    | UTF-8    | Manter|
| .json               | UTF-8    | Sem   |
| .css                | UTF-8    | Sem   |
| .md                 | UTF-8    | Sem   |
```

### Tabela de Controllers

```markdown
| Controller                 | Responsabilidade                    |
|----------------------------|-------------------------------------|
| PacientesController        | CRUD de pacientes                   |
| MedicosController          | CRUD de médicos                     |
| InstituicoesController     | CRUD de instituições                |
| RequisitarController       | Requisições de exames               |
| PlanoExamesController      | Gestão de planos de exames          |
```

## Erros Comuns

### ❌ Linha Ultrapassa 120 Caracteres

```markdown
| Pacote | Versão | Uso | Descrição Completa do Pacote com Muitos Detalhes |
|--------|--------|-----|--------------------------------------------------|
| EF Core | 8.0.19 | ORM | Este pacote é o Entity Framework Core que serve como ORM para acesso ao banco de dados PostgreSQL |
```

### ✅ Correção

```markdown
| Pacote  | Versão | Uso | Descrição                          |
|---------|--------|-----|------------------------------------|
| EF Core | 8.0.19 | ORM | Entity Framework Core para acesso  |
|         |        |     | ao banco de dados PostgreSQL       |
```

### ❌ Colunas Desalinhadas

```markdown
| Coluna1 | Coluna2 |
|-----------|-----------|
| Valor | Valor longo |
| Valor muito longo | Valor |
```

### ✅ Correção

```markdown
| Coluna1         | Coluna2         |
|-----------------|-----------------|
| Valor           | Valor longo     |
| Valor muito longo | Valor         |
```

## Checklist Rápido

Antes de finalizar:

```
□ Todas as linhas têm ≤ 120 caracteres?
□ Colunas alinhadas pelo maior texto?
│ Separadores (|---|) com comprimento correto?
□ Um espaço antes/depois do conteúdo?
□ Sem espaços extras ou desalinhamentos?
□ Texto longo quebrado ou resumido?
```

---

**Steering criado por Qoder - 21/04/2026**  
*Baseado nas melhores práticas do projeto LabWeb7*
