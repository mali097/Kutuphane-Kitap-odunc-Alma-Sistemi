using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class MainPage : ContentPage
{
    private readonly IBookService _bookService;
    private List<Book> _allBooks = new();
    private bool _isDarkMode;

    public MainPage()
    {
        InitializeComponent();
        _bookService = new BookService();

        QuotesCollection.ItemsSource = new List<QuoteCard>
        {
            new("Orhan Pamuk", "Okumak, hayatta kalmaktır.\nKitaplar bizi biz yapan kapılardır."),
            new("Elif Şafak", "Bir kitap, bir insanı değiştirir;\nbazen de bir hayatı."),
            new("Ahmet Ümit", "Her kitap yeni bir maceradır.\nOkudukça çoğalırız.")
        };

        PopularCollection.ItemsSource = new List<PopularBookCard>
        {
            new(1, "Suç ve Ceza", "Fyodor Dostoyevski", "4.8", "#F0D9A8"),
            new(2, "1984", "George Orwell", "4.7", "#2C6DFF"),
            new(3, "Kürk Mantolu Madonna", "Sabahattin Ali", "4.6", "#2EA77E"),
            new(4, "Simyacı", "Paulo Coelho", "4.5", "#F07B3A"),
            new(5, "Beyaz Zambaklar Ülkesinde", "Grigory Petrov", "4.4", "#7A4BD1")
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WelcomeLabel.Text = $"👋 Hoş geldin, {SessionHelper.CurrentUser?.FullName ?? "Kullanıcı"}!";
        await LoadBooksAsync();
    }

    private async Task LoadBooksAsync()
    {
        _allBooks = await _bookService.GetAllBooksAsync();

        if (SessionHelper.CurrentUser != null)
        {
            var favs = await _bookService.GetFavoritesAsync(SessionHelper.CurrentUser.Id);
            var favIds = favs.Select(f => f.BookId).ToHashSet();
            foreach (var book in _allBooks)
                book.IsFavorite = favIds.Contains(book.Id);
        }

        BooksCollectionView.ItemsSource = null;
        BooksCollectionView.ItemsSource = _allBooks;
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            BooksCollectionView.ItemsSource = _allBooks;
            return;
        }

        var filtered = _allBooks.Where(b =>
            b.Title.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase) ||
            b.Author.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase) ||
            b.ISBN.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase) ||
            b.Category.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        BooksCollectionView.ItemsSource = filtered;
    }

    private void CategoryPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        var selected = CategoryPicker.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(selected) || selected == "Tümü")
        {
            BooksCollectionView.ItemsSource = _allBooks;
            return;
        }

        BooksCollectionView.ItemsSource = _allBooks
            .Where(b => b.Category.Equals(selected, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async void Book_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Book book)
        {
            BooksCollectionView.SelectedItem = null;
            await Navigation.PushAsync(new BookDetailPage(book));
        }
    }

    private async void Favorite_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Book book)
        {
            if (SessionHelper.CurrentUser == null) return;
            int userId = SessionHelper.CurrentUser.Id;

            if (book.IsFavorite)
                await _bookService.RemoveFavoriteAsync(userId, book.Id);
            else
                await _bookService.AddFavoriteAsync(userId, book.Id);

            book.IsFavorite = !book.IsFavorite;
            var temp = BooksCollectionView.ItemsSource;
            BooksCollectionView.ItemsSource = null;
            BooksCollectionView.ItemsSource = temp;
        }
    }

    private async void Borrow_Clicked(object sender, EventArgs e)
    {
        var available = _allBooks.Where(b => b.IsAvailable).ToList();
        if (!available.Any())
        {
            await DisplayAlert("Bilgi", "Rafta uygun kitap yok.", "Tamam");
            return;
        }
        await Navigation.PushAsync(new BorrowPage(available));
    }

    private async void Favorites_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new FavoritesPage());

    private async void Profile_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new UserPanelPage());

    private async void Logout_Clicked(object sender, EventArgs e)
    {
        bool ok = await DisplayAlert("Çıkış", "Çıkış yapmak istiyor musunuz?", "Evet", "Hayır");
        if (ok)
        {
            SessionHelper.CurrentUser = null;
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }
    }

    private void ThemeToggle_Clicked(object sender, EventArgs e)
    {
        _isDarkMode = !_isDarkMode;
        Application.Current!.UserAppTheme = _isDarkMode ? AppTheme.Dark : AppTheme.Light;
    }

    private void Filter_Clicked(object sender, EventArgs e)
        => CategoryPicker.Focus();

    private async void Hamburger_Clicked(object sender, EventArgs e)
        => await DisplayAlert("Menü", "Menü yakında eklenecek.", "Tamam");

    private async void Notifications_Clicked(object sender, EventArgs e)
        => await DisplayAlert("Bildirimler", "Bildirimler yakında eklenecek.", "Tamam");

    private async void ViewAllQuotes_Clicked(object sender, EventArgs e)
        => await DisplayAlert("Yazarlar", "Tüm yazar görüşleri yakında eklenecek.", "Tamam");

    private async void ViewAllPopular_Clicked(object sender, EventArgs e)
        => await DisplayAlert("Popüler", "Tüm popüler kitaplar yakında eklenecek.", "Tamam");

    private void TabHome_Clicked(object sender, EventArgs e)
    {
        // already here
    }

    private void TabCategories_Clicked(object sender, EventArgs e)
        => CategoryPicker.Focus();

    private async void TabHistory_Clicked(object sender, EventArgs e)
        => await DisplayAlert("Geçmişim", "Geçmiş ekranı yakında eklenecek.", "Tamam");

    private sealed record QuoteCard(string Author, string Text);
    private sealed record PopularBookCard(int Rank, string Title, string Author, string Rating, string RowColor);
}
