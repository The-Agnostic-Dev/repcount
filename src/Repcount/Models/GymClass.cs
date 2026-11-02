namespace Repcount.Models;

/// <summary>A class on the weekly timetable, for example "Spin, Mondays at 7:15 PM".</summary>
public class GymClass
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;
    public DayOfWeek Day { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public string Room { get; set; } = "";
}
