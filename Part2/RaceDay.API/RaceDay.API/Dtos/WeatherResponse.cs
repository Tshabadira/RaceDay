namespace RaceDay.API.Dtos;

public class WeatherResponse
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public double TemperatureC { get; set; }
    public double FeelsLikeC { get; set; }
    public double HumidityPercent { get; set; }
    public double WindSpeedKmh { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public string? ObservedAt { get; set; }
    public string Source { get; set; } = "Open-Meteo (https://open-meteo.com)";
}
