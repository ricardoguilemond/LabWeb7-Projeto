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

### Bibliotecas Disponíveis
- ✅ jQuery 3.7.1
- ✅ Bootstrap
- ✅ DataTables
- ✅ Inputmask 5.x
- ✅ jquery-validation 1.21.0

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
```

---

**Steering criado por Qoder - 21/04/2026**  
*Baseado nas melhores práticas do projeto LabWeb7*
