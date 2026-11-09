namespace RaceDay.Web.Models;

// These classes match the JSON that the RaceDay API sends back.

public class UserDto
{
    public int UserID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LoginDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public string EventType { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
}

public class EventDto
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string Province { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}

public class OrganiserEventDto
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }
}

public class RouteDto
{
    public int RouteID { get; set; }
    public int CategoryID { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal? ElevationGainM { get; set; }
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public string? MapURL { get; set; }
}

public class WeatherDto
{
    public double TemperatureC { get; set; }
    public double FeelsLikeC { get; set; }
    public double HumidityPercent { get; set; }
    public double WindSpeedKmh { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public string? ObservedAt { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string? ParticipantName { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string RaceNumber { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

public class ResultDto
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public string? ParticipantName { get; set; }
    public string RaceNumber { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = string.Empty;
    public int EventID { get; set; }
    public DateOnly EventDate { get; set; }
    public int TotalFinishers { get; set; }
    public int CategoryFinishers { get; set; }
}
