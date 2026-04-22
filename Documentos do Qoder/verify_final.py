with open('analise-arquitetural-completa-labweb7.md', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Tabela 6.3 - Google Cloud (linhas 439-443)
print("=== TABELA 6.3 - Google Cloud ===")
table_63 = [l.rstrip() for l in lines[438:443] if '|' in l]
for i, l in enumerate(table_63, 439):
    print(f"Linha {i}: [{len(l):2d} chars]")
print(f"Todas iguais? {len(set(len(l) for l in table_63)) == 1}")
print(f"Tamanho: {len(table_63[0])} chars")

print()

# Tabela 6.6 - Utilities (linhas 462-470)
print("=== TABELA 6.6 - Utilities ===")
table_66 = [l.rstrip() for l in lines[461:471] if '|' in l]
for i, l in enumerate(table_66, 462):
    print(f"Linha {i}: [{len(l):2d} chars]")
print(f"Todas iguais? {len(set(len(l) for l in table_66)) == 1}")
print(f"Tamanho: {len(table_66[0])} chars")
