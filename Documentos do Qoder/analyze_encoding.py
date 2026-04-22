lines = [
    '| Extensão              | Encoding | BOM              |',
    '|-----------------------|----------|------------------|',
    '| .cs, .cshtml, .csproj | UTF-8    | ✅ Com BOM        |',
    '| .js                   | UTF-8    | 🔵 Manter         |',
    '| .json, .css, .md      | UTF-8    | ❌ Sem BOM        |'
]

print("Análise detalhada:\n")

for i, line in enumerate(lines, 332):
    print(f"Linha {i}: {len(line)} chars")
    parts = line.split('|')
    print(f"  Col1: [{len(parts[1])-1}] '{parts[1]}'")
    print(f"  Col2: [{len(parts[2])-1}] '{parts[2]}'")
    print(f"  Col3: [{len(parts[3])-1}] '{parts[3]}'")
    print()
