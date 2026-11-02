using System.ComponentModel.DataAnnotations;
using Repcount.Models;
using Repcount.Pages;
using Repcount.Services;

namespace Repcount.Tests;

public class TimetableTests : IDisposable
{
    private readonly TestGym gym = new();

    public void Dispose() => gym.Dispose();

    [Theory]
    [InlineData("2026-03-09")]
    [InlineData("2026-03-11")]
    [InlineData("2026-03-14")]
    public void Weeks_start_on_monday(string date)
    {
        Assert.Equal(new DateOnly(2026, 3, 9), Week.StartOf(DateOnly.Parse(date)));
    }

    [Fact]
    public void A_week_has_seven_days()
    {
        var days = Week.Days(TestGym.Monday).ToList();
        Assert.Equal(7, days.Count);
        Assert.Equal(new DateOnly(2026, 3, 15), days[^1]);
    }

    [Fact]
    public async Task Each_timetabled_class_gets_one_session_a_week()
    {
        gym.Db.Classes.AddRange(
            new GymClass { Name = "HIIT", Instructor = gym.Coach, Day = DayOfWeek.Monday, StartTime = new TimeOnly(6, 30), Capacity = 10 },
            new GymClass { Name = "Yoga", Instructor = gym.Coach, Day = DayOfWeek.Thursday, StartTime = new TimeOnly(18, 0), Capacity = 12 });
        await gym.Db.SaveChangesAsync();

        var nextWeek = TestGym.Monday.AddDays(14);
        var sessions = await gym.Timetable.GetWeekAsync(nextWeek);
        var again = await gym.Timetable.GetWeekAsync(nextWeek);

        Assert.Equal(2, sessions.Count);
        Assert.Equal(2, again.Count);
        Assert.All(sessions, s => Assert.True(Week.Contains(nextWeek, s.Date)));
        Assert.Equal(12, sessions.Single(s => s.GymClass.Name == "Yoga").Capacity);
    }

    [Theory]
    [InlineData("Ada Lovelace", "ada@example.com", true)]
    [InlineData("", "ada@example.com", false)]
    [InlineData("Ada Lovelace", "not-an-email", false)]
    public void Join_form_validation(string name, string email, bool valid)
    {
        var input = new JoinInput { Name = name, Email = email, PlanId = 1 };
        var ok = Validator.TryValidateObject(input, new ValidationContext(input), [], validateAllProperties: true);
        Assert.Equal(valid, ok);
    }
}
