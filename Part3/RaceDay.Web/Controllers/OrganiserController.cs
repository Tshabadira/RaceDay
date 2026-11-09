using System.Globalization;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize(Roles = "Organiser")]
public class OrganiserController : Controller
{
    private readonly ApiClient _api;

    public OrganiserController(ApiClient api)
    {
        _api = api;
    }

    private int CurrentUserId =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    // Loads an event and checks that it belongs to the logged-in Organiser
    private async Task<(EventDto? Event, IActionResult? Failure)> LoadOwnedEventAsync(int eventId)
    {
        var response = await _api.GetEventAsync(eventId);
        if (!response.Success || response.Data == null)
        {
            if (response.Status == HttpStatusCode.NotFound)
            {
                return (null, NotFound());
            }

            TempData["Error"] = response.Error;
            return (null, RedirectToAction(nameof(Events)));
        }

        if (response.Data.OrganiserID != CurrentUserId)
        {
            return (null, Forbid());
        }

        return (response.Data, null);
    }

    // ---------------- dashboard ----------------

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var response = await _api.GetMyEventsAsync();
        var events = response.Data ?? new List<OrganiserEventDto>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var model = new OrganiserDashboardViewModel
        {
            Events = events,
            TotalEnrolments = events.Sum(e => e.EnrolmentCount),
            UpcomingEvents = events
                .Where(e => e.EventDate >= today && e.Status == "Upcoming")
                .OrderBy(e => e.EventDate)
                .ToList(),
            LoadError = response.Success ? null : response.Error
        };

        return View(model);
    }

    // ---------------- events ----------------

    [HttpGet]
    public async Task<IActionResult> Events()
    {
        var response = await _api.GetMyEventsAsync();
        if (!response.Success)
        {
            TempData["Error"] = response.Error;
        }

        return View(response.Data ?? new List<OrganiserEventDto>());
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        return View("EventForm", new EventFormViewModel { Status = "Upcoming" });
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        var result = await _api.CreateEventAsync(model);
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not create the event.");
            return View("EventForm", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var banner = await _api.UploadBannerAsync(result.Data.EventID, model.Banner);
            if (!banner.Success)
            {
                TempData["Error"] = "The event was created, but the banner could not be uploaded: " + banner.Error;
                return RedirectToAction(nameof(Events));
            }
        }

        TempData["Success"] = "The event has been created. Now add its categories.";
        return RedirectToAction(nameof(Categories), new { eventId = result.Data.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var (ev, failure) = await LoadOwnedEventAsync(id);
        if (ev == null)
        {
            return failure!;
        }

        return View("EventForm", new EventFormViewModel
        {
            EventID = ev.EventID,
            EventName = ev.EventName,
            EventDate = ev.EventDate.ToDateTime(TimeOnly.MinValue),
            Location = ev.Location,
            Latitude = ev.Latitude,
            Longitude = ev.Longitude,
            Province = ev.Province,
            Description = ev.Description,
            Status = ev.Status,
            ExistingBannerUrl = ev.BannerImageUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(EventFormViewModel model)
    {
        if (model.EventID == null)
        {
            return RedirectToAction(nameof(Events));
        }

        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        var result = await _api.UpdateEventAsync(model.EventID.Value, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not save the event.");
            return View("EventForm", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var banner = await _api.UploadBannerAsync(model.EventID.Value, model.Banner);
            if (!banner.Success)
            {
                TempData["Error"] = "The event was saved, but the banner could not be uploaded: " + banner.Error;
                return RedirectToAction(nameof(Events));
            }
        }

        TempData["Success"] = "The event has been saved.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.DeleteEventAsync(id);
        if (result.Success)
        {
            TempData["Success"] = "The event has been deleted.";
        }
        else
        {
            TempData["Error"] = result.Status == HttpStatusCode.Conflict
                ? "This event still has categories. Delete its categories first."
                : result.Error;
        }

        return RedirectToAction(nameof(Events));
    }

    // ---------------- categories ----------------

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var (ev, failure) = await LoadOwnedEventAsync(eventId);
        if (ev == null)
        {
            return failure!;
        }

        return View(new CategoriesViewModel
        {
            Event = ev,
            NewCategory = new CategoryFormViewModel { EventID = eventId }
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Categories), new { eventId = model.EventID });
        }

        var result = await _api.CreateCategoryAsync(model.EventID, model);
        if (result.Success)
        {
            TempData["Success"] = "The category has been added.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Categories), new { eventId = model.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id, int eventId)
    {
        var (ev, failure) = await LoadOwnedEventAsync(eventId);
        if (ev == null)
        {
            return failure!;
        }

        var category = ev.Categories.FirstOrDefault(c => c.CategoryID == id);
        if (category == null)
        {
            return NotFound();
        }

        return View(new CategoryFormViewModel
        {
            CategoryID = category.CategoryID,
            EventID = eventId,
            CategoryName = category.CategoryName,
            DistanceKM = category.DistanceKM,
            EventType = category.EventType,
            EntryFee = category.EntryFee,
            MaxParticipants = category.MaxParticipants
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditCategory(CategoryFormViewModel model)
    {
        if (model.CategoryID == null)
        {
            return RedirectToAction(nameof(Categories), new { eventId = model.EventID });
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.UpdateCategoryAsync(model.CategoryID.Value, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not save the category.");
            return View(model);
        }

        TempData["Success"] = "The category has been saved.";
        return RedirectToAction(nameof(Categories), new { eventId = model.EventID });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var result = await _api.DeleteCategoryAsync(id);
        if (result.Success)
        {
            TempData["Success"] = "The category has been deleted.";
        }
        else
        {
            TempData["Error"] = result.Status == HttpStatusCode.Conflict
                ? "This category already has enrolments, so it cannot be deleted."
                : result.Error;
        }

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------------- enrolments ----------------

    [HttpGet]
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var (ev, failure) = await LoadOwnedEventAsync(eventId);
        if (ev == null)
        {
            return failure!;
        }

        var response = await _api.GetEventEnrolmentsAsync(eventId);
        if (!response.Success)
        {
            TempData["Error"] = response.Error;
        }

        return View(new EventEnrolmentsViewModel
        {
            Event = ev,
            Enrolments = response.Data ?? new List<EnrolmentDto>()
        });
    }

    [HttpPost]
    public async Task<IActionResult> SetPayment(int enrolmentId, int eventId, string status)
    {
        var result = await _api.SetPaymentStatusAsync(enrolmentId, status);
        if (result.Success)
        {
            TempData["Success"] = status == "Paid" ? "The enrolment is now confirmed." : "The enrolment is back to pending.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    // ---------------- results ----------------

    [HttpGet]
    public async Task<IActionResult> Results(int eventId)
    {
        var (ev, failure) = await LoadOwnedEventAsync(eventId);
        if (ev == null)
        {
            return failure!;
        }

        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);
        var results = await _api.GetEventResultsAsync(eventId);

        if (!enrolments.Success)
        {
            TempData["Error"] = enrolments.Error;
        }

        return View(new EventResultsViewModel
        {
            Event = ev,
            Enrolments = enrolments.Data ?? new List<EnrolmentDto>(),
            ResultsByEnrolment = (results.Data ?? new List<ResultDto>()).ToDictionary(r => r.EnrolmentID)
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(ResultFormViewModel model)
    {
        TimeSpan? finishTime = null;
        if (!string.IsNullOrWhiteSpace(model.FinishTime))
        {
            if (!TimeSpan.TryParse(model.FinishTime, CultureInfo.InvariantCulture, out var parsed))
            {
                TempData["Error"] = "The finish time must look like 01:52:10 (hours:minutes:seconds).";
                return RedirectToAction(nameof(Results), new { eventId = model.EventID });
            }

            finishTime = parsed;
        }

        ApiResponse<ResultDto> result;
        if (model.ResultID.HasValue)
        {
            result = await _api.UpdateResultAsync(model.ResultID.Value, new
            {
                finishTime = finishTime?.ToString("c", CultureInfo.InvariantCulture),
                overallPosition = model.OverallPosition,
                categoryPosition = model.CategoryPosition,
                status = model.Status
            });
        }
        else
        {
            result = await _api.CreateResultAsync(new
            {
                enrolmentId = model.EnrolmentID,
                finishTime = finishTime?.ToString("c", CultureInfo.InvariantCulture),
                overallPosition = model.OverallPosition,
                categoryPosition = model.CategoryPosition,
                status = model.Status
            });
        }

        if (result.Success)
        {
            TempData["Success"] = "The result has been saved and is now visible to the participant.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Results), new { eventId = model.EventID });
    }
}
