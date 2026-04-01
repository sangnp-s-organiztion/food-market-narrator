namespace food_market_narrator_api.Models;

public enum UserRole
{
    admin,
    saler
}

public static class UserRoleParser
{
    public static bool TryParse(string? value, out UserRole role)
    {
        return Enum.TryParse(value?.Trim(), ignoreCase: true, out role);
    }

    public static string NormalizeOrThrow(string? value)
    {
        if (!TryParse(value, out var role))
        {
            throw new ArgumentException("Role must be 'admin' or 'saler'.");
        }

        return role.ToString();
    }
}
