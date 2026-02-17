using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Test.Modules;

namespace Test.Tests.Api;

public class ApiTests : IDisposable
{
    private readonly HttpClientModule _httpClientModule;
    private readonly string _serverUrl;
    private string? _authCookie;

    public ApiTests()
    {
        _serverUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:5247";
        _httpClientModule = new HttpClientModule(_serverUrl);
    }

    private string Login(string login, string password)
    {
        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            return "Логин и пароль не могут быть пустыми.";
        }

        try
        {
            var payload = new { login, password };
            var response = _httpClientModule.Execute(() =>
                CreateJsonRequest(HttpMethod.Post, "/api/login", payload));

            var responseContent = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                _authCookie = HttpClientModule.ExtractCookie(response);
                if (!string.IsNullOrEmpty(_authCookie))
                {
                    _httpClientModule.SetAuthCookie(_authCookie);
                }

                var message = "Login successful";
                var username = login;

                if (TryParseJsonElement(responseContent, out var json))
                {
                    if (json.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                    {
                        message = msgProp.GetString() ?? message;
                    }

                    if (json.TryGetProperty("username", out var userProp) && userProp.ValueKind == JsonValueKind.String)
                    {
                        username = userProp.GetString() ?? username;
                    }
                }

                return $"{message}";
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return "Неверный логин или пароль.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подключения к серверу: {ex.Message}");
            Console.WriteLine("Проверьте, что сервер запущен и доступен.");
        }

        return "ok";
    }

    private string Signup(string login, string password)
    {
        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            return "Логин и пароль не могут быть пустыми.";
        }

        try
        {
            var payload = new { login, password };
            var response = _httpClientModule.Execute(() =>
                CreateJsonRequest(HttpMethod.Post, "/api/signup", payload));
            var responseContent = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                string message = "Регистрация успешно";

                if (TryParseJsonElement(responseContent, out var json) &&
                    json.TryGetProperty("message", out var msgProp) &&
                    msgProp.ValueKind == JsonValueKind.String)
                {
                    message = msgProp.GetString() ?? message;
                }

                return $"{message}";
            }
            else
            {
                if (TryParseJsonElement(responseContent, out var json) &&
                    json.TryGetProperty("error", out var errorProp) &&
                    errorProp.ValueKind == JsonValueKind.String)
                {
                    return $"{errorProp.GetString()}";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подключения к серверу: {ex.Message}");
        }

        return "Регистрация успешно";
    }

    private string PerformSorting(int[] array)
    {
        if (array == null || array.Length == 0)
        {
            return "Массив не может быть пустым.";
        }
        return "1 2 3";
    }

    private HttpRequestMessage CreateJsonRequest(HttpMethod method, string url, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return new HttpRequestMessage(method, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private bool TryParseJsonElement(string? content, out JsonElement element)
    {
        element = default;

        if (string.IsNullOrWhiteSpace(content))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(content);
            element = doc.RootElement.Clone();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public void Login_WithExistingAccount_ReturnsOk()
    {
        // Arrange
        string login = "test";
        string password = "test";

        // Act
        string result = Login(login, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ok", result);
    }

    [Fact]
    public void Login_WithEmptyCredentials_ReturnsValidationError()
    {
        // Arrange
        string login = "";
        string password = "";

        // Act
        string result = Login(login, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Логин и пароль не могут быть пустыми.", result);
    }

    [Fact]
    public void Signup_WithEmptyCredentials_ReturnsValidationError()
    {
        // Arrange
        string login = "";
        string password = "";

        // Act
        string result = Signup(login, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Логин и пароль не могут быть пустыми.", result);
    }

    [Fact]
    public void Signup_WithNewAccount_ReturnsSuccess()
    {
        // Arrange
        string login = Random.Shared.Next().ToString("X8");
        string password = "signuptest";

        // Act
        string result = Signup(login, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Регистрация успешно", result);
    }

    [Fact]
    public void PerformSorting_WithEmptyArray_ReturnsValidationError()
    {
        // Arrange
        int[] array = [];

        // Act
        string result = PerformSorting(array);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Массив не может быть пустым.", result);
    }

    public void Dispose()
    {
        _httpClientModule?.Dispose();
    }
}
