using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace TallyJ4.Application.Services.Auth;

public class EncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["Encryption:Key"];
        if (string.IsNullOrEmpty(keyString))
        {
            throw new InvalidOperationException("Encryption key is not configured. Please set 'Encryption:Key' in appsettings.json");
        }

        // Ensure key is exactly 32 bytes (256 bits) for AES-256
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));
        }

        using var aesGcm = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Combine nonce, tag, and cipher text
        var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));
        }

        var encryptedBytes = Convert.FromBase64String(encryptedText);
        var minLength = AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize + 1;

        if (encryptedBytes.Length < minLength)
        {
            throw new ArgumentException("Invalid encrypted data", nameof(encryptedText));
        }

        using var aesGcm = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);

        // Extract nonce, tag, and cipher text
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var cipherBytes = new byte[encryptedBytes.Length - nonce.Length - tag.Length];

        Buffer.BlockCopy(encryptedBytes, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(encryptedBytes, nonce.Length, tag, 0, tag.Length);
        Buffer.BlockCopy(encryptedBytes, nonce.Length + tag.Length, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}