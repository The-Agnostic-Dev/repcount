using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Repcount.Models;
using Repcount.Services;

namespace Repcount.Pages;

public record ClassCard(
    int SessionId,
    string Name,
    string Instructor,
    string Room,
    TimeOnly Starts,
    string Time,
    int DurationMinutes,
    int Capacity,
    int Booked,
    int Waitlisted,
    bool Started);

public record DayColumn(DateOnly Date, bool IsToday, List<ClassCard> Classes);

public class IndexModel(TimetableService timetable, Clock clock) : PageModel
{
    public DateOnly WeekStart { get; private set; }
    public List<DayColumn> Days { get; private set; } = [];
    public bool IsThisWeek { get; private set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? Date { get; set; }

    public async Task OnGetAsync()
    {
        var today = clock.Today;
        WeekStart = Week.StartOf(Date ?? today);
        IsThisWeek = Week.Contains(WeekStart, today);

        var sessions = await timetable.GetWeekAsync(WeekStart);
        Days = Week.Days(WeekStart)
            .Select(day => new DayColumn(day, day == today, sessions
                .Where(s => s.Date == day)
                .Select(ToCard)
                .ToList()))
            .ToList();
    }

    private ClassCard ToCard(ClassSession s) => new(
        s.Id,
        s.GymClass.Name,
        s.GymClass.Instructor.Name,
        s.GymClass.Room,
        s.GymClass.StartTime,
        s.GymClass.StartTime.ToString("h:mm tt", CultureInfo.InvariantCulture),
        s.GymClass.DurationMinutes,
        s.Capacity,
        s.Bookings.Count(b => b.Status == BookingStatus.Booked),
        s.Bookings.Count(b => b.Status == BookingStatus.Waitlisted),
        s.StartsAt <= clock.Now);
}
