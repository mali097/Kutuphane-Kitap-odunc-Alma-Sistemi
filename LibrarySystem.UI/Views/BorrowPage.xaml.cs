using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class BorrowPage : ContentPage
{
    private const int LoanPeriodDays = 15;
    private readonly IBorrowService _borrowService;
    private readonly IBookService _bookService;
    private readonly List<Book> _books;
    private DateTime _dueDate;

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
        _dueDate = DateTime.Today.AddDays(LoanPeriodDays);
        DueDateLabel.Text = $"{_dueDate:dd.MM.yyyy} ({LoanPeriodDays} gün)";

        if (_books.Count == 0)
            _books.AddRange(await _bookService.GetAllBooksAsync());

        BookPicker.ItemsSource = _books.Where(b => b.IsAvailable).ToList();
        BookPicker.ItemDisplayBinding = new Binding(nameof(Book.Title));

        if (SessionHelper.IsAdmin)
        {
            OverdueSection.IsVisible = true;
            var overdue = await _borrowService.GetOverdueBorrowsAsync();
            OverdueList.ItemsSource = overdue;
        }
        else
        {
            OverdueSection.IsVisible = false;
        }
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

        var userId = SessionHelper.CurrentUser.Id;
        var result = await _borrowService.BorrowBookAsync(selected.Id, userId);
        if (!result.Success)
        {
            ShowError(result.ErrorMessage ?? "Ödünç alma başarısız.");
            return;
        }

        await DisplayAlert(
            "✅",
            $"Kitap ödünç alındı.\n{DateTime.Today:dd.MM.yyyy} tarihinde \"{selected.Title}\" kitabını teslim aldınız. {_dueDate:dd.MM.yyyy} tarihine kadar ({LoanPeriodDays} gün içinde) kitabı teslim etmeniz gerekiyor.",
            "Tamam");
        await Navigation.PopAsync();
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
    }
}
