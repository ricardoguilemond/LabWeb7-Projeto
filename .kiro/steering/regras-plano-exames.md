---
inclusion: manual
description: Regras de negócio do Plano de Exames e Folha de Exames
---

# Steering — Regras de Negócio: Plano de Exames

## Contexto

Na tela de Folha de Exames, os itens exibidos são filtrados com base
apenas no **Plano de Exames SUS**, que funciona como um modelo
replicado para os demais planos de exames de cada Instituição.

## Regras do Modelo SUS

- Toda rotina de alteração/edição de itens do plano, ou de exclusão
  do plano, deve sempre considerar apenas o **Plano de Exames modelo
  (SUS)**.
- O campo `ExameId = 1` define os itens que pertencem ao SUS (modelo).
  No código, usar `(int)IdPadrao.SUS` para referenciar.
- Ao modificar ou excluir um item no modelo SUS, essa
  modificação/exclusão deve ser aplicada automaticamente em todos
  os planos das demais Instituições.

## Regras da ContaExame

- A `ContaExame` é um código de 11 dígitos com a estrutura:
  `XX` (11=crédito, 12=débito — fixo em 11, nunca utilizado
  neste sentido) + `XX` (folha) + `XXX` (conta principal) +
  `XXXX` (item)
- A validação da `ContaExame` do modelo é usada para verificar
  as FKs nas tabelas relacionadas.
- A replicação e validação devem considerar os **7 primeiros
  dígitos** da `ContaExame` (tipo + folha + conta principal),
  que formam o prefixo hierárquico. No código, usar
  `ContaExame.Substring(0, 7)` com `StartsWith` para identificar
  a conta principal e todos os seus itens filhos.
- Se esse prefixo de 7 dígitos existir em qualquer tabela
  relacionada (`ItensExamesRealizados`, `ItensExamesRealizadosAM`,
  `Requisitar`), o item do exame deve ser considerado válido
  em todas as Instituições.
- Conta principal: termina em `0000` (ex: `11.01.001.0000`)
- Conta item: últimos 4 dígitos > 0 (ex: `11.01.001.0001`)
- Conta folha: é a segunda dezena na formação do código que vai de 01 a 99 (aqui representada por 01) = `11.01.000.0000`

## Regras de Exclusão

- Não excluir um item do plano se a `ContaExame` existir em
  `ItensExamesRealizados`, `ItensExamesRealizadosAM` ou
  `Requisitar`.
- Se for conta principal (termina em `0000`), verificar também
  todos os itens filhos (mesmo prefixo de 7 dígitos), sempre
  observando a folha de exame.
- A exclusão no modelo SUS exclui automaticamente o mesmo item
  em todas as tabelas de preços das Instituições.

### Exclusão de Folha de Exame

- Ao excluir uma folha de exame (ex: `11.01.000.0000`), todas
  as suas contas principais e itens serão excluídos em cascata.
- A exclusão é bloqueada se **qualquer item** da folha estiver
  relacionado a exames já realizados (`ItensExamesRealizados`,
  `ItensExamesRealizadosAM`) ou requisições (`Requisitar`).
- Basta um único item vinculado para impedir a exclusão da
  folha inteira.

### Exclusão de Conta Principal

- Ao excluir uma conta principal (ex: `11.01.001.0000`), todos
  os seus itens filhos (ex: `11.01.001.0001`, `11.01.001.0002`)
  serão excluídos junto.
- A exclusão é bloqueada se qualquer item filho estiver
  relacionado a exames realizados ou requisições.

## Regras de Inclusão

- Ao incluir uma nova folha, conta principal ou item, o sistema
  deve verificar se existe um código **vago** (gap) na sequência
  antes de gerar um novo código.
- Exemplo: se existem folhas 01, 02 e 04, a próxima inclusão
  deve reutilizar o código 03 (vago) em vez de criar o 05.
- Isso se aplica tanto ao `ExameId` (Id da folha na tabela
  `ClasseExames`) quanto à `ContaExame` (código sequencial
  dentro do plano).
- O objetivo é evitar lacunas na numeração e manter a sequência
  compacta.

## Regras por Instituição

- Cada Instituição possui seu próprio `TabelaExamesId`, que serve
  como filtro para leitura da tabela correta de cada Instituição.
- A tabela `TabelaExames` contém os Ids relacionados ao
  `TabelaExamesId` do `PlanoExames`, bem como os nomes das
  Instituições.
- A correspondência entre modelo SUS e Instituição é feita pela
  `ContaExame` (mesma conta em diferentes `TabelaExamesId`).

## Tratamento de Preços/Valores

- Os campos `ValorCusto`, `ValorItem`, `TabelaCH`, `QCH`, `ICH`
  e outros relacionados a valores possuem **dois cenários** de
  edição:

### Cenário 1 — Preço individual por Instituição
- Edição inline no grid da Tabela de Preços (`SalvarItemGrid`)
- Salva apenas o registro específico (por Id)
- Cada Instituição mantém seus próprios valores
- Não afeta outras Instituições

### Cenário 2 — Preço em massa para todas as Instituições
- Tela de alteração completa (`AlterarPlanoExamesItens`)
- Ao salvar (`SalvarAlteracaoPlanoExamesItens`), replica
  `ValorCusto` e `ValorItem` para todas as Instituições
  que possuem a mesma `ContaExame`
- Útil para definir um preço base igual em todas as tabelas
- Campos estruturais (Descricao, Etiqueta, etc.) também são
  replicados neste cenário

### Regra geral
- A tela de Tabela de Preços (`PlanoExamesItens`) permite
  ambos os cenários
- O grid usa o Cenário 1 (individual)
- O botão "Alterar" do grid deve usar o Cenário 2 (massa)
- Nenhum dos cenários permite exclusão de registros — apenas
  edição de valores

## Resumo

Modificar o modelo SUS (`ExameId = 1`) significa modificar todos
os planos derivados. A verificação da `ContaExame` deve ser feita
pelos 7 últimos dígitos, e se houver correspondência em qualquer
tabela relacionada, a operação é válida para todas as Instituições.
O campo `TabelaExamesId` garante o filtro correto por Instituição,
e os preços/valores são sempre tratados de forma individual.
