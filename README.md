# Repcount

Repcount is the class booking app for our gym. Members see the weekly class
timetable, book a spot or join the waitlist when a class is full, and cancel from
"My bookings". Each membership plan allows a number of classes per week. Instructors
get a roster for every session and can change how many spots a session has.

There are no passwords yet: the front desk tablet and members' phones pick "who's
training" from a list (or join), and the choice is kept in a cookie.

## Stack

- .NET 8, ASP.NET Core Razor Pages
- EF Core 8 with SQLite (`repcount.db` next to the app)
- xUnit for tests

The database is recreated and filled with demo data (members, instructors, a week of
history and next week's bookings) every time the app starts. Turn that off with
`Repcount__ResetDatabaseOnStart=false`.

## Running it

With Docker:

```bash
docker compose up --build
```

With the .NET 8 SDK:

```bash
dotnet run --project src/Repcount
```

Then open http://localhost:5109.

## Tests

```bash
dotnet test
```

## Layout

| Path | What |
| --- | --- |
| `src/Repcount/Models` | Entities: `MembershipPlan`, `Member`, `Instructor`, `GymClass` (timetable slot), `ClassSession` (one dated occurrence), `Booking` |
| `src/Repcount/Data` | `RepcountDbContext` and the demo seed |
| `src/Repcount/Services` | Booking rules (`BookingService`), timetable generation (`TimetableService`), weekly usage (`UsageService`), week helpers |
| `src/Repcount/Pages` | Razor Pages: timetable (`Index`), class details, my bookings, plans, join, instructors and rosters |
| `tests/Repcount.Tests` | xUnit tests against an in-memory SQLite database |

## Booking rules

- Weeks run Monday to Sunday. Sessions for a week are created from the timetable the first time anyone looks at that week.
- A class is bookable until it starts. When it is full you join the waitlist instead.
- Cancelling a booked spot moves the first person on the waitlist into the class.
- Plans: Starter (2 classes a week), Regular (3), Unlimited. Booked and waitlisted classes count towards the week of the class.
- Instructors can change a single session's capacity from its roster page.
