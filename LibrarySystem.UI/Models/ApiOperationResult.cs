namespace LibrarySystem.UI.Models;

public sealed class ApiOperationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static ApiOperationResult Ok() => new() { Success = true };

    public static ApiOperationResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
