namespace LibrarySystem.UI.Views;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private async void Back_Clicked(object sender, EventArgs e)
        => await Navigation.PopAsync();
}
