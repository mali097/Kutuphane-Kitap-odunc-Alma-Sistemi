namespace LibrarySystem.Api.Contracts;

public sealed class UserLogoutRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
