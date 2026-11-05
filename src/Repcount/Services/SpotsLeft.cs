namespace Repcount.Services;

public static class SpotsLeft
{
    public static int For(int capacity, int booked) => Math.Max(0, capacity - booked);
}
