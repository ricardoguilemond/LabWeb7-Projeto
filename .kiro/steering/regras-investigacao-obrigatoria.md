---
inclusion: always
description: Política obrigatória de investigação baseada em código antes de qualquer análise ou implementação
---

# Steering — Investigação Obrigatória Baseada em Código

## Quando Aplicar

Esta política é obrigatória e não-opcional para qualquer
solicitação envolvendo:

- Análise técnica
- Levantamento de requisitos
- Arquitetura
- Design
- Geração de tarefas
- Investigação de bugs
- Proposta de refatoração
- Revisão de especificação

## 1. Investigação no Código (OBRIGATÓRIO)

- NUNCA inferir
- NUNCA deduzir sem evidência
- NUNCA assumir comportamento de implementação
- NUNCA adivinhar arquitetura

Toda análise DEVE ser baseada em investigação direta no
código-fonte.

Antes de responder:
- Inspecionar a implementação real
- Inspecionar classes relacionadas
- Inspecionar dependências
- Inspecionar cadeias de chamada
- Inspecionar acesso ao banco quando relevante
- Inspecionar DTOs, models, services e repositories

Se evidência não for encontrada, declarar explicitamente:
"Evidência de implementação não encontrada no código
inspecionado."

Não inventar comportamento.

## 2. Rastreabilidade

Toda investigação DEVE identificar:
- Projeto
- Arquivo
- Namespace
- Classe
- Método
- Assinatura do método
- Dependências relacionadas
- Linhas aproximadas envolvidas

Formato:
```
Projeto: LabWebMvc.MVC
Arquivo: Areas/Controllers/RequisitarController.cs
Classe: RequisitarController
Método: SalvarRequisicao(vmRequisitar vm, int registroID)
Linhas: ~570–710
Dependências:
- GeralController
- ListaAcumulativa
- ExclusaoService
```

## 3. Análise de Fluxo de Execução

Para cada método relevante, explicar:

- **Propósito:** Por que o método existe
- **Entradas:** Parâmetros, DTOs, ViewModels, Session,
  dependências de banco
- **Fluxo interno:** Passo a passo da execução
- **Intenção de negócio:** Qual regra está sendo aplicada
- **Saídas:** Retornos, redirects, exceções, efeitos
  colaterais, persistência
- **Caminhos de falha:** Cenários de erro possíveis

## 4. Revisão de Segurança (quando aplicável)

Identificar:
- Fragilidades de segurança
- Riscos de SQL injection
- Serialização insegura
- Falhas de autenticação/autorização
- Exposição de dados sensíveis
- Race conditions
- Riscos de concorrência
- Tratamento inseguro de datas/timezone
- Riscos de nullability
- Exceções engolidas

Classificar por severidade: Baixa, Média, Alta, Crítica.

## 5. Revisão de Performance (quando relevante)

Identificar oportunidades envolvendo:
- Roundtrips desnecessários ao banco
- Queries N+1
- Chamadas bloqueantes
- Sync sobre async
- Enumerações LINQ repetidas
- Alocações excessivas
- Materialização desnecessária
- Loops pesados
- Joins ineficientes
- Conversões redundantes

Explicar: ganho esperado, nível de impacto, áreas afetadas,
risco de regressão.

## 6. Governança de Bibliotecas

- NÃO sugerir introdução de novas bibliotecas
- Exceção: apenas se dependência instalada tiver
  vulnerabilidade, versão sem suporte, ou problema grave
- Ao sugerir atualização: explicar motivo, risco de
  breaking change, esforço de migração, projetos impactados

## 7. Análise Suficiente

A investigação DEVE conter detalhe técnico suficiente para
resolver o prompt, requisito, design ou implementação.

Sem resumos superficiais. A análise deve permitir
implementação com ambiguidade mínima.

## 8. Formato de Saída Estruturado

Organizar achados em tópicos distintos:

1. Resumo Executivo
2. Investigação Técnica
3. Fluxo de Execução
4. Riscos / Bugs Encontrados
5. Preocupações de Segurança
6. Oportunidades de Performance
7. Arquivos e Métodos Impactados
8. Caminhos de Resolução Recomendados
9. Risco de Regressão
10. Conclusão Técnica Final

## 9. Sem Falsa Confiança

Se a certeza for baixa: declarar incerteza explicitamente.
Nunca apresentar suposições como fatos.
Evidência > inferência.


## 10. Hook de Validação (OBRIGATÓRIO)

Antes de finalizar qualquer resposta de análise, requisitos,
design ou tarefas, validar TODOS os itens abaixo.

REPROVAR a resposta se QUALQUER item estiver ausente.

### Checklist

```
[ ] O código-fonte real foi inspecionado?
[ ] As conclusões são baseadas em evidência?
[ ] Os nomes de arquivo foram identificados?
[ ] Os nomes de classe foram identificados?
[ ] Os métodos foram identificados?
[ ] As linhas aproximadas foram identificadas?
[ ] O fluxo de execução foi explicado?
[ ] As entradas foram explicadas?
[ ] As saídas foram explicadas?
[ ] As intenções de negócio foram explicadas?
[ ] Os cenários de falha foram identificados?
[ ] Os bugs potenciais foram identificados?
[ ] As fragilidades de segurança foram analisadas?
[ ] As oportunidades de performance foram analisadas?
[ ] Novas bibliotecas desnecessárias foram evitadas?
[ ] A análise é suficiente para implementar a mudança?
[ ] Os achados estão agrupados em tópicos contextuais?
```

### Se qualquer resposta for NÃO:

- PARAR
- Continuar investigação antes de responder
- Não fornecer respostas especulativas
- Não inferir comportamento ausente
- Não fabricar detalhes de implementação


## 11. Política de Idioma (OBRIGATÓRIO)

Toda comunicação com o usuário DEVE ser em Português-Brasil
(pt-BR).

Isso inclui:
- Perguntas
- Análises
- Requisitos
- Discussões de design
- Geração de tarefas
- Explicações
- Avisos
- Planos de implementação
- Conclusões técnicas
- Resumos
- Explicações de commit
- Relatórios de bug

NUNCA fazer perguntas em inglês.

### Termos técnicos preservados em inglês

Quando necessário, manter termos técnicos no original:
- controller, repository, DTO, ViewModel
- async/await, deadlock, race condition
- nullable, UTC, middleware
- nomes de métodos, classes, namespaces
- SQL, stack traces, mensagens de exceção
- saída do compilador

### Regra

Se o usuário escreve em Português → responder SOMENTE em
Português.

Inglês permitido APENAS para:
- Código-fonte
- Identificadores
- Logs
- Saída do compilador
- Texto de exceções
- Terminologia de APIs de terceiros
