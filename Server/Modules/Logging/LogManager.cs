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

    public void LogSortOperation(string message, int[] inputArray, int[] outputArray, string? userId = null)
    {
        var inputStr = string.Join(", ", inputArray);
        var outputStr = string.Join(", ", outputArray);
        var fullMessage = $"{message} | Input: [{inputStr}] | Output: [{outputStr}]";

        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.INFO,
            Message = fullMessage,
            UserId = userId,
            InputArray = inputArray,
            OutputArray = outputArray
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
            
            // Массивы записываются на отдельной строке для удобного парсинга
            if (entry.InputArray != null && entry.OutputArray != null)
            {
                var arrayLine = $"  Arrays: Input=[{string.Join(", ", entry.InputArray)}] Output=[{string.Join(", ", entry.OutputArray)}]{Environment.NewLine}";
                File.AppendAllText(logFile, arrayLine, Encoding.UTF8);
            }
        }
    }

    private List<LogEntry> ReadLogsFromFile(string filePath)
    {
        var logs = new List<LogEntry>();

        if (!File.Exists(filePath))
            return logs;

        var lines = File.ReadAllLines(filePath);
        for (int i = 0; i < lines.Length; i++)
        {
            var entry = ParseLogLine(lines[i]);
            if (entry != null)
            {
                // Проверяем, есть ли следующая строка с массивами
                if (i + 1 < lines.Length && lines[i + 1].Trim().StartsWith("Arrays:"))
                {
                    ParseArraysFromLine(lines[i + 1], entry);
                    i++;
                }
                logs.Add(entry);
            }
        }

        return logs;
    }

    private void ParseArraysFromLine(string line, LogEntry entry)
    {
        try
        {
            var inputMatch = System.Text.RegularExpressions.Regex.Match(line, @"Input=\[([^\]]+)\]");
            var outputMatch = System.Text.RegularExpressions.Regex.Match(line, @"Output=\[([^\]]+)\]");

            if (inputMatch.Success)
            {
                var inputStr = inputMatch.Groups[1].Value;
                entry.InputArray = inputStr.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
            }

            if (outputMatch.Success)
            {
                var outputStr = outputMatch.Groups[1].Value;
                entry.OutputArray = outputStr.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
            }
        }
        catch
        {
        }
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

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int[]? InputArray { get; set; }
    public int[]? OutputArray { get; set; }
}

