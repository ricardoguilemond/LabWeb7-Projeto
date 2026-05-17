# Diagnóstico — DateTime e timestamptz no LabWeb7

**Data:** 03/05/2026
**Autor:** Kiro
**Escopo:** Análise de impacto das alterações de DateTime/timestamptz
realizadas pelo Qoder nos dias 01-02/05/2026

---

## 1. Diagnóstico Geral da Abordagem

### Estratégia adotada pelo Qoder

O Qoder implementou uma migração de `timestamp without time zone`
para `timestamptz` (timestamp with time zone) no PostgreSQL, com
a seguinte arquitetura:

- **Fonte canônica:** `TempoServidorPostgreSQL.ObterDataHoraUtc()`
  via `SELECT NOW()` → retorna UTC
- **Persistência:** UTC no banco (colunas `timestamptz`)
- **Exibição:** Conversão UTC → America/Sao_Paulo na camada de
  apresentação
- **Fallback:** `DateTime.UtcNow` quando o banco está inacessível
- **Métodos legacy:** Mantidos para compatibilidade, delegam
  internamente para UTC + conversão

### Avaliação

A estratégia é **tecnicamente correta** e segue boas práticas
para aplicações que precisam de consistência temporal. O padrão
"armazene UTC, exiba local" é o recomendado pela documentação
do PostgreSQL e do Npgsql.

**Porém**, a migração foi feita em múltiplas camadas
simultaneamente (modelo, controller, queries, serviços), o que
aumenta o risco de inconsistências entre camadas.

---

## 2. Riscos Técnicos Encontrados

### 2.1 CRÍTICO — Conflito entre Kiro e Qoder no RequisitarController

O `RequisitarController` foi alterado por **ambos** (Kiro e Qoder)
nos mesmos dias. O Kiro adicionou lógica de edição/exclusão de
requisições que usa filtros de data, enquanto o Qoder migrou
esses filtros para UTC.

**Risco:** As alterações do Kiro no `SalvarRequisicao` (exclusão
de itens anteriores) usam `ObterRangeDiaUtc()` para filtrar, mas
foram escritas originalmente com `DateTimeKind.Unspecified`. A
migração do Qoder pode ter alterado o comportamento dos filtros
sem que o Kiro soubesse.

**Evidência:** O bloco de exclusão no `SalvarRequisicao` (linhas
~580-630) usa `_geralController.ObterRangeDiaUtc()` — isso foi
migrado pelo Qoder. Se o range UTC não corresponder exatamente
ao dia local de Brasília, itens podem não ser encontrados para
exclusão.

### 2.2 ALTO — `ToFormataData()` é um ponto frágil

O método de extensão `ToFormataData()` em `UtilsBase.cs` faz
`Convert.ToDateTime(data)` — retorna `DateTime` com
`Kind=Unspecified`. Esse método é usado em vários pontos do
sistema para converter strings de data.

**Risco:** Se o resultado de `ToFormataData()` for passado
diretamente a uma query EF Core com coluna `timestamptz`, o
Npgsql 8.x lançará `InvalidOperationException` porque
`Kind=Unspecified` não é aceito em `timestamptz`.

**Evidência:** O `ObterDataHoraServidor()` (método legacy)
retorna string, e vários pontos do código fazem
`.ObterDataHoraServidor().ToFormataData()` — o resultado é
`Kind=Unspecified`.

### 2.3 ALTO — Métodos legacy ainda em uso

O `GeralController` mantém métodos legacy que retornam
`Kind=Unspecified`:

- `ObterDataHoraLocal()` → retorna `Kind=Unspecified`
- `ObterDataHoraServidor()` → retorna string (via legacy
  `TempoServidorPostgreSQL.ObterDataHoraServidor()`)

Esses métodos são usados em controllers que ainda não foram
migrados para UTC. Se um controller usa `ObterDataHoraLocal()`
para gravar em uma coluna que agora é `timestamptz`, o Npgsql
rejeitará o valor.

### 2.4 MÉDIO — `DateTime.Today` e `DateTime.Now` em IntegracaoUtils

O arquivo `IntegracaoUtils.cs` usa `DateTime.Today` para
calcular períodos de extração:

```csharp
DateTime dataInicio = DateTime.Today;
DateTime dataFim = dataInicio;
```

**Risco:** `DateTime.Today` retorna a data do servidor de
aplicação (não do PostgreSQL), com `Kind=Local`. Se o servidor
estiver em fuso diferente de Brasília, os períodos calculados
serão incorretos.

### 2.5 MÉDIO — Conversão de Nascimento para UTC

O `RequisitarController` converte `Nascimento` para UTC:

```csharp
paciente.Nascimento = _geralController
    .ConverterLocalParaUtc(vm.VmPacientes.Nascimento);
```

**Risco:** Data de nascimento é um dado estático (não depende
de fuso horário). Converter para UTC pode causar que um paciente
nascido em 01/01/2000 00:00 Brasília seja gravado como
31/12/1999 03:00 UTC — alterando a data visível.

**Recomendação conceitual:** Datas de nascimento, DUM e
DataEntradaBrasil deveriam ser `timestamp without time zone`
(ou `date`), não `timestamptz`. A conversão UTC é desnecessária
e potencialmente incorreta para esses campos.

### 2.6 BAIXO — Fallback para `DateTime.UtcNow`

O `TempoServidorPostgreSQL` usa `DateTime.UtcNow` como fallback
quando o banco está inacessível. Se o relógio do servidor de
aplicação estiver dessincronizado, os registros gravados no
fallback terão timestamps diferentes dos gravados via PostgreSQL.

---

## 3. Inconsistências de Padrão

### 3.1 Dois padrões de obtenção de data coexistem

| Padrão | Método | Retorno | Usado em |
|--------|--------|---------|----------|
| Novo (UTC) | `ObterDataHoraUtc()` | `Kind=Utc` | RequisitarController |
| Legacy | `ObterDataHoraServidor()` | `string` | Outros controllers |

**Risco:** Controllers não migrados continuam usando o padrão
legacy. Se as colunas do banco foram migradas para `timestamptz`,
esses controllers falharão ao gravar.

### 3.2 Dois padrões de filtro de data coexistem

| Padrão | Método | Usado em |
|--------|--------|----------|
| Novo (UTC range) | `ObterRangeDiaUtc()` | RequisitarController |
| Novo (conversão) | `ConverterDataLocalParaRangeUtc()` | RequisitarController |
| Legacy | `DataIni >= hoje && DataIni <= hojeFim` | Outros controllers |

### 3.3 `ExportacaoPacientes` usa padrão misto

O arquivo converte datas locais para UTC manualmente:

```csharp
var offset1 = tzExp.GetUtcOffset(primeiraData);
primeiraData = new DateTimeOffset(primeiraData, offset1)
    .UtcDateTime;
```

Isso é correto, mas usa um padrão diferente do
`ConverterLocalParaUtc()` do `GeralController`. Dois padrões
para a mesma operação aumentam o risco de divergência.

---

## 4. Possíveis Bugs Latentes

### 4.1 Horário de verão

O método `ConverterLocalParaUtc()` usa
`tz.GetUtcOffset(dataLocal)` para calcular o offset. Durante
a transição de horário de verão, o offset muda de -03:00 para
-02:00. Se uma data cair exatamente na hora de transição, o
offset pode ser ambíguo.

**Nota:** O Brasil suspendeu o horário de verão em 2019, então
esse risco é teórico no momento. Mas se for reativado, o código
precisará de tratamento para datas ambíguas.

### 4.2 Filtro de "hoje" pode excluir registros do início/fim do dia

O `ObterRangeDiaUtc()` calcula o range UTC do dia atual de
Brasília. Se um registro foi gravado às 23:59 de Brasília
(02:59 UTC do dia seguinte), ele cairá fora do range do dia
atual em UTC.

**Verificação necessária:** Confirmar que o range UTC inclui
corretamente as 24 horas do dia de Brasília (03:00 UTC até
02:59:59 UTC do dia seguinte).

### 4.3 `CupomRequisicao` usa `.Date` em vez de range UTC

O `CupomRequisicao` (após minha correção) filtra por:

```csharp
r.DataIni.Date == dataConsulta.Date
```

Se `DataIni` agora é `timestamptz` (UTC), o `.Date` extrai a
data UTC — que pode ser diferente da data local de Brasília.
Um registro gravado às 22:00 de Brasília (01:00 UTC do dia
seguinte) terá `.Date` do dia seguinte em UTC.

**Nota:** Verifiquei e o Qoder já corrigiu isso para usar
`ConverterDataLocalParaRangeUtc()` em alguns endpoints, mas
o `CupomRequisicao` pode não ter sido atualizado.

---

## 5. Compatibilidade com PostgreSQL (timestamptz)

### 5.1 Npgsql 8.x e DateTimeKind

O Npgsql 8.x (usado no projeto, versão 8.0.4) tem regras
estritas:

- `timestamptz` aceita apenas `Kind=Utc` ou `DateTimeOffset`
- `timestamp` (without tz) aceita apenas `Kind=Unspecified`
  ou `Kind=Local`
- `Kind=Unspecified` em `timestamptz` → **exceção**

O `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior")`
pode relaxar essas regras, mas não foi encontrado no projeto.
Isso significa que o projeto está usando o comportamento
**estrito** do Npgsql 8.x.

### 5.2 Mapeamento EF Core

O `db.cs` (OnModelCreating) mapeia `ControleConcorrencia.DataHora`
como `timestamp with time zone`. Outros modelos podem não ter
mapeamento explícito — o Npgsql infere o tipo da coluna do banco.

**Risco:** Se uma coluna no banco é `timestamptz` mas o modelo
C# não tem mapeamento explícito, o Npgsql infere e aplica as
regras estritas. Qualquer `DateTime` com `Kind=Unspecified`
passado a essa coluna causará exceção.

---

## 6. Recomendações Conceituais

### 6.1 Padronizar um único método de obtenção de data

Eliminar gradualmente os métodos legacy e usar exclusivamente
`ObterDataHoraUtc()` para persistência e `ObterRangeDiaUtc()`
/ `ConverterDataLocalParaRangeUtc()` para filtros.

### 6.2 Separar campos de data pura de campos de timestamp

Campos como `Nascimento`, `DUM`, `DataEntradaBrasil` são datas
puras (sem componente de hora/fuso). Deveriam ser `date` ou
`timestamp without time zone` no PostgreSQL, não `timestamptz`.

### 6.3 Auditar todos os controllers

Verificar se todos os controllers que gravam datas já foram
migrados para o padrão UTC. Controllers não migrados que gravam
em colunas `timestamptz` falharão silenciosamente ou com exceção.

### 6.4 Criar testes de integração para filtros de data

Os filtros "registros de hoje" são críticos para a operação do
sistema. Criar testes que verifiquem o comportamento nos limites
do dia (00:00, 23:59 de Brasília) para garantir que o range UTC
está correto.

### 6.5 Documentar a estratégia de timezone

Criar um steering específico documentando:
- Qual tipo de coluna usar para cada tipo de dado
- Como converter datas do cliente para UTC
- Como filtrar por dia local em colunas UTC
- Quais métodos usar em cada camada

---

## Checklist de Validação

```
[ ] Todos os controllers que gravam datas usam ObterDataHoraUtc()?
[ ] Todos os filtros de data usam range UTC?
[ ] Campos de data pura (Nascimento, DUM) estão como timestamptz?
[ ] O CupomRequisicao usa range UTC em vez de .Date?
[ ] IntegracaoUtils usa ObterDataHoraUtc() em vez de DateTime.Today?
[ ] Nenhum controller usa ToFormataData() para gravar em timestamptz?
[ ] O fallback DateTime.UtcNow está documentado como risco?
```
