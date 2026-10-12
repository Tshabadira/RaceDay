using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Registers a new account as either an Organiser or a Participant.
    /// </summary>
    /// <remarks>
    /// Anyone can call this endpoint. The password is hashed before it is stored
    /// and is never returned.
    /// </remarks>
    /// <response code="201">Account created. Returns the new user's details.</response>
    /// <response code="400">Invalid data, or the email is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim();

        var emailTaken = await _context.Users.AnyAsync(u => u.Email == email);
        if (emailTaken)
        {
            return BadRequest(new { message = "An account with this email already exists." });
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Role = request.Role,
            PhoneNumber = request.PhoneNumber
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(user));
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            UserID = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt
        };
    }
}