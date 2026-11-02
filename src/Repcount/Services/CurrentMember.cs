using Microsoft.EntityFrameworkCore;
using Repcount.Data;
using Repcount.Models;

namespace Repcount.Services;

/// <summary>
/// The member using this browser. Repcount runs on the front desk tablet and members'
/// phones without passwords for now, so we remember the choice in a cookie.
/// </summary>
public class CurrentMember(IHttpContextAccessor http, RepcountDbContext db)
{
    public const string CookieName = "repcount_member";

    private Member? member;
    private bool loaded;

    public async Task<Member?> GetAsync()
    {
        if (!loaded)
        {
            loaded = true;
            var raw = http.HttpContext?.Request.Cookies[CookieName];
            if (int.TryParse(raw, out var id))
            {
                member = await db.Members.Include(m => m.Plan).FirstOrDefaultAsync(m => m.Id == id);
            }
        }
        return member;
    }

    public void SignIn(int memberId)
    {
        http.HttpContext!.Response.Cookies.Append(CookieName, memberId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
        });
        loaded = false;
    }

    public void SignOut() => http.HttpContext!.Response.Cookies.Delete(CookieName);
}
