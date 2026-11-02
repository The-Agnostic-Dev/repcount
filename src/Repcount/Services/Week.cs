namespace Repcount.Services;

/// <summary>Timetable weeks run Monday to Sunday.</summary>
public static class Week
{
    public static DateOnly StartOf(DateOnly date) => date.AddDays(-(int)date.DayOfWeek + 1);

    public static IEnumerable<DateOnly> Days(DateOnly weekStart) =>
        Enumerable.Range(0, 7).Select(weekStart.AddDays);

    public static bool Contains(DateOnly weekStart, DateOnly date) =>
        date >= weekStart && date < weekStart.AddDays(7);
}
