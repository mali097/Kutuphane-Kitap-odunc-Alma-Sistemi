using System.Globalization;
using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class UserPanelPage : ContentPage
{
    private readonly IBorrowService _borrowService;
    private readonly IBookService _bookService;
    private readonly IAuthService _authService;

    public UserPanelPage()
    {
        InitializeComponent();
        _borrowService = new BorrowService();
        _bookService = new BookService();
        _authService = new AuthService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var user = SessionHelper.CurrentUser;
        if (user == null)
        {
            await DisplayAlert("Profil", "Oturum bilgisi yok. Ana sayfaya dönülüyor.", "Tamam");
            await Navigation.PopAsync();
            return;
        }

        SessionHelper.NormalizeProfile(user);

        var profile = await _authService.GetMyProfileAsync();
        if (profile?.CreatedAt is not null)
        {
            user.CreatedAt = profile.CreatedAt;
            SessionHelper.CurrentUser = user;
        }

        var hasFullName = !string.IsNullOrWhiteSpace(user.FullName);
        NameLabel.Text = hasFullName ? user.FullName!.Trim() : SessionHelper.GetDisplayUsername(user);
        UsernameLabel.IsVisible = !hasFullName;
        UsernameLabel.Text = SessionHelper.GetDisplayUsername(user);
        EmailLabel.Text = string.IsNullOrWhiteSpace(user.Email) ? "—" : user.Email;
        RoleLabel.Text = SessionHelper.IsAdmin ? "Yönetici"
            : SessionHelper.IsAuthor ? "Yazar"
            : "Üye";

        MembershipDateLabel.Text = user.CreatedAt.HasValue
            ? user.CreatedAt.Value.ToString("d MMMM yyyy", new CultureInfo("tr-TR"))
            : "Kayıt tarihi henüz alınamadı";

        AdminRow.IsVisible = SessionHelper.IsAdmin;
        AdminSeparator.IsVisible = SessionHelper.IsAdmin;

        var allBorrows = await _borrowService.GetMyBorrowsAsync();
        var active = await _borrowService.GetMyActiveBorrowsAsync();
        var history = allBorrows.Where(b => b.IsReturned)
            .OrderByDescending(b => b.ReturnDate)
            .ToList();

        ActiveBorrows.ItemsSource = active;
        HistoryBorrows.ItemsSource = history;

        StatActiveLabel.Text = active.Count.ToString();
        StatHistoryLabel.Text = history.Count.ToString();

        var favs = await _bookService.GetFavoritesAsync(user.Id);
        StatFavoritesLabel.Text = favs.Count.ToString();

        ActiveBorrows.HeightRequest = Math.Clamp(120 + active.Count * 96, 120, 360);
        HistoryBorrows.HeightRequest = Math.Clamp(100 + history.Count * 88, 100, 320);
    }

    private async void Theme_Tapped(object? sender, EventArgs e)
    {
        var pick = await DisplayActionSheet(
            "Tema", "İptal", null,
            "Açık tema", "Koyu tema", "Sistem varsayılanı");

        var choice = ThemeHelper.ChoiceFromActionSheet(pick);
        if (choice is null) return;

        ThemeHelper.ApplyThemeChoice(choice);
    }

    private async void NotificationSettings_Tapped(object? sender, EventArgs e)
        => await DisplayAlert("Bildirim ayarları",
            "Şu an yalnızca kitap teslim ve iade bildirimleri gösteriliyor. İleride ek seçenekler buradan yönetilebilecek.",
            "Tamam");

    private async void ReturnBorrow_Clicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: BorrowRecord record })
            return;

        var ok = await DisplayAlert(
            "Kitap iadesi",
            $"\"{record.BookTitle}\" kitabını iade etmek istiyor musunuz?",
            "İade et",
            "İptal");

        if (!ok)
            return;

        var success = await _borrowService.ReturnBookAsync(record.Id);
        if (!success)
        {
            await DisplayAlert("Hata", "İade işlemi başarısız. API çalışıyor mu?", "Tamam");
            return;
        }

        await DisplayAlert("✅", "Kitap iade edildi. Bildirimler sayfasından detayı görebilirsiniz.", "Tamam");
        await ReloadAsync();
    }

    private async void Favorites_Tapped(object? sender, EventArgs e)
        => await Navigation.PushAsync(new FavoritesPage());

    private async void Admin_Tapped(object? sender, EventArgs e)
    {
        if (!SessionHelper.IsAdmin) return;
        await Navigation.PushAsync(new AdminPage());
    }

    private async void ChangePass_Tapped(object? sender, EventArgs e)
        => await Navigation.PushAsync(new ChangePasswordPage());

    private async void Logout_Clicked(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("Çıkış", "Çıkış yapmak istiyor musunuz?", "Evet", "Hayır");
        if (!ok) return;

        SessionHelper.CurrentUser = null;
        await Shell.Current.GoToAsync("//MainPage");
    }
}
