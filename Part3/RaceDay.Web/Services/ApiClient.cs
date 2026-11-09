using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public HttpStatusCode Status { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// The ONLY class in this website that talks to the outside world: it sends requests to the RaceDay API.
/// The website never touches the database or Azure directly.
/// </summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    // ---------------- low level helpers ----------------

    private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string url, object? body = null, HttpContent? content = null)
    {
        try
        {
            if (body != null)
            {
                content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
            }

            using var request = new HttpRequestMessage(method, url) { Content = content };

            // Send the logged-in user's token (the wristband) with every request
            var token = _accessor.HttpContext?.Session.GetString(SessionKeys.Token);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await _http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                T? data = default;
                if (!string.IsNullOrWhiteSpace(text) && typeof(T) != typeof(object))
                {
                    data = JsonSerializer.Deserialize<T>(text, JsonOptions);
                }

                return new ApiResponse<T> { Success = true, Status = response.StatusCode, Data = data };
            }

            return new ApiResponse<T> { Success = false, Status = response.StatusCode, Error = ReadError(response.StatusCode, text) };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Status = HttpStatusCode.ServiceUnavailable,
                Error = "The RaceDay service is not available right now. Please try again in a moment."
            };
        }
    }

    private async Task<ApiResponse<T>> SendFileAsync<T>(string url, IFormFile file)
    {
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();

        var fileContent = new StreamContent(stream);
        if (MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType))
        {
            fileContent.Headers.ContentType = mediaType;
        }

        form.Add(fileContent, "file", file.FileName);
        return await SendAsync<T>(HttpMethod.Post, url, content: form);
    }

    // Turns an API error into one friendly sentence. Raw errors are never shown to the user.
    private static string ReadError(HttpStatusCode status, string body)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    {
                        return message.GetString() ?? FallbackMessage(status);
                    }

                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = new List<string>();
                        foreach (var field in errors.EnumerateObject())
                        {
                            if (field.Value.ValueKind != JsonValueKind.Array)
                            {
                                continue;
                            }

                            foreach (var item in field.Value.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String && item.GetString() is { } text)
                                {
                                    messages.Add(text);
                                }
                            }
                        }

                        if (messages.Count > 0)
                        {
                            return string.Join(" ", messages);
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // fall through to the standard message
        }

        return FallbackMessage(status);
    }

    private static string FallbackMessage(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Please log in again.",
        HttpStatusCode.Forbidden => "You do not have permission to do that.",
        HttpStatusCode.NotFound => "We could not find what you were looking for.",
        HttpStatusCode.Conflict => "That action conflicts with existing data.",
        HttpStatusCode.BadRequest => "Some of the information was not valid. Please check it and try again.",
        _ => "Something went wrong. Please try again."
    };

    // ---------------- authentication ----------------

    public Task<ApiResponse<LoginDto>> LoginAsync(string email, string password) =>
        SendAsync<LoginDto>(HttpMethod.Post, "api/auth/login", new { email, password });

    public Task<ApiResponse<UserDto>> RegisterAsync(RegisterViewModel m) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/auth/register", new
        {
            firstName = m.FirstName,
            lastName = m.LastName,
            email = m.Email,
            password = m.Password,
            role = m.Role,
            phoneNumber = m.PhoneNumber
        });

    public Task<ApiResponse<object>> LogoutAsync() =>
        SendAsync<object>(HttpMethod.Post, "api/auth/logout");

    // ---------------- profile ----------------

    public Task<ApiResponse<UserDto>> GetProfileAsync() =>
        SendAsync<UserDto>(HttpMethod.Get, "api/profile");

    public Task<ApiResponse<UserDto>> UpdateProfileAsync(ProfileViewModel m) =>
        SendAsync<UserDto>(HttpMethod.Put, "api/profile", new
        {
            firstName = m.FirstName,
            lastName = m.LastName,
            email = m.Email,
            phoneNumber = m.PhoneNumber
        });

    public Task<ApiResponse<UserDto>> UploadProfilePictureAsync(IFormFile file) =>
        SendFileAsync<UserDto>("api/profile/picture", file);

    // ---------------- events ----------------

    public Task<ApiResponse<List<EventDto>>> GetEventsAsync(string? province = null, string? status = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(province))
        {
            query.Add("province=" + Uri.EscapeDataString(province));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add("status=" + Uri.EscapeDataString(status));
        }

        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        return SendAsync<List<EventDto>>(HttpMethod.Get, url);
    }

    public Task<ApiResponse<EventDto>> GetEventAsync(int id) =>
        SendAsync<EventDto>(HttpMethod.Get, $"api/events/{id}");

    public Task<ApiResponse<List<OrganiserEventDto>>> GetMyEventsAsync() =>
        SendAsync<List<OrganiserEventDto>>(HttpMethod.Get, "api/events/mine");

    public Task<ApiResponse<EventDto>> CreateEventAsync(EventFormViewModel m) =>
        SendAsync<EventDto>(HttpMethod.Post, "api/events", EventBody(m));

    public Task<ApiResponse<EventDto>> UpdateEventAsync(int id, EventFormViewModel m) =>
        SendAsync<EventDto>(HttpMethod.Put, $"api/events/{id}", EventBody(m));

    public Task<ApiResponse<object>> DeleteEventAsync(int id) =>
        SendAsync<object>(HttpMethod.Delete, $"api/events/{id}");

    public Task<ApiResponse<EventDto>> UploadBannerAsync(int id, IFormFile file) =>
        SendFileAsync<EventDto>($"api/events/{id}/banner", file);

    public Task<ApiResponse<WeatherDto>> GetWeatherAsync(int id) =>
        SendAsync<WeatherDto>(HttpMethod.Get, $"api/events/{id}/weather");

    private static object EventBody(EventFormViewModel m) => new
    {
        eventName = m.EventName,
        eventDate = m.EventDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        location = m.Location,
        latitude = m.Latitude,
        longitude = m.Longitude,
        province = m.Province,
        description = m.Description,
        status = string.IsNullOrWhiteSpace(m.Status) ? null : m.Status
    };

    // ---------------- categories ----------------

    public Task<ApiResponse<CategoryDto>> CreateCategoryAsync(int eventId, CategoryFormViewModel m) =>
        SendAsync<CategoryDto>(HttpMethod.Post, $"api/events/{eventId}/categories", CategoryBody(m));

    public Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(int id, CategoryFormViewModel m) =>
        SendAsync<CategoryDto>(HttpMethod.Put, $"api/categories/{id}", CategoryBody(m));

    public Task<ApiResponse<object>> DeleteCategoryAsync(int id) =>
        SendAsync<object>(HttpMethod.Delete, $"api/categories/{id}");

    public Task<ApiResponse<RouteDto>> GetRouteAsync(int categoryId) =>
        SendAsync<RouteDto>(HttpMethod.Get, $"api/categories/{categoryId}/route");

    private static object CategoryBody(CategoryFormViewModel m) => new
    {
        categoryName = m.CategoryName,
        distanceKM = m.DistanceKM,
        eventType = m.EventType,
        entryFee = m.EntryFee,
        maxParticipants = m.MaxParticipants
    };

    // ---------------- enrolments ----------------

    public Task<ApiResponse<EnrolmentDto>> EnrolAsync(int categoryId) =>
        SendAsync<EnrolmentDto>(HttpMethod.Post, $"api/categories/{categoryId}/enrolments");

    public Task<ApiResponse<List<EnrolmentDto>>> GetMyEnrolmentsAsync() =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/enrolments/my");

    public Task<ApiResponse<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    public Task<ApiResponse<EnrolmentDto>> SetPaymentStatusAsync(int enrolmentId, string status) =>
        SendAsync<EnrolmentDto>(HttpMethod.Put, $"api/enrolments/{enrolmentId}/payment", new { paymentStatus = status });

    // ---------------- results ----------------

    public Task<ApiResponse<List<ResultDto>>> GetMyResultsAsync() =>
        SendAsync<List<ResultDto>>(HttpMethod.Get, "api/results/my");

    public Task<ApiResponse<List<ResultDto>>> GetEventResultsAsync(int eventId) =>
        SendAsync<List<ResultDto>>(HttpMethod.Get, $"api/events/{eventId}/results");

    public Task<ApiResponse<ResultDto>> CreateResultAsync(object body) =>
        SendAsync<ResultDto>(HttpMethod.Post, "api/results", body);

    public Task<ApiResponse<ResultDto>> UpdateResultAsync(int id, object body) =>
        SendAsync<ResultDto>(HttpMethod.Put, $"api/results/{id}", body);
}
