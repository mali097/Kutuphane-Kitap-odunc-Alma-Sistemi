namespace LibrarySystem.UI.Models;

public sealed class LoginResult
{
    public User? User { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsSuccess => User is not null && string.IsNullOrEmpty(ErrorMessage);

    public static LoginResult Ok(User user) => new() { User = user };

    public static LoginResult Fail(string message) => new() { ErrorMessage = message };
}
