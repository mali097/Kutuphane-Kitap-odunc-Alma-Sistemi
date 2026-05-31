using LibrarySystem.UI.Views;

namespace LibrarySystem.UI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(CategoriesPage), typeof(CategoriesPage));
        Routing.RegisterRoute(nameof(CategoryDetailPage), typeof(CategoryDetailPage));
        Routing.RegisterRoute(nameof(FavoritesPage), typeof(FavoritesPage));
        Routing.RegisterRoute(nameof(NotificationsPage), typeof(NotificationsPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(UserPanelPage), typeof(UserPanelPage));
        Routing.RegisterRoute(nameof(BookDetailPage), typeof(BookDetailPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(AdminPage), typeof(AdminPage));
        Routing.RegisterRoute(nameof(ChangePasswordPage), typeof(ChangePasswordPage));
        Routing.RegisterRoute(nameof(BorrowPage), typeof(BorrowPage));
        Routing.RegisterRoute(nameof(AddEditBookPage), typeof(AddEditBookPage));
        Routing.RegisterRoute(nameof(SupportPage), typeof(SupportPage));
        Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        Routing.RegisterRoute(nameof(AddBookRecommendationPage), typeof(AddBookRecommendationPage));
    }
}
