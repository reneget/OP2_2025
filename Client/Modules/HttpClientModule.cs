using System.Net;

namespace Client.Modules;

public class HttpClientModule : IDisposable
{
    private readonly HttpClient _httpClient;

    public HttpClientModule(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public HttpResponseMessage Execute(Func<HttpRequestMessage> requestFactory)
    {
        using var request = requestFactory();
        return _httpClient.Send(request);
    }
    
    public static string ReadContent(HttpResponseMessage response)
    {
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
    public void SetAuthCookie(string cookie)
    {
        _httpClient.DefaultRequestHeaders.Remove("Cookie");

        if (!string.IsNullOrEmpty(cookie))
            _httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
    }

    public void ClearAuthCookie()
    {
        _httpClient.DefaultRequestHeaders.Remove("Cookie");
    }

    public static string? ExtractCookie(HttpResponseMessage response)
    {
        if (!response.Headers.Contains("Set-Cookie"))
            return null;

        return response.Headers.GetValues("Set-Cookie").FirstOrDefault(c => c.Contains(".AspNetCore.Cookies"))
            ?.Split(';')[0];
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}