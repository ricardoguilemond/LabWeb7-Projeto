# 📚 Documentação Arquitetural - LabWeb7

**Criado por:** Qoder AI  
**Data:** 21/04/2026

---

## 📖 Visão Geral

Esta pasta contém a documentação arquitetural completa do projeto LabWeb7 (LabWebMvc.MVC), criada através de análise profunda do código-fonte, estrutura de projetos, banco de dados e padrões de desenvolvimento.

---

## 📁 Documentos Disponíveis

### 1. **analise-arquitetural-completa-labweb7.md**
**Tipo:** Documentação Completa (844 linhas)  
**Conteúdo:**
- ✅ Visão geral do projeto e stack tecnológica
- ✅ Estrutura completa da solution com 6 projetos
- ✅ Diagrama de dependências entre projetos
- ✅ Descrição detalhada de cada projeto
- ✅ Banco de dados PostgreSQL (51 tabelas categorizadas)
- ✅ DbContext db.cs features especiais
- ✅ Relacionamentos FK completos (20+ relações)
- ✅ Relacionamentos APENAS EM CÓDIGO (4 relações)
- ✅ Padrões de nomenclatura detalhados
- ✅ Padrões arquiteturais (DI, Factory, Repository, Strategy)
- ✅ Bibliotecas e dependências (20+ pacotes)
- ✅ 21 controllers e áreas funcionais
- ✅ Regras de negócio críticas
- ✅ Integrações (AWS, Azure, Google)
- ✅ Segurança e autenticação
- ✅ Logging e monitoramento
- ✅ Fluxos críticos do sistema
- ✅ Pontos de atenção e dívidas técnicas
- ✅ Recomendações de melhorias

**Uso:** Referência completa para entender toda a arquitetura do sistema.

---

### 2. **quick-reference-labweb7.md**
**Tipo:** Guia Rápido (442 linhas)  
**Conteúdo:**
- ✅ Estrutura de pastas principal
- ✅ Resumo do banco de dados
- ✅ Relacionamentos FK principais
- ✅ Relacionamentos apenas em código
- ✅ Padrões arquiteturais (código exemplo)
- ✅ Nomenclatura rápida
- ✅ Dependências principais
- ✅ Regras de negócio críticas (com exemplos de código)
- ✅ Arquivos protegidos
- ✅ Comandos úteis
- ✅ Fluxos críticos
- ✅ Troubleshooting comum

**Uso:** Consulta rápida durante desenvolvimento do dia-a-dia.

---

### 3. **diagramas-arquiteturais-labweb7.md**
**Tipo:** Diagramas Visuais (595 linhas)  
**Conteúdo:**
- ✅ 12 diagramas Mermaid
- ✅ Diagrama de dependências da solution
- ✅ Arquitetura em camadas
- ✅ Diagrama ER do banco de dados
- ✅ Fluxo de requisição de exames
- ✅ Fluxo de alteração de plano de exames (SUS)
- ✅ Fluxo de exclusão com validação de FK
- ✅ Multi-tenant (troca de conexão)
- ✅ SaveChanges com reutilização de IDs
- ✅ Hierarquia ContaExame
- ✅ Deploy (modos de execução)
- ✅ Autenticação e autorização
- ✅ Integrações (Strategy Pattern)

**Uso:** Visualização gráfica da arquitetura e fluxos.

---

### 4. **README.md** (este arquivo)
**Tipo:** Índice e Navegação  
**Conteúdo:**
- ✅ Visão geral dos documentos
- ✅ Quando usar cada documento
- ✅ Resumo das descobertas principais

---

## 🎯 Quando Usar Cada Documento

### Cenário 1: "Preciso entender como o sistema funciona"
👉 Leia: **analise-arquitetural-completa-labweb7.md**  
Seções recomendadas: 1, 2, 3, 5, 17

### Cenário 2: "Vou implementar uma feature nova"
👉 Leia: **quick-reference-labweb7.md**  
Seções recomendadas: 5, 6, 9, 10

### Cenário 3: "Preciso entender relacionamentos do banco"
👉 Leia: **diagramas-arquiteturais-labweb7.md**  
Diagramas recomendados: 3, 8, 9

### Cenário 4: "Vou alterar o Plano de Exames"
👉 Leia: **quick-reference-labweb7.md** → Seção 9 (Regras de Negócio)  
👉 Veja: **diagramas-arquiteturais-labweb7.md** → Diagrama 5

### Cenário 5: "Preciso criar um novo controller"
👉 Leia: **quick-reference-labweb7.md** → Seções 4, 5  
👉 Veja: BaseController pattern

### Cenário 6: "Vou fazer deploy"
👉 Leia: **analise-arquitetural-completa-labweb7.md** → Seção 14  
👉 Veja: **diagramas-arquiteturais-labweb7.md** → Diagrama 10

---

## 🔍 Principais Descobertas da Análise

### Estrutura
- **6 projetos** na solution com dependências bem definidas
- **21 controllers** usando BaseController pattern
- **52 models** mapeados via EF Core
- **51 tabelas** PostgreSQL
- **20 ViewModels** para validação

### Padrões Arquiteturais
- **Factory Pattern** para DbContext (troca dinâmica de banco)
- **Repository Pattern** genérico (IRepositorio<T>)
- **Strategy Pattern** para integrações
- **BaseController Pattern** para controllers
- **Multi-tenant** por conexão (banco separado por empresa)

### Banco de Dados
- **NO usa EF Migrations** - scripts SQL manuais
- **Custom SaveChanges** com reutilização de IDs
- **DeleteOrphans** automático
- **Table Locking** para concorrência
- **Sequence Synchronization** automático

### Relacionamentos
- **20+ FKs** mapeadas no EF Core
- **4 relacionamentos** apenas em código (sem FK no banco)
- **ContaExame** hierárquica (11 dígitos, validação por prefixo 7 dígitos)

### Regras Críticas
- **SUS Model** (ExameId=1) replica para TODAS instituições
- **Transação de Requisição**: Médico/Paciente FORA da transação
- **Data/Hora**: NUNCA usar DateTime.Now, sempre do servidor
- **Exclusão**: Validar FKs antes de DELETE

### Dependências
- **Npgsql** para PostgreSQL
- **EF Core 8.0.19**
- **AWS S3, Azure Blob Storage**
- **iText, PdfSharpCore** para PDF
- **SixLabors.ImageSharp** para imagens
- **Google reCAPTCHA Enterprise**

---

## 📊 Estatísticas da Análise

| Métrica | Valor |
|---------|-------|
| Linhas de código analisadas | ~15,000+ |
| Arquivos examinados | 100+ |
| Modelos analisados | 52 |
| Controllers examinados | 21 |
| Tabelas mapeadas | 51 |
| Relacionamentos FK | 20+ |
| Relacionamentos código-only | 4 |
| Pacotes NuGet | 20+ |
| Diagramas criados | 12 |
| Documentos gerados | 4 |

---

## 🔗 Links Relacionados

### Documentação do Projeto
- **Steering Rules:** `.kiro/steering/*.md` (5 arquivos de regras)
- **Kiro Documents:** `Documentos do Kiro/`
- **Project Documents:** `LabWeb7-Project Documentos/`

### Código-Fonte Principal
- **Startup:** `LabWebMvc.MVC/Startup.cs`
- **DbContext:** `LabWebMvc.MVC/Models/db.cs`
- **BaseController:** `LabWebMvc.MVC/Areas/Controllers/BaseController.cs`
- **AppSettings:** `LabWebMvc.MVC/appsettings.json`

### Banco de Dados
- **SQL Scripts:** `Biblioteca SQL/Base de Dados Vazio Postgresql/`
- **Controle de Acesso:** `Cria Tabelas de Controle de Acesso.sql`

---

## ⚠️ Notas Importantes

1. **Análise baseada em código-fonte** - Algumas inferências podem precisar de validação
2. **Relacionamentos code-only** - Requer atenção especial (não há constraints FK no banco)
3. **Multi-tenant** - Cada empresa tem banco próprio, manter schemas sincronizados
4. **Sem Migrations** - Alterações de schema via scripts SQL manuais
5. **DateTime handling** - Regra crítica: sempre usar data/hora do servidor

---

## 🚀 Próximos Passos Recomendados

1. ✅ **Validar** relacionamentos code-only com scripts SQL reais
2. ✅ **Documentar** stored procedures/functions se existirem
3. ✅ **Mapear** todas as validações de negócio nos controllers
4. ✅ **Criar** diagrama de sequência para fluxos adicionais
5. ✅ **Registrar** enums e constantes em documento separado
6. ✅ **Documentar** APIs de integração externas
7. ✅ **Criar** guia de troubleshooting expandido

---

## 📞 Contato e Suporte

Para dúvidas sobre esta documentação:
- Consulte os arquivos individuais para detalhes
- Verifique os steering rules em `.kiro/steering/`
- Analise o código-fonte diretamente para validação

---

**Documentação gerada automaticamente por Qoder AI**  
**Baseada em análise profunda do código-fonte em 21/04/2026**
