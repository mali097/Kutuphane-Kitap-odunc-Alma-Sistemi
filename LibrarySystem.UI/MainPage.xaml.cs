namespace LibrarySystem.UI;

public partial class MainPage : ContentPage
{
    private readonly List<SimpleBook> books = new();

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnAddBookClicked(object sender, EventArgs e)
    {
        string title = TitleEntry.Text?.Trim() ?? "";
        string author = AuthorEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(author))
        {
            DisplayAlert("Hata", "Kitap adı ve yazar boş olamaz", "OK");
            return;
        }

        books.Add(new SimpleBook
        {
            Title = title,
            Author = author
        });

        BooksList.ItemsSource = null;
        BooksList.ItemsSource = books;

        TitleEntry.Text = "";
        AuthorEntry.Text = "";
    }
}

public class SimpleBook
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
}