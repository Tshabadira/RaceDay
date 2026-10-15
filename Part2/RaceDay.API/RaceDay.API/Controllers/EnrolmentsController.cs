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
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public EnrolmentsController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Enters the logged-in Participant into a category.
    /// </summary>
    /// <remarks>
    /// Participant only. The Participant is taken from the token, so no body is needed.
    /// The system records the link between the Participant, the event and the chosen category,
    /// and assigns a race number.
    /// </remarks>
    /// <param name="categoryId">The category the Participant wants to enter.</param>
    /// <response code="201">The new enrolment, including the race number.</response>
    /// <response code="400">The event is not open for entries, or the category is full.</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not a Participant.</response>
    /// <response code="404">No category has this ID.</response>
    /// <response code="409">The Participant is already enrolled in this category.</response>
    [Authorize(Roles = "Participant")]
    [HttpPost("categories/{categoryId:int}/enrolments")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentResponse>> Enrol(int categoryId)
    {
        var participantId = User.GetUserId();

        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.Event.Status != "Upcoming")
        {
            return BadRequest(new { message = "This event is not open for entries." });
        }

        var alreadyEnrolled = await _context.Enrolments
            .AnyAsync(e => e.ParticipantID == participantId && e.CategoryID == categoryId);
        if (alreadyEnrolled)
        {
            return Conflict(new { message = "You are already enrolled in this category." });
        }

        var enrolledInCategory = await _context.Enrolments.CountAsync(e => e.CategoryID == categoryId);
        if (enrolledInCategory >= category.MaxParticipants)
        {
            return BadRequest(new { message = "This category is full." });
        }

        // Race numbers start at 1001 for each event
        var enrolledInEvent = await _context.Enrolments.CountAsync(e => e.Category.EventID == category.EventID);

        var enrolment = new Enrolment
        {
            ParticipantID = participantId,
            CategoryID = categoryId,
            Category = category,
            RaceNumber = (1001 + enrolledInEvent).ToString(),
            PaymentStatus = "Pending",
            EnrolmentDate = DateTime.Now
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, enrolment.ToResponse());
    }

    /// <summary>
    /// Lists the logged-in Participant's own enrolments.
    /// </summary>
    /// <remarks>Participant only. Newest first.</remarks>
    /// <response code="200">The Participant's enrolments (it can be empty).</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not a Participant.</response>
    [Authorize(Roles = "Participant")]
    [HttpGet("enrolments/my")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EnrolmentResponse>>> GetMyEnrolments()
    {
        var participantId = User.GetUserId();

        var enrolments = await _context.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Where(e => e.ParticipantID == participantId)
            .OrderByDescending(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToResponse()).ToList());
    }

    /// <summary>
    /// Lists everyone who has enrolled in an event, across all its categories.
    /// </summary>
    /// <remarks>Organiser only, and only for events that Organiser created.</remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <response code="200">The enrolments (it can be empty).</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has this ID.</response>
    [Authorize(Roles = "Organiser")]
    [HttpGet("events/{eventId:int}/enrolments")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrolmentResponse>>> GetEnrolmentsForEvent(int eventId)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view enrolments for events that you organise." });
        }

        var enrolments = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Where(e => e.Category.EventID == eventId)
            .OrderBy(e => e.RaceNumber)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToResponse()).ToList());
    }
}
