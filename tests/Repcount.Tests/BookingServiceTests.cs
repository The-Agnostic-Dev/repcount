using Microsoft.EntityFrameworkCore;
using Repcount.Models;
using Repcount.Services;

namespace Repcount.Tests;

public class BookingServiceTests : IDisposable
{
    private readonly TestGym gym = new();

    public void Dispose() => gym.Dispose();

    [Fact]
    public async Task Booking_a_class_with_space_confirms_a_spot()
    {
        var ada = gym.AddMember("Ada");
        var spin = gym.AddSession("Spin", TestGym.Monday.AddDays(3), 18, capacity: 5);

        var booking = await gym.Bookings.BookAsync(ada.Id, spin.Id);

        Assert.Equal(BookingStatus.Booked, booking.Status);
    }

    [Fact]
    public async Task Booking_a_full_class_puts_you_on_the_waitlist()
    {
        var spin = gym.AddSession("Spin", TestGym.Monday.AddDays(3), 18, capacity: 1);
        await gym.Bookings.BookAsync(gym.AddMember("Ada").Id, spin.Id);

        var second = await gym.Bookings.BookAsync(gym.AddMember("Bola").Id, spin.Id);

        Assert.Equal(BookingStatus.Waitlisted, second.Status);
    }

    [Fact]
    public async Task Cancelling_a_spot_promotes_the_first_person_waiting()
    {
        var spin = gym.AddSession("Spin", TestGym.Monday.AddDays(3), 18, capacity: 1);
        var ada = await gym.Bookings.BookAsync(gym.AddMember("Ada").Id, spin.Id);
        var bola = await gym.Bookings.BookAsync(gym.AddMember("Bola").Id, spin.Id);
        var chen = await gym.Bookings.BookAsync(gym.AddMember("Chen").Id, spin.Id);

        await gym.Bookings.CancelAsync(ada.MemberId, ada.Id);

        Assert.Equal(BookingStatus.Booked, (await gym.Db.Bookings.SingleAsync(b => b.Id == bola.Id)).Status);
        Assert.Equal(BookingStatus.Waitlisted, (await gym.Db.Bookings.SingleAsync(b => b.Id == chen.Id)).Status);
    }

    [Fact]
    public async Task You_cannot_book_the_same_class_twice()
    {
        var ada = gym.AddMember("Ada");
        var spin = gym.AddSession("Spin", TestGym.Monday.AddDays(3), 18, capacity: 5);
        await gym.Bookings.BookAsync(ada.Id, spin.Id);

        await Assert.ThrowsAsync<BookingException>(() => gym.Bookings.BookAsync(ada.Id, spin.Id));
    }

    [Fact]
    public async Task You_cannot_book_a_class_that_has_started()
    {
        var ada = gym.AddMember("Ada");
        var earlyHiit = gym.AddSession("HIIT", TestGym.Monday.AddDays(2), 6, capacity: 5);

        await Assert.ThrowsAsync<BookingException>(() => gym.Bookings.BookAsync(ada.Id, earlyHiit.Id));
    }

    [Fact]
    public async Task Plan_limit_stops_extra_bookings_this_week()
    {
        var ada = gym.AddMember("Ada", gym.OneAWeek);
        var thursday = gym.AddSession("Spin", TestGym.Monday.AddDays(3), 18, capacity: 5);
        var friday = gym.AddSession("Yoga", TestGym.Monday.AddDays(4), 18, capacity: 5);
        await gym.Bookings.BookAsync(ada.Id, thursday.Id);

        var error = await Assert.ThrowsAsync<BookingException>(() => gym.Bookings.BookAsync(ada.Id, friday.Id));
        Assert.Contains("1 classes a week", error.Message);
    }

    [Fact]
    public async Task Capacity_cannot_drop_below_people_already_booked()
    {
        var spin = gym.AddSession("Spin", TestGym.Monday.AddDays(3), 18, capacity: 3);
        await gym.Bookings.BookAsync(gym.AddMember("Ada").Id, spin.Id);
        await gym.Bookings.BookAsync(gym.AddMember("Bola").Id, spin.Id);

        await Assert.ThrowsAsync<BookingException>(() => gym.Bookings.UpdateCapacityAsync(spin.Id, 1));
    }
}
