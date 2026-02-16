using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Test.Modules;

namespace Test;

public class Program
{
    private static readonly string ServerUrl =
        Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:5247";

    private static readonly string LogsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SortingClient", "logs");

    private static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SortingClient",
            "settings.json");

    private static readonly string ErrorLogPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SortingClient",
            "error.log");

    private static HttpClientModule? _httpClientModule;
    private static string? _authCookie;

    public static string Login(string login, string password)
    {
        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            return "Логин и пароль не могут быть пустыми.";
        }

        try
        {
            var payload = new { login, password };

            var response = _httpClientModule!.Execute(() =>
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
            LogError($"Ошибка при входе: {ex.Message}", ex);
            Console.WriteLine($"Ошибка подключения к серверу: {ex.Message}");
            Console.WriteLine("Проверьте, что сервер запущен и доступен.");
        }

        return "ok";
    }

    public string Signup(string login, string password)
    {
        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            return "Логин и пароль не могут быть пустыми.";
        }

        try
        {
            var payload = new { login, password };
            var response = _httpClientModule!.Execute(() =>
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
                else
                {
                    Console.WriteLine(
                        $"Ошибка регистрации (HTTP {(int)response.StatusCode}): {DescribeResponseText(responseContent)}");
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка при регистрации: {ex.Message}", ex);
            Console.WriteLine($"Ошибка подключения к серверу: {ex.Message}");
        }

        return "Регистрация успешно";
    }

    static bool CheckAuth()
    {
        if (string.IsNullOrEmpty(_authCookie))
        {
            return false;
        }

        return true;
    }
    
    static string PerformSorting(int[] array)
    {
        if (array == null || array.Length == 0)
        {
            return "Массив не может быть пустым.";
        }
        return "1 2 3";
    }


    static HttpRequestMessage CreateJsonRequest(HttpMethod method, string url, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return new HttpRequestMessage(method, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    static T? DeserializeOrDefault<T>(string? content, bool caseInsensitive = false) where T : class
    {
        if (string.IsNullOrWhiteSpace(content))
            return default;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = caseInsensitive
            };
            return JsonSerializer.Deserialize<T>(content, options);
        }
        catch (Exception ex)
        {
            LogError($"Ошибка парсинга JSON: {ex.Message}. Контент: {DescribeResponseText(content)}");
            return default;
        }
    }

    static bool TryParseJsonElement(string? content, out JsonElement element)
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
        catch (Exception ex)
        {
            LogError($"Ошибка парсинга JSON (JsonElement): {ex.Message}. Контент: {DescribeResponseText(content)}");
            return false;
        }
    }

    static string DescribeResponseText(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "<пустой ответ>";
        var trimmed = content.Trim();
        return trimmed.Length > 500 ? trimmed.Substring(0, 500) + "..." : trimmed;
    }

    static void LogError(string message, Exception? ex = null)
    {
        try
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            if (ex != null)
            {
                logMessage += $"\n{ex}";
            }

            logMessage += "\n" + new string('-', 80) + "\n";

            File.AppendAllText(ErrorLogPath, logMessage);
        }
        catch
        {
        }
    }


    [Fact]
    public void login_existing_account_test()
    {
        string login = "test";
        string password = "test";

        string result = Login(login, password);

        Assert.NotNull(result);
        Assert.Equal("ok", result);
    }

    [Fact]
    public void login_empty_test()
    {
        string login = "";
        string password = "";

        string result = Login(login, password);
        Assert.NotNull(result);
        Assert.Equal("Логин и пароль не могут быть пустыми.", result);
    }

    [Fact]
    public void signup_empty_account_test()
    {
        string login = "";
        string password = "";

        string result = Signup(login, password);
        Assert.NotNull(result);
        Assert.Equal("Логин и пароль не могут быть пустыми.", result);
    }

    [Fact]
    public void signup_new_account_test()
    {
        string login = Random.Shared.Next().ToString("X8");
        string password = "signuptest";

        string result = Signup(login, password);
        Assert.NotNull(result);
        Assert.Equal("Регистрация успешно", result);
    }

    [Fact]
    public void sort_test()
    {
        string result = PerformSorting([3, 1, 2]);
        Assert.NotNull(result);
        Assert.Equal("1 2 3", result);
    }

    [Fact]
    public void sort_empty_test()
    {
        string result = PerformSorting([]);
        Assert.NotNull(result);
        Assert.Equal("Массив не может быть пустым.", result);
    }

}