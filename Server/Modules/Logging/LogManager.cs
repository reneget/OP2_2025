using System.Text;

namespace Server.Modules.Logging;

/// <summary>
/// Менеджер логирования с хранением на сервере
/// </summary>
public class LogManager
{
    private readonly string _logDirectory;
    private readonly object _lockObject = new object();
    private const string LogFileNameFormat = "app_{0:yyyy-MM-dd}.log";

    public LogManager(string logDirectory = "./logs")
    {
        _logDirectory = logDirectory;
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    /// <summary>
    /// Записывает лог в файл
    /// </summary>
    public void Log(LogLevel level, string message, string? userId = null)
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            UserId = userId
        };

        WriteLogToFile(logEntry);
    }

    /// <summary>
    /// Получает логи за указанный период
    /// </summary>
    public List<LogEntry> GetLogs(DateTime? from = null, DateTime? to = null, LogLevel? level = null, string? userId = null)
    {
        var logs = new List<LogEntry>();
        var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
        var toDate = to ?? DateTime.UtcNow;

        lock (_lockObject)
        {
            var currentDate = fromDate.Date;
            while (currentDate <= toDate.Date)
            {
                var logFile = Path.Combine(_logDirectory, string.Format(LogFileNameFormat, currentDate));
                if (File.Exists(logFile))
                {
                    var fileLogs = ReadLogsFromFile(logFile);
                    logs.AddRange(fileLogs);
                }
                currentDate = currentDate.AddDays(1);
            }
        }

        // Фильтрация
        var filteredLogs = logs.Where(log =>
            log.Timestamp >= fromDate &&
            log.Timestamp <= toDate &&
            (level == null || log.Level == level) &&
            (userId == null || log.UserId == userId)
        ).OrderByDescending(log => log.Timestamp).ToList();

        return filteredLogs;
    }

    private void WriteLogToFile(LogEntry entry)
    {
        var logFile = Path.Combine(_logDirectory, string.Format(LogFileNameFormat, entry.Timestamp.Date));

        lock (_lockObject)
        {
            var logLine = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC [{entry.Level}] " +
                         $"{(entry.UserId != null ? $"[User: {entry.UserId}] " : "")}" +
                         $"{entry.Message}{Environment.NewLine}";

            File.AppendAllText(logFile, logLine, Encoding.UTF8);
        }
    }

    private List<LogEntry> ReadLogsFromFile(string filePath)
    {
        var logs = new List<LogEntry>();

        if (!File.Exists(filePath))
            return logs;

        var lines = File.ReadAllLines(filePath);
        foreach (var line in lines)
        {
            var entry = ParseLogLine(line);
            if (entry != null)
            {
                logs.Add(entry);
            }
        }

        return logs;
    }

    private LogEntry? ParseLogLine(string line)
    {
        try
        {
            // Формат: 2024-01-01 12:00:00.000 UTC [INFO] [User: username] message
            var parts = line.Split(new[] { " UTC " }, StringSplitOptions.None);
            if (parts.Length != 2)
                return null;

            var timestampPart = parts[0];
            var rest = parts[1];

            if (!DateTime.TryParse(timestampPart, out var timestamp))
                return null;

            var levelMatch = System.Text.RegularExpressions.Regex.Match(rest, @"^\[(INFO|WARNING|ERROR|DEBUG)\]");
            if (!levelMatch.Success)
                return null;

            var levelStr = levelMatch.Groups[1].Value;
            var level = Enum.Parse<LogLevel>(levelStr);

            var userIdMatch = System.Text.RegularExpressions.Regex.Match(rest, @"\[User: ([^\]]+)\]");
            string? userId = userIdMatch.Success ? userIdMatch.Groups[1].Value : null;

            var messageStart = userIdMatch.Success ? userIdMatch.Index + userIdMatch.Length : levelMatch.Index + levelMatch.Length;
            var message = rest.Substring(messageStart).Trim();

            return new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = message,
                UserId = userId
            };
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Уровень логирования
/// </summary>
public enum LogLevel
{
    DEBUG,
    INFO,
    WARNING,
    ERROR
}

/// <summary>
/// Запись в логе
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
}

