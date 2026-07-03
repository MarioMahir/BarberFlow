using BarberFlow.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BarberFlow.Web.Data;

public static class SeedData
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Service>().HasData(
            new Service { Id = 1, Name = "Classic Haircut", Price = 25.00m, DurationMinutes = 30 },
            new Service { Id = 2, Name = "Beard Trim", Price = 15.00m, DurationMinutes = 15 },
            new Service { Id = 3, Name = "Haircut + Beard Combo", Price = 35.00m, DurationMinutes = 45 }
        );

        modelBuilder.Entity<Barber>().HasData(
            new Barber { Id = 1, FirstName = "Alex", LastName = "Rivera", Email = "alex.rivera@barberflow.dev", PhoneNumber = "555-0101" },
            new Barber { Id = 2, FirstName = "Jamie", LastName = "Chen", Email = "jamie.chen@barberflow.dev", PhoneNumber = "555-0102" }
        );

        modelBuilder.Entity<BarberWorkingHour>().HasData(
            new BarberWorkingHour { Id = 1, BarberId = 1, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) },
            new BarberWorkingHour { Id = 2, BarberId = 1, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) },
            new BarberWorkingHour { Id = 3, BarberId = 2, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(18, 0) }
        );
    }
}
