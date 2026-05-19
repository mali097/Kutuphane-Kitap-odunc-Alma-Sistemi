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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBorrowService, BorrowService>();
builder.Services.AddScoped<IWeeklyRecommendationService, WeeklyRecommendationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");
    try
    {
        var databaseName = db.Database.GetDbConnection().Database;
        logger.LogInformation("Applying EF migrations to database '{DatabaseName}'", databaseName);
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Database migration failed. API will continue but data operations may fail.");
    }
}

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

app.MapGet("/api/genres", () =>
    Results.Ok(GenreCatalog.AllNames.Select(name => new GenreOptionResponse(
        name,
        GenreCatalog.GetDisplayName(Enum.Parse<GenreType>(name, ignoreCase: true))))));

app.MapGet("/api/books/by-category", async (
    IBookService bookService,
    CancellationToken cancellationToken) =>
{
    var books = await bookService.GetAllBooksAsync(null, cancellationToken);
    var booksByGenre = books
        .Select(book => new { Book = book, Genre = GenreCatalog.NormalizeStoredGenre(book.Genre) })
        .Where(item => item.Genre is not null)
        .GroupBy(item => item.Genre!, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.Select(item => item.Book).ToList(), StringComparer.OrdinalIgnoreCase);

    var grouped = GenreCatalog.AllNames
        .Select(category => new BooksByCategoryResponse(
            category,
            GenreCatalog.GetDisplayName(Enum.Parse<GenreType>(category, ignoreCase: true)),
            (booksByGenre.TryGetValue(category, out var categoryBooks) ? categoryBooks : [])
                .OrderBy(book => book.Title)
                .ThenBy(book => book.Author)
                .Select(MapBookToResponse)
                .ToList()))
        .ToList();

    return Results.Ok(grouped);
});

app.MapGet("/api/books/{id:int}", async (int id, IBookService bookService, CancellationToken cancellationToken) =>
{
    var book = await bookService.GetBookByIdAsync(id, cancellationToken);
    return book is null
        ? Results.NotFound(new { Message = "Book not found." })
        : Results.Ok(MapBookToResponse(book));
});

// Public user capabilities:
// - list books
// - view book details
// - borrow books
// - self register (default Student)

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
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateChangePasswordRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var isChanged = await authService.ChangePasswordAsync(request, cancellationToken);
    return isChanged
        ? Results.Ok(new { Message = "Password changed." })
        : Results.BadRequest(new { Message = "Email or current password is invalid." });
});

app.MapPost("/books/{id}/upload-cover", async (int id, [FromForm] CoverUploadModel model) =>
{
    if (model.File == null || model.File.Length == 0)
    {
        return Results.BadRequest("Ltfen bir resim dosyas sein.");
    }

    // 1. Resmi kaydedeceimiz klasrn yolunu belirliyoruz (Projenin iinde wwwroot/uploads)
    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

    // Eer byle bir klasr yoksa, otomatik olutur
    if (!Directory.Exists(uploadsFolder))
    {
        Directory.CreateDirectory(uploadsFolder);
    }

    // 2. Dosyann adn belirliyoruz (rn: kitap-1.jpg)
    // Gerek projede uzanty (.jpg, .png) dinamik almak daha iyidir ama imdilik basitletiriyoruz.
    var filePath = Path.Combine(uploadsFolder, $"kitap-{id}.jpg");

    // 3. Dosyay klasre kopyalyoruz
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await model.File.CopyToAsync(stream);
    }

    return Results.Ok($"Resim baaryla kaydedildi! GET u noktasndan grntleyebilirsin.");
})
.WithName("UploadBookCover")
.DisableAntiforgery()
.WithOpenApi();
app.MapGet("/books/{id}/cover", (int id) =>
{
    // Kaydettiimiz resmin tam yolunu buluyoruz
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", $"kitap-{id}.jpg");

    // Eer bu kitaba ait resim yoksa hata dn
    if (!System.IO.File.Exists(filePath))
    {
        return Results.NotFound("Bu kitaba ait kapak resmi bulunamad.");
    }

    // TE SHRL SATIR: Dosyay bir "File" olarak ve tipini "image/jpeg" belirterek dndryoruz
    return Results.File(filePath, "image/jpeg");
})
.WithName("GetBookCover")
.WithOpenApi();

app.MapDelete("/books/{id}/cover", (int id) =>
{
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", $"kitap-{id}.jpg");

    if (!System.IO.File.Exists(filePath))
    {
        return Results.NotFound("Bu kitaba ait kapak resmi bulunamadi.");
    }

    System.IO.File.Delete(filePath);
    return Results.NoContent();
})
.WithName("DeleteBookCover")
.WithOpenApi();

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

app.MapPost("/api/admin/users", async (
    CreateUserRequest request,
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

    var validationErrors = ValidateCreateUserRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var roleErrors = ValidateUserRole(request.Role);
    if (roleErrors.Count != 0)
    {
        return Results.ValidationProblem(roleErrors);
    }

    var newUserId = await userService.AddAsync(request, authorization.AdminUserId, cancellationToken);
    var createdUser = await userService.GetByIdAsync(newUserId, cancellationToken);
    return Results.Created(
        $"/api/admin/users/{newUserId}",
        createdUser is null
            ? new { Message = "User created.", UserId = newUserId }
            : MapUserToResponse(createdUser));
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
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    GenreCatalog.TryParse(request.Genre, out var genre);

    var newBook = new Book
    {
        Title = request.Title,
        Author = request.Author,
        Genre = GenreCatalog.ToStorageName(genre),
        PublishYear = request.PublishYear,
        IsAvailable = request.IsAvailable
    };

    var newBookId = await bookService.AddBookAsync(newBook, authorization.AdminUserId, cancellationToken);
    var createdBook = await bookService.GetBookByIdAsync(newBookId, cancellationToken);
    return Results.Created(
        $"/api/books/{newBookId}",
        new
        {
            Message = "Book added by admin.",
            BookId = newBookId,
            Isbn = createdBook?.Isbn
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

static string? GetAdminSessionToken(string? adminTokenHeaderValue, HttpContext context)
{
    if (!string.IsNullOrWhiteSpace(adminTokenHeaderValue))
    {
        return adminTokenHeaderValue.Trim();
    }

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

    if (request.PublishYear < 0 || request.PublishYear > DateTime.UtcNow.Year + 1)
    {
        errors["publishYear"] = [$"PublishYear must be between 0 and {DateTime.UtcNow.Year + 1}."];
    }

    if (!GenreCatalog.TryParse(request.Genre, out _))
    {
        errors["genre"] =
        [
            "Genre must be one of the 22 allowed categories.",
            $"Allowed values: {string.Join(", ", GenreCatalog.AllNames)}"
        ];
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

    if (IsInvalidPatchValue(request.Genre))
    {
        errors["genre"] = ["Genre cannot be empty when provided."];
    }
    else if (request.Genre is not null && !GenreCatalog.TryParse(request.Genre, out _))
    {
        errors["genre"] =
        [
            "Genre must be one of the 22 allowed categories.",
            $"Allowed values: {string.Join(", ", GenreCatalog.AllNames)}"
        ];
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

    var roleErrors = ValidateUserRole(request.Role);
    foreach (var (key, messages) in roleErrors)
    {
        errors[key] = messages;
    }

    return errors;
}

static Dictionary<string, string[]> ValidateUserRole(string? role)
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(role))
    {
        return errors;
    }

    var allowedRoles = new[] { "student", "author", "admin" };
    if (!allowedRoles.Contains(role.Trim().ToLowerInvariant()))
    {
        errors["role"] = [$"Role must be one of: {string.Join(", ", allowedRoles)}."];
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

    if (string.IsNullOrWhiteSpace(request.Email))
    {
        errors["email"] = ["Email is required."];
    }
    else if (request.Email.Trim().Length > 150)
    {
        errors["email"] = ["Email cannot be longer than 150 characters."];
    }

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

internal sealed record BookResponse(
    int Id,
    string Title,
    string Author,
    string Isbn,
    string Genre,
    int PublishYear,
    bool IsAvailable
);

internal sealed record GenreOptionResponse(string Id, string DisplayName);

internal sealed record BooksByCategoryResponse(
    string Category,
    string DisplayName,
    IReadOnlyList<BookResponse> Books
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

internal sealed record AdminAuthorizationResult(bool IsAuthorized, int? AdminUserId, IResult? ErrorResult);
