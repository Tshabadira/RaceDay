using System.Security.Claims;

namespace RaceDay.API.Helpers;

public static class CurrentUserExtensions
{
    /// <summary>
    /// Reads the logged-in user's ID from their token.
    /// The ID always comes from the token, never from the request body.
    /// </summary>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }
}
