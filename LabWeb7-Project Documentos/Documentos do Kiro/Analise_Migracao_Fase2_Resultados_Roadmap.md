# Análise Comparativa LabWeb7 — Fase 2

## Estudo de Lançamento de Resultados, Proposta de Implementação e Roadmap

---

## ETAPA 5 — Estudo Completo: Lançamento de Resultados de Exames

### 5.1 Mapeamento no Delphi

| Artefato | Arquivo | Linhas |
|----------|---------|--------|
| Form principal | FExamesResultados.pas | 3154 |
| Form folhear | FFolhearResultados.pas | 370 |
| Form produção/bancada | FProducao.pas | 1260 |
| Relatório de resultado | FRelatorioResultado.pas | — |
| DataModule | BD_dtmGeral.pas | 1011 |
| Funções globais | FRotinas.pas, FGlobal.pas | — |

### 5.2 Fluxo Operacional Completo (Delphi)

```
1. REQUISIÇÃO (FRequisicaoExames)
   └─ Cria ExamesRealizados (header) + ItensExamesRealizados (itens básicos)
   └─ Liberacao = 0, Situacao = 0

2. LIBERAÇÃO (FExamesRealizados / FLiberarTudo)
   └─ Gera itens detalhados a partir do PlanoExames
   └─ Preenche: UnidadeMedida, Referencia, ValorItem, Etiquetas
   └─ Marca: Liberacao = 1, Situacao = 1, DataFim = NOW
   └─ Imprime etiquetas de código de barras

3. LANÇAMENTO DE RESULTADOS (FExamesResultados)
   └─ Filtra: Liberacao = 1, Baixado = 0 (liberados não baixados)
   └─ Período: DataIni >= X AND DataFim <= Y
   └─ Edita campo "Resultado" diretamente no grid
   └─ Gravar: Post + UpdateBatch com transação
   └─ Laudo adicional: campo Memo (Laudo)
   └─ Laudo fixo: arquivo .DOC no servidor (por ContaExame)
   └─ Laudo colado: HTML importado (WebBrowser)
   └─ Laudo PDF: arquivo importado

4. IMPRESSÃO DE RESULTADO (spdResultadoClick)
   └─ Valida: todos os itens têm Resultado preenchido
   └─ Atualiza: DataEntrega = hoje, Situacao = 3
   └─ Gera relatório QuickReport (preview modal)
   └─ Incrementa TotalImpresso

5. BAIXA / ARQUIVO-MORTO (spdBaixarClick → Baixar)
   └─ Valida: todos os resultados preenchidos
   └─ Marca: Situacao = 11 (sendo baixado)
   └─ Copia itens para ItensExamesRealizadosAM
   └─ Copia header para ExamesRealizadosAM
   └─ Exclui de ExamesRealizados e ItensExamesRealizados
   └─ Remove duplicatas no AM
   └─ Desfaz_Baixa em caso de erro
```

### 5.3 Operações do Form FExamesResultados

| Operação | Descrição | Tabelas |
|----------|-----------|---------|
| AbreExamesRealizados | Carrega exames liberados não baixados no período | ExamesRealizados |
| AbreItensExamesRealizados | Carrega itens do exame selecionado | ItensExamesRealizados |
| GravarResultado | Persiste resultado editado (Post + UpdateBatch + transação) | ItensExamesRealizados |
| DeletaItem | Exclui item individual do exame | ItensExamesRealizados |
| Baixar | Arquiva exame completo no AM | ExamesRealizadosAM, ItensExamesRealizadosAM |
| Desfaz_Baixa | Restaura Situacao anterior em caso de erro | ExamesRealizados |
| Gera_PDF | Exporta resultado em PDF | — |
| spdResultadoClick | Imprime laudo (QuickReport) | ExamesRealizados (UPDATE DataEntrega) |
| spdEmailClick | Envia resultado por email | — |
| AbrePlanoExames | Carrega laudo fixo de referência | PlanoExames |
| Resposta_Rapida | Textos prontos (ComboBox) | TextosProntos |

### 5.4 Regras de Negócio Identificadas

1. **Filtro de exames**: `Liberacao = 1 AND (Baixado = 0 OR Baixado IS NULL)`
2. **Período**: filtro por `DataIni >= X AND DataFim <= Y`
3. **Edição no grid**: apenas coluna Resultado é editável (colunas 0,1 ReadOnly)
4. **Gravação**: BeginTrans → Post → UpdateBatch(arCurrent) → CommitTrans
5. **Navegação com ENTER**: grava resultado atual e avança para próximo item (pula cabeçalhos de folha onde ContaExame termina em "0000")
6. **Validação antes de imprimir**: todos os itens devem ter Resultado preenchido
7. **Validação antes de baixar**: idem + campo Situacao não pode ser 11 (outro terminal baixando)
8. **Impressão marca DataEntrega e Situacao = 3**
9. **Baixa marca Situacao = 11 temporariamente, depois move para AM**
10. **Textos auxiliares**: ComboBox com textos prontos (DblClick insere no campo Resultado)
11. **Laudo fixo**: arquivo .DOC em pasta compartilhada (por ContaExame)
12. **Laudo colado**: HTML em campo ExameColado do ExamesRealizados (WebBrowser)

### 5.5 Campos Editáveis no Grid de Resultados

| Campo | Tipo | Editável | Observação |
|-------|------|----------|-----------|
| RefItem | String | ❌ ReadOnly | Identificação do item |
| Descricao | String | ❌ ReadOnly | Nome do exame |
| Resultado | String(30) | ✅ | Campo principal de digitação |
| UnidadeMedida | String(20) | ✅ | Unidade (mg/dL, etc.) |
| Referencia | String(60) | ✅ | Valor de referência |
| Laudo | Memo | ✅ | Laudo adicional por item |

### 5.6 Dependências para Implementação no .NET

| Dependência | Status no .NET | Ação necessária |
|-------------|---------------|-----------------|
| ExamesRealizados (tabela) | ✅ Existe | — |
| ItensExamesRealizados (tabela) | ✅ Existe | — |
| ExamesRealizadosAM (tabela) | ✅ Existe no banco | Criar model/mapeamento |
| ItensExamesRealizadosAM (tabela) | ✅ Existe no banco | Criar model/mapeamento |
| PlanoExames (laudo fixo) | ✅ Existe | Usar campo Laudo |
| TextosProntos (textos auxiliares) | ❓ Verificar no banco | — |
| Liberação (fluxo) | ❌ Não existe no .NET | Pré-requisito |

---

## ETAPA 6 — Proposta de Implementação para o .NET

### 6.1 Requirements

#### Requisitos Funcionais

| ID | Requisito |
|----|-----------|
| RF-001 | Listar exames liberados (Liberacao=1) e não baixados (Baixado=0) por período |
| RF-002 | Exibir itens do exame em grid editável (Resultado, UnidadeMedida, Referencia) |
| RF-003 | Salvar resultado com transação (um item por vez, via AJAX) |
| RF-004 | Navegação por ENTER: salva e avança para próximo item (pula cabeçalhos) |
| RF-005 | Textos auxiliares: ComboBox com textos prontos para inserção rápida |
| RF-006 | Laudo adicional: campo Memo editável por item |
| RF-007 | Validar preenchimento completo antes de permitir impressão |
| RF-008 | Impressão de laudo (PDF) com dados do paciente, médico, instituição |
| RF-009 | Filtros: período, código exame, nome paciente, instituição, sequencial |
| RF-010 | Múltiplas ordenações do grid (por código, paciente, instituição, etc.) |
| RF-011 | Laudo fixo de referência (readonly, vindo do PlanoExames) |
| RF-012 | Marcar DataEntrega e Situacao ao imprimir |
| RF-013 | Baixa para Arquivo-Morto (mover para tabelas AM) |
| RF-014 | Exclusão de item individual do exame |

#### Requisitos Não Funcionais

| ID | Requisito |
|----|-----------|
| RNF-001 | Performance: grid carregável em < 2s para até 100 itens |
| RNF-002 | Salvamento: endpoint AJAX individual por item (não POST do form completo) |
| RNF-003 | Concorrência: flag Situacao=11 impede baixa simultânea |
| RNF-004 | Build: 0 erros, 0 avisos |
| RNF-005 | Encoding: UTF-8 com BOM em .cs e .cshtml |
| RNF-006 | Sem pacotes NuGet adicionais |

### 6.2 Design

#### Layout da Tela

```
┌─────────────────────────────────────────────────────────────┐
│ [Filtros: Período | Código | Paciente | Instituição]  [Pesquisar] │
├─────────────────────────────────────────────────────────────┤
│ Grid ExamesRealizados (header — não editável)               │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Cód | Paciente | Inst | Tabela | Seq | DataIni | DataFim │ │
│ │ 62  | ASDRUBAL | BARROS | BARROS | 8  | 06/06  | 06/06   │ │
│ └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│ Painel Info: Paciente, Nasc, Idade, CPF, Médico, CRM, Inst  │
├─────────────────────────────────────────────────────────────┤
│ Grid ItensExamesRealizados (editável)                       │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Folha | Descrição | Resultado | Unidade | Referência     │ │
│ │ BIOQUIMICA | Glicose | [___95__] | mg/dL  | 70 a 99      │ │
│ │ BIOQUIMICA | Creatinina | [__1.2_] | mg/dL | 0.7 a 1.3   │ │
│ └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│ Painel Laudo: [Textos Prontos ▼] [Laudo Fixo readonly]      │
│ [Memo Laudo Adicional — editável]                           │
├─────────────────────────────────────────────────────────────┤
│ [Salvar F6] [Imprimir] [Baixar] [Excluir F4] [PDF] [Sair]  │
└─────────────────────────────────────────────────────────────┘
```

#### Componentes

| Componente | Tipo | Função |
|-----------|------|--------|
| Grid Header | DataTables readonly | Navega exames liberados |
| Grid Itens | DataTables editável ou tabela HTML com inputs | Digita resultados |
| Painel Info | HTML estático | Exibe dados do paciente/médico |
| ComboBox Textos | select + JS | Insere texto pronto no campo |
| Memo Laudo | textarea | Laudo adicional editável |
| Laudo Fixo | div readonly | Conteúdo do PlanoExames.Laudo |

#### Endpoints Necessários

| Rota | Método | Função |
|------|--------|--------|
| /ResultadoExames | GET | Tela principal (View) |
| /ResultadoExames/ObterExamesLiberados | GET | Grid header |
| /ResultadoExames/ObterItensExame | GET | Grid itens |
| /ResultadoExames/SalvarResultado | POST | Salva resultado de 1 item |
| /ResultadoExames/SalvarLaudo | POST | Salva laudo adicional |
| /ResultadoExames/ImprimirResultado | GET | Gera PDF do laudo |
| /ResultadoExames/BaixarExame | POST | Move para AM |
| /ResultadoExames/ExcluirItem | POST | Exclui item individual |
| /ResultadoExames/ObterTextosProntos | GET | Lista textos auxiliares |

### 6.3 Task List

#### Banco de Dados

- [ ] Verificar se tabelas ExamesRealizadosAM e ItensExamesRealizadosAM existem no banco
- [ ] Verificar se tabela TextosProntos existe
- [ ] Criar índice em ItensExamesRealizados (ExameRealizadoId, OrdemItem) se não existir

#### Backend

- [ ] Criar controller `ResultadoExamesController`
- [ ] Criar ViewModel `vmResultadoExames`
- [ ] Implementar endpoint `ObterExamesLiberados` (filtros + paginação)
- [ ] Implementar endpoint `ObterItensExame` (por ExameRealizadoId)
- [ ] Implementar endpoint `SalvarResultado` (item individual, transação)
- [ ] Implementar endpoint `SalvarLaudo` (campo Laudo do item)
- [ ] Implementar endpoint `ImprimirResultado` (gerar PDF)
- [ ] Implementar endpoint `BaixarExame` (mover para AM com validação)
- [ ] Implementar endpoint `ExcluirItem` (com validação de FK)
- [ ] Implementar endpoint `ObterTextosProntos`
- [ ] Adicionar models/mapeamentos para ExamesRealizadosAM e ItensExamesRealizadosAM (se necessário)

#### Frontend

- [ ] Criar View `ResultadoExames/Index.cshtml`
- [ ] Criar partial menu `_PartialMenuResultadoExames.cshtml`
- [ ] Implementar grid header (DataTables readonly)
- [ ] Implementar grid itens editável (inputs inline)
- [ ] Implementar salvamento AJAX por item (ENTER avança)
- [ ] Implementar ComboBox de textos prontos
- [ ] Implementar Memo de laudo adicional
- [ ] Implementar painel de informações do paciente
- [ ] Implementar filtros de período e buscas
- [ ] Implementar ação de impressão (PDF)
- [ ] Implementar ação de baixa com confirmação

#### Testes

- [ ] Teste manual: digitar resultado, salvar, navegar
- [ ] Teste manual: imprimir laudo
- [ ] Teste manual: baixar para AM
- [ ] Teste manual: excluir item
- [ ] Teste de concorrência: dois terminais no mesmo exame

---

## ETAPA 7 — Roadmap de Migração

### Curto Prazo (1-2 sprints)

| # | Funcionalidade | Justificativa |
|---|---------------|---------------|
| 1 | **Liberação de Exames** | Pré-requisito para lançamento de resultados |
| 2 | **Lançamento de Resultados** | Core do sistema — sem isso não opera |
| 3 | **Impressão de Laudo (PDF)** | Entregável obrigatório para o paciente |

### Médio Prazo (3-4 sprints)

| # | Funcionalidade | Justificativa |
|---|---------------|---------------|
| 4 | Baixa para Arquivo-Morto | Gestão de histórico e performance |
| 5 | Produção/Bancada | Otimiza digitação em volume |
| 6 | Etiquetas (código de barras) | Rastreabilidade laboratorial |
| 7 | Relatórios operacionais (pendentes, realizados) | Controle diário |
| 8 | Importação de Resultados (interfaceamento) | Integração com equipamentos |

### Longo Prazo (5+ sprints)

| # | Funcionalidade | Justificativa |
|---|---------------|---------------|
| 9 | Faturamento completo | Módulo financeiro |
| 10 | Exportação de Resultados | Integração com labs de apoio |
| 11 | Relatórios gerenciais (20+) | Gestão administrativa |
| 12 | Fichas e Mapas de Trabalho | Operação de bancada avançada |
| 13 | Orçamento | Atendimento ao cliente |

### Justificativa da Ordem

1. **Liberação** vem primeiro porque é o passo que transforma uma requisição em algo "analisável". Sem liberação, não há itens detalhados para receber resultados.
2. **Lançamento de Resultados** é a razão de existência do sistema. Um laboratório que não lança resultados não opera.
3. **Impressão de Laudo** é o produto final entregue ao paciente. Sem laudo impresso, o resultado não tem valor.
4. As demais funcionalidades são otimizações e complementos que melhoram a operação mas não a impedem.

---

## Conclusão

O sistema .NET já possui uma base sólida de cadastros e requisição. O próximo passo crítico é implementar a cadeia **Liberação → Lançamento de Resultados → Impressão de Laudo**, que representa o fluxo core do laboratório clínico.

A implementação pode ser feita incrementalmente, seguindo o padrão já estabelecido no projeto (controller + endpoints AJAX + view com DataTables/inputs inline), sem necessidade de novos frameworks ou bibliotecas.

**Pré-requisito obrigatório antes de iniciar:** Verificar e confirmar que o processo de Liberação existe ou será implementado em paralelo, pois o Lançamento de Resultados depende de exames com `Liberacao = 1`.

---

## ANEXO — Estrutura de Armazenamento de Laudos e PDFs

### Informação do Projeto .NET

- Pasta atual de Laudos Fixos (modelos .DOC por ContaExame): `\LabWebMvc.MVC\Laudos\`
- CNPJ do cliente logado acessível via: `Utils.LoginCNPJEmpresaLogado()` ou `Session.GetString("SessionCNPJEmpresa")`

### Estrutura Multi-Cliente por CNPJ

#### 1. Laudos Fixos (Templates/Modelos por ContaExame)

Modelos de laudo que servem como referência fixa para cada exame.
Separados por CNPJ para permitir personalização por cliente.

```
Local (desenvolvimento):
  \LabWebMvc.MVC\Laudos\{CNPJ}\{ContaExame}.DOC

Servidor/Nuvem:
  /app/data/laudos/{CNPJ}/{ContaExame}.DOC
```

Exemplo:
```
\LabWebMvc.MVC\Laudos\12345678000190\11030010001.DOC
\LabWebMvc.MVC\Laudos\98765432000111\11030010001.DOC
```

#### 2. PDFs de Resultados (Gerados sob demanda)

PDFs gerados quando o resultado é impresso. Armazenados fora do web root
para evitar exposição pública. Separados por CNPJ + mês.

```
Local (desenvolvimento):
  \LabWebMvc.MVC\App_Data\Resultados\{CNPJ}\{AnoMes}\{ExameId}.pdf

Servidor/Nuvem:
  /app/data/resultados/{CNPJ}/{AnoMes}/{ExameId}.pdf
```

Exemplo:
```
\App_Data\Resultados\12345678000190\202606\62.pdf
\App_Data\Resultados\12345678000190\202606\43.pdf
```

#### 3. PDFs Temporários (Preview antes de imprimir)

Para visualização prévia. Limpeza periódica automática.

```
Local e Servidor:
  {Path.GetTempPath()}\LabWeb7\{CNPJ}\{ExameId}_preview.pdf
```

Exemplo:
```
Windows: C:\Users\User\AppData\Local\Temp\LabWeb7\12345678000190\62_preview.pdf
Linux:   /tmp/LabWeb7/12345678000190/62_preview.pdf
```

### Tabela Resumo

| Tipo de Arquivo | Caminho | Persistência | Separação |
|----------------|---------|-------------|-----------|
| Laudos fixos (modelos .DOC) | `{ContentRoot}/Laudos/{CNPJ}/` | Permanente | Por CNPJ |
| PDFs de resultados | `{ContentRoot}/App_Data/Resultados/{CNPJ}/{AnoMes}/` | Permanente | Por CNPJ + mês |
| PDFs temporários (preview) | `{TempPath}/LabWeb7/{CNPJ}/` | Temporária | Por CNPJ |

### Configuração em appsettings.json

```json
"Armazenamento": {
  "LaudosFixos": "Laudos",
  "ResultadosPDF": "App_Data/Resultados",
  "Temporarios": ""
}
```

Quando `Temporarios` está vazio, usa `Path.GetTempPath()`.
Em nuvem, pode apontar para volume persistente ou blob storage (Azure/AWS S3).

### Código de Resolução de Caminho

```csharp
string cnpj = Utils.LoginCNPJEmpresaLogado() ?? "00000000000000";

// Laudos fixos
string caminhoLaudo = Path.Combine(
    _environment.ContentRootPath, "Laudos", cnpj, contaExame + ".DOC");

// PDFs de resultados
string caminhoResultados = Path.Combine(
    _environment.ContentRootPath, "App_Data", "Resultados", cnpj,
    DateTime.UtcNow.ToString("yyyyMM"));
Directory.CreateDirectory(caminhoResultados);
string arquivoPdf = Path.Combine(caminhoResultados, exameId + ".pdf");

// Temporários
string caminhoTemp = Path.Combine(
    Path.GetTempPath(), "LabWeb7", cnpj);
Directory.CreateDirectory(caminhoTemp);
```

### Migração da Pasta Atual

A pasta `\LabWebMvc.MVC\Laudos\` atual (sem separação por CNPJ) deve ser
tratada como fallback. O código deve buscar primeiro no caminho com CNPJ
e, se não encontrar, buscar na pasta raiz (compatibilidade):

```csharp
string laudoPath = Path.Combine(basePath, cnpj, contaExame + ".DOC");
if (!File.Exists(laudoPath))
    laudoPath = Path.Combine(basePath, contaExame + ".DOC"); // fallback
```
