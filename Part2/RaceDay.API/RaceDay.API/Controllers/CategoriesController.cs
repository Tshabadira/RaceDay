using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayContext _context;

    public CategoriesController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists the categories (distances) of one event.
    /// </summary>
    /// <remarks>Open to everyone. Ordered from the shortest distance to the longest.</remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <response code="200">The categories (it can be empty).</response>
    /// <response code="404">No event has this ID.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategoriesForEvent(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.EventID == eventId);
        if (!eventExists)
        {
            return NotFound(new { message = "Event not found." });
        }

        var categories = await _context.Categories
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.DistanceKM)
            .ToListAsync();

        return Ok(categories.Select(c => c.ToResponse()).ToList());
    }

    /// <summary>
    /// Adds a category (for example 10km run) to an event.
    /// </summary>
    /// <remarks>Organiser only, and only for events that Organiser created.</remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <response code="201">The new category.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has this ID.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPost("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(int eventId, CategoryRequest request)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only add categories to events that you organise." });
        }

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName.Trim(),
            DistanceKM = request.DistanceKM,
            EventType = request.EventType,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, category.ToResponse());
    }

    /// <summary>
    /// Updates a category.
    /// </summary>
    /// <remarks>Organiser only, and only for categories of events that Organiser created.</remarks>
    /// <param name="id">The category's ID.</param>
    /// <response code="200">The updated category.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No category has this ID.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("categories/{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.Event.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only change categories of events that you organise." });
        }

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKM = request.DistanceKM;
        category.EventType = request.EventType;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;

        await _context.SaveChangesAsync();
        return Ok(category.ToResponse());
    }

    /// <summary>
    /// Deletes a category (and its route, if it has one).
    /// </summary>
    /// <remarks>Organiser only, and only for categories of events that Organiser created. Categories that already have enrolments cannot be deleted.</remarks>
    /// <param name="id">The category's ID.</param>
    /// <response code="200">The category was deleted.</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No category has this ID.</response>
    /// <response code="409">The category already has enrolments.</response>
    [Authorize(Roles = "Organiser")]
    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.Event.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete categories of events that you organise." });
        }

        var hasEnrolments = await _context.Enrolments.AnyAsync(e => e.CategoryID == id);
        if (hasEnrolments)
        {
            return Conflict(new { message = "This category already has enrolments, so it cannot be deleted." });
        }

        var route = await _context.Routes.FirstOrDefaultAsync(r => r.CategoryID == id);
        if (route != null)
        {
            _context.Routes.Remove(route);
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Category deleted." });
    }

    /// <summary>
    /// Returns the route details (start, end, elevation gain) of a category.
    /// </summary>
    /// <remarks>Open to everyone. Used by Participants to prepare for race day.</remarks>
    /// <param name="id">The category's ID.</param>
    /// <response code="200">The route details.</response>
    /// <response code="404">The category does not exist, or it has no route recorded yet.</response>
    [HttpGet("categories/{id:int}/route")]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RouteResponse>> GetRoute(int id)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryID == id);
        if (!categoryExists)
        {
            return NotFound(new { message = "Category not found." });
        }

        var route = await _context.Routes.FirstOrDefaultAsync(r => r.CategoryID == id);
        if (route == null)
        {
            return NotFound(new { message = "No route has been recorded for this category yet." });
        }

        return Ok(route.ToResponse());
    }
}
