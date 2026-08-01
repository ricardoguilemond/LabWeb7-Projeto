# Plano de Implantação — Mapas de Trabalho (.Net)

## 1. Resumo Executivo

No Delphi, o recurso **Mapas de Trabalho** é composto por quatro frentes principais:

1. **Mapa Eletrônico / Produção** (`FProducao.pas`) — tela para lançamento de resultados de exames **já liberados** e **não baixados**, filtrados por período e agrupados por classe/folha de exame.
2. **Mapa Excel** (`FMapaExcel.pas`) — geração de planilha Excel a partir dos exames do dia, criando registros na tabela `FichasPlanilhas` por lote.
3. **Mapa Agrupado** (`FFichaAgrupada.pas` + `FRelMapaAgrupado.pas`) — geração de lotes na tabela `FichasLotes` e impressão de mapa agrupado por folha/paciente.
4. **Mapa Horizontal / Mapa Interno** (`FFichaHorizontal.pas`, `FFichaInterna.pas`, `FRelMapaHorizontal.pas`, `FRelFichaInterna.pas`) — geração de registros na tabela `FichasInternas` e impressão de mapa horizontal ou cortes/etiquetas.

O projeto .Net já possui:
- Modelos de dados equivalentes (`ExamesRealizados`, `ItensExamesRealizados`, `ClasseExames`, `PlanoExames`, `FichasInternas`, `FichasLotes`, `FichasPlanilhas`, `Pacientes`).
- Um módulo de **Resultado de Exames** (`ResultadoExamesController`) que lista exames pendentes, lança resultados, libera e imprime laudos em PDF com `PdfSharpCore`.
- Infraestrutura de menu dinâmico via `ControleDePerfilMenu` e itens fixos no final do menu.

O plano abaixo propõe a implantação gradual das quatro frentes, reaproveitando o máximo possível do código existente.

---

## 2. Mapeamento do Delphi

### 2.1. Acesso no menu

O Delphi utiliza menu dinâmico carregado da tabela `MenuSistema`. O botão **Mapa Eletrônico** na barra de atalhos do `FPrincipal.pas` abre diretamente `FrmProducao`. Os demais mapas provavelmente estão disponíveis dentro de menus de "Mapas de Trabalho" / "Folhas de Exames" (nomes exatos dependentes da carga de `MenuSistema` no banco de dados).

**Sugestão para o .Net:** criar um grupo de menu fixo ou dinâmico chamado **"Mapas de Trabalho"** com subitens:
- Mapa Eletrônico
- Mapa Excel
- Mapa Agrupado
- Mapa Horizontal
- Mapa Interno / Cortes

### 2.2. Forms e relatórios identificados

| Formulário Delphi | Função | Relatório associado |
|---|---|---|
| `FProducao.pas` | Lançamento de resultados por folha/classe | Impressora não-fiscal 40 colunas (`spdImprimeLista`) |
| `FMapaExcel.pas` | Gera lotes e exporta para Excel | Exportação via COM Excel |
| `FFichaAgrupada.pas` | Seleciona folhas e gera lote | `FRelMapaAgrupado.pas` (QuickReport) |
| `FFichaHorizontal.pas` | Seleciona folhas e gera mapa horizontal | `FRelMapaHorizontal.pas` |
| `FFichaInterna.pas` | Gera mapa interno/cortes | `FRelFichaInterna.pas`, `FEtiquetasHemograma.pas` |

### 2.3. Tabelas envolvidas

- **`ExamesRealizados`** — cabeçalho do exame; filtros por `DataIni`, `Liberacao = 1`, `Baixado <> 1`.
- **`ItensExamesRealizados`** — itens do exame; campos `ContaExame`, `CodigoCabecalhoFolha` (FK para `ClasseExames`), `Resultado`, `Liberado`, `Baixado`.
- **`ClasseExames`** — folhas/classes de exame; campos `RefExame`, `TipoMapa` (`'E'` = eletrônico), `Marcado`, `Planilha`, `MHI`.
- **`PlanoExames`** — configuração dos itens; campo `MapaHorizontal` usado como abreviação no mapa horizontal.
- **`Clientes/Pacientes`** — dados do paciente (`NomeCliente/NomePaciente`, `Nascimento`, `Sexo`).
- **`Medicos`** — médico responsável.
- **`Instituicao`** — instituição de origem.
- **`FichasPlanilhas`** — dados gerados para o mapa Excel.
- **`FichasLotes`** — dados gerados para o mapa agrupado.
- **`FichasInternas`** — dados gerados para o mapa horizontal/interno.

### 2.4. Regras de negócio observadas

- Exames considerados nos mapas devem estar **liberados** (`Liberacao = 1`) e **não baixados** (`Baixado <> 1`).
- Itens cuja conta termina em `0000` (conta principal/folha) são **ignorados** nos mapas; só entram os subitens.
- No mapa eletrônico, apenas classes com `TipoMapa = 'E'` entram no filtro inicial.
- No mapa horizontal, exames de **Hemograma/Eritrograma/Leucograma** são redirecionados para folha "HEMATOLOGIA".
- Lotes são gerados por **data de coleta** (`DataIni`/`DataExame`) e **sequencial numérico** (`MAX(Lote) + 1`).
- Registros antigos de `FichasPlanilhas` e `FichasLotes` com mais de 30 dias são sinalizados (`LiberadoExclusao = 'S'`) e posteriormente excluídos.

---

## 3. Arquitetura no .Net

### 3.1. Modelos já existentes (reaproveitáveis)

Os modelos abaixo já estão no `ModeloDeDados\Models` e representam as mesmas tabelas do Delphi:

- `ExamesRealizados`
- `ItensExamesRealizados`
- `ClasseExames`
- `PlanoExames`
- `FichasPlanilhas`
- `FichasLotes`
- `FichasInternas`
- `Pacientes`
- `Medicos`
- `Instituicao`
- `TabelaExames`

**Observação:** no .Net as chaves são `Id` (auto-incremento), enquanto no Delphi são `Codigo` (via `IncrementaRegistro`). Deve-se utilizar o EF Core para geração de novos IDs.

### 3.2. Controllers existentes relacionados

- `ResultadoExamesController` — já faz lançamento de resultados, liberação e impressão de laudo. Pode servir de base para o **Mapa Eletrônico**, com ajuste do filtro de exames (liberados e não baixados).
- `RequisitarController` — exemplo de geração de PDF com `PdfSharpCore` (`ImprimirCupom`).

### 3.3. Infraestrutura de impressão

- PDF: `PdfSharpCore` (já instalado no `LabWebMvc.MVC.csproj`).
- Excel: **não há biblioteca instalada**. Sugestão: adicionar `EPPlus` ou `ClosedXML`.

---

## 4. Proposta de Menu no .Net

Inserir um novo grupo dinâmico na tabela `ControleDePerfilMenu` (respeitando a regra de ordenação: inserir no final, antes de Sobre/Login/Logout, que são fixos).

**Estrutura sugerida:**

| Coluna | Menu | Controller | Action | Nível |
|---|---|---|---|---|
| (próxima) | Mapas de Trabalho | (null) | (null) | 000 |
| (mesma) | Mapa Eletrônico | MapasTrabalho | MapaEletronico | 001 |
| (mesma) | Mapa Excel | MapasTrabalho | MapaExcel | 002 |
| (mesma) | Mapa Agrupado | MapasTrabalho | MapaAgrupado | 003 |
| (mesma) | Mapa Horizontal | MapasTrabalho | MapaHorizontal | 004 |
| (mesma) | Mapa Interno | MapasTrabalho | MapaInterno | 005 |

Alternativa: criar controllers separados (`MapaEletronicoController`, `MapaExcelController`, etc.) se a lógica for muito distinta.

---

## 5. Plano de Implementação por Fase

### Fase 1 — Mapa Eletrônico (maior reaproveitamento)

**Objetivo:** permitir que o usuário selecione uma data/intervalo e uma classe/folha de exame, liste os itens liberados sem resultado e lance os resultados.

**Entregáveis:**
1. Controller `MapasTrabalhoController` (ou `MapaEletronicoController`).
2. Action `MapaEletronico` [GET] — tela com filtros de data inicial/final e lista de classes (`ClasseExames` com `TipoMapa = 'E'`).
3. Action `ObterItensMapaEletronico` [GET] — retorna JSON com itens de exames liberados/não baixados, filtrados por classe e período, sem resultados.
4. Action `SalvarResultadoMapa` [POST] — similar ao `SalvarResultado` do `ResultadoExamesController`, mas permitindo editar resultados em exames já liberados.
5. Action `ImprimirListaColetas` [GET] — gera TXT/PDF com lista de coletas para a classe selecionada (equivalente ao `spdImprimeLista` do Delphi).
6. View `MapaEletronico.cshtml` — interface com duas etapas (seleção de classe → grid de itens editáveis).

**Pontos de atenção:**
- Reaproveitar `SalvarResultado` existente, extraindo a lógica para um serviço compartilhado se necessário.
- No Delphi, o mapa eletrônico trabalha com exames **já liberados** e não baixados. O `ResultadoExamesController` atual lista exames **não liberados**. Será necessário inverter/adicionar o filtro `Liberacao == 1`.

### Fase 2 — Mapa Excel

**Objetivo:** gerar planilha Excel a partir dos exames de uma data, criando registros em `FichasPlanilhas`.

**Entregáveis:**
1. Action `MapaExcel` [GET] — tela com seleção de data e lista de folhas (`ClasseExames` com `Planilha = 1`).
2. Action `GerarMapaExcel` [POST] — gera registros em `FichasPlanilhas` por lote, seguindo a lógica do `FMapaExcel.Gera_Mapa`.
3. Action `ExportarExcel` [GET] — gera arquivo `.xlsx` a partir dos registros do lote selecionado.
4. Action `ExcluirLote` [POST] — remove registros de `FichasPlanilhas` por data/lote.

**Dependência técnica:** adicionar pacote NuGet `EPPlus` ou `ClosedXML`.

**Pontos de atenção:**
- Calcular lote como `MAX(Lote) + 1` filtrando por `DataExame`.
- Inserir apenas subitens (`SUBSTRING(ContaExame, 8, 4) <> '0000'`).
- Opção de filtro "somente sem resultados" conforme o radio group do Delphi.

### Fase 3 — Mapa Agrupado

**Objetivo:** gerar lotes em `FichasLotes` e imprimir mapa agrupado.

**Entregáveis:**
1. Action `MapaAgrupado` [GET] — tela com data e seleção de folhas.
2. Action `GerarMapaAgrupado` [POST] — popula `FichasLotes` com os exames selecionados.
3. Action `ImprimirMapaAgrupado` [GET] — gera PDF a partir de `FichasLotes` com layout similar ao `FRelMapaAgrupado`.
4. Helper `GeradorPdfMapaAgrupado` em `Areas\Utils`.

### Fase 4 — Mapa Horizontal e Mapa Interno

**Objetivo:** gerar registros em `FichasInternas` e imprimir mapa horizontal ou cortes/etiquetas.

**Entregáveis:**
1. Actions e Views para `MapaHorizontal` e `MapaInterno`.
2. Lógica de geração de `FichasInternas` respeitando:
   - redirecionamento de Hemograma/Eritrograma/Leucograma para HEMATOLOGIA;
   - limite de 14/18 colunas por página;
   - paginação por paciente/exame.
3. Helpers `GeradorPdfMapaHorizontal` e `GeradorPdfMapaInterno`.
4. Geração de etiquetas para hemograma (opcional, pode usar `PdfSharpCore` ou biblioteca de código de barras se necessário).

---

## 6. Impressão e Exportação

### 6.1. PDF

Recomenda-se seguir o padrão já adotado no projeto:
- Criar DTOs (`DadosPdfMapaAgrupado`, `ItemPdfMapaAgrupado`, etc.).
- Criar helpers em `LabWebMvc.MVC\Areas\Utils\`.
- Usar `PdfSharpCore.Drawing` e `PdfSharpCore.Pdf`.
- Retornar `File(pdfBytes, "application/pdf", "...")`.

### 6.2. Excel

- Adicionar pacote `ClosedXML` (mais simples) ou `EPPlus`.
- Criar helper `GeradorExcelMapa`.
- Layout sugerido: cabeçalho com empresa/data/lote; colunas com as folhas selecionadas; linhas com pacientes (controle de apoio).

### 6.3. Lista de Coletas (Mapa Eletrônico)

- Pode ser um PDF simplificado ou TXT, conforme a necessidade.
- O Delphi imprime em impressora 40 colunas; no .Net, PDF é mais adequado.

---

## 7. Considerações Técnicas e Riscos

| Tópico | Consideração |
|---|---|
| Fuso horário | O `ResultadoExamesController` já converte datas para UTC; os novos controllers devem seguir o mesmo padrão (`_geralController.ConverterDataLocalParaRangeUtc`). |
| Concorrência | A geração de lotes em `FichasPlanilhas`/`FichasLotes` deve considerar concorrência. Recomenda-se uso de transações ou controle de concorrência. |
| Performance | As queries do Delphi varrem muitos registros. No .Net, usar `AsNoTracking()`, projeções e paginação quando possível. |
| Permissões | Utilizar `[TypeFilter(typeof(SessionFilter))]` em todas as actions. |
| Menu | Inserir grupo via script SQL em `ControleDePerfilMenu`, respeitando a regra de ordenação (antes de Sobre/LoginLogout). |
| Banco de dados | Não há alteração de schema. As tabelas já existem e os modelos estão mapeados. |

---

## 8. Próximos Passos Imediatos (após validação)

1. Validar com o usuário a nomenclatura do menu e das telas.
2. Definir prioridade de implementação (sugestão: começar pelo Mapa Eletrônico).
3. Escolher biblioteca Excel (`ClosedXML` vs `EPPlus`).
4. Levantar se há necessidade de etiquetas/código de barras para hemograma.
5. Criar script SQL de carga do menu em `ControleDePerfilMenu`.
6. Iniciar implementação da Fase 1.
