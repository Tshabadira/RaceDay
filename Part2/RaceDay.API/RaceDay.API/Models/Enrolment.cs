using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Enrolment
{
    public int EnrolmentID { get; set; }

    public int ParticipantID { get; set; }
    public User Participant { get; set; } = null!;

    public int CategoryID { get; set; }
    public Category Category { get; set; } = null!;

    public DateTime EnrolmentDate { get; set; }

    [MaxLength(20)]
    public string RaceNumber { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    // One enrolment has at most one result (it only exists after the race)
    public Result? Result { get; set; }
}