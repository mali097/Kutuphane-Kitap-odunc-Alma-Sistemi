namespace LibrarySystem.Api.Contracts;

public sealed class UpdateUserRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PasswordHash { get; init; }
    public string Role { get; init; } = "Student";
}
