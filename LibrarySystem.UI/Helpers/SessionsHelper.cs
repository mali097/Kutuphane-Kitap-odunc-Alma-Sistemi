using LibrarySystem.UI.Models;

namespace LibrarySystem.UI.Helpers;

public static class SessionHelper
{
    public static User? CurrentUser { get; set; }

    public static bool IsAdmin => string.Equals(CurrentUser?.Role, "Admin", StringComparison.OrdinalIgnoreCase);

    public static bool IsAuthor => string.Equals(CurrentUser?.Role, "Author", StringComparison.OrdinalIgnoreCase);

    public static void NormalizeProfile(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Email) && user.Username.Contains('@', StringComparison.Ordinal))
        {
            user.Email = user.Username.Trim();
            user.Username = GetEmailLocalPart(user.Email);
            return;
        }

        if (!string.IsNullOrWhiteSpace(user.Email)
            && user.Username.Contains('@', StringComparison.Ordinal)
            && string.Equals(user.Username.Trim(), user.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            user.Username = GetEmailLocalPart(user.Email);
        }
    }

    public static string GetDisplayUsername(User? user)
    {
        if (user is null) return "Kullanıcı";

        NormalizeProfile(user);

        if (!string.IsNullOrWhiteSpace(user.Username) && !user.Username.Contains('@', StringComparison.Ordinal))
            return user.Username.Trim();

        if (!string.IsNullOrWhiteSpace(user.Email))
            return GetEmailLocalPart(user.Email);

        return "Kullanıcı";
    }

    public static string GetEmailLocalPart(string email)
    {
        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');
        return at > 0 ? trimmed[..at] : trimmed;
    }
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
