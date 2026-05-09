using LibrarySystem.Api.Contracts;
using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using LibrarySystem.Api.Services;
using Microsoft.EntityFrameworkCore;

const string ActorUserIdHeader = "X-Actor-User-Id";
const string AdminSetupKeyHeader = "X-Admin-Setup-Key";
const string AdminTokenHeader = "X-Admin-Token";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBorrowService, BorrowService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/books", async (
    [AsParameters] GetBooksQuery query,
    IBookService bookService,
    CancellationToken cancellationToken) =>
{
    var books = await bookService.GetAllBooksAsync(query, cancellationToken);
    return Results.Ok(books.Select(MapBookToResponse));
});

app.MapGet("/api/books/{id:int}", async (int id, IBookService bookService, CancellationToken cancellationToken) =>
{
    var book = await bookService.GetBookByIdAsync(id, cancellationToken);
    return book is null
        ? Results.NotFound(new { Message = "Book not found." })
        : Results.Ok(MapBookToResponse(book));
});

app.MapPost("/api/books", async (
    CreateBookRequest request,
    HttpContext httpContext,
    IBookService bookService,
    CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateCreateBookRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var actorUserId = GetActorUserId(httpContext);
    var newBook = new Book
    {
        Title = request.Title,
        Author = request.Author,
        Isbn = request.Isbn,
        Genre = request.Genre,
        PublishYear = request.PublishYear,
        IsAvailable = request.IsAvailable
    };

    var newId = await bookService.AddBookAsync(newBook, actorUserId, cancellationToken);
    return Results.Created($"/api/books/{newId}", new { Message = "Book added successfully.", BookId = newId });
});

app.MapPut("/api/books/{id:int}", async (
    int id,
    UpdateBookRequest request,
    HttpContext httpContext,
    IBookService bookService,
    CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateUpdateBookRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var actorUserId = GetActorUserId(httpContext);
    var isUpdated = await bookService.UpdateBookAsync(id, request, actorUserId, cancellationToken);
    return isUpdated
        ? Results.Ok(new { Message = "Book updated." })
        : Results.NotFound(new { Message = "Book not found." });
});

app.MapDelete("/api/books/{id:int}", async (
    int id,
    HttpContext httpContext,
    IBookService bookService,
    CancellationToken cancellationToken) =>
{
    var actorUserId = GetActorUserId(httpContext);
    var isDeleted = await bookService.DeleteBookAsync(id, actorUserId, cancellationToken);
    return isDeleted
        ? Results.Ok(new { Message = "Book deleted." })
        : Results.NotFound(new { Message = "Book not found." });
});

app.MapGet("/api/users", async (IUserService userService, CancellationToken cancellationToken) =>
{
    var users = await userService.GetAllAsync(cancellationToken);
    return Results.Ok(users.Select(MapUserToResponse));
});

app.MapGet("/api/users/{id:int}", async (int id, IUserService userService, CancellationToken cancellationToken) =>
{
    var user = await userService.GetByIdAsync(id, cancellationToken);
    return user is null
        ? Results.NotFound(new { Message = "User not found." })
        : Results.Ok(MapUserToResponse(user));
});

app.MapPost("/api/users", async (
    CreateUserRequest request,
    HttpContext httpContext,
    IUserService userService,
    CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateCreateUserRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var actorUserId = GetActorUserId(httpContext);
    var newUserId = await userService.AddAsync(request, actorUserId, cancellationToken);
    return Results.Created($"/api/users/{newUserId}", new { Message = "User added successfully.", UserId = newUserId });
});

app.MapPut("/api/users/{id:int}", async (
    int id,
    UpdateUserRequest request,
    HttpContext httpContext,
    IUserService userService,
    CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateUpdateUserRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var actorUserId = GetActorUserId(httpContext);
    var isUpdated = await userService.UpdateAsync(id, request, actorUserId, cancellationToken);
    return isUpdated
        ? Results.Ok(new { Message = "User updated." })
        : Results.NotFound(new { Message = "User not found." });
});

app.MapDelete("/api/users/{id:int}", async (
    int id,
    HttpContext httpContext,
    IUserService userService,
    CancellationToken cancellationToken) =>
{
    var actorUserId = GetActorUserId(httpContext);
    var isDeleted = await userService.DeleteAsync(id, actorUserId, cancellationToken);
    return isDeleted
        ? Results.Ok(new { Message = "User deleted." })
        : Results.NotFound(new { Message = "User not found." });
});

app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateLoginRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var response = await authService.LoginAsync(request, cancellationToken);
    return response is null
        ? Results.BadRequest(new { Message = "Invalid email or password." })
        : Results.Ok(response);
});

app.MapPost("/api/auth/logout", async (UserLogoutRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateLogoutRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var isLoggedOut = await authService.LogoutAsync(request, cancellationToken);
    return isLoggedOut
        ? Results.Ok(new { Message = "Logged out." })
        : Results.BadRequest(new { Message = "Session token is invalid." });
});

app.MapPost("/api/users/{id:int}/change-password", async (
    int id,
    ChangePasswordRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateChangePasswordRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var isChanged = await authService.ChangePasswordAsync(id, request, cancellationToken);
    return isChanged
        ? Results.Ok(new { Message = "Password changed." })
        : Results.BadRequest(new { Message = "Current password is invalid or user not found." });
});

app.MapPost("/api/admin/bootstrap", async (
    AdminBootstrapRequest request,
    HttpContext httpContext,
    IConfiguration configuration,
    IUserService userService,
    CancellationToken cancellationToken) =>
{
    var configuredSetupKey = configuration["AdminBootstrapKey"];
    if (string.IsNullOrWhiteSpace(configuredSetupKey))
    {
        return Results.Problem("Admin bootstrap key is not configured.");
    }

    if (!httpContext.Request.Headers.TryGetValue(AdminSetupKeyHeader, out var setupKeyValues)
        || !string.Equals(setupKeyValues.ToString(), configuredSetupKey, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    if (await userService.AdminExistsAsync(cancellationToken))
    {
        return Results.Conflict(new { Message = "An admin user already exists." });
    }

    var validationErrors = ValidateAdminBootstrapRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var createAdminRequest = new CreateUserRequest
    {
        FirstName = request.FirstName,
        LastName = request.LastName,
        Email = request.Email,
        PasswordHash = request.Password,
        Role = "Admin"
    };

    var adminUserId = await userService.AddAsync(createAdminRequest, actorUserId: 0, cancellationToken);
    return Results.Created($"/api/users/{adminUserId}", new { Message = "Admin created.", UserId = adminUserId });
});

app.MapPost("/api/admin/login", async (LoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateLoginRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var response = await authService.LoginAdminAsync(request, cancellationToken);
    return response is null
        ? Results.BadRequest(new { Message = "Invalid admin credentials." })
        : Results.Ok(response);
});

app.MapGet("/api/admin/users", async (
    HttpContext httpContext,
    IAuthService authService,
    IUserService userService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeAdminAsync(httpContext, authService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var users = await userService.GetAllAsync(cancellationToken);
    return Results.Ok(users.Select(MapUserToResponse));
});

app.MapPost("/api/admin/users", async (
    CreateUserRequest request,
    HttpContext httpContext,
    IAuthService authService,
    IUserService userService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeAdminAsync(httpContext, authService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var validationErrors = ValidateCreateUserRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var newUserId = await userService.AddAsync(request, authorization.AdminUserId, cancellationToken);
    return Results.Created($"/api/users/{newUserId}", new { Message = "User added by admin.", UserId = newUserId });
});

app.MapPost("/api/admin/books", async (
    CreateBookRequest request,
    HttpContext httpContext,
    IAuthService authService,
    IBookService bookService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeAdminAsync(httpContext, authService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var validationErrors = ValidateCreateBookRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var newBook = new Book
    {
        Title = request.Title,
        Author = request.Author,
        Isbn = request.Isbn,
        Genre = request.Genre,
        PublishYear = request.PublishYear,
        IsAvailable = request.IsAvailable
    };

    var newBookId = await bookService.AddBookAsync(newBook, authorization.AdminUserId, cancellationToken);
    return Results.Created($"/api/books/{newBookId}", new { Message = "Book added by admin.", BookId = newBookId });
});

app.MapGet("/api/borrow-records", async (
    int? userId,
    int? bookId,
    bool? isReturned,
    IBorrowService borrowService,
    CancellationToken cancellationToken) =>
{
    var records = await borrowService.GetBorrowRecordsAsync(userId, bookId, isReturned, cancellationToken);
    return Results.Ok(records.Select(MapBorrowRecordToResponse));
});

app.MapPost("/api/borrow-records/borrow", async (
    BorrowBookRequest request,
    HttpContext httpContext,
    IBorrowService borrowService,
    CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateBorrowBookRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var actorUserId = GetActorUserId(httpContext);
    var recordId = await borrowService.BorrowBookAsync(request, actorUserId, cancellationToken);
    return recordId.HasValue
        ? Results.Created($"/api/borrow-records/{recordId.Value}", new { Message = "Book borrowed.", BorrowRecordId = recordId.Value })
        : Results.BadRequest(new { Message = "Borrow action failed. User/book may be invalid or book unavailable." });
});

app.MapPost("/api/borrow-records/return", async (
    ReturnBookRequest request,
    HttpContext httpContext,
    IBorrowService borrowService,
    CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateReturnBookRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var actorUserId = GetActorUserId(httpContext);
    var isReturned = await borrowService.ReturnBookAsync(request, actorUserId, cancellationToken);
    return isReturned
        ? Results.Ok(new { Message = "Book returned." })
        : Results.BadRequest(new { Message = "Return action failed. Record not found or already returned." });
});

app.Run();

static int GetActorUserId(HttpContext context)
{
    if (context.Request.Headers.TryGetValue(ActorUserIdHeader, out var values)
        && int.TryParse(values.ToString(), out var parsedId)
        && parsedId > 0)
    {
        return parsedId;
    }

    return 1;
}

static async Task<AdminAuthorizationResult> AuthorizeAdminAsync(
    HttpContext context,
    IAuthService authService,
    CancellationToken cancellationToken)
{
    var sessionToken = GetAdminSessionToken(context);
    if (string.IsNullOrWhiteSpace(sessionToken))
    {
        return new AdminAuthorizationResult(false, null, Results.Unauthorized());
    }

    var userId = await authService.GetUserIdBySessionTokenAsync(sessionToken, cancellationToken);
    if (!userId.HasValue)
    {
        return new AdminAuthorizationResult(false, null, Results.Unauthorized());
    }

    var isAdmin = await authService.IsUserAdminAsync(userId.Value, cancellationToken);
    if (!isAdmin)
    {
        return new AdminAuthorizationResult(false, null, Results.Forbid());
    }

    return new AdminAuthorizationResult(true, userId.Value, null);
}

static string? GetAdminSessionToken(HttpContext context)
{
    if (context.Request.Headers.TryGetValue(AdminTokenHeader, out var values))
    {
        var headerValue = values.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue;
        }
    }

    if (context.Request.Headers.TryGetValue("Authorization", out var authValues))
    {
        var authorization = authValues.ToString().Trim();
        const string bearerPrefix = "Bearer ";
        if (authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[bearerPrefix.Length..].Trim();
        }
    }

    return null;
}

static Dictionary<string, string[]> ValidateCreateBookRequest(CreateBookRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        errors["title"] = ["Title is required."];
    }
    else if (request.Title.Trim().Length > 200)
    {
        errors["title"] = ["Title cannot be longer than 200 characters."];
    }

    if (string.IsNullOrWhiteSpace(request.Author))
    {
        errors["author"] = ["Author is required."];
    }
    else if (request.Author.Trim().Length > 120)
    {
        errors["author"] = ["Author cannot be longer than 120 characters."];
    }

    if (string.IsNullOrWhiteSpace(request.Isbn))
    {
        errors["isbn"] = ["Isbn is required."];
    }
    else if (request.Isbn.Trim().Length > 30)
    {
        errors["isbn"] = ["Isbn cannot be longer than 30 characters."];
    }

    if (request.PublishYear < 0 || request.PublishYear > DateTime.UtcNow.Year + 1)
    {
        errors["publishYear"] = [$"PublishYear must be between 0 and {DateTime.UtcNow.Year + 1}."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateUpdateBookRequest(UpdateBookRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (IsInvalidPatchValue(request.Title))
    {
        errors["title"] = ["Title cannot be empty when provided."];
    }
    else if (request.Title is not null && request.Title.Trim().Length > 200)
    {
        errors["title"] = ["Title cannot be longer than 200 characters."];
    }

    if (IsInvalidPatchValue(request.Author))
    {
        errors["author"] = ["Author cannot be empty when provided."];
    }
    else if (request.Author is not null && request.Author.Trim().Length > 120)
    {
        errors["author"] = ["Author cannot be longer than 120 characters."];
    }

    if (IsInvalidPatchValue(request.Isbn))
    {
        errors["isbn"] = ["Isbn cannot be empty when provided."];
    }
    else if (request.Isbn is not null && request.Isbn.Trim().Length > 30)
    {
        errors["isbn"] = ["Isbn cannot be longer than 30 characters."];
    }

    if (request.PublishYear.HasValue && (request.PublishYear.Value < 0 || request.PublishYear.Value > DateTime.UtcNow.Year + 1))
    {
        errors["publishYear"] = [$"PublishYear must be between 0 and {DateTime.UtcNow.Year + 1}."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateUpdateUserRequest(UpdateUserRequest request)
{
    var errors = new Dictionary<string, string[]>();
    if (IsInvalidPatchValue(request.FirstName))
    {
        errors["firstName"] = ["FirstName cannot be empty when provided."];
    }
    else if (request.FirstName is not null && request.FirstName.Trim().Length > 80)
    {
        errors["firstName"] = ["FirstName cannot be longer than 80 characters."];
    }

    if (IsInvalidPatchValue(request.LastName))
    {
        errors["lastName"] = ["LastName cannot be empty when provided."];
    }
    else if (request.LastName is not null && request.LastName.Trim().Length > 80)
    {
        errors["lastName"] = ["LastName cannot be longer than 80 characters."];
    }

    if (IsInvalidPatchValue(request.Email))
    {
        errors["email"] = ["Email cannot be empty when provided."];
    }
    else if (request.Email is not null && request.Email.Trim().Length > 150)
    {
        errors["email"] = ["Email cannot be longer than 150 characters."];
    }

    if (IsInvalidPatchValue(request.Role))
    {
        errors["role"] = ["Role cannot be empty when provided."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateCreateUserRequest(CreateUserRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.FirstName))
    {
        errors["firstName"] = ["FirstName is required."];
    }
    else if (request.FirstName.Trim().Length > 80)
    {
        errors["firstName"] = ["FirstName cannot be longer than 80 characters."];
    }

    if (string.IsNullOrWhiteSpace(request.LastName))
    {
        errors["lastName"] = ["LastName is required."];
    }
    else if (request.LastName.Trim().Length > 80)
    {
        errors["lastName"] = ["LastName cannot be longer than 80 characters."];
    }

    if (string.IsNullOrWhiteSpace(request.Email))
    {
        errors["email"] = ["Email is required."];
    }
    else if (request.Email.Trim().Length > 150)
    {
        errors["email"] = ["Email cannot be longer than 150 characters."];
    }

    if (string.IsNullOrWhiteSpace(request.PasswordHash))
    {
        errors["passwordHash"] = ["PasswordHash is required."];
    }
    else if (request.PasswordHash.Trim().Length < 6)
    {
        errors["passwordHash"] = ["Password must be at least 6 characters."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateAdminBootstrapRequest(AdminBootstrapRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.FirstName))
    {
        errors["firstName"] = ["FirstName is required."];
    }

    if (string.IsNullOrWhiteSpace(request.LastName))
    {
        errors["lastName"] = ["LastName is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Email))
    {
        errors["email"] = ["Email is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Password))
    {
        errors["password"] = ["Password is required."];
    }
    else if (request.Password.Trim().Length < 6)
    {
        errors["password"] = ["Password must be at least 6 characters."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateLoginRequest(LoginRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Email))
    {
        errors["email"] = ["Email is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Password))
    {
        errors["password"] = ["Password is required."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateLogoutRequest(UserLogoutRequest request)
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.SessionToken))
    {
        errors["sessionToken"] = ["SessionToken is required."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateChangePasswordRequest(ChangePasswordRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.CurrentPassword))
    {
        errors["currentPassword"] = ["CurrentPassword is required."];
    }

    if (string.IsNullOrWhiteSpace(request.NewPassword))
    {
        errors["newPassword"] = ["NewPassword is required."];
    }
    else if (request.NewPassword.Trim().Length < 6)
    {
        errors["newPassword"] = ["NewPassword must be at least 6 characters."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateBorrowBookRequest(BorrowBookRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (request.UserId <= 0)
    {
        errors["userId"] = ["UserId must be greater than 0."];
    }

    if (request.BookId <= 0)
    {
        errors["bookId"] = ["BookId must be greater than 0."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateReturnBookRequest(ReturnBookRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (request.BorrowRecordId <= 0)
    {
        errors["borrowRecordId"] = ["BorrowRecordId must be greater than 0."];
    }

    return errors;
}

static bool IsInvalidPatchValue(string? value)
{
    if (value is null)
    {
        return false;
    }

    return value.Trim().Length == 0;
}

static BookResponse MapBookToResponse(Book book)
{
    return new BookResponse(
        book.Id,
        book.Title,
        book.Author,
        book.Isbn,
        book.Genre,
        book.PublishYear,
        book.IsAvailable
    );
}

static UserResponse MapUserToResponse(User user)
{
    return new UserResponse(
        user.Id,
        user.FirstName,
        user.LastName,
        user.Email,
        user.Role
    );
}

static BorrowRecordResponse MapBorrowRecordToResponse(BorrowRecord record)
{
    return new BorrowRecordResponse(
        record.Id,
        record.UserId,
        record.BookId,
        record.BorrowDate,
        record.ExpectedReturnDate,
        record.ActualReturnDate,
        record.IsReturned
    );
}

internal sealed record BookResponse(
    int Id,
    string Title,
    string Author,
    string Isbn,
    string Genre,
    int PublishYear,
    bool IsAvailable
);

internal sealed record UserResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Role
);

internal sealed record BorrowRecordResponse(
    int Id,
    int UserId,
    int BookId,
    DateTime BorrowDate,
    DateTime ExpectedReturnDate,
    DateTime? ActualReturnDate,
    bool IsReturned
);

internal sealed record AdminAuthorizationResult(bool IsAuthorized, int? AdminUserId, IResult? ErrorResult);
