using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Repcount.Data;
using Repcount.Models;
using Repcount.Services;

namespace Repcount.Pages;

public class JoinInput
{
    [Required(ErrorMessage = "Please tell us your name.")]
    [StringLength(80)]
    [RegularExpression(@"^[A-Za-z ]+$", ErrorMessage = "Please use letters only for your name.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "We need an email address to send class reminders.")]
    [EmailAddress(ErrorMessage = "That email address doesn't look right.")]
    public string Email { get; set; } = "";

    [Required]
    public int PlanId { get; set; }
}

public class JoinModel(RepcountDbContext db, CurrentMember currentMember, Clock clock) : PageModel
{
    [BindProperty]
    public JoinInput Input { get; set; } = new();

    public List<MembershipPlan> Plans { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Plans = await db.Plans.OrderBy(p => p.MonthlyPrice).ToListAsync();
        Input.PlanId = Plans.FirstOrDefault()?.Id ?? 0;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Plans = await db.Plans.OrderBy(p => p.MonthlyPrice).ToListAsync();
        var email = Input.Email.Trim().ToLowerInvariant();
        if (ModelState.IsValid && await db.Members.AnyAsync(m => m.Email == email))
        {
            ModelState.AddModelError("Input.Email", "Someone has already joined with that email.");
        }
        if (ModelState.IsValid && Plans.All(p => p.Id != Input.PlanId))
        {
            ModelState.AddModelError("Input.PlanId", "Please choose a plan.");
        }
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var member = new Member
        {
            Name = Input.Name.Trim(),
            Email = email,
            PlanId = Input.PlanId,
            JoinedOn = clock.Today,
        };
        db.Members.Add(member);
        await db.SaveChangesAsync();

        currentMember.SignIn(member.Id);
        TempData["Notice"] = $"Welcome to Repcount, {member.Name}! Pick your first class.";
        return RedirectToPage("/Bookings/Index");
    }
}
