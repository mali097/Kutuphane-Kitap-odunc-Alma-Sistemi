namespace LibrarySystem.UI.Helpers;

public static class ApiClientHelper
{
    public static string GetBaseUrl()
    {
#if ANDROID
        return "http://10.0.2.2:5279";
#else
        return "http://localhost:5279";
#endif
    }

    public static HttpClient CreateClient() =>
        new() { BaseAddress = new Uri(GetBaseUrl()) };
}
