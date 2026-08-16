---
inclusion: always
---

# Steering de Formatação de Tabelas Markdown

## Objetivo

Garantir que todas as tabelas criadas ou editadas pelo Kiro sejam legíveis,
alinhadas e respeitem o limite de largura do projeto.

## Checklist obrigatório

### 1. Limite de caracteres por linha

- Cada linha da tabela deve ter no máximo **120 caracteres**.
- Se ultrapassar, quebrar o texto dentro da célula em múltiplas linhas.
- Largura máxima desejável de referência: **~105 caracteres** por linha
  (incluindo delimitadores `|` e espaços internos).

#### Exemplo de largura máxima de referência (105 chars)

```markdown
| Pacote                                  | Versão  | Finalidade                 | Criticidade |
|-----------------------------------------|---------|----------------------------|-------------|
| Npgsql.EntityFrameworkCore.PostgreSQL   | 8.0.4   | Provider EF Core PostgreSQL| CRÍTICA     |
| BCrypt.Net-Next                         | 4.1.0   | Hash de senhas             | CRÍTICA     |
```

- A tabela acima (4 colunas, ~105 chars) representa o **limite
  desejável** de largura para tabelas em linha única.
- Tabelas que ultrapassem essa largura devem usar o formato
  multi-linha (seção 5) ou reduzir o número de colunas.

### 2. Dimensionamento das células

- Ajustar cada coluna pelo maior texto presente naquela coluna.
- Nenhuma célula pode ficar desalinhada ou truncada.
- A linha de separadores (`|---|`) deve ter o mesmo comprimento das colunas.

### 3. Quebra de texto longo

Se um texto for extenso e fizer a linha ultrapassar 120 caracteres,
dividir em linhas internas da célula ou resumir o conteúdo.

Exemplo correto:

```markdown
| Item | Objetivo                                                    |
|------|-------------------------------------------------------------|
| 1    | Texto que respeita o limite de 120 caracteres por linha     |
| 2    | Outro texto dentro do limite                                |
```

Exemplo incorreto (linha ultrapassa 120 caracteres):

```markdown
| Item | Objetivo                                                                                                                              |
|------|-------------------------------------------------------------------------------------------------------------------------------------------|
| 1    | Este texto é muito longo e ultrapassa o limite de 120 caracteres por linha, tornando a tabela difícil de ler no editor e no repositório |
```

### 4. Separadores e alinhamento

- Usar `|` como delimitador de colunas.
- Alinhar todas as colunas de forma uniforme.
- Sem espaços extras ou desalinhamentos entre células.
- Um espaço antes e depois do conteúdo de cada célula.

### 5. Tabelas com conteúdo extenso (multi-linha por registro)

Quando uma tabela tem colunas cujo conteúdo é longo demais para caber
em uma única linha de 120 caracteres, usar o padrão **multi-linha por
registro**:

#### Regras

- Cada registro (componente) pode ocupar **até 3 linhas** na tabela.
- A primeira linha contém o início do texto de cada coluna.
- As linhas seguintes (2ª e 3ª) continuam o texto, deixando as demais
  colunas vazias (apenas `|` e espaços).
- Separar registros distintos com uma **linha vazia** na tabela
  (`| | | | |`) para facilitar a leitura visual.
- Os nomes de header devem ser **abreviados** quando necessário para
  reduzir a largura total (ex: "Compl." em vez de "Complexidade").
- Largura máxima de cada coluna: ~25 caracteres (flexível conforme
  necessidade, mas priorizando legibilidade).

#### Exemplo correto

```markdown
| Componente              | Localização              | Compl.    | Justificativa              |
|-------------------------|--------------------------|-----------|----------------------------|
| `SaveChangesWithSync`   | Models/db.cs             | CRÍTICA   | Reflection + lock +        |
| `Async`                 |                          |           | gap-fill + concorrência    |
|                         |                          |           |                            |
| `SalvarRequisicao`      | Areas/Controllers/       | ALTÍSSIMA | Orquestra paciente +       |
|                         | Requisitar               |           | médico + exame +           |
|                         |                          |           | itens + edição             |
|                         |                          |           |                            |
| `Repositorio<T>`        | Interfaces               | BAIXA     | CRUD genérico padrão       |
```

#### Quando usar

- Tabelas de classificação de complexidade com justificativas.
- Tabelas de inventário com colunas de descrição/observação.
- Qualquer tabela onde o conteúdo de 2+ colunas ultrapasse 25
  caracteres simultaneamente.

#### Quando NÃO usar

- Tabelas simples onde todo o conteúdo cabe em uma linha de 120 chars.
- Tabelas de referência rápida (lookup) com valores curtos.

### 6. Validação final obrigatória

Antes de concluir qualquer documento que contenha tabelas:

1. Verificar se todas as tabelas seguem o limite de 120 caracteres por linha.
2. Confirmar que as colunas estão alinhadas pelo maior texto.
3. Se alguma tabela estiver fora do padrão, corrigir imediatamente.
4. Se alguma tabela usar o formato multi-linha, confirmar que os
   registros estão separados por linha vazia e não excedem 3 linhas.
