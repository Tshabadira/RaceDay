using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public ResultsController(RaceDayContext context)
    {
        _context = context;
    }

    // Loads a result together with the participant, category and event it belongs to
    private IQueryable<Result> ResultsWithDetails()
    {
        return _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event);
    }

    /// <summary>
    /// Returns the logged-in Participant's own results (their race history).
    /// </summary>
    /// <remarks>Participant only. Newest event first.</remarks>
    /// <response code="200">The Participant's results (it can be empty).</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not a Participant.</response>
    [Authorize(Roles = "Participant")]
    [HttpGet("my")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ResultResponse>>> GetMyResults()
    {
        var participantId = User.GetUserId();

        var results = await ResultsWithDetails()
            .Where(r => r.Enrolment.ParticipantID == participantId)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .ToListAsync();

        return Ok(results.Select(r => r.ToResponse()).ToList());
    }

    /// <summary>
    /// Returns one result.
    /// </summary>
    /// <remarks>Open to everyone, like a published results list.</remarks>
    /// <param name="id">The result's ID.</param>
    /// <response code="200">The result.</response>
    /// <response code="404">No result has this ID.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultResponse>> GetResult(int id)
    {
        var result = await ResultsWithDetails().FirstOrDefaultAsync(r => r.ResultID == id);
        if (result == null)
        {
            return NotFound(new { message = "Result not found." });
        }

        return Ok(result.ToResponse());
    }

    /// <summary>
    /// Captures a participant's finish time and positions after the event.
    /// </summary>
    /// <remarks>
    /// Organiser only, and only for events that Organiser created. Each enrolment can have one result.
    /// A runner with status Finished must have a finish time.
    /// </remarks>
    /// <response code="201">The captured result.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No enrolment has this ID.</response>
    /// <response code="409">This enrolment already has a result.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPost]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> CreateResult(CreateResultRequest request)
    {
        var enrolmentId = request.EnrolmentId!.Value;

        var enrolment = await _context.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);
        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (enrolment.Category.Event.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only capture results for events that you organise." });
        }

        if (request.Status == "Finished" && request.FinishTime == null)
        {
            return BadRequest(new { message = "A finished runner needs a finish time." });
        }

        var alreadyHasResult = await _context.Results.AnyAsync(r => r.EnrolmentID == enrolmentId);
        if (alreadyHasResult)
        {
            return Conflict(new { message = "This enrolment already has a result. Use PUT to correct it." });
        }

        var result = new Result
        {
            EnrolmentID = enrolmentId,
            FinishTime = request.FinishTime,
            OverallPosition = request.OverallPosition,
            CategoryPosition = request.CategoryPosition,
            Status = request.Status
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        var saved = await ResultsWithDetails().FirstAsync(r => r.ResultID == result.ResultID);
        return CreatedAtAction(nameof(GetResult), new { id = result.ResultID }, saved.ToResponse());
    }

    /// <summary>
    /// Corrects an existing result.
    /// </summary>
    /// <remarks>Organiser only, and only for events that Organiser created.</remarks>
    /// <param name="id">The result's ID.</param>
    /// <response code="200">The updated result.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No result has this ID.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultResponse>> UpdateResult(int id, UpdateResultRequest request)
    {
        var result = await ResultsWithDetails().FirstOrDefaultAsync(r => r.ResultID == id);
        if (result == null)
        {
            return NotFound(new { message = "Result not found." });
        }

        if (result.Enrolment.Category.Event.OrganiserID != User.GetUserId())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only change results for events that you organise." });
        }

        if (request.Status == "Finished" && request.FinishTime == null)
        {
            return BadRequest(new { message = "A finished runner needs a finish time." });
        }

        result.FinishTime = request.FinishTime;
        result.OverallPosition = request.OverallPosition;
        result.CategoryPosition = request.CategoryPosition;
        result.Status = request.Status;

        await _context.SaveChangesAsync();
        return Ok(result.ToResponse());
    }
}
