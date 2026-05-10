using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Services;
using System.Xml;

namespace LibrarySystem.UI.Views;

public partial class UserPanelPage : ContentPage
{
    private readonly IBorrowService _borrowService;

    public UserPanelPage()
    {
        InitializeComponent();
        _borrowService = new BorrowService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var user = SessionHelper.CurrentUser;
        if (user == null) return;

        NameLabel.Text = user.FullName;
        UsernameLabel.Text = $"@{user.Username}";
        RoleLabel.Text = user.Role == "Admin" ? "👑 Admin" : "📚 Üye";

        var allBorrows = await _borrowService.GetUserBorrowsAsync(user.Id);

        // LINQ ile aktif ve geçmiş ayır
        ActiveBorrows.ItemsSource = allBorrows.Where(b => !b.IsReturned).ToList();
        HistoryBorrows.ItemsSource = allBorrows.Where(b => b.IsReturned)
                                               .OrderByDescending(b => b.ReturnDate)
                                               .ToList();
    }

    private async void ChangePass_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new ChangePasswordPage());
}