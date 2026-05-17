# Análise de Migração — Bootstrap 4.3.1 para Bootstrap 5

**Data da análise:** Gerado pelo Kiro
**Projeto:** LabWeb7
**Escopo:** Inventário completo de dependências legadas do Bootstrap 4.3.1

---

## Situação Atual — Versões Identificadas

| Recurso                                  | Versão         | Caminho                                  |
|------------------------------------------|----------------|------------------------------------------|
| CSS Bootstrap legado                     | **4.3.1**      | `lib/bootstrap/dist/css/bootstrap.css`   |
| CSS Bootstrap via SB Admin (styles.css)  | **5.2.3**      | `css/styles.css`                         |
| JS Bootstrap bundle                      | **5.0.2**      | `js/bootstrap.bundle.min.js`             |

> **Nota:** O `styles.css` contém Bootstrap **5.2.3** embutido
> (SB Admin v7.0.7), não 5.0.2 como informado inicialmente.
> O JS bundle é de fato 5.0.2. Há divergência de versão entre
> CSS (5.2.3) e JS (5.0.2).

---

## Seção 1 — Inventário de Dependências Bootstrap 4

### 1.1 Classes CSS Legadas Encontradas

#### 1.1.1 `form-group` — 282 ocorrências em 22 arquivos

Classe removida no Bootstrap 5. Substituir por `mb-3` ou
remover se o espaçamento já for controlado por CSS próprio.

| Arquivo                                | Qtd | Prioridade |
|----------------------------------------|-----|------------|
| AlterarInstituicao.cshtml              |  31 | Alta       |
| IncluirInstituicao.cshtml              |  30 | Alta       |
| _PartialFormulario.cshtml (Requisitar) |  24 | Alta       |
| IncluirPaciente.cshtml                 |  23 | Alta       |
| AlterarPaciente.cshtml                 |  23 | Alta       |
| AlterarClasseExames.cshtml             |  18 | Média      |
| IncluirClasseExames.cshtml             |  17 | Média      |
| IncluirUsuarios.cshtml                 |  14 | Média      |
| AlterarPostos.cshtml                   |  12 | Média      |
| IncluirPostos.cshtml                   |  12 | Média      |
| PlanoExamesItens/Index.cshtml          |  10 | Média      |
| SenhasManut.cshtml                     |   9 | Média      |
| IncluirPlanoExames.cshtml              |   9 | Média      |
| AlterarPlanoExamesItens.cshtml         |   8 | Média      |
| AlterarSenha.cshtml                    |   7 | Média      |
| AlterarPlanoExames.cshtml              |   7 | Média      |
| Login.cshtml                           |   6 | Média      |
| IncluirMedico.cshtml                   |   6 | Média      |
| AlterarMedico.cshtml                   |   6 | Média      |
| _PartialExames.cshtml                  |   5 | Baixa      |
| SenhaEsquecida.cshtml                  |   4 | Baixa      |
| _PartialFiltroPesquisas.cshtml         |   1 | Baixa      |

**Equivalente BS5:** `mb-3` ou remover (depende do layout).
**Risco:** Baixo — o BS 4.3.1 CSS ainda fornece a classe.
Após remoção do BS4, os formulários perderão espaçamento.


#### 1.1.2 `class="close"` com `data-dismiss` — 17 ocorrências

Padrão de botão de fechar modal do Bootstrap 4.
No BS5, substituído por `btn-close` + `data-bs-dismiss`.

| Arquivo                              | Linha | Risco |
|--------------------------------------|-------|-------|
| Senhas/ResetarSenha.cshtml           |    11 | Alto  |
| Postos/ConsultarPostos.cshtml        |    57 | Alto  |
| PlanoExamesItens/Modelo...Itens.cshtml |  70 | Alto  |
| PlanoExamesItens/Consultar...cshtml  |    51 | Alto  |
| PlanoExames/Consultar...cshtml       |    51 | Alto  |
| PlanoExames/Modelo...cshtml          |   122 | Alto  |
| Mensagem/ErrorGenerico.cshtml        |    11 | Alto  |
| Mensagem/Error.cshtml                |    11 | Alto  |
| Mensagem/MensagemView.cshtml         |    17 | Alto  |
| Mensagem/AcessoNegado.cshtml         |    11 | Alto  |
| Mensagem/AcessoEmail.cshtml          |    11 | Alto  |
| Pacientes/ConsultarPaciente.cshtml   |    62 | Alto  |
| Mensagem/MensagemTela.cshtml         |    17 | Alto  |
| Instituicoes/Consultar...cshtml      |    80 | Alto  |
| Medicos/ConsultarMedico.cshtml       |    56 | Alto  |
| Home/Logout.cshtml                   |    11 | Alto  |
| ClasseExames/Consultar...cshtml      |    79 | Alto  |

**Equivalente BS5:**
```html
<!-- BS4 (atual) -->
<button type="button" class="close" data-dismiss="modal">
  <span aria-hidden="true">&times;</span>
</button>

<!-- BS5 (migrado) -->
<button type="button" class="btn-close"
        data-bs-dismiss="modal" aria-label="Fechar">
</button>
```
**Risco:** Alto — após remoção do BS4 CSS, o botão `close`
perde estilização e `data-dismiss` não funciona com JS BS5.

#### 1.1.3 `ml-3` (margin-left) — 3 ocorrências

| Arquivo                       | Linha | Equivalente BS5 |
|-------------------------------|-------|------------------|
| Mensagem/Error500.cshtml      |    17 | `ms-3`           |
| Mensagem/Error404.cshtml      |    17 | `ms-3`           |
| Mensagem/Error401.cshtml      |    21 | `ms-3`           |

**Risco:** Baixo — o BS5 CSS (styles.css) não define `ml-3`.
Atualmente funciona porque o BS4 CSS é carregado primeiro.

#### 1.1.4 `text-left` — 3 ocorrências

| Arquivo                       | Linha | Equivalente BS5 |
|-------------------------------|-------|------------------|
| Mensagem/MensagemView.cshtml  |    24 | `text-start`     |
| Mensagem/MensagemView.cshtml  |    25 | `text-start`     |
| Mensagem/MensagemTela.cshtml  |    24 | `text-start`     |

**Risco:** Baixo — `text-left` não existe no BS5.

#### 1.1.5 `font-weight-normal` — 3 ocorrências

| Arquivo                       | Linha | Equivalente BS5 |
|-------------------------------|-------|------------------|
| Senhas/SenhaEsquecida.cshtml  |    20 | `fw-normal`      |
| Home/Login.cshtml              |    36 | `fw-normal`      |
| Home/Login.cshtml              |    73 | `fw-normal`      |

**Risco:** Baixo — classe utilitária que não existe no BS5.

#### 1.1.6 `btn-block` — 2 ocorrências

| Arquivo                              | Linha | Equivalente BS5       |
|--------------------------------------|-------|-----------------------|
| Home/Login.cshtml                    |    65 | `d-grid` no wrapper   |
| ClasseExames/IncluirClasseExames.cshtml | 251 | `d-grid` no wrapper |

**Risco:** Baixo — classe removida no BS5. Substituir por
`d-grid` no container pai ou `w-100` no botão.

#### 1.1.7 `control-label` — 9 ocorrências

| Arquivo                  | Linhas                    | Equivalente BS5  |
|--------------------------|---------------------------|------------------|
| Senhas/SenhasManut.cshtml | 21,26,31,36,41,46,51,56,61 | `form-label`    |

**Risco:** Baixo — classe do BS3/BS4 que não existe no BS5.
No BS5, usar `form-label`.

#### 1.1.8 `shrink-to-fit=no` no viewport meta — 1 ocorrência

| Arquivo                       | Linha | Observação              |
|-------------------------------|-------|-------------------------|
| Shared/_Layout.cshtml         |     9 | Desnecessário no BS5    |

**Risco:** Nenhum — pode ser removido, mas não causa problema.

### 1.2 Classes CSS Legadas NÃO Encontradas

As seguintes classes Bootstrap 4 **não foram encontradas**
em nenhum arquivo `.cshtml` do projeto:

| Classe BS4            | Equivalente BS5     | Status          |
|-----------------------|---------------------|-----------------|
| `mr-*`                | `me-*`              | Não encontrada  |
| `pl-*`                | `ps-*`              | Não encontrada  |
| `pr-*`                | `pe-*`              | Não encontrada  |
| `float-left`          | `float-start`       | Não encontrada  |
| `float-right`         | `float-end`         | Não encontrada  |
| `badge-*`             | `bg-*` + `badge`    | Não encontrada  |
| `sr-only`             | `visually-hidden`   | Não encontrada  |
| `custom-control`      | (removida)          | Não encontrada  |
| `input-group-prepend` | (removida)          | Não encontrada  |
| `input-group-append`  | (removida)          | Não encontrada  |
| `text-right`          | `text-end`          | Não encontrada  |
| `font-weight-bold`    | `fw-bold`           | Não encontrada  |

---

## Seção 2 — Referências CSS e JS no _Layout.cshtml

### 2.1 Ordem de Carregamento (exata)

```
<head>
  Linha 15: ~/lib/bootstrap/dist/css/bootstrap.css  → BS 4.3.1
  Linha 33: ~/css/styles.css                         → BS 5.2.3 (SB Admin)
  Linha 34: ~/css/site.css                           → CSS customizado
  Linha 38: ~/css/mydatatables.css                   → DataTables CSS
</head>
<body>
  Linha 237: ~/js/bootstrap.bundle.min.js            → BS 5.0.2 JS
  Linha 238: ~/js/scripts.js                         → SB Admin scripts
</body>
```

### 2.2 Qual Versão Vence (Cascata CSS)

O CSS funciona por cascata: **o último carregado vence**.

- `styles.css` (BS 5.2.3) é carregado **depois** de
  `lib/bootstrap/dist/css/bootstrap.css` (BS 4.3.1).
- Para classes com **mesmo nome** em ambas as versões
  (ex: `btn`, `form-control`, `modal`, `card`, `row`, `col-*`),
  o **BS 5.2.3 vence**.
- Para classes que **só existem no BS4** (ex: `form-group`,
  `ml-*`, `mr-*`, `text-left`, `font-weight-normal`,
  `control-label`, `btn-block`), o BS4 CSS ainda as fornece.
- Para classes que **só existem no BS5** (ex: `ms-*`, `me-*`,
  `text-start`, `fw-normal`, `visually-hidden`), o BS5 CSS
  as fornece normalmente.

**Conclusão:** O projeto funciona hoje porque o BS4 CSS
fornece as classes legadas que o BS5 não possui. Ao remover
o BS4 CSS, essas classes deixarão de existir.

### 2.3 Referências a `lib/bootstrap/` no Projeto

| Arquivo                                | Tipo       |
|----------------------------------------|------------|
| Views/Shared/_Layout.cshtml (linha 15) | CSS link   |
| Em testes/Lixo CSS_e_JS...txt          | Documentação |

**Única referência ativa:** `_Layout.cshtml` linha 15.

### 2.4 Conteúdo da Pasta `lib/bootstrap/dist/`

A pasta contém a distribuição completa do Bootstrap 4.3.1:
- `css/` — 12 arquivos (bootstrap.css, grid, reboot + maps)
- `js/` — 8 arquivos (bootstrap.js, bundle + maps)
- Nenhum JS do BS4 é carregado pelo `_Layout.cshtml`.
- Apenas o CSS é utilizado.

---

## Seção 3 — Atributos `data-*` que Precisam de Migração

### 3.1 `data-dismiss` → `data-bs-dismiss`

| Arquivo                              | Linha | Contexto              |
|--------------------------------------|-------|-----------------------|
| _CookieConsentPartial.cshtml         |    14 | `data-dismiss="alert"`|
| Senhas/ResetarSenha.cshtml           |    11 | `data-dismiss="modal"`|
| Postos/ConsultarPostos.cshtml        |    57 | `data-dismiss="modal"`|
| PlanoExamesItens/Modelo...cshtml     |    70 | `data-dismiss="modal"`|
| PlanoExamesItens/Consultar...cshtml  |    51 | `data-dismiss="modal"`|
| PlanoExames/Consultar...cshtml       |    51 | `data-dismiss="modal"`|
| PlanoExames/Modelo...cshtml          |   122 | `data-dismiss="modal"`|
| Mensagem/ErrorGenerico.cshtml        |    11 | `data-dismiss="modal"`|
| Mensagem/Error.cshtml                |    11 | `data-dismiss="modal"`|
| Mensagem/MensagemView.cshtml         |    17 | `data-dismiss="modal"`|
| Mensagem/MensagemView.cshtml         |    31 | `data-dismiss="modal"`|
| Mensagem/MensagemTela.cshtml         |    17 | `data-dismiss="modal"`|
| Mensagem/MensagemTela.cshtml         |    28 | `data-dismiss="modal"`|
| Mensagem/AcessoNegado.cshtml         |    11 | `data-dismiss="modal"`|
| Mensagem/AcessoEmail.cshtml          |    11 | `data-dismiss="modal"`|
| Pacientes/ConsultarPaciente.cshtml   |    62 | `data-dismiss="modal"`|
| Instituicoes/Consultar...cshtml      |    80 | `data-dismiss="modal"`|
| Medicos/ConsultarMedico.cshtml       |    56 | `data-dismiss="modal"`|
| Home/Logout.cshtml                   |    11 | `data-dismiss="modal"`|
| ClasseExames/Consultar...cshtml      |    79 | `data-dismiss="modal"`|

**Total:** 20 ocorrências em 18 arquivos.
**Risco:** **CRÍTICO** — `data-dismiss` não funciona com o
JS do Bootstrap 5. Atualmente funciona porque o BS4 CSS
define `.close` e o jQuery BS4 não está carregado (o JS é
BS5). **Esses botões de fechar modal provavelmente já não
funcionam via atributo** — podem estar funcionando por outro
mecanismo (clique no backdrop, tecla ESC, ou JS customizado).

### 3.2 `data-toggle="tooltip"` → `data-bs-toggle="tooltip"`

| Arquivo                       | Linha | Contexto               |
|-------------------------------|-------|------------------------|
| Senhas/Index.cshtml           |   130 | Tooltip em link ação   |
| Senhas/Index.cshtml           |   131 | Tooltip em link ação   |
| Senhas/Index.cshtml           |   132 | Tooltip em link ação   |
| mydatatables.js               |     7 | `$('[data-toggle=...]')` |
| mydatatables.js               |    86 | `$('[data-toggle=...]')` |
| mydatatables.js               |   144 | `$('[data-toggle=...]')` |
| mydatatables.js               |   206 | `$('[data-toggle=...]')` |

**Total:** 7 ocorrências (3 em HTML, 4 em JS).
**Risco:** Médio — os tooltips usam jQuery `.tooltip()` que
depende do BS4 jQuery plugin. Com o JS BS5 (vanilla), o
seletor `[data-toggle="tooltip"]` não será reconhecido.

### 3.3 Atributos `data-*` NÃO Encontrados

| Atributo BS4       | Equivalente BS5       | Status          |
|--------------------|-----------------------|-----------------|
| `data-target`      | `data-bs-target`      | Não encontrado  |
| `data-ride`        | `data-bs-ride`        | Não encontrado  |
| `data-slide`       | `data-bs-slide`       | Não encontrado  |
| `data-parent`      | `data-bs-parent`      | Não encontrado  |
| `data-backdrop`    | `data-bs-backdrop`    | Não encontrado  |
| `data-keyboard`    | `data-bs-keyboard`    | Não encontrado  |

### 3.4 Atributos Já Migrados para BS5

| Atributo BS5         | Arquivo                          | Status    |
|----------------------|----------------------------------|-----------|
| `data-bs-toggle`     | _Layout.cshtml (dropdown)        | Migrado ✓ |
| `data-bs-toggle`     | MenuDinamico/Default.cshtml      | Migrado ✓ |
| `data-bs-toggle`     | Requisitar/Index.cshtml (pills)  | Migrado ✓ |
| `data-bs-target`     | MenuDinamico/Default.cshtml      | Migrado ✓ |
| `data-bs-dismiss`    | Requisitar/ModalTabelas.cshtml   | Migrado ✓ |
| `data-bs-dismiss`    | Requisitar/ModalPostos.cshtml    | Migrado ✓ |
| `data-bs-dismiss`    | Requisitar/ModalPacientes.cshtml | Migrado ✓ |
| `data-bs-dismiss`    | Requisitar/ModalMedicos.cshtml   | Migrado ✓ |
| `data-bs-dismiss`    | Requisitar/ModalInstituicoes.cshtml | Migrado ✓ |
| `data-bs-dismiss`    | _StatusMessage.cshtml (Identity) | Migrado ✓ |

---

## Seção 4 — JavaScript jQuery Dependente de Bootstrap

### 4.1 Chamadas jQuery `.modal()` — Padrão BS4

| Arquivo                                | Linha | Chamada                              |
|----------------------------------------|-------|--------------------------------------|
| Requisitar/_PartialFormulario.cshtml   |    95 | `$("#...").modal("show")`            |
| Requisitar/_PartialFormulario.cshtml   |   100 | `$("#...").modal("show")`            |
| Requisitar/_PartialFormulario.cshtml   |   134 | `$("#...").modal("show")`            |
| Requisitar/_PartialFormulario.cshtml   |   139 | `$("#...").modal("show")`            |
| Requisitar/ModalPacientes.cshtml       |   107 | `$(modalEl).modal('hide')` (fallback)|

**Total:** 5 ocorrências.
**Risco:** **CRÍTICO** — `$().modal()` é API jQuery do BS4.
O BS5 usa `new bootstrap.Modal(el).show()`. Essas chamadas
dependem do plugin jQuery do Bootstrap 4 que **não está
carregado** (o JS é BS5 vanilla). Podem estar funcionando
por coincidência ou por outro mecanismo.

### 4.2 Chamadas jQuery `.tooltip()` — Padrão BS4

| Arquivo          | Linha | Chamada                              |
|------------------|-------|--------------------------------------|
| mydatatables.js  |     7 | `$('[data-toggle="tooltip"]').tooltip()` |
| mydatatables.js  |    86 | `$('[data-toggle="tooltip"]').tooltip()` |
| mydatatables.js  |   144 | `$('[data-toggle="tooltip"]').tooltip()` |
| mydatatables.js  |   206 | `$('[data-toggle="tooltip"]').tooltip()` |

**Total:** 4 ocorrências.
**Risco:** **ALTO** — `.tooltip()` é API jQuery do BS4.
No BS5, tooltips são inicializados via:
```javascript
var tooltipList = [].slice.call(
  document.querySelectorAll('[data-bs-toggle="tooltip"]')
);
tooltipList.map(function (el) {
  return new bootstrap.Tooltip(el);
});
```

### 4.3 Código Já Migrado para BS5 Vanilla JS

| Arquivo                          | Linha | Padrão BS5                          |
|----------------------------------|-------|-------------------------------------|
| js/requisitar-exames.js          |    11 | `bootstrap.Modal.getInstance()`     |
| js/requisitar-exames.js          |    13 | `new bootstrap.Modal(el)`           |
| Requisitar/ModalPacientes.cshtml |   102 | `bootstrap.Modal.getInstance()`     |
| Mensagem/MensagemConfirma.cshtml |    51 | `new bootstrap.Modal(el)`           |

### 4.4 Chamadas jQuery NÃO Encontradas

| Padrão             | Status          |
|--------------------|-----------------|
| `.collapse()`      | Não encontrado  |
| `.dropdown()`      | Não encontrado  |
| `.popover()`       | Não encontrado  |
| `.carousel()`      | Não encontrado  |

---

## Seção 5 — Plano de Migração em Etapas

### Etapa 1 — Inventário (este documento) ✓

- [x] Mapear todas as classes CSS legadas do BS4
- [x] Mapear todos os atributos `data-*` legados
- [x] Mapear todas as chamadas jQuery dependentes de BS4
- [x] Identificar o que já foi migrado para BS5
- [x] Documentar a ordem de carregamento CSS/JS

### Etapa 2 — Substituições Seguras (baixo risco)

Substituições que podem ser feitas sem impacto visual:

| De (BS4)             | Para (BS5)        | Arquivos | Esforço |
|----------------------|-------------------|----------|---------|
| `ml-3`               | `ms-3`            |        3 | Mínimo  |
| `text-left`          | `text-start`      |        2 | Mínimo  |
| `font-weight-normal` | `fw-normal`       |        2 | Mínimo  |
| `btn-block`          | `d-grid` + `w-100`|        2 | Baixo   |
| `control-label`      | `form-label`      |        1 | Baixo   |

**Estimativa:** ~15 minutos. Pode ser feito com o BS4 CSS
ainda carregado (as classes BS5 já existem no `styles.css`).

### Etapa 3 — Substituições de Risco Médio

#### 3a. Migrar `form-group` (282 ocorrências)

**Estratégia recomendada:**
1. Adicionar `mb-3` ao lado de `form-group` em todos os
   arquivos (ex: `class="form-group mb-3"`).
2. Testar visualmente cada formulário.
3. Após confirmação, remover `form-group`.

**Nota:** Alguns arquivos (ex: Configuracoes/Index.cshtml)
já usam `form-group mb-3` — padrão correto.

**Estimativa:** ~2 horas (substituição + teste visual).

#### 3b. Migrar botões `close` + `data-dismiss` (20 ocorrências)

**Estratégia:**
1. Substituir `class="close" data-dismiss="modal"` por
   `class="btn-close" data-bs-dismiss="modal"`.
2. Remover o `<span>&times;</span>` interno (BS5 usa
   ícone via CSS no `btn-close`).
3. Testar cada modal individualmente.

**Estimativa:** ~1 hora.

#### 3c. Migrar `data-toggle="tooltip"` (7 ocorrências)

**Estratégia:**
1. Nos HTML: trocar `data-toggle="tooltip"` por
   `data-bs-toggle="tooltip"`.
2. No `mydatatables.js`: substituir as 4 chamadas
   `$('[data-toggle="tooltip"]').tooltip()` pelo padrão
   BS5 vanilla JS.

**Estimativa:** ~30 minutos.

### Etapa 4 — Substituições de Risco Alto

#### 4a. Migrar chamadas jQuery `.modal()` (5 ocorrências)

**Estratégia:**
1. Substituir `$("#el").modal("show")` por:
   ```javascript
   var modal = bootstrap.Modal.getOrCreateInstance(
     document.getElementById('el')
   );
   modal.show();
   ```
2. Substituir `$(el).modal('hide')` por:
   ```javascript
   var modal = bootstrap.Modal.getInstance(el);
   if (modal) modal.hide();
   ```
3. O arquivo `requisitar-exames.js` já tem a função
   `abrirModal()` com padrão BS5 — avaliar reutilização.

**Estimativa:** ~1 hora.

#### 4b. Migrar `_CookieConsentPartial.cshtml`

Este arquivo usa `data-dismiss="alert"` (BS4).
Substituir por `data-bs-dismiss="alert"`.

**Estimativa:** ~5 minutos.

### Etapa 5 — Remoção da Referência ao Bootstrap 4.3.1

**Pré-requisito:** Todas as etapas 2, 3 e 4 concluídas
e testadas.

1. Remover a linha 15 do `_Layout.cshtml`:
   ```html
   <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.css" />
   ```
2. Testar **todas** as telas do sistema.
3. Se tudo funcionar, a pasta `lib/bootstrap/` pode ser
   removida do projeto (contém apenas BS4).

**Risco:** Médio — pode haver classes BS4 usadas em
arquivos `.css` ou geradas dinamicamente por C# que não
foram detectadas nesta análise estática.

### Etapa 6 — Checklist de Testes Visuais por Tela

Após cada etapa de migração, verificar visualmente:

| Tela                          | Itens a Verificar                  |
|-------------------------------|------------------------------------|
| Login                         | Formulário, botão, espaçamento     |
| Dashboard (_Layout)           | Navbar, sidebar, dropdown, cards   |
| Requisitar                    | Formulário, modais, cupom, tabs    |
| Pacientes (CRUD)              | Grid, modal consulta, formulários  |
| Médicos (CRUD)                | Grid, modal consulta, formulários  |
| Instituições (CRUD)           | Grid, modal consulta, formulários  |
| Postos (CRUD)                 | Grid, modal consulta, formulários  |
| Classe de Exames (CRUD)       | Formulários, upload assinaturas    |
| Plano de Exames (CRUD)        | Grid, modal, formulários           |
| Plano de Exames Itens (CRUD)  | Grid, modal, formulários           |
| Senhas/Usuários               | Formulários, tooltips, ações grid  |
| Configurações                 | Formulário completo                |
| Mensagens (Error, Aviso, etc) | Modais de erro e mensagem          |
| Senha Esquecida / Resetar     | Formulário, modal                  |
| Cookie Consent                | Alert dismissível                  |

**Critérios de aceite por tela:**
- [ ] Espaçamento entre campos mantido
- [ ] Botões de fechar modal funcionando
- [ ] Tooltips aparecendo corretamente
- [ ] Modais abrindo e fechando
- [ ] Formulários com layout correto
- [ ] Nenhum erro no console do browser

---

## Resumo Executivo

| Categoria                    | Qtd Total | Risco Geral |
|------------------------------|-----------|-------------|
| `form-group`                 |       282 | Médio       |
| `class="close"` + `data-dismiss` |  20 | Crítico     |
| `data-toggle="tooltip"`     |         7 | Médio       |
| jQuery `.modal()`            |         5 | Crítico     |
| jQuery `.tooltip()`          |         4 | Alto        |
| `ml-*` (margin-left)        |         3 | Baixo       |
| `text-left`                  |         3 | Baixo       |
| `font-weight-normal`        |         3 | Baixo       |
| `btn-block`                  |         2 | Baixo       |
| `control-label`             |         9 | Baixo       |
| **Total de alterações**      |   **338** |             |

**Estimativa total de migração:** ~5 horas de desenvolvimento
+ ~2 horas de testes visuais.

**Recomendação:** Executar as etapas na ordem proposta,
testando após cada etapa. Manter o BS4 CSS carregado até
que todas as substituições estejam concluídas e validadas.

---

> **Nota sobre divergência de versões:** O CSS em
> `styles.css` é Bootstrap **5.2.3** enquanto o JS em
> `bootstrap.bundle.min.js` é Bootstrap **5.0.2**. Após a
> migração, considerar alinhar ambos para a mesma versão
> (preferencialmente 5.2.3 ou superior) para evitar
> incompatibilidades entre CSS e JS.
