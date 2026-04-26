using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

// Diretório do PostgreSQL
string dataDir = @"C:\Program Files\PostgreSQL\18\data";
string certFile = Path.Combine(dataDir, "server.crt");
string keyFile  = Path.Combine(dataDir, "server.key");

Console.WriteLine("=== LABWEB7: Gerando certificados SSL para PostgreSQL ===");

// Gerar chave RSA 2048
using var rsa = RSA.Create(2048);

// Montar requisição de certificado
var req = new CertificateRequest(
    new X500DistinguishedName("CN=localhost, O=LABWEB7, C=BR"),
    rsa,
    HashAlgorithmName.SHA256,
    RSASignaturePadding.Pkcs1);

// Extensões básicas
req.CertificateExtensions.Add(
    new X509BasicConstraintsExtension(false, false, 0, true));
req.CertificateExtensions.Add(
    new X509KeyUsageExtension(
        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
req.CertificateExtensions.Add(
    new X509EnhancedKeyUsageExtension(
        new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false)); // Server Auth

// SAN: localhost + GUILEMOND-ACER
var sanBuilder = new SubjectAlternativeNameBuilder();
sanBuilder.AddDnsName("localhost");
sanBuilder.AddDnsName("GUILEMOND-ACER");
req.CertificateExtensions.Add(sanBuilder.Build());

// Assinar (autoassinado, válido 10 anos)
var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
var notAfter  = notBefore.AddYears(10);
using var cert = req.CreateSelfSigned(notBefore, notAfter);

// ---- Exportar server.crt (PEM) ----
var certPem = cert.ExportCertificatePem();
File.WriteAllText(certFile, certPem);
Console.WriteLine($"[OK] server.crt -> {certFile}");

// ---- Exportar server.key (PEM sem senha — PostgreSQL exige sem senha) ----
var keyPem = rsa.ExportRSAPrivateKeyPem();
File.WriteAllText(keyFile, keyPem);
Console.WriteLine($"[OK] server.key -> {keyFile}");

// ---- Verificar ----
Console.WriteLine("\nVerificando arquivos gerados:");
Console.WriteLine($"  server.crt: {new FileInfo(certFile).Length} bytes");
Console.WriteLine($"  server.key: {new FileInfo(keyFile).Length} bytes");
Console.WriteLine("\n=== CONCLUIDO ===");
Console.WriteLine("Agora reinicie o PostgreSQL como Administrador:");
Console.WriteLine("  Restart-Service -Name (Get-Service *postgres* | Select -First 1).Name -Force");
Console.WriteLine("Depois verifique no pgAdmin: SHOW ssl;");
