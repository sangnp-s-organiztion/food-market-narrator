using System.Security.Cryptography;

namespace food_market_narrator_api.Helpers;

public static class PasswordHasher
{
    private const string Algorithm = "PBKDF2";
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(stored))
        {
            return false;
        }

        var parts = stored.Split('$');
        if (parts.Length != 4 || !string.Equals(parts[0], Algorithm, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool IsHashed(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.StartsWith($"{Algorithm}$", StringComparison.Ordinal);
    }
}
