with open('quick-reference-labweb7.md', 'r', encoding='utf-8') as f:
    lines = f.readlines()

table = [l.rstrip() for l in lines[331:336]]

print("Tabela Encoding:")
for i, l in enumerate(table, 332):
    print(f"Linha {i}: [{len(l):2d} chars] {l}")

print(f"\nNota: Python conta emojis como 1 char, mas visualmente ocupam 2 chars")
print("Cabeçalho e tracejado: 56 chars (compensam largura dos emojis)")
print("Linhas com emojis: 55 chars no Python = 56 visualmente")
