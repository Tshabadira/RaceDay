using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;
using Xunit;

namespace RaceDay.Tests;

public class EnrolmentAndResultTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EnrolmentAndResultTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- Enrolments ----------

    [Fact]
    public async Task Participant_CanEnrol_AndTheEnrolmentIsRecorded()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);

        var response = await participant.Client.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(categoryId, enrolment!.CategoryID);
        Assert.Equal(eventId, enrolment.EventID);
        Assert.False(string.IsNullOrWhiteSpace(enrolment.RaceNumber));

        var mine = await participant.Client.GetAsync("/api/enrolments/my");
        var list = await mine.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Single(list!);
        Assert.Equal(enrolment.EnrolmentID, list![0].EnrolmentID);
    }

    [Fact]
    public async Task Participant_CannotEnrolInTheSameCategoryTwice_Returns409()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);

        await TestHelpers.EnrolAsync(participant.Client, categoryId);
        var second = await participant.Client.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);

        var response = await organiser.Client.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousUser_CannotEnrol_Returns401()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);

        var response = await _factory.CreateClient().PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrolling_InACategoryThatDoesNotExist_Returns404()
    {
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.PostAsync("/api/categories/999999/enrolments", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrolling_InAFullCategory_Returns400()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var first = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var second = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId, maxParticipants: 1);

        await TestHelpers.EnrolAsync(first.Client, categoryId);
        var response = await second.Client.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanSeeTheEnrolmentsForTheirEvent()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);
        await TestHelpers.EnrolAsync(participant.Client, categoryId);

        var response = await organiser.Client.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Single(list!);
        Assert.False(string.IsNullOrWhiteSpace(list![0].ParticipantName));
    }

    [Fact]
    public async Task Participant_CannotSeeAnEventsEnrolmentList_Returns403()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);

        var response = await participant.Client.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotSeeAnotherOrganisersEnrolments_Returns403()
    {
        var owner = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var otherOrganiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner.Client);

        var response = await otherOrganiser.Client.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Results ----------

    [Fact]
    public async Task Organiser_CanCaptureAResult_AndTheParticipantSeesItInTheirHistory()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, categoryId);

        var capture = await organiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = enrolment.EnrolmentID,
            finishTime = "01:52:10",
            overallPosition = 47,
            categoryPosition = 10,
            status = "Finished"
        });
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var mine = await participant.Client.GetAsync("/api/results/my");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var results = await mine.Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.Single(results!);
        Assert.Equal(47, results![0].OverallPosition);
        Assert.Equal(new TimeSpan(1, 52, 10), results[0].FinishTime);
    }

    [Fact]
    public async Task Participant_CannotCaptureAResult_Returns403()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, categoryId);

        var response = await participant.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = enrolment.EnrolmentID,
            finishTime = "00:45:00",
            overallPosition = 1,
            status = "Finished"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureAResultForAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var otherOrganiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(owner.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(owner.Client, eventId);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, categoryId);

        var response = await otherOrganiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = enrolment.EnrolmentID,
            finishTime = "00:45:00",
            status = "Finished"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CapturingASecondResultForTheSameEnrolment_Returns409()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, categoryId);
        var body = new { enrolmentId = enrolment.EnrolmentID, finishTime = "00:50:00", status = "Finished" };

        await organiser.Client.PostAsJsonAsync("/api/results", body);
        var second = await organiser.Client.PostAsJsonAsync("/api/results", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task AFinishedRunnerWithoutAFinishTime_Returns400()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, categoryId);

        var response = await organiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = enrolment.EnrolmentID,
            status = "Finished"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCorrectAResult()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);
        var enrolment = await TestHelpers.EnrolAsync(participant.Client, categoryId);

        var capture = await organiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = enrolment.EnrolmentID,
            finishTime = "00:50:00",
            overallPosition = 5,
            status = "Finished"
        });
        var created = await capture.Content.ReadFromJsonAsync<ResultResponse>();

        var update = await organiser.Client.PutAsJsonAsync($"/api/results/{created!.ResultID}", new
        {
            finishTime = "00:49:30",
            overallPosition = 4,
            status = "Finished"
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal(4, updated!.OverallPosition);
    }

    [Fact]
    public async Task Participant_CannotSeeAnotherParticipantsResultsInMyResults()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var runner = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var someoneElse = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser.Client);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser.Client, eventId);
        var enrolment = await TestHelpers.EnrolAsync(runner.Client, categoryId);
        await organiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = enrolment.EnrolmentID,
            finishTime = "00:50:00",
            status = "Finished"
        });

        var response = await someoneElse.Client.GetAsync("/api/results/my");

        var results = await response.Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.Empty(results!);
    }

    [Fact]
    public async Task Organiser_CannotViewMyResults_Returns403()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.GetAsync("/api/results/my");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
