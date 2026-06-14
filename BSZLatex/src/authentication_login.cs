public bool Login(string username, string password)
{
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
        return false;
    }

    var user = _userRepository.GetActiveUserByUsername(username.Trim());

    if (user == null) {
        return false;
    }

    if (user.PasswordHash != password) {
        return false;
    }

    CurrentUser = user;
    return true;
}