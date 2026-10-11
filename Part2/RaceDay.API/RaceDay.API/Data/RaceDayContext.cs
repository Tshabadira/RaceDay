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

        modelBuilder.Entity<User>().ToTable("User");

        modelBuilder.Entity<Event>().ToTable("Event");

        modelBuilder.Entity<Category>().ToTable("Category");

        modelBuilder.Entity<Route>().ToTable("Route");

        modelBuilder.Entity<Enrolment>().ToTable("Enrolment");

        modelBuilder.Entity<Result>().ToTable("Result");

        modelBuilder .Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder .Entity<Enrolment>()
            .HasIndex(e => new { e.ParticipantID, e.CategoryID })
            .IsUnique();
    }

}