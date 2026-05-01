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

- Framework: .NET 8 (C#), Frontend: JavaScript + jQuery + Razor, Banco: PostgreSQL
- Não utiliza Migrations
- Multi-cliente, banco único por empresa
- PostgreSQL roda local (desenvolvimento), não está em produção
- Princípio de frontend: **simples é melhor que sofisticado** —
  preferir CSS padrão, JavaScript puro e manipulação direta do DOM
  sobre plugins e bibliotecas adicionais.
- Regras detalhadas de CSS, JavaScript e DataTables estão no
  steering `regras-frontend-css-js.md`.

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
