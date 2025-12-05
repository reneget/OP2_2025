using Microsoft.Data.Sqlite;
using Server.Modules.Database;

namespace Server.Modules.Logging;

public class LogManager
{
    private readonly DBManager _dbManager;

    public LogManager(DBManager dbManager)
    {
        _dbManager = dbManager;
    }

    public void Log(LogLevel level, string message, string? userId = null)
    {
        var connection = _dbManager.GetConnection();
        if (connection == null)
            return;

        var query = @"INSERT INTO logs (Timestamp, Level, Message, UserId) 
                     VALUES (@timestamp, @level, @message, @userId)";

        try
        {
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
            command.Parameters.AddWithValue("@level", level.ToString());
            command.Parameters.AddWithValue("@message", message);
            command.Parameters.AddWithValue("@userId", userId ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing log: {ex.Message}");
        }
    }

    public void LogSortOperation(string message, int[] inputArray, int[] outputArray, string? userId = null)
    {
        var connection = _dbManager.GetConnection();
        if (connection == null)
            return;

        var inputStr = string.Join(",", inputArray);
        var outputStr = string.Join(",", outputArray);
        var fullMessage = $"{message} | Input: [{string.Join(", ", inputArray)}] | Output: [{string.Join(", ", outputArray)}]";

        var query = @"INSERT INTO logs (Timestamp, Level, Message, UserId, InputArray, OutputArray) 
                     VALUES (@timestamp, @level, @message, @userId, @inputArray, @outputArray)";

        try
        {
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
            command.Parameters.AddWithValue("@level", LogLevel.INFO.ToString());
            command.Parameters.AddWithValue("@message", fullMessage);
            command.Parameters.AddWithValue("@userId", userId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@inputArray", inputStr);
            command.Parameters.AddWithValue("@outputArray", outputStr);
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing sort log: {ex.Message}");
        }
    }

    public List<LogEntry> GetLogs(DateTime? from = null, DateTime? to = null, LogLevel? level = null, string? userId = null)
    {
        var logs = new List<LogEntry>();
        var connection = _dbManager.GetConnection();
        if (connection == null)
            return logs;

        var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
        var toDate = to ?? DateTime.UtcNow;

        var query = @"SELECT Timestamp, Level, Message, UserId, InputArray, OutputArray 
                     FROM logs 
                     WHERE Timestamp >= @from AND Timestamp <= @to";

        var parameters = new List<SqliteParameter>
        {
            new SqliteParameter("@from", fromDate),
            new SqliteParameter("@to", toDate)
        };

        if (level != null)
        {
            query += " AND Level = @level";
            parameters.Add(new SqliteParameter("@level", level.ToString()));
        }

        if (userId != null)
        {
            query += " AND UserId = @userId";
            parameters.Add(new SqliteParameter("@userId", userId));
        }

        query += " ORDER BY Timestamp DESC";

        try
        {
            using var command = new SqliteCommand(query, connection);
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var entry = new LogEntry
                {
                    Timestamp = reader.GetDateTime(0),
                    Level = Enum.Parse<LogLevel>(reader.GetString(1)),
                    Message = reader.GetString(2),
                    UserId = reader.IsDBNull(3) ? null : reader.GetString(3)
                };

                if (!reader.IsDBNull(4))
                {
                    var inputStr = reader.GetString(4);
                    if (!string.IsNullOrEmpty(inputStr))
                    {
                        entry.InputArray = inputStr.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
                    }
                }

                if (!reader.IsDBNull(5))
                {
                    var outputStr = reader.GetString(5);
                    if (!string.IsNullOrEmpty(outputStr))
                    {
                        entry.OutputArray = outputStr.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
                    }
                }

                logs.Add(entry);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading logs: {ex.Message}");
        }

        return logs;
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

