with open('quick-reference-labweb7.md', 'r', encoding='utf-8') as f:
    lines = f.readlines()

table = [l.rstrip() for l in lines[331:336] if '|' in l]

print('Tabela Encoding:')
for i, l in enumerate(table, 332):
    print(f'Linha {i}: [{len(l):2d} chars]')

print(f'\nTodas iguais? {len(set(len(l) for l in table)) == 1}')
