using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Repcount.Models;
using Repcount.Services;

namespace Repcount.Pages.Classes;

public class DetailsModel(TimetableService timetable, BookingService bookings, CurrentMember currentMember, Clock clock) : PageModel
{
    public ClassSession Session { get; private set; } = null!;
    public Member? Me { get; private set; }
    public Booking? MyBooking { get; private set; }
    public int Booked { get; private set; }
    public int Waitlisted { get; private set; }
    public bool Started { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var session = await timetable.GetSessionAsync(id);
        if (session is null)
        {
            return NotFound();
        }

        Session = session;
        Me = await currentMember.GetAsync();
        Booked = session.Bookings.Count(b => b.Status == BookingStatus.Booked);
        Waitlisted = session.Bookings.Count(b => b.Status == BookingStatus.Waitlisted);
        Started = session.StartsAt <= clock.Now;
        MyBooking = Me is null ? null : session.Bookings.FirstOrDefault(b => b.MemberId == Me.Id && b.Status != BookingStatus.Cancelled);
        return Page();
    }

    public async Task<IActionResult> OnPostBookAsync(int id)
    {
        var me = await currentMember.GetAsync();
        if (me is null)
        {
            TempData["Error"] = "Sign in or join first, then book your class.";
            return RedirectToPage("/SignIn");
        }

        try
        {
            var booking = await bookings.BookAsync(me.Id, id);
            TempData["Notice"] = $"You're booked into {booking.Session.GymClass.Name} on {booking.Session.Date:dddd d MMM}. See you there!";
        }
        catch (BookingException e)
        {
            TempData["Error"] = e.Message;
        }
        return RedirectToPage(new { id });
    }
}
