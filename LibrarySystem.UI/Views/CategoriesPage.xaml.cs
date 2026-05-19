using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class CategoriesPage : ContentPage
{
    private readonly IBookService _bookService;
    private List<BookCategoryItem> _categories = new();

    public CategoriesPage()
    {
        InitializeComponent();
        _bookService = new BookService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadAndRender();
    }

    private async void LoadAndRender()
    {
        try
        {
            var books = await _bookService.GetAllBooksAsync();
            _categories = BookCategories.CreateListWithCounts(books, useDemoCountsWhenEmpty: true);
        }
        catch
        {
            _categories = BookCategories.CreateListWithCounts(Enumerable.Empty<Book>(), useDemoCountsWhenEmpty: true);
        }

        CategoryCountLabel.Text = $"{_categories.Count} kategori";
        MainThread.BeginInvokeOnMainThread(() =>
            CategoryUiHelper.FillTwoColumnGrid(CategoriesGrid, _categories, OpenCategoryAsync));
    }

    private async Task OpenCategoryAsync(BookCategoryItem category)
    {
        CategoryFilterState.SelectedCategoryId = category.Id;
        await Shell.Current.GoToAsync(nameof(CategoryDetailPage));
    }

    private async void Back_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void Search_Clicked(object sender, EventArgs e)
        => await DisplayPromptAsync("Ara", "Kategori adı yazın:", "Tamam", "İptal");

    private async void TabHome_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToHomeAsync();

    private async void TabFavorites_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToFavoritesAsync();

    private async void TabNotifications_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToNotificationsAsync();

    private async void TabSettings_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToSettingsAsync();
}
