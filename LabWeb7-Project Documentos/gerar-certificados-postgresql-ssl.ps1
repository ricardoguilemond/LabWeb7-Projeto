# ============================================================
# LABWEB7 — Gerar certificados SSL para o PostgreSQL 18
# Executar como ADMINISTRADOR
# ============================================================
# O que este script faz:
#   1. Gera certificado autoassinado (10 anos) via PowerShell nativo
#   2. Exporta server.crt e server.key para o data dir do PostgreSQL
#   3. Ajusta permissoes do server.key (somente leitura para NETWORK SERVICE)
#   4. Reinicia o servico PostgreSQL para aplicar ssl=on
# ============================================================

$dataDir   = "C:\Program Files\PostgreSQL\18\data"
$certFile  = "$dataDir\server.crt"
$keyFile   = "$dataDir\server.key"
$pfxTemp   = "$env:TEMP\postgresql_ssl_temp.pfx"
$pfxPwd    = ConvertTo-SecureString "TempPwd123!" -AsPlainText -Force

Write-Host "=== LABWEB7: Gerando certificados SSL para PostgreSQL ===" -ForegroundColor Cyan

# --- Passo 1: Gerar certificado autoassinado no repositorio Windows ---
Write-Host "`n[1/5] Gerando certificado autoassinado (10 anos)..." -ForegroundColor Yellow
$cert = New-SelfSignedCertificate `
    -Subject "CN=localhost" `
    -DnsName "localhost", "GUILEMOND-ACER" `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotAfter (Get-Date).AddYears(10) `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -KeyUsage DigitalSignature, KeyEncipherment `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")

Write-Host "    Certificado criado. Thumbprint: $($cert.Thumbprint)" -ForegroundColor Green

# --- Passo 2: Exportar para PFX temporario ---
Write-Host "`n[2/5] Exportando para PFX temporario..." -ForegroundColor Yellow
Export-PfxCertificate -Cert $cert -FilePath $pfxTemp -Password $pfxPwd | Out-Null
Write-Host "    PFX exportado: $pfxTemp" -ForegroundColor Green

# --- Passo 3: Extrair CRT e KEY usando certutil + openssl do Git (se disponivel) ---
Write-Host "`n[3/5] Extraindo server.crt e server.key..." -ForegroundColor Yellow

# Tentar usar openssl do Git for Windows
$opensslPath = @(
    "C:\Program Files\Git\usr\bin\openssl.exe",
    "C:\Program Files (x86)\Git\usr\bin\openssl.exe",
    "C:\Git\usr\bin\openssl.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($opensslPath) {
    Write-Host "    OpenSSL encontrado: $opensslPath" -ForegroundColor Green

    # Exportar CRT (certificado publico)
    & $opensslPath pkcs12 -in $pfxTemp -clcerts -nokeys -out $certFile -passin pass:TempPwd123! 2>&1
    # Exportar KEY (chave privada sem criptografia — PostgreSQL nao aceita senha na chave)
    & $opensslPath pkcs12 -in $pfxTemp -nocerts -nodes -out $keyFile -passin pass:TempPwd123! 2>&1

    Write-Host "    server.crt gerado: $certFile" -ForegroundColor Green
    Write-Host "    server.key gerado: $keyFile" -ForegroundColor Green
} else {
    Write-Host "    OpenSSL nao encontrado. Usando metodo alternativo (certutil)..." -ForegroundColor Yellow

    # Exportar apenas o certificado publico via certutil
    certutil -exportPFX -p "TempPwd123!" My $cert.Thumbprint $pfxTemp 2>&1 | Out-Null

    # Exportar CRT em formato DER e converter para PEM
    $certDer = "$env:TEMP\server_temp.der"
    Export-Certificate -Cert $cert -FilePath $certDer -Type CERT | Out-Null
    certutil -encode $certDer $certFile 2>&1 | Out-Null
    # Ajustar cabecalho PEM (certutil usa "CERTIFICATE" correto)

    Write-Host "    server.crt gerado via certutil: $certFile" -ForegroundColor Green
    Write-Host ""
    Write-Host "    ATENCAO: server.key nao pode ser exportado sem OpenSSL." -ForegroundColor Red
    Write-Host "    Instale Git for Windows (https://git-scm.com) e execute este script novamente." -ForegroundColor Red
    Write-Host "    Ou instale OpenSSL: https://slproweb.com/products/Win32OpenSSL.html" -ForegroundColor Red

    Remove-Item $pfxTemp -ErrorAction SilentlyContinue
    exit 1
}

# --- Passo 4: Ajustar permissoes do server.key ---
Write-Host "`n[4/5] Ajustando permissoes do server.key..." -ForegroundColor Yellow

# PostgreSQL no Windows roda como NETWORK SERVICE
$acl = Get-Acl $keyFile
$acl.SetAccessRuleProtection($true, $false)  # Remover heranca

# Dar controle total ao SYSTEM e Administrators
$ruleSystem = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "NT AUTHORITY\SYSTEM", "FullControl", "Allow")
$ruleAdmin = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "BUILTIN\Administrators", "FullControl", "Allow")
$ruleNS = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "NT AUTHORITY\NETWORK SERVICE", "Read", "Allow")

$acl.AddAccessRule($ruleSystem)
$acl.AddAccessRule($ruleAdmin)
$acl.AddAccessRule($ruleNS)
Set-Acl $keyFile $acl

Write-Host "    Permissoes configuradas (SYSTEM + Administrators: Full, NETWORK SERVICE: Read)" -ForegroundColor Green

# --- Passo 5: Reiniciar PostgreSQL ---
Write-Host "`n[5/5] Reiniciando servico PostgreSQL..." -ForegroundColor Yellow

$pgService = Get-Service | Where-Object { $_.DisplayName -like "*PostgreSQL*" } | Select-Object -First 1

if ($pgService) {
    Write-Host "    Servico encontrado: $($pgService.Name)" -ForegroundColor Green
    Restart-Service -Name $pgService.Name -Force
    Start-Sleep -Seconds 3
    $status = (Get-Service -Name $pgService.Name).Status
    Write-Host "    Status apos reinicio: $status" -ForegroundColor $(if ($status -eq 'Running') { 'Green' } else { 'Red' })
} else {
    Write-Host "    Servico PostgreSQL nao encontrado. Reinicie manualmente via services.msc" -ForegroundColor Red
}

# --- Limpeza ---
Remove-Item $pfxTemp -ErrorAction SilentlyContinue
Remove-Item "$env:TEMP\server_temp.der" -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=== CONCLUIDO ===" -ForegroundColor Cyan
Write-Host "Verifique no pgAdmin com: SHOW ssl;" -ForegroundColor White
Write-Host "Resultado esperado: 'on'" -ForegroundColor White
