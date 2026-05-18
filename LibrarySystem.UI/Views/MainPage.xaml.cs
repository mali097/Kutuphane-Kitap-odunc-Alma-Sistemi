using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class MainPage : ContentPage
{
    private readonly IBookService _bookService;
    private List<Book> _allBooks = new();
    private string? _selectedCategory;

    private static readonly string[] CategoryOptions =
    {
        "Tümü", "Roman", "Bilim", "Tarih", "Teknoloji", "Felsefe", "Çocuk", "Biyografi"
    };

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

        ApplyBookFilter();
    }

    private void ApplyBookFilter()
    {
        IEnumerable<Book> q = _allBooks;

        if (!string.IsNullOrEmpty(_selectedCategory) && _selectedCategory != "Tümü")
        {
            q = q.Where(b =>
                b.Category.Equals(_selectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        var term = KitapSearchBar.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            q = q.Where(b =>
                b.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                b.Author.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                b.ISBN.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                b.Category.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        BooksCollectionView.ItemsSource = q.ToList();
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyBookFilter();

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

    private async void TabCategories_Clicked(object sender, EventArgs e)
    {
        var pick = await DisplayActionSheet("Kategori seç", "İptal", null, CategoryOptions);
        if (string.IsNullOrEmpty(pick) || pick == "İptal")
            return;

        _selectedCategory = pick == "Tümü" ? null : pick;
        ApplyBookFilter();
    }

    private async void Notifications_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new NotificationsPage());

    private async void TabNotifications_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new NotificationsPage());

    private async void TabSettings_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new UserPanelPage());

    private void TabHome_Clicked(object sender, EventArgs e) { }

    private sealed record QuoteCard(string Author, string Text);
    private sealed record PopularBookCard(int Rank, string Title, string Author, string Rating, string RowColor);
}
