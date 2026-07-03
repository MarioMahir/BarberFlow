using BarberFlow.Web.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BarberFlow.Web.Data;

public class BarberFlowDbContext : IdentityDbContext<ApplicationUser>
{
    public BarberFlowDbContext(DbContextOptions<BarberFlowDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Barber> Barbers => Set<Barber>();
    public DbSet<BarberWorkingHour> BarberWorkingHours => Set<BarberWorkingHour>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Barber)
            .WithMany()
            .HasForeignKey(u => u.BarberId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Barber>()
            .HasIndex(b => b.Email)
            .IsUnique();

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<BarberWorkingHour>()
            .HasOne(w => w.Barber)
            .WithMany(b => b.WorkingHours)
            .HasForeignKey(w => w.BarberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Client)
            .WithMany(c => c.Appointments)
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Barber)
            .WithMany(b => b.Appointments)
            .HasForeignKey(a => a.BarberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Service)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<Appointment>()
            .ToTable(t => t.HasCheckConstraint("CK_Appointment_EndAfterStart", "[EndTime] > [StartTime]"));

        SeedData.Seed(modelBuilder);
    }
}
