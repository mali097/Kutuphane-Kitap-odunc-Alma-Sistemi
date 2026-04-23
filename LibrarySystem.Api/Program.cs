using LibrarySystem.Api.Data;
using LibrarySystem.Api.Entities;
using LibrarySystem.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// BookService'i sisteme enjekte ediyoruz
builder.Services.AddScoped<IBookService, BookService>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
// --- KÝTAP API UÇ NOKTALARI ---

// 1. Tüm Kitaplarý Listele (GET)
app.MapGet("/api/books", (IBookService bookService) =>
{
    var books = bookService.GetAllBooks();
    return Results.Ok(books);
});

// 2. Yeni Kitap Ekle (POST)
app.MapPost("/api/books", (Book newBook, IBookService bookService) =>
{
    var newId = bookService.AddBook(newBook);
    return Results.Ok(new { Message = "Kitap baþarýyla eklendi!", BookId = newId });
});

// 3. Kitap Sil (DELETE)
app.MapDelete("/api/books/{id}", (int id, IBookService bookService) =>
{
    var result = bookService.DeleteBook(id);
    return result ? Results.Ok("Silindi.") : Results.NotFound("Bulunamadý.");
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
