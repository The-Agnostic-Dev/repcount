using Microsoft.EntityFrameworkCore;
using Repcount.Data;
using Repcount.Models;

namespace Repcount.Services;

public class TimetableService(RepcountDbContext db)
{
    /// <summary>All sessions in the week starting on <paramref name="weekStart"/>, creating them from the timetable if needed.</summary>
    public async Task<List<ClassSession>> GetWeekAsync(DateOnly weekStart)
    {
        await EnsureWeekAsync(weekStart);
        var weekEnd = weekStart.AddDays(7);
        return await db.Sessions
            .Include(s => s.GymClass).ThenInclude(c => c.Instructor)
            .Include(s => s.Bookings)
            .Where(s => s.Date >= weekStart && s.Date < weekEnd)
            .OrderBy(s => s.Date)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task EnsureWeekAsync(DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        if (await db.Sessions.AnyAsync(s => s.Date >= weekStart && s.Date < weekEnd))
        {
            return;
        }

        var classes = await db.Classes.OrderBy(c => c.Id).ToListAsync();
        foreach (var gymClass in classes)
        {
            db.Sessions.Add(new ClassSession
            {
                GymClassId = gymClass.Id,
                Date = weekStart.AddDays((int)gymClass.Day),
                Capacity = gymClass.Capacity,
            });
        }
        await db.SaveChangesAsync();
    }

    public Task<ClassSession?> GetSessionAsync(int id) =>
        db.Sessions
            .Include(s => s.GymClass).ThenInclude(c => c.Instructor)
            .Include(s => s.Bookings).ThenInclude(b => b.Member)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id);
}
