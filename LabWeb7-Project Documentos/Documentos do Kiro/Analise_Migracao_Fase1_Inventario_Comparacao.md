# Análise Comparativa LabWeb7 — Delphi x .NET

## Fase 1: Inventário, Comparação, Funcionalidades Não Migradas e Matriz de Rastreabilidade

---

## ETAPA 1 — Inventário de Telas

### 1.1 Projeto Delphi (F:\Meus Sistemas\Lab-Web7-Pascal)

#### Cadastros

| Tela | Arquivo | Finalidade | Tabelas |
|------|---------|-----------|---------|
| Clientes/Pacientes | FClientes.pas | CRUD de pacientes | Clientes |
| Médicos | FMedicos.pas | CRUD de médicos | Medicos |
| Instituições | FInstituicoes.pas | CRUD de instituições/convênios | Instituicao |
| Plano de Exames | FPlanoExames.pas | Cadastro de tabelas de preços | PlanoExames, TabelaExames |
| Itens Plano Exames | FPlanoItensExames.pas | Itens do plano de exames | PlanoExames |
| Itens Plano (v2) | FPlanoItensExames2.pas | Variante de itens | PlanoExames |
| Nomeação Plano | FPlanoNomeacao.pas | Nomenclatura de planos | PlanoExames |
| Folhas de Exames | FFolhasExames.pas | Cadastro de folhas/classes | ClasseExames |
| Bancos e Agências | FBancosAgencias.pas | Cadastro financeiro | BancosAgencias |
| Operadoras Cartão | FOperadorasCartao.pas | Cadastro de operadoras | OperadorasCartao |
| Servidores | FServidores.pas | Cadastro de servidores | Servidores |

#### Exames — Requisição e Entrada

| Tela | Arquivo | Finalidade | Tabelas |
|------|---------|-----------|---------|
| Requisição de Exames | FRequisicaoExames.pas | Requisição principal | ExamesRealizados, ItensExamesRealizados, RequisicaoOriginal, Clientes, Medicos |
| Pop-up Requisição | FRequisicaoPopUp.pas | Auxiliar da requisição | — |
| Entrada de Exames | FExamesEntrada.pas | Recepção de exames | ExamesRealizados |

#### Exames — Liberação e Resultados

| Tela | Arquivo | Finalidade | Tabelas |
|------|---------|-----------|---------|
| Exames Realizados | FExamesRealizados.pas | Exames não liberados, liberação | ExamesRealizados, ItensExamesRealizados, PlanoExames, RequisicaoOriginal |
| Liberar Tudo | FLiberarTudo.pas | Liberação em lote | ExamesRealizados, ItensExamesRealizados, PlanoExames |
| Resultados de Exames | FExamesResultados.pas | Lançamento de resultados | ExamesRealizados, ItensExamesRealizados, ExamesRealizadosAM, ItensExamesRealizadosAM |
| Produção/Bancada | FProducao.pas | Digitação de resultados em bancada | ItensExamesRealizados, ClasseExames |
| Resultados Prontos | FResultadosProntos.pas | Resultados prontos para entrega | ExamesRealizados, ItensExamesRealizados |
| Exames AM (Arquivo-Morto) | FExamesAM.pas | Consulta ao arquivo-morto | ExamesRealizadosAM, ItensExamesRealizadosAM |

#### Exames — Consultas (Folhear)

| Tela | Arquivo | Finalidade | Tabelas |
|------|---------|-----------|---------|
| Folhear Resultados | FFolhearResultados.pas | Busca/navega resultados | ExamesRealizados |
| Folhear Resultados AM | FFolhearResultadosAM.pas | Busca no arquivo-morto | ExamesRealizadosAM |
| Folhear Exames Pendentes | FFolhearExamesPendentes.pas | Consulta exames pendentes | ExamesPendentes |
| Folhear Exames Realizados | FFolhearExamesRealizados.pas | Consulta realizados | ExamesRealizados |
| Folhear Exames Impressos | FFolhearExamesImpressos.pas | Consulta impressos | ExamesImpressos |
| Folhear Plano Exames | FFolhearPlanoExames.pas | Consulta plano | PlanoExames |
| Folhear Clientes | FFolhearClientes.pas | Busca pacientes | Clientes |

#### Importação/Exportação e Interfaceamento

| Tela | Arquivo | Finalidade | Tabelas |
|------|---------|-----------|---------|
| Importação Exames | FImportacaoExames.pas | Importar exames externos | ExamesRealizados, ItensExamesRealizados |
| Exportação Exames | FExportacaoExames.pas | Exportar exames | ExamesExportados |
| Importação Plano | FImportacaoPlanoExames.pas | Importar plano de exames | PlanoExames |
| Exportação Plano | FExportacaoPlanoExames.pas | Exportar plano | PlanoExames |
| Importar Resultados | FImportarResultados.pas | Importar resultados de equipamentos | ItensExamesRealizados |
| Exportar Resultados | FExportarResultados.pas | Exportar para lab de apoio | ExamesExportados |
| Exportar XML | FExportaXML.pas | Exportação XML | — |
| Lista Exportados | FListaExamesExportados.pas | Consulta exportações | ExamesExportados |
| Interfaceamentos | FInterfaceamentos.pas | Config. de interfaces | — |
| Protocolo Apoio | FProtocoloApoio.pas | Protocolo lab de apoio | — |

#### Fichas e Mapas de Trabalho

| Tela | Arquivo | Finalidade |
|------|---------|-----------|
| Ficha Agrupada | FFichaAgrupada.pas | Ficha de trabalho agrupada |
| Ficha Bioquímica | FFichaBioquimica.pas | Ficha específica bioquímica |
| Ficha Horizontal | FFichaHorizontal.pas | Layout horizontal |
| Ficha Interna | FFichaInterna.pas | Ficha interna do lab |
| Ficha 40 Colunas | FFicha40Colunas.pas | Layout 40 colunas |
| Mapa Excel | FMapaExcel.pas | Exportação para Excel |

#### Etiquetas

| Tela | Arquivo | Finalidade |
|------|---------|-----------|
| Etiquetas Exames | FEtiquetasExames.pas | Código de barras para exames |
| Etiquetas Frascos | FEtiquetasFrascos.pas | Etiquetas para frascos |
| Etiquetas Hemograma | FEtiquetasHemograma.pas | Etiquetas específicas |

#### Faturamento

| Tela | Arquivo | Finalidade | Tabelas |
|------|---------|-----------|---------|
| Faturamento | FFaturamento.pas | Relatório de faturamento | ExamesRealizados, ExamesRealizadosAM, ItensExamesRealizados, PlanoExames |
| Faturas | FFaturas.pas | Gestão de faturas | Faturas |
| Todas Faturas | FTodasFaturas.pas | Visualização completa | Faturas |
| Enviar Faturamento | FEnviarFaturamento.pas | Envio de faturas | — |
| Manutenção Faturamento | FManutencaoFaturamento.pas | Manutenção | Faturas |
| Controle Custos | FControleCustos.pas | Controle de custos | — |
| Orçamento/Cupom | FOrcamentoCupom.pas | Emissão de orçamento | PlanoExames |

#### Relatórios (20+)

| Tela | Arquivo | Finalidade |
|------|---------|-----------|
| Rel. Clientes | FRelatorioClientes.pas | Listagem de pacientes |
| Rel. Médicos | FRelatorioMedicos.pas | Listagem de médicos |
| Rel. Resultados | FRelatorioResultado.pas | Impressão de laudo |
| Rel. Resultados AM | FRelatorioResultadoAM.pas | Laudo do arquivo-morto |
| Rel. Exames Pendentes | FRelatorioExamesPendentes.pas | Listagem pendentes |
| Rel. Exames Impressos | FRelatorioExamesImpressos.pas | Listagem impressos |
| Rel. Exames Publicados | FRelatorioExamesPublicados.pas | Listagem publicados |
| Rel. Exames Realizados | FRelExamesRealizados.pas | Relatório geral |
| Rel. Ficha Paciente | FRelatorioFichaExamesPaciente.pas | Ficha do paciente |
| Rel. Protocolo Apoio | FRelatorioProtocoloApoio.pas | Protocolo lab apoio |
| Rel. Preços Exames | FRelPrecosExames.pas | Tabela de preços |
| Rel. CH Exames | FRelCHExames.pas | Coeficiente honorário |
| Rel. Controle Custos | FRelControleCustos.pas | Custeio |
| Rel. Ficha Interna | FRelFichaInterna.pas | Ficha interna |
| Rel. Mapa Agrupado | FRelMapaAgrupado.pas | Mapa de trabalho |
| Rel. Mapa Horizontal | FRelMapaHorizontal.pas | Mapa horizontal |
| Rel. Orçamento | FRelOrcamento.pas | Orçamento impresso |
| Rel. Fatura | FRelFatura.pas | Fatura impressa |
| Rel. Faturas Emitidas | FRelFaturasEmitidas.pas | Listagem de faturas |
| Quadro Rel. Médicos | FQuadroRelatoriosMedicos.pas | Painel relatórios |

#### Utilitários e Configuração

| Tela | Arquivo | Finalidade |
|------|---------|-----------|
| Principal (MDI) | FPrincipal.pas | Tela principal |
| Configuração | FConfiguracao.pas | Configurações gerais |
| Compactar Tabelas | FCompactaTabelas.pas | Manutenção de banco |
| Cópias Segurança | FCopias.pas | Backup |
| Rotinas | FRotinas.pas | Funções utilitárias |

---

### 1.2 Projeto .NET (F:\Projetos dotNet\Web-Project\LabWeb7-Projeto)

| Controller | Finalidade | Views |
|-----------|-----------|-------|
| PacientesController | CRUD de pacientes + detail de exames | Pacientes/ |
| MedicosController | CRUD de médicos | Medicos/ |
| InstituicoesController | CRUD de instituições | Instituicoes/ |
| PostosController | CRUD de postos de coleta | Postos/ |
| ClasseExamesController | CRUD de classes/folhas de exames | ClasseExames/ |
| PlanoExamesController | CRUD de plano de exames | PlanoExames/ |
| PlanoExamesItensController | CRUD de itens do plano | PlanoExamesItens/ |
| RequisitarController | Requisição de exames (completo) | Requisitar/ |
| ConsultarExamesController | Consulta de exames realizados | ConsultarExames/ |
| HomeController | Dashboard | Home/ |
| GraficosController | Gráficos e estatísticas | Graficos/ |
| ConfiguracoesController | Configurações do sistema | Configuracoes/ |
| SenhasController | Gerenciamento de usuários | Senhas/ |
| ImplantacaoController | Implantação de dados iniciais | Implantacao/ |
| GeralController | Funções utilitárias (data/hora, etc.) | — (auxiliar) |
| ConnectionController | Gerenciamento de conexão | — (auxiliar) |
| MensagemController | Sistema de mensagens | Mensagem/ |
| MenuController | Menu dinâmico | — (auxiliar) |
| ReleaseController | Controle de versão | — (auxiliar) |

---

## ETAPA 2 — Comparação das Telas Equivalentes

### Pacientes/Clientes

| Aspecto | Delphi (FClientes) | .NET (PacientesController) |
|---------|-------------------|---------------------------|
| CRUD completo | ✅ | ✅ |
| Busca por nome/CPF | ✅ | ✅ (via DataTables) |
| Detail de exames | ❌ (usa form separado) | ✅ (detail inline AJAX) |
| Impressão ficha | ✅ | ❌ |
| Cartão saúde | ✅ | ❌ |

### Médicos

| Aspecto | Delphi | .NET |
|---------|--------|------|
| CRUD completo | ✅ | ✅ |
| Relatório | ✅ | ❌ |

### Requisição de Exames

| Aspecto | Delphi (FRequisicaoExames) | .NET (RequisitarController) |
|---------|---------------------------|---------------------------|
| Cadastro de paciente inline | ✅ | ✅ |
| Cadastro de médico inline | ✅ | ✅ |
| Seleção de instituição | ✅ (via grid/busca) | ✅ (via modal) |
| Seleção de tabela de preços | ✅ | ✅ (via modal) |
| Grid de itens de exame | ✅ (StringGrid) | ✅ (DataTables) |
| Cupom não-fiscal | ✅ (40 colunas direto) | ✅ (via ServicoImpressaoCupom) |
| Edição de requisição | ✅ | ✅ |
| Exclusão de requisição | ✅ | ✅ |
| Grid requisições do dia | ❌ | ✅ (GetLancamentosHoje) |
| Threads para performance | ✅ (TThread anônima) | ❌ (async/await) |
| Validação de ValorItem | ❌ | ✅ (bloqueia item sem valor) |

### Consultar Exames

| Aspecto | Delphi (FFolhearExamesRealizados) | .NET (ConsultarExamesController) |
|---------|----------------------------------|----------------------------------|
| Filtros por período | ✅ | ✅ |
| Filtros por paciente | ✅ | ✅ |
| Filtros por instituição | ✅ | ✅ |
| Detail inline de itens | ❌ (form separado) | ✅ (AJAX inline) |
| Exclusão de item | ❌ | ✅ |

---

## ETAPA 3 — Funcionalidades Ainda Não Migradas

| # | Funcionalidade | Delphi | Complexidade | Prioridade |
|---|---------------|--------|-------------|-----------|
| 1 | **Lançamento de Resultados** | FExamesResultados.pas | **Alta** | **Crítica** |
| 2 | **Liberação de Exames** | FExamesRealizados.pas, FLiberarTudo.pas | **Alta** | **Crítica** |
| 3 | **Produção/Bancada** | FProducao.pas | **Média** | Alta |
| 4 | **Arquivo-Morto (AM)** | FExamesAM.pas | **Média** | Média |
| 5 | **Faturamento** | FFaturamento.pas, FFaturas.pas | **Alta** | Alta |
| 6 | **Importação de Resultados** | FImportarResultados.pas | **Média** | Alta |
| 7 | **Exportação de Resultados** | FExportarResultados.pas | **Média** | Média |
| 8 | **Interfaceamento** | FInterfaceamentos.pas | **Alta** | Média |
| 9 | **Etiquetas** | FEtiquetas*.pas | **Baixa** | Média |
| 10 | **Fichas de Trabalho** | FFicha*.pas | **Média** | Baixa |
| 11 | **Mapas de Trabalho** | FMapa*.pas | **Média** | Baixa |
| 12 | **Relatórios (20+)** | FRelatorio*.pas, FRel*.pas | **Alta** | Média |
| 13 | **Controle de Custos** | FControleCustos.pas | **Baixa** | Baixa |
| 14 | **Orçamento** | FOrcamentoCupom.pas | **Baixa** | Baixa |
| 15 | **Protocolo Apoio** | FProtocoloApoio.pas | **Média** | Média |
| 16 | **Resultados Prontos** | FResultadosProntos.pas | **Baixa** | Média |

---

## ETAPA 4 — Matriz de Rastreabilidade Delphi → .NET

| Funcionalidade | Delphi Form | Controller .NET | Status |
|---------------|------------|----------------|--------|
| Cadastro Pacientes | FClientes | PacientesController | ✅ Migrado |
| Cadastro Médicos | FMedicos | MedicosController | ✅ Migrado |
| Cadastro Instituições | FInstituicoes | InstituicoesController | ✅ Migrado |
| Cadastro Postos | — (dentro de Instituicoes) | PostosController | ✅ Migrado (separado) |
| Folhas de Exames | FFolhasExames | ClasseExamesController | ✅ Migrado |
| Plano de Exames | FPlanoExames | PlanoExamesController | ✅ Migrado |
| Itens Plano | FPlanoItensExames | PlanoExamesItensController | ✅ Migrado |
| Requisição de Exames | FRequisicaoExames | RequisitarController | ✅ Migrado |
| Consultar Exames | FFolhearExamesRealizados | ConsultarExamesController | ✅ Migrado |
| Detail de Exames/Paciente | — | PacientesController (detail) | ✅ Nova implementação |
| Dashboard | — | HomeController, GraficosController | ✅ Nova implementação |
| Configurações | FConfiguracao | ConfiguracoesController | ⚠️ Parcial |
| Senhas/Usuários | FSenhas | SenhasController | ✅ Migrado |
| **Lançamento Resultados** | FExamesResultados | — | ❌ Não Migrado |
| **Liberação de Exames** | FExamesRealizados | — | ❌ Não Migrado |
| **Liberar Tudo** | FLiberarTudo | — | ❌ Não Migrado |
| **Produção/Bancada** | FProducao | — | ❌ Não Migrado |
| **Arquivo-Morto** | FExamesAM | — | ❌ Não Migrado |
| **Faturamento** | FFaturamento | — | ❌ Não Migrado |
| **Faturas** | FFaturas | — | ❌ Não Migrado |
| **Importar Resultados** | FImportarResultados | — | ❌ Não Migrado |
| **Exportar Resultados** | FExportarResultados | — | ❌ Não Migrado |
| **Interfaceamentos** | FInterfaceamentos | — | ❌ Não Migrado |
| **Etiquetas** | FEtiquetas*.pas | — | ❌ Não Migrado |
| **Fichas de Trabalho** | FFicha*.pas | — | ❌ Não Migrado |
| **Mapas de Trabalho** | FMapa*.pas | — | ❌ Não Migrado |
| **Relatórios (20+)** | FRelatorio*.pas | — | ❌ Não Migrado |
| **Controle Custos** | FControleCustos | — | ❌ Não Migrado |
| **Orçamento** | FOrcamentoCupom | — | ❌ Não Migrado |
| **Protocolo Apoio** | FProtocoloApoio | — | ❌ Não Migrado |
| **Resultados Prontos** | FResultadosProntos | — | ❌ Não Migrado |
| **Folhear Resultados** | FFolhearResultados | — | ❌ Não Migrado |
| **Exportação Plano** | FExportacaoPlanoExames | — | ❌ Não Migrado |
| **Importação Plano** | FImportacaoPlanoExames | — | ❌ Não Migrado |
| **Bancos/Agências** | FBancosAgencias | — | ❌ Não Migrado |
| **Operadoras Cartão** | FOperadorasCartao | — | ❌ Não Migrado |

---

### Percentual Estimado de Migração

| Status | Qtd | % |
|--------|-----|---|
| ✅ Migrado Integralmente | 11 | ~30% |
| ⚠️ Migrado Parcialmente | 1 | ~3% |
| ❌ Não Migrado | 24 | ~67% |

**Resumo: ~30% migrado, ~3% parcial, ~67% não migrado**

### Principais Lacunas

1. **Lançamento de Resultados** — funcionalidade core do sistema, inexistente no .NET
2. **Liberação de Exames** — pré-requisito para lançamento de resultados
3. **Faturamento** — módulo financeiro completo ausente
4. **Relatórios** — 20+ relatórios sem equivalente no .NET
5. **Interfaceamento** — integração com equipamentos laboratoriais

### Áreas de Maior Risco Operacional

1. **Lançamento de Resultados** — sem isso, o sistema .NET não pode operar como sistema de laboratório completo
2. **Liberação** — bloqueia o fluxo de produção
3. **Arquivo-Morto** — gestão de histórico e espaço

---

## Conclusão da Fase 1

O projeto .NET cobriu com sucesso os **cadastros base** e a **requisição de exames**, acrescentando melhorias como detail inline, regra inteligente de exibição, grid de requisições do dia e ordenação avançada.

Porém, o **ciclo completo do exame** (Requisição → Liberação → Lançamento de Resultados → Impressão de Laudo → Baixa/Arquivo-Morto) está incompleto — falta toda a cadeia pós-requisição.

A **Fase 2** abordará o estudo detalhado do Lançamento de Resultados (Etapa 5), a proposta de implementação (Etapa 6) e o roadmap de migração (Etapa 7).
