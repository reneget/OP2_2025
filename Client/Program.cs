using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Настройка статических файлов
builder.Services.AddControllers();

var app = builder.Build();

// Получить URL сервера из переменной окружения или конфигурации
var serverUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:5247";

// Статические файлы
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwrootPath),
    RequestPath = ""
});

// Endpoint для получения конфигурации (динамический)
app.MapGet("/config.js", () => 
{
    var serverUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:5247";
    // Если SERVER_URL указывает на внутренний сервис в Docker, заменяем на внешний адрес
    var configUrl = serverUrl;
    if (configUrl.Contains("server:5247"))
    {
        // В браузере нужно использовать localhost
        configUrl = "http://localhost:5247";
    }
    return Results.Content($"window.SERVER_URL = '{configUrl}';", "application/javascript");
});

// Fallback для SPA - все запросы перенаправлять на index.html
app.MapFallback(async context =>
{
    context.Response.ContentType = "text/html";
    var indexPath = Path.Combine(wwwrootPath, "index.html");
    if (File.Exists(indexPath))
    {
        await context.Response.SendFileAsync(indexPath);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("File not found");
    }
});

var port = Environment.GetEnvironmentVariable("CLIENT_PORT") ?? builder.Configuration["Client:Port"] ?? "8080";
var host = Environment.GetEnvironmentVariable("CLIENT_HOST") ?? "0.0.0.0";

Console.WriteLine($"Web client starting on http://{host}:{port}");
Console.WriteLine($"Server URL: {serverUrl}");

app.Run($"http://{host}:{port}");
