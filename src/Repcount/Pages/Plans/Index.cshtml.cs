using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Repcount.Data;
using Repcount.Models;
using Repcount.Services;

namespace Repcount.Pages.Plans;

public class IndexModel(RepcountDbContext db, CurrentMember currentMember, UsageService usage) : PageModel
{
    public List<MembershipPlan> Plans { get; private set; } = [];
    public Member? Me { get; private set; }

    public async Task OnGetAsync()
    {
        Plans = await db.Plans.OrderBy(p => p.MonthlyPrice).ToListAsync();
        Me = await currentMember.GetAsync();
    }

    public async Task<IActionResult> OnPostChooseAsync(int planId)
    {
        var me = await currentMember.GetAsync();
        if (me is null)
        {
            return RedirectToPage("/SignIn");
        }

        var plan = await db.Plans.FindAsync(planId);
        if (plan is null)
        {
            return NotFound();
        }

        var member = await db.Members.FindAsync(me.Id);
        member!.PlanId = plan.Id;
        await db.SaveChangesAsync();
        usage.Invalidate(me.Id);

        TempData["Notice"] = $"You're now on the {plan.Name} plan.";
        return RedirectToPage();
    }
}
