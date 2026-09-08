using System.Security.Cryptography;

namespace MileageClaims.Modules.Integrations.Services;

/// <summary>
/// Hash de contraseñas con PBKDF2 (sin paquete adicional). Las contraseñas acá son de las
/// cuentas fake del AD simulado, no credenciales reales de la compañía.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int HashSize = 32;
    private const int SaltSize = 16;

    public static (byte[] Hash, byte[] Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (hash, salt);
    }

    public static bool Verify(string password, byte[] hash, byte[] salt)
    {
        var computed = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(computed, hash);
    }
}
