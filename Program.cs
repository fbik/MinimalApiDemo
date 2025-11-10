using MinimalApiDemo.Data;
using MinimalApiDemo.Models;
using MinimalApiDemo.DTOs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация
builder.Configuration.AddEnvironmentVariables();

// База данных
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Сервисы
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Миграции базы данных с повторными попытками
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    var retries = 10;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Attempting to apply migrations...");
            db.Database.Migrate();
            logger.LogInformation("Migrations applied successfully");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning(ex, "Migration failed, retries left: {Retries}", retries);
            if (retries == 0) throw;
            Thread.Sleep(5000);
        }
    }
}

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// Endpoints
app.MapGet("/", () => "Hello World! Minimal API with PostgreSQL is working!");


app.MapGet("/hello/{name}", (string name) => $"Hello {name}!");

// GET все пользователи
app.MapGet("/api/users", async (AppDbContext context) =>
{
    var users = await context.Users.ToListAsync();
    return Results.Ok(users);
});

// GET пользователь по ID
app.MapGet("/api/users/{id}", async (int id, AppDbContext context) =>
{
    var user = await context.Users.FindAsync(id);
    return user != null ? Results.Ok(user) : Results.NotFound($"User with ID {id} not found");
});

// POST - создание пользователя
app.MapPost("/api/users", async (CreateUserDto userDto, AppDbContext context) =>
{
    var user = new User
    {
        Name = userDto.Name,
        Email = userDto.Email,
        CreatedAt = DateTime.UtcNow
    };

    context.Users.Add(user);
    await context.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", user);
});

// PUT - обновление пользователя
app.MapPut("/api/users/{id}", async (int id, UpdateUserDto userDto, AppDbContext context) =>
{
    var user = await context.Users.FindAsync(id);
    if (user == null)
        return Results.NotFound($"User with ID {id} not found");


    user.Name = userDto.Name;
    user.Email = userDto.Email;

    await context.SaveChangesAsync();
    return Results.Ok(user);
});

// DELETE - удаление пользователя
app.MapDelete("/api/users/{id}", async (int id, AppDbContext context) =>
{
    var user = await context.Users.FindAsync(id);
    if (user == null)

  
      return Results.NotFound($"User with ID {id} not found");

    context.Users.Remove(user);
    await context.SaveChangesAsync();


    return Results.NoContent();
});

// Используем порт из переменной окружения или 8080 по умолчанию
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://*:{port}");
