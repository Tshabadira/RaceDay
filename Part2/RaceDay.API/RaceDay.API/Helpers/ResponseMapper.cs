using RaceDay.API.Dtos;
using RaceDay.API.Models;
using Route = RaceDay.API.Models.Route;

namespace RaceDay.API.Helpers;

/// <summary>
/// Copies database objects into the "receipt" classes (DTOs) that the API sends back.
/// Nothing secret, such as PasswordHash, is ever copied.
/// </summary>
public static class ResponseMapper
{
    public static UserResponse ToResponse(this User user) => new()
    {
        UserID = user.UserID,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role,
        PhoneNumber = user.PhoneNumber,
        CreatedAt = user.CreatedAt
    };

    public static CategoryResponse ToResponse(this Category category) => new()
    {
        CategoryID = category.CategoryID,
        EventID = category.EventID,
        CategoryName = category.CategoryName,
        DistanceKM = category.DistanceKM,
        EventType = category.EventType,
        EntryFee = category.EntryFee,
        MaxParticipants = category.MaxParticipants
    };

    public static RouteResponse ToResponse(this Route route) => new()
    {
        RouteID = route.RouteID,
        CategoryID = route.CategoryID,
        RouteName = route.RouteName,
        ElevationGainM = route.ElevationGainM,
        StartPoint = route.StartPoint,
        EndPoint = route.EndPoint,
        MapURL = route.MapURL
    };

    public static EventResponse ToResponse(this Event ev) => new()
    {
        EventID = ev.EventID,
        OrganiserID = ev.OrganiserID,
        EventName = ev.EventName,
        EventDate = ev.EventDate,
        Location = ev.Location,
        Latitude = ev.Latitude,
        Longitude = ev.Longitude,
        Province = ev.Province,
        Description = ev.Description,
        Status = ev.Status,
        Categories = ev.Categories.OrderBy(c => c.DistanceKM).Select(c => c.ToResponse()).ToList()
    };

    public static EnrolmentResponse ToResponse(this Enrolment enrolment) => new()
    {
        EnrolmentID = enrolment.EnrolmentID,
        ParticipantID = enrolment.ParticipantID,
        ParticipantName = enrolment.Participant == null
            ? null
            : $"{enrolment.Participant.FirstName} {enrolment.Participant.LastName}",
        CategoryID = enrolment.CategoryID,
        CategoryName = enrolment.Category?.CategoryName ?? string.Empty,
        EventID = enrolment.Category?.EventID ?? 0,
        EventName = enrolment.Category?.Event?.EventName ?? string.Empty,
        EventDate = enrolment.Category?.Event?.EventDate ?? default,
        RaceNumber = enrolment.RaceNumber,
        EnrolmentDate = enrolment.EnrolmentDate,
        PaymentStatus = enrolment.PaymentStatus
    };

    public static ResultResponse ToResponse(this Result result) => new()
    {
        ResultID = result.ResultID,
        EnrolmentID = result.EnrolmentID,
        ParticipantName = result.Enrolment?.Participant == null
            ? null
            : $"{result.Enrolment.Participant.FirstName} {result.Enrolment.Participant.LastName}",
        RaceNumber = result.Enrolment?.RaceNumber ?? string.Empty,
        EventName = result.Enrolment?.Category?.Event?.EventName ?? string.Empty,
        CategoryName = result.Enrolment?.Category?.CategoryName ?? string.Empty,
        FinishTime = result.FinishTime,
        OverallPosition = result.OverallPosition,
        CategoryPosition = result.CategoryPosition,
        Status = result.Status
    };
}
