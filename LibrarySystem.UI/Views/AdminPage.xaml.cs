using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class AdminPage : ContentPage
{
    private readonly IAuthService _authService;
    private readonly IBorrowService _borrowService;

    public AdminPage()
    {
        InitializeComponent();
        _authService = new AuthService();
        _borrowService = new BorrowService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        AdminNameLabel.Text = $"Hoş geldin, {SessionHelper.CurrentUser?.FullName}";
        var users = await _authService.GetAllUsersAsync();
        // LINQ ile admin hariç listele
        UsersList.ItemsSource = users.Where(u => u.Role != "Admin").ToList();
    }

    private async void AddBook_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new AddEditBookPage());

    private async void BookList_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new MainPage());

    private async void Overdue_Clicked(object sender, EventArgs e)
    {
        var overdue = await _borrowService.GetOverdueBorrowsAsync();
        await DisplayAlert("⚠️ Geciken İadeler", $"Toplam {overdue.Count} gecikmiş kitap var.", "Tamam");
    }

    private async void DeleteUser_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is User user)
        {
            bool ok = await DisplayAlert("Kullanıcı Sil",
                $"{user.FullName} silinsin mi?", "Sil", "İptal");
            if (!ok) return;
            bool success = await _authService.DeleteUserAsync(user.Id);
            if (success) OnAppearing();
            else await DisplayAlert("Hata", "Silme başarısız.", "Tamam");
        }
    }

    private async void Logout_Clicked(object sender, EventArgs e)
    {
        SessionHelper.CurrentUser = null;
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }
}