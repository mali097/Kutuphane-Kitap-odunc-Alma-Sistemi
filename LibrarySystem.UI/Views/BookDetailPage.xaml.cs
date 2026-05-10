using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class BookDetailPage : ContentPage
{
    private readonly IBookService _bookService;
    private readonly IBorrowService _borrowService;
    private Book _book;

    public BookDetailPage(Book book)
    {
        InitializeComponent();
        _bookService = new BookService();
        _borrowService = new BorrowService();
        _book = book;
        LoadBookData();
    }

    private void LoadBookData()
    {
        Title = _book.Title;
        TitleLabel.Text = _book.Title;
        AuthorLabel.Text = $"✍️ {_book.Author}";
        StatusLabel.Text = _book.IsAvailable ? "✅ Rafta Mevcut" : "❌ Ödünçte";
        PageLabel.Text = _book.PageCount > 0 ? _book.PageCount.ToString() : "-";
        YearLabel.Text = _book.PublishYear > 0 ? _book.PublishYear.ToString() : "-";
        CategoryLabel.Text = _book.Category;
        PublisherLabel.Text = string.IsNullOrEmpty(_book.Publisher) ? "-" : _book.Publisher;
        DescLabel.Text = string.IsNullOrEmpty(_book.Description) ? "Açıklama bulunmuyor." : _book.Description;
        CreatedLabel.Text = $"Ekleyen: {_book.CreatedBy} • {_book.CreatedAt:dd.MM.yyyy}";
        UpdatedLabel.Text = _book.UpdatedAt.HasValue
            ? $"Güncelleyen: {_book.UpdatedBy} • {_book.UpdatedAt:dd.MM.yyyy}" : "Güncelleme yok";

        // Admin görünümü
        if (SessionHelper.IsAdmin)
        {
            AdminButtons.IsVisible = true;
            BorrowBtn.IsVisible = false;
        }
        else
        {
            BorrowBtn.IsEnabled = _book.IsAvailable;
            BorrowBtn.BackgroundColor = _book.IsAvailable
                ? Color.FromArgb("#FF6F00") : Colors.Gray;
        }
    }

    private async void Edit_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new AddEditBookPage(_book));

    private async void Delete_Clicked(object sender, EventArgs e)
    {
        bool ok = await DisplayAlert("Sil", $"'{_book.Title}' silinsin mi?", "Sil", "İptal");
        if (!ok) return;
        bool success = await _bookService.DeleteBookAsync(_book.Id);
        if (success) { await DisplayAlert("✅", "Kitap silindi.", "Tamam"); await Navigation.PopAsync(); }
        else await DisplayAlert("Hata", "Silme başarısız.", "Tamam");
    }

    private async void BorrowThis_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new BorrowPage(new List<Book> { _book }));
}