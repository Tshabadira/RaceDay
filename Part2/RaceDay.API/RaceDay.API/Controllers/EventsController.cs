using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public EventsController(RaceDayContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Lists all events, optionally filtered by province or status.
    /// </summary>
    /// <remarks>Open to everyone. Events are ordered by date and include their categories.</remarks>
    /// <param name="province">Optional. Only events in this province, for example Gauteng.</param>
    /// <param name="status">Optional. Upcoming, Completed or Cancelled.</param>
    /// <response code="200">The list of events (it can be empty).</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventResponse>>> GetEvents(string? province, string? status)
    {
        IQueryable<Event> query = _context.Events.Include(e => e.Categories);

        if (!string.IsNullOrWhiteSpace(province))
        {
            query = query.Where(e => e.Province == province);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => e.ToResponse()).ToList());
    }

    /// <summary>
    /// Returns one event, including its categories.
    /// </summary>
    /// <remarks>Open to everyone.</remarks>
    /// <param name="id">The event's ID.</param>
    /// <response code="200">The event.</response>
    /// <response code="404">No event has this ID.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEvent(int id)
    {
        var ev = await _context.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(ev.ToResponse());
    }

    /// <summary>
    /// Creates a new event owned by the logged-in Organiser.
    /// </summary>
    /// <remarks>Organiser only. The owner is taken from the token. Distance and event type belong to the event's categories.</remarks>
    /// <response code="201">The new event.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not an Organiser.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> CreateEvent(EventRequest request)
    {
        var ev = new Event
        {
            OrganiserID = User.GetUserId(),
            EventName = request.EventName.Trim(),
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Province = request.Province.Trim(),
            Description = request.Description,
            Status = request.Status ?? "Upcoming"
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, ev.ToResponse());
    }

    /// <summary>
    /// Updates an event.
    /// </summary>
    /// <remarks>Organiser only, and only for events that Organiser created.</remarks>
    /// <param name="id">The event's ID.</param>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has this ID.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> UpdateEvent(int id, EventRequest request)
    {
        var ev = await _context.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only change events that you organise." });
        }

        ev.EventName = request.EventName.Trim();
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.Latitude = request.Latitude;
        ev.Longitude = request.Longitude;
        ev.Province = request.Province.Trim();
        ev.Description = request.Description;

        if (request.Status != null)
        {
            ev.Status = request.Status;
        }

        await _context.SaveChangesAsync();
        return Ok(ev.ToResponse());
    }

    /// <summary>
    /// Deletes an event.
    /// </summary>
    /// <remarks>Organiser only, and only for events that Organiser created. Delete the event's categories first.</remarks>
    /// <param name="id">The event's ID.</param>
    /// <response code="200">The event was deleted.</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has this ID.</response>
    /// <response code="409">The event still has categories.</response>
    [Authorize(Roles = "Organiser")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete events that you organise." });
        }

        var hasCategories = await _context.Categories.AnyAsync(c => c.EventID == id);
        if (hasCategories)
        {
            return Conflict(new { message = "Delete this event's categories first." });
        }

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Event deleted." });
    }

    /// <summary>
    /// Returns the live weather at the event's location, to help a Participant prepare for race day.
    /// </summary>
    /// <remarks>
    /// Open to everyone. The API calls the free Open-Meteo service using the event's latitude and longitude,
    /// so no weather key ever reaches the front end.
    /// </remarks>
    /// <param name="id">The event's ID.</param>
    /// <response code="200">Current temperature, feels-like temperature, humidity, wind speed and conditions.</response>
    /// <response code="400">The event has no coordinates.</response>
    /// <response code="404">No event has this ID.</response>
    /// <response code="502">The weather service could not be reached.</response>
    [HttpGet("{id:int}/weather")]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<WeatherResponse>> GetWeather(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.Latitude == null || ev.Longitude == null)
        {
            return BadRequest(new { message = "This event has no coordinates, so its weather cannot be looked up." });
        }

        // InvariantCulture keeps the decimal point as "." (South African settings would use a comma)
        var latitude = ev.Latitude.Value.ToString(CultureInfo.InvariantCulture);
        var longitude = ev.Longitude.Value.ToString(CultureInfo.InvariantCulture);
        var url = "https://api.open-meteo.com/v1/forecast"
                  + $"?latitude={latitude}&longitude={longitude}"
                  + "&current=temperature_2m,apparent_temperature,relative_humidity_2m,wind_speed_10m,weather_code";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "The weather service did not answer correctly. Try again later." });
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<OpenMeteoResponse>(json);
            if (data?.Current == null)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "The weather service sent data we could not read." });
            }

            return Ok(new WeatherResponse
            {
                EventID = ev.EventID,
                EventName = ev.EventName,
                Latitude = ev.Latitude.Value,
                Longitude = ev.Longitude.Value,
                TemperatureC = data.Current.Temperature,
                FeelsLikeC = data.Current.ApparentTemperature,
                HumidityPercent = data.Current.RelativeHumidity,
                WindSpeedKmh = data.Current.WindSpeed,
                Conditions = DescribeWeather(data.Current.WeatherCode),
                ObservedAt = data.Current.Time
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "The weather service could not be reached. Try again later." });
        }
    }

    // Turns the weather service's numeric code into words (WMO weather codes)
    private static string DescribeWeather(int code) => code switch
    {
        0 => "Clear sky",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        >= 51 and <= 57 => "Drizzle",
        >= 61 and <= 67 => "Rain",
        >= 71 and <= 77 => "Snow",
        >= 80 and <= 82 => "Rain showers",
        85 or 86 => "Snow showers",
        >= 95 and <= 99 => "Thunderstorm",
        _ => "Unknown conditions"
    };

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current")]
        public OpenMeteoCurrent? Current { get; set; }
    }

    private sealed class OpenMeteoCurrent
    {
        [JsonPropertyName("time")]
        public string? Time { get; set; }

        [JsonPropertyName("temperature_2m")]
        public double Temperature { get; set; }

        [JsonPropertyName("apparent_temperature")]
        public double ApparentTemperature { get; set; }

        [JsonPropertyName("relative_humidity_2m")]
        public double RelativeHumidity { get; set; }

        [JsonPropertyName("wind_speed_10m")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }
    }
}
