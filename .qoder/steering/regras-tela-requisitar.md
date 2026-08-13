---
trigger: always
description: Proteção e regras críticas da tela Requisitar — alterações apenas com autorização expressa do usuário
---

# Steering — Tela de Requisitar (Requisição de Exames)

## ⚠️ Proteção Principal

A tela **Requisitar** (controller `Requisitar`, views em `LabWebMvc.MVC/Views/Requisitar`) é uma tela **CRÍTICA E ESTÁVEL** do sistema.

- **NÃO ALTERAR** comportamento, layout, fluxo, regras de negócio, CSS ou JavaScript desta tela sem **autorização expressa e explícita** do usuário.
- **NÃO APLICAR** melhorias de performance, refatorações cosméticas, alterações de biblioteca ou reorganizações de código nesta tela sem aprovação prévia.
- **NÃO MODIFICAR** as partials `_PartialFormulario.cshtml`, `_PartialExames.cshtml`, `_PartialLancarExames.cshtml`, `_PartialRequisitar.cshtml`, `_PartialMontarCupom.cshtml` e `Index.cshtml` sem validar integralmente o funcionamento com o usuário.

## 🎯 Funcionalidades Críticas (Não Alterar)

### 1. Fluxo de Lançamento de Exames
- Busca e seleção de paciente por ENTER no campo Nome do Paciente.
- Seleção de Instituição, Posto, Tabela de Preços e Médico via modais ou busca direta.
- Grid de **Roll de Exames** com duas colunas: **Check** (código da conta) e **Roll de exames** (descrição).
- Clique na linha do exame alterna seleção e atualiza o cupom (`montarCupomPorId` / `removerCupomPorId`).
- Campo de busca personalizado (`customSearchBox`) filtra o grid em tempo real e adiciona o primeiro item ao cupom ao pressionar ENTER.

### 2. Cupom de Caixa
- Exibe exames selecionados com descrição e valor.
- Botão **Esvaziar o cupom** limpa a seleção.
- Botão **Salvar e Imprimir Cupom** salva a requisição e dispara impressão.

### 3. Grid de Requisições de Hoje
- DataTable `modeloTableRequisitar` exibe requisições do dia.
- Botões por linha: reimprimir cupom, editar requisição, excluir requisição, recebimento na portaria.
- Clique na linha expande master/detail dos itens da requisição.

### 4. Recebimento na Portaria
- Modal `modalRecebimentoPortaria` permite lançar recebimento vinculado ao exame.
- Deve permanecer inicializado pelo Bootstrap 5 e **não** deve ser afetado por CSS genérico de modais.

## 🚫 Problemas Históricos Conhecidos (Não Reintroduzir)

### 1. Overlay modalLoading
- O overlay global de loading dependia exclusivamente de `window.onload`.
- **NUNCA** confiar em um único evento para remoção de overlays críticos.
- A correção utiliza `window.onload` + `DOMContentLoaded` + timeout de segurança.

### 2. CSS `.modal` global
- Regras como `.modal { display: block; position: fixed; ... }` em arquivos globais (`site.css`) afetam modais Bootstrap 5 e podem travar a tela.
- **SEMPRE** restringir CSS de modal ao escopo `#myModal.modal` ou a classes específicas.

### 3. URLs hardcoded sem prefixo do controller
- URLs como `/PartialLancarExames`, `/PartialMontarItensCupom`, `/RemoverExameCupom` podem quebrar conforme a rota base.
- **SEMPRE** usar `@Url.Action` ou data-attributes configurados na view principal.

### 4. DataTables Responsive no Roll de Exames
- `responsive: true` colapsa a coluna de descrição, escondendo-a e mostrando setas ► indesejadas.
- A configuração da tabela compacta **DEVE** usar `responsive: false`.

### 5. Clonagem do campo customSearchBox
- A função `window.inicializarLancarExames` clona o input `customSearchBox` para evitar acúmulo de listeners.
- Listeners de filtro em tempo real **DEVEM** ser adicionados **após** a clonagem, dentro de `inicializarLancarExames`.

## ✅ Checklist Antes de Qualquer Alteração na Tela Requisitar

- [ ] O usuário autorizou expressamente a alteração?
- [ ] A alteração não afeta o fluxo de lançamento de exames?
- [ ] A alteração não modifica o comportamento do cupom?
- [ ] A alteração não quebra o grid de Requisições de Hoje?
- [ ] A alteração não afeta o modal de Recebimento na Portaria?
- [ ] O CSS foi verificado para não vazar para modais Bootstrap 5?
- [ ] Os testes com Ctrl+F5 foram validados pelo usuário?

## 📁 Arquivos Protegidos

```
LabWebMvc.MVC/Areas/Controllers/RequisitarController.cs
LabWebMvc.MVC/Views/Requisitar/Index.cshtml
LabWebMvc.MVC/Views/Requisitar/Partials/_PartialFormulario.cshtml
LabWebMvc.MVC/Views/Requisitar/Partials/_PartialExames.cshtml
LabWebMvc.MVC/Views/Requisitar/Partials/_PartialLancarExames.cshtml
LabWebMvc.MVC/Views/Requisitar/Partials/_PartialRequisitar.cshtml
LabWebMvc.MVC/Views/Requisitar/Partials/_PartialMontarCupom.cshtml
LabWebMvc.MVC/wwwroot/js/requisitar-exames.js
LabWebMvc.MVC/wwwroot/js/mydatatables.js (função configTableCompacta)
LabWebMvc.MVC/wwwroot/css/site.css (regras de .modal)
LabWebMvc.MVC/Views/Shared/_Layout.cshtml (overlay modalLoading)
```

## 🔗 Relacionamentos

- Regras de frontend: `regras-frontend-css-js.md`
- Regras de controllers/views: `regras-controllers-views.md`
- Regras de requisição/resultados: `.kiro/steering/regras-requisicao-resultados.md`
- Regras de plano de exames: `regras-plano-exames.md`

---

**Steering criado por Qoder em 13/08/2026**  
*Motivação: proteger a tela Requisitar após série de regressões causadas por alterações não autorizadas.*
