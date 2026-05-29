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

        SessionHelper.NormalizeProfile(user);
        UsernameLabel.Text = string.IsNullOrWhiteSpace(user.FullName)
            ? SessionHelper.GetDisplayUsername(user)
            : user.FullName.Trim();
        EmailLabel.Text = string.IsNullOrWhiteSpace(user.Email) ? "—" : user.Email;

        ThemeHelper.ApplyBottomTab(
            TabSettingsBtn,
            TabHomeBtn, TabCategoriesBtn, TabFavoritesBtn, TabNotificationsBtn);
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
        if (user != null)
            SessionHelper.NormalizeProfile(user);

        var email = user == null || string.IsNullOrWhiteSpace(user.Email) ? "—" : user.Email;
        await DisplayAlert("E-posta Adresi", email, "Tamam");
    }

    private async void Theme_Tapped(object? sender, EventArgs e)
    {
        var pick = await DisplayActionSheet(
            "Tema", "İptal", null,
            "Açık tema", "Koyu tema", "Sistem varsayılanı");

        var choice = ThemeHelper.ChoiceFromActionSheet(pick);
        if (choice is null) return;

        ThemeHelper.ApplyThemeChoice(choice);

        ThemeHelper.ApplyBottomTab(
            TabSettingsBtn,
            TabHomeBtn, TabCategoriesBtn, TabFavoritesBtn, TabNotificationsBtn);
    }

    private async void About_Tapped(object? sender, EventArgs e)
        => await DisplayAlert("Hakkımızda",
            "Kütüphane Kitap Ödünç Alma Sistemi\nSürüm 1.2",
            "Tamam");

    private async void Help_Tapped(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(SupportPage));

    private async void TabHome_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToHomeAsync();

    private async void TabCategories_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToCategoriesAsync();

    private async void TabFavorites_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToFavoritesAsync();

    private async void TabNotifications_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToNotificationsAsync();
}
