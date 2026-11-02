using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Repcount.Data;
using Repcount.Models;
using Repcount.Services;

namespace Repcount.Pages;

public class SignInModel(RepcountDbContext db, CurrentMember currentMember) : PageModel
{
    public List<Member> Members { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Members = await db.Members.Include(m => m.Plan).OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(int memberId)
    {
        var member = await db.Members.FindAsync(memberId);
        if (member is null)
        {
            return RedirectToPage();
        }
        currentMember.SignIn(member.Id);
        TempData["Notice"] = $"Signed in as {member.Name}.";
        return RedirectToPage("/Index");
    }

    public IActionResult OnPostSignOut()
    {
        currentMember.SignOut();
        return RedirectToPage("/Index");
    }
}
