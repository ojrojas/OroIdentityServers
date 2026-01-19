using System.Security.Cryptography;
using System.Text;

namespace OroIdentityServers.EntityFramework.Services;

/// <summary>
/// Default implementation of IEncryptionService using AES encryption
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(string encryptionKey)
    {
        // Derive key and IV from the encryption key using PBKDF2
        using var deriveBytes = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(encryptionKey),
            Encoding.UTF8.GetBytes("OroIdentityServerSalt"),
            10000,
            HashAlgorithmName.SHA256);

        _key = deriveBytes.GetBytes(32); // AES-256
        _iv = deriveBytes.GetBytes(16);  // AES block size
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);

        sw.Write(plainText);
        sw.Flush();
        cs.FlushFinalBlock();

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(Convert.FromBase64String(encryptedText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch
        {
            // If decryption fails, assume it's a BCrypt hash (backward compatibility)
            return encryptedText;
        }
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        // Try BCrypt verification first
        if (BCrypt.Net.BCrypt.Verify(password, hash))
            return true;

        // If BCrypt fails, try decryption and comparison (for encrypted secrets)
        try
        {
            var decrypted = Decrypt(hash);
            return password == decrypted;
        }
        catch
        {
            return false;
        }
    }
}