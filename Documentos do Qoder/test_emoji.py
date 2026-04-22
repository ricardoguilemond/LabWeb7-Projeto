# Teste 1: Cabeçalho com +1 caractere (atual)
print("=== TESTE 1: Cabeçalho com +1 caractere ===")
t1_header = "| Extensão              | Encoding | BOM               |"
t1_sep =    "|-----------------------|----------|-------------------|"
t1_row1 =   "| .cs, .cshtml, .csproj | UTF-8    | ✅ Com BOM        |"
t1_row2 =   "| .js                   | UTF-8    | 🔵 Manter         |"
t1_row3 =   "| .json, .css, .md      | UTF-8    | ❌ Sem BOM        |"

print(f"Header:    [{len(t1_header)} chars]")
print(f"Separator: [{len(t1_sep)} chars]")
print(f"Row1:      [{len(t1_row1)} chars] (visual: {len(t1_row1)+1})")
print(f"Row2:      [{len(t1_row2)} chars] (visual: {len(t1_row2)+1})")
print(f"Row3:      [{len(t1_row3)} chars] (visual: {len(t1_row3)+1})")
print(f"\nVisual alignment:")
print(t1_header)
print(t1_sep)
print(t1_row1)
print(t1_row2)
print(t1_row3)

print("\n" + "="*80 + "\n")

# Teste 2: Cabeçalho com +2 caracteres
print("=== TESTE 2: Cabeçalho com +2 caracteres ===")
t2_header = "| Extensão              | Encoding | BOM                |"
t2_sep =    "|-----------------------|----------|--------------------|"
t2_row1 =   "| .cs, .cshtml, .csproj | UTF-8    | ✅ Com BOM        |"
t2_row2 =   "| .js                   | UTF-8    | 🔵 Manter         |"
t2_row3 =   "| .json, .css, .md      | UTF-8    | ❌ Sem BOM        |"

print(f"Header:    [{len(t2_header)} chars]")
print(f"Separator: [{len(t2_sep)} chars]")
print(f"Row1:      [{len(t2_row1)} chars] (visual: {len(t2_row1)+1})")
print(f"Row2:      [{len(t2_row2)} chars] (visual: {len(t2_row2)+1})")
print(f"Row3:      [{len(t2_row3)} chars] (visual: {len(t2_row3)+1})")
print(f"\nVisual alignment:")
print(t2_header)
print(t2_sep)
print(t2_row1)
print(t2_row2)
print(t2_row3)

print("\n" + "="*80 + "\n")

# Teste 3: Coluna 3 detalhada
print("=== DETALHE COLUNA 3 ===")
print(f"T1 Header col3:    ' BOM               ' = {len(' BOM               ')} chars")
print(f"T1 Sep col3:       '-------------------' = {len('-------------------')} chars")
print(f"T1 Row1 col3:      ' ✅ Com BOM        ' = {len(' ✅ Com BOM        ')} chars (visual: {len(' ✅ Com BOM        ')+1})")
print()
print(f"T2 Header col3:    ' BOM                ' = {len(' BOM                ')} chars")
print(f"T2 Sep col3:       '--------------------' = {len('--------------------')} chars")
print(f"T2 Row1 col3:      ' ✅ Com BOM        ' = {len(' ✅ Com BOM        ')} chars (visual: {len(' ✅ Com BOM        ')+1})")
