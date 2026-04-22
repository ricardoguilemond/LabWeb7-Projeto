with open('quick-reference-labweb7.md', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Tabela 1: Relacionamentos (linhas 105-110)
print("=== TABELA 1 - Relacionamentos Apenas em Código ===")
table1 = [l.rstrip() for l in lines[104:110] if '|' in l]
for i, l in enumerate(table1, 105):
    print(f"Linha {i}: [{len(l):2d} chars]")
print(f"Todas iguais? {len(set(len(l) for l in table1)) == 1}")
print(f"Tamanho: {len(table1[0])} chars")

print()

# Tabela 2: Nomenclatura (linhas 156-165)
print("=== TABELA 2 - Nomenclatura ===")
table2 = [l.rstrip() for l in lines[155:165] if '|' in l]
for i, l in enumerate(table2, 156):
    print(f"Linha {i}: [{len(l):2d} chars]")
print(f"Todas iguais? {len(set(len(l) for l in table2)) == 1}")
print(f"Tamanho: {len(table2[0])} chars")

print()

# Tabela 3: Encoding (linhas 332-336)
print("=== TABELA 3 - Encoding por Tipo ===")
table3 = [l.rstrip() for l in lines[331:336] if '|' in l]
for i, l in enumerate(table3, 332):
    print(f"Linha {i}: [{len(l):2d} chars]")
print(f"Todas iguais? {len(set(len(l) for l in table3)) == 1}")
print(f"Tamanho: {len(table3[0])} chars")
