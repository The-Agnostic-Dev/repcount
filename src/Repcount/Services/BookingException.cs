namespace Repcount.Services;

/// <summary>A booking rule was broken. The message is shown to the member as is.</summary>
public class BookingException(string message) : Exception(message);
