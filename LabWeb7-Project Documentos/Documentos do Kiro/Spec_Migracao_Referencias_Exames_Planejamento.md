# SPEC — Migração de Arquivos de Referência de Exames para Banco de Dados

## Status: PLANEJAMENTO v3 — Aguardando validação final

---

## 1. Resumo Executivo

O sistema LabWeb7 utiliza atualmente **45 arquivos .DOC** (formato RTF/Word)
na pasta `LabWebMvc.MVC/Laudos/` para armazenar textos técnicos de referência
(método, interpretação, valores de referência) impressos nos laudos PDF.

Esta spec planeja a migração para uma tabela PostgreSQL dedicada com cache
em memória via `IMemoryCache`. O conteúdo será armazenado preservando 100%
da formatação original do documento (fontes, negrito, itálico, espaçamentos,
imagens).

---

## 2. Decisões Confirmadas

| Item | Decisão |
|------|---------|
| Tabela | Nova tabela dedicada `ExameReferencia` (Opção A) |
| Cache | `IMemoryCache` com Dictionary em memória |
| Formato do conteúdo | Binário original (BYTEA) — preserva 100% da formatação |
| Renderização no PDF | wkhtmltopdf (LGPLv3 — 100% gratuito, já no projeto) |
| Interface de edição | Upload Word + Editor Quill.js (BSD — 100% gratuito) |
| Expiração do cache | SEM expiração por tempo — atualiza apenas no login |
| Atualização do cache | Ao logar: recarregar tudo. Ao editar: avisar que precisa relogar |
| Documento não encontrado | Silencioso — não imprime conteúdo, sem alerta |
| Documento duplicado | Imprimir ambos em sequência, por data de criação |
| Interface de edição | A definir (dúvida abaixo) |

---

## 3. Estrutura da Nova Tabela (PostgreSQL)

```sql
CREATE TABLE IF NOT EXISTS "ExameReferencia" (
    "Id"                SERIAL          NOT NULL,
    "ContaExame"        VARCHAR(11)     NOT NULL,
    "TabelaExamesId"    INT             NOT NULL,
    "ConteudoBinario"   BYTEA           NOT NULL,
    "FormatoOrigem"     VARCHAR(10)     NOT NULL DEFAULT 'RTF',
    "AlinhaLaudo"       INT             NOT NULL DEFAULT 0,
    "DataCriacao"       TIMESTAMPTZ     NOT NULL,
    "DataAlteracao"     TIMESTAMPTZ     NOT NULL,
    "UsuarioAlteracao"  VARCHAR(100)    NOT NULL,
    "Versao"            INT             NOT NULL DEFAULT 1,
    CONSTRAINT "iExameReferencia1" PRIMARY KEY ("Id"),
    CONSTRAINT "iExameReferencia_TabelaExames" FOREIGN KEY ("TabelaExamesId")
        REFERENCES "TabelaExames"("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "iExameReferencia2"
    ON "ExameReferencia"("ContaExame", "TabelaExamesId");
CREATE INDEX IF NOT EXISTS "iExameReferencia3"
    ON "ExameReferencia"("ContaExame");
```

**Justificativa dos campos:**

| Campo | Tipo | Justificativa |
|-------|------|---------------|
| `Id` | SERIAL PK | Identificador único |
| `ContaExame` | VARCHAR(11) | Código do exame (chave de busca, ex: 11030010018) |
| `TabelaExamesId` | INT FK | Vínculo com tabela de preços |
| `ConteudoBinario` | BYTEA | Documento completo em binário (preserva formatação) |
| `FormatoOrigem` | VARCHAR(10) | Tipo do arquivo: 'RTF', 'DOCX', 'HTML' |
| `AlinhaLaudo` | INT | Alinhamento na impressão (0=direita, 1=esquerda) |
| `DataCriacao` | TIMESTAMPTZ | Data de criação do registro |
| `DataAlteracao` | TIMESTAMPTZ | Última alteração |
| `UsuarioAlteracao` | VARCHAR(100) | Auditoria |
| `Versao` | INT | Concorrência otimista |

**Nota:** A constraint UNIQUE foi removida para permitir documentos
duplicados por `ContaExame + TabelaExamesId` (caso exista mais de um,
imprime ambos por ordem de `DataCriacao`).

---

## 4. Armazenamento — Binário Original (BYTEA)

### Decisão

O conteúdo do documento será armazenado como **binário completo** (BYTEA):
- Arquivos .DOC/RTF existentes: armazenados como estão (bytes do arquivo)
- Novos uploads .DOCX: armazenados como bytes do arquivo
- Conteúdo criado via Quill.js: armazenado como HTML (bytes UTF-8)

### Vantagens

- Fidelidade 100% — nenhuma conversão que possa perder formatação
- Suporta imagens embutidas no documento
- Suporta qualquer formatação que o Word aplicar
- Reversível — pode-se extrair o arquivo original a qualquer momento

### Renderização no PDF (wkhtmltopdf)

Na hora da impressão do laudo:
1. Recuperar binário do cache
2. Se `FormatoOrigem = 'RTF'`: salvar em arquivo temp, converter para HTML via
   regex existente (strip RTF), passar para wkhtmltopdf
3. Se `FormatoOrigem = 'HTML'`: passar diretamente para wkhtmltopdf
4. Se `FormatoOrigem = 'DOCX'`: salvar em arquivo temp, converter via
   DocumentFormat.OpenXml → HTML → wkhtmltopdf
5. wkhtmltopdf gera imagem PNG do trecho formatado
6. Inserir imagem no PDF via `XImage` no espaço dedicado ao item de exame

**Fallback para RTF simples (comportamento atual):**
Para os .DOC existentes do Delphi (que são RTF simples sem imagens),
manter o strip RTF + renderização por texto posicionado (como já funciona).
Isso garante compatibilidade retroativa sem necessidade de wkhtmltopdf
para os documentos antigos.

---

## 5. Estratégia de Cache

### Fluxo

```
Login do usuário
    → Carregar TODOS os ExameReferencia do banco do cliente
    → Montar Dictionary<string, List<byte[]>> (chave = ContaExame)
    → Armazenar em IMemoryCache SEM expiração por tempo
    → Cache permanece vivo até o app pool reciclar ou novo login

Impressão de laudo
    → cache.TryGetValue("ExameRef", out dict)
    → dict.TryGetValue(contaExame, out listaDocumentos)
    → Se encontrou: renderizar cada documento na ordem de DataCriacao
    → Se não encontrou ou vazio: silencioso, não imprime nada

Edição de referência (tela administrativa)
    → Salvar no banco
    → Exibir mensagem: "Documento atualizado. Para que a alteração
      reflita na impressão, efetue login novamente."
    → NÃO atualizar o cache em tempo real
```

### Política de Cache

- **Sem SlidingExpiration, sem AbsoluteExpiration** — cache permanece indefinidamente
- Cache carregado no momento do **login** do usuário
- Ao editar um documento: sistema exibe mensagem ao usuário:
  *"Documento atualizado com sucesso. Para que a alteração reflita na impressão
  de laudos, efetue login novamente."*
- **NÃO atualizar o cache em tempo real** — garante estabilidade durante a sessão
- Cache invalidado automaticamente a cada novo login (recarrega tudo do banco)
- Se o app pool reciclar: cache é perdido e recarregado no próximo login

---

## 6. Regra de Documento Duplicado

Se existirem dois registros com o mesmo `ContaExame + TabelaExamesId`:
- Imprimir ambos, um após o outro
- Ordem: `ORDER BY "DataCriacao" ASC` (mais antigo primeiro)
- Cada documento ocupa seu próprio espaço no laudo

---

## 7. Regra de Documento Não Encontrado / Vazio

- Se não existir registro para o `ContaExame`: **silencioso**, não imprime nada
- Se o campo `ConteudoBinario` estiver vazio/null: **silencioso**, não imprime nada
- Sem alertas, sem mensagens, sem log de erro
- O laudo continua normalmente com os demais itens

---

## 8. Importação dos Arquivos Existentes

### Rotina planejada

1. Listar todos os `.DOC` na pasta `Laudos/`
2. Para cada arquivo:
   a. Extrair `ContaExame` do nome (sem extensão)
   b. Ler conteúdo binário completo (preservar RTF original)
   c. Buscar `TabelaExamesId` via `PlanoExames` onde `ContaExame` = nome
   d. Gravar na tabela `ExameReferencia`
3. `FormatoOrigem = 'RTF'`
4. `DataCriacao = DataAlteracao = NOW()`, `UsuarioAlteracao = 'IMPORTACAO'`

### Proteções

- Se já existe registro com mesmo `ContaExame + TabelaExamesId`: **não sobrescrever**
- Permitir reexecução segura (idempotente)
- Log: quantos arquivos lidos, importados, ignorados (já existentes)

---

## 9. Interface de Edição — Upload Word + Quill.js

### Funcionalidades da tela administrativa

- **Upload de arquivo Word (.DOC/.DOCX/.RTF):**
  - O usuário edita no Microsoft Word (ou LibreOffice)
  - Faz upload via formulário
  - Sistema armazena o binário completo
  - `FormatoOrigem = 'DOCX'` ou `'RTF'`

- **Editor inline Quill.js (BSD — 100% gratuito):**
  - Para edições rápidas sem necessidade de Word
  - WYSIWYG no browser: bold, itálico, fontes, imagens, listas
  - Ao salvar: armazena HTML gerado pelo Quill
  - `FormatoOrigem = 'HTML'`

- **O usuário escolhe** a forma de edição na tela:
  - Aba "Upload" → arrasta ou seleciona arquivo Word
  - Aba "Editor" → edição inline com Quill.js

### Pesquisa e navegação

- Pesquisa por `ContaExame` (código do exame)
- Pesquisa por texto no conteúdo (busca full-text)
- Grid DataTables com: Código, Descrição (do PlanoExames), Formato, Data, Usuário
- Ao clicar: abre para visualização/edição

### Mensagem após salvar

Ao salvar (upload ou editor):
> "Documento atualizado com sucesso. Para que a alteração reflita na
> impressão de laudos, efetue login novamente."

---

## 10. Ordem de Implementação

| Etapa | Descrição | Complexidade |
|:-----:|-----------|:------------:|
| 1 | Criar DDL da tabela `ExameReferencia` | Baixa |
| 2 | Criar entidade EF Core + mapeamento no DbContext | Baixa |
| 3 | Adicionar `services.AddMemoryCache()` no Startup | Baixa |
| 4 | Criar interface + serviço `IExameReferenciaCache` | Média |
| 5 | Implementar carregamento do cache no login | Média |
| 6 | Implementar rotina de importação dos .DOC existentes | Média |
| 7 | Executar importação no banco de desenvolvimento | Baixa |
| 8 | Refatorar `GeradorPdfResultado` para usar cache | Média |
| 9 | Testar impressão (comportamento idêntico) | Baixa |
| 10 | Criar tela administrativa (pesquisa + upload) | Média-Alta |
| 11 | Remover leitura de disco (após validação completa) | Baixa |

---

## 11. Riscos e Mitigações

| Risco | Mitigação |
|-------|-----------|
| Formatação perdida na conversão RTF→texto | Armazenar binário original + converter apenas na renderização |
| Imagens no documento excederem tamanho razoável | Limitar upload a 500KB por documento |
| Cache consumindo muita memória | ~200 docs × ~5KB = ~1MB (insignificante) |
| Usuário edita e não vê resultado imediato | Mensagem clara sobre necessidade de relogar |
| Rollback necessário | Arquivos .DOC mantidos na pasta como backup |

---

## 12. Rollback

- Arquivos .DOC **não serão excluídos** da pasta durante a migração
- O `GeradorPdfResultado` pode manter fallback: se cache vazio → tentar disco
- Tabela `ExameReferencia` pode ser truncada sem impacto em outras tabelas
- Reverter `Startup.cs` para remover `AddMemoryCache()` se necessário

---

## 13. Confirmação

- ✅ Nenhum arquivo do projeto foi modificado nesta spec
- ✅ Nenhum código foi alterado
- ✅ Nenhum commit foi gerado
- ✅ Documento é exclusivamente um plano técnico

---

## 14. DECISÕES FINAIS (Dúvidas Resolvidas)

### Formato de armazenamento — BINÁRIO ORIGINAL (BYTEA)

- O conteúdo do documento Word/RTF será armazenado como binário (BYTEA)
- Preserva 100% da formatação: fontes, bold, itálico, imagens, espaçamentos
- Na importação dos .DOC existentes: armazena o arquivo RTF completo em binário
- Para novos documentos criados via editor web: armazena o HTML gerado pelo Quill.js

O campo `FormatoOrigem` indica o tipo: `'RTF'` (importação), `'HTML'` (editor web),
`'DOCX'` (upload Word).

### Renderização no PDF — WKHTMLTOPDF

- **Licença:** LGPLv3 — 100% gratuita, uso comercial sem restrições ✅
- **Já disponível no projeto:** `BLL/WkConverterPdf.cs` + executável instalado
- **Fluxo:** Conteúdo do documento (RTF/HTML) → gerar HTML temporário →
  wkhtmltopdf converte para imagem/PDF → incorporar no laudo como XImage
- **Fidelidade:** Alta — renderiza fontes, formatação, imagens exatamente como o Word

### Interface de edição — UPLOAD WORD + EDITOR QUILL.JS

- **Upload:** O usuário edita no Word e faz upload do .DOC/.DOCX. Sistema armazena
  binário original. Fidelidade máxima na impressão.
- **Editor inline:** Quill.js (licença BSD — 100% gratuito, uso comercial livre ✅)
  para edição rápida sem necessidade de Word instalado.
- **O usuário escolhe:** upload de arquivo OU edição inline no browser.
- **TinyMCE descartado:** licença GPLv2+ exige código-fonte aberto ou licença
  comercial paga — incompatível com software proprietário.

### Nota sobre licenças confirmadas

| Ferramenta | Licença | Uso comercial gratuito |
|-----------|---------|:----------------------:|
| wkhtmltopdf | LGPLv3 | ✅ Sim |
| Quill.js | BSD | ✅ Sim |
| TinyMCE | GPLv2+ | ❌ Não (exige GPL ou compra) |
| PdfSharpCore | MIT | ✅ Sim |

---

## 15. Confirmação Final

- ✅ Nenhum arquivo do projeto foi modificado nesta spec
- ✅ Nenhum código foi alterado
- ✅ Nenhum commit foi gerado
- ✅ Documento é exclusivamente um plano técnico
- ✅ Todas as dúvidas foram resolvidas
- ✅ Licenças validadas: wkhtmltopdf (LGPLv3) e Quill.js (BSD) — ambas gratuitas

**Aguardando validação para iniciar implementação.**
