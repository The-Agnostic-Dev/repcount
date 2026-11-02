namespace Repcount.Models;

public enum BookingStatus
{
    Booked,
    Waitlisted,
    Cancelled
}

public class Booking
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public int ClassSessionId { get; set; }
    public ClassSession Session { get; set; } = null!;
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
