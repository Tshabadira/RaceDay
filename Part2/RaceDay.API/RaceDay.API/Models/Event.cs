using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Event
{
    public int EventID { get; set; }

    public int OrganiserID { get; set; }
    public User Organiser { get; set; } = null!;

    [MaxLength(100)]
    public string EventName { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    [MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [MaxLength(50)]
    public string Province { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Upcoming";

    // Navigation property - uncomment once Category exists
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}