using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Services;
using Book = LibrarySystem.UI.Models.Book;

namespace LibrarySystem.UI.Views;

public partial class AddEditBookPage : ContentPage
{
    private readonly IBookService _bookService;
    private readonly Book? _existingBook;
    private readonly bool _isEdit;
    private readonly List<string> _categories =
        new() { "Roman", "Bilim", "Tarih", "Teknoloji", "Felsefe", "Çocuk", "Biyografi" };

    public AddEditBookPage() : this(null) { }

    public AddEditBookPage(Book? book)
    {
        InitializeComponent();
        _bookService = new BookService();
        _existingBook = book;
        _isEdit = book != null;

        CategoryPicker.ItemsSource = _categories;

        if (_isEdit)
        {
            Title = "Kitabı Düzenle";
            PageTitleLabel.Text = "✏️ Kitabı Düzenle";
            TitleEntry.Text = book!.Title;
            AuthorEntry.Text = book.Author;
            IsbnEntry.Text = book.ISBN;
            PageEntry.Text = book.PageCount.ToString();
            YearEntry.Text = book.PublishYear.ToString();
            PublisherEntry.Text = book.Publisher;
            DescEditor.Text = book.Description;
            IsAvailableCheckBox.IsChecked = book.IsAvailable;
            CategoryPicker.SelectedIndex = _categories.IndexOf(book.Category);
        }
        else
        {
            Title = "Kitap Ekle";
            PageTitleLabel.Text = "➕ Yeni Kitap Ekle";
        }
    }

    private async void Save_Clicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        { ShowError("Kitap adı boş olamaz."); return; }
        if (string.IsNullOrWhiteSpace(AuthorEntry.Text))
        { ShowError("Yazar boş olamaz."); return; }
        if (string.IsNullOrWhiteSpace(IsbnEntry.Text))
        { ShowError("ISBN boş olamaz."); return; }
        if (CategoryPicker.SelectedIndex == -1)
        { ShowError("Kategori seçin."); return; }

        var username = SessionHelper.CurrentUser?.Username ?? "system";
        bool success;

        if (_isEdit)
        {
            _existingBook!.Title = TitleEntry.Text.Trim();
            _existingBook.Author = AuthorEntry.Text.Trim();
            _existingBook.ISBN = IsbnEntry.Text.Trim();
            _existingBook.Category = CategoryPicker.SelectedItem.ToString()!;
            _existingBook.PageCount = int.TryParse(PageEntry.Text, out var p) ? p : 0;
            _existingBook.PublishYear = int.TryParse(YearEntry.Text, out var y) ? y : 0;
            _existingBook.Publisher = PublisherEntry.Text?.Trim() ?? "";
            _existingBook.Description = DescEditor.Text?.Trim() ?? "";
            _existingBook.IsAvailable = IsAvailableCheckBox.IsChecked;
            _existingBook.UpdatedAt = DateTime.Now;
            _existingBook.UpdatedBy = username;
            success = await _bookService.UpdateBookAsync(_existingBook);
        }
        else
        {
            var book = new Book
            {
                Title = TitleEntry.Text.Trim(),
                Author = AuthorEntry.Text.Trim(),
                ISBN = IsbnEntry.Text.Trim(),
                Category = CategoryPicker.SelectedItem.ToString()!,
                PageCount = int.TryParse(PageEntry.Text, out var p) ? p : 0,
                PublishYear = int.TryParse(YearEntry.Text, out var y) ? y : 0,
                Publisher = PublisherEntry.Text?.Trim() ?? "",
                Description = DescEditor.Text?.Trim() ?? "",
                IsAvailable = IsAvailableCheckBox.IsChecked,
                CreatedAt = DateTime.Now,
                CreatedBy = username
            };
            success = await _bookService.AddBookAsync(book);
        }

        if (success)
        {
            await DisplayAlert("✅", _isEdit ? "Kitap güncellendi." : "Kitap eklendi.", "Tamam");
            await Navigation.PopAsync();
        }
        else ShowError("İşlem başarısız. API çalışıyor mu?");
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
    }
}