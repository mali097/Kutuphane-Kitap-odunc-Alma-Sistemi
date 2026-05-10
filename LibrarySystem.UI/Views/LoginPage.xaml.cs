using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService;
    private bool _showPassword;

    public LoginPage()
    {
        InitializeComponent();
        _authService = new AuthService();
    }

    private void TogglePassword_Clicked(object sender, EventArgs e)
    {
        _showPassword = !_showPassword;
        PasswordEntry.IsPassword = !_showPassword;
        TogglePasswordButton.Text = _showPassword ? "🙈" : "👁";
    }

    private async void GoRegister_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(RegisterPage));

    private async void Login_Clicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        ErrorLabel.Text = string.Empty;

        var username = UsernameEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Kullanıcı adı ve şifre zorunludur.");
            return;
        }

        SetBusy(true);
        try
        {
            var user = await _authService.LoginAsync(username, password);
            if (user == null)
            {
                ShowError("Kullanıcı adı veya şifre hatalı.");
                return;
            }

            SessionHelper.CurrentUser = user;

            var isAdmin = string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            await Shell.Current.GoToAsync(isAdmin ? nameof(AdminPage) : "//MainPage");

            UsernameEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            PasswordEntry.IsPassword = true;
            _showPassword = false;
            TogglePasswordButton.Text = "👁";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        LoginButton.IsEnabled = !busy;
        BusyIndicator.IsRunning = busy;
        BusyIndicator.IsVisible = busy;
        BusyText.IsVisible = busy;
    }
}
