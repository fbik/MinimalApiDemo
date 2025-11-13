using MinimalApiDemo.Data;
using MinimalApiDemo.Models;
using MinimalApiDemo.DTOs;
using MinimalApiDemo.Services;
using MinimalApiDemo.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация
builder.Configuration.AddEnvironmentVariables();

// База данных
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Сервисы
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PasswordHasher>();

// Валидаторы
builder.Services.AddScoped<IValidator<LoginUser>, LoginUserValidator>();
builder.Services.AddScoped<IValidator<CreateUserDto>, CreateUserDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateUserDto>, UpdateUserDtoValidator>();

// CORS - разрешаем запросы с фронтенда
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// JWT Аутентификация
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// CORS - ДО любого другого middleware
app.UseCors("AllowFrontend");

// Статические файлы
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
    RequestPath = ""
});

// Затем аутентификация
app.UseAuthentication();
app.UseAuthorization();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// Миграции базы данных
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Testing database connection...");
        var canConnect = await db.Database.CanConnectAsync();
        
        if (canConnect)
        {
            logger.LogInformation("Database connected, applying migrations...");
            await db.Database.MigrateAsync();
            
            if (!await db.AppUsers.AnyAsync())
            {
                var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
                var testUser = new AppUser 
                { 
                    Username = "admin", 
                    Email = "admin@example.com",
                    PasswordHash = hasher.HashPassword("Admin123!"),
                    Role = "Admin"
                };
                db.AppUsers.Add(testUser);
                await db.SaveChangesAsync();
                logger.LogInformation("Test user created: admin/Admin123!");
            }
            
            logger.LogInformation("Migrations applied successfully");
        }
        else
        {
            logger.LogWarning("Cannot connect to database, skipping migrations");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed");
    }
}

// Главная страница
app.MapGet("/", () => Results.Redirect("/index.html"));

// Public endpoints
app.MapGet("/hello/{name}", (string name) => $"Hello {name}!");

// Auth endpoints с валидацией
app.MapPost("/api/auth/register", async (LoginUser user, AppDbContext context, PasswordHasher hasher, IValidator<LoginUser> validator) =>
{
    try
    {
        // Валидация
        var validationResult = await validator.ValidateAsync(user);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Results.BadRequest(new { errors });
        }

        // Проверяем существует ли пользователь
        if (await context.AppUsers.AnyAsync(u => u.Username == user.Username))
            return Results.BadRequest("Username already exists");

        var newUser = new AppUser
        {
            Username = user.Username,
            PasswordHash = hasher.HashPassword(user.Password),
            Email = $"{user.Username}@example.com",
            Role = "User"
        };

        context.AppUsers.Add(newUser);
        await context.SaveChangesAsync();

        return Results.Ok("User registered successfully");
    }
    catch (Exception ex)
    {
        return Results.Problem("Registration error: " + ex.Message);
    }
});

app.MapPost("/api/auth/login", async (LoginUser user, AppDbContext context, PasswordHasher hasher, TokenService tokenService, IValidator<LoginUser> validator) =>
{
    try
    {
        // Валидация
        var validationResult = await validator.ValidateAsync(user);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Results.BadRequest(new { errors });
        }

        var dbUser = await context.AppUsers.FirstOrDefaultAsync(u => u.Username == user.Username);
        if (dbUser == null || !hasher.VerifyPassword(user.Password, dbUser.PasswordHash))
            return Results.Unauthorized();

        var token = tokenService.GenerateToken(dbUser);
        var response = new AuthResponse
        {
            Token = token,
            Expires = DateTime.Now.AddHours(2),
            Username = dbUser.Username,
            Role = dbUser.Role
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Login error: " + ex.Message);
    }
});

// Protected endpoints с валидацией
app.MapGet("/api/users", async (AppDbContext context) =>
{
    try
    {
        var users = await context.Users.ToListAsync();
        var response = users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            CreatedAt = u.CreatedAt
        });
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Database error: " + ex.Message);
    }
}).RequireAuthorization();

app.MapGet("/api/users/{id}", async (int id, AppDbContext context) =>
{
    try
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
            return Results.NotFound($"User with ID {id} not found");

        var response = new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Database error: " + ex.Message);
    }
}).RequireAuthorization();

app.MapPost("/api/users", async (CreateUserDto userDto, AppDbContext context, IValidator<CreateUserDto> validator) =>
{
    try
    {
        // Валидация
        var validationResult = await validator.ValidateAsync(userDto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Results.BadRequest(new { errors });
        }

        var user = new User
        {
            Name = userDto.Name,
            Email = userDto.Email,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var response = new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };

        return Results.Created($"/api/users/{user.Id}", response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Database error: " + ex.Message);
    }
}).RequireAuthorization();

app.MapPut("/api/users/{id}", async (int id, UpdateUserDto userDto, AppDbContext context, IValidator<UpdateUserDto> validator) =>
{
    try
    {
        // Валидация
        var validationResult = await validator.ValidateAsync(userDto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Results.BadRequest(new { errors });
        }

        var user = await context.Users.FindAsync(id);
        if (user == null)
            return Results.NotFound($"User with ID {id} not found");

        user.Name = userDto.Name;
        user.Email = userDto.Email;

        await context.SaveChangesAsync();

        var response = new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Database error: " + ex.Message);
    }
}).RequireAuthorization();

app.MapDelete("/api/users/{id}", async (int id, AppDbContext context) =>
{
    try
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
            return Results.NotFound($"User with ID {id} not found");

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem("Database error: " + ex.Message);
    }
}).RequireAuthorization();

// Profile endpoint
app.MapGet("/api/profile", (ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var username = user.FindFirst(ClaimTypes.Name)?.Value;
    var email = user.FindFirst(ClaimTypes.Email)?.Value;
    var role = user.FindFirst(ClaimTypes.Role)?.Value;

    return Results.Ok(new { 
        userId, 
        username, 
        email, 
        role,
        message = "This data comes from your JWT token!" 
    });
}).RequireAuthorization();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://*:{port}");
