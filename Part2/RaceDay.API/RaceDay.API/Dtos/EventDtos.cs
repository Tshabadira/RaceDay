using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos;

/// <summary>Used to create and to update an event.</summary>
public class EventRequest
{
    [Required, MaxLength(100)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public DateOnly? EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    [Required, MaxLength(50)]
    public string Province { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [RegularExpression("^(Upcoming|Completed|Cancelled)$",
        ErrorMessage = "Status must be Upcoming, Completed or Cancelled.")]
    public string? Status { get; set; }
}

public class EventResponse
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
    public List<CategoryResponse> Categories { get; set; } = new();
}

/// <summary>Used to create and to update a category.</summary>
public class CategoryRequest
{
    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 999.99)]
    public decimal DistanceKM { get; set; }

    [Required]
    [RegularExpression("^(run|walk|cycle)$", ErrorMessage = "Event type must be run, walk or cycle.")]
    public string EventType { get; set; } = string.Empty;

    [Range(0, 999999.99)]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000)]
    public int MaxParticipants { get; set; }
}

public class CategoryResponse
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public string EventType { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
}

public class RouteResponse
{
    public int RouteID { get; set; }
    public int CategoryID { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal? ElevationGainM { get; set; }
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public string? MapURL { get; set; }
}
