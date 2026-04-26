---
inclusion: always
---

# Steering de Boas Práticas — Encoding e Acentuação (PT-BR)

## Objetivo

Garantir que todos os arquivos manipulados pelo Kiro mantenham:
- O encoding original definido pelo projeto.
- A acentuação correta e completa em Português-Brasil.

## Regras de Encoding do Projeto LabWeb7

### Padrão de encoding por tipo de arquivo

| Tipo de arquivo          | Encoding | BOM                                          |
|--------------------------|----------|----------------------------------------------|
| `.cs` (C#)               | UTF-8    | Com BOM                                      |
| `.csproj`                | UTF-8    | Com BOM                                      |
| `.sln`                   | UTF-8    | Com BOM                                      |
| `.cshtml` (Razor Views)  | UTF-8    | Com BOM                                      |
| `.config` / `.xml`       | UTF-8    | Com BOM                                      |
| `.js` (JavaScript)       | UTF-8    | Manter o que já existe (não adicionar/remover)|
| `.css`                   | UTF-8    | Sem BOM                                      |
| `.json`                  | UTF-8    | Sem BOM                                      |
| `.md`                    | UTF-8    | Sem BOM                                      |

### Regra principal: nunca alterar o encoding de um arquivo

- Se o arquivo está em UTF-8 com BOM, mantê-lo em UTF-8 com BOM.
- Se o arquivo está em UTF-8 sem BOM, mantê-lo em UTF-8 sem BOM.
- Nunca converter para ANSI, ISO-8859-1, Windows-1252 ou qualquer outro formato.
- Nunca usar `Set-Content` do PowerShell sem especificar encoding (ele usa UTF-16 por padrão).
- Ao usar PowerShell para manipular arquivos, sempre usar `[System.IO.File]::WriteAllBytes()` ou `[System.IO.File]::WriteAllText()` com encoding explícito.

### Preservar acentuação em Português-Brasil

- Sempre escrever em Português-Brasil com acentos corretos: ação, informação, útil, será, também, Órgão.
- Nunca substituir caracteres acentuados por versões sem acento.
- Nunca usar entidades HTML em strings C# ou labels Razor (usar o caractere real).
- Revisar strings e textos para garantir consistência ortográfica.

### Validação obrigatória

Antes de considerar uma tarefa concluída:
1. Verificar se o encoding dos arquivos alterados permanece o mesmo.
2. Confirmar que não houve perda de acentuação.
3. Se um script PowerShell ou ferramenta externa foi usado para alterar arquivos em lote, verificar o encoding de pelo menos 3 arquivos alterados.

### Proibições

- **Nunca** criar hooks que alterem encoding automaticamente (BOM hooks causaram corrupção de .js no passado).
- **Nunca** usar `Get-Content | Set-Content` do PowerShell para manipular arquivos .cshtml, .cs ou .js — isso pode alterar o encoding silenciosamente.
- **Nunca** usar regex replace em bytes brutos sem preservar o encoding original.

### Em caso de dúvida

- Sinalizar imediatamente ao usuário antes de prosseguir.
- Preferir não alterar o arquivo a arriscar corromper o encoding.
- Se precisar verificar o encoding de um arquivo, usar:
  ```powershell
  $bytes = [System.IO.File]::ReadAllBytes("caminho/arquivo")
  $hasBOM = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
  ```
