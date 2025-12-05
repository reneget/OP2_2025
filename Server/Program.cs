using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Modules.Database;
using ServerLogLevel = Server.Modules.Logging.LogLevel;
using Server.Modules.Logging;
using Server.Modules.Sorting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:8080",
                "http://localhost:3000",
                "http://127.0.0.1:8080",
                "http://127.0.0.1:3000"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Sorting Service API",
        Version = "v1",
        Description = "API для сортировки массивов методом расчёстки (Comb Sort). " +
                     "Система предоставляет функционал сортировки массивов целых чисел " +
                     "с возможностью просмотра логов операций.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Sorting Service",
        }
    });
    
    // Включить XML комментарии
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
    // Добавить поддержку cookie authentication в Swagger
    c.AddSecurityDefinition("cookieAuth", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Cookie,
        Name = ".AspNetCore.Cookies",
        Description = "Cookie-based authentication. Авторизуйтесь через /api/login для получения cookie."
    });
    
});

// Настройка модулей
var logDirectory = builder.Configuration["Logging:Directory"] ?? "./logs";
var dbPath = builder.Configuration["Database:Path"] ?? "./data/users.db";

var logManager = new LogManager(logDirectory);
var dbManager = new DBManager();
var combSortModule = new CombSortModule();

builder.Services.AddSingleton(logManager);
builder.Services.AddSingleton(dbManager);
builder.Services.AddSingleton(combSortModule);

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sorting Service API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Sorting Service API Documentation";
    c.DefaultModelsExpandDepth(-1);
});

if (!dbManager.ConnectToDB(dbPath))
{
    logManager.Log(ServerLogLevel.ERROR, $"Failed to connect to database at {dbPath}");
    Console.WriteLine($"Failed to connect to database at {dbPath}");
    Console.WriteLine("Shutdown!");
    return;
}

logManager.Log(ServerLogLevel.INFO, "Server started successfully");

app.MapGet("/", () => 
{
    var swaggerUrl = "/swagger";
    return $"Sorting Service API - Comb Sort\n\n" +
           $"API Documentation: {swaggerUrl}\n" +
           $"Open {swaggerUrl} in your browser to view interactive API documentation.";
});

app.MapPost("/api/sort", [Authorize] ([FromBody] SortRequest request, [FromServices] CombSortModule sortModule, [FromServices] LogManager logger, HttpContext context) =>
{
    var username = context.User.Identity?.Name ?? "unknown";
    
    try
    {
        if (request.Array == null || request.Array.Length == 0)
        {
            logger.Log(ServerLogLevel.WARNING, "Empty array provided for sorting", username);
            return Results.BadRequest(new { error = "Array cannot be empty" });
        }

        var sortedArray = sortModule.Sort(request.Array, request.Ascending ?? true);
        
        // Сохраняем входной и выходной массивы в лог
        logger.LogSortOperation($"Sorted array ({request.Array.Length} elements)", request.Array, sortedArray, username);
        
        return Results.Ok(new SortResponse
        {
            OriginalArray = request.Array,
            SortedArray = sortedArray,
            Ascending = request.Ascending ?? true
        });
    }
    catch (Exception ex)
    {
        logger.Log(ServerLogLevel.ERROR, $"Error during sorting: {ex.Message}", username);
        return Results.Problem($"Error during sorting: {ex.Message}");
    }
});

app.MapGet("/api/logs", [Authorize] (
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to,
    [FromQuery] string? level,
    [FromServices] LogManager logger,
    HttpContext context) =>
{
    var username = context.User.Identity?.Name ?? "unknown";
    
    try
    {
        Server.Modules.Logging.LogLevel? logLevel = null;
        if (!string.IsNullOrEmpty(level) && Enum.TryParse<Server.Modules.Logging.LogLevel>(level, true, out var parsedLevel))
        {
            logLevel = parsedLevel;
        }

        var logs = logger.GetLogs(from, to, logLevel, username);
        
        logger.Log(ServerLogLevel.INFO, $"Retrieved {logs.Count} log entries", username);
        
        return Results.Ok(new
        {
            Count = logs.Count,
            Logs = logs.Select(l => new
            {
                l.Timestamp,
                Level = l.Level.ToString(),
                l.Message,
                l.UserId,
                InputArray = l.InputArray,
                OutputArray = l.OutputArray
            })
        });
    }
    catch (Exception ex)
    {
        logger.Log(ServerLogLevel.ERROR, $"Error retrieving logs: {ex.Message}", username);
        return Results.Problem($"Error retrieving logs: {ex.Message}");
    }
});

app.MapPost("/api/login", async ([FromBody] LoginRequest request, [FromServices] DBManager db, [FromServices] LogManager logger, HttpContext context) =>
{
    if (string.IsNullOrEmpty(request.Login) || string.IsNullOrEmpty(request.Password))
    {
        return Results.BadRequest(new { error = "Login and password are required" });
    }

    if (!db.CheckUser(request.Login, request.Password))
    {
        logger.Log(ServerLogLevel.WARNING, $"Failed login attempt for user: {request.Login}");
        return Results.Unauthorized();
    }

    var claims = new List<Claim> { new Claim(ClaimTypes.Name, request.Login) };
    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

    logger.Log(ServerLogLevel.INFO, $"User logged in: {request.Login}", request.Login);
    return Results.Ok(new { message = "Login successful", username = request.Login });
});

app.MapPost("/api/signup", ([FromBody] SignupRequest request, [FromServices] DBManager db, [FromServices] LogManager logger) =>
{
    if (string.IsNullOrEmpty(request.Login) || string.IsNullOrEmpty(request.Password))
    {
        return Results.BadRequest(new { error = "Login and password are required" });
    }

    if (db.AddUser(request.Login, request.Password))
    {
        logger.Log(ServerLogLevel.INFO, $"User registered: {request.Login}", request.Login);
        return Results.Ok(new { message = $"User {request.Login} registered successfully!" });
    }

    logger.Log(ServerLogLevel.WARNING, $"Failed to register user: {request.Login}");
    return Results.Problem($"Failed to register user {request.Login}");
});

app.MapGet("/api/check_user", [Authorize] (HttpContext context, [FromServices] LogManager logger) =>
{
    if (context.User.Identity == null)
        return Results.BadRequest(new { error = "User is unknown" });

    var username = context.User.Identity.Name ?? "unknown";
    logger.Log(ServerLogLevel.INFO, "User check performed", username);
    return Results.Ok(new { username });
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    logManager.Log(ServerLogLevel.INFO, "Server is shutting down");
    dbManager.Disconnect();
});

var port = Environment.GetEnvironmentVariable("PORT") ?? builder.Configuration["Server:Port"] ?? "5247";
app.Run($"http://0.0.0.0:{port}");

public class SortRequest
{
    /// <summary>
    /// Массив целых чисел для сортировки
    /// </summary>
    /// <example>[5, 2, 8, 1, 9, 3]</example>
    public int[] Array { get; set; } = new int[0];
    
    /// <summary>
    /// Направление сортировки: true - по возрастанию, false - по убыванию
    /// </summary>
    /// <example>true</example>
    public bool? Ascending { get; set; } = true;
}

/// <summary>
/// Результат сортировки массива
/// </summary>
public class SortResponse
{
    /// <summary>
    /// Исходный массив
    /// </summary>
    public int[] OriginalArray { get; set; } = new int[0];
    
    /// <summary>
    /// Отсортированный массив
    /// </summary>
    public int[] SortedArray { get; set; } = new int[0];
    
    /// <summary>
    /// Направление сортировки
    /// </summary>
    public bool Ascending { get; set; }
}

/// <summary>
/// Запрос на вход в систему
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    /// <example>user123</example>
    public string Login { get; set; } = string.Empty;
    
    /// <summary>
    /// Пароль пользователя
    /// </summary>
    /// <example>password123</example>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на регистрацию нового пользователя
/// </summary>
public class SignupRequest
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    /// <example>newuser</example>
    public string Login { get; set; } = string.Empty;
    
    /// <summary>
    /// Пароль пользователя
    /// </summary>
    /// <example>securepassword</example>
    public string Password { get; set; } = string.Empty;
}

