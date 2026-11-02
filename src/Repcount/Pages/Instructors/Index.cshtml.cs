using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Repcount.Data;
using Repcount.Models;
using Repcount.Services;

namespace Repcount.Pages.Instructors;

public class IndexModel(RepcountDbContext db, TimetableService timetable, Clock clock) : PageModel
{
    public List<Instructor> Instructors { get; private set; } = [];
    public List<ClassSession> Sessions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var thisWeek = Week.StartOf(clock.Today);
        Instructors = await db.Instructors.OrderBy(i => i.Name).ToListAsync();
        Sessions = (await timetable.GetWeekAsync(thisWeek))
            .Concat(await timetable.GetWeekAsync(thisWeek.AddDays(7)))
            .Where(s => s.StartsAt > clock.Now)
            .OrderBy(s => s.StartsAt)
            .ToList();
    }
}
