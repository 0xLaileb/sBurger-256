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
byte[] decrypted = DecryptMessage(encrypted, plainBytes.Length, key);
string recovered = Encoding.UTF8.GetString(decrypted);

Console.WriteLine($"Decrypted  : {recovered}");
Console.WriteLine($"Match      : {message == recovered}");
Console.WriteLine();

// 4. Demonstrate wrong-key decryption ---
byte[] wrongKey = SHA256.HashData(Encoding.UTF8.GetBytes("wrong-passphrase"));
byte[] wrongDecrypted = DecryptMessage((byte[])encrypted.Clone(), plainBytes.Length, wrongKey);
string garbled = Encoding.UTF8.GetString(wrongDecrypted);

Console.WriteLine($"Wrong key  : {Convert.ToHexString(wrongKey)}");
Console.WriteLine($"Garbled    : {garbled}");
Console.WriteLine($"Match      : {message == garbled}");

/// <summary>
/// Encrypts a byte array by splitting it into 32-byte blocks with PKCS7 padding.
/// </summary>
static byte[] EncryptMessage(byte[] data, byte[] key)
{
    const int blockSize = sBurger256.sBurger256.MaxBlockSize;

    // Apply PKCS7 padding so the total length is a multiple of blockSize.
    int paddingNeeded = blockSize - (data.Length % blockSize);
    byte[] padded = new byte[data.Length + paddingNeeded];
    data.CopyTo(padded, 0);
    for (int i = data.Length; i < padded.Length; i++)
    {
        padded[i] = (byte)paddingNeeded;
    }

    var cipher = new sBurger256.sBurger256 { Key = key };
    cipher.GenerationSettings();

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
/// Decrypts a padded ciphertext and trims it to the original plaintext length.
/// </summary>
static byte[] DecryptMessage(byte[] ciphertext, int originalLength, byte[] key)
{
    const int blockSize = sBurger256.sBurger256.MaxBlockSize;

    var cipher = new sBurger256.sBurger256 { Key = key };
    cipher.GenerationSettings();

    byte[] buffer = (byte[])ciphertext.Clone();

    for (int offset = 0; offset < buffer.Length; offset += blockSize)
    {
        byte[] block = new byte[blockSize];
        Array.Copy(buffer, offset, block, 0, blockSize);
        cipher.Decryption(block);
        Array.Copy(block, 0, buffer, offset, blockSize);
    }

    // Trim to original length.
    byte[] result = new byte[originalLength];
    Array.Copy(buffer, result, originalLength);
    return result;
}
