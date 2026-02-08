using System.Text;

namespace sBurger256.Tests;

public class sBurger256Tests
{
    // A known 32-byte key used across multiple tests.
    private static readonly byte[] TestKey =
        Encoding.UTF8.GetBytes("TESTKEY_TESTKEY_TESTKEY_TESTKEY_"); // exactly 32 bytes

    private static readonly byte[] AltKey =
        Encoding.UTF8.GetBytes("ALTKEY__ALTKEY__ALTKEY__ALTKEY__"); // exactly 32 bytes

    /// <summary>
    /// Creates and initializes a cipher with the given key.
    /// </summary>
    private static sBurger256 CreateCipher(byte[] key)
    {
        var cipher = new sBurger256 { Key = key };
        cipher.GenerationSettings();
        return cipher;
    }

    [Fact]
    public void Key_SetNull_ThrowsArgumentNullException()
    {
        var cipher = new sBurger256();
        Assert.Throws<ArgumentNullException>(() => cipher.Key = null!);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(33)]
    [InlineData(64)]
    public void Key_SetWrongLength_ThrowsArgumentException(int length)
    {
        var cipher = new sBurger256();
        var badKey = new byte[length];
        Assert.Throws<ArgumentException>(() => cipher.Key = badKey);
    }

    [Fact]
    public void Key_Set32Bytes_Succeeds()
    {
        var cipher = new sBurger256();
        cipher.Key = TestKey;
        Assert.Equal(TestKey, cipher.Key);
    }

    [Fact]
    public void Key_SetCopiesArray_ExternalMutationDoesNotAffectCipher()
    {
        var cipher = new sBurger256();
        var keyCopy = (byte[])TestKey.Clone();
        cipher.Key = keyCopy;

        // Mutate the original array.
        keyCopy[0] ^= 0xFF;

        // Internal state should remain unchanged.
        Assert.Equal(TestKey, cipher.Key);
    }

    [Fact]
    public void GenerationSettings_WithoutKey_ThrowsInvalidOperationException()
    {
        var cipher = new sBurger256();
        // Key is all zeroes by default (never set by the caller).
        Assert.Throws<InvalidOperationException>(() => cipher.GenerationSettings());
    }

    [Fact]
    public void GenerationSettings_WithValidKey_DoesNotThrow()
    {
        var cipher = new sBurger256 { Key = TestKey };
        var exception = Record.Exception(() => cipher.GenerationSettings());
        Assert.Null(exception);
    }

    [Fact]
    public void Encryption_BeforeGenerationSettings_ThrowsInvalidOperationException()
    {
        var cipher = new sBurger256 { Key = TestKey };
        Assert.Throws<InvalidOperationException>(() => cipher.Encryption(new byte[16]));
    }

    [Fact]
    public void Decryption_BeforeGenerationSettings_ThrowsInvalidOperationException()
    {
        var cipher = new sBurger256 { Key = TestKey };
        Assert.Throws<InvalidOperationException>(() => cipher.Decryption(new byte[16]));
    }

    [Fact]
    public void Encryption_NullData_ThrowsArgumentNullException()
    {
        var cipher = CreateCipher(TestKey);
        Assert.Throws<ArgumentNullException>(() => cipher.Encryption(null!));
    }

    [Fact]
    public void Decryption_NullData_ThrowsArgumentNullException()
    {
        var cipher = CreateCipher(TestKey);
        Assert.Throws<ArgumentNullException>(() => cipher.Decryption(null!));
    }

    [Fact]
    public void Encryption_EmptyData_ThrowsArgumentException()
    {
        var cipher = CreateCipher(TestKey);
        Assert.Throws<ArgumentException>(() => cipher.Encryption([]));
    }

    [Fact]
    public void Decryption_EmptyData_ThrowsArgumentException()
    {
        var cipher = CreateCipher(TestKey);
        Assert.Throws<ArgumentException>(() => cipher.Decryption([]));
    }

    [Fact]
    public void Encryption_DataExceedsMaxBlock_ThrowsArgumentException()
    {
        var cipher = CreateCipher(TestKey);
        Assert.Throws<ArgumentException>(() => cipher.Encryption(new byte[33]));
    }

    [Fact]
    public void Decryption_DataExceedsMaxBlock_ThrowsArgumentException()
    {
        var cipher = CreateCipher(TestKey);
        Assert.Throws<ArgumentException>(() => cipher.Decryption(new byte[33]));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(32)]
    public void EncryptThenDecrypt_ReturnsOriginalPlaintext(int blockSize)
    {
        var cipher = CreateCipher(TestKey);
        var plaintext = new byte[blockSize];
        Random.Shared.NextBytes(plaintext);
        var original = (byte[])plaintext.Clone();

        cipher.Encryption(plaintext);
        cipher.Decryption(plaintext);

        Assert.Equal(original, plaintext);
    }

    [Fact]
    public void EncryptThenDecrypt_AllZeroes_ReturnsOriginal()
    {
        var cipher = CreateCipher(TestKey);
        var data = new byte[32];
        var original = (byte[])data.Clone();

        cipher.Encryption(data);
        cipher.Decryption(data);

        Assert.Equal(original, data);
    }

    [Fact]
    public void EncryptThenDecrypt_AllOnes_ReturnsOriginal()
    {
        var cipher = CreateCipher(TestKey);
        var data = Enumerable.Repeat((byte)0xFF, 32).ToArray();
        var original = (byte[])data.Clone();

        cipher.Encryption(data);
        cipher.Decryption(data);

        Assert.Equal(original, data);
    }

    [Fact]
    public void EncryptThenDecrypt_Utf8Text_ReturnsOriginal()
    {
        var cipher = CreateCipher(TestKey);
        var text = "Hello, sBurger-256 cipher test!!"; // exactly 32 bytes in UTF-8
        var data = Encoding.UTF8.GetBytes(text);
        Assert.Equal(32, data.Length);

        cipher.Encryption(data);
        cipher.Decryption(data);

        Assert.Equal(text, Encoding.UTF8.GetString(data));
    }

    [Fact]
    public void Encryption_ProducesDifferentOutput()
    {
        var cipher = CreateCipher(TestKey);
        var plaintext = Encoding.UTF8.GetBytes("sBurger-256 test data block!!!__"); // 32 bytes
        var original = (byte[])plaintext.Clone();

        cipher.Encryption(plaintext);

        Assert.NotEqual(original, plaintext);
    }

    [Fact]
    public void Encryption_SameKeyAndPlaintext_ProducesSameCiphertext()
    {
        var cipher = CreateCipher(TestKey);

        var data1 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var data2 = (byte[])data1.Clone();

        cipher.Encryption(data1);
        cipher.Encryption(data2);

        Assert.Equal(data1, data2);
    }

    [Fact]
    public void Encryption_DifferentKeys_ProduceDifferentCiphertext()
    {
        var cipher1 = CreateCipher(TestKey);
        var cipher2 = CreateCipher(AltKey);

        var data1 = new byte[] { 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42 };
        var data2 = (byte[])data1.Clone();

        cipher1.Encryption(data1);
        cipher2.Encryption(data2);

        Assert.NotEqual(data1, data2);
    }

    [Fact]
    public void Decryption_WithWrongKey_DoesNotRecoverPlaintext()
    {
        var cipherEnc = CreateCipher(TestKey);
        var cipherDec = CreateCipher(AltKey);

        var plaintext = Encoding.UTF8.GetBytes("Secret message goes here!_______"); // 32 bytes
        var original = (byte[])plaintext.Clone();

        cipherEnc.Encryption(plaintext);
        cipherDec.Decryption(plaintext);

        Assert.NotEqual(original, plaintext);
    }

    [Fact]
    public void Encryption_AfterKeyChange_RequiresNewGenerationSettings()
    {
        var cipher = CreateCipher(TestKey);

        // Encryption works after initial setup.
        cipher.Encryption([0x01]);

        // Change the key — settings should be reset.
        cipher.Key = AltKey;

        Assert.Throws<InvalidOperationException>(() => cipher.Encryption([0x01]));
    }

    [Fact]
    public void Encryption_AfterKeyChangeAndRegeneration_WorksCorrectly()
    {
        var cipher = CreateCipher(TestKey);

        // Change key and regenerate.
        cipher.Key = AltKey;
        cipher.GenerationSettings();

        var data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var original = (byte[])data.Clone();

        cipher.Encryption(data);
        cipher.Decryption(data);

        Assert.Equal(original, data);
    }

    [Fact]
    public void MultipleBlocks_EncryptAndDecrypt_ReturnsOriginal()
    {
        var cipher = CreateCipher(TestKey);
        var fullMessage = Encoding.UTF8.GetBytes("This is a longer message that needs to be split into 32-byte chunks for encryption!____padding_");

        // Process in 32-byte blocks.
        var blockCount = (fullMessage.Length + 31) / 32;
        var encrypted = new byte[blockCount * 32];
        Array.Copy(fullMessage, encrypted, fullMessage.Length);

        for (var i = 0; i < blockCount; i++)
        {
            var block = new byte[32];
            Array.Copy(encrypted, i * 32, block, 0, 32);
            cipher.Encryption(block);
            Array.Copy(block, 0, encrypted, i * 32, 32);
        }

        // Decrypt.
        for (var i = 0; i < blockCount; i++)
        {
            var block = new byte[32];
            Array.Copy(encrypted, i * 32, block, 0, 32);
            cipher.Decryption(block);
            Array.Copy(block, 0, encrypted, i * 32, 32);
        }

        var decrypted = new byte[fullMessage.Length];
        Array.Copy(encrypted, decrypted, fullMessage.Length);

        Assert.Equal(fullMessage, decrypted);
    }

    [Fact]
    public void KeyLength_Is32()
    {
        Assert.Equal(32, sBurger256.KeyLength);
    }

    [Fact]
    public void MaxBlockSize_EqualsKeyLength()
    {
        Assert.Equal(sBurger256.KeyLength, sBurger256.MaxBlockSize);
    }
}
