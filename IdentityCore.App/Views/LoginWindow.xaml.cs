using System.Windows;
using IdentityCore.App.Services;

namespace IdentityCore.App.Views;

public partial class LoginWindow : Window
{
    private readonly AuthenticationService _authenticationService = new();

    public LoginWindow()
    {
        InitializeComponent();
        UsernameTextBox.Focus();
        UsernameTextBox.SelectAll();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Text = string.Empty;

        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        var loginSuccess = _authenticationService.Login(username, password);

        if (!loginSuccess)
        {
            ErrorTextBlock.Text = "Nieprawidłowy login lub hasło.";
            PasswordBox.Clear();
            PasswordBox.Focus();
            return;
        }

        var mainWindow = new MainWindow();
        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();

        Close();
    }
}
