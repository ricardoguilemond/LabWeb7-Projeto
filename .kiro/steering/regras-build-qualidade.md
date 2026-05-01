---
inclusion: always
description: Regras de qualidade de build, pacotes e dependências para o projeto LabWeb7
---

# Steering — Qualidade de Build e Dependências

## Objetivo

Garantir que o projeto compile sempre com **0 erros e 0 avisos**,
e que nenhuma dependência seja alterada sem validação explícita do usuário.

## Regra 1 — Build deve ser 0 erros e 0 avisos

- Após qualquer alteração de código, o build **deve ser executado**.
- O resultado aceitável é exclusivamente: `0 Erro(s)` e `0 Aviso(s)`.
- Qualquer erro (`error`), aviso (`warning`) ou hint que apareça no
  output do build **deve ser corrigido antes de considerar a tarefa concluída**.
- Isso inclui:
  - Erros de compilação C#
  - Erros de restauração de pacotes (`NU1605`, `NU1701`, etc.)
  - Warnings de nullable reference types
  - Warnings de obsolescência de API
  - Hints e sugestões do compilador que apareçam no output

### Como compilar o projeto MVC

```powershell
dotnet build "LabWebMvc.MVC/LabWebMvc.MVC.csproj"
```

### Como compilar a solution completa

```powershell
dotnet build "LabWebMvc.sln"
```

### Resultado esperado

```
Compilação com êxito.
    0 Aviso(s)
    0 Erro(s)
```

## Regra 2 — Conflitos de pacotes devem ser resolvidos

- Erros `NU1605` (downgrade de pacote detectado) devem ser corrigidos
  adicionando a referência direta ao pacote na versão correta no `.csproj`
  afetado.
- Erros `NU1701` (pacote não compatível com o framework alvo) devem ser
  investigados e reportados ao usuário antes de qualquer ação.
- Warnings `NU1903` (vulnerabilidade conhecida) devem ser reportados ao
  usuário com a descrição do CVE antes de qualquer atualização.

### Exemplo de correção de NU1605

Se `WindowsService.csproj` apresentar downgrade de
`System.Security.Cryptography.Xml` de `9.0.15` para `9.0.10`:

```xml
<!-- Adicionar referência direta na versão mais alta -->
<PackageReference Include="System.Security.Cryptography.Xml" Version="9.0.15" />
```

## Regra 3 — Bibliotecas e pacotes NuGet

### Proibições sem autorização explícita do usuário

- ❌ **NUNCA** adicionar um novo pacote NuGet sem aprovação do usuário.
- ❌ **NUNCA** atualizar a versão de um pacote existente sem aprovação.
- ❌ **NUNCA** remover um pacote existente sem aprovação.
- ❌ **NUNCA** trocar um pacote por outro equivalente sem aprovação.

### Preferência

- A preferência é **sempre manter as bibliotecas existentes** nas versões
  atuais, salvo quando houver conflito de build que exija resolução.
- Quando um conflito exigir atualização de versão, **apresentar ao usuário**
  a versão atual, a versão necessária e o motivo, e aguardar aprovação.

### Fluxo obrigatório para qualquer mudança de dependência

```
1. Identificar o problema (erro de build, conflito, vulnerabilidade)
2. Descrever ao usuário: pacote afetado, versão atual, versão proposta, motivo
3. Aguardar aprovação explícita
4. Somente então aplicar a mudança
5. Compilar e confirmar 0 erros e 0 avisos
```

## Regra 4 — Checklist de encerramento de tarefa

Antes de declarar qualquer tarefa como concluída:

```
□ Build executado após as alterações?
□ Resultado: 0 erros e 0 avisos?
□ Nenhum pacote foi adicionado, removido ou atualizado sem aprovação?
□ Se houve conflito de pacote, foi reportado e aprovado pelo usuário?
□ Encoding dos arquivos alterados foi preservado?
```
