using System.Windows;
using SmartXWpf.Services;
using SmartXWpf.ViewModels;

namespace SmartXWpf.Views;

public partial class LoginView : Window
{
    private readonly LoginViewModel _viewModel = new();
    private bool _signingIn;

    public LoginView()
    {
        InitializeComponent();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        if (_signingIn)
            return;

        _signingIn = true;
        try
        {
            await _viewModel.LoginAsync(
                UsernameBox.Text,
                PasswordBox.Password);

            OpenMain(isGuest: false);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(ex.Message, "Login failed");
        }
        finally
        {
            _signingIn = false;
        }
    }

    private void Guest_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ContinueAsGuest();
        OpenMain(isGuest: true);
    }

    private void OpenMain(bool isGuest)
    {
        var main = new MainWindow();
        if (isGuest)
            main.Title = "SmartX Farm Monitoring — Guest Mode";

        main.Show();
        Close();
    }
}
