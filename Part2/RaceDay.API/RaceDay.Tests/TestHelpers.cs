using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.API.Dtos;

namespace RaceDay.Tests;

public record TestUser(HttpClient Client, string Email);

public static class TestHelpers
{
    public const string Password = "Passw0rd!23";

    /// <summary>Registers a brand new user, logs them in and returns a client that sends their token.</summary>
    public static async Task<TestUser> CreateUserAsync(ApiFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = role,
            email,
            password = Password,
            role,
            phoneNumber = "0821234567"
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);

        return new TestUser(client, email);
    }

    public static object EventPayload(string name = "Test Marathon") => new
    {
        eventName = name,
        eventDate = "2027-03-08",
        location = "Green Point, Cape Town",
        latitude = -33.9065,
        longitude = 18.4109,
        province = "Western Cape",
        description = "A test event"
    };

    public static object CategoryPayload(int maxParticipants = 100) => new
    {
        categoryName = "10km Run",
        distanceKM = 10,
        eventType = "run",
        entryFee = 120,
        maxParticipants
    };

    public static async Task<int> CreateEventAsync(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", EventPayload());
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<EventResponse>();
        return created!.EventID;
    }

    public static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryPayload(maxParticipants));
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        return created!.CategoryID;
    }

    public static async Task<EnrolmentResponse> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        return created!;
    }
}
