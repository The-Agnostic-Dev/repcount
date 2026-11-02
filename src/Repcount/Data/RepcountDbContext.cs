using Microsoft.EntityFrameworkCore;
using Repcount.Models;

namespace Repcount.Data;

public class RepcountDbContext(DbContextOptions<RepcountDbContext> options) : DbContext(options)
{
    public DbSet<MembershipPlan> Plans => Set<MembershipPlan>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<GymClass> Classes => Set<GymClass>();
    public DbSet<ClassSession> Sessions => Set<ClassSession>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MembershipPlan>().Property(p => p.MonthlyPrice).HasConversion<double>();
        modelBuilder.Entity<Member>().HasIndex(m => m.Email).IsUnique();
        modelBuilder.Entity<ClassSession>().HasIndex(s => new { s.GymClassId, s.Date }).IsUnique();
        modelBuilder.Entity<ClassSession>().Ignore(s => s.StartsAt);
        modelBuilder.Entity<Booking>().Property(b => b.Status).HasConversion<string>();
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Session)
            .WithMany(s => s.Bookings)
            .HasForeignKey(b => b.ClassSessionId);
    }
}
