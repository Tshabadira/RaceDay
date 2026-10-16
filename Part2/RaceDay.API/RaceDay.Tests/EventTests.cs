using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;
using Xunit;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EventTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organiser_CanCreateAnEvent()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/events", TestHelpers.EventPayload());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Upcoming", created!.Status);
    }

    [Fact]
    public async Task Participant_CannotCreateAnEvent_Returns403()
    {
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/events", TestHelpers.EventPayload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousUser_CannotCreateAnEvent_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/events", TestHelpers.EventPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anyone_CanBrowseTheEventList()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        await TestHelpers.CreateEventAsync(organiser.Client);

        var response = await _factory.CreateClient().GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.NotEmpty(events!);
    }

    [Fact]
    public async Task GettingAnEventThatDoesNotExist_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Event_WithAMissingName_Returns400()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            eventDate = "2027-03-08",
            location = "Somewhere",
            province = "Gauteng"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateTheirOwnEvent()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);

        var response = await organiser.Client.PutAsJsonAsync($"/api/events/{eventId}",
            TestHelpers.EventPayload("Renamed Marathon"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Renamed Marathon", updated!.EventName);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var otherOrganiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner.Client);

        var response = await otherOrganiser.Client.PutAsJsonAsync($"/api/events/{eventId}",
            TestHelpers.EventPayload("Stolen Marathon"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDeleteAnEvent_Returns403()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);

        var response = await participant.Client.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteTheirOwnEvent_ThenItIsGone()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);

        var delete = await organiser.Client.DeleteAsync($"/api/events/{eventId}");
        var get = await _factory.CreateClient().GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnEventThatStillHasCategories_Returns409()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);

        var response = await organiser.Client.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanAddACategory_AndAnyoneCanListIt()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);

        var create = await organiser.Client.PostAsJsonAsync($"/api/events/{eventId}/categories",
            TestHelpers.CategoryPayload());
        var list = await _factory.CreateClient().GetAsync($"/api/events/{eventId}/categories");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var categories = await list.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        Assert.Single(categories!);
        Assert.Equal("run", categories![0].EventType);
    }

    [Fact]
    public async Task Participant_CannotAddACategory_Returns403()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);

        var response = await participant.Client.PostAsJsonAsync($"/api/events/{eventId}/categories",
            TestHelpers.CategoryPayload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Category_WithAnInvalidEventType_Returns400()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);

        var response = await organiser.Client.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Swim",
            distanceKM = 2,
            eventType = "swim",
            entryFee = 100,
            maxParticipants = 50
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Weather_ForAnEventThatDoesNotExist_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events/999999/weather");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Weather_ForAnEventWithoutCoordinates_Returns400()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var create = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            eventName = "No Coordinates Run",
            eventDate = "2027-05-01",
            location = "Unknown",
            province = "Gauteng"
        });
        var created = await create.Content.ReadFromJsonAsync<EventResponse>();

        var response = await _factory.CreateClient().GetAsync($"/api/events/{created!.EventID}/weather");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
