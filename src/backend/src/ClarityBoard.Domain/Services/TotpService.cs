using System.Security.Cryptography;

namespace ClarityBoard.Domain.Services;

/// <summary>
/// TOTP (Time-based One-Time Password) service implementing RFC 6238.
/// Uses HMAC-SHA1 with 30-second time steps and 6-digit codes.
/// </summary>
public static class TotpService
{
    private const int SecretLength = 20; // 160 bits
    private const int TimeStepSeconds = 30;
    private const int CodeDigits = 6;
    private const int CodeModulus = 1_000_000; // 10^6
    private const int LookBackWindow = 1; // Check current step + 1 previous

    // Base32 alphabet (RFC 4648)
    private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// Generates a cryptographically secure 20-byte random secret.
    /// </summary>
    public static byte[] GenerateSecret()
    {
        return RandomNumberGenerator.GetBytes(SecretLength);
    }

    /// <summary>
    /// Encodes a byte array to a Base32 string (RFC 4648, no padding).
    /// </summary>
    public static string ToBase32(byte[] data)
    {
        if (data.Length == 0)
            return string.Empty;

        var output = new char[(data.Length * 8 + 4) / 5];
        var outputIndex = 0;
        var buffer = 0;
        var bitsInBuffer = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsInBuffer += 8;

            while (bitsInBuffer >= 5)
            {
                bitsInBuffer -= 5;
                output[outputIndex++] = Base32Chars[(buffer >> bitsInBuffer) & 0x1F];
            }
        }

        if (bitsInBuffer > 0)
        {
            output[outputIndex++] = Base32Chars[(buffer << (5 - bitsInBuffer)) & 0x1F];
        }

        return new string(output, 0, outputIndex);
    }

    /// <summary>
    /// Decodes a Base32 string back to a byte array.
    /// </summary>
    public static byte[] FromBase32(string base32)
    {
        if (string.IsNullOrEmpty(base32))
            return [];

        // Remove padding if present
        base32 = base32.TrimEnd('=').ToUpperInvariant();

        var byteCount = base32.Length * 5 / 8;
        var output = new byte[byteCount];
        var buffer = 0;
        var bitsInBuffer = 0;
        var outputIndex = 0;

        foreach (var c in base32)
        {
            var value = Base32Chars.IndexOf(c);
            if (value < 0)
                throw new FormatException($"Invalid Base32 character: '{c}'");

            buffer = (buffer << 5) | value;
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                bitsInBuffer -= 8;
                output[outputIndex++] = (byte)(buffer >> bitsInBuffer);
            }
        }

        return output;
    }

    /// <summary>
    /// Generates a 6-digit TOTP code for the given secret and time step counter.
    /// Implements RFC 6238 / RFC 4226 (HOTP with time-based counter).
    /// </summary>
    public static string GenerateCode(byte[] secret, long timeStep)
    {
        // Convert counter to big-endian 8-byte array
        var counterBytes = new byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(timeStep & 0xFF);
            timeStep >>= 8;
        }

        // HMAC-SHA1
        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);

        // Dynamic truncation (RFC 4226 Section 5.3)
        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var otp = binaryCode % CodeModulus;
        return otp.ToString().PadLeft(CodeDigits, '0');
    }

    /// <summary>
    /// Validates a TOTP code against the given secret.
    /// Checks the current time step and the previous time step (look-back window = 1).
    /// </summary>
    public static bool ValidateCode(byte[] secret, string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != CodeDigits)
            return false;

        var currentTimeStep = GetCurrentTimeStep();

        // Check current step and look-back window
        for (var i = 0; i <= LookBackWindow; i++)
        {
            var expectedCode = GenerateCode(secret, currentTimeStep - i);
            if (CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(expectedCode),
                System.Text.Encoding.UTF8.GetBytes(code)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the current UNIX time step (counter) for TOTP.
    /// </summary>
    public static long GetCurrentTimeStep()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TimeStepSeconds;
    }

    /// <summary>
    /// Generates a provisioning URI for QR code generation.
    /// Format: otpauth://totp/{issuer}:{account}?secret={base32}&issuer={issuer}
    /// </summary>
    public static string GenerateQrCodeUri(string email, string base32Secret, string issuer = "ClarityBoard")
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={base32Secret}&issuer={encodedIssuer}";
    }

    /// <summary>
    /// Generates a set of random alphanumeric recovery codes.
    /// </summary>
    public static List<string> GenerateRecoveryCodes(int count = 10, int length = 8)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var codes = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var code = new char[length];
            for (var j = 0; j < length; j++)
            {
                code[j] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            }
            codes.Add(new string(code));
        }

        return codes;
    }

    /// <summary>
    /// Hashes a recovery code using SHA-256 for secure storage.
    /// </summary>
    public static string HashRecoveryCode(string code)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code.ToUpperInvariant()));
        return Convert.ToHexStringLower(bytes);
    }
}
