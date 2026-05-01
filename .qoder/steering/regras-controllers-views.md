---
trigger: always
description: Regras de Controllers e Views para LabWeb7
---

# Steering de Controllers e Views - Qoder

## Controllers

### BaseController Pattern

Todos controllers **DEVEM** herdar de `BaseController`:

```csharp
public class PacientesController : BaseController
{
    public PacientesController(
        IDbFactory dbFactory,
        IValidadorDeSessao validador,
        GeralController geralController,
        IEventLogHelper eventLogHelper,
        Imagem imagem,
        ExclusaoService exclusaoService)
        : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService)
    {
    }
}
```

### Serviços Disponíveis via BaseController

```csharp
protected readonly IDbFactory _dbFactory;          // Factory para criar DbContext
protected readonly IValidadorDeSessao _validador;   // Validação de sessão
protected readonly GeralController _geralController; // Métodos utilitários gerais
protected readonly IEventLogHelper _eventLogHelper;  // Logging no Event Viewer
protected readonly Imagem _imagem;                   // Serviço de imagens
protected readonly ExclusaoService _exclusaoService; // Serviço de exclusões
protected Db _db;                                    // DbContext (criado via factory)
```

### GeralController - NÃO Alterar

- ❌ **NUNCA** alterar a assinatura ou retorno de métodos do `GeralController` sem autorização explícita
- ✅ `GeralController` contém métodos utilitários usados por toda a aplicação
- ✅ Injeção de dependência configurada em `Startup.cs`

### ValidacaoGenerica

```csharp
// CORRETO - Retorna View() SEM model, dados via ViewBag
public IActionResult ValidacaoGenerica()
{
    ViewBag.Mensagem = "Erro de validação";
    ViewBag.Dados = dados;
    return View();  // SEM model
}

// ERRADO - NÃO retornar com model
public IActionResult ValidacaoGenerica()
{
    return View(model);  // ERRADO!
}
```

## Views

### Localização
- **MVC Views:** `LabWebMvc.MVC/Views/{Controller}/{Action}.cshtml`
- **Áreas:** `LabWebMvc.MVC/Areas/{Area}/Views/{Controller}/{Action}.cshtml`

### Diretiva @page

- ❌ **Views MVC** (pasta `Views/`) **NÃO** devem ter diretiva `@page`
- ✅ `@page` é **exclusiva** de Razor Pages (pasta `Pages/`)

```cshtml
@* ✅ CORRETO - View MVC *@
@model LabWebMvc.MVC.ViewModel.vmPacientes
@{
    ViewData["Title"] = "Pacientes";
}

@* ❌ ERRADO - NÃO usar @page em Views MVC *@
@page
@model LabWebMvc.MVC.ViewModel.vmPacientes
```

### site.js - Carregamento Duplo

- ⚠️ O `site.js` é carregado **DUAS VEZES** no `_Layout.cshtml`:
  - Uma vez no `<head>`
  - Uma vez no final do `<body>`
- ❌ **NÃO** adicionar uma terceira referência
- ✅ Usar as referências existentes

```html
@* _Layout.cshtml - NÃO ALTERAR *@
<head>
    <script src="~/js/site.js"></script>  @* Primeiro carregamento *@
</head>
<body>
    @* ... content ... *@
    <script src="~/js/site.js"></script>  @* Segundo carregamento *@
</body>
```

## ViewModels

### Padrão de Nomenclatura

- **Prefixo:** `vm` ou `VM` (ex: `vmPacientes`, `VMGeral`)
- **Localização:** `LabWebMvc.MVC/ViewModel/`
- **Propósito:** Validação de formulários, transporte de dados para views

### Exemplo
```csharp
public class vmPacientes
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string NomePaciente { get; set; }
    
    [Required(ErrorMessage = "CPF é obrigatório")]
    [RegularExpression(@"\d{3}\.\d{3}\.\d{3}-\d{2}", ErrorMessage = "CPF inválido")]
    public string CPF { get; set; }
}
```

## ViewBag vs ViewModel

### Quando Usar ViewBag
- ✅ Dados auxiliares (dropdowns, mensagens)
- ✅ Configurações de layout
- ✅ Dados não relacionados ao model principal

```csharp
ViewBag.UFList = new SelectList(ufs, "Sigla", "Nome");
ViewBag.Mensagem = "Operação realizada com sucesso";
```

### Quando Usar ViewModel
- ✅ Dados do formulário principal
- ✅ Validações de negócio
- ✅ Dados fortemente tipados

```csharp
public IActionResult Create()
{
    var vm = new vmPacientes();
    ViewBag.UFList = new SelectList(ufs, "Sigla", "Nome");
    return View(vm);
}
```

## JavaScript e jQuery

### Princípio Geral

**Simples é melhor que sofisticado quando tudo funciona bem.**

- Preferir CSS padrão sobre soluções JavaScript para layout e visual.
- Preferir JavaScript puro (vanilla) sobre bibliotecas adicionais.
- Preferir manipulação direta do DOM sobre plugins de terceiros.
- Só adicionar complexidade quando o CSS padrão comprovadamente
  não resolver o problema.

### Bibliotecas Disponíveis
- ✅ jQuery 3.7.1
- ✅ Bootstrap
- ✅ DataTables
- ✅ Inputmask 5.x
- ✅ jquery-validation 1.21.0
- ✅ SweetAlert2 (confirmações destrutivas)
- ❌ **NÃO** adicionar bibliotecas JavaScript ou CSS de terceiros
  sem aprovação explícita do usuário

### Regras Obrigatórias de JavaScript
1. Usar JavaScript puro (vanilla) sempre que possível.
2. jQuery é aceito porque já faz parte do projeto — não substituir
   por outra biblioteca, mas também não expandir seu uso
   desnecessariamente.
3. Manipulação de DOM para criar elementos auxiliares (ex: barra
   de scroll superior) é preferível a plugins.
4. Usar `fetch` ou `$.ajax` conforme o padrão já existente na tela
   — não misturar ambos na mesma função sem motivo.
5. Sempre converter datas do grid (`dd/MM/yyyy`) para `yyyy-MM-dd`
   antes de enviar ao backend.
6. Confirmações destrutivas (exclusão) devem usar SweetAlert2
   (`Swal.fire`).
7. Mensagens informativas devem usar `clickAviso` — função já
   existente no projeto.

### Padrão de Scripts
```javascript
// site.js - Padrão IIFE
(function () {
    'use strict';
    
    // Código aqui
    
})();

// DOM Ready
$(document).ready(function () {
    // Inicializações
});
```

## CSS

### Regras Obrigatórias
1. Usar CSS nativo (`position: sticky`, `overflow`, `z-index`,
   `nth-child`, etc.) como primeira opção para qualquer
   comportamento visual ou de layout.
2. Não introduzir frameworks CSS além do que já existe no projeto.
3. Usar `!important` apenas quando necessário para sobrescrever
   estilos injetados por bibliotecas (ex: DataTables aplica
   estilos inline que exigem override).
4. Seletores devem ser ancorados no wrapper ou container específico
   da tela (ex: `#wrapperRequisitar`) para evitar vazamento de
   estilos para outros grids ou componentes.
5. Estilos específicos de uma partial devem ficar no bloco `<style>`
   da própria partial, não em arquivos CSS globais, salvo quando o
   estilo for reutilizado em múltiplas telas.

### Colunas Fixas em Grids com Scroll Horizontal
- Usar `position: sticky` com valores de `left` calculados pela
  largura acumulada das colunas anteriores.
- Travar largura das colunas fixas com `min-width`, `width` e
  `max-width` iguais para garantir que os offsets de `left`
  permaneçam corretos.
- Diferenciar `z-index` entre header (maior) e body (menor) para
  que o cabeçalho fique acima das células ao rolar.
- Aplicar `background-color` sólido nas colunas fixas (header e
  body) para evitar que o conteúdo das colunas que rolam fique
  visível por trás.
- Aplicar `box-shadow` na última coluna fixa para criar separação
  visual entre a área fixa e a área que rola.
- Manter zebra (odd/even) e hover nas colunas fixas para
  consistência visual.

### Tamanho de Fonte em Grids DataTables
- O DataTables 2.x injeta elementos internos nos `<th>`
  (`.dt-column-title`, `.dt-column-order`, `span`).
- Para controlar o tamanho da fonte dos títulos, o override CSS
  deve atingir tanto o `<th>` quanto esses sub-elementos.
- Exemplo de seletor completo:
  ```css
  #wrapper table.dataTable thead th,
  #wrapper table.dataTable thead th span,
  #wrapper table.dataTable thead th .dt-column-title,
  #wrapper table.dataTable thead th .dt-column-order {
      font-size: 14px !important;
  }
  ```

### Alinhamento Condicional em Células do Body
- O DataTables 2.x também envolve o conteúdo de cada `<td>`
  do body em `<span>` internos.
- Para alterar o alinhamento de uma célula específica (ex:
  centralizar um traço quando o campo está vazio), aplicar
  `text-align` tanto no `<td>` quanto nos `<span>` filhos.
- Usar `createdRow` do DataTables para aplicar condicionalmente:
  ```javascript
  createdRow: function (row, data) {
      if (data.campo === '-') {
          var td = $('td', row).eq(indiceColuna);
          td.css('text-align', 'center');
          td.find('span').css('text-align', 'center');
      }
  }
  ```
- ❌ **NÃO** usar `display:block` em `<span>` dentro de `<td>` —
  isso aumenta a altura da linha. Manter o `display` original e
  aplicar apenas `text-align`.

## DataTables

### Versão e Atualização
- O DataTables **pode** ser atualizado para versões mais novas sob
  demanda, desde que:
  1. Haja avaliação prévia do impacto no design e nos recursos que
     atualmente funcionam.
  2. Nenhum recurso existente quebre após a atualização.
  3. O layout visual permaneça consistente com o design atual.
  4. O usuário aprove a atualização antes da execução.
- Nenhum outro plugin jQuery ou biblioteca de grid pode ser
  introduzido como substituto do DataTables sem aprovação.

### Configuração Padrão para Grids do Projeto
- `scrollX: true` — habilita scroll horizontal.
- `autoWidth: false` — respeita larguras fixas definidas por coluna.
- `responsive: false` — o projeto usa scroll horizontal, não collapse
  responsivo.
- Larguras de coluna definidas na propriedade `columns[].width`.
- Ordenação padrão: `order: [[0, 'desc']]` (coluna Id decrescente),
  salvo necessidade específica da tela.

### Não Usar Plugins DataTables para Colunas Fixas
- ❌ **NÃO usar** o plugin `fixedColumns` do DataTables.
- A fixação de colunas deve ser feita via CSS `position: sticky`
  conforme descrito na seção CSS acima.
- Motivo: o plugin `fixedColumns` clona a tabela internamente, causa
  conflitos com `scrollX`, e é mais pesado e menos previsível que a
  solução CSS pura.

### Layout e Idioma
- Usar a propriedade `layout` (DataTables 2.x) para posicionar
  controles (pageLength, search, info, paging).
- Textos devem estar em Português-Brasil via propriedade `language`.
- Tabela vazia ou sem resultados: exibir mensagem em vermelho e
  negrito.

### Tabela HTML
- A tabela deve ter `<thead>`, `<tbody>` vazio e `<tfoot>`.
- Classe padrão: `display compact order-column table-striped
  table-hover nowrap`.
- Se a tabela tiver `width: 100%` vindo de CSS externo
  (ex: `mydatatables.css`), sobrescrever com
  `width: auto !important` no seletor da tabela específica para
  que o scroll horizontal funcione.

### Barra de Rolagem Superior (Dual Scrollbar)
- Quando um grid DataTables usa `scrollX: true`, a barra de rolagem
  horizontal fica apenas no rodapé (`.dt-scroll-body`).
- Para replicar no topo, injetar um `div` com `overflow-x: auto`
  antes do `.dt-scroll-body`, contendo um `div` interno com a mesma
  largura do `scrollWidth` do body.
- Sincronizar `scrollLeft` bidirecionalmente entre os dois containers
  via event listeners de `scroll`.
- Verificar se o elemento já existe antes de criar, para evitar
  duplicação em reloads.
- Usar `setTimeout` após a inicialização do DataTables para garantir
  que o DOM esteja pronto.

## Frontend Validation

### Client-Side Validation
```cshtml
@model vmPacientes

<form asp-action="Create">
    <div class="form-group">
        <label asp-for="NomePaciente"></label>
        <input asp-for="NomePaciente" class="form-control" />
        <span asp-validation-for="NomePaciente" class="text-danger"></span>
    </div>
    <button type="submit" class="btn btn-primary">Salvar</button>
</form>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

## Controllers - Listagem Padrão

```csharp
public class PacientesController : BaseController
{
    public async Task<IActionResult> Index()
    {
        var pacientes = await _db.Pacientes
            .AsNoTracking()
            .OrderBy(p => p.NomePaciente)
            .ToListAsync();
        
        return View(pacientes);
    }
}
```

## Controllers - CRUD Padrão

### Create
```csharp
[HttpGet]
public IActionResult Create()
{
    var vm = new vmPacientes();
    return View(vm);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(vmPacientes vm)
{
    if (!ModelState.IsValid)
    {
        return View(vm);
    }
    
    var paciente = new Pacientes
    {
        NomePaciente = vm.NomePaciente,
        DataRegistro = await _geralController.ObterDataHoraServidor()
    };
    
    _db.Pacientes.Add(paciente);
    await _db.SaveChangesAsync();
    
    return RedirectToAction(nameof(Index));
}
```

### Delete com Validação de FK
```csharp
[HttpGet]
public async Task<IActionResult> Delete(int? id)
{
    if (id == null)
    {
        return NotFound();
    }
    
    // Verificar FKs antes de permitir exclusão
    var temExames = await _db.ExamesRealizados
        .AnyAsync(e => e.PacienteId == id);
    
    if (temExames)
    {
        ViewBag.Erro = "Paciente possui exames vinculados e não pode ser excluído.";
    }
    
    var paciente = await _db.Pacientes.FindAsync(id);
    if (paciente == null)
    {
        return NotFound();
    }
    
    return View(paciente);
}

[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var temExames = await _db.ExamesRealizados
        .AnyAsync(e => e.PacienteId == id);
    
    if (temExames)
    {
        TempData["Erro"] = "Paciente possui exames vinculados e não pode ser excluído.";
        return RedirectToAction(nameof(Index));
    }
    
    var paciente = await _db.Pacientes.FindAsync(id);
    if (paciente != null)
    {
        _db.Pacientes.Remove(paciente);
        await _db.SaveChangesAsync();
    }
    
    return RedirectToAction(nameof(Index));
}
```

## Checklist de Validação

Antes de criar/alterar controllers e views:

```
□ Controller herda de BaseController?
□ Não alterou métodos do GeralController?
□ ValidacaoGenerica retorna View() sem model?
□ Views MVC NÃO têm diretiva @page?
□ Não adicionou terceira referência ao site.js?
□ Data/hora usa ObterDataHoraServidor()?
□ FKs validadas antes de DELETE?
□ ViewModels usam prefixo vm/VM?
□ ViewModels em ViewModel/ pasta?
□ JavaScript usa bibliotecas disponíveis?
□ Validação client-side configurada?
□ CSS não vaza para outros componentes da página?
□ Colunas fixas mantêm alinhamento ao rolar?
□ Barra superior (se existir) sincroniza com a inferior?
□ Tamanho de fonte dos títulos consistente entre header e footer?
□ Zebra e hover nas colunas fixas funcionam?
□ Nenhuma biblioteca JS/CSS adicionada sem aprovação?
□ Build do projeto: 0 erros e 0 avisos?
```

## Regras de Negócio — Requisição de Exames

### Salvamento (transação)
- Paciente e Médico são salvos **fora da transação** dos itens de exame.
- Se o lançamento dos exames falhar e houver rollback, paciente e médico
  permanecem salvos no banco.
- Nunca envolver o cadastro de Médico/Paciente na mesma transação que
  processa os itens de exame.

### Grid de Requisições do Dia (`_PartialRequisitar.cshtml`)
- Exibe as requisições do dia atual, agrupadas por `PacienteId`
  (registro mais recente por paciente).
- Cada linha possui três botões de ação:

| Botão    | Cor      | Endpoint                                  | Regra                                              |
|----------|----------|-------------------------------------------|----------------------------------------------------|
| Imprimir | Verde    | `POST /Requisitar/CupomRequisicao`        | Sempre permitido                                   |
| Editar   | Amarelo  | `GET /Requisitar/CarregarRequisicaoParaEdicao` | Bloqueado se qualquer item tiver resultado     |
| Excluir  | Vermelho | `POST /Requisitar/ExcluirRequisicao`      | Bloqueado se qualquer item tiver resultado         |

- **Edição:** carrega todos os itens do paciente na data no formulário.
  Recarrega o grid de exames da tabela selecionada.
- **Exclusão:** exclui todos os itens de `Requisitar` do paciente na data.
  Mantém paciente e médico intactos. Exige confirmação SweetAlert2.
- **Double-click na linha não existe** — impressão é exclusivamente
  pelo botão verde.
- A data no grid está em `dd/MM/yyyy` — converter para `yyyy-MM-dd`
  antes de enviar ao backend.
- Validação de resultados é feita **no servidor** (campo `Resultado`
  da tabela `Requisitar`). Nunca confiar apenas no client-side.

---

**Steering criado por Qoder - 21/04/2026**  
*Baseado nas melhores práticas do projeto LabWeb7*
