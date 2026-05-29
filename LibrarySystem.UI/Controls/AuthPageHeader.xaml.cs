namespace LibrarySystem.UI.Controls;

public partial class AuthPageHeader : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(AuthPageHeader), "Giriş Yap",
            propertyChanged: (b, _, v) => ((AuthPageHeader)b).TitleLabel.Text = (string)v);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(AuthPageHeader), string.Empty,
            propertyChanged: (b, _, v) => ((AuthPageHeader)b).SubtitleLabel.Text = (string)v);

    public static readonly BindableProperty IllustrationProperty =
        BindableProperty.Create(nameof(Illustration), typeof(AuthHeaderIllustration), typeof(AuthPageHeader),
            AuthHeaderIllustration.Login,
            propertyChanged: (b, _, v) => ((AuthPageHeader)b).ApplyIllustration((AuthHeaderIllustration)v));

    public static readonly BindableProperty ShowBackButtonProperty =
        BindableProperty.Create(nameof(ShowBackButton), typeof(bool), typeof(AuthPageHeader), false,
            propertyChanged: (b, _, v) => ((AuthPageHeader)b).BackButton.IsVisible = (bool)v);

    public event EventHandler? BackClicked;

    public AuthPageHeader()
    {
        InitializeComponent();
        ApplyIllustration(Illustration);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public AuthHeaderIllustration Illustration
    {
        get => (AuthHeaderIllustration)GetValue(IllustrationProperty);
        set => SetValue(IllustrationProperty, value);
    }

    public bool ShowBackButton
    {
        get => (bool)GetValue(ShowBackButtonProperty);
        set => SetValue(ShowBackButtonProperty, value);
    }

    private void ApplyIllustration(AuthHeaderIllustration illustration)
    {
        LoginBooksIllustration.IsVisible = illustration == AuthHeaderIllustration.Login;
        RegisterBookIllustration.IsVisible = illustration == AuthHeaderIllustration.Register;
    }

    private void BackButton_Clicked(object? sender, EventArgs e)
        => BackClicked?.Invoke(this, EventArgs.Empty);
}

public enum AuthHeaderIllustration
{
    Login,
    Register
}
