---
inclusion: always
description: Regras definitivas da importação Firebird→PostgreSQL (Carga de Dados) — encoding e ODBC
---

# Steering — Carga de Dados: Importação Firebird → PostgreSQL

## REGRA IMUTÁVEL — NÃO ALTERAR SEM AUTORIZAÇÃO EXPLÍCITA

As regras abaixo foram validadas após extensiva investigação e múltiplas
tentativas. São o resultado final que funciona corretamente para preservar
acentuação e caracteres especiais pt-BR na importação.

**Proibido alterar qualquer aspecto desta configuração sem autorização
explícita do usuário.**

## Driver de Importação de Dados: ODBC com Charset=NONE (reconexão por tabela)

A importação de dados textuais do Firebird usa **exclusivamente** o
driver ODBC Firebird via `OdbcConnection`/`OdbcCommand` com `Charset=NONE`.

### Por que ODBC com Charset=NONE

- ODBC com Charset=NONE preserva acentos CORRETAMENTE (confirmado).
- NETProvider 10.3.2 com CAST CHARACTER SET NONE **NÃO** preserva acentos
  (retorna U+FFFD — bug persistente na versão 10.3.2).
- ODBC retorna string ANSI do Windows (WIN1252) quando Charset=NONE.
- O `SanitizarStringWin1252` no TypeConverter cuida da conversão de
  chars 0x80-0x9F para os chars Unicode corretos.

### Por que reconexão por tabela

ODBC com conexão longa crashava em tabelas grandes (ExamesRealizados,
ExamesRealizadosAM). O crash era causado por:
- Schema comparison (FbConnection via NETProvider managed) pode carregar
  fbclient.dll indiretamente
- Conflito de handles entre NETProvider managed e ODBC nativo com
  conexão longa

**Solução**: cada tabela abre/fecha sua própria conexão ODBC fresca.
Se a abertura falhar, tenta reconectar UMA vez antes de marcar como erro.

### Histórico de tentativas que falharam

- NETProvider CAST CHARACTER SET NONE → retorna U+FFFD (bug 10.3.2) ❌
- Charset NONE + sem CAST (NETProvider) → driver usa ASCII da coluna ❌
- Charset NONE + CAST OCTETS → servidor transliterava ASCII→OCTETS ❌
- Charset WIN1252 → "Invalid character set specified" (fbclient 2.5.x) ❌
- Charset ISO8859_1 → "Cannot transliterate character" ❌
- GetBytes() → não funciona para VARCHAR no FbDataReader ❌
- ODBC com conexão única (sem reconexão) → crash em tabelas grandes ❌

### Configuração que funciona

Connection string ODBC DSN-less:
```
Driver={Firebird/InterBase(r) driver};
Dbname=servidor/porta:caminho_banco.FDB;
Uid=SYSDBA;
Pwd=senha;
Charset=NONE;
```

SELECT simples (SEM CAST CHARACTER SET NONE):
```sql
SELECT "Coluna1", "Coluna2", "ColunaInt", "ColunaDate"
FROM "Tabela"
```

- **Charset=NONE na conexão ODBC**: o driver retorna bytes brutos sem
  transliteração, decodificados como ANSI do Windows (WIN1252)
- **Sem CAST**: com ODBC Charset=NONE não é necessário CAST — o driver
  retorna string ANSI corretamente
- **CommandTimeout = 0**: sem limite de timeout (tabelas grandes podem
  demorar vários minutos)
- O `SanitizarStringWin1252` no TypeConverter converte chars 0x80-0x9F
  (diferença entre ISO-8859-1 e WIN1252) para os chars Unicode corretos
- Chars 0xA0-0xFF (Ç, Ã, É, Ó) são idênticos em ISO-8859-1 e WIN1252

### Referência: DSN do Delphi

O sistema Delphi legado usava o DSN "DSN FIREBIRD Lab-Web7" com:
- Character Set: ASCII
- Client: C:\Windows\System32\FBCLIENT.DLL
- Dialect: 3

Esta configuração trazia acentos corretamente. A implementação .NET
com ODBC + Charset=NONE replica o mesmo comportamento.

## Fase de Preparação (Schema + Contagem)

A fase de preparação (schema comparison e contagem de registros) MANTÉM
o uso de FbConnection (NETProvider 10.x):
- NETProvider 10.x é 100% managed (wire protocol puro, sem fbclient.dll)
- Schema comparison não lê dados textuais acentuados
- A fase de preparação termina ANTES da importação ODBC começar
- Sem conflito entre NETProvider e ODBC

## Arquitetura da Importação

| Operação             | Driver         | Charset | Motivo                    |
|----------------------|----------------|---------|---------------------------|
| Teste de conexão     | FbConnection   | NONE    | Não lê dados textuais     |
| Schema comparison    | FbConnection   | NONE    | Lê metadata, não dados    |
| Contagem registros   | FbConnection   | NONE    | COUNT(*), sem texto       |
| **Importação dados** | **OdbcConnection** | **NONE** | **Preserva acentos**  |
| Teste conexão ODBC   | OdbcConnection | NONE    | Validação do driver       |

## Proibições Absolutas

- ❌ NUNCA usar NETProvider (FbConnection) para importação de dados textuais
  (bug 10.3.2 retorna U+FFFD com CAST NONE)
- ❌ NUNCA adicionar CAST CHARACTER SET NONE no SELECT da importação ODBC
  (desnecessário — Charset=NONE na conexão é suficiente)
- ❌ NUNCA usar Charset=WIN1252 ou ISO8859_1 na conexão (causa erros)
- ❌ NUNCA remover o SanitizarStringWin1252 do TypeConverter
- ❌ NUNCA manter conexão ODBC aberta entre tabelas (causa crash)
- ❌ NUNCA usar timeout diferente de 0 no CommandTimeout ODBC
  (tabelas grandes precisam de tempo ilimitado)

## Requisitos do Ambiente

- NETProvider 10.x (FirebirdSql.Data.FirebirdClient) — apenas para
  schema/preparação (100% managed, wire protocol puro)
- Driver ODBC Firebird instalado: "Firebird/InterBase(r) driver"
- Versão do Firebird server: 2.5.x
- Colunas no banco Firebird: charset ASCII com dados WIN1252 (legado Delphi)

## Fluxo de Conversão de Encoding

```
Firebird (bytes WIN1252 em colunas ASCII)
    ↓ SELECT simples (sem CAST)
Driver ODBC Firebird (Charset=NONE) retorna string ANSI
    ↓ OdbcDataReader retorna string .NET
String .NET com chars Windows-1252 (ANSI)
    ↓ TypeConverter.ConverterParaString → SanitizarStringWin1252
String Unicode correta (Ç, Ã, É, Ó preservados)
    ↓ NpgsqlParameter
PostgreSQL (UTF-8)
```

## Data da solução: 26/07/2026

Implementado e validado pelo Kiro. Reverte de NETProvider para ODBC
após confirmação de que NETProvider 10.3.2 com CAST CHARACTER SET NONE
NÃO preserva acentos (retorna U+FFFD). ODBC com Charset=NONE +
reconexão por tabela é a solução definitiva.

## STATUS: IMPORTAÇÃO CONCLUÍDA COM ÊXITO (01/08/2026)

A Carga de Dados está completa e funcional. Nada deve ser alterado
sem pedido explícito do usuário com base em análise documentada.

## Fases Pós-Importação Automáticas

Após importar todas as tabelas, o sistema executa automaticamente:

### Fase 3 — Criação de Folhas Ausentes (PlanoExames)

- Executa após a importação de PlanoExames
- Verifica combinações ClasseExamesId + TabelaExamesId que têm
  Principais/Itens mas não têm registro de Folha (ContaExame 0000000)
- Cria automaticamente o registro de Folha com:
  - ContaExame = "11" + ClasseExamesId(2 dígitos) + "0000000"
  - Descricao = RefExame = RefItem = nome da ClasseExames
  - Demais campos numéricos = 0
- Método: `CriarFolhasAusentesPlanoExamesAsync`

### Fase 4 — Deduplicação (Pacientes e Médicos)

- Executa após todas as tabelas serem importadas
- Pacientes: agrupa por UPPER(TRIM(NomePaciente)) + Nascimento::date
- Médicos: agrupa por UPPER(TRIM(NomeMedico)) + UPPER(TRIM(CRM))
- Mantém o maior Id (mais recente), migra FKs, exclui duplicados
- Método: `DeduplicarPosImportacaoAsync`

## Pós-Processamento de PlanoExames

### ClasseExamesId (derivado da ContaExame)

O campo `PlanoExames.ClasseExamesId` (antigo `ExameId`) é derivado
automaticamente da `ContaExame` durante a importação:
- `ClasseExamesId = int(ContaExame[2:4])` (dígitos 3-4, posição 1-indexed)
- No Firebird não existe coluna separada para a folha
- O mapeamento antigo `["CodigoItem"] = "ExameId"` era INCORRETO
  (CodigoItem é o código do item, não da folha)
- Corrigido em 01/08/2026: removido do mapeamento, derivado via
  pós-processamento no loop de leitura paginada

### Renomeações Estruturais (01/08/2026)

| Tabela | Antes | Depois | Motivo |
|--------|-------|--------|--------|
| PlanoExames | ExameId | ClasseExamesId | Consistência com modelo |
| Requisitar | ExameId (removido) | — | Redundante com ClasseExamesId |

## Retry de Conexão PostgreSQL

- **Antes de cada lote**: verifica `connPgExterna.State != Open`,
  reconecta se necessário
- **Durante o lote**: se detecta `CONNECTION_LOST` (conexão caiu no
  meio do INSERT), faz rollback, reconecta, retenta o lote inteiro

## Regra de Valores no PlanoExames (Hierarquia Exclusiva)

Os campos de valor (ValorCusto, ValorItem) seguem regra de
exclusividade hierárquica na tela PlanoExamesItens:

| Se valor em... | Desabilita campos de... |
|----------------|-------------------------|
| Folha (0000000) | Todos os Principais e Itens da folha |
| Principal (CCC0000) | A Folha + Itens daquele grupo |
| Item (CCCNNNN) | A Folha + o Principal daquele grupo |

Implementado na partial `_PartialPlanoContaItem.cshtml` via atributo
`disabled` nos inputs de valor.

## Proibições Absolutas (Adicionais)

- ❌ NUNCA alterar a lógica de importação sem autorização explícita
- ❌ NUNCA remover o pós-processamento de ClasseExamesId
- ❌ NUNCA remover a criação automática de Folhas ausentes
- ❌ NUNCA remover a deduplicação pós-importação
- ❌ NUNCA restaurar o campo ExameId em PlanoExames ou Requisitar
