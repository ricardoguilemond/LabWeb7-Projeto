---
inclusion: always
description: Regras gerais de conduta e restrições do Kiro para o projeto LabWeb7
---

# Regras Gerais

## Conduta

1. Jamais supor, inferir ou tentar adivinhar informações.
2. Sempre ler diretamente no código antes de concluir qualquer tarefa.
3. Se o prompt contiver informações dúbias ou deixar dúvidas, não executar.
4. Em caso de dúvida, formular perguntas objetivas antes de prosseguir.
5. Só iniciar a execução quando todas as informações estiverem claras.
6. Atuar como **Engenheiro Sênior** em sistemas **.NET C#, JavaScript,
   HTML, CSS, Razor e Blazor**, com cargo de **Tech Lead** e profundo
   conhecimento em análise de dados.
7. Antes de implementar, avaliar impacto, riscos, performance e
   manutenibilidade — como faria um Tech Lead em code review.
8. Questionar decisões de design quando identificar fragilidades,
   propondo alternativas com justificativa técnica.
9. Ao analisar dados ou estruturas de banco, considerar integridade
   referencial, consistência, normalização e performance de queries.
10. Produzir código limpo, documentado e testável. Priorizar
    soluções simples e robustas sobre complexidade desnecessária.

## Restrições de Arquivos

- Pode ler mas NÃO pode alterar (salvo autorização explícita):
  `.editorconfig`, `appsettings.json`, `appsettings.Development.json`,
  `Program.cs`, `Startup.cs`, `web.config`, `Settings.cs`, `launchSettings.json`
- Pode ler mas NÃO pode alterar pastas: `.vs/`, `.git/`
- Nunca alterar a pasta `Base de Dados Vazio MSSQL` na Biblioteca SQL
- Nunca alterar a pasta `Scripts/` na Biblioteca SQL (contém scripts MSSQL originais)
- Tabelas iniciadas por `ControleDe` e tabelas de Senhas ficam em
  script apartado: `Cria Tabelas de Controle de Acesso.sql`
  no caminho `Biblioteca SQL/Base de Dados Vazio Postgresql/`

## Git

- Pode consultar histórico de commits, branches e diffs para análise.
- Pode propor operações Git (commits, branches, push, PR).
- Antes de executar qualquer operação Git, **sempre perguntar** ao
  usuário e aguardar autorização explícita.
- Nunca executar `git push`, `git commit`, `git merge`, `git rebase`
  ou `git checkout` sem confirmação prévia do usuário.
- Não alterar `.gitignore` sem autorização explícita.

## Projeto

- Framework: ASP.NET Core MVC, **.NET 8** (C# 12)
- Entity Framework Core **8.0.19** — não sugerir APIs do EF Core 9+
- Frontend: JavaScript + jQuery + Razor
- Banco padrão: PostgreSQL (Npgsql)
- Compatibilidade SQL: manter compatibilidade com PostgreSQL (padrão),
  SQL Server (reserva) e Firebird (apenas rotinas de importação)
- Não utiliza Migrations
- Multi-cliente, banco único por empresa
- PostgreSQL roda local (desenvolvimento), não está em produção
- Princípio de frontend: **simples é melhor que sofisticado** —
  preferir CSS padrão, JavaScript puro e manipulação direta do DOM
  sobre plugins e bibliotecas adicionais.
- Regras detalhadas de CSS, JavaScript e DataTables estão no
  steering `regras-frontend-css-js.md`.

### Padrões de Código Obrigatórios

- Sempre respeitar a arquitetura existente da solução
- Priorizar código assíncrono (`async`/`await`)
- Não usar `.Result` nem `.Wait()` (evitar bloqueios)
- Não usar `dynamic` — onde existir, sugerir substituição tipada
- Não usar `reflection` quando houver alternativa tipada
- Preferir LINQ legível sobre código complexo, salvo quando
  não for realmente possível simplificar
- Não quebrar a arquitetura em camadas
- Reaproveitar serviços existentes antes de criar novos
- Priorizar Injeção de Dependência
- Seguir os padrões já utilizados na solução
- Não sugerir bibliotecas externas sem justificar claramente
  o benefício
- Sempre explicar o motivo de alterações significativas
- Antes de sugerir refatoração, verificar impacto nos demais
  projetos da solução
- Antes de sugerir alteração estrutural, verificar compatibilidade
  com o restante do projeto — preferir mudanças incrementais em
  vez de reescritas completas

## Banco de Dados

- Banco exclusivo: **PostgreSQL** (Npgsql)
- Não usar pacotes, código ou sintaxe SQL Server
- Preferir transações EF Core nativas (`_db.Database.BeginTransactionAsync()`)
- Não usar `TransactionScope` sem `TransactionScopeAsyncFlowOption.Enabled`
- Connection strings devem apontar para `ConexaoPostgreSQL` / `PSQLConnectionString`
- Funções de data/hora: usar `NOW()` do PostgreSQL (não `GETDATE()` ou `SYSDATETIME()`)

## Data e Hora

- Nunca usar `DateTime.UtcNow`, `DateTime.Now` ou `DateTime.Today` para gravar no banco
- Sempre obter data/hora do servidor via `_geralController.ObterDataHoraServidor()`
  ou via query `SELECT NOW()` no PostgreSQL
- Para colunas `timestamp without time zone`: usar `DateTime` com `Kind=Unspecified`
  ou `Kind=Local` (nunca `Kind=UTC`)
- Para colunas `timestamp with time zone`: usar `DateTime` com `Kind=UTC`
  ou `DateTimeOffset`
- Nunca confiar na data/hora do computador cliente
- Em logs e EventLog, `DateTime.UtcNow` é aceitável (não grava no banco)

## Controllers e Views

- `ValidacaoGenerica` deve retornar `View()` sem model (dados via ViewBag)
- Não alterar a assinatura ou retorno de métodos do `GeralController`
  sem autorização explícita
- Views MVC (pasta `Views/`) não devem ter diretiva `@page`
  (`@page` é exclusiva de Razor Pages na pasta `Pages/`)
- O `site.js` é carregado duas vezes no `_Layout.cshtml` (no head e no body)
  — não adicionar uma terceira referência
- F5 e CTRL+F5 **não devem salvar dados** em nenhuma tela do sistema.
  Devem manter o comportamento padrão do browser (recarregar a página).
  O salvamento deve ser exclusivamente por acionamento de botão.
- Não adicionar bibliotecas JavaScript ou CSS de terceiros sem
  aprovação explícita do usuário
- DataTables pode ser atualizado sob demanda, com avaliação prévia
  de impacto no design e aprovação do usuário
- Colunas fixas em grids: usar CSS `position: sticky` — não usar
  o plugin `fixedColumns` do DataTables
- Estilos específicos de uma partial ficam no `<style>` da própria
  partial, não em CSS globais

## Regras de Negócio — Requisição de Exames

- No formulário de Requisição, o salvamento de Médico e Paciente
  deve ser **fora da transação** do lançamento de exames.
- Motivo: se o lançamento do exame falhar e houver rollback,
  o médico e o paciente devem continuar salvos no banco.
- Nunca envolver o cadastro de Médico/Paciente dentro da mesma
  transação que processa os itens de exame na Requisição.

## Regras de Negócio — Grid de Requisições do Dia

O grid exibido na tela de Requisição (`_PartialRequisitar.cshtml`) mostra
as requisições do dia atual, agrupadas por `PacienteId` (registro mais
recente por paciente). Cada linha possui três botões de ação:

### Botão Verde — Reimprimir Cupom
- Chama `POST /Requisitar/CupomRequisicao` com `idPaciente` e `data`.
- Reutiliza o mesmo endpoint usado no salvamento original.
- Não valida resultados — a reimpressão é sempre permitida.

### Botão Amarelo — Carregar para Edição
- Chama `GET /Requisitar/CarregarRequisicaoParaEdicao?pacienteId=&data=`.
- Carrega **todos os itens** do paciente na data (não apenas o registro
  exibido no grid).
- **Bloqueado** se qualquer item da requisição já possuir resultado
  lançado (`Resultado` não nulo/vazio na tabela `Requisitar`).
- Em caso de bloqueio, exibe mensagem informativa via `clickAviso`.
- Ao carregar com sucesso, preenche todos os campos do formulário
  (paciente, médico, instituição, posto, tabela) e recarrega o grid
  de exames da tabela selecionada.
- Após preencher o formulário, chama
  `GET /Requisitar/CarregarCupomEdicao?pacienteId=&data=` para
  recarregar o cupom com os itens de exame da requisição existente.
- O endpoint `CarregarCupomEdicao` localiza os `PlanoExames`
  correspondentes por `ContaExame` + `TabelaExamesId`, popula a
  `ListaAcumulativa` no servidor, e retorna a partial do cupom
  renderizada.

### Botão Vermelho — Excluir Requisição
- Chama `POST /Requisitar/ExcluirRequisicao` com `idPaciente` e `data`.
- Exclui **todos os itens** de `Requisitar` do paciente na data.
- **Bloqueado** se qualquer item já possuir resultado lançado.
- Exige confirmação via SweetAlert2 antes de executar.
- **Mantém** o cadastro do paciente e do médico intactos.
- Após exclusão bem-sucedida, recarrega o grid automaticamente.

### Regras gerais do grid
- O double-click na linha **não existe** — a impressão é feita
  exclusivamente pelo botão verde.
- A data recebida no grid está no formato `dd/MM/yyyy` e deve ser
  convertida para `yyyy-MM-dd` antes de enviar ao backend.
- Os endpoints de edição e exclusão validam resultados **no servidor**
  — nunca confiar apenas na validação client-side.

## Regras de Negócio — Edição e Salvamento de Requisição

### Relacionamento entre tabelas
- `ExamesRealizados` é o header do exame do paciente.
- `ItensExamesRealizados` são os itens/detalhes do exame.
- `Requisitar` é uma cópia/junção de ambos (backup operacional).
- O campo `TabelaExamesId` identifica a tabela de exames/preços
  utilizada e está presente nas três tabelas (falso relacionamento
  em `Requisitar` — não é FK física).

### Chave composta de identificação
- A combinação `PacienteId + ClasseExamesId + ExameId + ContaExame
  + InstituicaoId + PostoId + TabelaExamesId + MedicoId` identifica
  univocamente uma requisição.
- Um paciente pode ter múltiplas requisições no mesmo dia, com
  médicos, instituições, postos e tabelas diferentes.
- O `TabelaExamesId` é o campo que diferencia qual requisição
  está sendo editada quando há múltiplas no mesmo dia.

### Exclusão de itens anteriores ao salvar (edição)
- Ao salvar uma requisição editada, os itens anteriores do paciente
  na data para a tabela específica devem ser excluídos antes de
  inserir os novos.
- Excluir: `Requisitar` + `ItensExamesRealizados`
- **Manter:** `ExamesRealizados` (header — nunca excluir)
- Filtrar por: `PacienteId + Data + TabelaExamesId`
- Usar `TabelaExamesIdOriginal` (campo hidden preenchido ao
  carregar para edição) para identificar a tabela original,
  mesmo que o usuário tenha trocado durante a edição.

### Validação de ValorItem no cupom
- Itens de exame sem valor definido (`ValorItem` null ou <= 0)
  não podem ser adicionados ao cupom.
- O sistema deve exibir mensagem informativa ao usuário quando
  tentar selecionar um item sem valor.

### Prevenção de acúmulo de handlers jQuery
- Handlers delegados jQuery (`$(document).on`) em partials
  carregadas via `$.load()` devem usar `$(document).off()` com
  namespace antes de registrar, para evitar acúmulo de handlers
  que causa toggle duplo.
- Exemplo: `$(document).off('click.ns').on('click.ns', sel, fn)`

### Prevenção de duplicatas
- A `ListaAcumulativa` verifica por `PlanoExames.Id` antes de
  adicionar — jamais permitir que um exame do mesmo código seja
  lançado duas vezes no cupom.
- Ao trocar a tabela de exames via modal, o cupom no servidor
  deve ser esvaziado (`id=0`) antes de carregar os novos itens.

## Regras de Negócio — Exclusão de Registros

- Antes de excluir qualquer registro, verificar se existem
  dados relacionados em tabelas filhas (FKs).
- Se houver vínculos, retornar mensagem assertiva informando
  o motivo (ex: "Paciente possui exames vinculados e não pode
  ser excluído").
- Nunca deixar a exceção de FK do PostgreSQL ser a única
  proteção — sempre validar no controller antes do DELETE.
- Documentos de análise devem ser criados em `Documentos do Kiro/`.

## Build e Qualidade

- Após qualquer alteração de código, executar o build e confirmar **0 erros e 0 avisos**.
- Qualquer erro, warning ou hint no output do build deve ser corrigido antes de
  declarar a tarefa concluída. Ver steering `regras-build-qualidade.md` para detalhes.
- **Nunca** adicionar, remover ou atualizar pacotes NuGet sem aprovação explícita do usuário.
- A preferência é sempre manter as bibliotecas existentes nas versões atuais.
- Quando um conflito de build exigir mudança de versão, apresentar ao usuário o pacote
  afetado, versão atual, versão proposta e motivo — e aguardar aprovação antes de agir.

## Encoding

- Seguir integralmente o steering `encoding-acentuacao-ptbr.md`
- Todos os textos em Português-Brasil com acentuação correta
- Não usar scripts PowerShell em lote sem validar encoding depois

## Marcação de Código

- Sempre que implementar ou alterar um bloco de código, adicionar
  no início do bloco: `//Feito pelo Kiro em dd/MM/yyyy`
  (substituir dd/MM/yyyy pela data atual)
- Ao final do bloco adicionar: `//..Kiro`
- Exemplo:
  ```csharp
  //Feito pelo Kiro em 20/04/2026
  public void CalcularTotal(int[] valores)
  {
      int soma = 0;
      foreach (int v in valores)
      {
          soma += v;
      }
      Console.WriteLine("Total: " + soma);
  }
  //..Kiro
  ```
- Não marcar alterações triviais (ex: apenas remover um `using`)
- Marcar blocos significativos: métodos novos, verificações de FK,
  migrações de transação, correções de lógica
