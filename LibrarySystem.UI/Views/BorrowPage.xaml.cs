using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class BorrowPage : ContentPage
{
    private readonly IBorrowService _borrowService;
    private readonly IBookService _bookService;
    private readonly List<Book> _books;

    public BorrowPage(List<Book>? availableBooks = null)
    {
        InitializeComponent();
        _borrowService = new BorrowService();
        _bookService = new BookService();
        _books = availableBooks ?? new List<Book>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        ErrorLabel.IsVisible = false;

        if (_books.Count == 0)
            _books.AddRange(await _bookService.GetAllBooksAsync());

        BookPicker.ItemsSource = _books.Where(b => b.IsAvailable).ToList();
        BookPicker.ItemDisplayBinding = new Binding(nameof(Book.Title));
        DueDatePicker.Date = DateTime.Today.AddDays(14);

        var overdue = await _borrowService.GetOverdueBorrowsAsync();
        OverdueList.ItemsSource = overdue;
    }

    private async void Borrow_Clicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (SessionHelper.CurrentUser == null)
        {
            ShowError("Kullanıcı oturumu bulunamadı.");
            return;
        }

        if (BookPicker.SelectedItem is not Book selected)
        {
            ShowError("Lütfen bir kitap seçin.");
            return;
        }

        if (DueDatePicker.Date <= DateTime.Today)
        {
            ShowError("İade tarihi bugünden ileri olmalı.");
            return;
        }

        bool ok = await _borrowService.BorrowBookAsync(selected.Id, SessionHelper.CurrentUser.Id, DueDatePicker.Date);
        if (!ok)
        {
            ShowError("Ödünç alma başarısız. API çalışıyor mu?");
            return;
        }

        await DisplayAlert("✅", "Kitap ödünç alındı.", "Tamam");
        await Navigation.PopAsync();
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
    }
}