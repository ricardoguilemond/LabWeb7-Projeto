import re

path = r'C:\Program Files\PostgreSQL\18\data\postgresql.conf'

with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Acao 2: listen_addresses = 'localhost'
content = content.replace(
    "listen_addresses = 'localhost'\t\t\t# LABWEB7-SEGURANCA: restrito a localhost (era '*')",
    "listen_addresses = 'localhost'"
)
content = content.replace(
    "listen_addresses = '*'",
    "listen_addresses = 'localhost'  # LABWEB7-SEGURANCA: era '*'"
)

# Acao 3: ssl = on
content = content.replace(
    "#ssl = off",
    "ssl = on  # LABWEB7-SEGURANCA: ativado em 21/04/2026"
)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

# Verificar resultado
with open(path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

for i, line in enumerate(lines, 1):
    if 'listen_addresses' in line or ('ssl' in line and 'ssl_' not in line and '#ssl_' not in line):
        print(f"Linha {i}: {line.rstrip()}")

print("CONCLUIDO")
