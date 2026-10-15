using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayContext _context;
    private readonly JwtTokenService _tokenService;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(RaceDayContext context, JwtTokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Registers a new account as either an Organiser or a Participant.
    /// </summary>
    /// <remarks>
    /// Anyone can call this endpoint. The password is hashed before it is stored and is never returned.
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

        return StatusCode(StatusCodes.Status201Created, user.ToResponse());
    }

    /// <summary>
    /// Logs a user in and returns a token (JWT) to send with every later request.
    /// </summary>
    /// <remarks>
    /// Anyone can call this endpoint. Send the token in the Authorization header as "Bearer your-token",
    /// or press the Authorize button in Swagger. The token carries the user's role and expires after 2 hours.
    /// </remarks>
    /// <response code="200">Login succeeded. Returns the token, its expiry time and the user's details.</response>
    /// <response code="401">The email or password is wrong.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such email" and "wrong password", so nobody can find out which emails exist
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var check = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (check == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var (token, expiresAt) = _tokenService.CreateToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = user.ToResponse()
        });
    }

    /// <summary>
    /// Ends the current session.
    /// </summary>
    /// <remarks>
    /// Tokens are stateless, so the server keeps no session to close. The client must delete its stored token.
    /// </remarks>
    /// <response code="200">Logged out.</response>
    /// <response code="401">No valid token was sent.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out. Delete the token on the client." });
    }
}
