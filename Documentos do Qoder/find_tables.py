with open('quick-reference-labweb7.md', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Encontrar todas as tabelas (linhas com |)
tables = []
current_table = []
in_table = False

for i, line in enumerate(lines, 1):
    if '|' in line and line.strip().startswith('|'):
        if not in_table:
            in_table = True
            current_table = [(i, line.rstrip())]
        else:
            current_table.append((i, line.rstrip()))
    else:
        if in_table:
            tables.append(current_table)
            current_table = []
            in_table = False

if current_table:
    tables.append(current_table)

print(f"Encontradas {len(tables)} tabelas:\n")

for idx, table in enumerate(tables, 1):
    print(f"=== TABELA {idx} (linhas {table[0][0]}-{table[-1][0]}) ===")
    for line_num, line in table:
        print(f"  Linha {line_num}: [{len(line):2d} chars]")
    
    # Verificar se todas têm o mesmo tamanho
    sizes = [len(line) for _, line in table]
    if len(set(sizes)) == 1:
        print(f"  ✅ Todas iguais: {sizes[0]} chars")
    else:
        print(f"  ❌ Tamanhos diferentes: {set(sizes)} chars")
    print()
