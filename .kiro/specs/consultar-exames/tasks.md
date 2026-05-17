# Plano de Implementação: Consultar Exames

## Visão Geral

Implementação da tela de consulta e exclusão de exames realizados, acessível
via menu **Exames → Consultar Exames**. A implementação segue o padrão
Master/Detail inline com grid DataTables, exclusão transacional com validação,
e filtros backend + client-side.

A implementação está dividida em 7 grupos independentes, ordenados por
dependência (infraestrutura primeiro, qualidade por último).

## Tarefas

- [ ] 1. Infraestrutura — Controller, Rotas e Injeção de Dependência
  - [x] 1.1 Criar o arquivo `Areas/Controllers/ConsultarExamesController.cs`
    - Namespace: `LabWebMvc.MVC.Areas.Controllers`
    - Herdar de `BaseController`
    - Implementar construtor com injeção de dependência: `IDbFactory`, `IValidadorDeSessao`, `GeralController`, `IEventLogHelper`, `Imagem`, `ExclusaoService`, `IConnectionService`
    - Chamar `base(...)` com todos os parâmetros
    - Adicionar `[TypeFilter(typeof(SessionFilter))]` em todas as actions
    - Definir rotas: `[Route("ConsultarExames")]`, `[Route("ConsultarExames/ObterItensExame")]`, `[Route("ConsultarExames/ExcluirExame")]`
    - Criar stubs vazios das 3 actions (Index, ObterItensExame, ExcluirExame) retornando `View()` / `Json()` temporários
    - Marcar bloco com `//Feito pelo Kiro em dd/MM/yyyy` e `//..Kiro`
    - _Requisitos: 1.1, 1.2, 1.3, 1.4_

- [x] 2. Listagem e Filtros — Action Index com Query e Filtros Backend
  - [x] 2.1 Implementar a Action `Index` com query base e projeção
    - Assinatura: `async Task<IActionResult> Index(string? dataExame, string? nomePaciente, int? codigoExame, string? siglaInstituicao, string? nomeInstituicao, string? nomePosto)`
    - Atributos: `[HttpGet]`, `[Route("ConsultarExames")]`
    - Query base com `AsNoTracking()` e `.Include()` para Instituicao, Postos, Pacientes, TabelaExames
    - Sem filtros: `Take(100).OrderByDescending(x => x.Id)`
    - Projetar resultado em lista `dynamic` com campos: Id, TabelaExamesId, SiglaInstituicao, NomeInstituicao, NomePosto, NomePaciente, Nascimento, Sequencial, DataIni
    - Montar `vmListaValidacao<dynamic>` e retornar via `ValidacaoGenerica`
    - _Requisitos: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [x] 2.2 Implementar filtros backend na Action `Index`
    - Filtro por data: converter para range UTC via `_geralController.ConverterDataLocalParaRangeUtc()`
    - Filtro por nome do paciente: `ToLower().Contains()` case-insensitive
    - Filtro por código do exame: correspondência exata por `Id`
    - Filtro por sigla da instituição: `ToLower().Contains()` case-insensitive
    - Filtro por nome da instituição: `ToLower().Contains()` case-insensitive
    - Filtro por nome do posto: `ToLower().Contains()` case-insensitive
    - Quando filtro aplicado: sem limite de 100 registros
    - _Requisitos: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

- [x] 3. Checkpoint — Compilar e validar infraestrutura + listagem
  - Executar `dotnet build "LabWebMvc.MVC/LabWebMvc.MVC.csproj"`
  - Resultado obrigatório: 0 erros, 0 avisos
  - Garantir que o controller compila corretamente com todas as dependências
  - Perguntar ao usuário se há dúvidas antes de prosseguir

- [x] 4. View e Grid Header — HTML, DataTables e Layout
  - [x] 4.1 Criar a View `Views/ConsultarExames/Index.cshtml`
    - Encoding: UTF-8 com BOM
    - Incluir `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`
    - Incluir `@using BLL` e `@using static BLL.UtilBLL`
    - Incluir partial de menu: `<partial name='Partials/_PartialMenuConsultarExames' />`
    - Área de filtros com `<form method="get">` e campos: dataExame, nomePaciente, codigoExame, siglaInstituicao, nomeInstituicao, nomePosto
    - Botão "Pesquisar" que submete via GET para a action Index
    - _Requisitos: 7.1, 7.2, 7.5, 7.6_

  - [x] 4.2 Implementar o Grid Header (ExamesRealizados) na View
    - Tabela com `id="modeloTable"`, `name="datatable"`, `data-order='[[ 0, "desc" ]]'`
    - Classes: `display compact order-column stripe table-hover nowrap`
    - Estrutura: `<thead>`, `<tbody>` com `@foreach`, `<tfoot>`
    - Colunas: Id, TabelaExamesId, Sigla Instituição, Nome Instituição, Nome Posto, Nome Paciente, Data Nascimento, Sequencial, Data do Exame
    - Coluna de Opções com botão excluir (ícone `fa-sharp fa-solid fa-trash-can`)
    - Formatação de datas com `ToLocalString("dd/MM/yyyy")`
    - _Requisitos: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [x] 4.3 Configurar DataTables e section Scripts
    - Incluir `@section Scripts` com `<partial name='_PartialDatatables' />`
    - Utilizar `configTable()` padrão do `mydatatables.js`
    - Filtro nativo DataTables ativo (search box no topo, inputs no footer)
    - _Requisitos: 7.3, 7.4, 8.1, 8.2, 8.3_

- [x] 5. Master/Detail — Action ObterItensExame, JavaScript e Grid Detail
  - [x] 5.1 Implementar a Action `ObterItensExame` no controller
    - Assinatura: `async Task<IActionResult> ObterItensExame(int exameRealizadoId)`
    - Atributos: `[HttpGet]`, `[Route("ConsultarExames/ObterItensExame")]`
    - Query com `AsNoTracking()` filtrando por `ExameRealizadoId`
    - Ordenar por `OrdemItem`
    - Projetar: ClasseExamesNome, RefExame, RefItem, ContaExame, Descricao, ValorItem (formatado N2 ou "-"), Etiquetas
    - Retornar `Json(new { sucesso = true, itens })`
    - _Requisitos: 5.1, 5.3, 5.6_

  - [x] 5.2 Implementar o Grid Detail (HTML oculto) na View
    - Container `#detailContainer` com `style="display: none;"`
    - Tabela `#detailTable` com classes DataTables padrão
    - Colunas: Classe, Ref. Exame, Ref. Item, Conta Exame, Descrição, Valor Item, Etiquetas (Qtd)
    - `<tbody id="detailBody">` vazio (preenchido via JS)
    - _Requisitos: 5.2, 5.3, 5.5_

  - [x] 5.3 Implementar JavaScript de Master/Detail
    - Event handler no click da linha do grid header (ignorar coluna de opções)
    - Função `carregarDetail(exameRealizadoId)` com `$.ajax` GET para `ConsultarExames/ObterItensExame`
    - Função `renderizarDetail(itens)` que limpa e popula `#detailBody`
    - Mostrar `#detailContainer` após carregar com sucesso
    - Somente um detail aberto por vez (limpa anterior ao clicar nova linha)
    - _Requisitos: 5.1, 5.4, 5.5, 5.6_

- [x] 6. Exclusão — Action ExcluirExame, Validação, Transação e AJAX
  - [x] 6.1 Implementar a Action `ExcluirExame` no controller
    - Assinatura: `async Task<IActionResult> ExcluirExame(int id)`
    - Atributos: `[HttpGet]`, `[Route("ConsultarExames/ExcluirExame")]`
    - Buscar `ExamesRealizados` pelo `id`
    - Buscar `ItensExamesRealizados` onde `ExameRealizadoId == id`
    - Validar: se algum `Resultado` preenchido → retornar erro JSON com mensagem específica
    - Se exame não encontrado → retornar erro JSON
    - _Requisitos: 6.2, 6.3_

  - [x] 6.2 Implementar transação de exclusão na Action `ExcluirExame`
    - Usar `_db.Database.BeginTransactionAsync()`
    - Remover `ItensExamesRealizados` via `RemoveRange`
    - Buscar e remover `Requisitar` vinculados via `RemoveRange`
    - Remover `ExamesRealizados` via `Remove`
    - `SaveChangesAsync()` + `CommitAsync()`
    - Em caso de erro: `RollbackAsync()` + log via `_eventLogHelper.LogEventViewer()` com nível `"wError"`
    - Retornar JSON com `titulo`, `mensagem`, `sucesso`
    - _Requisitos: 6.4, 6.6, 6.7, 9.3_

  - [x] 6.3 Implementar JavaScript de exclusão com SweetAlert2
    - Função `clickDeleteExame(x)` usando `clickConfirm` do `site.js`
    - Parâmetros: `clickConfirm(x, null, "Excluir este exame?", null, "ConsultarExames/ExcluirExame")`
    - O `clickConfirm` já implementa confirmação, loading, mensagem e reload
    - _Requisitos: 6.1, 6.5_

- [x] 7. Checkpoint — Compilar e validar funcionalidades completas
  - Executar `dotnet build "LabWebMvc.MVC/LabWebMvc.MVC.csproj"`
  - Resultado obrigatório: 0 erros, 0 avisos
  - Garantir que todas as actions, view e scripts estão integrados
  - Perguntar ao usuário se há dúvidas antes de prosseguir

- [x] 8. Menu e Navegação — Partial de Menu e Integração
  - [x] 8.1 Criar a partial `Views/ConsultarExames/Partials/_PartialMenuConsultarExames.cshtml`
    - Encoding: UTF-8 com BOM
    - Seguir padrão visual de `_PartialMenuPacientes.cshtml`
    - Título: "Consultar Exames"
    - Adaptar estrutura HTML/CSS conforme padrão existente
    - _Requisitos: 7.2_

  - [x] 8.2 Integrar no menu principal (Exames → Consultar Exames)
    - Adicionar link no menu existente apontando para `/ConsultarExames`
    - Seguir padrão de navegação das demais telas
    - Verificar que o item de menu aparece na posição correta
    - _Requisitos: 2.1_

- [x] 9. Qualidade — Build Final, Encoding e Marcação de Código
  - [x] 9.1 Validação de build final
    - Executar `dotnet build "LabWebMvc.MVC/LabWebMvc.MVC.csproj"`
    - Resultado obrigatório: 0 erros, 0 avisos
    - _Requisitos: 9.1_

  - [x] 9.2 Validação de encoding dos arquivos criados
    - Confirmar UTF-8 com BOM em: `ConsultarExamesController.cs`, `Index.cshtml`, `_PartialMenuConsultarExames.cshtml`
    - Confirmar acentuação correta em todos os textos pt-BR
    - _Requisitos: 9.4_

  - [x] 9.3 Validação de marcação de código
    - Confirmar presença de `//Feito pelo Kiro em dd/MM/yyyy` no início dos blocos significativos
    - Confirmar presença de `//..Kiro` no final dos blocos
    - _Requisitos: 9.6_

  - [x] 9.4 Validação de conformidade técnica
    - Confirmar uso de `AsNoTracking()` em todas as queries de consulta
    - Confirmar uso de tratamento de exceções com log na exclusão
    - Confirmar que nenhum pacote NuGet foi adicionado
    - _Requisitos: 9.2, 9.3, 9.5_

## Notas

- Cada grupo pode ser implementado separadamente, respeitando a ordem de dependência
- O Grupo 1 (Infraestrutura) é pré-requisito para todos os demais
- Os Grupos 2 e 4 podem ser implementados em paralelo após o Grupo 1
- O Grupo 5 depende do Grupo 4 (view precisa existir para o detail)
- O Grupo 6 depende do Grupo 1 (controller precisa existir para a action)
- O Grupo 8 (Menu) pode ser implementado a qualquer momento após o Grupo 4
- O Grupo 9 (Qualidade) é sempre o último, validando tudo
- Checkpoints (Grupos 3 e 7) garantem validação incremental
- Não há property-based tests — o design não possui Correctness Properties
- Testes unitários e de integração são recomendados mas não obrigatórios nesta fase
