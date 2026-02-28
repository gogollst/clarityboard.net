using System.Security.Cryptography;
using System.Text;

namespace ClarityBoard.Infrastructure.Services;

public static class WebhookSignatureValidator
{
    /// <summary>
    /// Validates an HMAC-SHA256 signature against the payload and secret.
    /// Supports both "sha256=..." prefixed format and raw hex format.
    /// </summary>
    public static bool ValidateHmacSha256(string payload, string secret, string signature)
    {
        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signature))
            return false;

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var computedHash = hmac.ComputeHash(payloadBytes);
        var computedHex = Convert.ToHexStringLower(computedHash);

        // Strip "sha256=" prefix if present
        var signatureHex = signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            ? signature["sha256=".Length..]
            : signature;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(signatureHex.ToLowerInvariant()));
    }

    /// <summary>
    /// Computes an HMAC-SHA256 hash of the payload using the given secret.
    /// Returns the hex-encoded hash string.
    /// </summary>
    public static string ComputeHmacSha256(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexStringLower(hash);
    }
}
