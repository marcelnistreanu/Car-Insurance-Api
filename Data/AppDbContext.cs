using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<InsurancePolicy> Policies => Set<InsurancePolicy>();
    public DbSet<InsuranceClaim> Claims => Set<InsuranceClaim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Car>()
            .HasIndex(c => c.Vin)
            .IsUnique(true);

        modelBuilder.Entity<InsurancePolicy>()
            .Property(p => p.StartDate)
            .IsRequired();

        modelBuilder.Entity<InsurancePolicy>()
            .Property(p => p.EndDate)
            .IsRequired();

        modelBuilder.Entity<InsuranceClaim>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(p => p.ClaimDate).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(ClaimStatusEnumType.Pending);
            entity.HasOne(c => c.Car)
                .WithMany(c => c.Claims)
                .HasForeignKey(c => c.CarId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public static class SeedData
{
    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.Owners.Any()) return;

        var ana = new Owner { Name = "Ana Pop", Email = "ana.pop@example.com" };
        var bogdan = new Owner { Name = "Bogdan Ionescu", Email = "bogdan.ionescu@example.com" };
        db.Owners.AddRange(ana, bogdan);
        db.SaveChanges();

        var car1 = new Car { Vin = "VIN12345", Make = "Dacia", Model = "Logan", YearOfManufacture = 2018, OwnerId = ana.Id };
        var car2 = new Car { Vin = "VIN67890", Make = "VW", Model = "Golf", YearOfManufacture = 2021, OwnerId = bogdan.Id };
        db.Cars.AddRange(car1, car2);
        db.SaveChanges();

        db.Policies.AddRange(
            new InsurancePolicy { CarId = car1.Id, Provider = "Allianz", StartDate = new DateOnly(2024, 1, 1), EndDate = new DateOnly(2024, 12, 31) },
            new InsurancePolicy { CarId = car1.Id, Provider = "Groupama", StartDate = new DateOnly(2025, 1, 1), EndDate = new DateOnly(2025, 12, 31) },
            new InsurancePolicy { CarId = car2.Id, Provider = "Allianz", StartDate = new DateOnly(2025, 3, 1), EndDate = new DateOnly(2026, 3, 1) }
        );
        db.SaveChanges();
    }
}
