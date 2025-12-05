using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Client;

class Program
{
    private static readonly CookieContainer cookieContainer = new CookieContainer();
    private static readonly HttpClientHandler handler = new HttpClientHandler
    {
        CookieContainer = cookieContainer,
        UseCookies = true
    };
    private static readonly HttpClient client = new HttpClient(handler);
    private static string? _username = null;

    static void Main(string[] args)
    {
        var serverUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:5247";
        client.BaseAddress = new Uri(serverUrl);

        Console.WriteLine("=== Сортировка расчёсткой - Клиент ===");
        Console.WriteLine($"Подключение к серверу: {serverUrl}\n");

        bool authenticated = false;
        while (!authenticated)
        {
            Console.WriteLine("1. Войти");
            Console.WriteLine("2. Зарегистрироваться");
            Console.Write("Выберите действие: ");

            var choice = Console.ReadLine();

            if (choice == "1")
            {
                authenticated = Login();
            }
            else if (choice == "2")
            {
                Signup();
            }
            else
            {
                Console.WriteLine("Неверный выбор. Попробуйте снова.\n");
            }
        }

        if (!authenticated)
        {
            Console.WriteLine("Не удалось аутентифицироваться. Выход.");
            return;
        }

        // Главное меню
        while (true)
        {
            Console.WriteLine("\n=== Главное меню ===");
            Console.WriteLine("1. Отсортировать массив");
            Console.WriteLine("2. Просмотреть логи");
            Console.WriteLine("3. Выйти");
            Console.Write("Выберите действие: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    PerformSort();
                    break;
                case "2":
                    ViewLogs();
                    break;
                case "3":
                    Console.WriteLine("Выход...");
                    return;
                default:
                    Console.WriteLine("Неверный выбор. Попробуйте снова.");
                    break;
            }
        }
    }

    static bool Login()
    {
        Console.Write("Введите логин: ");
        var login = Console.ReadLine();
        Console.Write("Введите пароль: ");
        var password = Console.ReadLine();

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Логин и пароль не могут быть пустыми.");
            return false;
        }

        try
        {
            var request = new { Login = login, Password = password };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = client.PostAsync("/api/login", content).Result;

            if (response.IsSuccessStatusCode)
            {
                _username = login;
                Console.WriteLine("Успешный вход!");
                return true;
            }
            else
            {
                Console.WriteLine("Неверный логин или пароль.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при входе: {ex.Message}");
            return false;
        }
    }

    static void Signup()
    {
        Console.Write("Введите логин: ");
        var login = Console.ReadLine();
        Console.Write("Введите пароль: ");
        var password = Console.ReadLine();

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Логин и пароль не могут быть пустыми.");
            return;
        }

        try
        {
            var request = new { Login = login, Password = password };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = client.PostAsync("/api/signup", content).Result;
            var result = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Пользователь {login} успешно зарегистрирован!");
            }
            else
            {
                Console.WriteLine($"Ошибка регистрации: {result}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при регистрации: {ex.Message}");
        }
    }

    static void PerformSort()
    {
        Console.WriteLine("\n=== Сортировка массива ===");
        Console.Write("Введите числа через пробел: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("Массив не может быть пустым.");
            return;
        }

        try
        {
            var numbers = input.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => int.Parse(s))
                              .ToArray();

            Console.Write("Сортировать по возрастанию? (y/n): ");
            var ascendingInput = Console.ReadLine()?.ToLower();
            var ascending = ascendingInput != "n";

            var request = new
            {
                Array = numbers,
                Ascending = ascending
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine("\nОтправка запроса на сервер...");
            var response = client.PostAsync("/api/sort", content).Result;
            var responseJson = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                var sortResponse = JsonSerializer.Deserialize<SortResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (sortResponse != null)
                {
                    Console.WriteLine($"\nИсходный массив: [{string.Join(", ", sortResponse.OriginalArray)}]");
                    Console.WriteLine($"Отсортированный массив: [{string.Join(", ", sortResponse.SortedArray)}]");
                    Console.WriteLine($"Направление: {(sortResponse.Ascending ? "По возрастанию" : "По убыванию")}");
                }
            }
            else
            {
                Console.WriteLine($"Ошибка сортировки: {responseJson}");
            }
        }
        catch (FormatException)
        {
            Console.WriteLine("Ошибка: введите только целые числа, разделённые пробелами.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    static void ViewLogs()
    {
        Console.WriteLine("\n=== Просмотр логов ===");
        Console.WriteLine("1. За последние 24 часа");
        Console.WriteLine("2. За последнюю неделю");
        Console.WriteLine("3. За последний месяц");
        Console.WriteLine("4. Все логи");
        Console.Write("Выберите период: ");

        var choice = Console.ReadLine();
        DateTime? from = null;
        DateTime? to = null;

        switch (choice)
        {
            case "1":
                from = DateTime.UtcNow.AddDays(-1);
                break;
            case "2":
                from = DateTime.UtcNow.AddDays(-7);
                break;
            case "3":
                from = DateTime.UtcNow.AddDays(-30);
                break;
            case "4":
                from = null;
                break;
            default:
                Console.WriteLine("Неверный выбор.");
                return;
        }

        try
        {
            var queryParams = new List<string>();
            if (from.HasValue)
                queryParams.Add($"from={from.Value:yyyy-MM-ddTHH:mm:ssZ}");
            if (to.HasValue)
                queryParams.Add($"to={to.Value:yyyy-MM-ddTHH:mm:ssZ}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = client.GetAsync($"/api/logs{queryString}").Result;
            var responseJson = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                var logsResponse = JsonSerializer.Deserialize<LogsResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (logsResponse != null && logsResponse.Logs != null)
                {
                    Console.WriteLine($"\nНайдено записей: {logsResponse.Count}");
                    Console.WriteLine("=" .PadRight(80, '='));
                    foreach (var log in logsResponse.Logs)
                    {
                        Console.WriteLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.Level}] " +
                                        $"{(log.UserId != null ? $"[User: {log.UserId}] " : "")}" +
                                        $"{log.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Ошибка получения логов: {responseJson}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }
}

// DTOs
public class SortResponse
{
    public int[] OriginalArray { get; set; } = Array.Empty<int>();
    public int[] SortedArray { get; set; } = Array.Empty<int>();
    public bool Ascending { get; set; }
}

public class LogsResponse
{
    public int Count { get; set; }
    public List<LogEntry>? Logs { get; set; }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
}

