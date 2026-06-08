# Requirements: Lançamento de Resultados — Fase 4 (Impressão PDF + Baixa AM)

## Accepted Requirements

### Requirement 1: Endpoint ImprimirResultado
**User Story:** Como técnico do laboratório, quero imprimir o resultado de um exame em PDF para entregar ao paciente.
#### Acceptance Criteria
- 1.1 Endpoint `GET /ResultadoExames/ImprimirResultado?exameRealizadoId=X` com SessionFilter
- 1.2 Validar que todos os itens editáveis do exame possuem Resultado preenchido (não nulo/vazio) antes de gerar o PDF
- 1.3 Se houver resultado faltando, retornar JSON com `{ sucesso: false, mensagem }` informando quais itens faltam
- 1.4 Gerar PDF com dados completos: paciente, médico, instituição, itens com resultado, unidade e referência
- 1.5 Salvar PDF em: `{ContentRootPath}/App_Data/Resultados/{CNPJ}/{yyyyMM}/{ExameId}.pdf`
- 1.6 Criar diretórios inexistentes automaticamente
- 1.7 Após gerar PDF: marcar `DataEntrega = ObterDataHoraUtc()` no ExamesRealizados
- 1.8 Após gerar PDF: marcar `Situacao = 3` (Impresso) no ExamesRealizados
- 1.9 Incrementar `TotalImpresso` (+1) a cada impressão
- 1.10 Retornar o PDF como FileResult para download no browser

### Requirement 2: Geração de PDF com iText
**User Story:** Como sistema, devo gerar o PDF de resultado usando a biblioteca iText já presente no projeto.
#### Acceptance Criteria
- 2.1 Usar iText (pacote `itext` versão 9.3.0 já instalado no LabWebMvc.MVC.csproj)
- 2.2 PDF deve conter: cabeçalho com dados da empresa (nome, CNPJ, endereço, telefone)
- 2.3 PDF deve conter: dados do paciente (nome, Id, nascimento, CPF)
- 2.4 PDF deve conter: dados do médico (nome, CRM)
- 2.5 PDF deve conter: tabela de resultados (Folha, Descrição, Resultado, Unidade, Referência)
- 2.6 PDF deve agrupar itens por Folha com cabeçalho visual (como na tela)
- 2.7 Itens "Principal" (ContaExame termina em "0000" e pos 5-7 > "000") aparecem como cabeçalho
- 2.8 Formato do papel: A4, margens razoáveis
- 2.9 Data de impressão no rodapé do PDF

### Requirement 3: Endpoint BaixarExame (Arquivo-Morto)
**User Story:** Como administrador, quero baixar exames impressos para arquivo-morto, liberando o grid principal.
#### Acceptance Criteria
- 3.1 Endpoint `POST /ResultadoExames/BaixarExame` com parâmetro `int exameRealizadoId` e SessionFilter
- 3.2 Validar que `Situacao != 11` (não está sendo baixado por outro terminal)
- 3.3 Marcar `Situacao = 11` (lock temporário) imediatamente antes de iniciar
- 3.4 Copiar `ExamesRealizados` → `ExamesRealizadosAM` (mapeando todos os campos, OrigemId = Id original)
- 3.5 Copiar `ItensExamesRealizados` → `ItensExamesRealizadosAM` (OrigemAmid = Id do ExamesRealizadosAM criado)
- 3.6 Após cópia bem-sucedida: excluir `ItensExamesRealizados` do exame original
- 3.7 Após exclusão de itens: excluir `ExamesRealizados` original
- 3.8 Toda operação dentro de uma transação EF Core (`BeginTransactionAsync`)
- 3.9 Em caso de falha: rollback automático + reverter `Situacao` para valor anterior
- 3.10 Retornar JSON `{ sucesso, mensagem }`

### Requirement 4: Proteção de Concorrência
**User Story:** Como sistema, devo impedir que dois terminais baixem o mesmo exame simultaneamente.
#### Acceptance Criteria
- 4.1 Antes de iniciar a baixa, verificar `Situacao == 11` (lock por outro terminal)
- 4.2 Se `Situacao == 11`, retornar JSON com mensagem "Exame está sendo baixado por outro terminal"
- 4.3 O lock `Situacao = 11` é removido automaticamente ao final da baixa (sucesso ou falha)

### Requirement 5: Endpoint ExcluirItem
**User Story:** Como técnico, quero excluir um item individual de um exame quando lançado erroneamente.
#### Acceptance Criteria
- 5.1 Endpoint `POST /ResultadoExames/ExcluirItem` com parâmetro `int itemId` e SessionFilter
- 5.2 Validar que o item existe em `ItensExamesRealizados`
- 5.3 Excluir o item
- 5.4 Retornar JSON `{ sucesso, mensagem }`
- 5.5 O frontend deve solicitar confirmação via SweetAlert2 antes de chamar

### Requirement 6: Frontend — Botões de Impressão, Baixa e Exclusão
**User Story:** Como técnico, quero botões na tela para acionar impressão, baixa e exclusão.
#### Acceptance Criteria
- 6.1 Botão "Imprimir Resultado" visível quando um exame está selecionado no grid header
- 6.2 Botão "Baixar Arquivo-Morto" visível quando um exame está selecionado
- 6.3 Botão "Excluir Item" em cada linha editável do grid de itens
- 6.4 Confirmação SweetAlert2 antes de baixar ou excluir
- 6.5 Após impressão: abrir PDF em nova aba ou download automático
- 6.6 Após baixa: remover linha do grid header e limpar grid de itens
- 6.7 Após exclusão de item: remover linha do grid de itens

### Requirement 7: Build e Qualidade
**User Story:** Como desenvolvedor, quero garantir que o código compila sem erros.
#### Acceptance Criteria
- 7.1 Build `dotnet build LabWebMvc.MVC/LabWebMvc.MVC.csproj` com 0 erros e 0 avisos
- 7.2 Encoding UTF-8 com BOM em arquivos .cs
- 7.3 Marcação `//Feito pelo Kiro em dd/MM/yyyy` em blocos significativos
- 7.4 Nenhum pacote NuGet adicionado ou alterado
