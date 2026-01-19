namespace OroIdentityServers.EntityFramework.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data like client secrets
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plain text string
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted string
    /// </summary>
    string Decrypt(string encryptedText);

    /// <summary>
    /// Hashes a password using BCrypt (for backward compatibility)
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hash
    /// </summary>
    bool VerifyPassword(string password, string hash);
}