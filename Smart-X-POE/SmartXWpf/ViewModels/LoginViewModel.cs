using SmartXWpf.Services;

namespace SmartXWpf.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly AuthenticationService _authService = new();

    /// <summary>
    /// Signs in with Firebase email/password. Throws ArgumentException / InvalidOperationException
    /// with a user-friendly message when sign-in fails.
    /// </summary>
    public Task LoginAsync(string email, string password) =>
        _authService.SignInAsync(email, password);

    public void ContinueAsGuest() => _authService.SignOut();
}
