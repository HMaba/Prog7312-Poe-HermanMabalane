namespace SmartXWpf.Services;

public static class AuthSession
{
    public static string? IdToken { get; internal set; }
    public static string? RefreshToken { get; internal set; }
    public static DateTime ExpiresAtUtc { get; internal set; }
    public static string? Email { get; internal set; }

    public static bool IsSignedIn => !string.IsNullOrWhiteSpace(IdToken);

    public static void Clear()
    {
        IdToken = null;
        RefreshToken = null;
        ExpiresAtUtc = DateTime.MinValue;
        Email = null;
    }
}
