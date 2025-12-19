using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Client.Modules;

namespace Client;

class Program
{
    private static readonly string ServerUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:5247";
    private static readonly string LogsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SortingClient", "logs");
    private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SortingClient", "settings.json");
    private static readonly string ErrorLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SortingClient", "error.log");
    
    private static HttpClientModule? _httpClientModule;
    private static string? _authCookie;
    private static DisplaySettings _displaySettings = new();

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        // Инициализация директорий
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        
        // Загрузка настроек
        LoadSettings();
        
        // Инициализация HTTP клиента
        _httpClientModule = new HttpClientModule(ServerUrl, maxRetries: 3, retryDelayMs: 1000);
        
        // Приветствие
        ShowWelcome();
        
        // Главный цикл
        bool running = true;
        while (running)
        {
            try
            {
                running = await MainMenu();
            }
            catch (Exception ex)
            {
                LogError($"Критическая ошибка: {ex.Message}", ex);
                Console.WriteLine($"\n❌ Произошла ошибка: {ex.Message}");
                Console.WriteLine("Нажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }
        
        Console.WriteLine("\n👋 До свидания!");
    }

    static void ShowWelcome()
    {
        Console.Clear();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("     🧮 СИСТЕМА СОРТИРОВКИ РАСЧЁСТКОЙ (COMB SORT)");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine();
        ShowAlgorithmInfo();
        Console.WriteLine();
    }

    static void ShowAlgorithmInfo()
    {
        Console.WriteLine("📖 СПРАВКА ПО АЛГОРИТМУ СОРТИРОВКИ «РАСЧЁСТКОЙ»:");
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("Алгоритм сортировки расчёсткой (Comb Sort) - это улучшенная");
        Console.WriteLine("версия пузырьковой сортировки, разработанная Влодзимежем");
        Console.WriteLine("Добосевичем в 1980 году.");
        Console.WriteLine();
        Console.WriteLine("Принцип работы:");
        Console.WriteLine("  • Использует «шаг отбрасывания» (gap) для сравнения");
        Console.WriteLine("    элементов на расстоянии друг от друга");
        Console.WriteLine("  • Начальный шаг обычно равен размеру массива / 1.3");
        Console.WriteLine("  • Шаг уменьшается на каждой итерации");
        Console.WriteLine("  • Когда шаг становится равным 1, выполняется финальный");
        Console.WriteLine("    проход пузырьковой сортировки");
        Console.WriteLine();
        Console.WriteLine("Преимущества:");
        Console.WriteLine("  • Быстрее пузырьковой сортировки");
        Console.WriteLine("  • Простая реализация");
        Console.WriteLine("  • Эффективен для небольших и средних массивов");
        Console.WriteLine("───────────────────────────────────────────────────────────");
    }

    static async Task<bool> MainMenu()
    {
        Console.WriteLine("\n📋 ГЛАВНОЕ МЕНЮ:");
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("1. 🔐 Вход в систему");
        Console.WriteLine("2. 📝 Регистрация");
        Console.WriteLine("3. 🧮 Выполнить сортировку");
        Console.WriteLine("4. 📊 Просмотр логов");
        Console.WriteLine("5. ⚙️  Настройки вывода");
        Console.WriteLine("6. 📖 Показать справку");
        Console.WriteLine("0. 🚪 Выход");
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.Write("Выберите действие: ");

        var choice = Console.ReadLine()?.Trim();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await Login();
                return true;
            case "2":
                await Signup();
                return true;
            case "3":
                if (await CheckAuth())
                {
                    await PerformSorting();
                }
                return true;
            case "4":
                if (await CheckAuth())
                {
                    await ViewLogs();
                }
                return true;
            case "5":
                await ManageSettings();
                return true;
            case "6":
                ShowAlgorithmInfo();
                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();
                ShowWelcome();
                return true;
            case "0":
                return false;
            default:
                Console.WriteLine("❌ Неверный выбор. Попробуйте снова.");
                return true;
        }
    }

    static async Task Login()
    {
        Console.WriteLine("🔐 ВХОД В СИСТЕМУ");
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.Write("Логин: ");
        var login = Console.ReadLine()?.Trim();
        Console.Write("Пароль: ");
        var password = ReadPassword();

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("❌ Логин и пароль не могут быть пустыми.");
            return;
        }

        try
        {
            var payload = new { login, password };
            var response = await _httpClientModule!.ExecuteWithRetryAsync(() =>
                CreateJsonRequest(HttpMethod.Post, "/api/login", payload));
            
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Сохраняем cookie для последующих запросов
                _authCookie = HttpClientModule.ExtractCookie(response);
                if (!string.IsNullOrEmpty(_authCookie))
                {
                    _httpClientModule.SetAuthCookie(_authCookie);
                }

                string message = "Login successful";
                string username = login;

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

                Console.WriteLine($"✅ {message}");
                Console.WriteLine($"👤 Пользователь: {username}");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("❌ Неверный логин или пароль.");
            }
            else
            {
                Console.WriteLine($"❌ Ошибка входа (HTTP {(int)response.StatusCode}): {DescribeResponseText(responseContent)}");
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка при входе: {ex.Message}", ex);
            Console.WriteLine($"❌ Ошибка подключения к серверу: {ex.Message}");
            Console.WriteLine("Проверьте, что сервер запущен и доступен.");
        }
    }

    static async Task Signup()
    {
        Console.WriteLine("📝 РЕГИСТРАЦИЯ");
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.Write("Логин: ");
        var login = Console.ReadLine()?.Trim();
        Console.Write("Пароль: ");
        var password = ReadPassword();

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("❌ Логин и пароль не могут быть пустыми.");
            return;
        }

        try
        {
            var payload = new { login, password };
            var response = await _httpClientModule!.ExecuteWithRetryAsync(() =>
                CreateJsonRequest(HttpMethod.Post, "/api/signup", payload));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                string message = "Регистрация успешно";

                if (TryParseJsonElement(responseContent, out var json) &&
                    json.TryGetProperty("message", out var msgProp) &&
                    msgProp.ValueKind == JsonValueKind.String)
                {
                    message = msgProp.GetString() ?? message;
                }

                Console.WriteLine($"✅ {message}");
            }
            else
            {
                if (TryParseJsonElement(responseContent, out var json) &&
                    json.TryGetProperty("error", out var errorProp) &&
                    errorProp.ValueKind == JsonValueKind.String)
                {
                    Console.WriteLine($"❌ {errorProp.GetString()}");
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка регистрации (HTTP {(int)response.StatusCode}): {DescribeResponseText(responseContent)}");
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка при регистрации: {ex.Message}", ex);
            Console.WriteLine($"❌ Ошибка подключения к серверу: {ex.Message}");
        }
    }

    static Task<bool> CheckAuth()
    {
        if (string.IsNullOrEmpty(_authCookie))
        {
            Console.WriteLine("❌ Вы не авторизованы. Пожалуйста, войдите в систему.");
            Console.WriteLine("Нажмите любую клавишу для продолжения...");
            Console.ReadKey();
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    static async Task PerformSorting()
    {
        Console.WriteLine("🧮 ВЫПОЛНЕНИЕ СОРТИРОВКИ");
        Console.WriteLine("───────────────────────────────────────────────────────────");
        
        // Выбор способа ввода данных
        Console.WriteLine("Выберите способ ввода данных:");
        Console.WriteLine("1. Ввод вручную");
        Console.WriteLine("2. Загрузка из файла");
        Console.Write("Выбор: ");
        
        var inputChoice = Console.ReadLine()?.Trim();
        int[]? array = null;

        if (inputChoice == "1")
        {
            array = await InputArrayManually();
        }
        else if (inputChoice == "2")
        {
            array = await LoadArrayFromFile();
        }
        else
        {
            Console.WriteLine("❌ Неверный выбор.");
            return;
        }

        if (array == null || array.Length == 0)
        {
            Console.WriteLine("❌ Массив не может быть пустым.");
            return;
        }

        // Выбор направления сортировки
        Console.Write("\nНаправление сортировки (1 - по возрастанию, 2 - по убыванию) [1]: ");
        var sortDirection = Console.ReadLine()?.Trim();
        bool ascending = sortDirection != "2";

        // Выбор шага отбрасывания
        Console.Write($"Шаг отбрасывания (Enter для автоматического выбора, рекомендуемый: {(int)(array.Length / 1.3)}): ");
        var gapInput = Console.ReadLine()?.Trim();
        int? gap = null;
        if (!string.IsNullOrEmpty(gapInput) && int.TryParse(gapInput, out var gapValue) && gapValue > 0 && gapValue <= array.Length)
        {
            gap = gapValue;
        }

        // Выполнение сортировки
        try
        {
            var payload = new
            {
                array,
                ascending,
                gap = gap.HasValue ? gap : null
            };

            var response = await _httpClientModule!.ExecuteWithRetryAsync(() =>
                CreateJsonRequest(HttpMethod.Post, "/api/sort", payload));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = DeserializeOrDefault<SortResponse>(responseContent, caseInsensitive: true);

                if (result != null)
                {
                    DisplaySortResult(result);
                    
                    // Предложение сохранить в лог
                    Console.Write("\n💾 Сохранить результат в файл логов? (y/n) [y]: ");
                    var saveChoice = Console.ReadLine()?.Trim().ToLower();
                    if (saveChoice != "n")
                    {
                        await SaveToLogFile(result);
                    }

                    // Предложение повторить
                    Console.Write("\n🔄 Выполнить еще одну сортировку? (y/n) [n]: ");
                    var repeatChoice = Console.ReadLine()?.Trim().ToLower();
                    if (repeatChoice == "y")
                    {
                        await PerformSorting();
                    }
                }
                else
                {
                    Console.WriteLine("❌ Ошибка сортировки: пустой ответ от сервера.");
                }
            }
            else
            {
                if (TryParseJsonElement(responseContent, out var errorJson) &&
                    errorJson.TryGetProperty("error", out var errorProp) &&
                    errorProp.ValueKind == JsonValueKind.String)
                {
                    Console.WriteLine($"❌ Ошибка сортировки: {errorProp.GetString()}");
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка сортировки (HTTP {(int)response.StatusCode}): {DescribeResponseText(responseContent)}");
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка при сортировке: {ex.Message}", ex);
            Console.WriteLine($"❌ Ошибка подключения к серверу: {ex.Message}");
        }
    }

    static Task<int[]?> InputArrayManually()
    {
        Console.WriteLine("\nВведите массив чисел через пробел или запятую:");
        Console.Write("Массив: ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
        {
            return Task.FromResult<int[]?>(null);
        }

        return Task.FromResult<int[]?>(ValidationModule.ValidateArray(input));
    }

    static async Task<int[]?> LoadArrayFromFile()
    {
        Console.Write("\nВведите абсолютный путь к файлу: ");
        var filePath = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("❌ Файл не найден или путь указан неверно.");
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            return ValidationModule.ValidateArray(content);
        }
        catch (Exception ex)
        {
            LogError($"Ошибка чтения файла: {ex.Message}", ex);
            Console.WriteLine($"❌ Ошибка чтения файла: {ex.Message}");
            return null;
        }
    }

    static void DisplaySortResult(SortResponse result)
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("                    РЕЗУЛЬТАТ СОРТИРОВКИ");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        
        if (_displaySettings.ShowOriginalArray)
        {
            Console.WriteLine($"📥 Исходный массив: [{string.Join(", ", result.OriginalArray)}]");
        }
        
        if (_displaySettings.ShowSortedArray)
        {
            Console.WriteLine($"📤 Отсортированный массив: [{string.Join(", ", result.SortedArray)}]");
        }
        
        if (_displaySettings.ShowGap)
        {
            Console.WriteLine($"🔢 Шаг отбрасывания: {result.Gap}");
        }
        
        if (_displaySettings.ShowExecutionTime)
        {
            Console.WriteLine($"⏱️  Время выполнения: {result.ExecutionTimeMs} мс");
        }
        
        if (_displaySettings.ShowCompletionTime)
        {
            Console.WriteLine($"📅 Дата и время завершения: {result.CompletionTime:yyyy-MM-dd HH:mm:ss}");
        }
        
        Console.WriteLine("═══════════════════════════════════════════════════════════");
    }

    static async Task SaveToLogFile(SortResponse result)
    {
        try
        {
            var logEntry = new LogEntry
            {
                Timestamp = result.CompletionTime,
                OriginalArray = result.OriginalArray,
                SortedArray = result.SortedArray,
                Gap = result.Gap,
                ExecutionTimeMs = result.ExecutionTimeMs,
                Ascending = result.Ascending
            };

            var logPath = Path.Combine(LogsDirectory, $"sort_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });
            
            // Шифруем данные перед сохранением
            var encryptedJson = EncryptionModule.Encrypt(json);
            await File.WriteAllTextAsync(logPath, encryptedJson);
            
            Console.WriteLine($"✅ Результат сохранен в файл: {logPath}");
        }
        catch (Exception ex)
        {
            LogError($"Ошибка сохранения лога: {ex.Message}", ex);
            Console.WriteLine($"❌ Ошибка сохранения лога: {ex.Message}");
        }
    }

    static async Task ViewLogs()
    {
        Console.WriteLine("📊 ПРОСМОТР ЛОГОВ");
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("Выберите источник логов:");
        Console.WriteLine("1. Локальные логи (сохраненные в файлы)");
        Console.WriteLine("2. Логи с сервера (через API)");
        Console.Write("Выбор: ");
        
        var sourceChoice = Console.ReadLine()?.Trim();
        Console.WriteLine();

        if (sourceChoice == "1")
        {
            await ViewLocalLogs();
        }
        else if (sourceChoice == "2")
        {
            await ViewServerLogs();
        }
        else
        {
            Console.WriteLine("❌ Неверный выбор.");
        }
    }

    static async Task ViewLocalLogs()
    {
        var logFiles = Directory.GetFiles(LogsDirectory, "sort_*.json").OrderByDescending(f => f).ToList();
        
        if (logFiles.Count == 0)
        {
            Console.WriteLine("📭 Локальные логи не найдены.");
            return;
        }

        Console.WriteLine($"Найдено локальных логов: {logFiles.Count}");
        Console.WriteLine("\nСписок логов:");
        for (int i = 0; i < logFiles.Count; i++)
        {
            var fileName = Path.GetFileName(logFiles[i]);
            Console.WriteLine($"{i + 1}. {fileName}");
        }

        Console.Write("\nВыберите номер лога для просмотра (0 - все, Enter - выход): ");
        var choice = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(choice))
        {
            return;
        }

        if (choice == "0")
        {
            foreach (var logFile in logFiles)
            {
                await DisplayLogFile(logFile);
            }
        }
        else if (int.TryParse(choice, out var index) && index > 0 && index <= logFiles.Count)
        {
            await DisplayLogFile(logFiles[index - 1]);
        }
        else
        {
            Console.WriteLine("❌ Неверный выбор.");
        }
    }

    static async Task ViewServerLogs()
    {
        try
        {
            Console.WriteLine("Фильтры для просмотра логов:");
            Console.Write("Начальная дата (yyyy-MM-dd, Enter - пропустить): ");
            var fromInput = Console.ReadLine()?.Trim();
            DateTime? from = null;
            if (!string.IsNullOrEmpty(fromInput) && DateTime.TryParse(fromInput, out var fromDate))
            {
                from = fromDate;
            }

            Console.Write("Конечная дата (yyyy-MM-dd, Enter - пропустить): ");
            var toInput = Console.ReadLine()?.Trim();
            DateTime? to = null;
            if (!string.IsNullOrEmpty(toInput) && DateTime.TryParse(toInput, out var toDate))
            {
                to = toDate;
            }

            Console.Write("Уровень лога (INFO, WARNING, ERROR, Enter - все): ");
            var levelInput = Console.ReadLine()?.Trim();

            var url = "/api/logs?";
            if (from.HasValue)
                url += $"from={from.Value:yyyy-MM-ddTHH:mm:ssZ}&";
            if (to.HasValue)
                url += $"to={to.Value:yyyy-MM-ddTHH:mm:ssZ}&";
            if (!string.IsNullOrEmpty(levelInput))
                url += $"level={levelInput}&";
            
            url = url.TrimEnd('&', '?');

            var response = await _httpClientModule!.ExecuteWithRetryAsync(() =>
                new HttpRequestMessage(HttpMethod.Get, url));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                if (!TryParseJsonElement(responseContent, out var json) ||
                    !json.TryGetProperty("Count", out var cntProp))
                {
                    Console.WriteLine("❌ Ответ сервера пуст или некорректен.");
                    return;
                }

                var count = cntProp.GetInt32();
                var logs = json.GetProperty("Logs");

                Console.WriteLine($"\nНайдено логов на сервере: {count}");
                Console.WriteLine("───────────────────────────────────────────────────────────");

                foreach (var log in logs.EnumerateArray())
                {
                    var timestamp = log.GetProperty("Timestamp").GetDateTime();
                    var level = log.GetProperty("Level").GetString();
                    var message = log.GetProperty("Message").GetString();
                    var userId = log.TryGetProperty("UserId", out var uid) ? uid.GetString() : "unknown";

                    Console.WriteLine($"Дата и время: {timestamp:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"Уровень: {level}");
                    Console.WriteLine($"Пользователь: {userId}");
                    Console.WriteLine($"Сообщение: {message}");

                    if (log.TryGetProperty("InputArray", out var inputArray) && inputArray.ValueKind != JsonValueKind.Null)
                    {
                        var input = inputArray.EnumerateArray().Select(x => x.GetInt32()).ToArray();
                        Console.WriteLine($"Исходный массив: [{string.Join(", ", input)}]");
                    }

                    if (log.TryGetProperty("OutputArray", out var outputArray) && outputArray.ValueKind != JsonValueKind.Null)
                    {
                        var output = outputArray.EnumerateArray().Select(x => x.GetInt32()).ToArray();
                        Console.WriteLine($"Отсортированный массив: [{string.Join(", ", output)}]");
                    }

                    Console.WriteLine("───────────────────────────────────────────────────────────");
                }
            }
            else
            {
                Console.WriteLine($"❌ Ошибка получения логов (HTTP {(int)response.StatusCode}): {DescribeResponseText(responseContent)}");
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка при получении логов с сервера: {ex.Message}", ex);
            Console.WriteLine($"❌ Ошибка подключения к серверу: {ex.Message}");
        }
    }

    static async Task DisplayLogFile(string filePath)
    {
        try
        {
            var encryptedContent = await File.ReadAllTextAsync(filePath);
            var decryptedContent = EncryptionModule.Decrypt(encryptedContent);
            var logEntry = JsonSerializer.Deserialize<LogEntry>(decryptedContent);

            if (logEntry != null)
            {
                Console.WriteLine("\n───────────────────────────────────────────────────────────");
                Console.WriteLine($"Файл: {Path.GetFileName(filePath)}");
                Console.WriteLine($"Дата и время: {logEntry.Timestamp:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Исходный массив: [{string.Join(", ", logEntry.OriginalArray)}]");
                Console.WriteLine($"Отсортированный массив: [{string.Join(", ", logEntry.SortedArray)}]");
                Console.WriteLine($"Шаг отбрасывания: {logEntry.Gap}");
                Console.WriteLine($"Время выполнения: {logEntry.ExecutionTimeMs} мс");
                Console.WriteLine($"Направление: {(logEntry.Ascending ? "по возрастанию" : "по убыванию")}");
                Console.WriteLine("───────────────────────────────────────────────────────────");
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка чтения лога: {ex.Message}", ex);
            Console.WriteLine($"❌ Ошибка чтения лога: {ex.Message}");
        }
    }

    static Task ManageSettings()
    {
        Console.WriteLine("⚙️  НАСТРОЙКИ ВЫВОДА");
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("Выберите поля, которые нужно скрыть в результате сортировки:");
        Console.WriteLine();
        
        Console.WriteLine($"1. Исходный массив: {(_displaySettings.ShowOriginalArray ? "✅" : "❌")}");
        Console.WriteLine($"2. Отсортированный массив: {(_displaySettings.ShowSortedArray ? "✅" : "❌")}");
        Console.WriteLine($"3. Шаг отбрасывания: {(_displaySettings.ShowGap ? "✅" : "❌")}");
        Console.WriteLine($"4. Время выполнения: {(_displaySettings.ShowExecutionTime ? "✅" : "❌")}");
        Console.WriteLine($"5. Дата и время завершения: {(_displaySettings.ShowCompletionTime ? "✅" : "❌")}");
        Console.WriteLine();
        Console.WriteLine("Введите номера полей для переключения (через пробел), или Enter для выхода:");
        Console.Write("Выбор: ");
        
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return Task.CompletedTask;
        }

        var choices = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var choice in choices)
        {
            switch (choice)
            {
                case "1":
                    _displaySettings.ShowOriginalArray = !_displaySettings.ShowOriginalArray;
                    break;
                case "2":
                    _displaySettings.ShowSortedArray = !_displaySettings.ShowSortedArray;
                    break;
                case "3":
                    _displaySettings.ShowGap = !_displaySettings.ShowGap;
                    break;
                case "4":
                    _displaySettings.ShowExecutionTime = !_displaySettings.ShowExecutionTime;
                    break;
                case "5":
                    _displaySettings.ShowCompletionTime = !_displaySettings.ShowCompletionTime;
                    break;
            }
        }

        SaveSettings();
        Console.WriteLine("✅ Настройки сохранены.");
        return Task.CompletedTask;
    }

    static void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _displaySettings = JsonSerializer.Deserialize<DisplaySettings>(json) ?? new DisplaySettings();
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка загрузки настроек: {ex.Message}", ex);
        }
    }

    static void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_displaySettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            LogError($"Ошибка сохранения настроек: {ex.Message}", ex);
        }
    }

    static string ReadPassword()
    {
        var password = new StringBuilder();
        ConsoleKeyInfo key;
        
        do
        {
            key = Console.ReadKey(true);
            
            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Remove(password.Length - 1, 1);
                Console.Write("\b \b");
            }
        }
        while (key.Key != ConsoleKey.Enter);
        
        Console.WriteLine();
        return password.ToString();
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
            // Игнорируем ошибки логирования
        }
    }
}

// Модели данных
public class SortResponse
{
    public int[] OriginalArray { get; set; } = new int[0];
    public int[] SortedArray { get; set; } = new int[0];
    public bool Ascending { get; set; }
    public int Gap { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime CompletionTime { get; set; }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public int[] OriginalArray { get; set; } = new int[0];
    public int[] SortedArray { get; set; } = new int[0];
    public int Gap { get; set; }
    public long ExecutionTimeMs { get; set; }
    public bool Ascending { get; set; }
}

public class DisplaySettings
{
    public bool ShowOriginalArray { get; set; } = true;
    public bool ShowSortedArray { get; set; } = true;
    public bool ShowGap { get; set; } = true;
    public bool ShowExecutionTime { get; set; } = true;
    public bool ShowCompletionTime { get; set; } = true;
}
