# Análise de Métodos de Exclusão — LabWeb7

**Data da análise:** Julho/2025
**Fonte dos dados:** Controllers C# + DDL PostgreSQL (`Tabelas_Vazias.sql`)
**Modelo de referência:** `PacientesController.ExcluirPaciente` (já implementado)

---

## 1. Resumo Executivo

Foram analisados **6 controllers** contendo **8 métodos de exclusão**.
Destes, **2 métodos** são limpeza de campos (não excluem registros)
e **1 método** já possui verificação de vínculos implementada.

| Situação                          | Quantidade |
|-----------------------------------|:----------:|
| Métodos de exclusão de registros  |     6      |
| Limpeza de campos (sem risco FK)  |     2      |
| Com verificação de vínculos       |     1      |
| Sem verificação de vínculos       |     4      |
| Com TODO pendente no código       |     2      |

**Risco principal:** 4 métodos excluem registros sem verificar
tabelas filhas. Se houver dados vinculados, o PostgreSQL lançará
exceção de FK, mas a mensagem genérica "Registro não foi excluído"
não informa o motivo ao usuário.

---

## 2. Tabela Resumo de Todos os Métodos

| #  | Controller           | Método                | Tabela        | Verifica FK? | Prioridade |
|----|----------------------|-----------------------|---------------|:------------:|:----------:|
| 1  | MedicosController    | ExcluirMedico         | Medicos       |     Não      |    Alta    |
| 2  | InstituicoesCtrl     | ExcluirInstituicao    | Instituicao   |     Não      |    Alta    |
| 3  | InstituicoesCtrl     | ExcluirImagemTimbre   | Instituicao*  |     N/A      |    Baixa   |
| 4  | InstituicoesCtrl     | ExcluirImagemLogomarca| Instituicao*  |     N/A      |    Baixa   |
| 5  | PostosController     | ExcluirPostos         | Postos        |     Não      |    Alta    |
| 6  | SenhasController     | ExcluirUsuario        | Senhas        |     Não      |   Média    |
| 7  | PlanoExamesCtrl      | ExcluirPlanoExames    | PlanoExames   |     Não      |    Alta    |
| 8  | ClasseExamesCtrl     | ExcluirClasseExames   | ClasseExames  |     Sim      |    Alta    |

> (*) Limpeza de campos `byte[]` e nome — não excluem o registro.

---

## 3. Detalhamento por Método

### 3.1 ExcluirMedico

- **Controller:** `MedicosController.cs`
- **Tabela:** `Medicos`
- **Mecanismo:** Exclusão direta com `_db.Remove(registro)` dentro
  de transação EF Core.
- **Verificação de vínculos:** Não

**Tabelas filhas com FK para Medicos(Id):**

| Tabela filha           | Coluna FK  | Tipo de dado       |
|------------------------|------------|--------------------|
| ExamesRealizados       | MedicoId   | Exames finalizados |
| ExamesRealizadosAM     | MedicoId   | Exames AM          |
| ExamesExportados       | MedicoId   | Exames exportados  |
| ExamesPendentes        | MedicoId   | Exames pendentes   |
| FichasInternas         | MedicoId   | Fichas internas    |
| FichasLotes            | MedicoId   | Fichas em lote     |
| FichasPlanilhas        | MedicoId   | Fichas planilhas   |
| Requisitar             | MedicoId   | Requisições        |

**Risco:** Exceção de FK do PostgreSQL com mensagem genérica.
O usuário não saberá que existem exames vinculados ao médico.

**Mensagem sugerida:**
> "Médico possui requisições ou exames vinculados e não pode
> ser excluído."

**Prioridade:** Alta — médicos são referenciados em 8 tabelas
de dados operacionais críticos.

---

### 3.2 ExcluirInstituicao

- **Controller:** `InstituicoesController.cs`
- **Tabela:** `Instituicao`
- **Mecanismo:** Usa `DeleteStrategy<Instituicao>` via
  `DeleteContext` (Strategy Pattern genérico).
- **Verificação de vínculos:** Não

**Tabelas filhas com FK para Instituicao(Id):**

| Tabela filha           | Coluna FK       | Tipo de dado       |
|------------------------|-----------------|--------------------|
| ExamesRealizados       | InstituicaoId   | Exames finalizados |
| ExamesRealizadosAM     | InstituicaoId   | Exames AM          |
| ExamesExportados       | InstituicaoId   | Exames exportados  |
| ExamesImpressos        | InstituicaoId   | Exames impressos   |
| ExamesPendentes        | InstituicaoId   | Exames pendentes   |
| FichasInternas         | InstituicaoId   | Fichas internas    |
| FichasLotes            | InstituicaoId   | Fichas em lote     |
| FichasPlanilhas        | InstituicaoId   | Fichas planilhas   |
| ItensExamesRealizados  | InstituicaoId   | Itens de exames    |
| ItensExamesRealizadosAM| InstituicaoId   | Itens exames AM    |
| Requisitar             | InstituicaoId   | Requisições        |

**Risco:** Exceção de FK do PostgreSQL. A `DeleteStrategy`
captura a exceção no `catch` genérico e retorna `false`,
gerando mensagem "Registro não foi excluído" sem explicação.

**Mensagem sugerida:**
> "Instituição possui exames, requisições ou fichas vinculadas
> e não pode ser excluída."

**Prioridade:** Alta — instituição é referenciada em 11 tabelas.
É a entidade com mais dependências no sistema.

---

### 3.3 ExcluirImagemTimbre

- **Controller:** `InstituicoesController.cs`
- **Tabela:** `Instituicao` (atualização, não exclusão)
- **Mecanismo:** Limpa os campos `NomeTimbre = ""` e
  `Timbre = null` no registro da instituição.
- **Verificação de vínculos:** N/A

**Análise:** Este método **não exclui registros**. Apenas limpa
campos `byte[]` e `varchar` da própria tabela `Instituicao`.
Não há risco de violação de FK.

**Prioridade:** Baixa — sem necessidade de verificação.

---

### 3.4 ExcluirImagemLogomarca

- **Controller:** `InstituicoesController.cs`
- **Tabela:** `Instituicao` (atualização, não exclusão)
- **Mecanismo:** Limpa os campos `NomeLogomarca = ""` e
  `Logomarca = null` no registro da instituição.
- **Verificação de vínculos:** N/A

**Análise:** Idêntico ao `ExcluirImagemTimbre`. Apenas limpa
campos. Sem risco de FK.

**Prioridade:** Baixa — sem necessidade de verificação.

---

### 3.5 ExcluirPostos

- **Controller:** `PostosController.cs`
- **Tabela:** `Postos`
- **Mecanismo:** Exclusão direta com `_db.Remove(registro)` dentro
  de transação EF Core.
- **Verificação de vínculos:** Não

**Tabelas filhas com FK para Postos(Id):**

| Tabela filha           | Coluna FK  | Tipo de dado       |
|------------------------|------------|--------------------|
| ExamesRealizados       | PostoId    | Exames finalizados |
| ExamesRealizadosAM     | PostoId    | Exames AM          |
| Requisitar             | PostoId    | Requisições        |

**Risco:** Exceção de FK do PostgreSQL com mensagem genérica
"Registro não foi excluído".

**Mensagem sugerida:**
> "Posto possui requisições ou exames vinculados e não pode
> ser excluído."

**Prioridade:** Alta — postos são referenciados em exames
realizados e requisições.

---

### 3.6 ExcluirUsuario

- **Controller:** `SenhasController.cs`
- **Tabela:** `Senhas`
- **Mecanismo:** Busca por `Email` (não por `Id`). Possui
  verificação de `Administrador == 1` (proprietário não pode
  ser excluído). Contém **TODO** no código indicando que a
  exclusão real ainda não foi implementada — o método marca
  `deletou = true` mas **não executa o DELETE** de fato.
- **Verificação de vínculos:** Não (e a exclusão nem ocorre)

**Tabelas filhas com FK para Senhas(Id):**

| Tabela filha   | Coluna FK | Cascade?                    |
|----------------|-----------|-----------------------------|
| UsuariosWeb    | SenhaId   | ON DELETE CASCADE ON UPDATE CASCADE |

**Observações importantes:**
1. O método **não executa a exclusão real** — há dois `TODO`
   no código: "Deletar tudo do perfil deste usuário" e
   "Deletar usuario".
2. A FK de `UsuariosWeb` possui `ON DELETE CASCADE`, então ao
   excluir o registro de `Senhas`, o registro correspondente
   em `UsuariosWeb` será excluído automaticamente pelo banco.
3. Não há outras tabelas com FK para `Senhas` no DDL atual.
4. A busca é por `Email` (campo único), não por `Id`.

**Mensagem sugerida (quando implementar a exclusão):**
> "Ao excluir este usuário, seus dados de acesso web
> (UsuariosWeb) também serão removidos. Deseja continuar?"

**Prioridade:** Média — a exclusão ainda não está implementada.
Quando for implementada, o CASCADE resolve a FK, mas é
recomendável informar o usuário sobre o efeito cascata.

---

### 3.7 ExcluirPlanoExames

- **Controller:** `PlanoExamesController.cs`
- **Tabela:** `PlanoExames`
- **Mecanismo:** Exclusão em massa com `ExecuteDeleteAsync()`.
  Se a conta é principal (termina em `0000`), exclui a conta
  e todos os itens filhos. Se é item, exclui apenas o item
  em todas as tabelas de exames.
- **Verificação de vínculos:** Não
- **TODO no código:** "PRECISA BLOQUEAR A DELEÇÃO QUANDO A
  CONTA JÁ ESTIVER SENDO UTILIZADA EM ALGUM PACIENTE."

**Tabelas filhas com FK para PlanoExames:**

A tabela `PlanoExames` **não possui FKs declaradas** no DDL
apontando para ela. Porém, a coluna `ContaExame` é usada como
referência lógica (não FK formal) em:

| Tabela                  | Coluna      | Tipo de vínculo |
|-------------------------|-------------|-----------------|
| ItensExamesRealizados   | ContaExame  | Lógico (varchar)|
| ItensExamesRealizadosAM | ContaExame  | Lógico (varchar)|
| Requisitar              | ContaExame  | Lógico (varchar)|
| ExamesPendentes         | ContaExame  | Lógico (varchar)|

**Risco:** Não haverá exceção de FK do PostgreSQL (não há FK
formal), mas a exclusão criará **inconsistência lógica**: itens
de exames realizados e requisições referenciarão contas que
não existem mais no plano.

**Mensagem sugerida:**
> "Esta conta do Plano de Exames possui itens de exames
> realizados ou requisições vinculadas e não pode ser excluída."

**Prioridade:** Alta — a exclusão de contas em uso causa
inconsistência de dados em exames de pacientes. O próprio
código já reconhece isso no TODO.

---

### 3.8 ExcluirClasseExames

- **Controller:** `ClasseExamesController.cs`
- **Tabela:** `ClasseExames`
- **Mecanismo:** Usa `ExclusaoService.ExcluirEntidadeComConcorrenciaAsync`
  com validação extra e controle de concorrência.
- **Verificação de vínculos:** **Sim** — implementada via
  `validacaoExtra` que verifica `ItensExamesRealizados` e
  `ItensExamesRealizadosAM` por `ClasseExamesId`.

**Tabelas filhas com FK para ClasseExames(Id):**

| Tabela filha           | Coluna FK       | Verificada? |
|------------------------|-----------------|:-----------:|
| ItensExamesRealizados  | ClasseExamesId  |     Sim     |
| ItensExamesRealizadosAM| ClasseExamesId  |     Sim     |
| ExamesPendentes        | ClasseExamesId  |     Não     |
| Requisitar             | ClasseExamesId  |     Não     |

**Observações:**
1. A verificação atual consulta `ItensExamesRealizados` e
   `ItensExamesRealizadosAM` via JOIN com `PlanoExames`,
   mas **não verifica** `ExamesPendentes` nem `Requisitar`.
2. A exclusão também remove os registros de `PlanoExames`
   vinculados à folha (via `ExecuteDeleteAsync` no service).
3. Possui controle de concorrência via `ControleConcorrencia`.

**Lacuna identificada:** Faltam verificações em `ExamesPendentes`
e `Requisitar` que também possuem FK para `ClasseExames(Id)`.

**Mensagem atual (via ExclusaoService):**
> "Não é possível excluir o registro {id}, pois há vínculos
> ativos."

**Mensagem sugerida (mais específica):**
> "Folha de Exames Nº {id} possui exames realizados, pendentes
> ou requisições vinculadas e não pode ser excluída."

**Prioridade:** Alta — já possui verificação parcial, mas
precisa incluir `ExamesPendentes` e `Requisitar`.

---

## 4. Mapa de FKs por Entidade (Resumo Visual)

```
Medicos ──────────► ExamesRealizados
                  ► ExamesRealizadosAM
                  ► ExamesExportados
                  ► ExamesPendentes
                  ► FichasInternas
                  ► FichasLotes
                  ► FichasPlanilhas
                  ► Requisitar

Instituicao ──────► ExamesRealizados
                  ► ExamesRealizadosAM
                  ► ExamesExportados
                  ► ExamesImpressos
                  ► ExamesPendentes
                  ► FichasInternas
                  ► FichasLotes
                  ► FichasPlanilhas
                  ► ItensExamesRealizados
                  ► ItensExamesRealizadosAM
                  ► Requisitar

Postos ───────────► ExamesRealizados
                  ► ExamesRealizadosAM
                  ► Requisitar

ClasseExames ─────► ItensExamesRealizados
                  ► ItensExamesRealizadosAM
                  ► ExamesPendentes
                  ► Requisitar

PlanoExames ──────► (sem FK formal, vínculos lógicos
                     via ContaExame em varchar)

Senhas ───────────► UsuariosWeb (CASCADE)
```

---

## 5. Recomendações de Implementação

### 5.1 Padrão a seguir

Usar o modelo já implementado em `PacientesController.ExcluirPaciente`
como referência:

```csharp
bool possuiVinculos =
    await _db.TabelaFilha1.AnyAsync(r => r.EntidadeId == id)
 || await _db.TabelaFilha2.AnyAsync(e => e.EntidadeId == id);

if (possuiVinculos)
    return Json(new {
        titulo = MensagensError_pt_BR.ErroFalhou,
        mensagem = "Mensagem assertiva sobre o vínculo",
        action = "",
        sucesso = false
    });
```

### 5.2 Ordem de prioridade para implementação

| Prioridade | Método              | Motivo                        |
|:----------:|---------------------|-------------------------------|
|     1      | ExcluirMedico       | 8 tabelas filhas, sem proteção|
|     2      | ExcluirInstituicao  | 11 tabelas filhas, sem proteção|
|     3      | ExcluirPostos       | 3 tabelas filhas, sem proteção|
|     4      | ExcluirPlanoExames  | Vínculos lógicos, TODO no código|
|     5      | ExcluirClasseExames | Verificação parcial, completar|
|     6      | ExcluirUsuario      | Exclusão não implementada ainda|

### 5.3 Tabelas mínimas a verificar por método

**ExcluirMedico:**
- `Requisitar` (MedicoId)
- `ExamesRealizados` (MedicoId)
- `ExamesPendentes` (MedicoId)

**ExcluirInstituicao:**
- `Requisitar` (InstituicaoId)
- `ExamesRealizados` (InstituicaoId)
- `ExamesPendentes` (InstituicaoId)

**ExcluirPostos:**
- `Requisitar` (PostoId)
- `ExamesRealizados` (PostoId)

**ExcluirPlanoExames:**
- `ItensExamesRealizados` (ContaExame — vínculo lógico)
- `Requisitar` (ContaExame — vínculo lógico)

**ExcluirClasseExames (completar):**
- `ExamesPendentes` (ClasseExamesId) — falta
- `Requisitar` (ClasseExamesId) — falta

**ExcluirUsuario (quando implementar):**
- `UsuariosWeb` (SenhaId) — CASCADE, mas informar o usuário

### 5.4 Observações adicionais

1. **Não confiar apenas na exceção de FK do PostgreSQL** como
   proteção. A regra de negócio do steering `regras_gerais.md`
   exige validação explícita no controller antes do DELETE.

2. **Fichas (Internas, Lotes, Planilhas)** dependem de
   `ExamesRealizados`, que por sua vez depende de `Medicos`,
   `Instituicao` e `Postos`. A verificação nas tabelas
   principais (`Requisitar`, `ExamesRealizados`,
   `ExamesPendentes`) já cobre indiretamente as fichas.

3. **PlanoExames** não possui FK formal no DDL. A verificação
   deve ser feita por `ContaExame` (varchar), comparando com
   `ItensExamesRealizados.ContaExame` e
   `Requisitar.ContaExame`.

4. **ExcluirUsuario** precisa primeiro ter a exclusão real
   implementada (resolver os TODOs) antes de adicionar
   verificação de vínculos.

5. Para os métodos `ExcluirImagemTimbre` e
   `ExcluirImagemLogomarca`, **nenhuma ação é necessária** —
   são limpezas de campos, não exclusões de registros.
