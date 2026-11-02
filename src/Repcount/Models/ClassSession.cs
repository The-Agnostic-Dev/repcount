namespace Repcount.Models;

/// <summary>One occurrence of a timetabled class on a specific date.</summary>
public class ClassSession
{
    public int Id { get; set; }
    public int GymClassId { get; set; }
    public GymClass GymClass { get; set; } = null!;
    public DateOnly Date { get; set; }

    /// <summary>Copied from the class when the session is created; instructors can change it per session.</summary>
    public int Capacity { get; set; }

    public List<Booking> Bookings { get; set; } = [];

    public DateTime StartsAt => Date.ToDateTime(GymClass.StartTime);
}
