# Implementation Plan: Consultar Exames do Paciente (Master/Detail)

## Overview

Implementação incremental da consulta de exames na tela de Pacientes,
adicionando filtros backend, endpoints AJAX e detail inline
(expand/collapse). A abordagem segue: investigação → backend
(endpoints) → frontend (filtros + detail) → verificação final.

Linguagem: C# (.NET 8) + JavaScript/jQuery + Razor

## Tasks

- [x] 1. Investigação obrigatória (Fase 1) — Relatório de análise
  - Investigar `Areas/Controllers/PacientesController.cs`: métodos existentes, assinatura do Index, DbContext injetado, imports
  - Investigar `Views/Pacientes/Index.cshtml`: estrutura HTML, DataTables config, scripts existentes, partial menus
  - Investigar models: `Pacientes`, `ExamesRealizados`, `ItensExamesRealizados`, `ClasseExames` — confirmar FKs e navigation properties
  - Investigar padrão AJAX existente no projeto (ex: `ConsultarExames/Index.cshtml`) para replicar o padrão de detail inline
  - Investigar `GeralController`: método `ConverterDataLocalParaRangeUtc()` — assinatura e uso
  - Investigar extension methods: `ToLocalString()`, `FormatarContaExameSem11()` — localização e assinatura
  - Gerar relatório em `Documentos do Kiro/investigacao-consultar-exames.md` com: arquivos analisados, controller encontrado, cshtml analisado, padrão DataTables, padrão AJAX, relacionamentos reais, riscos e plano técnico
  - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5_

- [x] 2. Implementar endpoints backend no PacientesController
  - [x] 2.1 Implementar endpoint `ObterFolhasExame`
    - Criar método `[HttpGet]` com `[Route("Pacientes/ObterFolhasExame")]`
    - Aplicar `[TypeFilter(typeof(SessionFilter))]`
    - Query: `_db.ClasseExames.AsNoTracking().OrderBy(c => c.RefExame).Select(c => new { c.Id, c.RefExame })`
    - Retornar `Json(new { sucesso = true, folhas })`
    - Tratamento de exceção com `_eventLogHelper` e retorno `{ sucesso = false, mensagem }`
    - Marcar bloco com `//Feito pelo Kiro em dd/MM/yyyy` e `//..Kiro`
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [x] 2.2 Implementar endpoint `ObterExamesPaciente`
    - Criar método `[HttpGet]` com `[Route("Pacientes/ObterExamesPaciente")]`
    - Aplicar `[TypeFilter(typeof(SessionFilter))]`
    - Parâmetro: `int pacienteId`
    - Query com `AsNoTracking()`, `Include(Instituicao)`, `Include(Postos)`, `Include(ClasseExames)`
    - Ordenar por `DataIni` decrescente
    - Projetar: Id, DataIni formatada dd/MM/yyyy, DataFim formatada, Sigla Instituição, NomePosto abreviado (max 12 chars), Folha (RefExame)
    - Retornar `Json(new { sucesso = true, exames })`
    - Tratamento de exceção com log
    - Marcar bloco com `//Feito pelo Kiro em dd/MM/yyyy` e `//..Kiro`
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 5.1, 5.3, 5.4_

  - [x] 2.3 Implementar endpoint `ObterItensExame`
    - Criar método `[HttpGet]` com `[Route("Pacientes/ObterItensExame")]`
    - Aplicar `[TypeFilter(typeof(SessionFilter))]`
    - Parâmetro: `int exameRealizadoId`
    - Query com `AsNoTracking()`, filtrar por `ExameRealizadoId`, ordenar por `OrdemItem`
    - Projetar: RefExame, RefItem, ContaExame (formatado via `FormatarContaExameSem11()`), Descricao
    - Retornar `Json(new { sucesso = true, itens })`
    - Tratamento de exceção com log
    - Marcar bloco com `//Feito pelo Kiro em dd/MM/yyyy` e `//..Kiro`
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 6.1, 6.2, 6.3_

  - [ ]* 2.4 Escrever teste unitário para lógica de abreviação de NomePosto
    - **Property 4: Abreviação de NomePosto respeita limite de 12 caracteres**
    - **Validates: Requirements 5.1, 5.4**
    - Testar strings de tamanhos variados: 0, 1, 11, 12, 13, 50, 100 caracteres
    - Verificar: se length > 12 → resultado tem 12 chars e termina com "..."
    - Verificar: se length <= 12 → resultado é igual à string original

- [x] 3. Estender método Index com filtros backend
  - [x] 3.1 Adicionar parâmetros de filtro ao método Index existente
    - Estender assinatura: `string? dataInicial, string? dataFinal, string? nomePaciente, int? folhaId`
    - Preservar integralmente o comportamento atual do parâmetro `Conteudo`
    - Novos filtros só se aplicam quando `Conteudo` está vazio
    - _Requirements: 1.1, 1.2, 4.7_

  - [x] 3.2 Implementar lógica de filtro por período
    - Usar `_geralController.ConverterDataLocalParaRangeUtc()` para converter datas
    - Filtrar pacientes que possuam `ExamesRealizados.DataIni` dentro do range UTC
    - Usar `AsNoTracking()` na query
    - _Requirements: 4.3, 3.3_

  - [x] 3.3 Implementar lógica de filtro por nome e folha
    - Filtro nome: `NomePaciente.Contains(nomePaciente)` case-insensitive
    - Filtro folha: pacientes com `ExamesRealizados.ClasseExamesId == folhaId`
    - Combinar filtros aditivamente (AND)
    - Marcar bloco com `//Feito pelo Kiro em dd/MM/yyyy` e `//..Kiro`
    - _Requirements: 4.4, 4.6_

- [x] 4. Checkpoint — Verificar build backend
  - Ensure all tests pass, ask the user if questions arise.
  - Executar `dotnet build` e confirmar 0 erros e 0 avisos
  - Verificar que rotas existentes continuam respondendo

- [x] 5. Implementar filtros backend no frontend (HTML + JS)
  - [x] 5.1 Adicionar HTML dos filtros acima do grid
    - Criar `<div id="filtrosPacientesExames">` com form GET
    - Campos: Data Inicial (type=date, padrão hoje-3), Data Final (type=date, padrão hoje), Nome (text), Folha (select), Botão Pesquisar
    - Estilo inline: flex-wrap, gap 8px, border, border-radius, background #f9f9f9
    - Posicionar antes do grid existente `#modeloTable`
    - _Requirements: 4.1, 4.2_

  - [x] 5.2 Implementar carregamento AJAX do ComboBox de Folhas
    - Ao carregar a página, chamar `GET /Pacientes/ObterFolhasExame`
    - Popular o `<select>` com option value=Id e text=RefExame
    - Adicionar option vazia "Todas" como padrão
    - Preservar seleção atual via query string
    - _Requirements: 4.5, 9.1_

  - [x] 5.3 Adicionar CSS dos filtros no bloco `<style>` da view
    - Estilos para responsividade dos filtros em telas menores
    - Manter consistência visual com o restante da tela
    - _Requirements: 11.4_

- [x] 6. Implementar detail inline de exames (expand/collapse + AJAX)
  - [x] 6.1 Implementar handler de clique na linha do paciente
    - Handler delegado com namespace: `$(document).off('click.detailExames').on('click.detailExames', '#modeloTable tbody tr', handler)`
    - Ignorar cliques na coluna de opções (`.grid_fundo_opcoes`)
    - Ignorar cliques em linhas de detalhe (`.detail-row`, `.detail-header-row`)
    - Fechar detail anterior antes de abrir novo (somente um aberto por vez)
    - Extrair `pacienteId` da linha clicada
    - _Requirements: 2.1, 2.5, 11.1_

  - [x] 6.2 Implementar chamada AJAX para ObterExamesPaciente
    - Chamar `GET /Pacientes/ObterExamesPaciente?pacienteId=X`
    - Tratar resposta: se `sucesso=true` e `exames.length > 0` → renderizar detail
    - Se `exames.length === 0` → exibir mensagem "Nenhum exame encontrado"
    - Se erro de rede → exibir mensagem via `clickAviso`
    - Prevenir clique duplo (flag de carregamento)
    - _Requirements: 2.6, 3.1, 3.2, 5.2_

  - [x] 6.3 Implementar renderização do detail inline (TRs injetados)
    - Injetar TR de header com título "Exames Realizados" e colunas
    - Injetar TRs de dados com: Cód. Exame, Data Ini, Data Fim, Sigla Instituição, Posto, Folha
    - Aplicar classes CSS: `.detail-row`, `.detail-header-row`, `.detail-parent-highlight`
    - Highlight na linha do paciente clicado
    - _Requirements: 2.2, 2.3, 2.4, 5.1, 5.3_

- [x] 7. Implementar sub-detail de itens do exame
  - [x] 7.1 Implementar handler de clique na linha do exame no detail
    - Handler delegado com namespace: `$(document).off('click.detailItens').on('click.detailItens', '.detail-exame-row', handler)`
    - Extrair `exameRealizadoId` da linha clicada
    - Fechar sub-detail anterior antes de abrir novo
    - _Requirements: 6.2_

  - [x] 7.2 Implementar chamada AJAX para ObterItensExame e renderização
    - Chamar `GET /Pacientes/ObterItensExame?exameRealizadoId=Y`
    - Renderizar TRs de itens com: RefExame, RefItem, ContaExame, Descrição
    - Aplicar classe `.detail-item-row`
    - Tratar lista vazia e erros de rede
    - _Requirements: 6.1, 6.3, 8.1, 8.4_

  - [x] 7.3 Adicionar CSS do detail e sub-detail no bloco `<style>`
    - Estilos para `.detail-row`, `.detail-header-row`, `.detail-parent-highlight`, `.detail-exame-row`, `.detail-item-row`
    - Seguir padrão visual de `ConsultarExames/Index.cshtml`
    - Garantir que detail não interfere no grid existente
    - _Requirements: 10.1, 10.2, 11.2, 11.3_

- [x] 8. Checkpoint — Verificar integração frontend + backend
  - Ensure all tests pass, ask the user if questions arise.
  - Executar `dotnet build` e confirmar 0 erros e 0 avisos
  - Verificar que o grid existente mantém busca, paginação, ordenação e ações
  - Verificar que filtros submetem corretamente via GET
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 12.5_

- [x] 9. Verificação final — Build, encoding e marcação
  - [x] 9.1 Executar build completo e confirmar 0 erros e 0 avisos
    - Comando: `dotnet build "LabWebMvc.MVC/LabWebMvc.MVC.csproj"`
    - Resultado esperado: 0 Erro(s) e 0 Aviso(s)
    - _Requirements: 12.1_

  - [x] 9.2 Verificar encoding UTF-8 com BOM nos arquivos .cs e .cshtml alterados
    - Confirmar BOM (EF BB BF) nos primeiros bytes de cada arquivo alterado
    - Confirmar acentuação correta em todos os textos pt-BR
    - _Requirements: 12.3_

  - [x] 9.3 Verificar marcação de código `//Feito pelo Kiro` em todos os blocos implementados
    - Confirmar que cada bloco significativo tem marcação de início e fim
    - Confirmar formato da data: dd/MM/yyyy
    - _Requirements: 11.5_

  - [x] 9.4 Verificar que nenhum pacote NuGet foi adicionado ou alterado
    - Comparar `.csproj` antes e depois — nenhuma alteração em PackageReference
    - _Requirements: 12.2_

## Notes

- Tasks marcadas com `*` são opcionais e podem ser ignoradas para MVP mais rápido
- A Task 1 (Investigação) é pré-requisito obrigatório — implementação só inicia após aprovação do relatório
- Cada task referencia requisitos específicos para rastreabilidade
- Checkpoints garantem validação incremental
- O detail inline NÃO usa DataTables — é TR injetado via DOM (mais leve)
- Todos os endpoints usam `AsNoTracking()` (somente leitura)
- Handlers jQuery usam namespace e `off()` antes de `on()` para evitar acúmulo
- Property 4 (abreviação NomePosto) é a única propriedade adequada para teste unitário/PBT por ser função pura
