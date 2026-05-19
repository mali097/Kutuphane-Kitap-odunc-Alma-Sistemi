using LibrarySystem.UI.Helpers;

namespace LibrarySystem.UI.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var user = SessionHelper.CurrentUser;
        if (user == null) return;

        UsernameLabel.Text = user.Username;
        EmailLabel.Text = $"{user.Username}@email.com";
    }

    private async void Back_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void ProfileInfo_Tapped(object? sender, EventArgs e)
        => await Navigation.PushAsync(new UserPanelPage());

    private async void ChangePassword_Tapped(object? sender, EventArgs e)
        => await Navigation.PushAsync(new ChangePasswordPage());

    private async void Email_Tapped(object? sender, EventArgs e)
    {
        var user = SessionHelper.CurrentUser;
        var email = user == null ? "—" : $"{user.Username}@email.com";
        await DisplayAlert("E-posta Adresi", email, "Tamam");
    }

    private async void Appearance_Tapped(object? sender, EventArgs e)
    {
        var pick = await DisplayActionSheet("Görünüm", "İptal", null, "Açık tema", "Koyu tema", "Sistem varsayılanı");
        if (pick is null or "İptal") return;

        if (pick == "Açık tema")
            Application.Current!.UserAppTheme = AppTheme.Light;
        else if (pick == "Koyu tema")
            Application.Current!.UserAppTheme = AppTheme.Dark;
        else
            Application.Current!.UserAppTheme = AppTheme.Unspecified;
    }

    private async void About_Tapped(object? sender, EventArgs e)
        => await DisplayAlert("Hakkımızda",
            "Kütüphane Kitap Ödünç Alma Sistemi\nSürüm 1.2",
            "Tamam");

    private async void Help_Tapped(object? sender, EventArgs e)
        => await DisplayAlert("Yardım ve Destek",
            "Sorularınız için: destek@kutuphane-ornek.com",
            "Tamam");

    private async void TabHome_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToHomeAsync();

    private async void TabCategories_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToCategoriesAsync();

    private async void TabFavorites_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToFavoritesAsync();

    private async void TabNotifications_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToNotificationsAsync();
}
