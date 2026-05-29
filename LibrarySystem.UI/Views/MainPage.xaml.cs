using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class MainPage : ContentPage
{
    private readonly IBookService _bookService;
    private readonly IBorrowService _borrowService;
    private readonly IRecommendationService _recommendationService;
    private List<Book> _allBooks = new();
    private List<BookCategoryItem> _categories = new();
    private CancellationTokenSource? _searchDebounceCts;

    public MainPage()
    {
        InitializeComponent();
        _bookService = new BookService();
        _borrowService = new BorrowService();
        _recommendationService = new RecommendationService();
        _categories = BookCategories.CreateListWithCounts(Enumerable.Empty<Book>(), useDemoCountsWhenEmpty: true);

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

        if (SessionHelper.CurrentUser is null)
        {
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        var user = SessionHelper.CurrentUser;
        ProfileUsernameLabel.Text = GetProfileDisplayName(user);
        CategoryFilterState.SelectedCategoryId = null;
        UpdateWelcomeMessage();
        ApplyRoleBasedUi();
        UpdateAuthorRecommendationUi();
        await LoadAuthorRecommendationsAsync();
        await LoadBooksAsync();
        await LoadActiveBorrowsAsync();
        await LoadCategoriesAsync();

        if (MainTabNavigationState.OpenCategoriesTab)
        {
            MainTabNavigationState.OpenCategoriesTab = false;
            ShowCategoriesTab();
        }
        else
            ShowHomeTab();
    }

    private Task LoadCategoriesAsync()
    {
        _categories = BookCategories.CreateListWithCounts(_allBooks, useDemoCountsWhenEmpty: true);
        CategoryCountLabel.Text = $"{_categories.Count} kategori";
        CategoryUiHelper.FillTwoColumnGrid(CategoriesGrid, _categories, OpenCategoryAsync);
        return Task.CompletedTask;
    }

    private async Task OpenCategoryAsync(BookCategoryItem category)
    {
        CategoryFilterState.SelectedCategoryId = category.Id;
        await Shell.Current.GoToAsync(nameof(CategoryDetailPage));
    }

    private void UpdateWelcomeMessage()
    {
        var user = SessionHelper.CurrentUser;
        if (SessionHelper.IsAuthor)
        {
            var name = string.IsNullOrWhiteSpace(user?.FullName) ? user?.Username : user?.FullName;
            WelcomeLabel.Text = $"✍️ Hoş geldin, Yazar {name ?? "Kullanıcı"}!";
            return;
        }

        WelcomeLabel.Text = $"👋 Hoş geldin, {GetProfileDisplayName(user)}!";
    }

    private static string GetProfileDisplayName(Models.User? user)
    {
        if (user is null) return "Kullanıcı";
        if (!string.IsNullOrWhiteSpace(user.FullName)) return user.FullName;
        return SessionHelper.GetDisplayUsername(user);
    }

    private void ApplyRoleBasedUi()
    {
        // Yazarlar da öğrenci gibi tüm ana sayfa bölümlerini görür; ekstra sadece öneri ekleme (+).
    }

    private void ShowHomeTab()
    {
        HomePanel.IsVisible = true;
        CategoriesPanel.IsVisible = false;
        UpdateWelcomeMessage();
        UpdateAuthorRecommendationUi();
        ThemeHelper.ApplyBottomTab(
            TabHomeBtn,
            TabCategoriesBtn, TabFavoritesBtn, TabNotificationsBtn, TabSettingsBtn);
    }

    private void ShowCategoriesTab()
    {
        HomePanel.IsVisible = false;
        CategoriesPanel.IsVisible = true;
        ThemeHelper.ApplyBottomTab(
            TabCategoriesBtn,
            TabHomeBtn, TabFavoritesBtn, TabNotificationsBtn, TabSettingsBtn);
        CategoryUiHelper.FillTwoColumnGrid(CategoriesGrid, _categories, OpenCategoryAsync);
    }

    private async Task LoadActiveBorrowsAsync()
    {
        if (SessionHelper.CurrentUser is null || SessionHelper.IsAdmin)
        {
            ActiveBorrowsSection.IsVisible = false;
            return;
        }

        var active = await _borrowService.GetMyActiveBorrowsAsync();
        ActiveBorrowsCollectionView.ItemsSource = active;
        ActiveBorrowCountLabel.Text = active.Count == 0 ? "" : $"{active.Count} kitap";
        ActiveBorrowsSection.IsVisible = true;
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
        BooksCollectionView.ItemsSource = _allBooks.ToList();
    }

    private async void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        try
        {
            await Task.Delay(350, token);
            await LoadBooksFromSearchAsync();
        }
        catch (TaskCanceledException)
        {
            // yeni arama isteği geldi
        }
    }

    private async Task LoadBooksFromSearchAsync()
    {
        var term = KitapSearchBar.Text?.Trim();

        _allBooks = string.IsNullOrWhiteSpace(term)
            ? await _bookService.GetAllBooksAsync()
            : await _bookService.SearchBooksAsync(term);

        if (SessionHelper.CurrentUser != null)
        {
            var favs = await _bookService.GetFavoritesAsync(SessionHelper.CurrentUser.Id);
            var favIds = favs.Select(f => f.BookId).ToHashSet();
            foreach (var book in _allBooks)
            {
                book.IsFavorite = favIds.Contains(book.Id);
            }
        }

        ApplyBookFilter();
    }

    private async void Book_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Book book)
        {
            BooksCollectionView.SelectedItem = null;
            BookNavigationState.PendingBook = book;
            await Shell.Current.GoToAsync(nameof(BookDetailPage));
        }
    }

    private async void Favorite_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Book book)
        {
            if (SessionHelper.CurrentUser == null) return;
            int userId = SessionHelper.CurrentUser.Id;

            bool success;
            if (book.IsFavorite)
                success = await _bookService.RemoveFavoriteAsync(userId, book.Id);
            else
                success = await _bookService.AddFavoriteAsync(userId, book.Id);

            if (!success)
            {
                await DisplayAlert("Hata", "Favori işlemi başarısız. API çalışıyor mu ve kitap veritabanında var mı kontrol edin.", "Tamam");
                return;
            }

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
        => await TabNavigation.GoToFavoritesAsync();

    private async void Profile_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(UserPanelPage));

    private async void Logout_Clicked(object sender, EventArgs e)
    {
        bool ok = await DisplayAlert("Çıkış", "Çıkış yapmak istiyor musunuz?", "Evet", "Hayır");
        if (ok)
        {
            SessionHelper.CurrentUser = null;
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    private void TabCategories_Clicked(object sender, EventArgs e)
        => ShowCategoriesTab();

    private async void TabNotifications_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToNotificationsAsync();

    private async void TabSettings_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToSettingsAsync();

    private void TabHome_Clicked(object sender, EventArgs e)
        => ShowHomeTab();

    private void UpdateAuthorRecommendationUi()
    {
        AddRecommendationButton.IsVisible = SessionHelper.IsAuthor && !SessionHelper.IsAdmin;
    }

    private async Task LoadAuthorRecommendationsAsync()
    {
        var recommendations = await _recommendationService.GetWeeklyRecommendationsAsync();
        var cards = recommendations.Count > 0
            ? recommendations.Select(item => new QuoteCard(
                item.AuthorName,
                item.BookTitle,
                item.Idea)).ToList()
            : GetFallbackQuoteCards();

        QuotesCollection.ItemsSource = cards;
    }

    private static List<QuoteCard> GetFallbackQuoteCards() => new()
    {
        new("Orhan Pamuk", "Kara Kitap", "Okumak, hayatta kalmaktır. Kitaplar bizi biz yapan kapılardır."),
        new("Elif Şafak", "Aşk", "Bir kitap, bir insanı değiştirir; bazen de bir hayatı."),
        new("Ahmet Ümit", "İstanbul Hatırası", "Her kitap yeni bir maceradır. Okudukça çoğalırız.")
    };

    private async void AddRecommendation_Clicked(object sender, EventArgs e)
    {
        if (!SessionHelper.IsAuthor)
        {
            return;
        }

        await Navigation.PushAsync(new AddBookRecommendationPage());
    }

    private sealed record QuoteCard(string Author, string BookTitle, string Text);
    private sealed record PopularBookCard(int Rank, string Title, string Author, string Rating, string RowColor);
}
