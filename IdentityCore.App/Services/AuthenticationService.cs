using IdentityCore.App.Data;
using IdentityCore.App.Models;

namespace IdentityCore.App.Services;

public class AuthenticationService
{
    private readonly UserRepository _userRepository = new();

    public User? CurrentUser { get; private set; }

    public bool Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var user = _userRepository.GetActiveUserByUsername(username.Trim());

        if (user == null)
        {
            return false;
        }

        // Na potrzeby projektu hasło jest demonstracyjne.
        // W dokumentacji można zaznaczyć, że produkcyjnie należałoby użyć hashowania.
        if (user.PasswordHash != password)
        {
            return false;
        }

        CurrentUser = user;
        return true;
    }
}