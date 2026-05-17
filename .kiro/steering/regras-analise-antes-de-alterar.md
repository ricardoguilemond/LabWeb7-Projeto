---
inclusion: always
description: Regra obrigatória de análise antes de alterações estruturais ou de múltiplos arquivos
---

# Steering — Análise Obrigatória Antes de Alteração

## Quando Aplicar

Esta regra é obrigatória sempre que a solicitação envolver:

- Múltiplos arquivos (3 ou mais)
- Frameworks (Bootstrap, jQuery, DataTables, SweetAlert2, etc.)
- Refatoração estrutural (mover, renomear, reorganizar)
- Remoção de código legado
- Alteração de layout ou comportamento visual
- Troca de versão de biblioteca
- Mudança em arquivos globais (`_Layout.cshtml`, `site.js`,
  `site.css`, `mydatatables.js`, `styles.css`)

## Etapas Obrigatórias (antes de qualquer modificação)

### 1. Inventário de dependências

- Identificar todos os arquivos afetados pela alteração.
- Mapear referências diretas (imports, links, scripts) e
  indiretas (classes CSS usadas, funções JS chamadas).
- Detectar uso de APIs, classes e atributos relevantes
  via busca no código (`grepSearch`), não por suposição.
- Verificar versões exatas dos arquivos envolvidos lendo
  os primeiros bytes/linhas do arquivo.

### 2. Classificação de risco

- Avaliar impacto potencial por item identificado.
- Ordenar do pior impacto para o menor impacto.
- Classificar cada item como:
  - **Crítico** — quebra funcionalidade visível ao usuário
  - **Alto** — altera comportamento mas pode não ser
    imediatamente visível
  - **Médio** — afeta estilo ou layout sem quebrar função
  - **Baixo** — alteração cosmética ou de organização

### 3. Mapeamento de equivalência

- Para cada item legado, indicar a substituição compatível.
- Indicar mudanças obrigatórias (ex: `ml-3` → `ms-3`).
- Confirmar que o equivalente existe no código atual
  (não assumir — verificar no arquivo CSS/JS de destino).

### 4. Plano de execução

- Definir etapas incrementais (do menor risco ao maior).
- Garantir que cada etapa seja reversível (rollback via
  Git ou desfazer manual).
- Evitar mudanças em massa sem validação intermediária.
- Executar build após cada etapa.
- Solicitar teste visual do usuário quando aplicável.

### 5. Restrição de execução

- **Não modificar arquivos na etapa de análise.**
- Só executar mudanças após validação explícita do usuário.
- Se houver dúvida sobre o impacto, perguntar antes de agir.
- Nunca assumir que dois arquivos com mesmo nome em pastas
  diferentes são equivalentes — verificar versão e conteúdo.

### 6. Decomposição automática

- Dividir a análise em sub-tarefas quando o escopo for
  grande (ex: 20+ arquivos, múltiplos frameworks).
- Utilizar sub-agentes para cobertura completa do código.
- Consolidar os resultados em um único relatório.

## Saída Esperada

A etapa de análise deve produzir:

1. **Relatório estruturado** com inventário, riscos e
   equivalências (documento em `Documentos do Kiro/`).
2. **Plano de migração** com etapas numeradas, estimativa
   de esforço e checklist de testes.
3. **Nenhuma alteração de código** nesta fase.

## Exceções

Esta regra **não se aplica** a:

- Correções pontuais em um único arquivo (ex: fix de typo,
  ajuste de valor CSS, correção de bug localizado).
- Adição de código novo que não altera o existente.
- Alterações solicitadas explicitamente pelo usuário com
  instrução direta de execução imediata.

## Exemplo de Violação

```
❌ ERRADO:
Usuário: "Organize o Bootstrap para ficar consistente"
Kiro: [move arquivo, muda referência no _Layout.cshtml]
→ Quebrou o menu porque não verificou as versões antes.

✅ CORRETO:
Usuário: "Organize o Bootstrap para ficar consistente"
Kiro: [analisa versões, identifica 4.3.1 vs 5.0.2,
       cria relatório, propõe plano, aguarda aprovação]
→ Nenhum arquivo alterado até validação do usuário.
```

## Protocolo de Regressão

Quando uma funcionalidade parar de funcionar após mudanças
recentes:

1. **Comparar antes vs depois** — usar `git show` ou
   `git diff` para identificar o estado anterior do código.
2. **Identificar a mudança que causou o problema** — não
   supor, verificar no histórico Git qual alteração
   introduziu a regressão.
3. **Classificar o tipo de regressão:**
   - CSS (estilo/layout quebrado)
   - JS (funcionalidade quebrada)
   - Backend (lógica/dados incorretos)
   - Referência (arquivo movido/removido)
4. **Propor a correção mínima** — reverter apenas o que
   causou o problema, sem refatorar ou reescrever código
   desnecessariamente.
5. **Focar na causa raiz** — não aplicar patches sobre
   patches. Se a segunda tentativa falhar, voltar ao
   estado original e repensar a abordagem.

### Como acionar a análise de regressão

Quando o usuário reportar que algo parou de funcionar,
sugerir o uso do protocolo com o formato abaixo:

```
EXECUTAR: ANALISE_DE_REGRESSAO
SINTOMA: [descrever o que parou de funcionar]
ANTES: [quando funcionava]
DEPOIS: [após quais alterações parou]
```

Exemplo 1 — quando sabe o antes/depois:
```
EXECUTAR: ANALISE_DE_REGRESSAO
SINTOMA: botão não abre modal
ANTES: funcionava normalmente
DEPOIS: após ajustes no layout e scripts
```

Exemplo 2 — quando sabe quais alterações foram feitas:
```
EXECUTAR: ANALISE_DE_REGRESSAO
ALTERAÇÃO RECENTE:
- mudança no _Layout.cshtml
- ajuste em script de modal
SINTOMA:
- modal não abre mais
```

Ao receber qualquer um desses formatos, executar
automaticamente as etapas 1 a 5 do protocolo acima.
