---
trigger: manual
description: Regras de Negócio do Plano de Exames e Folha de Exames para LabWeb7
---

# Steering de Plano de Exames - Qoder

## Contexto

Na tela de **Folha de Exames**, os itens exibidos são filtrados com base apenas no **Plano de Exames SUS**, que funciona como um modelo replicado para os demais planos de exames de cada Instituição.

## Regras do Modelo SUS

### SUS é o Template Base
- ✅ Toda rotina de alteração/edição de itens do plano, ou de exclusão do plano, deve **SEMPRE** considerar apenas o **Plano de Exames modelo (SUS)**
- ✅ O campo `ExameId = 1` define os itens que pertencem ao SUS (modelo)
- ✅ No código, usar `(int)IdPadrao.SUS` para referenciar
- ✅ Ao modificar ou excluir um item no modelo SUS, essa modificação/exclusão deve ser aplicada **AUTOMATICAMENTE** em todos os planos das demais Instituições

### Exemplo de Código
```csharp
// Verificar se é modelo SUS
if (plano.ExameId == (int)IdPadrao.SUS)  // IdPadrao.SUS = 1
{
    // Replicar para TODAS as instituições
    var todasInstituicoes = await _db.TabelaExames.ToListAsync();
    
    foreach (var instituicao in todasInstituicoes)
    {
        var planoInst = await _db.PlanoExames
            .FirstOrDefaultAsync(p => p.ContaExame == plano.ContaExame 
                                   && p.TabelaExamesId == instituicao.Id);
        
        if (planoInst != null)
        {
            planoInst.Descricao = plano.Descricao;
            planoInst.ValorItem = plano.ValorItem;
            // ... outros campos
        }
    }
}
```

## Estrutura da ContaExame

### Formato: 11 Dígitos
```
XX . XX . XXX . XXXX
│    │    │     │
│    │    │     └─ Item específico (0001-9999)
│    │    └─────── Conta principal (001-999)
│    └──────────── Folha de exame (01-99)
└───────────────── Tipo (11=crédito, fixo em 11)
```

### Exemplos
```
11.01.000.0000  → Folha 01
11.01.001.0000  → Conta principal 001 da folha 01
11.01.001.0001  → Item 0001 da conta principal 001
11.01.001.0002  → Item 0002 da conta principal 001
```

### Hierarquia
- **Folha de Exame:** Segunda dezena (01-99) = `11.01.000.0000`
- **Conta Principal:** Termina em `0000` = `11.01.001.0000`
- **Item:** Últimos 4 dígitos > 0 = `11.01.001.0001`

## Validação por Prefixo (7 dígitos)

### Regra
- ✅ A validação da `ContaExame` do modelo é usada para verificar as FKs nas tabelas relacionadas
- ✅ A replicação e validação devem considerar os **7 primeiros dígitos** da `ContaExame` (tipo + folha + conta principal)
- ✅ Formam o **prefixo hierárquico**

### Implementação
```csharp
// Extrair prefixo de 7 dígitos
var prefixo = contaExame.Substring(0, 7);  // "11.01.001"

// Verificar se existe em tabelas relacionadas
var existeEmExamesRealizados = await _db.ItensExamesRealizados
    .AnyAsync(i => i.ContaExame.StartsWith(prefixo));

var existeEmRequisicoes = await _db.Requisitar
    .AnyAsync(r => r.ContaExame.StartsWith(prefixo));

// Se existir em qualquer tabela, item é válido em TODAS instituições
if (existeEmExamesRealizados || existeEmRequisicoes)
{
    // Item válido - não pode excluir
}
```

## Regras de Exclusão

### ❌ BLOQUEAR Exclusão Se

**ContaExame existe em:**
- `ItensExamesRealizados`
- `ItensExamesRealizadosAM`
- `Requisitar`

### Exclusão de Folha de Exame
```csharp
// Ex: 11.01.000.0000
// Todas as contas principais e itens serão excluídos em cascata

var prefixoFolha = "11.01";  // Primeiro 5 dígitos

// Verificar SE QUALQUER item da folha está vinculado
var temVinculo = await _db.ItensExamesRealizados
    .AnyAsync(i => i.ContaExame.StartsWith(prefixoFolha));

if (temVinculo)
{
    return "Não é possível excluir a folha. Existem exames realizados vinculados.";
}

// Excluir toda a folha
var itensFolha = await _db.PlanoExames
    .Where(p => p.ContaExame.StartsWith(prefixoFolha))
    .ToListAsync();

_db.PlanoExames.RemoveRange(itensFolha);
```

### Exclusão de Conta Principal
```csharp
// Ex: 11.01.001.0000
// Todos os itens filhos serão excluídos junto

var prefixoConta = "11.01.001";  // Primeiro 7 dígitos

// Verificar se algum item filho está vinculado
var temVinculo = await _db.ItensExamesRealizados
    .AnyAsync(i => i.ContaExame.StartsWith(prefixoConta));

if (temVinculo)
{
    return "Não é possível excluir a conta. Existem exames realizados vinculados.";
}

// Excluir conta principal e todos os itens filhos
var itensConta = await _db.PlanoExames
    .Where(p => p.ContaExame.StartsWith(prefixoConta))
    .ToListAsync();

_db.PlanoExames.RemoveRange(itensConta);
```

### Regra de Ouro da Exclusão
- ✅ Basta **UM ÚNICO** item vinculado para impedir a exclusão da folha inteira
- ✅ A exclusão no modelo SUS exclui **AUTOMATICAMENTE** o mesmo item em todas as tabelas de preços das Instituições

## Regras de Inclusão

### Reutilizar Códigos Vagos (Gap Detection)

**Regra:** Ao incluir uma nova folha, conta principal ou item, o sistema deve verificar se existe um código **vago** (gap) na sequência **ANTES** de gerar um novo código.

### Exemplo - Folhas
```
Folhas existentes: 01, 02, 04, 05
                   ↑
                   Gap detectado!

Próxima inclusão deve usar: 03 (vago)
NÃO usar: 06
```

### Exemplo - Contas Principais
```
Contas existentes: 001, 002, 004, 005
                   ↑
                   Gap detectado!

Próxima inclusão deve usar: 003 (vago)
NÃO usar: 006
```

### Implementação
```csharp
// Encontrar gap na sequência
public async Task<int> EncontrarCodigoVagoAsync(string prefixo)
{
    var codigosExistentes = await _db.PlanoExames
        .Where(p => p.ContaExame.StartsWith(prefixo))
        .Select(p => p.ContaExame)
        .ToListAsync();
    
    for (int i = 1; i <= 999; i++)
    {
        var codigo = $"{prefixo}.{i:D3}.0000";
        if (!codigosExistentes.Contains(codigo))
        {
            return i;  // Gap encontrado
        }
    }
    
    // Não encontrou gap, retorna próximo
    return codigosExistentes.Count + 1;
}
```

### Aplica-se A:
- ✅ `ExameId` (Id da folha na tabela `ClasseExames`)
- ✅ `ContaExame` (código sequencial dentro do plano)

### Objetivo
- Evitar lacunas na numeração
- Manter a sequência compacta

## Regras por Instituição

### TabelaExamesId
- ✅ Cada Instituição possui seu próprio `TabelaExamesId`
- ✅ Serve como filtro para leitura da tabela correta de cada Instituição
- ✅ A tabela `TabelaExames` contém os Ids relacionados ao `TabelaExamesId` do `PlanoExames`
- ✅ Bem como os nomes das Instituições

### Correspondência SUS ↔ Instituição
```csharp
// A correspondência é feita pela ContaExame
// (mesma conta em diferentes TabelaExamesId)

var planoSUS = await _db.PlanoExames
    .FirstOrDefaultAsync(p => p.ExameId == (int)IdPadrao.SUS 
                           && p.ContaExame == "11.01.001.0001");

var planoInstituicao = await _db.PlanoExames
    .FirstOrDefaultAsync(p => p.TabelaExamesId == instituicaoId 
                           && p.ContaExame == "11.01.001.0001");
```

## Tratamento de Preços/Valores

### Dois Cenários de Edição

#### Cenário 1 — Preço Individual por Instituição
```csharp
// Edição inline no grid da Tabela de Preços
public async Task<IActionResult> SalvarItemGrid(int id, decimal valorItem)
{
    var plano = await _db.PlanoExames.FindAsync(id);
    plano.ValorItem = valorItem;
    
    // Salva APENAS este registro (por Id)
    await _db.SaveChangesAsync();
    
    // NÃO afeta outras Instituições
}
```

**Características:**
- ✅ Edição inline no grid
- ✅ Salva apenas o registro específico (por Id)
- ✅ Cada Instituição mantém seus próprios valores
- ✅ **NÃO** afeta outras Instituições

#### Cenário 2 — Preço em Massa para Todas as Instituições
```csharp
// Tela de alteração completa
public async Task<IActionResult> SalvarAlteracaoPlanoExamesItens(int planoId)
{
    var planoSUS = await _db.PlanoExames.FindAsync(planoId);
    
    // Replica para TODAS as instituições com mesma ContaExame
    var todasInstituicoes = await _db.TabelaExames.ToListAsync();
    
    foreach (var inst in todasInstituicoes)
    {
        var plano = await _db.PlanoExames
            .FirstOrDefaultAsync(p => p.ContaExame == planoSUS.ContaExame 
                                   && p.TabelaExamesId == inst.Id);
        
        if (plano != null)
        {
            plano.ValorCusto = planoSUS.ValorCusto;
            plano.ValorItem = planoSUS.ValorItem;
            plano.Descricao = planoSUS.Descricao;
            plano.Etiqueta = planoSUS.Etiqueta;
            // ... campos estruturais também replicados
        }
    }
    
    await _db.SaveChangesAsync();
}
```

**Características:**
- ✅ Tela de alteração completa (`AlterarPlanoExamesItens`)
- ✅ Replica `ValorCusto` e `ValorItem` para **TODAS** as Instituições
- ✅ Útil para definir um preço base igual em todas as tabelas
- ✅ Campos estruturais (Descricao, Etiqueta, etc.) **também** são replicados

### Regra Geral

| Tela | Cenário | Afeta |
|------|---------|-------|
| Tabela de Preços (grid inline) | Cenário 1 | Apenas instituição selecionada |
| Botão "Alterar" do grid | Cenário 2 | **TODAS** as instituições |

### ⚠️ Importante
- ❌ **Nenhum** dos cenários permite exclusão de registros
- ✅ Apenas **edição** de valores

## Resumo das Regras

### Modificar Modelo SUS (ExameId = 1)
```
Modificar SUS = Modificar TODOS os planos derivados
```

### Verificação da ContaExame
```
Validar pelos 7 primeiros dígitos (StartsWith)
Se houver correspondência em qualquer tabela relacionada → Operação válida para TODAS Instituições
```

### Filtro por Instituição
```
TabelaExamesId garante o filtro correto por Instituição
```

### Preços/Valores
```
Sempre tratados de forma individual (Cenário 1)
OU em massa para todas (Cenário 2)
```

## Checklist de Validação

Antes de operar no Plano de Exames:

```
□ É modelo SUS (ExameId = 1)?
  → Se SIM: replicar para TODAS instituições
□ ContaExame está usando validação por prefixo (7 dígitos)?
□ FKs verificadas antes de exclusão (ItensExamesRealizados, Requisitar)?
□ Gap detection implementado para inclusão?
□ Cenário correto de preço (individual vs em massa)?
□ NÃO está permitindo exclusão (apenas edição)?
□ TabelaExamesId usado para filtro correto?
```

---

**Steering criado por Qoder - 21/04/2026**  
*Baseado nas melhores práticas do projeto LabWeb7*
