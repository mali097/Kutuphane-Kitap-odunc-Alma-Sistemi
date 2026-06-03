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

    private static readonly string[] PopularRowColors =
    [
        "#F0D9A8", "#2C6DFF", "#2EA77E", "#F07B3A", "#7A4BD1"
    ];

    public MainPage()
    {
        InitializeComponent();
        _bookService = new BookService();
        _borrowService = new BorrowService();
        _recommendationService = new RecommendationService();
        _categories = BookCategories.CreateListWithCounts(Enumerable.Empty<Book>(), useDemoCountsWhenEmpty: true);
        PopularCollection.ItemsSource = Array.Empty<PopularBookCard>();
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
        await LoadTopRatedBooksAsync();
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
        await ApplyFavoriteFlagsAsync();
        UpdateSearchResultsUi();
    }

    private async Task LoadTopRatedBooksAsync()
    {
        var topRated = await _bookService.GetTopRatedBooksAsync();
        if (topRated.Count == 0)
        {
            topRated = _allBooks
                .Where(book => book.AverageRating.HasValue)
                .OrderByDescending(book => book.AverageRating)
                .ThenByDescending(book => book.RatingCount)
                .Take(5)
                .Select(book => new TopRatedBook
                {
                    BookId = book.Id,
                    Title = book.Title,
                    Author = book.Author,
                    AverageRating = book.AverageRating!.Value,
                    RatingCount = book.RatingCount
                })
                .ToList();
        }

        PopularCollection.ItemsSource = topRated
            .Select((book, index) => new PopularBookCard(
                book.BookId,
                index + 1,
                book.Title,
                book.Author,
                book.AverageRating.ToString("0.0"),
                PopularRowColors[index % PopularRowColors.Length]))
            .ToList();
    }

    private async Task ApplyFavoriteFlagsAsync()
    {
        if (SessionHelper.CurrentUser == null)
        {
            return;
        }

        var favs = await _bookService.GetFavoritesAsync(SessionHelper.CurrentUser.Id);
        var favIds = favs.Select(f => f.BookId).ToHashSet();
        foreach (var book in _allBooks)
        {
            book.IsFavorite = favIds.Contains(book.Id);
        }
    }

    private void UpdateSearchResultsUi()
    {
        var term = KitapSearchBar.Text?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(term);
        SearchResultsPanel.IsVisible = hasSearch;

        if (!hasSearch)
        {
            return;
        }

        var results = _allBooks
            .Where(book =>
                book.Title.Contains(term!, StringComparison.OrdinalIgnoreCase)
                || book.Author.Contains(term!, StringComparison.OrdinalIgnoreCase))
            .ToList();

        SearchResultsCollection.ItemsSource = results;
        SearchEmptyLabel.IsVisible = results.Count == 0;
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

        if (string.IsNullOrWhiteSpace(term))
        {
            _allBooks = await _bookService.GetAllBooksAsync();
        }
        else
        {
            _allBooks = await _bookService.SearchBooksAsync(term);

            if (_allBooks.Count == 0)
            {
                var allBooks = await _bookService.GetAllBooksAsync();
                _allBooks = allBooks
                    .Where(book =>
                        book.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || book.Author.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        await ApplyFavoriteFlagsAsync();
        UpdateSearchResultsUi();
    }

    private async void SearchResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Book book)
        {
            return;
        }

        SearchResultsCollection.SelectedItem = null;
        BookNavigationState.PendingBook = book;
        await Shell.Current.GoToAsync(nameof(BookDetailPage));
    }

    private async void PopularBook_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PopularBookCard card)
        {
            return;
        }

        PopularCollection.SelectedItem = null;
        var book = _allBooks.FirstOrDefault(item => item.Id == card.BookId)
            ?? await _bookService.GetBookByIdAsync(card.BookId)
            ?? new Book
            {
                Id = card.BookId,
                Title = card.Title,
                Author = card.Author
            };

        BookNavigationState.PendingBook = book;
        await Shell.Current.GoToAsync(nameof(BookDetailPage));
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
        AuthorThoughtsSection.IsVisible = !SessionHelper.IsAdmin;
        AddRecommendationButton.IsVisible = SessionHelper.IsAuthor && !SessionHelper.IsAdmin;
    }

    private async Task LoadAuthorRecommendationsAsync()
    {
        var recommendations = await _recommendationService.GetWeeklyRecommendationsAsync();
        QuotesCollection.ItemsSource = recommendations
            .Select(item => new QuoteCard(item.AuthorName, item.BookTitle, item.Idea))
            .ToList();
    }

    private async void AddRecommendation_Clicked(object sender, EventArgs e)
    {
        if (!SessionHelper.IsAuthor)
        {
            return;
        }

        await Navigation.PushAsync(new AddBookRecommendationPage());
    }

    private sealed record QuoteCard(string Author, string BookTitle, string Text);
    private sealed record PopularBookCard(int BookId, int Rank, string Title, string Author, string Rating, string RowColor);
}
