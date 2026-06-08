# Implementation Plan: Lançamento de Resultados — Fase 4 (Impressão PDF + Baixa AM)

## Overview

Implementação dos endpoints de Impressão de Resultado em PDF, Baixa para
Arquivo-Morto e Exclusão de Item individual na tela de Resultado de Exames.
O controller `ResultadoExamesController.cs` já existe com os endpoints de
listagem, ObterItensExame e SalvarResultado. Esta fase adiciona os endpoints
de impressão, baixa e exclusão, e os botões correspondentes no frontend.

Linguagem: C# (.NET 8) + JavaScript/jQuery + Razor + iText 9.3.0

## Tasks

- [x] 1. Implementar endpoint ImprimirResultado com geração de PDF via iText
  - Adicionar método `[HttpGet] [Route("ResultadoExames/ImprimirResultado")]` com `[TypeFilter(typeof(SessionFilter))]`
  - Parâmetro: `int exameRealizadoId`
  - Carregar ExamesRealizados com Includes (Pacientes, Medicos, Instituicao, Postos, TabelaExames)
  - Carregar ItensExamesRealizados do exame (excluindo Folha geral: Substring(4,7) != "0000000"), ordenar por ContaExame ASC
  - Validar que todos os itens editáveis (últimos 4 dígitos > "0000") possuem Resultado não-nulo/vazio
  - Se faltar resultado: retornar `Json(new { sucesso = false, mensagem = "Resultado faltando em: ..." })`
  - Carregar dados da Empresa (`_db.Empresa.FirstOrDefault()`) para cabeçalho do PDF
  - Gerar PDF com iText 9 (namespace `iText.Kernel.Pdf`, `iText.Layout`):
    - Cabeçalho: nome empresa, subtítulo, CNPJ, telefone, endereço
    - Dados paciente: nome, Id, nascimento, CPF
    - Dados médico: nome, CRM
    - Tabela de resultados com colunas: Folha, Descrição, Resultado, Unidade, Referência
    - Itens "Principal" (últimos 4 = "0000") como cabeçalho de grupo (bold)
    - Sub-itens (últimos 4 > "0000") como linhas normais
    - Rodapé: data/hora de impressão
    - Formato A4 com margens adequadas
  - Salvar PDF em: `{ContentRootPath}/App_Data/Resultados/{CNPJ}/{yyyyMM}/{ExameId}.pdf`
  - Criar diretórios inexistentes com `Directory.CreateDirectory()`
  - Atualizar ExamesRealizados: `DataEntrega = _geralController.ObterDataHoraUtc()`, `Situacao = 3`, `TotalImpresso += 1`
  - `await _db.SaveChangesAsync()`
  - Retornar `File(pdfBytes, "application/pdf", "Resultado_{ExameId}.pdf")`
  - Tratamento de exceção com log via `_eventLogHelper` e retorno JSON de erro
  - Marcar bloco com `//Feito pelo Kiro em dd/MM/yyyy` e `//..Kiro`
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 1.10, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9_

- [x] 2. Implementar endpoint BaixarExame (Arquivo-Morto) com transação e proteção de concorrência
  - Adicionar método `[HttpPost] [Route("ResultadoExames/BaixarExame")]` com `[TypeFilter(typeof(SessionFilter))]`
  - Parâmetro: `int exameRealizadoId`
  - Carregar ExamesRealizados com tracking (sem AsNoTracking)
  - Validar existência do exame
  - Verificar `Situacao != 11` (proteção de concorrência); se == 11 retornar erro "Exame está sendo baixado por outro terminal"
  - Guardar `situacaoAnterior` para restauração em caso de falha
  - Marcar `Situacao = 11` + `await _db.SaveChangesAsync()` (lock imediato, fora da transação)
  - Iniciar transação: `await _db.Database.BeginTransactionAsync()`
  - Carregar todos os ItensExamesRealizados do exame
  - Obter `ClasseExamesId` do primeiro item (para o header AM)
  - Criar `ExamesRealizadosAM` mapeando todos os campos (OrigemId = Id original, Baixado = 1, Situacao = 4)
  - `_db.ExamesRealizadosAM.Add(...)` + `await _db.SaveChangesAsync()` (para obter Id gerado)
  - Para cada item: criar `ItensExamesRealizadosAM` (OrigemAmid = item.Id, ExameRealizadoAMId = novoAM.Id, Baixado = 1)
  - `_db.ItensExamesRealizadosAM.AddRange(...)` + `await _db.SaveChangesAsync()`
  - Excluir itens originais: `_db.ItensExamesRealizados.RemoveRange(itens)`
  - Excluir header original: `_db.ExamesRealizados.Remove(exame)`
  - `await _db.SaveChangesAsync()`
  - `await transaction.CommitAsync()`
  - Retornar `Json(new { sucesso = true, mensagem = "Exame baixado para Arquivo-Morto com sucesso." })`
  - No catch: `await transaction.RollbackAsync()`, restaurar `Situacao = situacaoAnterior`, `SaveChanges`
  - Log via `_eventLogHelper`
  - Marcar bloco com `//Feito pelo Kiro em dd/MM/yyyy` e `//..Kiro`
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 4.1, 4.2, 4.3_

- [x] 3. Implementar endpoint ExcluirItem
  - Adicionar método `[HttpPost] [Route("ResultadoExames/ExcluirItem")]` com `[TypeFilter(typeof(SessionFilter))]`
  - Parâmetro: `int itemId`
  - Buscar item em `_db.ItensExamesRealizados.FindAsync(itemId)`
  - Validar existência; se não existir retornar erro
  - `_db.ItensExamesRealizados.Remove(item)`
  - `await _db.SaveChangesAsync()`
  - Retornar `Json(new { sucesso = true, mensagem = "Item excluído com sucesso." })`
  - Tratamento de exceção com log
  - Marcar bloco com `//Feito pelo Kiro em dd/MM/yyyy` e `//..Kiro`
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [x] 4. Implementar frontend — Botões de Impressão, Baixa e Exclusão na View
  - Adicionar seção de botões de ação abaixo do painel informativo (`#painelInfoExame`):
    - Botão "Imprimir Resultado" (ícone fa-print, cor verde)
    - Botão "Baixar Arquivo-Morto" (ícone fa-box-archive, cor azul)
  - Botões visíveis apenas quando um exame está selecionado no grid header
  - Adicionar botão "Excluir" (ícone fa-trash, cor vermelho) em cada linha editável do grid de itens
  - Implementar handler JS para "Imprimir Resultado":
    - Obter `exameRealizadoId` do exame selecionado
    - Chamar `GET /ResultadoExames/ImprimirResultado?exameRealizadoId=X` como download (window.open ou fetch+blob)
    - Se JSON de erro: exibir via `clickAviso`
    - Se PDF: abrir em nova aba
    - Atualizar status no grid header para "Impresso" (verde escuro)
    - Atualizar `TotalImpresso` no painel info
  - Implementar handler JS para "Baixar Arquivo-Morto":
    - Confirmação via `Swal.fire` ("Deseja baixar este exame para Arquivo-Morto?")
    - Se confirmado: `POST /ResultadoExames/BaixarExame` com `{ exameRealizadoId }`
    - Se sucesso: remover linha do grid header, esconder painel info e grid itens
    - Se erro: exibir via `clickAviso`
  - Implementar handler JS para "Excluir Item":
    - Confirmação via `Swal.fire` ("Deseja excluir este item?")
    - Se confirmado: `POST /ResultadoExames/ExcluirItem` com `{ itemId }`
    - Se sucesso: remover TR do grid de itens
    - Se erro: exibir via `clickAviso`
  - Usar namespace nos handlers: `$(document).off('click.impressao').on(...)`
  - Marcar bloco com `@*Feito pelo Kiro em dd/MM/yyyy*@` e `@*..Kiro*@`
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_

- [x] 5. Verificar build e qualidade
  - Executar `dotnet build "LabWebMvc.MVC/LabWebMvc.MVC.csproj"` e confirmar 0 erros e 0 avisos
  - Verificar encoding UTF-8 com BOM nos arquivos .cs alterados
  - Verificar marcação `//Feito pelo Kiro` em todos os blocos significativos
  - Confirmar que nenhum pacote NuGet foi adicionado, removido ou alterado
  - _Requirements: 7.1, 7.2, 7.3, 7.4_

## Notes

- O controller `ResultadoExamesController.cs` já existe com: Index, ObterItensExame, SalvarResultado
- A view `Index.cshtml` já tem: filtros, grid header, painel info, grid itens editável, handler ENTER
- iText 9.3.0 já está no `LabWebMvc.MVC.csproj` — não precisa instalar
- As entidades `ExamesRealizadosAM` e `ItensExamesRealizadosAM` já existem no DbContext
- O campo `ClasseExamesId` é obrigatório no AM mas não existe no ExamesRealizados header — usar do primeiro item
- ExamesRealizadosAM não tem campo `ClasseExamesId` diretamente — VERIFICAR modelo real antes de mapear (o campo existe na entidade)
- Usar `_geralController.ObterDataHoraUtc()` para DataEntrega (nunca DateTime.UtcNow para banco)
- Transação EF Core nativa: `_db.Database.BeginTransactionAsync()` — não usar TransactionScope
- O lock `Situacao = 11` deve ser aplicado ANTES da transação (SaveChanges separado)
- Handlers jQuery com namespace e off() antes de on() para evitar acúmulo
- SweetAlert2 para confirmações destrutivas, clickAviso para mensagens informativas
