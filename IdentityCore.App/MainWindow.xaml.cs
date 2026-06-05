using System.Windows;
using IdentityCore.App.ViewModels;
using IdentityCore.App.Views;

namespace IdentityCore.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var loginWindow = new LoginWindow();
        Application.Current.MainWindow = loginWindow;
        loginWindow.Show();

        Close();
    }
}
