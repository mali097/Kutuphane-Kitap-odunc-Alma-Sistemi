using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class FavoritesPage : ContentPage
{
    private readonly IBookService _bookService;

    public FavoritesPage()
    {
        InitializeComponent();
        _bookService = new BookService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (SessionHelper.CurrentUser == null) return;
        var favs = await _bookService.GetFavoritesAsync(SessionHelper.CurrentUser.Id);
        FavoritesList.ItemsSource = favs;
    }

    private async void RemoveFav_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is FavoriteBook fav)
        {
            bool ok = await _bookService.RemoveFavoriteAsync(SessionHelper.CurrentUser!.Id, fav.BookId);
            if (ok)
            {
                await DisplayAlert("✅", "Favorilerden çıkarıldı.", "Tamam");
                OnAppearing();
            }
        }
    }
}