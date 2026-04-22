# TABELA 1: Relacionamentos Apenas em Código (linhas 105-110)
print("=== TABELA 1 - Relacionamentos Apenas em Código ===")
col1 = ['Origem', 'PlanoExames', 'PlanoExames', 'ItensExamesRealizados', 'Requisitar']
col2 = ['Destino', 'TabelaExames', 'ClasseExames', 'PlanoExames', 'ClasseExames']
col3 = ['Campo', 'TabelaExamesId', 'ExameId', 'ContaExame (string)', 'ClasseExamesId']
col4 = ['Tipo', 'Lógico', 'Lógico (SUS=1)', 'Por código', 'Lógico']

col1_max = max(len(t) for t in col1)
col2_max = max(len(t) for t in col2)
col3_max = max(len(t) for t in col3)
col4_max = max(len(t) for t in col4)

print(f"Col1 max: {col1_max}, Col2 max: {col2_max}, Col3 max: {col3_max}, Col4 max: {col4_max}")

header = f"| {'Origem'.ljust(col1_max)} | {'Destino'.ljust(col2_max)} | {'Campo'.ljust(col3_max)} | {'Tipo'.ljust(col4_max)} |"
sep = f"|{'-'*(col1_max+2)}|{'-'*(col2_max+2)}|{'-'*(col3_max+2)}|{'-'*(col4_max+2)}|"
row1 = f"| {'PlanoExames'.ljust(col1_max)} | {'TabelaExames'.ljust(col2_max)} | {'TabelaExamesId'.ljust(col3_max)} | {'Lógico'.ljust(col4_max)} |"
row2 = f"| {'PlanoExames'.ljust(col1_max)} | {'ClasseExames'.ljust(col2_max)} | {'ExameId'.ljust(col3_max)} | {'Lógico (SUS=1)'.ljust(col4_max)} |"
row3 = f"| {'ItensExamesRealizados'.ljust(col1_max)} | {'PlanoExames'.ljust(col2_max)} | {'ContaExame (string)'.ljust(col3_max)} | {'Por código'.ljust(col4_max)} |"
row4 = f"| {'Requisitar'.ljust(col1_max)} | {'ClasseExames'.ljust(col2_max)} | {'ClasseExamesId'.ljust(col3_max)} | {'Lógico'.ljust(col4_max)} |"

print("\nTabela formatada:")
for line in [header, sep, row1, row2, row3, row4]:
    print(line)
    print(f"  -> {len(line)} chars")

print("\n" + "="*80 + "\n")

# TABELA 2: Nomenclatura (linhas 156-165)
print("=== TABELA 2 - Nomenclatura ===")
col1 = ['Tipo', 'Model Class', 'Controller', 'ViewModel', 'Tabela BD', 'Index BD', 'FK Constraint', 'Interface', 'Service']
col2 = ['Padrão', 'PascalCase', 'PascalCase + Controller', 'vm/VM + PascalCase', 'PascalCase', 'i + Tabela + Nº', 'i + Origem + Destino', 'I + PascalCase', 'PascalCase + Service']
col3 = ['Exemplo', 'Pacientes, ExamesRealizados', 'PacientesController', 'vmPacientes, VMGeral', 'Pacientes, ExamesRealizados', 'iPacientes1, iPacientes2', 'iExamesRealizados_Pacientes', 'IEventLogHelper', 'ExclusaoService']

col1_max = max(len(t) for t in col1)
col2_max = max(len(t) for t in col2)
col3_max = max(len(t) for t in col3)

print(f"Col1 max: {col1_max}, Col2 max: {col2_max}, Col3 max: {col3_max}")

header = f"| {'Tipo'.ljust(col1_max)} | {'Padrão'.ljust(col2_max)} | {'Exemplo'.ljust(col3_max)} |"
sep = f"|{'-'*(col1_max+2)}|{'-'*(col2_max+2)}|{'-'*(col3_max+2)}|"
rows = [
    f"| {'Model Class'.ljust(col1_max)} | {'PascalCase'.ljust(col2_max)} | {'Pacientes, ExamesRealizados'.ljust(col3_max)} |",
    f"| {'Controller'.ljust(col1_max)} | {'PascalCase + Controller'.ljust(col2_max)} | {'PacientesController'.ljust(col3_max)} |",
    f"| {'ViewModel'.ljust(col1_max)} | {'vm/VM + PascalCase'.ljust(col2_max)} | {'vmPacientes, VMGeral'.ljust(col3_max)} |",
    f"| {'Tabela BD'.ljust(col1_max)} | {'PascalCase'.ljust(col2_max)} | {'Pacientes, ExamesRealizados'.ljust(col3_max)} |",
    f"| {'Index BD'.ljust(col1_max)} | {'i + Tabela + Nº'.ljust(col2_max)} | {'iPacientes1, iPacientes2'.ljust(col3_max)} |",
    f"| {'FK Constraint'.ljust(col1_max)} | {'i + Origem + Destino'.ljust(col2_max)} | {'iExamesRealizados_Pacientes'.ljust(col3_max)} |",
    f"| {'Interface'.ljust(col1_max)} | {'I + PascalCase'.ljust(col2_max)} | {'IEventLogHelper'.ljust(col3_max)} |",
    f"| {'Service'.ljust(col1_max)} | {'PascalCase + Service'.ljust(col2_max)} | {'ExclusaoService'.ljust(col3_max)} |"
]

print("\nTabela formatada:")
for line in [header, sep] + rows:
    print(line)
    print(f"  -> {len(line)} chars")

print("\n" + "="*80 + "\n")

# TABELA 3: Encoding por Tipo (linhas 332-336)
print("=== TABELA 3 - Encoding por Tipo ===")
col1 = ['Extensão', '.cs, .cshtml, .csproj', '.js', '.json, .css, .md']
col2 = ['Encoding', 'UTF-8', 'UTF-8', 'UTF-8']
col3 = ['BOM', '✅ Com BOM', 'Manter existente', '❌ Sem BOM']

col1_max = max(len(t) for t in col1)
col2_max = max(len(t) for t in col2)
col3_max = max(len(t) for t in col3)

print(f"Col1 max: {col1_max}, Col2 max: {col2_max}, Col3 max: {col3_max}")

header = f"| {'Extensão'.ljust(col1_max)} | {'Encoding'.ljust(col2_max)} | {'BOM'.ljust(col3_max)} |"
sep = f"|{'-'*(col1_max+2)}|{'-'*(col2_max+2)}|{'-'*(col3_max+2)}|"
row1 = f"| {'.cs, .cshtml, .csproj'.ljust(col1_max)} | {'UTF-8'.ljust(col2_max)} | {'✅ Com BOM'.ljust(col3_max)} |"
row2 = f"| {'.js'.ljust(col1_max)} | {'UTF-8'.ljust(col2_max)} | {'Manter existente'.ljust(col3_max)} |"
row3 = f"| {'.json, .css, .md'.ljust(col1_max)} | {'UTF-8'.ljust(col2_max)} | {'❌ Sem BOM'.ljust(col3_max)} |"

print("\nTabela formatada:")
for line in [header, sep, row1, row2, row3]:
    print(line)
    print(f"  -> {len(line)} chars")
