namespace sBurger256;

using System.Runtime.CompilerServices;

/// <summary>
/// Implements the sBurger-256 symmetric block cipher.
/// <para>
/// Key size: 256 bits (32 bytes). Block size: 1..32 bytes.
/// Each byte undergoes one round of substitution-permutation transformations
/// derived from the key.
/// </para>
/// </summary>
public class sBurger256
{
    /// <summary>
    /// Required key length in bytes (256 bits).
    /// </summary>
    public const int KeyLength = 32;

    /// <summary>
    /// Maximum data block size in bytes that can be encrypted/decrypted in a single call.
    /// </summary>
    public const int MaxBlockSize = KeyLength;

    private byte[] _key = new byte[KeyLength];
    private bool _settingsGenerated;

    private readonly int[] _b = new int[4];
    private readonly int[] _f = new int[4];

    /// <summary>
    /// Gets or sets the 256-bit encryption key.
    /// <para>
    /// The key must be exactly <see cref="KeyLength"/> (32) bytes long.
    /// Setting a new key resets internal cipher settings; call
    /// <see cref="GenerationSettings"/> again before encrypting or decrypting.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the value length is not 32 bytes.</exception>
    public byte[] Key
    {
        get => _key;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length != KeyLength)
            {
                throw new ArgumentException(
                    $"Key must be exactly {KeyLength} bytes long, but was {value.Length}.",
                    nameof(value));
            }

            value.CopyTo(_key.AsSpan());
            _settingsGenerated = false;
        }
    }

    /// <summary>
    /// Generates the internal cipher settings derived from the current <see cref="Key"/>.
    /// <para>
    /// Must be called once after setting the key and before any
    /// <see cref="Encryption"/> or <see cref="Decryption"/> calls.
    /// If the key changes, call this method again.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the key has not been set (all zeroes).
    /// </exception>
    public void GenerationSettings()
    {
        EnsureKeyIsSet();

        var sumBytes = DefaultTools.SumBytes(_key).ToString();
        var positionParameters = CharToDigit(sumBytes[^1]) + CharToDigit(sumBytes[^2]);

        Span<int> bits = stackalloc int[8];
        DefaultTools.ByteToBits(_key[positionParameters], bits);
        var type = _key[positionParameters] % 2 != 0 ? 0 : 1;

        for (var i = 0; i < _b.Length; i++)
        {
            _b[i] = CharToDigit(sumBytes[i]) + DefaultTools.CountBits(bits, type, 2 * i, 8 - (7 - 2 * i));
        }

        for (var i = 1; i <= _f.Length; i++)
        {
            _f[i - 1] = CharToDigit(DefaultTools.SumBytes(_key.AsSpan(8 * i - 8, 8)).ToString()[0]);
        }

        _settingsGenerated = true;
    }

    /// <summary>
    /// Encrypts a data block in place using the sBurger-256 algorithm.
    /// <para>
    /// The data array is modified in place and also returned for convenience.
    /// Length must be between 1 and <see cref="MaxBlockSize"/> (32) bytes inclusive.
    /// </para>
    /// </summary>
    /// <param name="data">The plaintext block to encrypt (modified in place).</param>
    /// <returns>The same <paramref name="data"/> array, now containing ciphertext.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> is empty or exceeds <see cref="MaxBlockSize"/> bytes.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="GenerationSettings"/> has not been called.</exception>
    public byte[] Encryption(byte[] data)
    {
        ValidateDataBlock(data);

        if (_f[1] % 2 != 0)
        {
            Array.Reverse(data);
        }

        for (var i = 0; i < data.Length; i++)
        {
            if (_b[2] % 2 == 0)
            {
                data[i] = CryptTools.Xor(data[i], (byte)_b[2]);
            }

            if (_b[0] % 2 != 0)
            {
                data[i] = CryptTools.InversionByte(data[i]);
            }

            if (_f[0] % 8 != 0)
            {
                data[i] = CryptTools.RotateLeft(data[i], _f[0] % 8);
            }

            data[i] = CryptTools.Xor(data[i], _key[i]);
            if (_b[1] % 2 != 0)
            {
                data[i] = CryptTools.InversionByte(data[i]);
            }

            if (_f[2] % 8 != 0)
            {
                data[i] = CryptTools.RotateRight(data[i], _f[2] % 8);
            }

            if (_b[3] % 2 == 0)
            {
                data[i] = CryptTools.Xor(data[i], (byte)_b[3]);
            }
        }

        if (_f[3] % 2 != 0)
        {
            Array.Reverse(data);
        }

        return data;
    }

    /// <summary>
    /// Decrypts a data block in place using the sBurger-256 algorithm.
    /// <para>
    /// The data array is modified in place and also returned for convenience.
    /// Length must be between 1 and <see cref="MaxBlockSize"/> (32) bytes inclusive.
    /// </para>
    /// </summary>
    /// <param name="data">The ciphertext block to decrypt (modified in place).</param>
    /// <returns>The same <paramref name="data"/> array, now containing plaintext.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> is empty or exceeds <see cref="MaxBlockSize"/> bytes.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="GenerationSettings"/> has not been called.</exception>
    public byte[] Decryption(byte[] data)
    {
        ValidateDataBlock(data);

        if (_f[3] % 2 != 0)
        {
            Array.Reverse(data);
        }

        for (var i = 0; i < data.Length; i++)
        {
            if (_b[3] % 2 == 0)
            {
                data[i] = CryptTools.Xor(data[i], (byte)_b[3]);
            }

            if (_f[2] % 8 != 0)
            {
                data[i] = CryptTools.RotateLeft(data[i], _f[2] % 8);
            }

            if (_b[1] % 2 != 0)
            {
                data[i] = CryptTools.InversionByte(data[i]);
            }

            data[i] = CryptTools.Xor(data[i], _key[i]);
            if (_f[0] % 8 != 0)
            {
                data[i] = CryptTools.RotateRight(data[i], _f[0] % 8);
            }

            if (_b[0] % 2 != 0)
            {
                data[i] = CryptTools.InversionByte(data[i]);
            }

            if (_b[2] % 2 == 0)
            {
                data[i] = CryptTools.Xor(data[i], (byte)_b[2]);
            }
        }

        if (_f[1] % 2 != 0)
        {
            Array.Reverse(data);
        }

        return data;
    }

    /// <summary>
    /// Converts a digit character ('0'..'9') to its integer value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CharToDigit(char c) => c - '0';

    /// <summary>
    /// Throws if the key is all zeroes (i.e. never set by the caller).
    /// </summary>
    private void EnsureKeyIsSet()
    {
        if (_key.AsSpan().IndexOfAnyExcept((byte)0) < 0)
        {
            throw new InvalidOperationException(
                "Key has not been set. Assign a 256-bit key before calling GenerationSettings().");
        }
    }

    /// <summary>
    /// Validates common preconditions for encryption/decryption operations.
    /// </summary>
    private void ValidateDataBlock(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0 || data.Length > MaxBlockSize)
        {
            throw new ArgumentException(
                $"Data block must be between 1 and {MaxBlockSize} bytes, but was {data.Length}.",
                nameof(data));
        }

        if (!_settingsGenerated)
        {
            throw new InvalidOperationException(
                "Cipher settings have not been generated. Call GenerationSettings() first.");
        }
    }

    /// <summary>
    /// Key-analysis utilities used during settings generation.
    /// </summary>
    private static class DefaultTools
    {
        /// <summary>
        /// Counts the number of bits equal to <paramref name="type"/> in the given range.
        /// </summary>
        public static int CountBits(ReadOnlySpan<int> bits, int type, int start, int stop)
        {
            var result = 0;
            for (var i = start; i <= stop; i++)
            {
                if (bits[i] == type)
                {
                    result++;
                }
            }
            return result;
        }

        /// <summary>
        /// Sums all byte values in the span.
        /// </summary>
        public static int SumBytes(ReadOnlySpan<byte> bytes)
        {
            var result = 0;
            for (var i = 0; i < bytes.Length; i++)
            {
                result += bytes[i];
            }

            return result;
        }

        /// <summary>
        /// Decomposes a byte into 8 individual bit values (MSB first) and writes them into
        /// the provided <paramref name="result"/> span.
        /// </summary>
        public static void ByteToBits(byte value, Span<int> result)
        {
            for (var b = 0; b < 8; b++)
            {
                result[b] = (value >> (7 - b)) & 1;
            }
        }
    }

    /// <summary>
    /// Low-level bitwise cryptographic primitives.
    /// </summary>
    private static class CryptTools
    {
        /// <summary>Rotates a byte left by <paramref name="s"/> positions.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte RotateLeft(byte value, int s)
            => (byte)((value << s) | (value >> (8 - s)));

        /// <summary>Rotates a byte right by <paramref name="s"/> positions.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte RotateRight(byte value, int s)
            => (byte)((value >> s) | (value << (8 - s)));

        /// <summary>XORs two bytes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Xor(byte a, byte b)
            => (byte)(a ^ b);

        /// <summary>Bitwise inverts a byte (~byte).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte InversionByte(byte value)
            => (byte)~value;
    }
}
