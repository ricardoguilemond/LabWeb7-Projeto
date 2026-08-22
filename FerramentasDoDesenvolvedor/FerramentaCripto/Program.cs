/* =====================================================================================================================
 * FerramentaCripto — CONTRAPARTIDA EXPLÍCITA DE DESCRIPTOGRAFIA PARA O DESENVOLVEDOR
 * =====================================================================================================================
 * Feito pelo Qoder em 22/08/2026.
 *
 * Ferramenta autônoma (execução apartada do sistema web) que permite ao DESENVOLVEDOR descriptografar
 * o conteúdo de campos criptografados em tabelas do banco de dados do LabWeb7, conforme requisito de negócio:
 * "toda criptografia aplicada em dados/configurações deve ter contrapartida explícita de descriptografia
 *  disponível apenas para o desenvolvedor".
 *
 * O QUE ESTA FERRAMENTA DESCRIPTOGRAFA:
 *   1) AES legado (sem prefixo)   — AES-CBC com chave E IV fixos = Secrets:myVetorDeCifras.
 *   2) AES v2 (prefixo LABW7V2$)  — AES-CBC com IV aleatório prepended ao ciphertext.
 *   3) Cifra interna de substituição ("Criptografia", usada em configurações legadas).
 *   4) Token de nome de arquivo ("ArquivoToken").
 *
 * O QUE ESTA FERRAMENTA NÃO DESCRIPTOGRAFA (por definição, irreversível):
 *   X) SENHAS de usuário (colunas de senha armazenam hash BCrypt "$2a$..."). Não existe — nem deve existir —
 *      qualquer meio de recuperar a senha original a partir do hash. Para senhas, use o fluxo de produto
 *      "Esqueci minha senha" (recuperação por e-mail).
 *
 * SEGURANÇA DA FERRAMENTA:
 *   - A chave AES NUNCA é embutida aqui: ela é lida em tempo de execução de appsettings.Segredos.json
 *     (mesma cadeia da aplicação: appsettings.json → appsettings.Segredos.json → variáveis de ambiente)
 *     ou digitada de forma oculta no prompt (opção --prompt), ou passada via --chave (menos recomendado,
 *     porque fica no histórico do terminal).
 *   - Este projeto não é referenciado pela aplicação web e não é publicado com ela.
 *
 * COMO COMPILAR E EXECUTAR (a partir da pasta desta ferramenta):
 *   dotnet run -- --descriptografar "TEXTO_CRIPTOGRAFADO"
 *   dotnet run -- --criptografar "texto em claro"              (gera no formato v2, recomendado)
 *   dotnet run -- --criptografar "texto" --legado              (gera no formato legado, compatibilidade)
 *   dotnet run -- --cifra-interna "texto" --acao C             (C = cifra, D = decifra a substituição interna)
 *   dotnet run -- --arquivo-token "nome.ext" --acao D          (D = recupera nome original do token)
 *   dotnet run -- --prompt                                      (digita a chave oculta no teclado)
 *
 * Documento de referência: "Documentos do Qoder\criptografia-e-descriptografia-labweb7.md".
 * ===================================================================================================================== */

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

// Prefixo que identifica o formato v2 (idêntico ao de CriptoDecripto.PREFIXO_CRIPTOGRAFIA_V2)
const string PREFIXO_V2 = "LABW7V2$";

// ---------------------------------------------------------------------------------------------------------------------
// 1) Interpretação dos argumentos de linha de comando
// ---------------------------------------------------------------------------------------------------------------------
string? texto = null;
string modo = "descriptografar";      // descriptografar | criptografar | cifra-interna | arquivo-token
char acaoCifra = 'D';                 // usada por cifra-interna / arquivo-token
bool usarLegado = false;              // ao criptografar, usa o formato legado (IV fixo) em vez do v2
bool chaveNoPrompt = false;
string? chavePorArgumento = null;
string baseDir = AppContext.BaseDirectory;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i].ToLowerInvariant())
    {
        case "--descriptografar":
        case "-d":
            modo = "descriptografar";
            texto = ValorDoArgumento(args, ref i);
            break;
        case "--criptografar":
        case "-c":
            modo = "criptografar";
            texto = ValorDoArgumento(args, ref i);
            break;
        case "--cifra-interna":
            modo = "cifra-interna";
            texto = ValorDoArgumento(args, ref i);
            break;
        case "--arquivo-token":
            modo = "arquivo-token";
            texto = ValorDoArgumento(args, ref i);
            break;
        case "--acao":
            acaoCifra = (ValorDoArgumento(args, ref i) ?? "D").Trim().ToUpperInvariant()[0];
            break;
        case "--legado":
            usarLegado = true;
            break;
        case "--prompt":
            chaveNoPrompt = true;
            break;
        case "--chave":
        case "--key":
            chavePorArgumento = ValorDoArgumento(args, ref i);
            break;
        case "--dir-config":
            baseDir = ValorDoArgumento(args, ref i) ?? baseDir;
            break;
        case "--ajuda":
        case "-h":
        case "--help":
            ImprimirAjuda();
            return 0;
    }
}

if (string.IsNullOrEmpty(texto))
{
    Console.WriteLine("Nada para processar. Use --ajuda para ver as opções.");
    return 1;
}

// ---------------------------------------------------------------------------------------------------------------------
// 2) Obtenção da chave AES (somente para os modos que usam AES; cifra interna/token não usam chave externa)
// ---------------------------------------------------------------------------------------------------------------------
string chaveAes = string.Empty;
if (modo is "descriptografar" or "criptografar")
{
    if (chaveNoPrompt)
    {
        Console.Write("Digite a chave AES (Secrets:myVetorDeCifras) — entrada oculta: ");
        chaveAes = LerSenhaOculta();
        Console.WriteLine();
    }
    else if (!string.IsNullOrEmpty(chavePorArgumento))
    {
        chaveAes = chavePorArgumento;
    }
    else
    {
        // Mesma cadeia de configuração da aplicação: appsettings.json → appsettings.Segredos.json → ambiente.
        // Procura o arquivo também na pasta da aplicação web quando executada do repositório.
        chaveAes = LerChaveDaConfiguracao(baseDir) ?? string.Empty;
    }

    if (string.IsNullOrEmpty(chaveAes))
    {
        Console.WriteLine("ERRO: chave AES não encontrada.");
        Console.WriteLine("Ela deve existir em appsettings.Segredos.json (Secrets:myVetorDeCifras),");
        Console.WriteLine("ou use --prompt para digitá-la, ou --chave \"valor\".");
        return 2;
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// 3) Execução do modo escolhido
// ---------------------------------------------------------------------------------------------------------------------
try
{
    switch (modo)
    {
        case "descriptografar":
            Console.WriteLine();
            Console.WriteLine("=== RESULTADO DESCRIPTOGRAFADO ===");
            Console.WriteLine(Descriptografar(texto!, chaveAes));
            break;

        case "criptografar":
            string cifrado = usarLegado
                ? CriptografarLegado(texto!, chaveAes)
                : CriptografarV2(texto!, chaveAes);
            Console.WriteLine();
            Console.WriteLine($"=== RESULTADO CRIPTOGRAFADO ({(usarLegado ? "legado" : "v2")}) ===");
            Console.WriteLine(cifrado);
            break;

        case "cifra-interna":
            Console.WriteLine();
            Console.WriteLine($"=== CIFRA INTERNA ({acaoCifra}) ===");
            Console.WriteLine(CifraInterna(texto!, acaoCifra));
            break;

        case "arquivo-token":
            Console.WriteLine();
            Console.WriteLine($"=== TOKEN DE ARQUIVO ({acaoCifra}) ===");
            Console.WriteLine(ArquivoToken(texto!, acaoCifra is 'C' or 'c'));
            break;
    }
    return 0;
}
catch (CryptographicException)
{
    Console.WriteLine("ERRO: falha ao descriptografar. Verifique se a chave está correta e se o texto");
    Console.WriteLine("é um ciphertext válido (a senha do usuário, em BCrypt, NÃO é descriptografável).");
    return 3;
}
catch (FormatException)
{
    Console.WriteLine("ERRO: o texto informado não é um Base64 válido (não parece ser um ciphertext AES).");
    Console.WriteLine("Se começar com \"$2a$\", é hash BCrypt de senha — irreversível por definição.");
    return 3;
}

// =====================================================================================================================
// Rotinas de criptografia (réplicas autossuficientes de CriptoDecripto — sem referência à aplicação web)
// =====================================================================================================================

static string Descriptografar(string cipherText, string chave)
{
    // Formato v2: LABW7V2$ + Base64(IV(16) || ciphertext)
    if (!string.IsNullOrEmpty(cipherText) && cipherText.StartsWith(PREFIXO_V2))
    {
        byte[] pacote = Convert.FromBase64String(cipherText.Substring(PREFIXO_V2.Length));
        if (pacote.Length < 17)
            throw new CryptographicException("Ciphertext v2 inválido: tamanho menor que IV + 1 bloco.");

        byte[] ivMensagem = pacote.AsSpan(0, 16).ToArray();
        byte[] corpo = pacote.AsSpan(16).ToArray();
        return AesParaString(corpo, Encoding.UTF8.GetBytes(chave), ivMensagem);
    }

    // Formato legado: Base64 puro, chave E IV fixos iguais à Secrets:myVetorDeCifras
    return AesParaString(Convert.FromBase64String(cipherText), Encoding.UTF8.GetBytes(chave), Encoding.UTF8.GetBytes(chave));
}

static string CriptografarV2(string plainText, string chave)
{
    byte[] ivMensagem = RandomNumberGenerator.GetBytes(16);
    byte[] corpo = StringParaAes(plainText, Encoding.UTF8.GetBytes(chave), ivMensagem);

    byte[] pacote = new byte[ivMensagem.Length + corpo.Length];
    Buffer.BlockCopy(ivMensagem, 0, pacote, 0, ivMensagem.Length);
    Buffer.BlockCopy(corpo, 0, pacote, ivMensagem.Length, corpo.Length);

    return PREFIXO_V2 + Convert.ToBase64String(pacote);
}

static string CriptografarLegado(string plainText, string chave)
{
    return Convert.ToBase64String(StringParaAes(plainText, Encoding.UTF8.GetBytes(chave), Encoding.UTF8.GetBytes(chave)));
}

static byte[] StringParaAes(string plainText, byte[] key, byte[] iv)
{
    using Aes aes = Aes.Create();
    aes.Key = key;
    aes.IV = iv;
    using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
    using MemoryStream ms = new();
    using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
    using (StreamWriter sw = new(cs))
    {
        sw.Write(plainText);
    }
    return ms.ToArray();
}

static string AesParaString(byte[] cipherText, byte[] key, byte[] iv)
{
    using Aes aes = Aes.Create();
    aes.Key = key;
    aes.IV = iv;
    using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
    using MemoryStream ms = new(cipherText);
    using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
    using StreamReader sr = new(cs);
    return sr.ReadToEnd();
}

/* Réplica exata de CriptoDecripto.Criptografia (cifra de substituição interna, legada) */
static string CifraInterna(string texto, char acao)
{
    const string normalKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890!@#$%&*()+-=<>:?,.;^~[]{}ÈÌãÃçÇ";
    const string criptoKey = "tu1APvyzC23DE90!@FGH$Ã%)+l-=IJKx&*(OSYw7UaQRÇo~[pZchr4LM}ÈÌãsdebçVWiBjk5NTX8#<>:?qfg,.;mn6^]{";

    if (texto == string.Empty) return texto;

    string de = (new char[] { 'c', 'C' }.Contains(acao)) ? normalKey : criptoKey;
    string para = (new char[] { 'c', 'C' }.Contains(acao)) ? criptoKey : normalKey;

    StringBuilder destino = new(texto.Length);
    foreach (char letra in texto)
    {
        int posicao = de.IndexOf(letra);
        destino.Append(posicao > 0 ? para[posicao] : letra);
    }
    return destino.ToString();
}

/* Réplica exata de CriptoDecripto.ArquivoToken (token de nome de arquivo) */
static string ArquivoToken(string nomeArquivo, bool criptografar)
{
    const string normalKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZÇçabcdefghijklmnopqrstuvwxyz1234567890@-_.()$#";
    const string criptoKey = "a7y8BuEItUOV5ck2-PxR_0S6rmMT$9WX@nYZÇbefK3gQhiAjlD.opNdqL(svG)çwF#z1H4CJ";

    string ext = Path.GetExtension(nomeArquivo);
    nomeArquivo = Path.GetFileNameWithoutExtension(nomeArquivo);
    if (nomeArquivo == string.Empty) return nomeArquivo;

    string de = criptografar ? normalKey : criptoKey;
    string para = criptografar ? criptoKey : normalKey;

    StringBuilder destino = new(nomeArquivo.Length);
    foreach (char letra in nomeArquivo)
    {
        int posicao = de.IndexOf(letra);
        destino.Append(posicao > 0 ? para[posicao] : letra);
    }
    return string.Concat(destino, ext);
}

// =====================================================================================================================
// Utilitários de apoio
// =====================================================================================================================

static string? LerChaveDaConfiguracao(string baseDir)
{
    try
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(baseDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Segredos.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? chave = configuration["Secrets:myVetorDeCifras"];
        if (!string.IsNullOrEmpty(chave)) return chave;

        // Segunda tentativa: pasta da aplicação web quando executada a partir do repositório
        string? dirRepo = ProcurarPastaDaAplicacao(baseDir);
        if (dirRepo != null)
        {
            IConfiguration configuration2 = new ConfigurationBuilder()
                .SetBasePath(dirRepo)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Segredos.json", optional: true)
                .Build();
            return configuration2["Secrets:myVetorDeCifras"];
        }
    }
    catch
    {
        // silencia falhas de leitura e deixa o chamador decidir pelo prompt
    }
    return null;
}

static string? ProcurarPastaDaAplicacao(string baseDir)
{
    // Sobe até 6 níveis procurando LabWebMvc.MVC (cenário: execução de bin\Debug\net8.0 desta ferramenta)
    DirectoryInfo? dir = new(baseDir);
    for (int i = 0; i < 6 && dir != null; i++)
    {
        string candidato = Path.Combine(dir.FullName, "LabWebMvc.MVC");
        if (Directory.Exists(candidato)) return candidato;
        dir = dir.Parent;
    }
    return null;
}

static string LerSenhaOculta()
{
    StringBuilder senha = new();
    ConsoleKeyInfo tecla;
    do
    {
        tecla = Console.ReadKey(intercept: true);
        if (tecla.Key == ConsoleKey.Backspace && senha.Length > 0)
        {
            senha.Length--;
            Console.Write("\b \b");
        }
        else if (!char.IsControl(tecla.KeyChar))
        {
            senha.Append(tecla.KeyChar);
            Console.Write('*');
        }
    } while (tecla.Key != ConsoleKey.Enter);
    return senha.ToString();
}

static string? ValorDoArgumento(string[] args, ref int i)
{
    return (i + 1 < args.Length) ? args[++i] : null;
}

static void ImprimirAjuda()
{
    Console.WriteLine(
"""
FerramentaCripto — descriptografia de campos criptografados do LabWeb7 (uso exclusivo do desenvolvedor).
ATENÇÃO: senhas de usuário são BCrypt IRREVERSÍVEL e não podem ser descriptografadas (use "Esqueci minha senha").

Uso:
  dotnet run -- --descriptografar "<ciphertext>"        decifra AES legado ou v2 (auto-detecção pelo prefixo)
  dotnet run -- --criptografar "<texto>"                cifra no formato v2 (IV aleatório — recomendado)
  dotnet run -- --criptografar "<texto>" --legado       cifra no formato legado (IV fixo — só compatibilidade)
  dotnet run -- --cifra-interna "<texto>" --acao C|D    cifra/decifra a substituição interna legada
  dotnet run -- --arquivo-token "<nome.ext>" --acao C|D gera/recupera token de nome de arquivo

Origem da chave AES (nessa ordem):
  1) --chave "<valor>"        passado por argumento (fica no histórico do terminal — menos recomendado)
  2) --prompt                 digitada de forma oculta no teclado
  3) appsettings.Segredos.json (Secrets:myVetorDeCifras) na pasta do bin ou em LabWebMvc.MVC
""");
}
