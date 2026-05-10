using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Helpers;

public static class SessionHelper
{
    public static User? CurrentUser { get; set; }
}

// Keep compatibility if other code references SessionsHelper.
public static class SessionsHelper
{
    public static User? CurrentUser
    {
        get => SessionHelper.CurrentUser;
        set => SessionHelper.CurrentUser = value;
    }
}
