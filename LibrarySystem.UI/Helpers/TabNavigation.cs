using LibrarySystem.UI.Views;

namespace LibrarySystem.UI.Helpers;

public static class TabNavigation
{
    public static Task GoToHomeAsync()
    {
        MainTabNavigationState.OpenCategoriesTab = false;
        return Shell.Current.GoToAsync("//MainPage");
    }

    public static Task GoToCategoriesAsync()
    {
        MainTabNavigationState.OpenCategoriesTab = true;
        return Shell.Current.GoToAsync("//MainPage");
    }

    public static Task GoToFavoritesAsync()
        => Shell.Current.GoToAsync(nameof(FavoritesPage));

    public static Task GoToNotificationsAsync()
        => Shell.Current.GoToAsync(nameof(NotificationsPage));

    public static Task GoToSettingsAsync()
        => Shell.Current.GoToAsync(nameof(SettingsPage));
}
