using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos;

public class EnrolmentResponse
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string? ParticipantName { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string RaceNumber { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

/// <summary>Used by an Organiser to capture a participant's result.</summary>
public class CreateResultRequest
{
    [Required]
    public int? EnrolmentId { get; set; }

    public TimeSpan? FinishTime { get; set; }

    [Range(1, 1000000)]
    public int? OverallPosition { get; set; }

    [Range(1, 1000000)]
    public int? CategoryPosition { get; set; }

    [RegularExpression("^(Finished|DNF|DSQ)$", ErrorMessage = "Status must be Finished, DNF or DSQ.")]
    public string Status { get; set; } = "Finished";
}

/// <summary>Used by an Organiser to correct an existing result.</summary>
public class UpdateResultRequest
{
    public TimeSpan? FinishTime { get; set; }

    [Range(1, 1000000)]
    public int? OverallPosition { get; set; }

    [Range(1, 1000000)]
    public int? CategoryPosition { get; set; }

    [RegularExpression("^(Finished|DNF|DSQ)$", ErrorMessage = "Status must be Finished, DNF or DSQ.")]
    public string Status { get; set; } = "Finished";
}

public class ResultResponse
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public string? ParticipantName { get; set; }
    public string RaceNumber { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = string.Empty;
}
