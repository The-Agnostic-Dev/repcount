using Microsoft.EntityFrameworkCore;
using Repcount.Data;
using Repcount.Models;

namespace Repcount.Services;

public record MemberBooking(Booking Booking, int PeopleAhead);

public class BookingService(RepcountDbContext db, UsageService usage, Clock clock)
{
    public const int MaxCapacity = 60;

    /// <summary>
    /// Books the member into a session, or puts them on the waitlist when it is full.
    /// </summary>
    public async Task<Booking> BookAsync(int memberId, int sessionId)
    {
        var session = await db.Sessions.Include(s => s.GymClass).FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new BookingException("That class could not be found.");
        if (session.StartsAt <= clock.Now)
        {
            throw new BookingException($"{session.GymClass.Name} on {session.Date:dddd d MMM} has already started.");
        }

        var bookedCount = await db.Bookings.CountAsync(b => b.ClassSessionId == sessionId && b.Status == BookingStatus.Booked);
        var isFull = bookedCount >= session.Capacity;

        var member = await db.Members.Include(m => m.Plan).FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new BookingException("Please choose who you are before booking.");

        var alreadyOnList = await db.Bookings.AnyAsync(b =>
            b.MemberId == memberId && b.ClassSessionId == sessionId && b.Status != BookingStatus.Cancelled);
        if (alreadyOnList)
        {
            throw new BookingException($"You're already on the list for {session.GymClass.Name}.");
        }

        if (member.Plan.ClassesPerWeek is int limit)
        {
            var used = await usage.CountClassesInWeekAsync(memberId, Week.StartOf(clock.Today));
            if (used >= limit)
            {
                throw new BookingException(
                    $"Your {member.Plan.Name} plan includes {limit} classes a week and you've booked {used} that week.");
            }
        }

        var booking = new Booking
        {
            MemberId = memberId,
            ClassSessionId = sessionId,
            Status = isFull ? BookingStatus.Waitlisted : BookingStatus.Booked,
            CreatedAt = clock.Now,
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        usage.Invalidate(memberId);
        booking.Session = session;
        return booking;
    }

    public async Task<Booking> CancelAsync(int memberId, int bookingId)
    {
        var booking = await db.Bookings
            .Include(b => b.Session).ThenInclude(s => s.GymClass)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.MemberId == memberId)
            ?? throw new BookingException("That booking could not be found.");
        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new BookingException("That booking was already cancelled.");
        }

        var heldASpot = booking.Status == BookingStatus.Booked;
        booking.Status = BookingStatus.Cancelled;
        await db.SaveChangesAsync();

        if (heldASpot)
        {
            await PromoteWaitlistAsync(booking.Session);
        }
        return booking;
    }

    /// <summary>Instructors can make a single session bigger or smaller, e.g. when a room changes.</summary>
    public async Task<ClassSession> UpdateCapacityAsync(int sessionId, int capacity)
    {
        var session = await db.Sessions.Include(s => s.GymClass).FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new BookingException("That class could not be found.");
        var bookedCount = await db.Bookings.CountAsync(b => b.ClassSessionId == sessionId && b.Status == BookingStatus.Booked);

        if (capacity < 1 || capacity > MaxCapacity)
        {
            throw new BookingException($"Capacity must be between 1 and {MaxCapacity}.");
        }
        if (capacity < bookedCount)
        {
            throw new BookingException($"{bookedCount} people are already booked in, so capacity can't go below that.");
        }

        session.Capacity = capacity;
        await db.SaveChangesAsync();
        return session;
    }

    /// <summary>Moves people from the waitlist into any free spots, first come first served.</summary>
    private async Task PromoteWaitlistAsync(ClassSession session)
    {
        var bookedCount = await db.Bookings.CountAsync(b => b.ClassSessionId == session.Id && b.Status == BookingStatus.Booked);
        var freeSpots = session.Capacity - bookedCount;
        if (freeSpots <= 0)
        {
            return;
        }

        var promoted = await db.Bookings
            .Where(b => b.ClassSessionId == session.Id && b.Status == BookingStatus.Waitlisted)
            .OrderBy(b => b.CreatedAt).ThenBy(b => b.Id)
            .Take(freeSpots)
            .ToListAsync();
        foreach (var booking in promoted)
        {
            booking.Status = BookingStatus.Booked;
            usage.Invalidate(booking.MemberId);
        }
        await db.SaveChangesAsync();
    }

    /// <summary>The member's bookings that have not started yet, soonest first.</summary>
    public async Task<List<MemberBooking>> UpcomingForAsync(int memberId)
    {
        var today = clock.Today;
        var bookings = await db.Bookings
            .Include(b => b.Session).ThenInclude(s => s.GymClass).ThenInclude(c => c.Instructor)
            .Where(b => b.MemberId == memberId && b.Status != BookingStatus.Cancelled && b.Session.Date >= today)
            .ToListAsync();

        var result = new List<MemberBooking>();
        foreach (var booking in bookings.Where(b => b.Session.StartsAt > clock.Now)
                     .OrderBy(b => b.Session.StartsAt))
        {
            var ahead = booking.Status != BookingStatus.Waitlisted ? 0 : await db.Bookings.CountAsync(b =>
                b.ClassSessionId == booking.ClassSessionId &&
                b.Status == BookingStatus.Waitlisted &&
                (b.CreatedAt < booking.CreatedAt || (b.CreatedAt == booking.CreatedAt && b.Id < booking.Id)));
            result.Add(new MemberBooking(booking, ahead));
        }
        return result;
    }

    public Task<List<Booking>> RecentForAsync(int memberId, int count = 10)
    {
        var today = clock.Today;
        return db.Bookings
            .Include(b => b.Session).ThenInclude(s => s.GymClass)
            .Where(b => b.MemberId == memberId && b.Session.Date < today && b.Status == BookingStatus.Booked)
            .OrderByDescending(b => b.Session.Date)
            .Take(count)
            .ToListAsync();
    }
}
