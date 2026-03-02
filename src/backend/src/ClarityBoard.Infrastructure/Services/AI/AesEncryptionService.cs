using System.Security.Cryptography;
using System.Text;
using ClarityBoard.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClarityBoard.Infrastructure.Services.AI;

/// <summary>
/// AES-256-GCM symmetric encryption for sensitive data at rest (e.g. AI API keys).
/// Format: Base64( IV[12] + Ciphertext + AuthTag[16] )
/// Key is loaded from "Encryption:Key" configuration (Base64-encoded 32-byte key).
/// </summary>
public sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:Key"];
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            // Development fallback – must be overridden in production
            keyBase64 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="; // 32 zero bytes
        }

        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption:Key must be a Base64-encoded 256-bit (32-byte) key.");
    }

    public string Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);

        var iv      = new byte[12];
        var tag     = new byte[16];
        var cipher  = new byte[plaintextBytes.Length];

        RandomNumberGenerator.Fill(iv);
        aes.Encrypt(iv, plaintextBytes, cipher, tag);

        // Layout: iv(12) + cipher(n) + tag(16)
        var combined = new byte[12 + cipher.Length + 16];
        iv.CopyTo(combined, 0);
        cipher.CopyTo(combined, 12);
        tag.CopyTo(combined, 12 + cipher.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string ciphertext)
    {
        var combined = Convert.FromBase64String(ciphertext);
        if (combined.Length < 28) // 12 iv + 0 cipher + 16 tag
            throw new CryptographicException("Invalid ciphertext: too short.");

        var iv     = combined[..12];
        var tag    = combined[^16..];
        var cipher = combined[12..^16];
        var plain  = new byte[cipher.Length];

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Decrypt(iv, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }
}

