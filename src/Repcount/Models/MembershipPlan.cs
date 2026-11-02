namespace Repcount.Models;

public class MembershipPlan
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";

    /// <summary>Classes a member may book per calendar week (Monday to Sunday). Null means unlimited.</summary>
    public int? ClassesPerWeek { get; set; }

    public decimal MonthlyPrice { get; set; }
}
