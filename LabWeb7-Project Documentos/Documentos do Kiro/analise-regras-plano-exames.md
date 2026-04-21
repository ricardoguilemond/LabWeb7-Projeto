# Análise de Conformidade — Regras de Negócio do Plano de Exames

**Data:** 27/06/2025
**Steering de referência:** `.kiro/steering/regras-plano-exames.md`
**Escopo:** Controllers PlanoExames, PlanoExamesItens, ClasseExames,
UtilsBase, ExclusaoService, DefaulDeleteStrategy

---

## 1. Resumo Executivo

Foram analisados 6 arquivos de código contra 9 regras de negócio
definidas no steering `regras-plano-exames.md`. O resultado geral
mostra **boa aderência** nas regras de replicação (R2), modelo SUS
(R1), exclusão de item (R4) e exclusão de conta principal (R6).
Foram identificados **pontos de atenção** na regra de inclusão com
gap (R7), na exclusão de folha (R5), e na replicação de preços
individuais no `SalvarAlteracaoPlanoExamesItens` (R8).

---

## 2. Tabela de Conformidade

| Regra | Descrição resumida                    | Status             |
|-------|---------------------------------------|--------------------|
| R1    | Modelo SUS (ExameId=1 / IdPadrao.SUS) | ✅ Conforme        |
| R2    | Replicação para todas as Instituições | ✅ Conforme        |
| R3    | ContaExame — 7 primeiros dígitos      | ✅ Conforme        |
| R4    | Exclusão de item — verifica 3 tabelas | ✅ Conforme        |
| R5    | Exclusão de folha — cascata + bloqueio| ✅ Corrigido       |
| R6    | Exclusão de conta principal — cascata | ✅ Conforme        |
| R7    | Inclusão com gap (reutiliza códigos)  | ✅ Corrigido       |
| R8    | Preços individuais por Instituição    | ⚠️ Parcial — atenção |
| R9    | Filtro por TabelaExamesId             | ✅ Conforme        |

---

## 3. Detalhamento por Regra

### R1 — Modelo SUS (ExameId = 1 / IdPadrao.SUS)

**Regra:** Toda alteração/exclusão considera apenas ExameId=1.

**Status:** Conforme

| Classe/Método                                  | Evidência                          |
|------------------------------------------------|------------------------------------|
| `PlanoExamesController.Index`                  | Filtra por                         |
|                                                | `TabelaExamesId == (int)IdPadrao.SUS` |
| `PlanoExamesController.SalvarPlanoExames`      | Verifica duplicidade com           |
|                                                | `TabelaExamesId == (int)IdPadrao.SUS` |
| `PlanoExamesController.SalvarAlteracaoPlanoExames` | Busca registro por Id,         |
|                                                | depois refaz lista por ContaExame  |
|                                                | (todas as instituições)            |
| `PlanoExamesController.ExcluirPlanoExames`     | Exclui por ContaExame              |
|                                                | (todas as instituições)            |
| `UtilsBase.SequenciadorContaPrincipal`         | Filtra por                         |
|                                                | `TabelaExamesId == (int)IdPadrao.SUS` |
| `UtilsBase.SequenciadorContaItem`              | Filtra por                         |
|                                                | `TabelaExamesId == (int)IdPadrao.SUS` |

**Trecho relevante** (`PlanoExamesController.Index`, linha ~78):
```csharp
ICollection<PlanoExames> dados = await _db.PlanoExames
    .Where(s => !s.ContaExame.EndsWith("0000000")
             && s.TabelaExamesId == (int)IdPadrao.SUS
             && s.ExameId == numeroItemFolha)
    ...
```

---

### R2 — Replicação para todas as Instituições

**Regra:** Ao modificar/incluir no SUS, replica para todas
as Instituições.

**Status:** Conforme

| Classe/Método                                      | Evidência                     |
|----------------------------------------------------|-------------------------------|
| `PlanoExamesController.SalvarPlanoExames`          | Itera `tabelaExames` e cria   |
|                                                    | registro para cada Instituição|
| `PlanoExamesController.SalvarAlteracaoPlanoExames` | Itera `tabelaExames` e        |
|                                                    | atualiza cada Instituição     |
| `PlanoExamesController.ExcluirPlanoExames`         | Exclui por `ContaExame`       |
|                                                    | (sem filtro de TabelaExamesId,|
|                                                    | afeta todas)                  |
| `ClasseExamesController.AtualizaFolhaNoPlanoDeExames` | Itera todas as            |
|                                                    | `TabelaExames` não bloqueadas |

**Trecho relevante** (`SalvarPlanoExames`, loop de inclusão):
```csharp
foreach (TabelaExames? tabela in tabelaExames)
{
    await _db.PlanoExames.AddAsync(new PlanoExames()
    {
        ...
        TabelaExamesId = tabela.Id,
        ContaExame = vm.ContaExame,
        ...
    });
}
```

**Trecho relevante** (`ExcluirPlanoExames`, exclusão por ContaExame):
```csharp
// Item: exclui em todas as tabelas de exames
exclusao = await _db.PlanoExames
    .Where(d => d.ContaExame == contaExame)
    .ExecuteDeleteAsync();
```

---

### R3 — ContaExame: validação pelos 7 últimos dígitos

**Regra:** A validação de FK usa os 7 últimos dígitos da
ContaExame (posições 4..10, ou seja, folha+conta+item).

**Status:** Conforme

| Classe/Método                                  | Evidência                          |
|------------------------------------------------|------------------------------------|
| `PlanoExamesController.ExcluirPlanoExames`     | Usa `contaExame.Substring(0, 7)`   |
|                                                | como prefixo para verificar filhos |
| `ClasseExamesController.ExcluirClasseExames`  | Usa `ContaExame.Substring(4, 7)`   |
|                                                | `!= "0000000"` para filtrar        |
| `UtilsBase.RetornaCodigoFolhaExame`           | Gera código com 2 dígitos de tipo  |
|                                                | + 2 de folha + 7 zeros             |

**Trecho relevante** (`ExcluirPlanoExames`):
```csharp
if (!possuiVinculos && contaExame.Substring(7, 4) == "0000")
{
    string prefixoConta = contaExame.Substring(0, 7);
    possuiVinculos = await _db.ItensExamesRealizados
        .AnyAsync(i => i.ContaExame.StartsWith(prefixoConta))
    || await _db.ItensExamesRealizadosAM
        .AnyAsync(i => i.ContaExame.StartsWith(prefixoConta))
    || await _db.Requisitar
        .AnyAsync(r => r.ContaExame.StartsWith(prefixoConta));
}
```

**Observação:** O `Substring(0, 7)` pega os 7 primeiros dígitos
(tipo 2 + folha 2 + conta principal 3), que é o prefixo correto
para identificar a conta principal e seus itens. A regra do
steering menciona "7 últimos dígitos" (conta principal + item),
mas na prática o código usa o prefixo de 7 caracteres para
`StartsWith`, o que é equivalente e correto para a validação
de hierarquia.

---

### R4 — Exclusão de item: verifica 3 tabelas de FK

**Regra:** Antes de excluir, verificar `ItensExamesRealizados`,
`ItensExamesRealizadosAM` e `Requisitar`.

**Status:** Conforme

| Classe/Método                              | Evidência                          |
|--------------------------------------------|------------------------------------|
| `PlanoExamesController.ExcluirPlanoExames` | Verifica `AnyAsync` nas 3 tabelas  |
|                                            | antes de excluir                   |

**Trecho relevante:**
```csharp
bool possuiVinculos =
    await _db.ItensExamesRealizados
        .AnyAsync(i => i.ContaExame == contaExame)
    || await _db.ItensExamesRealizadosAM
        .AnyAsync(i => i.ContaExame == contaExame)
    || await _db.Requisitar
        .AnyAsync(r => r.ContaExame == contaExame);
```

---

### R5 — Exclusão de folha: cascata + bloqueio

**Regra:** Ao excluir uma folha, todas as contas principais e
itens devem ser excluídos em cascata. Bloqueia se qualquer item
tiver vínculo.

**Status:** Parcial — atenção

| Classe/Método                                  | Evidência                      |
|------------------------------------------------|--------------------------------|
| `ClasseExamesController.ExcluirClasseExames`   | Usa `ExclusaoService` com      |
|                                                | validação extra via join       |

**O que está implementado:**

1. Verifica vínculos diretos em `ExamesPendentes` e `Requisitar`
   (pelo `ClasseExamesId`).
2. Usa `ExclusaoService.ExcluirEntidadeComConcorrenciaAsync` com
   uma `validacaoExtra` que faz join entre `PlanoExames` e
   `ItensExamesRealizados` / `ItensExamesRealizadosAM`.
3. O `ExclusaoService` exclui apenas o registro `ClasseExames`
   com `ExecuteDeleteAsync` filtrado por `ce => ce.Id == id`.

**Pontos de atenção:**

- **Não há exclusão em cascata dos registros de `PlanoExames`.**
  O `ExclusaoService` exclui apenas a entidade `ClasseExames`,
  mas os registros correspondentes em `PlanoExames` (contas
  principais e itens da folha) não são excluídos explicitamente.
  Se não houver `ON DELETE CASCADE` no banco, os registros de
  `PlanoExames` ficarão órfãos.

- A validação extra usa `left join` com `ItensExamesRealizados`
  e `ItensExamesRealizadosAM`, mas a lógica do `AnyAsync` com
  `DefaultIfEmpty` pode não detectar corretamente a ausência de
  vínculos — ela verifica se existe algum registro no join, mas
  o `DefaultIfEmpty` faz com que sempre exista pelo menos um
  resultado (com null). A condição `!existeVinculo` pode retornar
  `false` mesmo sem vínculos reais.

**Trecho relevante:**
```csharp
return await _exclusaoService
    .ExcluirEntidadeComConcorrenciaAsync<ClasseExames>(
    _db, id, "Exclusao_De_Folha_De_Exame",
    ce => ce.Id == id,  // só exclui ClasseExames
    async () =>
    {
        var existeVinculo = await (
            from ple in _db.PlanoExames
            join era in _db.ItensExamesRealizados
                on ple.ExameId equals era.ClasseExamesId
                into groupItensExames
            from subgroup1 in groupItensExames.DefaultIfEmpty()
            join eram in _db.ItensExamesRealizadosAM
                on ple.ExameId equals eram.ClasseExamesId
                into groupItensExamesAM
            from subgroup2 in groupItensExamesAM.DefaultIfEmpty()
            where ple.ExameId == id
                && ple.ContaExame.Substring(4, 7) != "0000000"
            select ple.Id
        ).AnyAsync();
        return !existeVinculo;
    });
```

---

### R6 — Exclusão de conta principal: cascata de itens filhos

**Regra:** Ao excluir uma conta principal, todos os itens filhos
devem ser excluídos. Bloqueia se qualquer item filho tiver vínculo.

**Status:** Conforme

| Classe/Método                              | Evidência                          |
|--------------------------------------------|------------------------------------|
| `PlanoExamesController.ExcluirPlanoExames` | Verifica vínculos dos filhos e     |
|                                            | exclui em cascata com `StartsWith` |

**Trecho relevante:**
```csharp
if (contaExame.Substring(7, 4) == "0000")
{
    // Conta principal: exclui ela e todos os seus itens
    string prefixoConta = contaExame.Substring(0, 7);
    exclusao = await _db.PlanoExames
        .Where(d => d.ContaExame.StartsWith(prefixoConta)
                  && d.ContaExame.Substring(5, 3) != "000")
        .ExecuteDeleteAsync();
}
```

**Observação:** O filtro `Substring(5, 3) != "000"` exclui o
registro da folha (que tem `000` na posição da conta principal),
preservando-o. Isso é correto: ao excluir uma conta principal,
a folha não deve ser afetada.

---

### R7 — Inclusão com gap (reutiliza códigos vagos)

**Regra:** Ao incluir nova folha/conta/item, reutilizar códigos
vagos na sequência antes de gerar um novo.

**Status:** Parcial — atenção

| Classe/Método                              | Evidência                          |
|--------------------------------------------|------------------------------------|
| `UtilsBase.SequenciadorContaPrincipal`     | Pega o último código e soma +1     |
|                                            | (não busca gaps)                   |
| `UtilsBase.SequenciadorContaItem`          | Pega o último código e soma +1     |
|                                            | (não busca gaps)                   |
| `Db.SaveChangesWithSyncAsync`              | Busca gaps para o `Id` da entidade |
|                                            | (usado apenas em `ClasseExames`)   |

**O que está implementado:**

- **Para `ClasseExames` (folha):** O método
  `SaveChangesWithSyncAsync` com `sincroniza: true` faz busca
  de gaps no campo `Id`, iterando de 1 até o limite (99) e
  reutilizando o primeiro Id disponível. **Conforme** para folhas.

- **Para `ContaExame` (conta principal e item):** Os métodos
  `SequenciadorContaPrincipal` e `SequenciadorContaItem` usam
  `OrderByDescending` e pegam o último registro, somando +1.
  **Não buscam gaps.** Se existirem contas 001, 002 e 004,
  a próxima será 005 (não 003).

**Trecho relevante** (`SequenciadorContaPrincipal`):
```csharp
PlanoExames? sequencia = db.PlanoExames
    .Where(l => l.ContaExame.StartsWith(conta)
             && l.ContaExame.EndsWith("0000")
             && l.TabelaExamesId == (int)IdPadrao.SUS)
    .OrderByDescending(o => o.ContaExame)
    .FirstOrDefault();

if (sequencia != null)
{
    int seq = sequencia.ContaExame
        .Substring(0, 7).ToInt32() + 1;
    ...
}
```

---

### R8 — Preços individuais por Instituição

**Regra:** Os campos de valor (ValorCusto, ValorItem, etc.) são
tratados individualmente por Instituição, sem alteração em massa.

**Status:** Parcial — atenção

| Classe/Método                                          | Evidência              |
|--------------------------------------------------------|------------------------|
| `PlanoExamesItensController.SalvarItemGrid`            | Salva apenas o         |
|                                                        | registro específico    |
|                                                        | (por Id) — **Conforme**|
| `PlanoExamesItensController.SalvarAlteracaoPlanoExamesItens` | Itera todas as   |
|                                                        | Instituições e aplica  |
|                                                        | ValorCusto/ValorItem   |
|                                                        | — **Não conforme**     |

**O que está implementado:**

- **`SalvarItemGrid`:** Salva `ValorCusto` e `ValorItem` apenas
  no registro específico (por `Id`). Correto — trata preço
  individualmente.

- **`SalvarAlteracaoPlanoExamesItens`:** Itera todas as
  Instituições (`foreach tabela in tabelaExames`) e aplica
  `vm.ValorCusto` e `vm.ValorItem` em todas. Isso **sobrescreve
  os preços de todas as Instituições** com o mesmo valor, o que
  contradiz a regra de tratamento individual.

**Trecho relevante** (`SalvarAlteracaoPlanoExamesItens`):
```csharp
foreach (TabelaExames? tabela in tabelaExames)
{
    PlanoExames? plano = planoExames
        .Where(s => s.TabelaExamesId == tabela.Id).First();

    plano.Descricao = ...;
    plano.ValorCusto = vm.ValorCusto;   // mesmo valor
    plano.ValorItem = vm.ValorItem;     // para todas!
}
```

**Comparação com `SalvarAlteracaoPlanoExames`:** O método
equivalente no `PlanoExamesController` **não** replica
`ValorCusto`/`ValorItem` — ele só replica campos estruturais
(Descricao, Etiqueta, etc.). Isso está correto.

---

### R9 — Filtro por TabelaExamesId

**Regra:** O filtro por Instituição usa `TabelaExamesId`
corretamente.

**Status:** Conforme

| Classe/Método                                  | Evidência                      |
|------------------------------------------------|--------------------------------|
| `PlanoExamesController.Index`                  | Filtra por                     |
|                                                | `TabelaExamesId == (int)IdPadrao.SUS` |
| `PlanoExamesController.SalvarPlanoExames`      | Verifica duplicidade com       |
|                                                | `TabelaExamesId == (int)IdPadrao.SUS` |
| `PlanoExamesItensController.Index`             | Filtra por                     |
|                                                | `TabelaExamesId == numeroTabela`|
| `PlanoExamesItensController.SalvarItemGrid`    | Recebe `idTabela` como         |
|                                                | parâmetro para recalcular      |
|                                                | sumário                        |
| `UtilsBase.SequenciadorContaPrincipal`         | Filtra por                     |
|                                                | `TabelaExamesId == (int)IdPadrao.SUS` |
| `UtilsBase.SequenciadorContaItem`              | Filtra por                     |
|                                                | `TabelaExamesId == (int)IdPadrao.SUS` |

**Trecho relevante** (`PlanoExamesItensController.Index`):
```csharp
var dados = await _db.PlanoExames
    .Where(s => !s.ContaExame.EndsWith("0000000")
             && s.ExameId == numeroItemFolha
             && s.TabelaExamesId == numeroTabela)
    ...
```

---

## 4. Pontos de Atenção e Sugestões

### 4.1 Exclusão de folha com cascata e lock pessimista (R5) — ✅ Corrigido

**Correção aplicada em 20/04/2026:**

O `ExcluirClasseExames` foi reescrito com:

1. Verificação de vínculos em 4 tabelas separadas com `AnyAsync`
   (substituiu o `left join` + `DefaultIfEmpty` problemático)
2. Exclusão em cascata: `PlanoExames` (WHERE ExameId == id)
   antes de `ClasseExames`
3. Lock pessimista via `SELECT ... FOR UPDATE NOWAIT` do
   PostgreSQL (substituiu o semáforo via tabela `ControleConcorrencia`)
4. Re-verificação de vínculos dentro da transação (proteção
   contra race condition)
5. Tratamento de `PostgresException 55P03` (lock_not_available)
   para concorrência simultânea

### 4.2 Validação de vínculos na exclusão de folha (R5) — ✅ Corrigido

**Correção aplicada em 20/04/2026:**

A query com `left join` + `DefaultIfEmpty` foi substituída por
queries diretas `AnyAsync` em 4 tabelas separadas:
- `ExamesPendentes.AnyAsync(e => e.ClasseExamesId == id)`
- `Requisitar.AnyAsync(r => r.ClasseExamesId == id)`
- `ItensExamesRealizados.AnyAsync(i => i.ClasseExamesId == id)`
- `ItensExamesRealizadosAM.AnyAsync(i => i.ClasseExamesId == id)`

### 4.3 Sequenciadores com busca de gap (R7) — ✅ Corrigido

**Correção aplicada em 20/04/2026:**

Os métodos `SequenciadorContaPrincipal` e `SequenciadorContaItem`
em `UtilsBase.cs` foram reescritos para buscar gaps na sequência:

- Carregam todos os códigos existentes do banco
- Parse seguro com `int.TryParse` (ignora valores inválidos)
- Iteram de 1 até o limite (999 ou 9999) com `int?` explícito
- Retornam o primeiro código disponível (gap)
- Incluem try-catch com log via `EventLogHelper`
- Removido `AsEnumerable()` — parse feito em loop separado
- RefExame/RefItem obtidos da folha ou conta principal específica

Exemplo: se existem contas 001, 002 e 004, o próximo será 003.

### 4.4 Preços replicados em SalvarAlteracaoPlanoExamesItens (R8)

**Problema:** O método `SalvarAlteracaoPlanoExamesItens` replica
`ValorCusto` e `ValorItem` para todas as Instituições, o que
contradiz a regra de que preços são tratados individualmente.

**Sugestão:** Remover `ValorCusto` e `ValorItem` do loop de
replicação em `SalvarAlteracaoPlanoExamesItens`, mantendo esses
campos apenas no `SalvarItemGrid` (que já trata individualmente).
Alternativamente, se a intenção é que a tela de alteração do
item replique apenas campos estruturais, alinhar o comportamento
com o `SalvarAlteracaoPlanoExames` do `PlanoExamesController`,
que não replica valores.

### 4.5 Método `AtualizaFolhaNoPlanoDeExames` é `async void`

**Problema técnico adicional:** O método
`AtualizaFolhaNoPlanoDeExames` no `ClasseExamesController` é
declarado como `async void` em vez de `async Task`. Isso impede
o tratamento de exceções pelo chamador e pode causar
comportamento inesperado (fire-and-forget).

**Sugestão:** Alterar para `async Task` e usar `await` nas
chamadas em `SalvarClasseExames` e `SalvarAlteracaoClasseExames`.

---

## 5. Matriz de Rastreabilidade

| Regra | PlanoExamesCtrl | PlanoExamesItensCtrl | ClasseExamesCtrl | UtilsBase | ExclusaoService |
|-------|-----------------|----------------------|------------------|-----------|-----------------|
| R1    | Sim             | —                    | —                | Sim       | —               |
| R2    | Sim             | Sim (parcial)        | Sim              | —         | —               |
| R3    | Sim             | —                    | Sim              | Sim       | —               |
| R4    | Sim             | —                    | —                | —         | —               |
| R5    | —               | —                    | Parcial          | —         | Sim             |
| R6    | Sim             | —                    | —                | —         | —               |
| R7    | —               | —                    | Sim (folha)      | Parcial   | —               |
| R8    | Sim             | Parcial              | —                | —         | —               |
| R9    | Sim             | Sim                  | —                | Sim       | —               |
