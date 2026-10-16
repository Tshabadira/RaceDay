using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;
using Xunit;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object RegisterPayload(string email, string role = "Participant") => new
    {
        firstName = "Lerato",
        lastName = "Sithole",
        email,
        password = TestHelpers.Password,
        role,
        phoneNumber = "0715551234"
    };

    [Fact]
    public async Task Register_WithValidDetails_Returns201_AndNeverReturnsThePassword()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", RegisterPayload($"{Guid.NewGuid():N}@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Participant", text);
    }

    [Fact]
    public async Task Register_WithAnEmailThatIsAlreadyUsed_Returns400()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";

        await client.PostAsJsonAsync("/api/auth/register", RegisterPayload(email));
        var second = await client.PostAsJsonAsync("/api/auth/register", RegisterPayload(email));

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithAnInvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            RegisterPayload($"{Guid.NewGuid():N}@example.com", "Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAShortPassword_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Lerato",
            lastName = "Sithole",
            email = $"{Guid.NewGuid():N}@example.com",
            password = "short",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectDetails_Returns200_AndAToken()
    {
        var user = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = user.Email, password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Organiser", body.User.Role);
    }

    [Fact]
    public async Task Login_WithAWrongPassword_Returns401()
    {
        var user = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = user.Email, password = "WrongPassword!1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithAnEmailThatDoesNotExist_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@example.com", password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutAToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithAToken_ReturnsTheLoggedInUsersOwnProfile()
    {
        var user = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await user.Client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(user.Email, profile!.Email);
        Assert.Equal("Participant", profile.Role);
    }

    [Fact]
    public async Task Profile_CanBeUpdated_ButNotToAnEmailAnotherUserHas()
    {
        var first = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var second = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var ok = await first.Client.PutAsJsonAsync("/api/profile", new
        {
            firstName = "Changed",
            lastName = "Name",
            email = first.Email,
            phoneNumber = "0829998888"
        });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var updated = await ok.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Changed", updated!.FirstName);

        var clash = await first.Client.PutAsJsonAsync("/api/profile", new
        {
            firstName = "Changed",
            lastName = "Name",
            email = second.Email
        });
        Assert.Equal(HttpStatusCode.BadRequest, clash.StatusCode);
    }

    [Fact]
    public async Task Logout_WithAToken_Returns200_AndWithoutOneReturns401()
    {
        var user = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var loggedIn = await user.Client.PostAsync("/api/auth/logout", null);
        var anonymous = await _factory.CreateClient().PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }
}
