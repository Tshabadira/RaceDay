using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly RaceDayContext _context;

    public ProfileController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the logged-in user's own profile.
    /// </summary>
    /// <remarks>Any logged-in user (Organiser or Participant). The user is identified from the token.</remarks>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="404">The account no longer exists.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetProfile()
    {
        var user = await _context.Users.FindAsync(User.GetUserId());
        if (user == null)
        {
            return NotFound(new { message = "Account not found." });
        }

        return Ok(user.ToResponse());
    }

    /// <summary>
    /// Updates the logged-in user's own name, email and phone number.
    /// </summary>
    /// <remarks>The role cannot be changed here.</remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Invalid data, or the email belongs to another account.</response>
    /// <response code="401">No valid token was sent.</response>
    /// <response code="404">The account no longer exists.</response>
    [HttpPut]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> UpdateProfile(UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "Account not found." });
        }

        var email = request.Email.Trim();
        var emailTaken = await _context.Users.AnyAsync(u => u.Email == email && u.UserID != userId);
        if (emailTaken)
        {
            return BadRequest(new { message = "Another account already uses this email." });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.PhoneNumber = request.PhoneNumber;

        await _context.SaveChangesAsync();

        return Ok(user.ToResponse());
    }
}
