# Auditoria de Queries e Operações de Banco — Controllers

**Data:** 03/05/2026
**Autor:** Kiro
**Escopo:** Todos os controllers da solução LabWeb7
**Método:** Análise estática via busca no código (sem execução)

---

## Resumo Executivo

- **Controllers analisados:** 21
- **Com operações de banco:** 19
- **Sem operações de banco:** 2 (MenuController, ReleaseController)
- **Problemas críticos:** 0
- **Problemas altos:** 3
- **Problemas médios:** 4
- **Problemas baixos:** 3

---

## Inventário por Controller

### RequisitarController (COMPLEXO)

| Método | Operação | Forma | AsNoTracking | Transação | DateTime |
|--------|----------|-------|--------------|-----------|----------|
| Index | SELECT | LINQ | ✅ | — | — |
| GetLancamentosHoje | SELECT | LINQ+Subquery | ✅ | — | ✅ UTC |
| SalvarRequisicao | INSERT/DELETE | EF Core | — | ✅ | ✅ UTC |
| CriarOuAtualizarPacienteAsync | INSERT/UPDATE | EF Core | — | Fora¹ | ✅ UTC |
| SalvarExameRealizadoAsync | INSERT | EF Core | — | ✅ | ✅ UTC |
| GeraSequencialAsync | SELECT+UPDATE | SQL bruto | — | ✅ | — |
| CarregarRequisicaoParaEdicao | SELECT | LINQ+Include | ✅ | — | ✅ UTC |
| CarregarCupomEdicao | SELECT | LINQ | ✅ | — | ✅ UTC |
| ExcluirRequisicao | DELETE | EF Core | — | ✅ | ✅ UTC |
| CupomRequisicao | SELECT | LINQ | — | — | ✅ UTC |
| PartialMontarItensCupom | SELECT | LINQ | ✅ | — | — |
| PartialLancarExames | SELECT | LINQ | ✅ | — | — |
| DiagnosticoHoje | SELECT | LINQ | — | — | ✅ UTC |

¹ Paciente/Médico salvos fora da transação (regra de negócio).

### ClasseExamesController

| Método | Operação | Forma | AsNoTracking | Transação | Risco |
|--------|----------|-------|--------------|-----------|-------|
| Index | SELECT | LINQ | ✅ | — | — |
| SalvarClasseExames | INSERT | EF Core | — | ✅ | — |
| SalvarAlteracaoClasseExames | UPDATE | EF Core | — | ✅ | — |
| ExcluirClasseExames | DELETE | SQL+EF | — | ✅ | — |
| AtualizaFolhaNoPlanoDeExames | INSERT/UPDATE | EF Core | — | ✅ | ⚠️¹ |

¹ Múltiplas transações aninhadas em loop — risco de performance.

### PacientesController

| Método | Operação | Forma | AsNoTracking | Transação | DateTime |
|--------|----------|-------|--------------|-----------|----------|
| Index | SELECT | LINQ | ✅ | — | ✅ UTC |
| SalvarPaciente | INSERT | EF Core | — | ✅ | ✅ UTC |
| SalvarAlteracaoPaciente | UPDATE | EF Core | — | ✅ | ✅ UTC |
| ExcluirPaciente | DELETE | Strategy | — | ✅ | — |

### InstituicoesController

| Método | Operação | Forma | AsNoTracking | Transação |
|--------|----------|-------|--------------|-----------|
| Index | SELECT | LINQ | ✅ | — |
| SalvarInstituicao | INSERT | EF Core | — | ✅ |
| SalvarAlteracaoInstituicao | UPDATE | EF Core | — | ✅ |
| ExcluirInstituicao | DELETE | Strategy | — | ✅ |
| ExcluirImagemTimbre | UPDATE | EF Core | — | ✅ |
| ExcluirImagemLogomarca | UPDATE | EF Core | — | ✅ |

### MedicosController

| Método | Operação | Forma | AsNoTracking | Transação |
|--------|----------|-------|--------------|-----------|
| Index | SELECT | LINQ | ✅ | — |
| SalvarMedico | INSERT | EF Core | — | ✅ |
| SalvarAlteracaoMedico | UPDATE | EF Core | — | ✅ |
| ExcluirMedico | DELETE | EF Core | — | ✅ |

### PostosController

| Método | Operação | Forma | AsNoTracking | Transação |
|--------|----------|-------|--------------|-----------|
| Index | SELECT | LINQ | ✅ | — |
| SalvarPostos | INSERT | EF Core | — | ✅ |
| SalvarAlteracaoPostos | UPDATE | EF Core | — | ✅ |
| ExcluirPostos | DELETE | EF Core | — | ✅ |

### PlanoExamesController

| Método | Operação | Forma | AsNoTracking | Transação |
|--------|----------|-------|--------------|-----------|
| Index | SELECT | LINQ | ⚠️ parcial | — |
| SalvarPlanoExames | INSERT | EF Core | — | ✅ |
| SalvarAlteracaoPlanoExames | UPDATE | EF Core | — | ✅ |
| ModeloPlanoExames | SELECT | SQL+LINQ | ✅ | — |

### PlanoExamesItensController

| Método | Operação | Forma | AsNoTracking | Transação |
|--------|----------|-------|--------------|-----------|
| Index | SELECT | LINQ | ✅ | — |
| SalvarAlteracaoPlanoExamesItens | UPDATE | EF Core | — | ✅ |
| SalvarItemGrid | UPDATE | EF Core | — | ✅ |

### SenhasController

| Método | Operação | Forma | AsNoTracking | Transação | Risco |
|--------|----------|-------|--------------|-----------|-------|
| Index | SELECT | LINQ | ✅ | — | — |
| ExcluirUsuario | DELETE | EF Core | — | ✅ | — |
| UsuarioSalvarSenha | UPDATE | EF Core | — | ⚠️¹ | Alto |
| ConfirmarAlterarSenha | UPDATE | EF Core | — | ⚠️¹ | Alto |
| ResetarSenha | UPDATE | EF Core | — | ⚠️¹ | Alto |

¹ Usa `TransactionScope` (legado) em vez de transação EF Core.

### ConfiguracoesController

| Método | Operação | Forma | AsNoTracking | Transação | Risco |
|--------|----------|-------|--------------|-----------|-------|
| Index GET | SELECT | LINQ | — | — | Baixo |
| Index POST | INSERT/UPDATE | EF Core | — | ⚠️ | Médio |

### GraficosController

| Método | Operação | Forma | AsNoTracking | Risco |
|--------|----------|-------|--------------|-------|
| GraficoReCaptcha | SELECT | LINQ | ❌ | Baixo |

### ReCaptchaTrackerController

| Método | Operação | Forma | AsNoTracking | Transação | Risco |
|--------|----------|-------|--------------|-----------|-------|
| RegistrarSolicitacao | INSERT/UPDATE | EF Core | — | ❌ | Médio |
| VerificarLimite | SELECT | LINQ | ❌ | — | Baixo |

### HomeController

| Método | Operação | Forma | AsNoTracking | Risco |
|--------|----------|-------|--------------|-------|
| Login | SELECT | LINQ | ❌ | Baixo |
| ContinuarLogin | SELECT | LINQ | ❌ | Baixo |

---

## Problemas Encontrados (por criticidade)

### ALTO (3)

| # | Controller | Método | Problema | Sugestão |
|---|-----------|--------|----------|----------|
| 1 | SenhasController | UsuarioSalvarSenha | Usa `TransactionScope` legado | Migrar para `_db.Database.BeginTransactionAsync()` |
| 2 | SenhasController | ConfirmarAlterarSenha | Usa `TransactionScope` legado | Migrar para transação EF Core |
| 3 | SenhasController | ResetarSenha | Usa `TransactionScope` legado | Migrar para transação EF Core |

### MÉDIO (4)

| # | Controller | Método | Problema | Sugestão |
|---|-----------|--------|----------|----------|
| 4 | ConfiguracoesController | Index POST | INSERT/UPDATE sem transação | Adicionar transação |
| 5 | ReCaptchaTrackerController | Registrar | INSERT/UPDATE sem transação | Adicionar transação |
| 6 | ClasseExamesController | AtualizaFolha | Transações aninhadas em loop | Consolidar em transação única |
| 7 | RequisitarController | Index | `.AsEnumerable().Count()` | Usar `.CountAsync()` direto |

### BAIXO (3)

| # | Controller | Método | Problema | Sugestão |
|---|-----------|--------|----------|----------|
| 8 | GraficosController | GraficoReCaptcha | SELECT sem AsNoTracking | Adicionar `.AsNoTracking()` |
| 9 | ReCaptchaTrackerController | VerificarLimite | SELECT sem AsNoTracking | Adicionar `.AsNoTracking()` |
| 10 | HomeController | Login | SELECT sem AsNoTracking | Adicionar `.AsNoTracking()` |

---

## Avaliação de DateTime/Timestamptz

| Controller | Usa UTC | Usa Range UTC | Usa Legacy | Status |
|-----------|---------|---------------|------------|--------|
| RequisitarController | ✅ | ✅ | ❌ | Migrado |
| PacientesController | ✅ | ✅ | ❌ | Migrado |
| SenhasController | — | ✅ | ❌ | Migrado |
| ClasseExamesController | — | — | — | Sem datas |
| InstituicoesController | — | — | — | Sem datas |
| MedicosController | — | — | — | Sem datas |
| PostosController | — | — | — | Sem datas |
| ConfiguracoesController | — | — | — | Sem datas |

---

## Avaliação de Segurança

### SQL Injection
- **Risco:** Baixo — o projeto usa EF Core com LINQ para a
  maioria das queries. As poucas queries SQL brutas usam
  `FromSqlRaw` com parâmetros (`{0}`, `{1}`), não concatenação.
- **Exceção:** `GeraSequencialAsync` usa `FromSqlRaw` com
  parâmetro — correto.

### Validação de FK antes de DELETE
- **Status:** ✅ Todos os controllers que fazem DELETE validam
  FKs antes de excluir, retornando mensagem assertiva ao
  usuário.

### Transações
- **Status:** ✅ A maioria dos controllers usa transações EF
  Core para operações de escrita. Exceções documentadas acima.

---

## Avaliação de Performance

### Pontos positivos
- `AsNoTracking()` usado na maioria das leituras
- Projeção direta no `GetLancamentosHoje` (sem Include)
- Índice `idx_requisitar_dataini_pacienteid` criado
- Paginação via `.Take(registros)` nas listagens

### Pontos de atenção
- `RequisitarController.Index` usa `.AsEnumerable().Count()`
  que carrega todos os registros em memória para contar
- `CarregarRequisicaoParaEdicao` usa 5 `Include` — poderia
  usar projeção direta como o `GetLancamentosHoje`
- `AtualizaFolhaNoPlanoDeExames` cria transação por iteração
  do loop — deveria ser uma transação única

---

## Checklist de Ações

```
[ ] Migrar TransactionScope → EF Core no SenhasController
[ ] Adicionar transação no ConfiguracoesController POST
[ ] Adicionar transação no ReCaptchaTrackerController
[ ] Adicionar AsNoTracking no GraficosController
[ ] Adicionar AsNoTracking no ReCaptchaTrackerController
[ ] Adicionar AsNoTracking no HomeController
[ ] Otimizar .AsEnumerable().Count() no RequisitarController
[ ] Avaliar projeção direta no CarregarRequisicaoParaEdicao
[ ] Consolidar transações no AtualizaFolhaNoPlanoDeExames
```
