using LibrarySystem.UI.Helpers;
using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Views;

public partial class SupportPage : ContentPage
{
    private const uint ExpandDurationMs = 220;
    private const uint CollapseDurationMs = 180;

    private SupportHelpCategory? _selectedCategory;
    private readonly List<FaqAccordionEntry> _accordionEntries = new();
    private readonly List<SupportHelpCategory> _allCategories = CreateCategories();

    public SupportPage()
    {
        InitializeComponent();
        BuildCategoryList(_allCategories);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ThemeHelper.ApplyBottomTab(
            TabSettingsBtn,
            TabHomeBtn, TabCategoriesBtn, TabFavoritesBtn, TabNotificationsBtn);
    }

    private static List<SupportHelpCategory> CreateCategories() =>
    [
        new()
        {
            Id = "borrow",
            Icon = "📚",
            Title = "Kitap Ödünç İşlemleri",
            Subtitle = "Süre uzatma, iade ve rezervasyon işlemleri.",
            Faqs =
            [
                new()
                {
                    Question = "Ödünç süremi nasıl uzatabilirim?",
                    Answer = "Kitap detay sayfasından veya profilindeki aktif ödünçler listesinden \"Süre Uzat\" butonunu kullanabilirsin. Uzatma hakkın kitap başına sınırlı olabilir."
                },
                new()
                {
                    Question = "Kitabı nasıl iade ederim?",
                    Answer = "Kütüphane gişesine getirerek veya uygulama üzerinden iade işlemini başlatarak kitabı teslim edebilirsin."
                }
            ]
        },
        new()
        {
            Id = "account",
            Icon = "🔑",
            Title = "Hesap ve Giriş Sorunları",
            Subtitle = "Şifre yenileme, profil bilgileri ve güvenlik.",
            Faqs =
            [
                new()
                {
                    Question = "Yazar girişi ile Öğrenci girişi arasındaki fark nedir?",
                    Answer = "Yazar girişi içerik üretmek ve öneri paylaşmak içindir. Öğrenci girişi ise kütüphaneyi keşfetmek ve ödünç almak içindir."
                },
                new()
                {
                    Question = "Giriş yaparken hata alıyorum, ne yapmalıyım?",
                    Answer = "İnternet bağlantınızı kontrol edin. Sorun devam ederse sistem yöneticinizle iletişime geçin."
                }
            ]
        },
        new()
        {
            Id = "app",
            Icon = "📱",
            Title = "Uygulama Hataları & Bildirimler",
            Subtitle = "Bildirim gelmeme ve genel uygulama sorunları.",
            Faqs =
            [
                new()
                {
                    Question = "Bildirimler neden gelmiyor?",
                    Answer = "Cihaz ayarlarından uygulama bildirimlerinin açık olduğundan emin ol. Ayarlar > Bildirim ayarları bölümünü de kontrol edebilirsin."
                },
                new()
                {
                    Question = "Uygulama beklenmedik şekilde kapanıyor, ne yapmalıyım?",
                    Answer = "Uygulamayı kapatıp yeniden açmayı dene. Güncelleme varsa yükleyip tekrar giriş yap. Sorun sürerse sistem yöneticinle iletişime geç."
                }
            ]
        }
    ];

    private void BuildCategoryList(IEnumerable<SupportHelpCategory> categories)
    {
        CategoryListContainer.Children.Clear();

        foreach (var cat in categories)
            CategoryListContainer.Children.Add(CreateCategoryCard(cat));
    }

    private Border CreateCategoryCard(SupportHelpCategory category)
    {
        var card = new Border
        {
            Style = (Style)Application.Current!.Resources["ThemedCard"],
            Padding = new Thickness(14, 12)
        };

        var iconLabel = new Label
        {
            Text = category.Icon,
            FontSize = 22,
            VerticalOptions = LayoutOptions.Start
        };

        var titleLabel = new Label
        {
            Text = category.Title,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            TextColor = ThemeHelper.GetColor("TextPrimaryColor"),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var subtitleLabel = new Label
        {
            Text = category.Subtitle,
            FontSize = 12,
            TextColor = ThemeHelper.GetColor("TextSecondaryColor"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        var textStack = new VerticalStackLayout
        {
            Spacing = 4,
            Children = { titleLabel, subtitleLabel }
        };

        var chevron = new Label
        {
            Text = "›",
            FontSize = 22,
            TextColor = ThemeHelper.GetColor("AccentChevronColor"),
            VerticalOptions = LayoutOptions.Center
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12,
            Children = { iconLabel, textStack, chevron }
        };
        Grid.SetColumn(textStack, 1);
        Grid.SetColumn(chevron, 2);

        card.Content = grid;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => OpenCategory(category);
        card.GestureRecognizers.Add(tap);

        return card;
    }

    private void OpenCategory(SupportHelpCategory category)
    {
        _selectedCategory = category;
        CategoryBackTitle.Text = category.Title;
        CategoryBackBar.IsVisible = true;
        SearchBarWrap.IsVisible = false;
        CategoryListContainer.IsVisible = false;
        FaqAccordionContainer.IsVisible = true;
        BuildFaqAccordion(category.Faqs);
    }

    private void CategoryBack_Tapped(object? sender, EventArgs e)
        => ShowCategoryList();

    private void ShowCategoryList()
    {
        _selectedCategory = null;
        CategoryBackBar.IsVisible = false;
        SearchBarWrap.IsVisible = true;
        CategoryListContainer.IsVisible = true;
        FaqAccordionContainer.IsVisible = false;
        _accordionEntries.Clear();
        FaqAccordionContainer.Children.Clear();
        ApplySearchFilter();
    }

    private void BuildFaqAccordion(IReadOnlyList<SupportFaqItem> items)
    {
        FaqAccordionContainer.Children.Clear();
        _accordionEntries.Clear();

        foreach (var item in items)
            FaqAccordionContainer.Children.Add(CreateFaqCard(item));
    }

    private View CreateFaqCard(SupportFaqItem item)
    {
        var chevron = new Label
        {
            Text = "›",
            FontSize = 20,
            TextColor = ThemeHelper.GetColor("AccentPurpleColor"),
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };

        var questionLabel = new Label
        {
            Text = item.Question,
            TextColor = ThemeHelper.GetColor("TextPrimaryColor"),
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap,
            VerticalOptions = LayoutOptions.Center
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10,
            Padding = new Thickness(14, 14),
            Children = { questionLabel, chevron }
        };
        Grid.SetColumn(chevron, 1);

        var answerLabel = new Label
        {
            Text = item.Answer,
            TextColor = ThemeHelper.GetColor("TextSecondaryColor"),
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap,
            Margin = new Thickness(14, 0, 14, 14),
            Opacity = 0,
            IsVisible = false,
            ScaleY = 0,
            AnchorY = 0
        };

        var card = new Border
        {
            Style = (Style)Application.Current!.Resources["ThemedCard"],
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children = { headerGrid, answerLabel }
            }
        };

        var entry = new FaqAccordionEntry(answerLabel, chevron);
        _accordionEntries.Add(entry);

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await ToggleFaqAsync(entry);
        card.GestureRecognizers.Add(tap);

        return card;
    }

    private async Task ToggleFaqAsync(FaqAccordionEntry entry)
    {
        if (entry.IsExpanded)
        {
            await CollapseAsync(entry);
            return;
        }

        foreach (var other in _accordionEntries)
        {
            if (other != entry && other.IsExpanded)
                await CollapseAsync(other);
        }

        await ExpandAsync(entry);
    }

    private static async Task ExpandAsync(FaqAccordionEntry entry)
    {
        entry.IsExpanded = true;
        entry.Answer.IsVisible = true;

        await Task.WhenAll(
            entry.Answer.FadeTo(1, ExpandDurationMs, Easing.CubicOut),
            entry.Answer.ScaleYTo(1, ExpandDurationMs, Easing.CubicOut),
            entry.Chevron.RotateTo(90, ExpandDurationMs, Easing.CubicOut));
    }

    private static async Task CollapseAsync(FaqAccordionEntry entry)
    {
        entry.IsExpanded = false;

        await Task.WhenAll(
            entry.Answer.FadeTo(0, CollapseDurationMs, Easing.CubicIn),
            entry.Answer.ScaleYTo(0, CollapseDurationMs, Easing.CubicIn),
            entry.Chevron.RotateTo(0, CollapseDurationMs, Easing.CubicIn));

        entry.Answer.IsVisible = false;
    }

    private void Search_TextChanged(object? sender, TextChangedEventArgs e)
        => ApplySearchFilter();

    private void ApplySearchFilter()
    {
        if (_selectedCategory is not null) return;

        var term = SearchEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            BuildCategoryList(_allCategories);
            return;
        }

        var filtered = _allCategories.Where(c =>
            c.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            c.Subtitle.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            c.Faqs.Any(f =>
                f.Question.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                f.Answer.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();

        BuildCategoryList(filtered);
    }

    private async void Back_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void Settings_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void TabHome_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToHomeAsync();

    private async void TabCategories_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToCategoriesAsync();

    private async void TabFavorites_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToFavoritesAsync();

    private async void TabNotifications_Clicked(object sender, EventArgs e)
        => await TabNavigation.GoToNotificationsAsync();

    private async void TabSettings_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private sealed class FaqAccordionEntry(Label answer, Label chevron)
    {
        public Label Answer { get; } = answer;
        public Label Chevron { get; } = chevron;
        public bool IsExpanded { get; set; }
    }
}
