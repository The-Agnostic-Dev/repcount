using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Repcount.Models;
using Repcount.Services;

namespace Repcount.Pages.Bookings;

public class IndexModel(BookingService bookings, UsageService usage, CurrentMember currentMember) : PageModel
{
    public Member Me { get; private set; } = null!;
    public List<MemberBooking> Upcoming { get; private set; } = [];
    public List<Booking> Recent { get; private set; } = [];
    public IReadOnlyList<WeeklyUsage> Usage { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var me = await currentMember.GetAsync();
        if (me is null)
        {
            return RedirectToPage("/SignIn");
        }

        Me = me;
        Upcoming = await bookings.UpcomingForAsync(me.Id);
        Recent = await bookings.RecentForAsync(me.Id);
        Usage = await usage.GetAsync(me);
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int bookingId)
    {
        var me = await currentMember.GetAsync();
        if (me is null)
        {
            return RedirectToPage("/SignIn");
        }

        try
        {
            var booking = await bookings.CancelAsync(me.Id, bookingId);
            TempData["Notice"] = $"Cancelled {booking.Session.GymClass.Name} on {booking.Session.Date:dddd d MMM}.";
        }
        catch (BookingException e)
        {
            TempData["Error"] = e.Message;
        }
        return RedirectToPage();
    }
}
