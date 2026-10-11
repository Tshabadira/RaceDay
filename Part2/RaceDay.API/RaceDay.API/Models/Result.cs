using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Result
{
    public int ResultID { get; set; }

    public int EnrolmentID { get; set; }
    public Enrolment Enrolment { get; set; } = null!;

    public TimeSpan? FinishTime { get; set; }

    public int? OverallPosition { get; set; }

    public int? CategoryPosition { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Finished";
}
