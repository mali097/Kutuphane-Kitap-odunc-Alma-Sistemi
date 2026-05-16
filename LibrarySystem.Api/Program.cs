using LibrarySystem.Api.Contracts;
using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using LibrarySystem.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

const string ActorUserIdHeader = "X-Actor-User-Id";
const string AdminSetupKeyHeader = "X-Admin-Setup-Key";
const string AdminTokenHeader = "X-Admin-Token";
const string AuthorTokenHeader = "X-Author-Token";
const string UserTokenHeader = "X-User-Token";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBorrowService, BorrowService>();
builder.Services.AddScoped<IWeeklyRecommendationService, WeeklyRecommendationService>();
builder.Services.AddScoped<IBookRatingService, BookRatingService>();
builder.Services.AddScoped<IBookFavoriteService, BookFavoriteService>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

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
    IBookRatingService bookRatingService,
    CancellationToken cancellationToken) =>
{
    var books = await bookService.GetAllBooksAsync(query, cancellationToken);
    var summaries = await bookRatingService.GetBookRatingSummariesAsync(books.Select(item => item.Id), cancellationToken);
    return Results.Ok(books.Select(book => MapBookToResponse(book, GetBookRatingSummary(book.Id, summaries))));
});

app.MapGet("/api/books/{id:int}", async (
    int id,
    IBookService bookService,
    IBookRatingService bookRatingService,
    CancellationToken cancellationToken) =>
{
    var book = await bookService.GetBookByIdAsync(id, cancellationToken);
    return book is null
        ? Results.NotFound(new { Message = "Book not found." })
        : Results.Ok(MapBookToResponse(
            book,
            await bookRatingService.GetBookRatingSummaryAsync(id, cancellationToken)));
});

app.MapGet("/api/books/top-rated", async (
    int? limit,
    IBookRatingService bookRatingService,
    CancellationToken cancellationToken) =>
{
    var topBooks = await bookRatingService.GetTopRatedBooksAsync(limit ?? 50, cancellationToken);
    return Results.Ok(topBooks.Select(MapTopRatedBookToResponse));
});

app.MapPost("/api/books/{bookId:int}/ratings", async (
    int bookId,
    CreateBookRatingRequest request,
    [FromHeader(Name = UserTokenHeader)] string? userToken,
    [FromHeader(Name = AuthorTokenHeader)] string? authorToken,
    HttpContext httpContext,
    IAuthService authService,
    IUserService userService,
    IBookRatingService bookRatingService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeStudentOrAuthorAsync(userToken, authorToken, httpContext, authService, userService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var validationErrors = ValidateCreateBookRatingRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var ratingResult = await bookRatingService.RateBookAsync(
        authorization.AdminUserId ?? 0,
        bookId,
        request.Score,
        cancellationToken);

    return ratingResult.IsSuccess
        ? Results.Ok(new
        {
            Message = "Book rating saved.",
            BookId = bookId,
            MyRating = request.Score,
            ratingResult.AverageRating,
            ratingResult.RatingCount
        })
        : Results.BadRequest(new { Message = ratingResult.ErrorMessage ?? "Rating failed." });
});

// Public user capabilities:
// - list books
// - view book details
// - borrow books
// - self register (default Student)
// - rate books (Student or Author; X-User-Token, X-Author-Token, or Bearer session)
// - manage favorites (Student or Author; same tokens)

app.MapPost("/api/users/register", async (
    CreateUserRequest request,
    IUserService userService,
    CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateSelfRegisterUserRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var createRequest = new CreateUserRequest
    {
        FirstName = request.FirstName,
        LastName = request.LastName,
        Email = request.Email,
        PasswordHash = request.PasswordHash,
        Role = "Student"
    };

    var newUserId = await userService.AddAsync(createRequest, actorUserId: 0, cancellationToken);
    return Results.Created($"/api/users/{newUserId}", new { Message = "User registered successfully.", UserId = newUserId });
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

app.MapPost("/api/auth/change-password", async (
    ChangePasswordRequest request,
    [FromHeader(Name = UserTokenHeader)] string? userToken,
    HttpContext httpContext,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var sessionToken = GetUserSessionToken(userToken, httpContext);
    if (string.IsNullOrWhiteSpace(sessionToken))
    {
        return Results.Unauthorized();
    }

    var userId = await authService.GetUserIdBySessionTokenAsync(sessionToken, cancellationToken);
    if (!userId.HasValue)
    {
        return Results.Unauthorized();
    }

    var validationErrors = ValidateChangePasswordRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var isChanged = await authService.ChangePasswordAsync(userId.Value, request, cancellationToken);
    return isChanged
        ? Results.Ok(new { Message = "Password changed." })
        : Results.BadRequest(new { Message = "Current password is invalid." });
});

app.MapGet("/api/users/me/ratings", async (
    [FromHeader(Name = UserTokenHeader)] string? userToken,
    [FromHeader(Name = AuthorTokenHeader)] string? authorToken,
    HttpContext httpContext,
    IAuthService authService,
    IUserService userService,
    IBookRatingService bookRatingService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeStudentOrAuthorAsync(userToken, authorToken, httpContext, authService, userService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var ratings = await bookRatingService.GetUserRatedBooksAsync(authorization.AdminUserId ?? 0, cancellationToken);
    return Results.Ok(ratings.Select(MapUserRatedBookToResponse));
});

app.MapGet("/api/users/me/favorites", async (
    [FromHeader(Name = UserTokenHeader)] string? userToken,
    [FromHeader(Name = AuthorTokenHeader)] string? authorToken,
    HttpContext httpContext,
    IAuthService authService,
    IUserService userService,
    IBookFavoriteService bookFavoriteService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeStudentOrAuthorAsync(userToken, authorToken, httpContext, authService, userService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var favorites = await bookFavoriteService.GetFavoritesAsync(authorization.AdminUserId ?? 0, cancellationToken);
    return Results.Ok(favorites.Select(MapFavoriteBookToResponse));
});

app.MapPost("/api/users/me/favorites/{bookId:int}", async (
    int bookId,
    [FromHeader(Name = UserTokenHeader)] string? userToken,
    [FromHeader(Name = AuthorTokenHeader)] string? authorToken,
    HttpContext httpContext,
    IAuthService authService,
    IUserService userService,
    IBookFavoriteService bookFavoriteService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeStudentOrAuthorAsync(userToken, authorToken, httpContext, authService, userService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    if (bookId <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["bookId"] = ["BookId must be greater than 0."]
        });
    }

    var result = await bookFavoriteService.AddFavoriteAsync(authorization.AdminUserId ?? 0, bookId, cancellationToken);
    if (!result.IsSuccess)
    {
        return Results.BadRequest(new { Message = result.ErrorMessage ?? "Could not add favorite." });
    }

    return result.AlreadyInState
        ? Results.Ok(new { Message = "Book is already in favorites.", BookId = bookId })
        : Results.Created($"/api/users/me/favorites/{bookId}", new { Message = "Book added to favorites.", BookId = bookId });
});

app.MapDelete("/api/users/me/favorites/{bookId:int}", async (
    int bookId,
    [FromHeader(Name = UserTokenHeader)] string? userToken,
    [FromHeader(Name = AuthorTokenHeader)] string? authorToken,
    HttpContext httpContext,
    IAuthService authService,
    IUserService userService,
    IBookFavoriteService bookFavoriteService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeStudentOrAuthorAsync(userToken, authorToken, httpContext, authService, userService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    if (bookId <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["bookId"] = ["BookId must be greater than 0."]
        });
    }

    var result = await bookFavoriteService.RemoveFavoriteAsync(authorization.AdminUserId ?? 0, bookId, cancellationToken);
    if (!result.IsSuccess)
    {
        return result.AlreadyInState
            ? Results.NotFound(new { Message = "Favorite not found.", BookId = bookId })
            : Results.BadRequest(new { Message = result.ErrorMessage ?? "Could not remove favorite." });
    }

    return Results.Ok(new { Message = "Book removed from favorites.", BookId = bookId });
});

app.MapPost("/api/admin/bootstrap", async (
    AdminBootstrapRequest request,
    [FromHeader(Name = AdminSetupKeyHeader)] string? adminSetupKey,
    IConfiguration configuration,
    IUserService userService,
    CancellationToken cancellationToken) =>
{
    var configuredSetupKey = configuration["AdminBootstrapKey"];
    if (string.IsNullOrWhiteSpace(configuredSetupKey))
    {
        return Results.Problem("Admin bootstrap key is not configured.");
    }

    if (string.IsNullOrWhiteSpace(adminSetupKey)
        || !string.Equals(adminSetupKey.Trim(), configuredSetupKey, StringComparison.Ordinal))
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

app.MapPost("/api/author/recommendations", async (
    CreateWeeklyRecommendationRequest request,
    [FromHeader(Name = AuthorTokenHeader)] string? authorToken,
    HttpContext httpContext,
    IAuthService authService,
    IWeeklyRecommendationService recommendationService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeAuthorAsync(authorToken, httpContext, authService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var validationErrors = ValidateCreateWeeklyRecommendationRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var recommendation = await recommendationService.AddRecommendationAsync(
        authorization.AdminUserId ?? 0,
        request,
        cancellationToken);

    return Results.Created($"/api/recommendations/weekly/{recommendation.RecommendationId}", recommendation);
});

app.MapGet("/api/recommendations/weekly", async (
    IWeeklyRecommendationService recommendationService,
    CancellationToken cancellationToken) =>
{
    var recommendations = await recommendationService.GetCurrentWeekRecommendationsAsync(cancellationToken);
    return Results.Ok(recommendations);
});

app.MapGet("/api/admin/users", async (
    [FromHeader(Name = AdminTokenHeader)] string? adminToken,
    HttpContext httpContext,
    IAuthService authService,
    IUserService userService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeAdminAsync(adminToken, httpContext, authService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var users = await userService.GetAllAsync(cancellationToken);
    return Results.Ok(users.Select(MapUserToResponse));
});

app.MapPost("/api/admin/books", async (
    CreateBookRequest request,
    [FromHeader(Name = AdminTokenHeader)] string? adminToken,
    HttpContext httpContext,
    IAuthService authService,
    IBookService bookService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeAdminAsync(adminToken, httpContext, authService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var validationErrors = ValidateCreateBookRequest(request);
    var (genreErrors, parsedGenres) = GenreTypeListConverter.ValidateAndParseNames(request.Genres, required: true);
    foreach (var (key, messages) in genreErrors)
    {
        validationErrors[key] = messages;
    }

    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var newBook = new Book
    {
        Title = request.Title,
        Author = request.Author,
        Genres = parsedGenres,
        PublishYear = request.PublishYear,
        IsAvailable = request.IsAvailable
    };

    var newBookId = await bookService.AddBookAsync(newBook, authorization.AdminUserId, cancellationToken);
    return Results.Created($"/api/books/{newBookId}", new
    {
        Message = "Book added by admin.",
        BookId = newBookId,
        Isbn = newBook.Isbn
    });
});

app.MapPut("/api/admin/books/{id:int}", async (
    int id,
    UpdateBookRequest request,
    [FromHeader(Name = AdminTokenHeader)] string? adminToken,
    HttpContext httpContext,
    IAuthService authService,
    IBookService bookService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeAdminAsync(adminToken, httpContext, authService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var validationErrors = ValidateUpdateBookRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var isUpdated = await bookService.UpdateBookAsync(id, request, authorization.AdminUserId, cancellationToken);
    return isUpdated
        ? Results.Ok(new { Message = "Book updated by admin." })
        : Results.NotFound(new { Message = "Book not found." });
});

app.MapDelete("/api/admin/books/{id:int}", async (
    int id,
    [FromHeader(Name = AdminTokenHeader)] string? adminToken,
    HttpContext httpContext,
    IAuthService authService,
    IBookService bookService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeAdminAsync(adminToken, httpContext, authService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var isDeleted = await bookService.DeleteBookAsync(id, authorization.AdminUserId, cancellationToken);
    return isDeleted
        ? Results.Ok(new { Message = "Book deleted by admin." })
        : Results.NotFound(new { Message = "Book not found." });
});

app.MapGet("/api/admin/borrow-records", async (
    [FromHeader(Name = AdminTokenHeader)] string? adminToken,
    HttpContext httpContext,
    IAuthService authService,
    IBorrowService borrowService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeAdminAsync(adminToken, httpContext, authService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var records = await borrowService.GetAllBorrowRecordsWithDetailsAsync(cancellationToken);
    return Results.Ok(records.Select(MapAdminBorrowRecordToResponse));
});

app.MapGet("/api/admin/borrow-records/active", async (
    [FromHeader(Name = AdminTokenHeader)] string? adminToken,
    HttpContext httpContext,
    IAuthService authService,
    IBorrowService borrowService,
    CancellationToken cancellationToken) =>
{
    var authorization = await AuthorizeAdminAsync(adminToken, httpContext, authService, cancellationToken);
    if (!authorization.IsAuthorized)
    {
        return authorization.ErrorResult!;
    }

    var records = await borrowService.GetActiveBorrowRecordsWithDetailsAsync(cancellationToken);
    return Results.Ok(records.Select(MapAdminBorrowRecordToResponse));
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
    string? adminTokenHeaderValue,
    HttpContext context,
    IAuthService authService,
    CancellationToken cancellationToken)
{
    var sessionToken = GetAdminSessionToken(adminTokenHeaderValue, context);
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

static async Task<AdminAuthorizationResult> AuthorizeAuthorAsync(
    string? authorTokenHeaderValue,
    HttpContext context,
    IAuthService authService,
    CancellationToken cancellationToken)
{
    var sessionToken = GetAdminSessionToken(authorTokenHeaderValue, context);
    if (string.IsNullOrWhiteSpace(sessionToken))
    {
        return new AdminAuthorizationResult(false, null, Results.Unauthorized());
    }

    var userId = await authService.GetUserIdBySessionTokenAsync(sessionToken, cancellationToken);
    if (!userId.HasValue)
    {
        return new AdminAuthorizationResult(false, null, Results.Unauthorized());
    }

    var isAuthor = await authService.IsUserAuthorAsync(userId.Value, cancellationToken);
    if (!isAuthor)
    {
        return new AdminAuthorizationResult(false, null, Results.Forbid());
    }

    return new AdminAuthorizationResult(true, userId.Value, null);
}

static async Task<AdminAuthorizationResult> AuthorizeStudentOrAuthorAsync(
    string? userTokenHeaderValue,
    string? authorTokenHeaderValue,
    HttpContext context,
    IAuthService authService,
    IUserService userService,
    CancellationToken cancellationToken)
{
    var sessionToken = GetStudentOrAuthorSessionToken(userTokenHeaderValue, authorTokenHeaderValue, context);
    if (string.IsNullOrWhiteSpace(sessionToken))
    {
        return new AdminAuthorizationResult(false, null, Results.Unauthorized());
    }

    var userId = await authService.GetUserIdBySessionTokenAsync(sessionToken, cancellationToken);
    if (!userId.HasValue)
    {
        return new AdminAuthorizationResult(false, null, Results.Unauthorized());
    }

    var user = await userService.GetByIdAsync(userId.Value, cancellationToken);
    if (user is null)
    {
        return new AdminAuthorizationResult(false, null, Results.Unauthorized());
    }

    var isAllowedRole = string.Equals(user.Role, "Student", StringComparison.OrdinalIgnoreCase)
        || string.Equals(user.Role, "Author", StringComparison.OrdinalIgnoreCase);
    if (!isAllowedRole)
    {
        return new AdminAuthorizationResult(false, null, Results.Forbid());
    }

    return new AdminAuthorizationResult(true, userId.Value, null);
}

static string? GetAdminSessionToken(string? adminTokenHeaderValue, HttpContext context)
{
    return GetSessionToken(adminTokenHeaderValue, AdminTokenHeader, context);
}

static string? GetUserSessionToken(string? userTokenHeaderValue, HttpContext context)
{
    return GetSessionToken(userTokenHeaderValue, UserTokenHeader, context);
}

/// <summary>Student sessions typically use X-User-Token; authors may use X-Author-Token for the same login token.</summary>
static string? GetStudentOrAuthorSessionToken(string? userTokenHeaderValue, string? authorTokenHeaderValue, HttpContext context)
{
    var fromUser = GetSessionToken(userTokenHeaderValue, UserTokenHeader, context);
    if (!string.IsNullOrWhiteSpace(fromUser))
    {
        return fromUser;
    }

    return GetSessionToken(authorTokenHeaderValue, AuthorTokenHeader, context);
}

static string? GetSessionToken(string? tokenHeaderValue, string headerName, HttpContext context)
{
    if (!string.IsNullOrWhiteSpace(tokenHeaderValue))
    {
        return tokenHeaderValue.Trim();
    }

    if (context.Request.Headers.TryGetValue(headerName, out var values))
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

    if (request.PublishYear.HasValue && (request.PublishYear.Value < 0 || request.PublishYear.Value > DateTime.UtcNow.Year + 1))
    {
        errors["publishYear"] = [$"PublishYear must be between 0 and {DateTime.UtcNow.Year + 1}."];
    }

    if (request.Genres is not null)
    {
        var (genreErrors, _) = GenreTypeListConverter.ValidateAndParseNames(request.Genres, required: true);
        foreach (var (key, messages) in genreErrors)
        {
            errors[key] = messages;
        }
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

static Dictionary<string, string[]> ValidateSelfRegisterUserRequest(CreateUserRequest request)
{
    return ValidateCreateUserRequest(request);
}

static Dictionary<string, string[]> ValidateCreateWeeklyRecommendationRequest(CreateWeeklyRecommendationRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.BookTitle))
    {
        errors["bookTitle"] = ["BookTitle is required."];
    }
    else if (request.BookTitle.Trim().Length > 200)
    {
        errors["bookTitle"] = ["BookTitle cannot be longer than 200 characters."];
    }

    if (string.IsNullOrWhiteSpace(request.Idea))
    {
        errors["idea"] = ["Idea is required."];
    }
    else if (request.Idea.Trim().Length > 1000)
    {
        errors["idea"] = ["Idea cannot be longer than 1000 characters."];
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

static Dictionary<string, string[]> ValidateCreateBookRatingRequest(CreateBookRatingRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (request.Score < 0.5m || request.Score > 5m)
    {
        errors["score"] = ["Score must be between 0.5 and 5."];
        return errors;
    }

    var halfStep = request.Score * 2m;
    if (halfStep != decimal.Truncate(halfStep))
    {
        errors["score"] = ["Score must be in 0.5 increments (e.g. 3.5, 4.0, 4.5)."];
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

static BookRatingSummary GetBookRatingSummary(int bookId, IReadOnlyDictionary<int, BookRatingSummary> summaries)
{
    return summaries.TryGetValue(bookId, out var summary)
        ? summary
        : new BookRatingSummary(null, 0);
}

static BookResponse MapBookToResponse(Book book, BookRatingSummary ratingSummary)
{
    return new BookResponse(
        book.Id,
        book.Title,
        book.Author,
        book.Isbn,
        GenreTypeListConverter.ToGenreNames(book.Genres),
        book.PublishYear,
        book.IsAvailable,
        ratingSummary.AverageRating,
        ratingSummary.RatingCount
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

static AdminBorrowRecordResponse MapAdminBorrowRecordToResponse(BorrowRecord record)
{
    return new AdminBorrowRecordResponse(
        record.Id,
        record.UserId,
        record.User?.FirstName ?? string.Empty,
        record.User?.LastName ?? string.Empty,
        record.User?.Email ?? string.Empty,
        record.BookId,
        record.Book?.Title ?? string.Empty,
        record.BorrowDate,
        record.ExpectedReturnDate,
        record.ActualReturnDate,
        record.IsReturned
    );
}

static UserRatedBookResponse MapUserRatedBookToResponse(UserRatedBookItem item)
{
    return new UserRatedBookResponse(
        item.BookId,
        item.Title,
        item.Author,
        item.Genres,
        item.MyRating,
        item.AverageRating,
        item.RatingCount,
        item.RatedAt);
}

static TopRatedBookResponse MapTopRatedBookToResponse(TopRatedBookItem item)
{
    return new TopRatedBookResponse(
        item.BookId,
        item.Title,
        item.Author,
        item.Genres,
        item.PublishYear,
        item.AverageRating,
        item.RatingCount);
}

static FavoriteBookResponse MapFavoriteBookToResponse(FavoriteBookItem item)
{
    return new FavoriteBookResponse(
        item.BookId,
        item.Title,
        item.Author,
        item.Isbn,
        item.Genres,
        item.PublishYear,
        item.IsAvailable,
        item.FavoritedAt);
}

internal sealed record BookResponse(
    int Id,
    string Title,
    string Author,
    string Isbn,
    IReadOnlyList<string> Genres,
    int PublishYear,
    bool IsAvailable,
    decimal? AverageRating,
    int RatingCount
);

internal sealed record UserResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Role
);

internal sealed record AdminBorrowRecordResponse(
    int BorrowRecordId,
    int UserId,
    string UserFirstName,
    string UserLastName,
    string UserEmail,
    int BookId,
    string BookTitle,
    DateTime BorrowDate,
    DateTime ExpectedReturnDate,
    DateTime? ActualReturnDate,
    bool IsReturned
);

internal sealed record UserRatedBookResponse(
    int BookId,
    string Title,
    string Author,
    IReadOnlyList<string> Genres,
    decimal MyRating,
    decimal? AverageRating,
    int RatingCount,
    DateTime RatedAt
);

internal sealed record TopRatedBookResponse(
    int BookId,
    string Title,
    string Author,
    IReadOnlyList<string> Genres,
    int PublishYear,
    decimal AverageRating,
    int RatingCount
);

internal sealed record FavoriteBookResponse(
    int BookId,
    string Title,
    string Author,
    string Isbn,
    IReadOnlyList<string> Genres,
    int PublishYear,
    bool IsAvailable,
    DateTime FavoritedAt
);

internal sealed record AdminAuthorizationResult(bool IsAuthorized, int? AdminUserId, IResult? ErrorResult);
