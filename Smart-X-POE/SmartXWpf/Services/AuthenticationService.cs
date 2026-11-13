using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartXWpf.Services;

public sealed class AuthenticationService
{
    // Firebase Web API keys identify a project; they are not service-account credentials.
    private const string FirebaseApiKey = "AIzaSyAiP5t-52W9Q3Z9kFKc9vGbx4oi5nB4nCM";

    private static readonly HttpClient FirebaseHttp = new() { Timeout = TimeSpan.FromSeconds(20) };
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task SignInAsync(string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Enter your email address.", nameof(email));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Enter your password.", nameof(password));

        var endpoint = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FirebaseApiKey}";

        string body;
        bool ok;
        try
        {
            using var response = await FirebaseHttp.PostAsJsonAsync(
                endpoint, new FirebaseSignInRequest(email.Trim(), password, true), ct);
            body = await response.Content.ReadAsStringAsync(ct);
            ok = response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException("Could not reach Firebase. Check your internet connection and try again.");
        }

        if (!ok)
        {
            var code = "AUTHENTICATION_FAILED";
            try
            {
                using var document = JsonDocument.Parse(body);
                code = document.RootElement.GetProperty("error").GetProperty("message").GetString() ?? code;
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException) { }

            throw new InvalidOperationException(FriendlyFirebaseError(code));
        }

        var result = JsonSerializer.Deserialize<FirebaseSignInResponse>(body, Json);
        if (result is null || string.IsNullOrWhiteSpace(result.IdToken))
            throw new InvalidOperationException("Sign-in succeeded but Firebase did not return an ID token.");

        AuthSession.IdToken = result.IdToken;
        AuthSession.RefreshToken = result.RefreshToken;
        AuthSession.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(ParseSeconds(result.ExpiresIn));
        AuthSession.Email = result.Email ?? email.Trim();
    }

    public void SignOut() => AuthSession.Clear();

    
    /// Returns the current ID token (or null when signed out / guest). Firebase ID tokens last about an hour so a token that is close to expiry is silently refreshed with the stored refresh token.
    
    public static async Task<string?> GetValidTokenAsync(CancellationToken ct = default)
    {
        if (!AuthSession.IsSignedIn)
            return null;

        if (!IsNearExpiry())
            return AuthSession.IdToken;

        await RefreshLock.WaitAsync(ct);
        try
        {
            if (!AuthSession.IsSignedIn)
                return null;
            if (!IsNearExpiry() || string.IsNullOrWhiteSpace(AuthSession.RefreshToken))
                return AuthSession.IdToken;

            try
            {
                var endpoint = $"https://securetoken.googleapis.com/v1/token?key={FirebaseApiKey}";
                using var form = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = AuthSession.RefreshToken!
                });
                using var response = await FirebaseHttp.PostAsync(endpoint, form, ct);
                if (!response.IsSuccessStatusCode)
                    return AuthSession.IdToken; 
                var body = await response.Content.ReadAsStringAsync(ct);
                var refreshed = JsonSerializer.Deserialize<FirebaseRefreshResponse>(body, Json);
                if (refreshed is null || string.IsNullOrWhiteSpace(refreshed.IdToken))
                    return AuthSession.IdToken;

                AuthSession.IdToken = refreshed.IdToken;
                if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken))
                    AuthSession.RefreshToken = refreshed.RefreshToken;
                AuthSession.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(ParseSeconds(refreshed.ExpiresIn));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
               
            }

            return AuthSession.IdToken;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static bool IsNearExpiry() =>
        DateTime.UtcNow >= AuthSession.ExpiresAtUtc - TimeSpan.FromMinutes(2);

    private static int ParseSeconds(string? value) =>
        int.TryParse(value, out var seconds) && seconds > 0 ? seconds : 3600;

    private static string FriendlyFirebaseError(string code) => code switch
    {
        "EMAIL_NOT_FOUND" or "INVALID_LOGIN_CREDENTIALS" => "No account matches that email and password.",
        "INVALID_PASSWORD" => "The password is incorrect.",
        "USER_DISABLED" => "This account has been disabled.",
        "OPERATION_NOT_ALLOWED" => "Email/password sign-in is disabled. Enable Email/Password in Firebase Console → Authentication → Sign-in method.",
        "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many sign-in attempts. Try again later.",
        "INVALID_EMAIL" => "Enter a valid email address.",
        "API_KEY_INVALID" => "The Firebase Web API key was rejected. Verify the Firebase project configuration.",
        _ => $"Sign-in failed ({code}). Check the Firebase Authentication settings and account."
    };

    private sealed record FirebaseSignInRequest(string Email, string Password, bool ReturnSecureToken);

    private sealed record FirebaseSignInResponse(
        string IdToken, string Email, string RefreshToken, string LocalId, string ExpiresIn);

    private sealed record FirebaseRefreshResponse(
        [property: JsonPropertyName("id_token")] string IdToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] string ExpiresIn);
}
