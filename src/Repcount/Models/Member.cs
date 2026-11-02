namespace Repcount.Models;

public class Member
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public int PlanId { get; set; }
    public MembershipPlan Plan { get; set; } = null!;
    public DateOnly JoinedOn { get; set; }
}
