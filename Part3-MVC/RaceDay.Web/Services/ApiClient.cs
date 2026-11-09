using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string Error { get; init; } = string.Empty;
    public HttpStatusCode StatusCode { get; init; }
}

/// <summary>
/// The only way the website talks to the API. It keeps each user's API session cookie
/// in that user's own web session and sends it with every request.
/// </summary>
public class ApiClient
{
    private const string CookieKey = "ApiCookie";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) =>
        SendAsync<T>(HttpMethod.Get, url, null);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Post, url, ToJson(body));

    public Task<ApiResult<T>> PutAsync<T>(string url, object body) =>
        SendAsync<T>(HttpMethod.Put, url, ToJson(body));

    public Task<ApiResult<bool>> DeleteAsync(string url) =>
        SendAsync<bool>(HttpMethod.Delete, url, null);

    public async Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(fileContent, "file", file.FileName);
        return await SendAsync<T>(HttpMethod.Post, url, form);
    }

    private static StringContent? ToJson(object? body) =>
        body is null
            ? null
            : new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
    {
        var session = _accessor.HttpContext!.Session;
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            var cookie = session.GetString(CookieKey);
            if (!string.IsNullOrEmpty(cookie))
                request.Headers.Add("Cookie", cookie);

            using var response = await _http.SendAsync(request);

            // After login the API sends back its session cookie: remember it for this user only.
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                var sessionCookie = setCookies.FirstOrDefault(c => c.StartsWith("RaceDay.Session="));
                if (sessionCookie is not null)
                    session.SetString(CookieKey, sessionCookie.Split(';')[0]);
            }

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<T>
                {
                    Success = false,
                    StatusCode = response.StatusCode,
                    Error = BuildError(response.StatusCode, body)
                };
            }

            T? data = default;
            if (typeof(T) == typeof(bool))
                data = (T)(object)true;
            else if (!string.IsNullOrWhiteSpace(body))
                data = JsonSerializer.Deserialize<T>(body, JsonOptions);

            return new ApiResult<T> { Success = true, Data = data, StatusCode = response.StatusCode };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Error = "The RaceDay service could not be reached. Please try again in a moment."
            };
        }
    }

    /// <summary>Turns an API error into a short, friendly message (never raw error text).</summary>
    private static string BuildError(HttpStatusCode status, string body)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = errors.EnumerateObject()
                            .SelectMany(p => p.Value.EnumerateArray().Select(m => m.GetString()))
                            .Where(m => !string.IsNullOrWhiteSpace(m));
                        var joined = string.Join(" ", messages);
                        if (joined.Length > 0) return joined;
                    }

                    if (status is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Conflict
                        && root.TryGetProperty("title", out var title)
                        && title.ValueKind == JsonValueKind.String)
                    {
                        return title.GetString() ?? string.Empty;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // fall through to the friendly default below
        }

        return status switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "We could not find what you were looking for.",
            _ => "Something went wrong. Please try again."
        };
    }
}