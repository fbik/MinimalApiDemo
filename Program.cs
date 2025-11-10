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
    c.RoutePrefix = "swagger"; // Swagger будет доступен по /swagger
});

app.UseHttpsRedirection();

// Basic endpoint
app.MapGet("/", () => "Hello World! Minimal API is working!");

// Endpoint с параметром
app.MapGet("/hello/{name}", (string name) => $"Hello {name}!");

// Endpoint с JSON ответом - теперь используем нашу модель
app.MapGet("/api/user", () => 
{
    var user = new User { Id = 1, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
    return Results.Json(user);
});

// GET все пользователи (заглушка)
app.MapGet("/api/users", () =>
{
    var users = new List<User>
    {
        new User { Id = 1, Name = "Irina", Email = "irina@example.com", CreatedAt = DateTime.UtcNow },
        new User { Id = 2, Name = "Alex", Email = "alex@example.com", CreatedAt = DateTime.UtcNow.AddDays(-1) }
    };
    return Results.Ok(users);
});

// GET пользователь по ID
app.MapGet("/api/users/{id}", (int id) =>
{
    // Заглушка - в реальном приложении здесь поиск в БД
    if (id == 1)
    {
        var user = new User { Id = 1, Name = "Irina", Email = "irina@example.com", CreatedAt = DateTime.UtcNow };
        return Results.Ok(user);
    }
    return Results.NotFound($"User with ID {id} not found");
});

// POST - создание пользователя с валидацией
app.MapPost("/api/users", (User user) =>
{
    // Базовая валидация
    if (string.IsNullOrEmpty(user.Name) || string.IsNullOrEmpty(user.Email))
    {
        return Results.BadRequest("Name and Email are required");
    }

    // Здесь обычно сохраняем в базу данных
    // Пока просто возвращаем созданного пользователя
    user.Id = new Random().Next(10, 1000); // Генерируем ID
    user.CreatedAt = DateTime.UtcNow;
    
    return Results.Created($"/api/users/{user.Id}", user);
});

// PUT - обновление пользователя
app.MapPut("/api/users/{id}", (int id, User updatedUser) =>
{
    // Заглушка - в реальном приложении обновляем в БД
    if (id != 1)
    {
        return Results.NotFound($"User with ID {id} not found");
    }

    updatedUser.Id = id;
    return Results.Ok(updatedUser);
});

// DELETE - удаление пользователя
app.MapDelete("/api/users/{id}", (int id) =>
{
    // Заглушка - в реальном приложении удаляем из БД
    if (id != 1)
    {
        return Results.NotFound($"User with ID {id} not found");
    }

    return Results.NoContent();
});

app.Run();
=