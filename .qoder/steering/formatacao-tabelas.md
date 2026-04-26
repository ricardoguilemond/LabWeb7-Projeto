---
trigger: always
description: Steering de Formatação de Tabelas para LabWeb7 — padrão único via desenhar_tabela_ascii
---

# Steering de Formatação de Tabelas - Qoder

## Regra Principal

**TODA tabela criada ou editada pelo Qoder DEVE ser gerada pela rotina `desenhar_tabela_ascii` abaixo.**
Nao ha alternativa. Nenhum outro metodo e aceito.

> Para diagramas de fluxo, ver `formatacao-diagramas.md`.

---

## Rotina Padrao de Geracao de Tabelas

```python
def desenhar_tabela_ascii(headers, rows):
    """
    Gera uma tabela ASCII a partir de cabecalhos e linhas.
    Usa bordas +, -, | — alinhamento perfeito em texto puro.
    """
    col_widths = [len(h) for h in headers]
    for row in rows:
        for i, cell in enumerate(row):
            col_widths[i] = max(col_widths[i], len(str(cell)))

    def linha_horizontal():
        return '+' + '+'.join('-' * (w + 2) for w in col_widths) + '+'

    tabela = linha_horizontal() + '\n'
    tabela += '|' + '|'.join(f' {headers[i].ljust(col_widths[i])} ' for i in range(len(headers))) + '|\n'
    tabela += linha_horizontal() + '\n'

    for row in rows:
        tabela += '|' + '|'.join(f' {str(row[i]).ljust(col_widths[i])} ' for i in range(len(row))) + '|\n'
    tabela += linha_horizontal()

    return tabela
```

---

## Exemplo de Uso

```python
headers = ["Tipo", "LABWEB7Empresas", "Banco do Cliente"]
rows = [
    ["OWNER/ADM", "EmpresaCliente + Emails", "Senhas + UsuariosWeb"],
    ["Usuario comum", "Apenas Emails", "Senhas + UsuariosWeb"],
]
print(desenhar_tabela_ascii(headers, rows))
```

**Saida:**
```
+---------------+---------------------------+----------------------+
| Tipo          | LABWEB7Empresas           | Banco do Cliente     |
+---------------+---------------------------+----------------------+
| OWNER/ADM     | EmpresaCliente + Emails   | Senhas + UsuariosWeb |
+---------------+---------------------------+----------------------+
| Usuario comum | Apenas Emails             | Senhas + UsuariosWeb |
+---------------+---------------------------+----------------------+
```

---

## Regras de Aplicacao

### 1. Geracao sempre via Python

- Criar um script `.py` temporario com os dados da tabela e a rotina acima
- Executar com `python script.py > output.txt`
- Ler `output.txt` e aplicar o resultado no documento via `search_replace`
- Deletar os arquivos temporarios ao final

### 2. Formato de saida

- Bordas ASCII: `+`, `-`, `|`
- Primeira linha da tabela = **cabecalho**
- Cada linha de dado separada por linha horizontal
- Colunas calculadas pelo **maior conteudo** de cada coluna

### 3. Blocos de codigo no Markdown

Toda tabela gerada deve ser envolta em bloco de codigo para preservar o alinhamento:

````markdown
```
+-------+--------+--------------------+
|Campo  |Tipo    |Descricao           |
+-------+--------+--------------------+
|`Id`   |int     |PK auto-incremento  |
+-------+--------+--------------------+
```
````

### 4. Emojis

- **Nao usar emojis dentro de celulas de tabela**
- Emojis tem largura visual de 2 caracteres mas sao contados como 1 pelo Python,
  o que causa desalinhamento
- Se necessario, usar texto equivalente: `OK`, `ERRO`, `SIM`, `NAO`

---

## Workflow Completo

```
1. Identificar tabela a criar/corrigir no documento
2. Montar estrutura: headers = [...] e rows = [[...], ...]
3. Criar script temporario format_tables.py com a rotina + dados
4. Executar: python format_tables.py > tables_output.txt
5. Ler tables_output.txt
6. Aplicar resultado no documento via search_replace (dentro de bloco ```)
7. Deletar format_tables.py e tables_output.txt
```

---

## O que NAO fazer

- Criar tabelas manualmente com `| col | col |` e calcular espacos a mao
- Usar separadores `|---|---|` do Markdown padrao
- Deixar tabelas sem bloco de codigo (o alinhamento se perde no render Markdown)
- Usar emojis nas celulas
- Usar `format_table()` ou `calc_col_widths()` — rotinas obsoletas

---

**Steering atualizado por Qoder — 21/04/2026**
*Padrao unico: `desenhar_tabela_ascii` — ASCII puro, sem PNG, sem Mermaid*
