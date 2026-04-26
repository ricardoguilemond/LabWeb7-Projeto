---
trigger: always
description: Steering de Geracao de Diagramas de Fluxo para LabWeb7 — padrao unico ASCII via desenhar_fluxograma_ascii
---

# Steering de Geracao de Diagramas de Fluxo - Qoder

## Regra Principal

**TODO diagrama de fluxo criado ou editado pelo Qoder DEVE ser gerado pela rotina `desenhar_fluxograma_ascii` abaixo.**
A saida e texto ASCII puro, incorporado no Markdown dentro de bloco ` ``` `.
Nao ha alternativa. Nenhum outro metodo e aceito.

> Para tabelas, ver `formatacao-tabelas.md`.

---

## Rotina Padrao — `desenhar_fluxograma_ascii`

```python
def desenhar_fluxograma_ascii(edges):
    """
    Gera um fluxograma ASCII simples a partir de conexoes (edges).

    Parametros:
      edges : lista de tuplas (no_origem, no_destino)

    Cada no e desenhado como:
      +------------------+
      | Nome do no       |
      +------------------+

    Nos conectados por:
         |
         v

    Para ramificacoes (decisoes com SIM/NAO), o diagrama deve ser
    construido manualmente com indentacao e labels SIM:/NAO:.
    """
    fluxo = ""
    for i, (src, dst) in enumerate(edges):
        fluxo += f"+{'-' * (len(src) + 2)}+\n"
        fluxo += f"| {src} |\n"
        fluxo += f"+{'-' * (len(src) + 2)}+\n"
        fluxo += "   |\n   v\n"
        fluxo += f"+{'-' * (len(dst) + 2)}+\n"
        fluxo += f"| {dst} |\n"
        fluxo += f"+{'-' * (len(dst) + 2)}+\n"
        if i < len(edges) - 1:
            fluxo += "   |\n   v\n"
    return fluxo
```

---

## Fluxos com Ramificacao (decisoes SIM/NAO)

Para fluxos que contem decisoes (nos com `?`), o output linear do
`desenhar_fluxograma_ascii` nao mostra ramificacao. Nesses casos,
construir o diagrama manualmente usando:

- `+--+` caixas com `| texto |`
- `|` e `v` para setas verticais
- `+==>` para ramificacao lateral (caminho NAO)
- `NAO:` e `SIM:` como labels nos ramos
- Indentacao progressiva para sub-ramos

---

## Convencao de Nomenclatura dos Nos

- Texto curto e descritivo: `"Email encontrado?"`, `"BCrypt.Verify"`
- Evitar acentos nos nomes dos nos
- Decisoes terminam com `?`: `"Senha valida?"`, `"OWNER encontrado?"`
- Erros prefixam com `ERRO:`: `"ERRO: Acesso nao autorizado"`
- Nao usar emojis nos nos
- Nomes de metodos: `"HomeController.ContinuarLogin"`

---

## Incorporar no Markdown

```markdown
### X.Y Diagrama de Fluxo (ASCII)

```
+--------+
| Inicio |
+--------+
   |
   v
+--------+
| Etapa  |
+--------+
```
```

Sempre envolver o diagrama em bloco de codigo ` ``` ` para preservar alinhamento.

---

## Workflow Completo

```
1. Identificar o fluxo a representar (etapas, decisoes, erros, saidas)
2. Montar lista de arestas: [("no_origem", "no_destino"), ...]
3. Se o fluxo NAO tem ramificacao: usar desenhar_fluxograma_ascii(edges)
4. Se o fluxo TEM ramificacao: construir manualmente com caixas + SIM/NAO
5. Envolver em bloco ``` no Markdown
6. Deletar arquivos temporarios
```

---

## O que NAO fazer

- Usar **Mermaid** (` ```mermaid `) — estilo proibido; layout incontrolavel, inconsistente entre renderizadores
- Gerar diagramas como imagem PNG — imagens nao ficam dentro do documento .md
- Usar `matplotlib`, `networkx`, `phart` ou qualquer biblioteca grafica
- Desenhar diagramas com caracteres Unicode de box-drawing
- Usar emojis nos nos
- Deixar diagrama sem bloco de codigo ``` — alinhamento se perde

---

**Steering atualizado por Qoder — 21/04/2026**
*Padrao unico: `desenhar_fluxograma_ascii` — ASCII puro, sem PNG, sem Mermaid*