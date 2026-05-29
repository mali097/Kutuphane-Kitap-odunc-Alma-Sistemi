using LibrarySystem.UI.Controls;
using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService;
    private bool _showPassword;
    private bool _isAuthorLoginMode;

    public LoginPage()
    {
        InitializeComponent();
        _authService = new AuthService();
        ApplyLoginModeUi();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AuthHeader.ShowBackButton = Navigation.NavigationStack.Count > 1;
    }

    private async void AuthHeader_BackClicked(object? sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
    }

    private void StudentRoleTab_Clicked(object sender, EventArgs e)
    {
        if (!_isAuthorLoginMode)
        {
            return;
        }

        _isAuthorLoginMode = false;
        ApplyLoginModeUi();
    }

    private void AuthorRoleTab_Clicked(object sender, EventArgs e)
    {
        if (_isAuthorLoginMode)
        {
            return;
        }

        _isAuthorLoginMode = true;
        ApplyLoginModeUi();
    }

    private void ApplyLoginModeUi()
    {
        ErrorLabel.IsVisible = false;

        if (_isAuthorLoginMode)
        {
            AuthHeader.Title = "Yazar Girişi";
            AuthHeader.Subtitle = "Yazar paneline giriş yaparak kitap önerilerinizi paylaşın";
            AuthHeader.Illustration = AuthHeaderIllustration.Register;
            LoginButton.Text = "Yazar Girişi";

            AuthorRoleTab.BackgroundColor = Color.FromArgb("#6B46C1");
            AuthorRoleTab.TextColor = Colors.White;
            StudentRoleTab.BackgroundColor = Color.FromArgb("#EDE9FF");
            StudentRoleTab.TextColor = Color.FromArgb("#6B46C1");

            AuthorInfoLabel.IsVisible = true;
            RegisterFooter.IsVisible = false;
        }
        else
        {
            AuthHeader.Title = "Giriş Yap";
            AuthHeader.Subtitle = "Hesabınıza giriş yaparak kitap dünyasına devam edin";
            AuthHeader.Illustration = AuthHeaderIllustration.Login;
            LoginButton.Text = "Giriş Yap";

            StudentRoleTab.BackgroundColor = Color.FromArgb("#6B46C1");
            StudentRoleTab.TextColor = Colors.White;
            AuthorRoleTab.BackgroundColor = Color.FromArgb("#EDE9FF");
            AuthorRoleTab.TextColor = Color.FromArgb("#6B46C1");

            AuthorInfoLabel.IsVisible = false;
            RegisterFooter.IsVisible = true;
        }
    }

    private void TogglePassword_Clicked(object sender, EventArgs e)
    {
        _showPassword = !_showPassword;
        PasswordEntry.IsPassword = !_showPassword;
        TogglePasswordButton.Text = _showPassword ? "🙈" : "👁";
    }

    private async void GoRegister_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new RegisterPage());

    private async void Login_Clicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        ErrorLabel.Text = string.Empty;

        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("E-posta ve şifre zorunludur.");
            return;
        }

        var expectedRole = _isAuthorLoginMode ? "Author" : "Student";

        SetBusy(true);
        try
        {
            var result = await _authService.LoginAsync(email, password, expectedRole);
            if (!result.IsSuccess || result.User is null)
            {
                ShowError(result.ErrorMessage ?? "E-posta veya şifre hatalı.");
                return;
            }

            SessionHelper.CurrentUser = result.User;

            if (string.Equals(result.User.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                await Shell.Current.GoToAsync(nameof(AdminPage));
            }
            else
            {
                await Shell.Current.GoToAsync("//MainPage");
            }

            EmailEntry.Text = string.Empty;
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
        EmailEntry.IsEnabled = !busy;
        PasswordEntry.IsEnabled = !busy;
        StudentRoleTab.IsEnabled = !busy;
        AuthorRoleTab.IsEnabled = !busy;
        BusyRow.IsVisible = busy;
        BusyIndicator.IsRunning = busy;
        BusyIndicator.IsVisible = busy;
        BusyText.IsVisible = busy;
    }
}
