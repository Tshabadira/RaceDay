using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize(Roles = "Participant")]
public class ParticipantController : Controller
{
    private readonly ApiClient _api;

    public ParticipantController(ApiClient api)
    {
        _api = api;
    }

    // Enter an event by choosing a category
    [HttpPost]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        var result = await _api.EnrolAsync(categoryId);

        if (result.Success)
        {
            TempData["Success"] = "You are entered. Your enrolment is shown below.";
            return RedirectToAction(nameof(Enrolments));
        }

        TempData["Error"] = result.Error;
        return RedirectToAction("Details", "Home", new { id = eventId });
    }

    // My enrolments and their status
    [HttpGet]
    public async Task<IActionResult> Enrolments()
    {
        var response = await _api.GetMyEnrolmentsAsync();
        if (!response.Success)
        {
            TempData["Error"] = response.Error;
        }

        return View(response.Data ?? new List<EnrolmentDto>());
    }

    // My race history
    [HttpGet]
    public async Task<IActionResult> Results()
    {
        var response = await _api.GetMyResultsAsync();
        if (!response.Success)
        {
            TempData["Error"] = response.Error;
        }

        return View(response.Data ?? new List<ResultDto>());
    }
}
