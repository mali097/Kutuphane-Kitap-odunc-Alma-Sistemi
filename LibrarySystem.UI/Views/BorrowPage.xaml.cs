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
        ConfigureDueDatePicker();

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
        var result = await _borrowService.BorrowBookAsync(selected.Id, userId, _dueDate);
        if (!result.Success)
        {
            ShowError(result.ErrorMessage ?? "Ödünç alma başarısız.");
            return;
        }

        var loanDays = GetLoanDayCount(_dueDate);
        await DisplayAlert(
            "✅",
            $"Kitap ödünç alındı.\n{DateTime.Today:dd.MM.yyyy} tarihinde \"{selected.Title}\" kitabını teslim aldınız. {_dueDate:dd.MM.yyyy} tarihine kadar ({loanDays} gün içinde) kitabı teslim etmeniz gerekiyor.",
            "Tamam");
        await Navigation.PopAsync();
    }

    private async void Back_Clicked(object? sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
    }

    private void ConfigureDueDatePicker()
    {
        var today = DateTime.Today;
        var maxDue = today.AddDays(LoanPeriodDays);

        DueDatePicker.MinimumDate = today;
        DueDatePicker.MaximumDate = maxDue;
        DueDatePicker.Date = maxDue;
        UpdateDueDate(maxDue);
    }

    private void DueDatePicker_DateSelected(object? sender, DateChangedEventArgs e)
    {
        UpdateDueDate(e.NewDate);
    }

    private void UpdateDueDate(DateTime date)
    {
        var today = DateTime.Today;
        var clamped = date.Date;
        if (clamped < today)
            clamped = today;
        if (clamped > today.AddDays(LoanPeriodDays))
            clamped = today.AddDays(LoanPeriodDays);

        _dueDate = clamped;
        var days = GetLoanDayCount(clamped);
        DueDateLabel.Text = days >= LoanPeriodDays
            ? $"Son teslim: {clamped:dd.MM.yyyy} ({LoanPeriodDays} gün)"
            : $"Seçilen süre: {days} gün — son teslim {clamped:dd.MM.yyyy}";
    }

    private static int GetLoanDayCount(DateTime dueDate)
    {
        var days = (int)(dueDate.Date - DateTime.Today).TotalDays;
        return Math.Max(1, days);
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
    }
}
