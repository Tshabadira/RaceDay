using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api)
    {
        _api = api;
    }

    // Home page: all upcoming events, with filters
    public async Task<IActionResult> Index(string? province, string? type, string? search)
    {
        var response = await _api.GetEventsAsync(status: "Upcoming");
        var all = response.Success ? response.Data ?? new List<EventDto>() : new List<EventDto>();

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(province))
        {
            filtered = filtered.Where(e => e.Province == province);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            filtered = filtered.Where(e => e.Categories.Any(c => c.EventType == type));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(e =>
                e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var model = new HomeIndexViewModel
        {
            Events = filtered.ToList(),
            Provinces = all.Select(e => e.Province).Distinct().OrderBy(p => p).ToList(),
            Province = province,
            Type = type,
            Search = search,
            LoadError = response.Success ? null : response.Error
        };

        return View(model);
    }

    // Full information about one event: categories, route, weather, directions
    public async Task<IActionResult> Details(int id)
    {
        var response = await _api.GetEventAsync(id);
        if (!response.Success || response.Data == null)
        {
            if (response.Status == HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            TempData["Error"] = response.Error;
            return RedirectToAction(nameof(Index));
        }

        var model = new EventDetailsViewModel { Event = response.Data };

        var weather = await _api.GetWeatherAsync(id);
        if (weather.Success)
        {
            model.Weather = weather.Data;
        }

        foreach (var category in response.Data.Categories)
        {
            var route = await _api.GetRouteAsync(category.CategoryID);
            if (route.Success && route.Data != null)
            {
                model.Routes[category.CategoryID] = route.Data;
            }
        }

        return View(model);
    }

    public IActionResult Status(int code)
    {
        ViewData["Code"] = code;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
