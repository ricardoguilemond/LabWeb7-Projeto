# Requirements Document — Consultar Exames

## Introdução

Funcionalidade de consulta e exclusão de exames realizados, acessível pelo menu
**Exames → Consultar Exames**. A tela apresenta um grid Master/Detail onde o
header exibe registros de `ExamesRealizados` e o detail exibe os respectivos
`ItensExamesRealizados`. Não permite inclusão nem edição — apenas consulta e
exclusão com validação de resultados.

## Glossário

- **Sistema**: Aplicação LabWeb7 (LabWebMvc.MVC)
- **Grid_Header**: Grid principal DataTables exibindo registros de `ExamesRealizados`
- **Grid_Detail**: Grid secundário DataTables exibindo `ItensExamesRealizados` do
  header selecionado
- **ExamesRealizados**: Tabela/model header dos exames do paciente
  (`LabWebMvc.MVC.Models.ExamesRealizados`)
- **ItensExamesRealizados**: Tabela/model de itens/detalhes do exame
  (`LabWebMvc.MVC.Models.ItensExamesRealizados`)
- **Requisitar**: Tabela/model de cópia operacional da requisição
  (`LabWebMvc.MVC.Models.Requisitar`)
- **Pacientes**: Tabela/model de pacientes (`LabWebMvc.MVC.Models.Pacientes`)
- **Instituicao**: Tabela/model de instituições (`LabWebMvc.MVC.Models.Instituicao`)
- **Postos**: Tabela/model de postos de coleta (`LabWebMvc.MVC.Models.Postos`)
- **TabelaExames**: Tabela/model de tabelas de exames/preços
  (`LabWebMvc.MVC.Models.TabelaExames`)
- **Controller**: `ConsultarExamesController`, herdando de `BaseController`
- **Filtro_Backend**: Filtros específicos processados no servidor antes de
  retornar dados ao grid
- **Filtro_DataTables**: Filtro nativo do DataTables (search box) que opera
  client-side sobre os dados já carregados

## Requisitos

### Requisito 1: Estrutura do Controller

**User Story:** Como desenvolvedor, eu quero que o controller siga o padrão
arquitetural existente, para que a manutenção seja consistente com o restante
do sistema.

#### Critérios de Aceitação

1. THE Controller SHALL herdar de `BaseController` e receber via construtor os
   mesmos serviços injetados no padrão existente (`IDbFactory`, `IValidadorDeSessao`,
   `GeralController`, `IEventLogHelper`, `Imagem`, `ExclusaoService`,
   `IConnectionService`)
2. THE Controller SHALL utilizar o atributo `[TypeFilter(typeof(SessionFilter))]`
   em todas as actions públicas
3. THE Controller SHALL utilizar o namespace
   `LabWebMvc.MVC.Areas.Controllers`
4. THE Controller SHALL residir no arquivo
   `Areas/Controllers/ConsultarExamesController.cs`

### Requisito 2: Rota e Action Index (Listagem)

**User Story:** Como usuário do laboratório, eu quero acessar a tela de
consulta de exames pelo menu, para que eu possa visualizar os exames
realizados.

#### Critérios de Aceitação

1. WHEN o usuário acessar a rota `GET /ConsultarExames`, THE Controller SHALL
   retornar a view Index com dados de `ExamesRealizados`
2. WHEN nenhum filtro for informado, THE Controller SHALL retornar no máximo
   100 registros ordenados por `Id` decrescente
3. THE Controller SHALL utilizar `ValidacaoGenerica` com `vmListaValidacao<dynamic>`
   para retornar dados via `ViewBag.ListaDados`, conforme padrão de
   `PacientesController.Index`
4. THE Controller SHALL realizar joins com `Instituicao`, `Postos`, `Pacientes`
   e `TabelaExames` para obter Sigla da Instituição, Nome da Instituição,
   Nome do Posto e Nome do Paciente
5. THE Controller SHALL formatar a data `DataIni` para exibição em `dd/MM/yyyy`
   utilizando conversão UTC → local conforme padrão do projeto

### Requisito 3: Filtros Backend

**User Story:** Como usuário do laboratório, eu quero filtrar exames por
critérios específicos, para que eu encontre rapidamente o exame desejado.

#### Critérios de Aceitação

1. WHEN o filtro de Data do Exame for informado, THE Controller SHALL filtrar
   `ExamesRealizados.DataIni` utilizando range UTC via
   `_geralController.ConverterDataLocalParaRangeUtc()`
2. WHEN o filtro de Nome do Paciente for informado, THE Controller SHALL
   filtrar por `Pacientes.NomePaciente` com comparação case-insensitive
3. WHEN o filtro de Id (Código do Exame) for informado, THE Controller SHALL
   filtrar por `ExamesRealizados.Id` com correspondência exata
4. WHEN o filtro de Sigla da Instituição for informado, THE Controller SHALL
   filtrar por `Instituicao.Sigla` com comparação case-insensitive
5. WHEN o filtro de Nome da Instituição for informado, THE Controller SHALL
   filtrar por `Instituicao.Nome` com comparação case-insensitive
6. WHEN o filtro de Nome do Posto for informado, THE Controller SHALL filtrar
   por `Postos.NomePosto` com comparação case-insensitive
7. WHEN qualquer filtro backend for aplicado, THE Controller SHALL retornar
   todos os registros correspondentes sem limite de 100
8. THE Controller SHALL processar os filtros no backend antes de retornar
   dados ao DataTables

### Requisito 4: Grid Header (ExamesRealizados)

**User Story:** Como usuário do laboratório, eu quero visualizar os exames
realizados em um grid organizado, para que eu tenha visão geral dos exames.

#### Critérios de Aceitação

1. THE Grid_Header SHALL exibir as colunas na seguinte ordem: Id (Código),
   TabelaExamesId (Tabela), Sigla da Instituição, Nome da Instituição,
   Nome do Posto, Nome do Paciente, Data de Nascimento, Sequencial,
   DataIni (Data do Exame)
2. THE Grid_Header SHALL incluir uma coluna de Opções com botão de exclusão
   seguindo o padrão visual de ícones Font Awesome existente
   (`fa-sharp fa-solid fa-trash-can`)
3. THE Grid_Header SHALL utilizar a configuração padrão `configTable()` do
   `mydatatables.js` com filtro nativo do DataTables ativo (search box no
   topo e inputs no footer)
4. THE Grid_Header SHALL utilizar a tabela HTML com classes
   `display compact order-column stripe table-hover nowrap`
5. THE Grid_Header SHALL ter `<thead>`, `<tbody>` e `<tfoot>` conforme padrão
   existente em `Views/Pacientes/Index.cshtml`
6. THE Grid_Header SHALL ordenar por Id decrescente como padrão
   (`data-order='[[ 0, "desc" ]]'`)

### Requisito 5: Grid Detail (ItensExamesRealizados)

**User Story:** Como usuário do laboratório, eu quero visualizar os itens de
um exame ao clicar na linha do header, para que eu veja os detalhes sem sair
da tela.

#### Critérios de Aceitação

1. WHEN o usuário clicar em uma linha do Grid_Header, THE Sistema SHALL exibir
   o Grid_Detail com os itens de `ItensExamesRealizados` filtrados por
   `ExameRealizadoId` correspondente ao `ExamesRealizados.Id` selecionado
2. THE Grid_Detail SHALL iniciar oculto ao carregar a página
3. THE Grid_Detail SHALL exibir as colunas: ClasseExamesNome, RefExame,
   RefItem, ContaExame, Descricao, ValorItem, Etiquetas (Quantidade)
4. WHILE um Grid_Detail estiver aberto, WHEN o usuário clicar em outra linha
   do Grid_Header, THE Sistema SHALL fechar o detail anterior e abrir o novo
   (somente um detail aberto por vez)
5. THE Grid_Detail SHALL permanecer em tela abaixo da linha clicada, sem
   modal e sem navegação para outra página
6. THE Grid_Detail SHALL buscar os itens via requisição AJAX ao backend
   (`GET /ConsultarExames/ObterItensExame?exameRealizadoId=X`)

### Requisito 6: Exclusão de Exame

**User Story:** Como usuário do laboratório, eu quero excluir um exame
realizado que não possui resultados, para que eu possa corrigir lançamentos
indevidos.

#### Critérios de Aceitação

1. WHEN o usuário clicar no botão excluir de uma linha do Grid_Header,
   THE Sistema SHALL exibir confirmação via SweetAlert2 (`Swal.fire`) com
   botões "Sim" e "Não"
2. WHEN o usuário confirmar a exclusão, THE Controller SHALL verificar se
   TODOS os campos `Resultado` em `ItensExamesRealizados` do exame estão
   vazios ou nulos
3. IF algum campo `Resultado` em `ItensExamesRealizados` estiver preenchido,
   THEN THE Controller SHALL bloquear a exclusão e retornar mensagem:
   "Este exame não pode ser excluído pois um ou mais itens já possuem
   resultado lançado."
4. WHEN a exclusão for permitida, THE Controller SHALL excluir dentro de uma
   transação: os registros de `ItensExamesRealizados` do exame, o registro
   de `ExamesRealizados`, e os registros de `Requisitar` vinculados ao
   `ExameRealizadoId`
5. WHEN a exclusão for concluída com sucesso, THE Sistema SHALL atualizar o
   Grid_Header via AJAX sem reload completo da página
6. IF ocorrer erro durante a exclusão, THEN THE Controller SHALL realizar
   rollback da transação e retornar mensagem de erro ao usuário
7. THE Controller SHALL utilizar `_db.Database.BeginTransactionAsync()` para
   gerenciar a transação, conforme padrão existente em `RequisitarController`

### Requisito 7: View e Layout

**User Story:** Como desenvolvedor, eu quero que a view siga o padrão visual
existente, para que a tela seja consistente com as demais do sistema.

#### Critérios de Aceitação

1. THE View SHALL residir em `Views/ConsultarExames/Index.cshtml`
2. THE View SHALL incluir partial de menu seguindo o padrão de
   `_PartialMenuPacientes.cshtml` (adaptada para "Consultar Exames")
3. THE View SHALL incluir a section Scripts com
   `<partial name='_PartialDatatables' />` para inicialização do DataTables
4. THE View SHALL utilizar `@section Scripts` para scripts específicos da tela
5. THE View SHALL incluir área de filtros específicos (campos de input) acima
   do grid, com botão "Pesquisar" que submete os filtros via GET para a
   action Index
6. THE View SHALL manter o filtro nativo do DataTables (search box) ativo
   conforme configuração padrão de `configTable()`

### Requisito 8: Filtro Nativo DataTables

**User Story:** Como usuário do laboratório, eu quero utilizar o filtro
rápido do DataTables para buscar dentro dos dados já carregados, para que
eu tenha agilidade na consulta.

#### Critérios de Aceitação

1. THE Grid_Header SHALL manter o search box nativo do DataTables ativo
   (campo "Busca:" no topo direito)
2. THE Grid_Header SHALL manter os inputs de busca por coluna no `<tfoot>`
   conforme padrão de `configTable()` em `mydatatables.js`
3. THE Filtro_DataTables SHALL operar client-side sobre os dados já
   carregados na tabela, independente dos filtros backend

### Requisito 9: Qualidade e Conformidade

**User Story:** Como desenvolvedor, eu quero que a implementação compile sem
erros e siga todas as convenções do projeto, para que não haja regressão.

#### Critérios de Aceitação

1. THE Sistema SHALL compilar com 0 erros e 0 avisos após a implementação
2. THE Sistema SHALL utilizar `AsNoTracking()` em todas as queries de
   consulta (somente leitura)
3. THE Controller SHALL utilizar tratamento de exceções com log via
   `_eventLogHelper.LogEventViewer()` em operações de exclusão
4. THE View SHALL utilizar encoding UTF-8 com BOM conforme padrão do projeto
5. THE Sistema SHALL utilizar os mesmos pacotes NuGet já existentes, sem
   adicionar novos
6. THE Controller SHALL marcar blocos de código significativos com
   `//Feito pelo Kiro em dd/MM/yyyy` e `//..Kiro`
