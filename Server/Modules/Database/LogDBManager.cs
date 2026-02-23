using Microsoft.Data.Sqlite;
using Server.Modules.Logging;

namespace Server.Modules.Database;

/// <summary>
/// Менеджер базы данных для логов
/// </summary>
public class LogDBManager
{
    private SqliteConnection? _connection = null;

    /// <summary>
    /// Подключение к базе данных логов
    /// </summary>
    /// <param name="path">Путь к файлу базы данных</param>
    /// <returns>true если подключение успешно, иначе false</returns>
    public bool ConnectToDB(string path)
    {
        Console.WriteLine("Connecting to logs database...");

        try
        {
            // Создаём директорию для БД, если её нет
            var dbDirectory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

            _connection = new SqliteConnection("Data Source=" + path);
            _connection.Open();

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                Console.WriteLine("Failed to open logs database!");
                return false;
            }

            // Создаём таблицу логов, если её нет
            InitializeDatabase();
        }
        catch (Exception exp)
        {
            Console.WriteLine($"Logs database connection error: {exp.Message}");
            return false;
        }

        Console.WriteLine("Logs database connected successfully!");
        return true;
    }

    /// <summary>
    /// Инициализация структуры базы данных
    /// </summary>
    private void InitializeDatabase()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            return;

        var createTableQuery = @"
            CREATE TABLE IF NOT EXISTS logs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp DATETIME NOT NULL,
                Level TEXT NOT NULL,
                Message TEXT NOT NULL,
                UserId TEXT,
                InputArray TEXT,
                OutputArray TEXT,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS idx_logs_timestamp ON logs(Timestamp);
            CREATE INDEX IF NOT EXISTS idx_logs_level ON logs(Level);
            CREATE INDEX IF NOT EXISTS idx_logs_userid ON logs(UserId);
        ";

        using var command = new SqliteCommand(createTableQuery, _connection);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Отключение от базы данных
    /// </summary>
    public void Disconnect()
    {
        if (_connection == null)
            return;

        if (_connection.State != System.Data.ConnectionState.Open)
            return;

        _connection.Close();
        _connection.Dispose();
        _connection = null;

        Console.WriteLine("Disconnected from logs database");
    }

    /// <summary>
    /// Добавление записи лога в базу данных
    /// </summary>
    /// <param name="entry">Запись лога</param>
    /// <returns>true если запись успешно добавлена, иначе false</returns>
    public bool AddLog(LogEntry entry)
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            return false;

        var query = @"
            INSERT INTO logs (Timestamp, Level, Message, UserId, InputArray, OutputArray)
            VALUES (@timestamp, @level, @message, @userId, @inputArray, @outputArray)";

        try
        {
            using var command = new SqliteCommand(query, _connection);
            command.Parameters.AddWithValue("@timestamp", entry.Timestamp);
            command.Parameters.AddWithValue("@level", entry.Level.ToString());
            command.Parameters.AddWithValue("@message", entry.Message);
            command.Parameters.AddWithValue("@userId", entry.UserId ?? (object)DBNull.Value);
            
            // Сохраняем массивы как JSON строки
            command.Parameters.AddWithValue("@inputArray", 
                entry.InputArray != null ? string.Join(",", entry.InputArray) : (object)DBNull.Value);
            command.Parameters.AddWithValue("@outputArray", 
                entry.OutputArray != null ? string.Join(",", entry.OutputArray) : (object)DBNull.Value);

            var result = command.ExecuteNonQuery();
            return result == 1;
        }
        catch (Exception exp)
        {
            Console.WriteLine($"Error adding log: {exp.Message}");
            return false;
        }
    }

    /// <summary>
    /// Получение логов с фильтрацией
    /// </summary>
    /// <param name="from">Начальная дата (опционально)</param>
    /// <param name="to">Конечная дата (опционально)</param>
    /// <param name="level">Уровень логирования (опционально)</param>
    /// <param name="userId">ID пользователя (опционально)</param>
    /// <returns>Список записей логов</returns>
    public List<LogEntry> GetLogs(DateTime? from = null, DateTime? to = null, Server.Modules.Logging.LogLevel? level = null, string? userId = null)
    {
        var logs = new List<LogEntry>();

        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            return logs;

        var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
        var toDate = to ?? DateTime.UtcNow;

        var query = @"
            SELECT Timestamp, Level, Message, UserId, InputArray, OutputArray
            FROM logs
            WHERE Timestamp >= @fromDate AND Timestamp <= @toDate";

        var parameters = new List<SqliteParameter>
        {
            new SqliteParameter("@fromDate", fromDate),
            new SqliteParameter("@toDate", toDate)
        };

        if (level != null)
        {
            query += " AND Level = @level";
            parameters.Add(new SqliteParameter("@level", level.ToString()));
        }

        if (!string.IsNullOrEmpty(userId))
        {
            query += " AND UserId = @userId";
            parameters.Add(new SqliteParameter("@userId", userId));
        }

        query += " ORDER BY Timestamp DESC";

        try
        {
            using var command = new SqliteCommand(query, _connection);
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
                    Level = Enum.Parse<Server.Modules.Logging.LogLevel>(reader.GetString(1)),
                    Message = reader.GetString(2),
                    UserId = reader.IsDBNull(3) ? null : reader.GetString(3)
                };

                // Парсим массивы из строк
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
        catch (Exception exp)
        {
            Console.WriteLine($"Error retrieving logs: {exp.Message}");
        }

        return logs;
    }

    /// <summary>
    /// Удаление старых логов (очистка базы данных)
    /// </summary>
    /// <param name="olderThan">Удалить логи старше указанной даты</param>
    /// <returns>Количество удалённых записей</returns>
    public int DeleteOldLogs(DateTime olderThan)
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            return 0;

        var query = "DELETE FROM logs WHERE Timestamp < @olderThan";

        try
        {
            using var command = new SqliteCommand(query, _connection);
            command.Parameters.AddWithValue("@olderThan", olderThan);
            return command.ExecuteNonQuery();
        }
        catch (Exception exp)
        {
            Console.WriteLine($"Error deleting old logs: {exp.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Получение количества записей в базе данных
    /// </summary>
    /// <returns>Количество записей</returns>
    public int GetLogCount()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            return 0;

        var query = "SELECT COUNT(*) FROM logs";

        try
        {
            using var command = new SqliteCommand(query, _connection);
            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch (Exception exp)
        {
            Console.WriteLine($"Error getting log count: {exp.Message}");
            return 0;
        }
    }
}

