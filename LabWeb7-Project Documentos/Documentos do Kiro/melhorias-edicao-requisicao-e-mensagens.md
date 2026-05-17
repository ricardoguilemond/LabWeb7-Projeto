# Melhorias — Edição de Requisição, Mensagens e Refatorações

**Data:** 02/05/2026
**Autor:** Kiro
**Escopo:** Tela de Requisição de Exames — edição, cupom, mensagens,
performance de mensagens SweetAlert2

---

## 1. Carregamento do cupom na edição (botão amarelo)

**Problema:** Ao clicar no botão amarelo (editar requisição), o
formulário era preenchido com os dados do paciente, médico,
instituição, posto e tabela, mas o cupom de exames ficava vazio
e a tabela de lançamento de exames não carregava.

**Solução:**

### Backend — Novo endpoint `CarregarCupomEdicao`
- Rota: `GET /Requisitar/CarregarCupomEdicao?pacienteId=&data=`
- Esvazia o cupom atual na `ListaAcumulativa`
- Busca itens da requisição por `ContaExame` + `TabelaExamesId`
- Localiza os `PlanoExames` correspondentes
- Popula a `ListaAcumulativa` e retorna a partial renderizada

### Frontend — `editarRequisicao` atualizada
- Carrega a tabela de exames via `/PartialLancarExames`
  (corrigido de `PlanoExamesItens` que apontava para o controller
  errado)
- Carrega o cupom via `CarregarCupomEdicao`

**Arquivos alterados:**
- `RequisitarController.cs` — novo endpoint
- `_PartialRequisitar.cshtml` — chamada ao novo endpoint

---

## 2. Correção da URL da tabela de lançamento de exames

**Problema:** O `data-url-plano-exames-itens` no `Index.cshtml`
apontava para `PlanoExamesItens` do `PlanoExamesItensController`
(tela de administração, view completa) em vez de
`PartialLancarExames` do `RequisitarController` (partial com grid
compacto).

**Solução:** URL corrigida para `/PartialLancarExames` na função
`editarRequisicao`.

---

## 3. Exclusão de itens anteriores ao salvar (edição)

**Problema:** Ao salvar uma requisição editada, os novos itens
eram adicionados sobre os antigos, causando duplicação.

**Solução:** No `SalvarRequisicao`, antes de gravar os novos
itens, exclui os anteriores do mesmo paciente no dia para a
tabela de exames envolvida.

### Regras de exclusão
- Exclui `Requisitar` (todos os itens do paciente na data
  para a tabela específica)
- Exclui `ItensExamesRealizados` (itens filhos vinculados
  aos `ExamesRealizados` do paciente na data para a tabela)
- **Mantém** `ExamesRealizados` (header — não é excluído)
- Filtra por `PacienteId + Data + TabelaExamesId`
- Usa `TabelaExamesIdOriginal` (campo hidden preenchido ao
  carregar para edição) para identificar a tabela original
  mesmo que o usuário tenha trocado durante a edição

### Campo `TabelaExamesIdOriginal`
- Adicionado campo hidden na partial `_PartialExames.cshtml`
- Adicionada propriedade no ViewModel `vmRequisitar`
- Preenchido pelo JavaScript ao carregar para edição
- Resetado ao limpar o formulário (nova requisição)

**Arquivos alterados:**
- `RequisitarController.cs` — lógica de exclusão
- `_PartialExames.cshtml` — campo hidden
- `vmRequisitar.cs` — propriedade
- `_PartialRequisitar.cshtml` — preenchimento do campo

---

## 4. Prevenção de duplicatas no cupom

**Problema:** O `AdicionarCupom` na `ListaAcumulativa` fazia
`AddRange` sem verificar duplicatas, permitindo que o mesmo
exame fosse adicionado múltiplas vezes.

**Solução:** Verificação por `PlanoExames.Id` antes de adicionar.

**Arquivo alterado:** `IListaAcumulativa.cs`

---

## 5. Esvaziamento do cupom ao trocar tabela

**Problema:** Ao trocar a tabela de exames via modal, a chamada
ao `PartialMontarItensCupom` não passava `id=0`, então o cupom
no servidor não era esvaziado.

**Solução:** Adicionado `?id=0` na chamada do `ModalTabelas.cshtml`.

**Arquivo alterado:** `ModalTabelas.cshtml`

---

## 6. Centralização do traço (-) em colunas do grid

**Problema:** Campos vazios (Posto, Laboratório de Apoio) exibiam
"Não informado" — texto longo que poluía o grid.

**Solução:**
- Backend: substituído por traço `-`
- Frontend: `createdRow` aplica `text-align: center` no `<td>`
  e nos `<span>` internos quando o valor é `-`

**Arquivos alterados:**
- `RequisitarController.cs` — projeção
- `_PartialRequisitar.cshtml` — `createdRow`

---

## 7. SweetAlert2 — CSS global para fontes

**Problema:** Cada chamada `Swal.fire` usava `<span>` inline com
`font-size` e `color`, causando inconsistência e código repetido.

**Solução:**
- Adicionadas regras `.swal2-title` e `.swal2-html-container`
  no `site.css` com tamanhos padrão (1.1em e 0.85em)
- Removidos todos os `<span>` inline das funções genéricas
  (`clickConfirm`, `clickAviso`, `clickAction`) e das views

**Arquivos alterados:**
- `site.css` — regras globais
- `site.js` — funções genéricas
- `Index.cshtml` — loading
- `_PartialRequisitar.cshtml` — confirmação de exclusão

---

## 8. Refatoração das funções de mensagem

### `clickConfirm`
- PNG → ícones nativos Swal2 (`success`, `error`, `question`)
- `async: false` → `async: true` com `Swal.showLoading()`
- Removido `setInterval` vazio

### `clickAviso`
- PNG → ícones nativos Swal2 (`success`, `warning`, `error`)
- `imageHeight: 120` removido (ícone nativo tem tamanho padrão)
- Removido `setInterval` vazio

### `clickAction`
- PNG → ícones nativos Swal2
- `async: false` → `async: true` com `Swal.showLoading()`
- Corrigido `icon: 'danger'` → `icon: 'error'` (danger não
  existe no Swal2)
- `dataType: "text"` → `dataType: "json"` (consistência)

### `CallMethodJson`
- Mantido `async: false` por precaução (POST com intenção
  deliberada de sincronismo no comentário original)

**Arquivo alterado:** `site.js`

---

## 9. Remoção do bPopup

**Problema:** `jquery.bpopup.min.js` era carregado em todas as
páginas via `_Layout.cshtml` mas não tinha nenhuma chamada ativa.

**Solução:** Referência removida do `_Layout.cshtml`. O arquivo
`.js` permanece no disco para remoção manual posterior.

**Arquivo alterado:** `_Layout.cshtml`

---

## Checklist de validação

```
[x] Build executado: 0 erros e 0 avisos (projeto MVC)
[x] Nenhum pacote adicionado, removido ou atualizado
[x] Nenhuma regra de negócio existente quebrada
[x] Encoding dos arquivos preservado
[x] Marcação de código aplicada
[x] Steerings atualizados (Kiro e Qoder)
```


---

## 10. Campo Id do Paciente visível

- Campo readonly ao lado do nome do paciente com width 60px
- Preenchido automaticamente ao selecionar paciente
  (modal, busca, edição)
- Arquivos: `_PartialFormulario.cshtml`,
  `ModalPacientes.cshtml`, `_PartialRequisitar.cshtml`

---

## 11. Scroll no topo na edição

- Removida mensagem `clickAviso` da edição (feedback visual
  implícito)
- `setTimeout(500ms)` com `scrollTo({top:0})` após operações
  assíncronas
- Garante que a página permaneça no topo após carregar
  tabela + cupom

---

## 12. Cupom esvaziado após salvamento

- `fetch('/PartialMontarItensCupom?id=0')` após salvar com
  sucesso
- `TabelaExamesIdOriginal` resetado para 0
- Arquivo: `Index.cshtml`

---

## 13. Coluna Id Paciente ampliada

- Largura aumentada de 70px para 80px para acomodar Ids de
  3 dígitos
- Offsets sticky recalculados: col3 `left:130px`,
  col4 `left:310px`
- Arquivo: `_PartialRequisitar.cshtml`

---

## 14. F5/CTRL+F5 não aciona salvamento

- Listener global removido do `_Layout.cshtml` e
  `layout-footer.js`
- F5 mantém comportamento padrão do browser (recarregar
  página)
- Regra: salvamento exclusivamente por acionamento de botão

---

## 15. Prevenção de acúmulo de handlers jQuery

- `$(document).off('click.lancarExame')` antes de `.on()`
- Namespace `.lancarExame` para identificação única
- Arquivo: `_PartialLancarExames.cshtml`

---

## 16. Validação de ValorItem no cupom

- Itens sem valor (null ou <= 0) não são adicionados ao cupom
- Mensagem em vermelho: "Este item de exame não possui valor
  definido e não pode ser selecionado"
- Arquivos: `RequisitarController.cs`,
  `_PartialMontarItensCupom.cshtml`

---

## 17. TabelaExamesId no fluxo de edição

- Campo `TabelaExamesId` adicionado ao
  `vmRequisitarSimplificado`
- `GetLancamentosHoje` retorna `TabelaExamesId` no JSON
- Botão amarelo passa `tabelaExamesId` ao
  `editarRequisicao`
- `CarregarRequisicaoParaEdicao` filtra por
  `TabelaExamesId`
- Garante que a requisição correta seja carregada quando o
  paciente tem múltiplas no mesmo dia
