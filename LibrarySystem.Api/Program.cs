using LibrarySystem.Api.Data;
using LibrarySystem.Api.Contracts;
using LibrarySystem.Api.Entities;
using LibrarySystem.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/books", async (IBookService bookService, CancellationToken cancellationToken) =>
{
    var books = await bookService.GetAllBooksAsync(cancellationToken);
    return Results.Ok(books.Select(MapToResponse));
});

app.MapGet("/api/books/{id:int}", async (int id, IBookService bookService, CancellationToken cancellationToken) =>
{
    var book = await bookService.GetBookByIdAsync(id, cancellationToken);
    return book is null
        ? Results.NotFound(new { Message = "Book not found." })
        : Results.Ok(MapToResponse(book));
});

app.MapPost("/api/books", async (CreateBookRequest request, IBookService bookService, CancellationToken cancellationToken) =>
{
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

    var newId = await bookService.AddBookAsync(newBook, cancellationToken);
    return Results.Created($"/api/books/{newId}", new { Message = "Book added successfully.", BookId = newId });
});

app.MapDelete("/api/books/{id:int}", async (int id, IBookService bookService, CancellationToken cancellationToken) =>
{
    var isDeleted = await bookService.DeleteBookAsync(id, cancellationToken);
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

app.MapPost("/api/users", async (CreateUserRequest request, IUserService userService, CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateCreateUserRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var newUserId = await userService.AddAsync(request, cancellationToken);
    return Results.Created($"/api/users/{newUserId}", new { Message = "User added successfully.", UserId = newUserId });
});

app.MapPut("/api/users/{id:int}", async (int id, UpdateUserRequest request, IUserService userService, CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateUpdateUserRequest(request);
    if (validationErrors.Count != 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var isUpdated = await userService.UpdateAsync(id, request, cancellationToken);
    return isUpdated
        ? Results.Ok(new { Message = "User updated." })
        : Results.NotFound(new { Message = "User not found." });
});

app.MapDelete("/api/users/{id:int}", async (int id, IUserService userService, CancellationToken cancellationToken) =>
{
    var isDeleted = await userService.DeleteAsync(id, cancellationToken);
    return isDeleted
        ? Results.Ok(new { Message = "User deleted." })
        : Results.NotFound(new { Message = "User not found." });
});

app.Run();

static Dictionary<string, string[]> ValidateCreateBookRequest(CreateBookRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        errors["title"] = ["Title is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Author))
    {
        errors["author"] = ["Author is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Isbn))
    {
        errors["isbn"] = ["Isbn is required."];
    }

    if (request.PublishYear < 0 || request.PublishYear > DateTime.UtcNow.Year + 1)
    {
        errors["publishYear"] = [$"PublishYear must be between 0 and {DateTime.UtcNow.Year + 1}."];
    }

    return errors;
}

static BookResponse MapToResponse(Book book)
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

static Dictionary<string, string[]> ValidateUpdateUserRequest(UpdateUserRequest request)
{
    var errors = new Dictionary<string, string[]>();
    if (IsInvalidPatchValue(request.FirstName))
    {
        errors["firstName"] = ["FirstName cannot be empty when provided."];
    }

    if (IsInvalidPatchValue(request.LastName))
    {
        errors["lastName"] = ["LastName cannot be empty when provided."];
    }

    if (IsInvalidPatchValue(request.Email))
    {
        errors["email"] = ["Email cannot be empty when provided."];
    }

    if (IsInvalidPatchValue(request.Role))
    {
        errors["role"] = ["Role cannot be empty when provided."];
    }

    return errors;
}

static bool IsInvalidPatchValue(string? value)
{
    if (value is null)
    {
        return false;
    }

    var trimmed = value.Trim();
    return trimmed.Length == 0;
}

static Dictionary<string, string[]> ValidateCreateUserRequest(CreateUserRequest request)
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

    if (string.IsNullOrWhiteSpace(request.PasswordHash))
    {
        errors["passwordHash"] = ["PasswordHash is required."];
    }

    return errors;
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
