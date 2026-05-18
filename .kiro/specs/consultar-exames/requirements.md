# Requirements Document — Consultar Exames do Paciente (Master/Detail)

## Introdução

Evolução incremental da tela existente de **Cadastro de Pacientes**
(`/Pacientes`), adicionando consulta de exames do paciente em formato
Master/Detail inline no grid já existente. O grid atual de pacientes
permanece integralmente operacional — nenhuma funcionalidade, rota,
comportamento ou layout existente é alterado.

Ao clicar no registro do paciente no grid existente, um detalhe inline
é expandido abaixo da linha, exibindo os exames realizados (header) e
seus respectivos itens (sub-detail), carregados sob demanda via AJAX.

A funcionalidade é exclusivamente de **visualização** — não há botões
de edição, exclusão, cancelamento ou qualquer manutenção de dados.

## Glossário

- **Sistema**: Aplicação LabWeb7 (LabWebMvc.MVC)
- **Grid_Pacientes**: Grid DataTables existente na view
  `Views/Pacientes/Index.cshtml`, exibindo registros de `Pacientes`
- **Detail_Exames**: Área inline expandida abaixo da linha do paciente
  selecionado, exibindo exames realizados e seus itens
- **ExamesRealizados**: Tabela/model header dos exames do paciente
  (`LabWebMvc.MVC.Models.ExamesRealizados`). Possui FK para
  `Pacientes` via `PacienteId`, FK para `ClasseExames` via
  `ClasseExamesId`, FK para `Instituicao` via `InstituicaoId`,
  FK para `Postos` via `PostoId`
- **ItensExamesRealizados**: Tabela/model de itens/detalhes do exame
  (`LabWebMvc.MVC.Models.ItensExamesRealizados`). Possui FK para
  `ExamesRealizados` via `ExameRealizadoId`
- **ClasseExames**: Tabela/model que representa as Folhas de Exame
  (`LabWebMvc.MVC.Models.ClasseExames`). O campo `RefExame` é o
  nome/identificador da Folha
- **Pacientes**: Tabela/model de pacientes
  (`LabWebMvc.MVC.Models.Pacientes`)
- **Instituicao**: Tabela/model de instituições
  (`LabWebMvc.MVC.Models.Instituicao`). Campos: `Sigla`, `Nome`
- **Postos**: Tabela/model de postos de coleta
  (`LabWebMvc.MVC.Models.Postos`). Campo: `NomePosto`
- **Controller_Pacientes**: `PacientesController` existente em
  `Areas/Controllers/PacientesController.cs`
- **Filtro_Backend**: Filtros processados no servidor antes de
  retornar dados ao grid
- **Filtro_DataTables**: Filtro nativo do DataTables (search box)
  que opera client-side sobre os dados já carregados

## Requisitos

### Requisito 1: Preservação Integral da Tela Existente

**User Story:** Como usuário do laboratório, eu quero que a tela de
Cadastro de Pacientes continue funcionando exatamente como hoje, para
que nenhuma funcionalidade existente seja perdida.

#### Critérios de Aceitação

1. THE Sistema SHALL preservar integralmente o comportamento atual do
   Grid_Pacientes, incluindo busca textual, paginação, ordenação e
   filtros já existentes
2. THE Sistema SHALL preservar todas as rotas existentes do
   Controller_Pacientes sem alteração
3. THE Sistema SHALL preservar todos os botões de ação existentes no
   Grid_Pacientes (Consultar, Exames Realizados, Alterar, Excluir)
4. THE Sistema SHALL preservar a arquitetura, layout, CSS e
   responsividade atuais da tela de Pacientes

### Requisito 2: Comportamento do Detail Inline

**User Story:** Como usuário do laboratório, eu quero visualizar os
exames de um paciente diretamente no grid, para que eu não precise
navegar para outra tela.

#### Critérios de Aceitação

1. WHEN o usuário clicar em uma linha do Grid_Pacientes, THE Sistema
   SHALL exibir o Detail_Exames inline abaixo da linha do paciente
   selecionado
2. THE Detail_Exames SHALL iniciar fechado ao carregar a página
3. THE Detail_Exames SHALL permanecer em tela abaixo da linha
   clicada, sem modal e sem navegação para outra página
4. THE Detail_Exames SHALL manter o Grid_Pacientes visível em tela
   durante a exibição do detalhe
5. WHILE um Detail_Exames estiver aberto, WHEN o usuário clicar em
   outra linha do Grid_Pacientes, THE Sistema SHALL fechar o detail
   anterior e abrir o novo (somente um detail aberto por vez)
6. THE Sistema SHALL carregar os dados do Detail_Exames sob demanda
   via AJAX somente quando o paciente for clicado

### Requisito 3: Performance — Carregamento Sob Demanda

**User Story:** Como usuário do laboratório, eu quero que os exames
sejam carregados apenas quando eu clicar no paciente, para que a tela
não fique lenta.

#### Critérios de Aceitação

1. WHEN o usuário clicar em um paciente no Grid_Pacientes, THE
   Sistema SHALL buscar via AJAX apenas os exames e itens daquele
   paciente específico
2. THE Sistema SHALL carregar exames do paciente somente no momento
   do clique, sem pré-carregar exames de todos os pacientes no
   carregamento inicial da página
3. THE Controller_Pacientes SHALL utilizar `AsNoTracking()` em todas
   as queries de consulta de exames e itens

### Requisito 4: Filtros Backend Específicos

**User Story:** Como usuário do laboratório, eu quero filtrar os
pacientes por período de exame, nome e folha, para que eu encontre
rapidamente o paciente com exames no período desejado.

#### Critérios de Aceitação

1. THE Sistema SHALL exibir filtros backend acima do Grid_Pacientes
   com os campos: Data Inicial, Data Final, Nome do Paciente e
   Folha de Exame
2. THE Filtro de Período SHALL ter como valor padrão: Data Inicial =
   Hoje menos 3 dias, Data Final = Hoje
3. WHEN o filtro de Período for informado, THE Controller_Pacientes
   SHALL filtrar pacientes que possuam `ExamesRealizados.DataIni`
   dentro do range UTC correspondente ao período informado, utilizando
   `_geralController.ConverterDataLocalParaRangeUtc()`
4. WHEN o filtro de Nome do Paciente for informado, THE
   Controller_Pacientes SHALL filtrar por `Pacientes.NomePaciente`
   com comparação case-insensitive
5. THE Folha de Exame SHALL ser um ComboBox (select/dropdown)
   carregado com os valores existentes na tabela `ClasseExames`,
   exibindo o campo `RefExame` como texto e `Id` como valor
6. WHEN a Folha de Exame for selecionada, THE Controller_Pacientes
   SHALL retornar somente pacientes que possuam exames no período
   informado E que possuam exame vinculado àquela Folha
   (`ExamesRealizados.ClasseExamesId == folhaSelecionada`)
7. THE Controller_Pacientes SHALL processar os filtros no backend
   antes de retornar dados ao DataTables

### Requisito 5: Estrutura do Detail — Exames Realizados (Header)

**User Story:** Como usuário do laboratório, eu quero ver os dados
resumidos dos exames do paciente no detalhe expandido, para que eu
tenha visão geral dos exames realizados.

#### Critérios de Aceitação

1. THE Detail_Exames SHALL exibir os exames realizados do paciente
   com as seguintes colunas: ExameRealizadoId (exibição: "Cód.
   Exame"), Data Inicial (`DataIni`), Data Final (`DataFim`), Sigla
   Instituição (`Instituicao.Sigla`), Nome do Posto
   (`Postos.NomePosto` abreviado até no máximo 12 caracteres),
   Folha (`ClasseExames.RefExame`)
2. THE Detail_Exames SHALL buscar os exames via endpoint AJAX no
   Controller_Pacientes, filtrando por `PacienteId`
3. THE Detail_Exames SHALL formatar datas UTC para exibição local
   no formato `dd/MM/yyyy`
4. THE Detail_Exames SHALL abreviar o Nome do Posto para no máximo
   12 caracteres, truncando com reticências quando necessário

### Requisito 6: Estrutura do Detail — Itens dos Exames (Sub-Detail)

**User Story:** Como usuário do laboratório, eu quero ver os itens
de cada exame no detalhe expandido, para que eu tenha visão completa
dos exames e seus componentes.

#### Critérios de Aceitação

1. THE Detail_Exames SHALL exibir os itens de cada exame com as
   seguintes colunas: RefExame
   (`ItensExamesRealizados.RefExame`), RefItem
   (`ItensExamesRealizados.RefItem`), ContaExame
   (`ItensExamesRealizados.ContaExame` formatado conforme padrão
   atual do sistema), Descricao
   (`ItensExamesRealizados.Descricao`)
2. THE Detail_Exames SHALL buscar os itens via endpoint AJAX no
   Controller_Pacientes, filtrando por `ExameRealizadoId`
3. THE Detail_Exames SHALL ordenar os itens por
   `ItensExamesRealizados.OrdemItem`

### Requisito 7: Endpoint AJAX — Obter Exames do Paciente

**User Story:** Como desenvolvedor, eu quero um endpoint que retorne
os exames de um paciente específico, para que o frontend carregue os
dados sob demanda.

#### Critérios de Aceitação

1. THE Controller_Pacientes SHALL expor um endpoint
   `GET /Pacientes/ObterExamesPaciente` que receba o parâmetro
   `pacienteId` (int)
2. THE endpoint SHALL retornar JSON com os exames realizados do
   paciente, incluindo joins com `Instituicao`, `Postos` e
   `ClasseExames`
3. THE endpoint SHALL utilizar `AsNoTracking()` e
   `[TypeFilter(typeof(SessionFilter))]`
4. THE endpoint SHALL ordenar os exames por `DataIni` decrescente
5. THE endpoint SHALL retornar `Json(new { sucesso = true, exames })`

### Requisito 8: Endpoint AJAX — Obter Itens do Exame

**User Story:** Como desenvolvedor, eu quero um endpoint que retorne
os itens de um exame específico, para que o frontend carregue os
detalhes sob demanda.

#### Critérios de Aceitação

1. THE Controller_Pacientes SHALL expor um endpoint
   `GET /Pacientes/ObterItensExame` que receba o parâmetro
   `exameRealizadoId` (int)
2. THE endpoint SHALL retornar JSON com os itens do exame, projetando
   os campos: RefExame, RefItem, ContaExame, Descricao
3. THE endpoint SHALL utilizar `AsNoTracking()` e
   `[TypeFilter(typeof(SessionFilter))]`
4. THE endpoint SHALL ordenar os itens por `OrdemItem`
5. THE endpoint SHALL retornar `Json(new { sucesso = true, itens })`

### Requisito 9: Endpoint AJAX — Carregar Folhas para ComboBox

**User Story:** Como desenvolvedor, eu quero um endpoint que retorne
as folhas de exame disponíveis, para que o ComboBox de filtro seja
populado dinamicamente.

#### Critérios de Aceitação

1. THE Controller_Pacientes SHALL expor um endpoint
   `GET /Pacientes/ObterFolhasExame` que retorne a lista de
   `ClasseExames` com campos `Id` e `RefExame`
2. THE endpoint SHALL utilizar `AsNoTracking()` e
   `[TypeFilter(typeof(SessionFilter))]`
3. THE endpoint SHALL ordenar por `RefExame` ascendente
4. THE endpoint SHALL retornar
   `Json(new { sucesso = true, folhas })`

### Requisito 10: Proibição de Ações no Detail

**User Story:** Como usuário do laboratório, eu quero que o detalhe
de exames seja somente visualização, para que não haja risco de
alteração acidental de dados.

#### Critérios de Aceitação

1. THE Detail_Exames SHALL exibir dados exclusivamente em modo
   leitura, sem botões de edição, exclusão, cancelamento ou
   qualquer ícone de ação
2. THE Detail_Exames SHALL seguir integralmente o Bootstrap atual,
   CSS atual e responsividade atual do projeto

### Requisito 11: Comportamento Visual e Padrões do Projeto

**User Story:** Como desenvolvedor, eu quero que a implementação
siga os padrões visuais e técnicos do projeto, para que a
manutenção seja consistente.

#### Critérios de Aceitação

1. THE Sistema SHALL utilizar handlers delegados jQuery com
   namespace e `off()` antes de `on()` para evitar acúmulo
2. THE Sistema SHALL utilizar DataTables com `scrollX: true`,
   `autoWidth: false`, `responsive: false` no detail
3. THE Sistema SHALL utilizar colunas fixas via CSS
   `position: sticky` quando necessário (não usar plugin
   fixedColumns)
4. THE Sistema SHALL manter todos os textos em Português-Brasil
   com acentuação correta
5. THE Sistema SHALL marcar blocos de código significativos com
   `//Feito pelo Kiro em dd/MM/yyyy` e `//..Kiro`

### Requisito 12: Qualidade e Conformidade

**User Story:** Como desenvolvedor, eu quero que a implementação
compile sem erros e não cause regressão, para que o sistema
permaneça estável.

#### Critérios de Aceitação

1. THE Sistema SHALL compilar com 0 erros e 0 avisos após a
   implementação
2. THE Sistema SHALL utilizar os mesmos pacotes NuGet já existentes,
   sem adicionar novos
3. THE Sistema SHALL preservar o encoding UTF-8 com BOM nos arquivos
   `.cs` e `.cshtml` alterados
4. THE Sistema SHALL utilizar `ObterDataHoraUtc()` ou
   `SELECT NOW()` do PostgreSQL para obter data/hora — nunca
   `DateTime.Now` ou `DateTime.Today`
5. THE Sistema SHALL garantir zero regressão no comportamento atual
   da tela de Pacientes

### Requisito 13: Investigação Obrigatória (Fase 1)

**User Story:** Como desenvolvedor, eu quero que a implementação
seja precedida de investigação completa do código existente, para
que nenhuma decisão seja baseada em suposição.

#### Critérios de Aceitação

1. WHEN a implementação for iniciada, THE desenvolvedor SHALL
   investigar e documentar: Controller de Pacientes, View CSHTML
   existente, DataTables já implementado, filtros atuais, padrão
   AJAX do projeto
2. THE investigação SHALL identificar o relacionamento real entre:
   Pacientes, ExamesRealizados, ItensExamesRealizados, ClasseExames
3. THE investigação SHALL identificar a origem real da Folha de
   Exame (tabela `ClasseExames`, campo `RefExame`)
4. THE investigação SHALL gerar relatório com: arquivos analisados,
   controller encontrado, cshtml analisado, padrão DataTables
   encontrado, padrão AJAX encontrado, relacionamento real
   encontrado, origem real da Folha, riscos identificados e plano
   técnico
5. THE implementação SHALL ocorrer somente após aprovação do
   relatório de investigação pelo usuário
