using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class CategoryDetailPage : ContentPage
{
    private readonly BookCategoryItem? _passedCategory;
    private readonly IBookService _bookService;
    private BookCategoryItem? _category;

    public CategoryDetailPage() : this(null) { }

    public CategoryDetailPage(BookCategoryItem? category)
    {
        InitializeComponent();
        _passedCategory = category;
        _bookService = new BookService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ThemeHelper.ApplyBottomTab(
            TabCategoriesBtn,
            TabHomeBtn, TabFavoritesBtn, TabNotificationsBtn, TabSettingsBtn);

        _category = _passedCategory
            ?? (CategoryFilterState.SelectedCategoryId is int id ? BookCategories.GetById(id) : null);

        if (_category == null)
        {
            await DisplayAlert("Kategori", "Kategori bulunamadı.", "Tamam");
            await NavigateBackToCategoriesAsync();
            return;
        }

        HeaderTitleLabel.Text = _category.Name;
        HeroIconLabel.Text = _category.Icon;
        HeroIconBorder.BackgroundColor = _category.IconBackground;
        HeroNameLabel.Text = _category.Name;
        HeroDescriptionLabel.Text = _category.Description;

        var books = await _bookService.GetAllBooksAsync();
        var filtered = BookCategories.FilterBooks(books, _category.Id);
        _category.BookCount = filtered.Count > 0
            ? filtered.Count
            : BookCategories.GetDemoCount(_category.Id);
        HeroCountLabel.Text = _category.BookCountText;

        BooksList.ItemsSource = filtered
            .Select((b, i) => new CategoryBookRow
            {
                Book = b,
                RatingDisplay = (4.2 + (i % 7) * 0.1).ToString("0.0")
            })
            .ToList();
    }

    private async Task NavigateBackToCategoriesAsync()
    {
        MainTabNavigationState.OpenCategoriesTab = true;
        await Shell.Current.GoToAsync("..");
    }

    private async void Back_Clicked(object sender, EventArgs e)
        => await NavigateBackToCategoriesAsync();

    protected override bool OnBackButtonPressed()
    {
        MainTabNavigationState.OpenCategoriesTab = true;
        return base.OnBackButtonPressed();
    }

    private async void Filter_Clicked(object sender, EventArgs e)
        => await DisplayAlert("Filtre", "Sıralama ve filtre seçenekleri yakında eklenecek.", "Tamam");

    private async void Book_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CategoryBookRow row)
            return;

        BooksList.SelectedItem = null;
        BookNavigationState.PendingBook = row.Book;
        await Shell.Current.GoToAsync(nameof(BookDetailPage));
    }

    private async void Bookmark_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Book book })
            return;

        if (SessionHelper.CurrentUser == null)
        {
            await DisplayAlert("Giriş", "Favorilere eklemek için giriş yapmalısın.", "Tamam");
            return;
        }

        await _bookService.AddFavoriteAsync(SessionHelper.CurrentUser.Id, book.Id);
        await DisplayAlert("Favoriler", "Kitap favorilere eklendi.", "Tamam");
    }

    private async void TabHome_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToHomeAsync();

    private async void TabCategories_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToCategoriesAsync();

    private async void TabFavorites_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToFavoritesAsync();

    private async void TabNotifications_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToNotificationsAsync();

    private async void TabSettings_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToSettingsAsync();
}
