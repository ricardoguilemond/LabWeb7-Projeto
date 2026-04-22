# Tabela 6.3 - Google Cloud
print("=== TABELA 6.3 - Google Cloud ===")
col1_texts = ['Pacote', 'Google.Cloud.RecaptchaEnterprise.V1', 'Google.Api.Gax']
col2_texts = ['Versão', '2.18.0', '4.11.0']
col3_texts = ['Uso', 'reCAPTCHA Enterprise', 'Google API Extensions']

col1_max = max(len(t) for t in col1_texts)
col2_max = max(len(t) for t in col2_texts)
col3_max = max(len(t) for t in col3_texts)

print(f"Coluna 1 - maior texto: '{max(col1_texts, key=len)}' = {col1_max} chars")
print(f"Coluna 2 - maior texto: '{max(col2_texts, key=len)}' = {col2_max} chars")
print(f"Coluna 3 - maior texto: '{max(col3_texts, key=len)}' = {col3_max} chars")

col1_width = col1_max + 2
col2_width = col2_max + 2
col3_width = col3_max + 2
total = col1_width + col2_width + col3_width + 4

print(f"\nLarguras: {col1_width} + {col2_width} + {col3_width} + 4 (pipes) = {total} chars")

print("\nTabela formatada:")
header = f"| {'Pacote'.ljust(col1_max)} | {'Versão'.ljust(col2_max)} | {'Uso'.ljust(col3_max)} |"
separator = f"|{'-' * (col1_max + 2)}|{'-' * (col2_max + 2)}|{'-' * (col3_max + 2)}|"
row1 = f"| {'Google.Cloud.RecaptchaEnterprise.V1'.ljust(col1_max)} | {'2.18.0'.ljust(col2_max)} | {'reCAPTCHA Enterprise'.ljust(col3_max)} |"
row2 = f"| {'Google.Api.Gax'.ljust(col1_max)} | {'4.11.0'.ljust(col2_max)} | {'Google API Extensions'.ljust(col3_max)} |"

print(header)
print(separator)
print(row1)
print(row2)

print(f"\nVerificação:")
for i, line in enumerate([header, separator, row1, row2], 1):
    print(f"Linha {i}: [{len(line)} chars]")

print(f"\nTodas iguais? {len(set(len(l) for l in [header, separator, row1, row2])) == 1}")

print("\n" + "="*80)

# Tabela 6.6 - Utilities
print("\n=== TABELA 6.6 - Utilities ===")
col1_texts = ['Pacote', 'Newtonsoft.Json', 'RecaptchaNet', 'System.Configuration.ConfigurationManager', 
              'Microsoft.Extensions.Hosting.WindowsServices', 'System.ServiceProcess.ServiceController',
              'System.Security.Cryptography.Xml', 'System.Drawing.Common']
col2_texts = ['Versão', '13.0.4', '3.1.0', '9.0.10', '9.0.0', '9.0.10', '9.0.10', '9.0.10']
col3_texts = ['Uso', 'JSON serialization', 'reCAPTCHA validation', 'Configuration management',
              'Windows Service hosting', 'Service control', 'XML cryptography', 'GDI+ drawing (Windows)']

col1_max = max(len(t) for t in col1_texts)
col2_max = max(len(t) for t in col2_texts)
col3_max = max(len(t) for t in col3_texts)

print(f"Coluna 1 - maior texto: '{max(col1_texts, key=len)}' = {col1_max} chars")
print(f"Coluna 2 - maior texto: '{max(col2_texts, key=len)}' = {col2_max} chars")
print(f"Coluna 3 - maior texto: '{max(col3_texts, key=len)}' = {col3_max} chars")

col1_width = col1_max + 2
col2_width = col2_max + 2
col3_width = col3_max + 2
total = col1_width + col2_width + col3_width + 4

print(f"\nLarguras: {col1_width} + {col2_width} + {col3_width} + 4 (pipes) = {total} chars")

print("\nTabela formatada:")
header = f"| {'Pacote'.ljust(col1_max)} | {'Versão'.ljust(col2_max)} | {'Uso'.ljust(col3_max)} |"
separator = f"|{'-' * (col1_max + 2)}|{'-' * (col2_max + 2)}|{'-' * (col3_max + 2)}|"
rows = [
    f"| {'Newtonsoft.Json'.ljust(col1_max)} | {'13.0.4'.ljust(col2_max)} | {'JSON serialization'.ljust(col3_max)} |",
    f"| {'RecaptchaNet'.ljust(col1_max)} | {'3.1.0'.ljust(col2_max)} | {'reCAPTCHA validation'.ljust(col3_max)} |",
    f"| {'System.Configuration.ConfigurationManager'.ljust(col1_max)} | {'9.0.10'.ljust(col2_max)} | {'Configuration management'.ljust(col3_max)} |",
    f"| {'Microsoft.Extensions.Hosting.WindowsServices'.ljust(col1_max)} | {'9.0.0'.ljust(col2_max)} | {'Windows Service hosting'.ljust(col3_max)} |",
    f"| {'System.ServiceProcess.ServiceController'.ljust(col1_max)} | {'9.0.10'.ljust(col2_max)} | {'Service control'.ljust(col3_max)} |",
    f"| {'System.Security.Cryptography.Xml'.ljust(col1_max)} | {'9.0.10'.ljust(col2_max)} | {'XML cryptography'.ljust(col3_max)} |",
    f"| {'System.Drawing.Common'.ljust(col1_max)} | {'9.0.10'.ljust(col2_max)} | {'GDI+ drawing (Windows)'.ljust(col3_max)} |"
]

print(header)
print(separator)
for row in rows:
    print(row)

print(f"\nVerificação:")
all_lines = [header, separator] + rows
for i, line in enumerate(all_lines, 1):
    print(f"Linha {i}: [{len(line)} chars]")

print(f"\nTodas iguais? {len(set(len(l) for l in all_lines)) == 1}")
