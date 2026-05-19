using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace LibrarySystem.UI;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly ObservableCollection<BookCardModel> _books = new();
    private readonly ObservableCollection<WeeklyRecommendationModel> _recommendations = new();

    public MainPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ResolveApiBaseAddress())
        };
        BooksList.ItemsSource = _books;
        WeeklyRecommendationsList.ItemsSource = _recommendations;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAllDataAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadAllDataAsync();
    }

    private async void OnBorrowBookClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not int bookId)
        {
            return;
        }

        if (!int.TryParse(UserIdEntry.Text?.Trim(), out var userId) || userId <= 0)
        {
            await DisplayAlert("Hata", "Lütfen geçerli bir kullanıcı ID girin.", "Tamam");
            return;
        }

        var response = await _httpClient.PostAsJsonAsync(
            "/api/borrow-records/borrow",
            new BorrowBookApiRequest(userId, bookId));

        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync();
            await DisplayAlert("Ödünç Alma Başarısız", details, "Tamam");
            return;
        }

        await DisplayAlert("Başarılı", "Kitap ödünç alındı.", "Tamam");
        await LoadBooksAsync();
    }

    private async Task LoadAllDataAsync()
    {
        await LoadBooksAsync();
        await LoadWeeklyRecommendationsAsync();
    }

    private async Task LoadBooksAsync()
    {
        try
        {
            var books = await _httpClient.GetFromJsonAsync<List<BookApiResponse>>("/api/books")
                ?? new List<BookApiResponse>();
            _books.Clear();
            foreach (var book in books)
            {
                _books.Add(new BookCardModel
                {
                    Id = book.Id,
                    Title = book.Title,
                    AuthorLine = $"{book.Author} | Tür: {book.Genre}",
                    DetailLine = $"ISBN: {book.Isbn} | Yıl: {book.PublishYear}",
                    IsAvailable = book.IsAvailable,
                    AvailabilityText = book.IsAvailable ? "Durum: Aktif / Müsait" : "Durum: Ödünçte",
                    AvailabilityColor = book.IsAvailable ? Colors.ForestGreen : Colors.IndianRed
                });
            }
        }
        catch (Exception exception)
        {
            await DisplayAlert("Veri Hatası", $"Kitap listesi alınamadı: {exception.Message}", "Tamam");
        }
    }

    private async Task LoadWeeklyRecommendationsAsync()
    {
        try
        {
            var recommendations = await _httpClient
                .GetFromJsonAsync<List<WeeklyRecommendationApiResponse>>("/api/recommendations/weekly")
                ?? new List<WeeklyRecommendationApiResponse>();

            _recommendations.Clear();
            foreach (var recommendation in recommendations)
            {
                _recommendations.Add(new WeeklyRecommendationModel
                {
                    BookTitle = recommendation.BookTitle,
                    AuthorName = recommendation.AuthorName,
                    Idea = recommendation.Idea
                });
            }
        }
        catch (Exception)
        {
            // UI sessizce boş öneri alanı gösterebilir.
            _recommendations.Clear();
        }
    }

    private static string ResolveApiBaseAddress()
    {
#if ANDROID
        return "http://10.0.2.2:5279";
#else
        return "http://localhost:5279";
#endif
    }
}

public sealed class BookCardModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AuthorLine { get; set; } = string.Empty;
    public string DetailLine { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string AvailabilityText { get; set; } = string.Empty;
    public Color AvailabilityColor { get; set; } = Colors.Gray;
}

public sealed class WeeklyRecommendationModel
{
    public string BookTitle { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Idea { get; set; } = string.Empty;
}

internal sealed record BorrowBookApiRequest(int UserId, int BookId);

internal sealed record BookApiResponse(
    int Id,
    string Title,
    string Author,
    string Isbn,
    string Genre,
    int PublishYear,
    bool IsAvailable
);

internal sealed record WeeklyRecommendationApiResponse(
    int RecommendationId,
    string BookTitle,
    string Idea,
    int AuthorUserId,
    string AuthorName,
    DateTime CreatedAt,
    DateTime WeekStartDate,
    DateTime WeekEndDate
);