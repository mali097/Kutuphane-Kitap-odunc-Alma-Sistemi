using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Services;

namespace LibrarySystem.UI.Views;

public partial class AddBookRecommendationPage : ContentPage
{
    private const int MaxThoughtsLength = 500;
    private readonly IRecommendationService _recommendationService;

    public AddBookRecommendationPage()
    {
        InitializeComponent();
        _recommendationService = new RecommendationService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!SessionHelper.IsAuthor)
        {
            _ = Navigation.PopAsync();
        }
    }

    private void Back_Clicked(object sender, EventArgs e)
        => Navigation.PopAsync();

    private void Cancel_Clicked(object sender, EventArgs e)
        => Navigation.PopAsync();

    private void Thoughts_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var length = ThoughtsEditor.Text?.Length ?? 0;
        if (length > MaxThoughtsLength)
        {
            ThoughtsEditor.Text = ThoughtsEditor.Text![..MaxThoughtsLength];
            length = MaxThoughtsLength;
        }

        CharCountLabel.Text = $"{length} / {MaxThoughtsLength}";
        UpdateShareButtonState();
    }

    private void Form_TextChanged(object? sender, TextChangedEventArgs e)
        => UpdateShareButtonState();

    private void UpdateShareButtonState()
    {
        var canShare = !string.IsNullOrWhiteSpace(BookTitleEntry.Text)
            && !string.IsNullOrWhiteSpace(ThoughtsEditor.Text);

        ShareButton.IsEnabled = canShare;
        ShareButton.Opacity = canShare ? 1 : 0.55;
    }

    private async void Share_Clicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        var bookTitle = BookTitleEntry.Text?.Trim() ?? string.Empty;
        var idea = ThoughtsEditor.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(bookTitle) || string.IsNullOrWhiteSpace(idea))
        {
            ShowError("Kitap adı ve düşünceleriniz zorunludur.");
            return;
        }

        SetBusy(true);
        try
        {
            var created = await _recommendationService.AddRecommendationAsync(bookTitle, idea);
            if (created is null)
            {
                ShowError("Öneri paylaşılamadı. Lütfen tekrar deneyin.");
                return;
            }

            await DisplayAlert("Başarılı", "Kitap öneriniz paylaşıldı.", "Tamam");
            await Navigation.PopAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        BusyIndicator.IsRunning = busy;
        BusyIndicator.IsVisible = busy;
        BookTitleEntry.IsEnabled = !busy;
        ThoughtsEditor.IsEnabled = !busy;

        if (busy)
        {
            ShareButton.IsEnabled = false;
            ShareButton.Opacity = 0.55;
        }
        else
        {
            UpdateShareButtonState();
        }
    }
}
