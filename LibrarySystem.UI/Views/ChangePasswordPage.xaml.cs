using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class ChangePasswordPage : ContentPage
{
    private readonly IAuthService _authService;

    public ChangePasswordPage()
    {
        InitializeComponent();
        _authService = new AuthService();
    }

    private async void Change_Clicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        if (string.IsNullOrWhiteSpace(OldPasswordEntry.Text)) { ShowError("Mevcut şifre boş olamaz."); return; }
        if (string.IsNullOrWhiteSpace(NewPasswordEntry.Text) || NewPasswordEntry.Text.Length < 6)
        { ShowError("Yeni şifre en az 6 karakter olmalı."); return; }
        if (NewPasswordEntry.Text != ConfirmPasswordEntry.Text)
        { ShowError("Şifreler eşleşmiyor."); return; }

        bool ok = await _authService.ChangePasswordAsync(
            SessionHelper.CurrentUser!.Id, OldPasswordEntry.Text, NewPasswordEntry.Text);

        if (ok)
        {
            await DisplayAlert("✅", "Şifreniz başarıyla değiştirildi.", "Tamam");
            await Navigation.PopAsync();
        }
        else ShowError("Şifre değiştirme başarısız. Mevcut şifreniz yanlış olabilir.");
    }

    private void ShowError(string msg) { ErrorLabel.Text = msg; ErrorLabel.IsVisible = true; }
}