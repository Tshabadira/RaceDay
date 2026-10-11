using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Category
{
    public int CategoryID { get; set; }

    public int EventID { get; set; }
    public Event Event { get; set; } = null!;

    [MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public decimal DistanceKM { get; set; }

    [MaxLength(20)]
    public string EventType { get; set; } = string.Empty;

    public decimal EntryFee { get; set; }

    public int MaxParticipants { get; set; }

    // One category has at most one route (one-to-one, and the route may not exist yet)
    public Route? Route { get; set; }

    // Navigation property - uncomment once Enrolment exists
     public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}