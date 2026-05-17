im---
inclusion: always
description: Regras de frontend — CSS, JavaScript, jQuery e DataTables para o projeto LabWeb7
---

# Steering — Regras de Frontend: CSS, JavaScript e DataTables

## Princípio Geral

**Simples é melhor que sofisticado quando tudo funciona bem.**

- Preferir CSS padrão sobre soluções JavaScript para layout e visual.
- Preferir JavaScript puro (vanilla) sobre bibliotecas adicionais.
- Preferir manipulação direta do DOM sobre plugins de terceiros.
- Só adicionar complexidade quando o CSS padrão comprovadamente
  não resolver o problema.

## CSS

### Regras obrigatórias

1. Usar CSS nativo (`position: sticky`, `overflow`, `z-index`,
   `nth-child`, etc.) como primeira opção para qualquer
   comportamento visual ou de layout.
2. Não introduzir frameworks CSS (Bootstrap Grid, Tailwind, etc.)
   além do que já existe no projeto.
3. Usar `!important` apenas quando necessário para sobrescrever
   estilos injetados por bibliotecas (ex: DataTables aplica
   estilos inline que exigem override).
4. Seletores devem ser ancorados no wrapper ou container
   específico da tela (ex: `#wrapperRequisitar`) para evitar
   vazamento de estilos para outros grids ou componentes.
5. Estilos específicos de uma partial devem ficar no bloco
   `<style>` da própria partial, não em arquivos CSS globais,
   salvo quando o estilo for reutilizado em múltiplas telas.

### Colunas fixas em grids com scroll horizontal

- Usar `position: sticky` com valores de `left` calculados
  pela largura acumulada das colunas anteriores.
- Travar largura das colunas fixas com `min-width`, `width`
  e `max-width` iguais para garantir que os offsets de `left`
  permaneçam corretos.
- Diferenciar `z-index` entre header (maior) e body (menor)
  para que o cabeçalho fique acima das células ao rolar.
- Aplicar `background-color` sólido nas colunas fixas (header
  e body) para evitar que o conteúdo das colunas que rolam
  fique visível por trás.
- Aplicar `box-shadow` na última coluna fixa para criar
  separação visual entre a área fixa e a área que rola.
- Manter zebra (odd/even) e hover nas colunas fixas para
  consistência visual.

### Tamanho de fonte em grids DataTables

- O DataTables 2.x injeta elementos internos nos `<th>`
  (`.dt-column-title`, `.dt-column-order`, `span`).
- Para controlar o tamanho da fonte dos títulos, o override
  CSS deve atingir tanto o `<th>` quanto esses sub-elementos.
- Exemplo de seletor completo:
  ```css
  #wrapper table.dataTable thead th,
  #wrapper table.dataTable thead th span,
  #wrapper table.dataTable thead th .dt-column-title,
  #wrapper table.dataTable thead th .dt-column-order {
      font-size: 14px !important;
  }
  ```

### Alinhamento condicional em células do body

- O DataTables 2.x também envolve o conteúdo de cada `<td>`
  do body em `<span>` internos.
- Para alterar o alinhamento de uma célula específica (ex:
  centralizar um traço quando o campo está vazio), aplicar
  `text-align` tanto no `<td>` quanto nos `<span>` filhos.
- Usar `createdRow` do DataTables para aplicar condicionalmente:
  ```javascript
  createdRow: function (row, data) {
      if (data.campo === '-') {
          var td = $('td', row).eq(indiceColuna);
          td.css('text-align', 'center');
          td.find('span').css('text-align', 'center');
      }
  }
  ```
- Não usar `display:block` em `<span>` dentro de `<td>` — isso
  aumenta a altura da linha. Manter o `display` original e
  aplicar apenas `text-align`.

## JavaScript e jQuery

### Regras obrigatórias

1. Usar JavaScript puro (vanilla) sempre que possível.
2. jQuery é aceito porque já faz parte do projeto — não
   substituir por outra biblioteca, mas também não expandir
   seu uso desnecessariamente.
3. Não adicionar bibliotecas JavaScript de terceiros sem
   aprovação explícita do usuário.
4. Manipulação de DOM para criar elementos auxiliares
   (ex: barra de scroll superior) é preferível a plugins.
5. Usar `fetch` ou `$.ajax` conforme o padrão já existente
   na tela — não misturar ambos na mesma função sem motivo. Mas,
   se for melhor otimizado preferir o uso de `fetch`.
6. Sempre converter datas do grid (`dd/MM/yyyy`) para
   `yyyy-MM-dd` antes de enviar ao backend.
7. Confirmações destrutivas (exclusão) devem usar SweetAlert2
   (`Swal.fire`) — já presente no projeto.
8. Mensagens informativas devem usar `clickAviso` — função
   já existente no projeto.
9. Handlers delegados em partials carregadas via `$.load()`
   devem usar namespace e `$(document).off()` antes de
   `$(document).on()` para evitar acúmulo. Exemplo:
   `$(document).off('click.meuNamespace').on('click.meuNamespace', selector, handler)`

### SweetAlert2 — Padrão de fontes para confirmações

O tamanho de fonte das mensagens SweetAlert2 é controlado
globalmente via `site.css`, usando as classes nativas do
SweetAlert2:

```css
.swal2-title {
    font-size: 1.1em !important;
    color: gray;
}
.swal2-html-container {
    font-size: 0.85em !important;
    color: #646464;
}
```

- Não usar `<span style="font-size:...">` inline no `title`
  ou `html` do `Swal.fire` — o CSS global já cuida disso.
- Se uma mensagem específica precisar de tamanho diferente,
  o inline sobrescreve o CSS global normalmente.
- As funções genéricas `clickConfirm`, `clickAviso` e
  `clickAction` do `site.js` passam o título e mensagem
  como texto puro — sem `<span>` wrapper.
- O `clickAviso` usa `returnFocus: false` para evitar que o
  browser restaure o foco no elemento anterior ao fechar,
  prevenindo scroll indesejado.
- Mensagens informativas são dispensáveis quando a ação visual
  já é autoexplicativa (ex: preenchimento de campos na edição).
  Preferir feedback visual implícito sobre mensagens explícitas.

### Barra de rolagem superior (dual scrollbar)

- Quando um grid DataTables usa `scrollX: true`, a barra de
  rolagem horizontal fica apenas no rodapé (`.dt-scroll-body`).
- Para replicar no topo, injetar um `div` com `overflow-x: auto`
  antes do `.dt-scroll-body`, contendo um `div` interno com a
  mesma largura do `scrollWidth` do body.
- Sincronizar `scrollLeft` bidirecionalmente entre os dois
  containers via event listeners de `scroll`.
- Verificar se o elemento já existe antes de criar, para evitar
  duplicação em reloads.
- Usar `setTimeout` após a inicialização do DataTables para
  garantir que o DOM esteja pronto.

## DataTables

### Versão e atualização

- O DataTables **pode** ser atualizado para versões mais novas
  sob demanda, desde que:
  1. Haja avaliação prévia do impacto no design e nos recursos
     que atualmente funcionam.
  2. Nenhum recurso existente quebre após a atualização.
  3. O layout visual permaneça consistente com o design atual.
  4. O usuário aprove a atualização antes da execução.
- Nenhum outro plugin jQuery ou biblioteca de grid pode ser
  introduzido como substituto do DataTables sem aprovação.

### Configuração padrão para grids do projeto

- `scrollX: true` — habilita scroll horizontal.
- `autoWidth: false` — respeita larguras fixas definidas por
  coluna.
- `responsive: false` — o projeto usa scroll horizontal, não
  collapse responsivo.
- Larguras de coluna definidas na propriedade `columns[].width`.
- Ordenação padrão: `order: [[0, 'desc']]` (coluna Id
  decrescente), salvo necessidade específica da tela.

### Não usar plugins DataTables para colunas fixas

- **Não usar** o plugin `fixedColumns` do DataTables.
- A fixação de colunas deve ser feita via CSS `position: sticky`
  conforme descrito na seção CSS acima.
- Motivo: o plugin `fixedColumns` clona a tabela internamente,
  causa conflitos com `scrollX`, e é mais pesado e menos
  previsível que a solução CSS pura.

### Layout e idioma

- Usar a propriedade `layout` (DataTables 2.x) para posicionar
  controles (pageLength, search, info, paging).
- Textos devem estar em Português-Brasil via propriedade
  `language`.
- Tabela vazia ou sem resultados: exibir mensagem em vermelho
  e negrito.

### Tabela HTML

- A tabela deve ter `<thead>`, `<tbody>` vazio e `<tfoot>`.
- Classe padrão: `display compact order-column table-striped
  table-hover nowrap`.
- Se a tabela tiver `width: 100%` vindo de CSS externo
  (ex: `mydatatables.css`), sobrescrever com
  `width: auto !important` no seletor da tabela específica
  para que o scroll horizontal funcione.

## Validação de Alterações Frontend

Antes de considerar uma alteração de frontend concluída:

1. Verificar se o CSS não vaza para outros componentes da página.
2. Confirmar que colunas fixas mantêm alinhamento ao rolar.
3. Confirmar que a barra superior (se existir) sincroniza com
   a inferior.
4. Confirmar que o tamanho de fonte dos títulos está consistente
   entre header e footer.
5. Verificar zebra e hover nas colunas fixas.
6. Executar o build do projeto e confirmar 0 erros e 0 avisos.
