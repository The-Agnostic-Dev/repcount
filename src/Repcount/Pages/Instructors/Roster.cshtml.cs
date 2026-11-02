using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Repcount.Models;
using Repcount.Services;

namespace Repcount.Pages.Instructors;

public class RosterModel(TimetableService timetable, BookingService bookings) : PageModel
{
    public ClassSession Session { get; private set; } = null!;
    public List<Booking> Booked { get; private set; } = [];
    public List<Booking> Waitlist { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var session = await timetable.GetSessionAsync(id);
        if (session is null)
        {
            return NotFound();
        }

        Session = session;
        Booked = session.Bookings.Where(b => b.Status == BookingStatus.Booked).OrderBy(b => b.Member.Name).ToList();
        Waitlist = session.Bookings.Where(b => b.Status == BookingStatus.Waitlisted)
            .OrderBy(b => b.CreatedAt).ThenBy(b => b.Id).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostCapacityAsync(int id, int capacity)
    {
        try
        {
            var session = await bookings.UpdateCapacityAsync(id, capacity);
            TempData["Notice"] = $"Capacity for this {session.GymClass.Name} is now {session.Capacity}.";
        }
        catch (BookingException e)
        {
            TempData["Error"] = e.Message;
        }
        return RedirectToPage(new { id });
    }
}
