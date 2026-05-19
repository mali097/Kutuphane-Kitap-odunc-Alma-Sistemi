namespace LibrarySystem.Api.Contracts;

public sealed class ChangePasswordRequest
{
    public string Email { get; init; } = string.Empty;
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
