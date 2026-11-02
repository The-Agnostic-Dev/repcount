using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Repcount.Data;
using Repcount.Models;
using Repcount.Services;

namespace Repcount.Tests;

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

/// <summary>A tiny gym in an in-memory SQLite database. "Now" is Wednesday 11 March 2026, 08:00.</summary>
public sealed class TestGym : IDisposable
{
    public static readonly DateOnly Monday = new(2026, 3, 9);

    private readonly SqliteConnection connection;

    public RepcountDbContext Db { get; }
    public Clock Clock { get; } = new(new FixedTimeProvider(new DateTimeOffset(2026, 3, 11, 8, 0, 0, TimeSpan.Zero)));
    public UsageService Usage { get; }
    public BookingService Bookings { get; }
    public TimetableService Timetable { get; }

    public MembershipPlan OneAWeek { get; }
    public MembershipPlan Unlimited { get; }
    public Instructor Coach { get; }

    public TestGym()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        Db = new RepcountDbContext(new DbContextOptionsBuilder<RepcountDbContext>().UseSqlite(connection).Options);
        Db.Database.EnsureCreated();

        Usage = new UsageService(Db, new MemoryCache(new MemoryCacheOptions()), Clock);
        Bookings = new BookingService(Db, Usage, Clock);
        Timetable = new TimetableService(Db);

        OneAWeek = new MembershipPlan { Name = "Taster", ClassesPerWeek = 1, MonthlyPrice = 15m };
        Unlimited = new MembershipPlan { Name = "Unlimited", ClassesPerWeek = null, MonthlyPrice = 69m };
        Coach = new Instructor { Name = "Coach" };
        Db.AddRange(OneAWeek, Unlimited, Coach);
        Db.SaveChanges();
    }

    public Member AddMember(string name, MembershipPlan? plan = null)
    {
        var member = new Member { Name = name, Email = $"{name.ToLowerInvariant()}@example.com", Plan = plan ?? Unlimited, JoinedOn = Monday };
        Db.Members.Add(member);
        Db.SaveChanges();
        return member;
    }

    public ClassSession AddSession(string name, DateOnly date, int hour, int capacity)
    {
        var gymClass = new GymClass
        {
            Name = name,
            Instructor = Coach,
            Day = date.DayOfWeek,
            StartTime = new TimeOnly(hour, 0),
            DurationMinutes = 45,
            Capacity = capacity,
        };
        var session = new ClassSession { GymClass = gymClass, Date = date, Capacity = capacity };
        Db.Sessions.Add(session);
        Db.SaveChanges();
        return session;
    }

    public void Dispose()
    {
        Db.Dispose();
        connection.Dispose();
    }
}
