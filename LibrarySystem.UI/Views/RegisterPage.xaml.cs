using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class RegisterPage : ContentPage
{
    private readonly IAuthService _authService;
    private bool _showPassword;

    public RegisterPage()
    {
        InitializeComponent();
        _authService = new AuthService();
    }

    private async void AuthHeader_BackClicked(object? sender, EventArgs e)
        => await Navigation.PopAsync();

    private async void GoLogin_Clicked(object sender, EventArgs e)
        => await Navigation.PopAsync();

    private void TogglePassword_Clicked(object sender, EventArgs e)
    {
        _showPassword = !_showPassword;
        PasswordEntry.IsPassword = !_showPassword;
        TogglePasswordButton.Text = _showPassword ? "🙈" : "👁";
    }

    private async void Register_Clicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        ErrorLabel.Text = string.Empty;

        var fullName = FullNameEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Ad soyad, e-posta ve şifre zorunludur.");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Şifre en az 6 karakter olmalıdır.");
            return;
        }

        var (firstName, lastName) = SplitFullName(fullName);

        SetBusy(true);
        try
        {
            var success = await _authService.RegisterAsync(firstName, lastName, email, password);
            if (!success)
            {
                ShowError("Kayıt oluşturulamadı. E-posta kullanımda olabilir.");
                return;
            }

            await DisplayAlert("Başarılı", "Hesabınız oluşturuldu. Giriş yapabilirsiniz.", "Tamam");
            await Navigation.PopAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static (string FirstName, string LastName) SplitFullName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        if (parts.Length == 1)
        {
            return (parts[0], "-");
        }

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        RegisterButton.IsEnabled = !busy;
        FullNameEntry.IsEnabled = !busy;
        EmailEntry.IsEnabled = !busy;
        PasswordEntry.IsEnabled = !busy;
        BusyRow.IsVisible = busy;
        BusyIndicator.IsRunning = busy;
        BusyIndicator.IsVisible = busy;
        BusyText.IsVisible = busy;
    }
}
