using System.Globalization;

namespace RaceDay.Web.Helpers;

/// <summary>Small helpers that turn data into friendly text for the pages.</summary>
public static class Display
{
    public static readonly string[] Provinces =
    {
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
        "Mpumalanga", "North West", "Northern Cape", "Western Cape"
    };

    // 1 -> 1st, 2 -> 2nd, 47 -> 47th
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13)
        {
            return number + "th";
        }

        return (number % 10) switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th"
        };
    }

    // The database says Paid; the website says Confirmed
    public static string StatusLabel(string? status) => status switch
    {
        "Paid" => "Confirmed",
        null or "" => "Unknown",
        _ => status
    };

    public static string StatusClass(string? status) => status switch
    {
        "Paid" or "Upcoming" or "Finished" => "rd-status-good",
        "Pending" => "rd-status-pending",
        "Cancelled" or "DNF" or "DSQ" => "rd-status-bad",
        _ => "rd-status-neutral"
    };

    public static string Time(TimeSpan? time) =>
        time.HasValue ? time.Value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) : "-";

    public static string Money(decimal amount) =>
        amount == 0 ? "Free" : "R" + amount.ToString("0.00", CultureInfo.InvariantCulture);

    public static string Distance(decimal km) =>
        km.ToString("0.##", CultureInfo.InvariantCulture) + " km";
}
