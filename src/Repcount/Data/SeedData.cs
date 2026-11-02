using Microsoft.EntityFrameworkCore;
using Repcount.Models;
using Repcount.Services;

namespace Repcount.Data;

/// <summary>Demo gym used in development and on the demo tablet. Dates are relative to today.</summary>
public static class SeedData
{
    public static async Task InitializeAsync(RepcountDbContext db, Clock clock)
    {
        if (await db.Plans.AnyAsync())
        {
            return;
        }

        var starter = new MembershipPlan { Name = "Starter", ClassesPerWeek = 2, MonthlyPrice = 29m, Description = "Two classes a week, gym floor any time." };
        var regular = new MembershipPlan { Name = "Regular", ClassesPerWeek = 3, MonthlyPrice = 45m, Description = "Three classes a week, gym floor any time." };
        var unlimited = new MembershipPlan { Name = "Unlimited", ClassesPerWeek = null, MonthlyPrice = 69m, Description = "Every class on the timetable." };
        db.Plans.AddRange(starter, regular, unlimited);

        var amara = new Instructor { Name = "Amara Obi", Bio = "Spin and HIIT, ex-track sprinter." };
        var jonas = new Instructor { Name = "Jonas Weber", Bio = "Strength coach, loves a kettlebell." };
        var priya = new Instructor { Name = "Priya Nair", Bio = "Yoga and Pilates teacher for ten years." };
        var tomasz = new Instructor { Name = "Tomasz Kowal", Bio = "Boxing and bootcamp." };
        db.Instructors.AddRange(amara, jonas, priya, tomasz);

        GymClass Class(string name, Instructor who, DayOfWeek day, int hour, int minute, int minutes, int capacity, string room, string description) =>
            new() { Name = name, Instructor = who, Day = day, StartTime = new TimeOnly(hour, minute), DurationMinutes = minutes, Capacity = capacity, Room = room, Description = description };

        db.Classes.AddRange(
            Class("HIIT", amara, DayOfWeek.Monday, 6, 30, 45, 12, "Studio 1", "Short, sharp intervals to start the week."),
            Class("Vinyasa Yoga", priya, DayOfWeek.Monday, 18, 0, 60, 15, "Studio 2", "Flowing sequences linking breath and movement."),
            Class("Spin", amara, DayOfWeek.Monday, 19, 15, 45, 10, "Bike room", "Hill climbs and sprints to a loud playlist."),
            Class("Spin", amara, DayOfWeek.Tuesday, 7, 0, 45, 10, "Bike room", "Early ride before work."),
            Class("Pilates", priya, DayOfWeek.Tuesday, 12, 15, 45, 12, "Studio 2", "Mat Pilates for core strength and posture."),
            Class("Boxing Fit", tomasz, DayOfWeek.Tuesday, 18, 30, 60, 14, "Studio 1", "Pads, bag work and footwork. Gloves provided."),
            Class("HIIT", amara, DayOfWeek.Wednesday, 6, 30, 45, 12, "Studio 1", "Short, sharp intervals."),
            Class("Mobility", priya, DayOfWeek.Wednesday, 10, 0, 45, 12, "Studio 2", "Joint-friendly mobility and stretching."),
            Class("Strength Circuit", jonas, DayOfWeek.Wednesday, 18, 0, 50, 12, "Weights area", "Barbells, kettlebells and sled in stations."),
            Class("Spin", amara, DayOfWeek.Thursday, 7, 0, 45, 10, "Bike room", "Early ride before work."),
            Class("Boxing Fit", tomasz, DayOfWeek.Thursday, 18, 30, 60, 14, "Studio 1", "Pads, bag work and footwork. Gloves provided."),
            Class("HIIT", amara, DayOfWeek.Friday, 6, 30, 45, 12, "Studio 1", "Short, sharp intervals to end the week."),
            Class("Kettlebells", jonas, DayOfWeek.Friday, 12, 15, 45, 10, "Weights area", "Swings, cleans and presses."),
            Class("Yoga", priya, DayOfWeek.Friday, 17, 30, 60, 15, "Studio 2", "Slow flow to wind down."),
            Class("Bootcamp", tomasz, DayOfWeek.Saturday, 9, 0, 60, 20, "Outdoor court", "Team drills outside, rain or shine."),
            Class("Pilates", priya, DayOfWeek.Saturday, 10, 30, 45, 12, "Studio 2", "Mat Pilates for core strength and posture."),
            Class("Yoga Flow", priya, DayOfWeek.Sunday, 10, 0, 60, 15, "Studio 2", "Sunday morning flow."),
            Class("Stretch & Recover", jonas, DayOfWeek.Sunday, 17, 0, 45, 15, "Studio 1", "Foam rolling and long holds."));

        var today = clock.Today;
        Member Join(string name, string email, MembershipPlan plan, int monthsAgo) =>
            new() { Name = name, Email = email, Plan = plan, JoinedOn = today.AddMonths(-monthsAgo) };

        var members = new[]
        {
            Join("Chidi Okeke", "chidi.okeke@example.com", unlimited, 18),
            Join("Hannah Schmidt", "hannah.schmidt@example.com", regular, 11),
            Join("Mei Lin", "mei.lin@example.com", starter, 9),
            Join("Oluwaseun Adeyemi", "seun.adeyemi@example.com", unlimited, 8),
            Join("Sofia Rossi", "sofia.rossi@example.com", regular, 7),
            Join("Daniel Mensah", "daniel.mensah@example.com", starter, 6),
            Join("Aisha Bello", "aisha.bello@example.com", unlimited, 5),
            Join("Lucas Martin", "lucas.martin@example.com", regular, 4),
            Join("Fatima Yusuf", "fatima.yusuf@example.com", unlimited, 3),
            Join("Ben Carter", "ben.carter@example.com", starter, 2),
            Join("Grace Wanjiku", "grace.wanjiku@example.com", regular, 2),
            Join("Kofi Boateng", "kofi.boateng@example.com", unlimited, 1),
        };
        db.Members.AddRange(members);
        await db.SaveChangesAsync();

        var timetable = new TimetableService(db);
        var thisWeek = Week.StartOf(today);
        var lastWeek = await timetable.GetWeekAsync(thisWeek.AddDays(-7));
        await timetable.GetWeekAsync(thisWeek);
        var nextWeek = await timetable.GetWeekAsync(thisWeek.AddDays(7));

        var bookedAt = clock.Now.AddDays(-3);
        void Book(ClassSession session, Member member, BookingStatus status)
        {
            bookedAt = bookedAt.AddMinutes(7);
            db.Bookings.Add(new Booking { Session = session, Member = member, Status = status, CreatedAt = bookedAt });
        }

        ClassSession Find(IEnumerable<ClassSession> week, string name, DayOfWeek day) =>
            week.First(s => s.GymClass.Name == name && s.GymClass.Day == day);

        // Last week, for history.
        foreach (var m in members.Take(6))
        {
            Book(Find(lastWeek, "Bootcamp", DayOfWeek.Saturday), m, BookingStatus.Booked);
        }
        Book(Find(lastWeek, "Spin", DayOfWeek.Monday), members[1], BookingStatus.Booked);
        Book(Find(lastWeek, "Yoga", DayOfWeek.Friday), members[2], BookingStatus.Booked);

        // Next week: Monday evening spin is full with a waitlist.
        var spin = Find(nextWeek, "Spin", DayOfWeek.Monday);
        foreach (var i in new[] { 0, 1, 2, 3, 4, 6, 7, 8, 10, 11 })
        {
            Book(spin, members[i], BookingStatus.Booked);
        }
        Book(spin, members[5], BookingStatus.Waitlisted);
        Book(spin, members[9], BookingStatus.Waitlisted);

        var strength = Find(nextWeek, "Strength Circuit", DayOfWeek.Wednesday);
        foreach (var i in new[] { 0, 3, 6 })
        {
            Book(strength, members[i], BookingStatus.Booked);
        }
        foreach (var i in new[] { 1, 4, 8, 10 })
        {
            Book(Find(nextWeek, "Pilates", DayOfWeek.Tuesday), members[i], BookingStatus.Booked);
        }
        foreach (var i in new[] { 0, 6, 11 })
        {
            Book(Find(nextWeek, "Bootcamp", DayOfWeek.Saturday), members[i], BookingStatus.Booked);
        }

        await db.SaveChangesAsync();
    }
}
