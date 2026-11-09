using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your first name."), MaxLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name."), MaxLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address."), MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password."), MinLength(8, ErrorMessage = "The password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please repeat the password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a role.")]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Organiser or Participant.")]
    public string Role { get; set; } = "Participant";

    [MaxLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your first name."), MaxLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name."), MaxLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address."), MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string? ProfilePictureUrl { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class EventFormViewModel
{
    public int? EventID { get; set; }

    [Required(ErrorMessage = "Please enter the event name."), MaxLength(100)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."), MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }

    [Required(ErrorMessage = "Please choose the province."), MaxLength(50)]
    public string Province { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string Status { get; set; } = "Upcoming";

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBannerUrl { get; set; }
}

public class CategoryFormViewModel
{
    public int? CategoryID { get; set; }
    public int EventID { get; set; }

    [Required(ErrorMessage = "Please enter the category name."), MaxLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKM { get; set; }

    [Required(ErrorMessage = "Please choose the event type.")]
    [RegularExpression("^(run|walk|cycle)$", ErrorMessage = "Event type must be run, walk or cycle.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "run";

    [Range(0, 999999.99, ErrorMessage = "The entry fee cannot be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000, ErrorMessage = "Maximum participants must be at least 1.")]
    [Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 100;
}

public class ResultFormViewModel
{
    public int EventID { get; set; }
    public int EnrolmentID { get; set; }
    public int? ResultID { get; set; }
    public string? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = "Finished";
}

public class HomeIndexViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public List<string> Provinces { get; set; } = new();
    public string? Province { get; set; }
    public string? Type { get; set; }
    public string? Search { get; set; }
    public string? LoadError { get; set; }
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public WeatherDto? Weather { get; set; }
    public Dictionary<int, RouteDto> Routes { get; set; } = new();
}

public class OrganiserDashboardViewModel
{
    public List<OrganiserEventDto> Events { get; set; } = new();
    public List<OrganiserEventDto> UpcomingEvents { get; set; } = new();
    public int TotalEnrolments { get; set; }
    public string? LoadError { get; set; }
}

public class CategoriesViewModel
{
    public EventDto Event { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EventEnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class EventResultsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public Dictionary<int, ResultDto> ResultsByEnrolment { get; set; } = new();
}
