using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos
{
    public class RegisterRequest
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8), MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Organiser|Perticipants)$",
            ErrorMessage = "Role must be either 'Organiser' or 'Perticipants'.")]
        public string Role { get; set; } = string.Empty;

        [ MaxLength(20)]
        public string? PhoneNumber { get; set; }

    }
}
