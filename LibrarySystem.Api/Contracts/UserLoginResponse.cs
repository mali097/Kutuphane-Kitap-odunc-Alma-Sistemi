namespace LibrarySystem.Api.Contracts;

public sealed record UserLoginResponse(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string Token
);
