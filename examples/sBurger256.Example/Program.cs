using System.Security.Cryptography;
using System.Text;

// ──────────────────────────────────────────────
//  sBurger-256  —  Encryption / Decryption Demo
// ──────────────────────────────────────────────

Console.OutputEncoding = Encoding.UTF8;

const string passphrase = "my-secret-passphrase";
const string message = "Hello, sBurger-256! This is a secret message that will be encrypted.";

Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║        sBurger-256  Demo                 ║");
Console.WriteLine("╚══════════════════════════════════════════╝");
Console.WriteLine();

// 1. Derive a 256-bit key from a passphrase using SHA-256 ---
byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(passphrase));

Console.WriteLine($"Passphrase : {passphrase}");
Console.WriteLine($"Derived key: {Convert.ToHexString(key)}");
Console.WriteLine($"Plaintext  : {message}");
Console.WriteLine();

// 2. Encrypt the message block by block (32-byte blocks) ---
byte[] plainBytes = Encoding.UTF8.GetBytes(message);
byte[] encrypted = EncryptMessage(plainBytes, key);

Console.WriteLine($"Ciphertext (hex): {Convert.ToHexString(encrypted)}");
Console.WriteLine();

// 3. Decrypt back to plaintext ---
byte[] decrypted = DecryptMessage(encrypted, key);
string recovered = Encoding.UTF8.GetString(decrypted);

Console.WriteLine($"Decrypted  : {recovered}");
Console.WriteLine($"Match      : {message == recovered}");
Console.WriteLine();

// 4. Demonstrate wrong-key decryption ---
byte[] wrongKey = SHA256.HashData(Encoding.UTF8.GetBytes("wrong-passphrase"));

Console.WriteLine($"Wrong key  : {Convert.ToHexString(wrongKey)}");
try
{
    byte[] wrongDecrypted = DecryptMessage((byte[])encrypted.Clone(), wrongKey);
    string garbled = Encoding.UTF8.GetString(wrongDecrypted);

    Console.WriteLine($"Garbled    : {garbled}");
    Console.WriteLine($"Match      : {message == garbled}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Decrypt    : failed ({ex.Message})");
    Console.WriteLine("Match      : False");
}

/// <summary>
/// Encrypts a byte array by splitting it into 32-byte blocks with PKCS7 padding.
/// </summary>
static byte[] EncryptMessage(byte[] data, byte[] key)
{
    ArgumentNullException.ThrowIfNull(data);
    ArgumentNullException.ThrowIfNull(key);

    const int blockSize = sBurger256.sBurger256.MaxBlockSize;

    // Apply PKCS7 padding so the total length is a multiple of blockSize.
    int paddingNeeded = blockSize - (data.Length % blockSize);
    byte[] padded = new byte[data.Length + paddingNeeded];
    data.CopyTo(padded, 0);
    for (int i = data.Length; i < padded.Length; i++)
    {
        padded[i] = (byte)paddingNeeded;
    }

    using var cipher = new sBurger256.sBurger256(key);

    // Encrypt each 32-byte block.
    for (int offset = 0; offset < padded.Length; offset += blockSize)
    {
        byte[] block = new byte[blockSize];
        Array.Copy(padded, offset, block, 0, blockSize);
        cipher.Encryption(block);
        Array.Copy(block, 0, padded, offset, blockSize);
    }

    return padded;
}

/// <summary>
/// Decrypts a padded ciphertext and removes PKCS7 padding.
/// </summary>
static byte[] DecryptMessage(byte[] ciphertext, byte[] key)
{
    ArgumentNullException.ThrowIfNull(ciphertext);
    ArgumentNullException.ThrowIfNull(key);

    const int blockSize = sBurger256.sBurger256.MaxBlockSize;

    if (ciphertext.Length == 0 || ciphertext.Length % blockSize != 0)
    {
        throw new ArgumentException(
            $"Ciphertext length must be a positive multiple of {blockSize} bytes.",
            nameof(ciphertext));
    }

    using var cipher = new sBurger256.sBurger256(key);

    byte[] buffer = (byte[])ciphertext.Clone();

    for (int offset = 0; offset < buffer.Length; offset += blockSize)
    {
        byte[] block = new byte[blockSize];
        Array.Copy(buffer, offset, block, 0, blockSize);
        cipher.Decryption(block);
        Array.Copy(block, 0, buffer, offset, blockSize);
    }

    return RemovePkcs7Padding(buffer, blockSize);
}

/// <summary>
/// Validates and removes PKCS7 padding from a decrypted buffer.
/// </summary>
static byte[] RemovePkcs7Padding(byte[] data, int blockSize)
{
    ArgumentNullException.ThrowIfNull(data);

    if (data.Length == 0)
    {
        throw new InvalidOperationException("Invalid PKCS7 padding.");
    }

    int paddingLength = data[^1];
    if (paddingLength <= 0 || paddingLength > blockSize || paddingLength > data.Length)
    {
        throw new InvalidOperationException("Invalid PKCS7 padding.");
    }

    for (int i = data.Length - paddingLength; i < data.Length; i++)
    {
        if (data[i] != paddingLength)
        {
            throw new InvalidOperationException("Invalid PKCS7 padding.");
        }
    }

    byte[] result = new byte[data.Length - paddingLength];
    Array.Copy(data, result, result.Length);
    return result;
}
