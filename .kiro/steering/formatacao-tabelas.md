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

### 5. Validação final obrigatória

Antes de concluir qualquer documento que contenha tabelas:

1. Verificar se todas as tabelas seguem o limite de 120 caracteres por linha.
2. Confirmar que as colunas estão alinhadas pelo maior texto.
3. Se alguma tabela estiver fora do padrão, corrigir imediatamente.
