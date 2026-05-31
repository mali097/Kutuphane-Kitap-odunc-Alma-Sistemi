using LibrarySystem.UI.Helpers;

namespace LibrarySystem.UI.Controls;

public partial class BookCoverImage : ContentView
{
    public static readonly BindableProperty BookIdProperty =
        BindableProperty.Create(
            nameof(BookId),
            typeof(int),
            typeof(BookCoverImage),
            0,
            propertyChanged: (bindable, _, _) => ((BookCoverImage)bindable).LoadCoverAsync());

    public static readonly BindableProperty PlaceholderFontSizeProperty =
        BindableProperty.Create(
            nameof(PlaceholderFontSize),
            typeof(double),
            typeof(BookCoverImage),
            28d,
            propertyChanged: (bindable, _, newValue) =>
            {
                var view = (BookCoverImage)bindable;
                view.PlaceholderLabel.FontSize = (double)newValue;
            });

    private CancellationTokenSource? _loadCts;

    public BookCoverImage()
    {
        InitializeComponent();
    }

    public int BookId
    {
        get => (int)GetValue(BookIdProperty);
        set => SetValue(BookIdProperty, value);
    }

    public double PlaceholderFontSize
    {
        get => (double)GetValue(PlaceholderFontSizeProperty);
        set => SetValue(PlaceholderFontSizeProperty, value);
    }

    private async void LoadCoverAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        await BookCoverHelper.SetCoverAsync(CoverImage, PlaceholderLabel, BookId, token);
    }
}
