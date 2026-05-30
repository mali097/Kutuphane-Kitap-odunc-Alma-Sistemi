namespace LibrarySystem.UI.Helpers;

public static class ApiClientHelper
{
    public static string GetBaseUrl()
    {
#if ANDROID
        return "http://10.0.2.2:5279";
#else
        return "http://127.0.0.1:5279";
#endif
    }

    public static string GetBookCoverUrl(int bookId)
        => $"{GetBaseUrl()}/books/{bookId}/cover";

    public static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler();
#if DEBUG
        handler.ServerCertificateCustomValidationCallback = static (_, _, _, _) => true;
#endif

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(GetBaseUrl()),
            Timeout = TimeSpan.FromSeconds(30)
        };
        ApplySessionHeaders(client);
        return client;
    }

    public static void ApplySessionHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("X-User-Token");
        client.DefaultRequestHeaders.Remove("X-Author-Token");
        client.DefaultRequestHeaders.Remove("X-Admin-Token");
        client.DefaultRequestHeaders.Remove("X-Actor-User-Id");

        var user = SessionHelper.CurrentUser;
        if (user is null || user.Id <= 0)
        {
            return;
        }

        client.DefaultRequestHeaders.Add("X-Actor-User-Id", user.Id.ToString());

        if (string.IsNullOrWhiteSpace(user.Token))
        {
            return;
        }

        if (SessionHelper.IsAdmin)
        {
            client.DefaultRequestHeaders.Add("X-Admin-Token", user.Token);
            return;
        }

        client.DefaultRequestHeaders.Add("X-User-Token", user.Token);
        if (string.Equals(user.Role, "Author", StringComparison.OrdinalIgnoreCase))
        {
            client.DefaultRequestHeaders.Add("X-Author-Token", user.Token);
        }
    }
}
