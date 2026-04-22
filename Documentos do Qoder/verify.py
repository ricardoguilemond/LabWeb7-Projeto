with open('analise-arquitetural-completa-labweb7.md', 'r', encoding='utf-8') as f:
    lines = f.readlines()

table_lines = [l.rstrip() for l in lines[421:429] if '|' in l]

print('Verificação da tabela:')
for i, l in enumerate(table_lines):
    print(f'Linha {i+422}: [{len(l):2d} chars]')

print(f'\nTodas iguais? {len(set(len(l) for l in table_lines)) == 1}')
print(f'Tamanho: {len(table_lines[0])} chars' if table_lines else 'Tabela vazia')
