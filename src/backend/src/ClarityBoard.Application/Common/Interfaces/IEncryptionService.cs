namespace ClarityBoard.Application.Common.Interfaces;

/// <summary>
/// AES-256-GCM symmetric encryption service for sensitive data at rest
/// (e.g. AI provider API keys).
/// </summary>
public interface IEncryptionService
{
    /// <summary>Encrypts plaintext and returns a Base64-encoded ciphertext (includes IV + auth tag).</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypts a Base64-encoded ciphertext produced by <see cref="Encrypt"/>.</summary>
    string Decrypt(string ciphertext);
}

