using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Route
{
    public int RouteID { get; set; }

    public int CategoryID { get; set; }
    public Category Category { get; set; } = null!;

    [MaxLength(100)]
    public string RouteName { get; set; } = string.Empty;

    public decimal? ElevationGainM { get; set; }

    [MaxLength(150)]
    public string StartPoint { get; set; } = string.Empty;

    [MaxLength(150)]
    public string EndPoint { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? MapURL { get; set; }
}