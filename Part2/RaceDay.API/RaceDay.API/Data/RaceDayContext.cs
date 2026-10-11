using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;
using Route = RaceDay.API.Models.Route;

namespace RaceDay.API.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Route> Routes { get; set; }
    public DbSet<Enrolment> Enrolments { get; set; }
    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Table names and CHECK rules - match the Part 1 SQL script
        modelBuilder.Entity<User>().ToTable("User", t =>
            t.HasCheckConstraint("CK_User_Role",
                "[Role] IN ('Organiser', 'Participant')"));

        modelBuilder.Entity<Event>().ToTable("Event", t =>
            t.HasCheckConstraint("CK_Event_Status",
                "[Status] IN ('Upcoming', 'Completed', 'Cancelled')"));

        modelBuilder.Entity<Category>().ToTable("Category", t =>
            t.HasCheckConstraint("CK_Category_EventType",
                "[EventType] IN ('run', 'walk', 'cycle')"));

        modelBuilder.Entity<Route>().ToTable("Route");

        modelBuilder.Entity<Enrolment>().ToTable("Enrolment", t =>
            t.HasCheckConstraint("CK_Enrolment_PaymentStatus",
                "[PaymentStatus] IN ('Pending', 'Paid')"));

        modelBuilder.Entity<Result>().ToTable("Result", t =>
            t.HasCheckConstraint("CK_Result_Status",
                "[Status] IN ('Finished', 'DNF', 'DSQ')"));

        // Unique rules
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Enrolment>()
            .HasIndex(e => new { e.ParticipantID, e.CategoryID })
            .IsUnique();

        // Defaults calculated by the database (DEFAULT GETDATE())
        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Enrolment>()
            .Property(e => e.EnrolmentDate)
            .HasDefaultValueSql("GETDATE()");

        // Fixed default values
        modelBuilder.Entity<Event>()
            .Property(e => e.Status)
            .HasDefaultValue("Upcoming");

        modelBuilder.Entity<Enrolment>()
            .Property(e => e.PaymentStatus)
            .HasDefaultValue("Pending");

        modelBuilder.Entity<Result>()
            .Property(r => r.Status)
            .HasDefaultValue("Finished");

        // Number sizes - match the DECIMAL columns in the SQL script
        modelBuilder.Entity<Category>()
            .Property(c => c.EntryFee)
            .HasPrecision(8, 2)
            .HasDefaultValue(0m);

        modelBuilder.Entity<Category>()
            .Property(c => c.DistanceKM)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Event>()
            .Property(e => e.Latitude)
            .HasPrecision(9, 6);

        modelBuilder.Entity<Event>()
            .Property(e => e.Longitude)
            .HasPrecision(9, 6);

        modelBuilder.Entity<Route>()
            .Property(r => r.ElevationGainM)
            .HasPrecision(6, 1);

        // Delete rules - no cascading deletes, matching the Part 1 SQL script
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(entity => entity.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}