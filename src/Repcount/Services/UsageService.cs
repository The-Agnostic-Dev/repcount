using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Repcount.Data;
using Repcount.Models;

namespace Repcount.Services;

public record WeeklyUsage(DateOnly WeekStart, int Used, int? Limit);

/// <summary>
/// How many classes a member has booked this week and next, against their plan.
/// Shown on every visit to My bookings, so it is cached per member.
/// </summary>
public class UsageService(RepcountDbContext db, IMemoryCache cache, Clock clock)
{
    private static string Key(int memberId) => $"usage:{memberId}";

    public async Task<IReadOnlyList<WeeklyUsage>> GetAsync(Member member)
    {
        var usage = await cache.GetOrCreateAsync(Key(member.Id), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            var thisWeek = Week.StartOf(clock.Today);
            var nextWeek = thisWeek.AddDays(7);
            return (IReadOnlyList<WeeklyUsage>)
            [
                new WeeklyUsage(thisWeek, await CountClassesInWeekAsync(member.Id, thisWeek), member.Plan.ClassesPerWeek),
                new WeeklyUsage(nextWeek, await CountClassesInWeekAsync(member.Id, nextWeek), member.Plan.ClassesPerWeek),
            ];
        });
        return usage!;
    }

    public void Invalidate(int memberId) => cache.Remove(Key(memberId));

    /// <summary>Classes booked or waitlisted in the week. Cancelled bookings do not count.</summary>
    public Task<int> CountClassesInWeekAsync(int memberId, DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        return db.Bookings.CountAsync(b =>
            b.MemberId == memberId &&
            b.Status != BookingStatus.Cancelled &&
            b.Session.Date >= weekStart &&
            b.Session.Date < weekEnd);
    }
}
