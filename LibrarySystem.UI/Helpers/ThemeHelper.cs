using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace LibrarySystem.UI.Helpers;

public static class ThemeHelper
{
    private const string PreferenceKey = "app_theme_choice";

    public const string Light = "Light";
    public const string Dark = "Dark";
    public const string System = "System";

    public static void ApplySavedTheme()
    {
        if (Application.Current is null) return;

        var saved = Preferences.Get(PreferenceKey, System);
        Application.Current.UserAppTheme = saved switch
        {
            Light => AppTheme.Light,
            Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    public static void ApplyThemeChoice(string choice)
    {
        if (Application.Current is null) return;

        Application.Current.UserAppTheme = choice switch
        {
            Light => AppTheme.Light,
            Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        Preferences.Set(PreferenceKey, choice);
    }

    /// <summary>
    /// DisplayActionSheet'ten gelen Türkçe seçim metnini kalıcı tema sabitine çevirir.
    /// </summary>
    public static string? ChoiceFromActionSheet(string? pick) => pick switch
    {
        "Açık tema" => Light,
        "Koyu tema" => Dark,
        "Sistem varsayılanı" => System,
        _ => null
    };

    /// <summary>
    /// Aktif temaya göre AppThemeColors.xaml'de "{baseKey}Light" veya "{baseKey}Dark"
    /// anahtarıyla tanımlı rengi döndürür.
    /// </summary>
    public static Color GetColor(string baseKey)
    {
        if (Application.Current is null)
            return Colors.Gray;

        var theme = Application.Current.RequestedTheme;
        var suffix = theme == AppTheme.Dark ? "Dark" : "Light";
        var resourceKey = baseKey + suffix;

        if (Application.Current.Resources.TryGetValue(resourceKey, out var value) && value is Color color)
            return color;

        return Colors.Gray;
    }

    /// <summary>
    /// Alt sekme barında aktif/pasif buton görünümlerini tema renkleriyle uygular.
    /// </summary>
    public static void ApplyBottomTab(Button activeTab, params Button[] inactiveTabs)
    {
        if (activeTab is null) return;

        activeTab.BackgroundColor = GetColor("TabActiveBgColor");
        activeTab.TextColor = GetColor("TabActiveTextColor");
        activeTab.CornerRadius = 18;
        activeTab.FontAttributes = FontAttributes.Bold;

        foreach (var tab in inactiveTabs)
        {
            if (tab is null) continue;
            tab.BackgroundColor = Colors.Transparent;
            tab.TextColor = GetColor("TabInactiveColor");
            tab.FontAttributes = FontAttributes.None;
        }
    }
}
