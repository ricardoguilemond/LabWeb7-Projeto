---
trigger: always
description: Steering de Boas Práticas — Encoding e Acentuação (PT-BR) para LabWeb7
---

# Steering de Encoding e Acentuação - Qoder

## Objetivo

Garantir que todos os arquivos manipulados pelo Qoder mantenham:
- ✅ O encoding original definido pelo projeto
- ✅ A acentuação correta e completa em Português-Brasil
- ✅ Consistência ortográfica em toda a codebase

## Padrão de Encoding por Tipo de Arquivo

| Tipo de Arquivo          | Encoding | BOM                                            |
|--------------------------|----------|------------------------------------------------|
| `.cs` (C#)               | UTF-8    | ✅ Com BOM                                      |
| `.csproj`                | UTF-8    | ✅ Com BOM                                      |
| `.sln`                   | UTF-8    | ✅ Com BOM                                      |
| `.cshtml` (Razor Views)  | UTF-8    | ✅ Com BOM                                      |
| `.config` / `.xml`       | UTF-8    | ✅ Com BOM                                      |
| `.js` (JavaScript)       | UTF-8    | ⚠️ Manter o que já existe (NÃO adicionar/remover) |
| `.css`                   | UTF-8    | ❌ Sem BOM                                       |
| `.json`                  | UTF-8    | ❌ Sem BOM                                       |
| `.md`                    | UTF-8    | ❌ Sem BOM                                       |

## Regra Principal: NUNCA Alterar o Encoding

### Preservação
- ✅ Se o arquivo está em UTF-8 **com BOM**, mantê-lo em UTF-8 **com BOM**
- ✅ Se o arquivo está em UTF-8 **sem BOM**, mantê-lo em UTF-8 **sem BOM**
- ❌ **NUNCA** converter para ANSI, ISO-8859-1, Windows-1252 ou qualquer outro formato

### PowerShell - Manipulação Segura de Arquivos
```powershell
# ✅ CORRETO - Usando encoding explícito
[System.IO.File]::WriteAllText("caminho/arquivo.cs", $content, [System.Text.Encoding]::UTF8)

# ✅ CORRETO - Usando bytes
[System.IO.File]::WriteAllBytes("caminho/arquivo.js", $bytes)

# ❌ ERRADO - Set-Content usa UTF-16 por padrão
Get-Content "arquivo.cs" | Set-Content "arquivo.cs"

# ❌ ERRADO - Sem especificar encoding
Out-File -FilePath "arquivo.cshtml" -InputObject $content
```

## Preservar Acentuação em Português-Brasil

### Regras de Escrita
- ✅ **SEMPRE** escrever em Português-Brasil com acentos corretos:
  - ação, informação, útil, será, também, Órgão, necessário, execução
- ❌ **NUNCA** substituir caracteres acentuados por versões sem acento
- ❌ **NUNCA** usar entidades HTML em strings C# ou labels Razor
  - ❌ `&ccedil;&atilde;o` (ERRADO)
  - ✅ `ção` (CORRETO - usar caractere real)

### Exemplos de Strings Corretas
```csharp
// ✅ CORRETO
var mensagem = "Paciente possui exames vinculados e não pode ser excluído.";
var titulo = "Configurações do Sistema";
var label = "Selecione a Instituição";

// ❌ ERRADO
var mensagem = "Paciente possui exames vinculados e nao pode ser excluido.";
var titulo = "Configuracoes do Sistema";
```

### Razor Views
```cshtml
@* ✅ CORRETO *@
<h2>Gerenciamento de Pacientes</h2>
<label>Nome do Médico:</label>
<button type="submit">Salvar Configurações</button>

@* ❌ ERRADO *@
<h2>Gerenciamento de Pacientes</h2>
<label>Nome do Medico:</label>
<button type="submit">Salvar Configuracoes</button>
```

## Validação Obrigatória

### Antes de Considerar Tarefa Concluída
1. ✅ Verificar se o encoding dos arquivos alterados permanece o mesmo
2. ✅ Confirmar que **NÃO houve perda de acentuação**
3. ✅ Se um script PowerShell ou ferramenta externa foi usado para alterar arquivos em lote, verificar o encoding de pelo menos **3 arquivos alterados**

### Verificar Encoding (PowerShell)
```powershell
# Verificar se arquivo tem BOM
$bytes = [System.IO.File]::ReadAllBytes("caminho/arquivo.cs")
$hasBOM = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
Write-Host "Tem BOM: $hasBOM"

# Verificar acentuação em arquivo
$content = [System.IO.File]::ReadAllText("caminho/arquivo.cs", [System.Text.Encoding]::UTF8)
if ($content -match '[àáâãéêíóôõúüç]') {
    Write-Host "Arquivo contém caracteres acentuados"
}
```

## Proibições

### ❌ NUNCA Fazer
- ❌ Criar hooks que alterem encoding automaticamente (BOM hooks causaram corrupção de .js no passado)
- ❌ Usar `Get-Content | Set-Content` do PowerShell para manipular arquivos .cshtml, .cs ou .js
- ❌ Usar regex replace em bytes brutos sem preservar o encoding original
- ❌ Converter arquivos em lote sem validar encoding depois
- ❌ Usar ferramentas de "limpeza" que removem acentos

## Em Caso de Dúvida

1. 🛑 **Sinalizar imediatamente** ao usuário antes de prosseguir
2. 🛑 **Preferir não alterar** o arquivo a arriscar corromper o encoding
3. 🛑 Se precisar verificar o encoding, usar o script PowerShell acima

## Problemas Conhecidos

### Incidente Histórico
- **Problema:** Hooks de BOM causaram corrupção de arquivos .js
- **Causa:** Conversão automática de encoding sem validação
- **Solução:** NUNCA criar hooks automáticos de encoding
- **Lição:** Sempre validar manualmente após alterações em lote

## Checklist de Validação

Antes de commitar alterações:

```
□ Encoding dos arquivos .cs está UTF-8 com BOM?
□ Encoding dos arquivos .cshtml está UTF-8 com BOM?
□ Encoding dos arquivos .js foi mantido (sem alterar BOM)?
□ Encoding dos arquivos .json está UTF-8 sem BOM?
□ Todos os acentos em PT-BR estão preservados?
□ Nenhuma entidade HTML foi usada em strings C#?
□ Textos em Razor Views usam caracteres reais (não entidades)?
```

---

**Steering criado por Qoder - 21/04/2026**  
*Baseado nas melhores práticas do projeto LabWeb7*
