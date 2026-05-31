using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class BookDetailPage : ContentPage
{
    private readonly IBookService _bookService;
    private readonly IBorrowService _borrowService;
    private Book _book = new();
    private bool _isSubmittingRating;

    public BookDetailPage() : this(BookNavigationState.PendingBook ?? new Book()) { }

    public BookDetailPage(Book book)
    {
        InitializeComponent();
        _bookService = new BookService();
        _borrowService = new BorrowService();
        _book = book;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BookNavigationState.PendingBook != null)
        {
            _book = BookNavigationState.PendingBook;
            BookNavigationState.PendingBook = null;
        }

        if (string.IsNullOrWhiteSpace(_book.Title))
        {
            await DisplayAlert("Kitap", "Kitap bilgisi yüklenemedi.", "Tamam");
            await Shell.Current.GoToAsync("..");
            return;
        }

        await RefreshBookFromApiAsync();
        await RefreshFavoriteStateAsync();
        await LoadMyRatingAsync();
        LoadBookData();
        ConfigureRatingSection();
    }

    private async Task RefreshBookFromApiAsync()
    {
        if (_book.Id <= 0)
        {
            return;
        }

        var freshBook = await _bookService.GetBookByIdAsync(_book.Id);
        if (freshBook is not null)
        {
            freshBook.IsFavorite = _book.IsFavorite;
            _book = freshBook;
        }
    }

    private void LoadBookData()
    {
        Title = _book.Title;
        TitleLabel.Text = _book.Title;
        AuthorLabel.Text = _book.Author;
        InfoAuthorLabel.Text = _book.Author;
        CategoryTagLabel.Text = string.IsNullOrWhiteSpace(_book.Category) ? "—" : _book.Category;
        CategoryLabel.Text = CategoryTagLabel.Text;
        StatusLabel.Text = _book.IsAvailable ? "✅ Rafta mevcut" : "❌ Ödünçte";
        StatusLabel.TextColor = _book.IsAvailable
            ? Color.FromArgb("#2E7D32")
            : Color.FromArgb("#C62828");

        PageLabel.Text = _book.PageCount > 0 ? _book.PageCount.ToString() : "—";
        YearLabel.Text = _book.PublishYear > 0 ? _book.PublishYear.ToString() : "—";
        IsbnLabel.Text = string.IsNullOrWhiteSpace(_book.ISBN) ? "—" : _book.ISBN;
        PublisherLabel.Text = string.IsNullOrWhiteSpace(_book.Publisher) ? "—" : _book.Publisher;
        DescLabel.Text = string.IsNullOrWhiteSpace(_book.Description)
            ? "Bu kitap için henüz açıklama eklenmemiş."
            : _book.Description;

        AverageRatingView.Rating = _book.AverageRating;
        RatingLabel.Text = _book.AverageRating?.ToString("0.0") ?? "—";
        ReviewCountLabel.Text = $"({_book.RatingCount})";
        CoverView.BookId = _book.Id;
        UpdateMyRatingDisplay();

        if (SessionHelper.IsAdmin)
        {
            AdminButtons.IsVisible = true;
            FavoriteBtn.IsVisible = false;
            BorrowBtn.IsVisible = false;
        }
        else
        {
            AdminButtons.IsVisible = false;
            BorrowBtn.IsVisible = true;
            BorrowBtn.IsEnabled = _book.IsAvailable;
            BorrowBtn.BackgroundColor = _book.IsAvailable
                ? Color.FromArgb("#FF6F00")
                : Colors.Gray;
        }
    }

    private void ConfigureRatingSection()
    {
        var canRate = SessionHelper.CurrentUser is not null && !SessionHelper.IsAdmin;
        UserRatingCard.IsVisible = canRate;
        MyRatingView.IsInteractive = canRate && !_isSubmittingRating;
    }

    private async Task LoadMyRatingAsync()
    {
        if (SessionHelper.CurrentUser is null || SessionHelper.IsAdmin || _book.Id <= 0)
        {
            return;
        }

        var myRating = await _bookService.GetMyRatingAsync(_book.Id);
        _book.MyRating = myRating;
    }

    private void UpdateMyRatingDisplay()
    {
        MyRatingView.Rating = _book.MyRating;
        MyRatingLabel.Text = _book.MyRating?.ToString("0.0") ?? "Seçin";
    }

    private void UpdateAverageRatingDisplay()
    {
        AverageRatingView.Rating = _book.AverageRating;
        RatingLabel.Text = _book.AverageRating?.ToString("0.0") ?? "—";
        ReviewCountLabel.Text = $"({_book.RatingCount})";
    }

    private async void MyRatingView_RatingChanged(object? sender, decimal score)
    {
        if (_isSubmittingRating || SessionHelper.CurrentUser is null || SessionHelper.IsAdmin)
        {
            return;
        }

        _isSubmittingRating = true;
        ConfigureRatingSection();
        RatingStatusLabel.IsVisible = true;
        RatingStatusLabel.Text = "Kaydediliyor...";
        RatingStatusLabel.TextColor = Color.FromArgb("#757575");

        var result = await _bookService.RateBookAsync(_book.Id, score);

        _isSubmittingRating = false;
        ConfigureRatingSection();

        if (!result.IsSuccess)
        {
            RatingStatusLabel.Text = result.ErrorMessage ?? "Puan kaydedilemedi.";
            RatingStatusLabel.TextColor = Color.FromArgb("#C62828");
            MyRatingView.Rating = _book.MyRating;
            UpdateMyRatingDisplay();
            return;
        }

        _book.MyRating = result.MyRating;
        _book.AverageRating = result.AverageRating;
        _book.RatingCount = result.RatingCount;
        UpdateMyRatingDisplay();
        UpdateAverageRatingDisplay();

        RatingStatusLabel.Text = "Puanınız kaydedildi.";
        RatingStatusLabel.TextColor = Color.FromArgb("#2E7D32");
    }

    private async Task RefreshFavoriteStateAsync()
    {
        if (SessionHelper.CurrentUser == null || SessionHelper.IsAdmin)
            return;

        var favs = await _bookService.GetFavoritesAsync(SessionHelper.CurrentUser.Id);
        _book.IsFavorite = favs.Any(f => f.BookId == _book.Id);
        UpdateFavoriteButton();
    }

    private void UpdateFavoriteButton()
    {
        if (_book.IsFavorite)
        {
            FavoriteBtn.Text = "✓  Favorilerde";
            FavoriteBtn.BackgroundColor = Color.FromArgb("#4A4758");
        }
        else
        {
            FavoriteBtn.Text = "+  Favorilere Ekle";
            FavoriteBtn.BackgroundColor = Color.FromArgb("#5E4BB6");
        }
    }

    private async void Back_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void FavoriteHeader_Clicked(object sender, EventArgs e)
        => await ToggleFavoriteAsync();

    private async void FavoriteBtn_Clicked(object sender, EventArgs e)
        => await ToggleFavoriteAsync();

    private async Task ToggleFavoriteAsync()
    {
        if (SessionHelper.CurrentUser == null)
        {
            await DisplayAlert("Giriş", "Favorilere eklemek için giriş yapmalısın.", "Tamam");
            return;
        }

        var userId = SessionHelper.CurrentUser.Id;
        if (_book.IsFavorite)
        {
            var removed = await _bookService.RemoveFavoriteAsync(userId, _book.Id);
            if (!removed)
            {
                await DisplayAlert("Hata", "Favorilerden çıkarılamadı.", "Tamam");
                return;
            }

            _book.IsFavorite = false;
            await DisplayAlert("Favoriler", "Kitap favorilerden çıkarıldı.", "Tamam");
        }
        else
        {
            var added = await _bookService.AddFavoriteAsync(userId, _book.Id);
            if (!added)
            {
                await DisplayAlert("Hata", "Favorilere eklenemedi. API çalışıyor mu kontrol edin.", "Tamam");
                return;
            }

            _book.IsFavorite = true;
            await DisplayAlert("Favoriler", "Kitap favorilere eklendi.", "Tamam");
        }

        UpdateFavoriteButton();
    }

    private async void Edit_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new AddEditBookPage(_book));

    private async void Delete_Clicked(object sender, EventArgs e)
    {
        bool ok = await DisplayAlert("Sil", $"'{_book.Title}' silinsin mi?", "Sil", "İptal");
        if (!ok) return;
        bool success = await _bookService.DeleteBookAsync(_book.Id);
        if (success)
        {
            await DisplayAlert("✅", "Kitap silindi.", "Tamam");
            await Shell.Current.GoToAsync("..");
        }
        else
            await DisplayAlert("Hata", "Silme başarısız.", "Tamam");
    }

    private async void BorrowThis_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new BorrowPage(new List<Book> { _book }));
}
