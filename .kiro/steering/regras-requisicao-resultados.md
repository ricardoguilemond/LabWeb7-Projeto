---
inclusion: always
description: Regras de negócio — Requisição de Exames, Expansão de Itens e Lançamento de Resultados
---

# Steering — Requisição e Lançamento de Resultados

## Estrutura da ContaExame (11 dígitos)

```
XX . XX . XXX . XXXX
│    │    │      └─ Item (0001-9999) ou 0000 = Principal
│    │    └──────── Conta principal (001-999) ou 000 = Folha
│    └───────────── Folha de exame (01-99)
└────────────────── Tipo fixo (11)
```

Exemplos:
- `11020000000` → Folha geral BACTERIOSCOPIA (posições 5-11 = "0000000")
- `11020010000` → Item **Principal** BACTERIOSCOPIA (últimos 4 = "0000", posições 5-7 > "000")
- `11020010001` → Sub-item Material Analisado (últimos 4 > "0000")

## Regra de Valor/Preço

### Cenário 1 — Valor no Principal (0000)

- O Principal agrupa sub-itens (>0000)
- O valor/preço refere-se ao conjunto todo
- Cada sub-item receberá resultado de exame individualmente
- O paciente paga pelo Principal; resultados são lançados nos sub-itens

### Cenário 2 — Valor nos Itens (>0000)

- O Principal (0000) é apenas agrupador (sem valor)
- Cada item (>0000) com valor é cobrado individualmente
- Cada item com valor receberá resultado de exame

### Regra Universal

- Itens (>0000) são SEMPRE os que recebem resultados de exame
- O Principal (0000) NUNCA recebe resultado — é agrupador ou portador de preço

## Expansão Automática na Requisição

Ao salvar uma requisição, quando o item do cupom é um **Principal**
(ContaExame termina em "0000"):

1. Buscar no PlanoExames todos os sub-itens com mesmo prefixo de 7 dígitos
   e mesmo TabelaExamesId
2. Inserir o Principal na `ItensExamesRealizados` (com seu valor)
3. Inserir cada sub-item expandido (receberá resultado)
4. Sub-itens herdam: ClasseExamesId, ClasseExamesNome, ExameRealizadoId,
   InstituicaoId, Sequencial do Principal

### No Cupom

- Apenas o Principal aparece com seu preço
- Sub-itens NÃO aparecem no cupom — são expandidos internamente

## Lançamento de Resultados

### Filtro do Grid Header (ExamesRealizados)

```csharp
.Where(e => e.Liberacao == 0 && e.Baixado == 0)
```

### Filtro dos Itens (ItensExamesRealizados)

Exclui **apenas** a Folha geral (posições 5-11 = "0000000"):

```csharp
.Where(i => i.ContaExame.Substring(4, 7) != "0000000")
```

Equivalente SQL Delphi:
```sql
AND SUBSTRING("ContaExame" FROM 5 FOR 7) <> '0000000'
```

### Comportamento Visual dos Itens

| Tipo | Condição | Visual | Editável |
|------|----------|--------|----------|
| Folha geral | Posições 5-11 = "0000000" | **Excluído** (não aparece) | — |
| Principal | Últimos 4 = "0000" E pos 5-7 > "000" | Laranja/bold (cabeçalho) | ❌ Não |
| Sub-item | Últimos 4 > "0000" | Normal | ✅ Sim |

### Navegação com ENTER

- Ao pressionar ENTER no campo Resultado: salva e avança
- Pula automaticamente linhas de Principal (últimos 4 = "0000")
- Foco no próximo campo Resultado editável

### Ordenação dos Itens

- Grid de itens sempre ordenado por `ContaExame ASC`
- Garante agrupamento visual: Principal → seus sub-itens

### Agrupamento Visual Automático

Quando a Folha muda entre itens consecutivos e não há um Principal
como separador, o frontend insere automaticamente um cabeçalho
de grupo (azul) com o nome da Folha.

## Exibição Inteligente de Exames (Tela Pacientes)

Regra: `MAX(últimos 4 exames, exames nos últimos 90 dias) LIMIT 8`

- Mínimo: 4 exames
- Janela temporal: 90 dias
- Máximo: 8 exames
- Indicador clicável: expande para últimos 12 meses
- Link "Recolher": volta ao estado normal

## Ordenação Padrão

| Tela | Ordenação |
|------|-----------|
| Consultar Exames | `DataIni DESC, Id DESC` |
| Resultado de Exames (header) | `DataIni DESC, Id DESC` |
| Resultado de Exames (itens) | `ContaExame ASC` |
| Pacientes (detail) | `DataIni DESC, Id DESC` |

## Tabela de Situação do Exame (SituacaoExames)

| Código | Descrição | Cor no grid | Significado |
|--------|-----------|-------------|-------------|
| 0 | Pendente | Vermelho | Requisição criada, aguarda análise |
| 1 | Em Análise | Azul | Primeiro resultado lançado |
| 2 | Resultado Online | Verde | Resultado disponível para consulta |
| 3 | Impresso | Verde escuro | Laudo impresso |
| 4 | Arquivo-morto | — | Baixado para AM |
| 5 | A Repetir | Laranja | Exame precisa ser repetido |
| 6 | Material Inválido | Vermelho escuro | Material invalidado |
| 7 | Pend. Cadastral | Cinza | Pendente de informação cadastral |
| 11 | Baixando... | Cinza | Lock temporário durante baixa |

### Transições implementadas no .NET

- Requisição criada → `Situacao = 0`
- Primeiro resultado lançado → `Situacao = 1` (melhoria sobre Delphi)
- Impressão de laudo → `Situacao = 3`
- Baixa para AM → `Situacao = 4`
