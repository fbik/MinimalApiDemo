using MinimalApiDemo.Models;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "My Minimal API", Version = "v1" });
});

var app = builder.Build();

// Настройка Swagger
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// In-memory хранилище
var users = new List<User>
{
    new User { Id = 1, Name = "Irina", Email = "irina@example.com", CreatedAt = DateTime.UtcNow }
};

// Basic endpoint
app.MapGet("/", () => "Hello World! Minimal API is working!");

// Endpoint с параметром
app.MapGet("/hello/{name}", (string name) => $"Hello {name}!");

// GET все пользователи
app.MapGet("/api/users", () => Results.Ok(users));

// GET пользователь по ID
app.MapGet("/api/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user != null ? Results.Ok(user) : Results.NotFound($"User with ID {id} not found");
});

// POST - создание пользователя
app.MapPost("/api/users", (User user) =>
{
    // Базовая валидация
    if (string.IsNullOrEmpty(user.Name) || string.IsNullOrEmpty(user.Email))
    {
        return Results.BadRequest("Name and Email are required");
    }

    // Генерируем новый ID
    user.Id = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
    user.CreatedAt = DateTime.UtcNow;
    
    users.Add(user);
    return Results.Created($"/api/users/{user.Id}", user);
});

// PUT - обновление пользователя
app.MapPut("/api/users/{id}", (int id, User updatedUser) =>
{
    var existingUser = users.FirstOrDefault(u => u.Id == id);
    if (existingUser == null)
    {
        return Results.NotFound($"User with ID {id} not found");
    }

    // Обновляем данные
    existingUser.Name = updatedUser.Name;
    existingUser.Email = updatedUser.Email;
    
    return Results.Ok(existingUser);
});

// DELETE - удаление пользователя
app.MapDelete("/api/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user == null)
    {
        return Results.NotFound($"User with ID {id} not found");
    }

    users.Remove(user);
    return Results.NoContent();
});

// Используем порт из переменной окружения или 8080 по умолчанию
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://*:{port}");
