namespace Repcount.Services;

/// <summary>Local wall-clock time for the gym, so tests can pin "now".</summary>
public class Clock(TimeProvider time)
{
    public DateTime Now => time.GetLocalNow().DateTime;

    public DateOnly Today => DateOnly.FromDateTime(Now);
}
