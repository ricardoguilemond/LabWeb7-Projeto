# Spec — Lançamento de Resultados de Exames

## Objetivo

Implementar a tela de Lançamento de Resultados de Exames no projeto .NET,
seguindo os mesmos propósitos existentes no sistema Delphi (FExamesResultados.pas)
e obedecendo criteriosamente à padronização de telas do projeto .NET atual.

---

## 1. REQUIREMENTS

### 1.1 Requisitos Funcionais

| ID | Requisito | Origem Delphi |
|----|-----------|---------------|
| RF-001 | Listar exames liberados (`Liberacao=1`) e não baixados (`Baixado=0`) por período (DataIni/DataFim) | AbreExamesRealizados |
| RF-002 | Exibir grid de exames realizados (header) com: Id, Paciente, Instituição, Tabela, Sequencial, Data, Médico | Grid principal |
| RF-003 | Ao clicar num exame no grid header, carregar seus itens no grid de itens editável | AbreItensExamesRealizados |
| RF-004 | Grid de itens deve exibir: Folha, Descrição, Resultado, UnidadeMedida, Referência, ContaExame | DBGrid |
| RF-005 | Campo **Resultado** editável inline (input text dentro da linha do grid) | Coluna editável |
| RF-006 | Campo **UnidadeMedida** editável inline | Coluna editável |
| RF-007 | Campo **Referência** editável inline | Coluna editável |
| RF-008 | Ao pressionar ENTER no campo Resultado, salvar e avançar para próximo item | GravarResultado + Next |
| RF-009 | Pular automaticamente linhas de cabeçalho de folha (ContaExame termina em "0000") | Lógica do KeyDown |
| RF-010 | Botão SALVAR (F6) para gravar resultado manualmente | spdGravarClick |
| RF-011 | Exibir painel informativo: paciente, nascimento, idade, CPF, médico, CRM, instituição, tabela, sequencial | Atualiza_Tela |
| RF-012 | Campo de **Laudo** adicional por item (textarea/memo) | dbMemoLaudo |
| RF-013 | Exibir **Laudo Fixo** de referência (readonly) vindo da pasta Laudos por ContaExame | AbrePlanoExames + RichEdit1 |
| RF-014 | ComboBox de **Textos Prontos** para inserção rápida no campo Resultado | BoxTextoAuxiliar |
| RF-015 | Validar preenchimento completo de Resultado antes de permitir impressão | Resultado_Faltando |
| RF-016 | **Imprimir Resultado** em PDF com dados completos (paciente, médico, itens, resultados, referências) | spdResultadoClick |
| RF-017 | Ao imprimir, marcar `DataEntrega = NOW` e `Situacao = 3` no ExamesRealizados | UPDATE DataEntrega |
| RF-018 | Incrementar `TotalImpresso` a cada impressão | UPDATE TotalImpresso |
| RF-019 | **Baixar para Arquivo-Morto**: mover ExamesRealizados → ExamesRealizadosAM e ItensExamesRealizados → ItensExamesRealizadosAM | Baixar |
| RF-020 | Validar que nenhum outro terminal está baixando o mesmo exame (`Situacao != 11`) | Controle de concorrência |
| RF-021 | **Excluir item** individual de um exame | DeletaItem |
| RF-022 | Filtros: período (DataFim), código exame, nome paciente, CPF, sequencial, código coleta | Buscas |
| RF-023 | Ordenação do grid header por: código exame, código paciente, nome, instituição, controle apoio. Grid de itens sempre ordenado por `ContaExame` ASC | rdgOrdemMapa + ORDER BY ContaExame |
| RF-024 | Opção de menu lateral para acessar a tela | ControleDePerfilMenu |

### 1.2 Requisitos Não Funcionais

| ID | Requisito |
|----|-----------|
| RNF-001 | Seguir padrão arquitetural existente: Controller herda BaseController, SessionFilter em todos endpoints |
| RNF-002 | View com partial de menu (`_PartialMenuResultadoExames.cshtml`) no padrão `.groupButtton` |
| RNF-003 | Grid header usa DataTables (`configTable()` via `_PartialDatatables`) |
| RNF-004 | Grid de itens: tabela HTML com inputs inline (não DataTables — precisa ser editável) |
| RNF-005 | Endpoints AJAX retornam JSON no padrão `{ sucesso, mensagem, dados }` |
| RNF-006 | Salvamento de resultado via AJAX individual (1 item por request) — não form submit completo |
| RNF-007 | Build: 0 erros, 0 avisos |
| RNF-008 | Encoding: UTF-8 com BOM em .cs e .cshtml |
| RNF-009 | Sem pacotes NuGet adicionais |
| RNF-010 | Sem alteração no modelo de dados ou DbContext — usar entidades e relacionamentos existentes |
| RNF-011 | Rota principal: `[Route("ResultadoExames")]` |
| RNF-012 | Arquivos de laudo fixo em: `\LabWebMvc.MVC\Laudos\{CNPJ}\{ContaExame}.DOC` (com fallback para pasta raiz) |
| RNF-013 | PDFs gerados em: `\App_Data\Resultados\{CNPJ}\{AnoMes}\{ExameId}.pdf` |

---

## 2. DESIGN

### 2.1 Layout da Tela

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ [■ Resultado de Exames]  [■ Imprimir Resultado]  [■ Baixar Arquivo-Morto]   │  ← partial menu
├─────────────────────────────────────────────────────────────────────────────┤
│ Filtros: [Período Ini] [Período Fim] [Código] [Paciente] [CPF] [Pesquisar] │
├─────────────────────────────────────────────────────────────────────────────┤
│ Grid Header (DataTables — readonly, clique seleciona exame)                 │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ Id | Paciente | Inst | Tabela | Seq | Data Liberação | Médico | Status  │ │
│ │ 62 | ASDRUBAL | BARROS | BARROS | 8 | 06/06/2026    | DR.FABIO | Lib.  │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────────────┤
│ Painel Info: Exame Nº 62 | Paciente: ASDRUBAL (98) | Nasc: 01/01/2001 |    │
│ Idade: 25a | CPF: 357.182.760-05 | Médico: DR. FABIO CRM: 52131343        │
│ Instituição: BARROS | Tabela: BARROS | Seq: 008                            │
├─────────────────────────────────────────────────────────────────────────────┤
│ [Ordenação: ○ Cód.Exame ○ Paciente ○ Nome ○ Instituição ○ Coleta]         │
├─────────────────────────────────────────────────────────────────────────────┤
│ Grid Itens (HTML com inputs — editável)                                     │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ Folha      | Descrição     | Resultado | Unidade | Referência           │ │
│ │ BIOQUIMICA | Glicose       | [___95__] | mg/dL   | 70 a 99             │ │
│ │ BIOQUIMICA | Creatinina    | [__1.2_]  | mg/dL   | 0.7 a 1.3           │ │
│ │ BIOQUIMICA | Triglicerídeos| [__150_]  | mg/dL   | < 150               │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────────────┤
│ Textos Prontos: [▼ Selecionar texto]    Laudo Fixo (readonly):             │
│ ┌─────────────────────────────────────┐ ┌───────────────────────────────┐  │
│ │ [Laudo Adicional — textarea]        │ │ [Conteúdo do .DOC readonly]   │  │
│ └─────────────────────────────────────┘ └───────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────────────┤
│ [SALVAR F6]  [IMPRIMIR]  [BAIXAR AM]  [EXCLUIR F4]  [EXPORTAR PDF]        │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Componentes Visuais

| Componente | Tipo | Padrão seguido |
|-----------|------|---------------|
| Partial Menu | `.groupButtton > .myButton > a[asp-action]` | ConsultarExames |
| Filtros | `<form method="get">` com inputs estilizados inline | ConsultarExames |
| Grid Header | `<table id="modeloTable" name="datatable">` + `_PartialDatatables` | ConsultarExames |
| Grid Itens | `<table>` HTML com `<input>` inline (sem DataTables) | Custom (editável) |
| Painel Info | `<div>` com labels estáticos | Requisitar |
| Textos Prontos | `<select>` com opções carregadas via AJAX | Requisitar (BoxTextoAuxiliar) |
| Laudo Adicional | `<textarea>` | dbMemoLaudo |
| Laudo Fixo | `<div>` readonly com conteúdo do .DOC | RichEdit1 |
| Botões de Ação | `<button>` com FontAwesome icons | Padrão do projeto |
| Mensagens | `clickAviso()` e `Swal.fire()` | site.js |
| Confirmações | `Swal.fire` com botões Sim/Não | Requisitar |

### 2.3 Endpoints

| Rota | Método | Função | SessionFilter |
|------|--------|--------|:------------:|
| `/ResultadoExames` | GET | View principal (Index) | ✅ |
| `/ResultadoExames/ObterExamesLiberados` | GET | JSON grid header (filtros) | ✅ |
| `/ResultadoExames/ObterItensExame` | GET | JSON grid itens (por ExameRealizadoId) | ✅ |
| `/ResultadoExames/SalvarResultado` | POST | Salva resultado de 1 item | ✅ |
| `/ResultadoExames/SalvarLaudo` | POST | Salva campo Laudo do item | ✅ |
| `/ResultadoExames/ImprimirResultado` | GET | Gera PDF e marca DataEntrega | ✅ |
| `/ResultadoExames/BaixarExame` | POST | Move para Arquivo-Morto | ✅ |
| `/ResultadoExames/ExcluirItem` | POST | Exclui item individual | ✅ |
| `/ResultadoExames/ObterLaudoFixo` | GET | Retorna conteúdo do laudo .DOC | ✅ |
| `/ResultadoExames/ObterTextosProntos` | GET | Lista de textos auxiliares | ✅ |

### 2.4 Fluxo de Navegação

```
Menu Lateral → "Resultado de Exames"
    ↓
Index (grid header com exames liberados)
    ↓ clique na linha
Carrega itens do exame (grid editável)
    ↓ digita resultado + ENTER
Salva via AJAX → avança para próximo item
    ↓ todos preenchidos
Botão IMPRIMIR → gera PDF → marca DataEntrega
    ↓
Botão BAIXAR → confirma → move para AM
```

### 2.5 Entidades Utilizadas (sem alteração)

| Entidade | Uso |
|----------|-----|
| ExamesRealizados | Header do exame (filtro, seleção, status) |
| ItensExamesRealizados | Itens editáveis (Resultado, UnidadeMedida, Referencia, Laudo) |
| Pacientes | Dados do paciente (via navigation) |
| Medicos | Dados do médico (via navigation) |
| Instituicao | Dados da instituição (via navigation) |
| Postos | Dados do posto (via navigation) |
| TabelaExames | Tabela de preços (via navigation) |
| ClasseExames | Folha do exame (via ItensExamesRealizados.ClasseExames) |
| ExamesRealizadosAM | Destino da baixa (header) |
| ItensExamesRealizadosAM | Destino da baixa (itens) |

---

## 3. TASK LIST

### Fase 1 — Estrutura Base

- [ ] 1.1 Inserir registro na tabela `ControleDePerfilMenu` para adicionar "Resultado de Exames" ao menu lateral
- [ ] 1.2 Criar controller `ResultadoExamesController.cs` herdando `BaseController`
- [ ] 1.3 Criar ViewModel `vmResultadoExames.cs`
- [ ] 1.4 Criar pasta `Views/ResultadoExames/`
- [ ] 1.5 Criar `Views/ResultadoExames/Partials/_PartialMenuResultadoExames.cshtml`
- [ ] 1.6 Criar `Views/ResultadoExames/Index.cshtml` (estrutura base com partial menu + filtros + grid header)
- [ ] 1.7 Implementar endpoint Index (GET) com filtros de período e carregamento do grid header
- [ ] 1.8 Verificar build: 0 erros, 0 avisos

### Fase 2 — Grid de Itens Editável

- [ ] 2.1 Implementar endpoint `ObterItensExame` (GET) — retorna itens do exame selecionado com campos editáveis
- [ ] 2.2 Implementar renderização do grid de itens via AJAX (clique no header carrega itens)
- [ ] 2.3 Criar grid de itens com inputs inline: Resultado, UnidadeMedida, Referência
- [ ] 2.4 Implementar painel informativo (paciente, médico, instituição, tabela, sequencial)
- [ ] 2.5 Implementar lógica de pular cabeçalhos de folha (ContaExame termina em "0000")
- [ ] 2.6 Verificar build

### Fase 3 — Salvamento de Resultados

- [ ] 3.1 Implementar endpoint `SalvarResultado` (POST) — salva Resultado, UnidadeMedida, Referência de 1 item
- [ ] 3.2 Implementar handler ENTER no grid: salva e avança para próximo input
- [ ] 3.3 Implementar botão SALVAR (F6)
- [ ] 3.4 Implementar endpoint `SalvarLaudo` (POST) — salva campo Laudo (textarea)
- [ ] 3.5 Implementar ComboBox de Textos Prontos (`ObterTextosProntos`)
- [ ] 3.6 Implementar Laudo Fixo readonly (`ObterLaudoFixo` — lê arquivo .DOC da pasta Laudos)
- [ ] 3.7 Verificar build

### Fase 4 — Impressão e Baixa

- [ ] 4.1 Implementar endpoint `ImprimirResultado` — valida preenchimento, gera PDF, marca DataEntrega/Situacao/TotalImpresso
- [ ] 4.2 Implementar geração de PDF com iText (já presente no projeto)
- [ ] 4.3 Implementar endpoint `BaixarExame` — valida, marca Situacao=11, copia para AM, exclui dos originais
- [ ] 4.4 Implementar proteção de concorrência (Situacao=11 impede baixa simultânea)
- [ ] 4.5 Implementar endpoint `ExcluirItem` — exclui item individual com confirmação
- [ ] 4.6 Verificar build

### Fase 5 — Verificação Final

- [ ] 5.1 Build completo da solution: 0 erros, 0 avisos
- [ ] 5.2 Verificar encoding UTF-8 com BOM
- [ ] 5.3 Verificar marcação de código `//Feito pelo Kiro`
- [ ] 5.4 Teste manual: carregar tela, selecionar exame, digitar resultado, salvar
- [ ] 5.5 Teste manual: imprimir PDF
- [ ] 5.6 Teste manual: baixar para AM
- [ ] 5.7 Teste manual: excluir item

---

## 4. NOTAS TÉCNICAS

### Menu

Inserir na tabela `ControleDePerfilMenu`:
```sql
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Controller", "Action", "Nivel", "Ativo")
VALUES (
    (SELECT MAX("Coluna") + 1 FROM "ControleDePerfilMenu"),
    'Resultado de Exames', 'ResultadoExames', 'Index', '001', 1
);
```
O grupo pai (Nivel "000") deve ser o grupo "Exames" (se existir) ou criar um novo.

### Laudo Fixo

Resolução de caminho com fallback:
```csharp
string cnpj = Utils.LoginCNPJEmpresaLogado() ?? "";
string laudoPath = Path.Combine(_env.ContentRootPath, "Laudos", cnpj, contaExame + ".DOC");
if (!File.Exists(laudoPath))
    laudoPath = Path.Combine(_env.ContentRootPath, "Laudos", contaExame + ".DOC");
```

### PDF de Resultados

Salvar em:
```csharp
string cnpj = Utils.LoginCNPJEmpresaLogado() ?? "00000000000000";
string caminho = Path.Combine(_env.ContentRootPath, "App_Data", "Resultados", cnpj, DateTime.UtcNow.ToString("yyyyMM"));
Directory.CreateDirectory(caminho);
string arquivo = Path.Combine(caminho, exameId + ".pdf");
```

### Grid Editável — Padrão de Input Inline

Não usar DataTables para o grid de itens (DataTables não suporta edição inline nativa).
Usar tabela HTML com inputs dentro dos `<td>`:

```html
<tr data-item-id="76">
    <td>BIOQUIMICA</td>
    <td>Glicose</td>
    <td><input type="text" class="input-resultado" value="95" data-field="Resultado" /></td>
    <td><input type="text" class="input-unidade" value="mg/dL" data-field="UnidadeMedida" /></td>
    <td><input type="text" class="input-referencia" value="70 a 99" data-field="Referencia" /></td>
</tr>
```

Handler ENTER:
```javascript
$('.input-resultado').on('keydown', function(e) {
    if (e.key === 'Enter') {
        e.preventDefault();
        salvarItem($(this).closest('tr'));
        // Avança para próxima linha (pula cabeçalhos)
        var $next = $(this).closest('tr').next('tr:not(.linha-cabecalho)');
        $next.find('.input-resultado').focus();
    }
});
```

### Salvamento AJAX por Item

```javascript
function salvarItem($tr) {
    var itemId = $tr.data('item-id');
    var resultado = $tr.find('.input-resultado').val();
    var unidade = $tr.find('.input-unidade').val();
    var referencia = $tr.find('.input-referencia').val();

    $.ajax({
        url: '/ResultadoExames/SalvarResultado',
        type: 'POST',
        data: { itemId, resultado, unidade, referencia },
        dataType: 'json',
        success: function(data) {
            if (!data.sucesso) clickAviso('Erro', data.mensagem, 'critica', null);
        }
    });
}
```

---

## 5. RISCOS E MITIGAÇÕES

| Risco | Mitigação |
|-------|-----------|
| Concorrência na baixa (dois terminais) | Flag Situacao=11 + verificação antes de iniciar |
| Performance com muitos itens | Carregamento AJAX paginado por exame (max ~50 itens) |
| Laudo .DOC pode não existir | Fallback com mensagem "Laudo fixo não disponível" |
| Tabelas AM podem não existir no banco | Verificar existência antes de implementar Fase 4 |
| Perda de dados ao navegar sem salvar | Alerta JS `beforeunload` se houver edições pendentes |

---

## Aguardando Validação

Este documento aguarda refinamento e validação antes da implementação.
Nenhuma alteração de código será feita até aprovação explícita.
