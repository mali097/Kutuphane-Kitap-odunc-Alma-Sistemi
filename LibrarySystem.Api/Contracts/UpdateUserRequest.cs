namespace LibrarySystem.Api.Contracts;

public sealed class UpdateUserRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PasswordHash { get; init; }
    public string? Role { get; init; }
}
