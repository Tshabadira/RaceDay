using System.ComponentModel.DataAnnotations;
namespace RaceDay.API.Models
{
    public class User
    {
        public int UserID { get; set; }

        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties - uncomment once Event and Enrolment exist
         public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
         public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
