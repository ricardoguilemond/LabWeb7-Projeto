# Design: Lançamento de Resultados — Fase 4 (Impressão PDF + Baixa AM)

## Architecture

### Componentes Envolvidos

| Componente | Projeto | Arquivo |
|-----------|---------|---------|
| Controller | LabWebMvc.MVC | Areas/Controllers/ResultadoExamesController.cs |
| View | LabWebMvc.MVC | Views/ResultadoExames/Index.cshtml |
| Entidades | ModeloDeDados | Models/Examesrealizados.cs, Itensexamesrealizados.cs |
| Entidades AM | ModeloDeDados | Models/Examesrealizadosam.cs, Itensexamesrealizadosam.cs |
| DbContext | LabWebMvc.MVC | Models/db.cs |
| PDF (iText) | LabWebMvc.MVC | Pacote itext 9.3.0 (já instalado) |
| GeralController | LabWebMvc.MVC | Areas/Controllers/GeralController.cs |
| BLL Utilitários | BLL | ConversoresPdf.cs, UtilBLL.cs |

### Padrão de PDF com iText 9.x

O projeto já possui iText 9.3.0 no LabWebMvc.MVC.csproj. A geração do PDF
de resultado deve usar a API do iText 9 (namespace `iText.Kernel.Pdf`,
`iText.Layout`). Estrutura:

```csharp
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

// Criar PDF
using var ms = new MemoryStream();
using var writer = new PdfWriter(ms);
using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer);
using var doc = new Document(pdfDoc, iText.Kernel.Geom.PageSize.A4);

// Adicionar conteúdo
doc.Add(new Paragraph("Cabeçalho Empresa"));
// ... tabela de resultados
doc.Close();

// Salvar no disco
File.WriteAllBytes(caminhoArquivo, ms.ToArray());
```

### Mapeamento ExamesRealizados → ExamesRealizadosAM

| Campo Origem (ExamesRealizados) | Campo Destino (ExamesRealizadosAM) |
|-------------------------------|------------------------------------|
| Id | OrigemId |
| PacienteId | PacienteId |
| TabelaExamesId | TabelaExamesId |
| InstituicaoId | InstituicaoId |
| PostoId | PostoId |
| MedicoId | MedicoId |
| ClasseExamesId (via item) | ClasseExamesId |
| Sequencial | Sequencial |
| LaboratorioApoio | LaboratorioApoio |
| ControleApoio | ControleApoio |
| HistoricoClinico | HistoricoClinico |
| ExameColado | ExameColado |
| ExameColadoImagens | ExameColadoImagens |
| TravaColado | TravaColado |
| DataIni | DataIni |
| DataFim | DataFim |
| Liberacao | Liberacao |
| DataExame | DataExame |
| DataColeta | DataColeta |
| DataEntrega | DataEntrega |
| Baixado | 1 (sempre) |
| EnviarEmail | EnviarEmail |
| Situacao | 4 (Arquivo-Morto) |
| TotalImpresso | TotalImpresso |

### Mapeamento ItensExamesRealizados → ItensExamesRealizadosAM

| Campo Origem | Campo Destino |
|-------------|---------------|
| Id | OrigemAmid |
| PacienteId | PacienteId |
| ClasseExamesId | ClasseExamesId |
| ClasseExamesNome | ClasseExamesNome |
| ExameRealizadoId → novo AM Id | ExameRealizadoAMId |
| TabelaExamesId | TabelaExamesId |
| OrdemItem | OrdemItem |
| RefExame | RefExame |
| RefItem | RefItem |
| ContaExame | ContaExame |
| Todos campos cito/lab/resultado | Mapeamento direto |
| Laudo | Laudo |
| ValorItem | ValorItem |
| Etiquetas | Etiquetas |
| DataEntregaParcial | DataEntregaParcial |
| Liberado | Liberado |
| Baixado | 1 (sempre) |

### Nota sobre ClasseExamesId no ExamesRealizadosAM

O campo `ClasseExamesId` é obrigatório no AM (FK para ClasseExames).
No `ExamesRealizados` não existe `ClasseExamesId` diretamente — ele
existe apenas nos itens. Para o header AM, usar o `ClasseExamesId`
do primeiro item do exame (ou 0 se não houver itens, embora essa
situação não deva ocorrer).

### Fluxo de Baixa (Transação)

```
1. Verificar Situacao != 11
2. Marcar Situacao = 11 (lock) + SaveChanges
3. BeginTransaction
4. Criar ExamesRealizadosAM (com OrigemId)
5. SaveChanges (obtém Id do AM)
6. Para cada ItensExamesRealizados: criar ItensExamesRealizadosAM
7. SaveChanges
8. Excluir todos ItensExamesRealizados do exame
9. Excluir ExamesRealizados
10. SaveChanges
11. Commit
12. Em caso de erro: Rollback + restaurar Situacao anterior
```

### Caminho do PDF

```
{ContentRootPath}/App_Data/Resultados/{CNPJ}/{yyyyMM}/{ExameId}.pdf
```

Onde:
- `CNPJ` = `_db.Empresa.FirstOrDefault()?.CNPJ` (sem formatação, só dígitos)
- `yyyyMM` = data UTC atual formatada
- `ExameId` = Id do ExamesRealizados

## Endpoints

| Rota | Método | Ação |
|------|--------|------|
| `/ResultadoExames/ImprimirResultado` | GET | Gera PDF, marca DataEntrega/Situacao/TotalImpresso, retorna arquivo |
| `/ResultadoExames/BaixarExame` | POST | Copia para AM, exclui original |
| `/ResultadoExames/ExcluirItem` | POST | Exclui item individual |
