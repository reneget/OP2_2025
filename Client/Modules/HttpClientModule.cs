using System.Net;
using System.Text;
using System.Text.Json;

namespace Client.Modules;

/// <summary>
/// Модуль для работы с HTTP запросами с поддержкой повторных попыток
/// </summary>
public class HttpClientModule
{
    private readonly HttpClient _httpClient;
    private readonly int _maxRetries;
    private readonly int _retryDelayMs;

    public HttpClientModule(string baseUrl, int maxRetries = 3, int retryDelayMs = 1000)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _maxRetries = maxRetries;
        _retryDelayMs = retryDelayMs;
    }

    /// <summary>
    /// Выполняет HTTP запрос с повторными попытками
    /// </summary>
    public async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;
        HttpResponseMessage? lastResponse = null;

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                // Создаем новый запрос на каждую попытку, чтобы избежать повторного использования
                using var request = requestFactory();
                var response = await _httpClient.SendAsync(request, cancellationToken);
                lastResponse = response;

                // Если успешный ответ или ошибка, которая не требует повтора
                if (response.IsSuccessStatusCode ||
                    response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.NotFound ||
                    response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return response;
                }

                // Для других ошибок пробуем повторить
                if (attempt < _maxRetries)
                {
                    await Task.Delay(_retryDelayMs * attempt, cancellationToken);
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                if (attempt < _maxRetries)
                {
                    await Task.Delay(_retryDelayMs * attempt, cancellationToken);
                }
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;
                if (attempt < _maxRetries)
                {
                    await Task.Delay(_retryDelayMs * attempt, cancellationToken);
                }
            }
        }

        // Если есть ответ сервера, возвращаем его, чтобы вызвать понятное сообщение об ошибке
        if (lastResponse != null)
        {
            return lastResponse;
        }

        throw lastException ?? new Exception("Не удалось выполнить запрос после всех попыток");
    }

    /// <summary>
    /// Устанавливает cookie для авторизации
    /// </summary>
    public void SetAuthCookie(string cookie)
    {
        _httpClient.DefaultRequestHeaders.Remove("Cookie");
        if (!string.IsNullOrEmpty(cookie))
        {
            _httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
        }
    }

    /// <summary>
    /// Очищает cookie авторизации
    /// </summary>
    public void ClearAuthCookie()
    {
        _httpClient.DefaultRequestHeaders.Remove("Cookie");
    }

    /// <summary>
    /// Извлекает cookie из ответа
    /// </summary>
    public static string? ExtractCookie(HttpResponseMessage response)
    {
        if (response.Headers.Contains("Set-Cookie"))
        {
            var cookies = response.Headers.GetValues("Set-Cookie");
            foreach (var cookie in cookies)
            {
                if (cookie.Contains(".AspNetCore.Cookies"))
                {
                    return cookie.Split(';')[0];
                }
            }
        }
        return null;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

