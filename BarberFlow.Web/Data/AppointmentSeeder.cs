using BarberFlow.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BarberFlow.Web.Data;

// Seeds demo clients and appointments at runtime rather than via EF HasData, for two reasons:
// 1. Appointments need dates relative to "today" so the dashboard has something to show,
//    and HasData bakes fixed values into the migration.
// 2. Seeding must be conditional on the tables actually being empty, so it never collides
//    with real data a user has already entered (HasData always tries to (re)insert its rows).
public static class AppointmentSeeder
{
    public static async Task SeedAsync(BarberFlowDbContext context)
    {
        if (!await context.Clients.AnyAsync())
        {
            context.Clients.AddRange(
                new Client { FirstName = "Sofia", LastName = "Morales", Email = "sofia.morales@example.com", PhoneNumber = "555-0201" },
                new Client { FirstName = "Diego", LastName = "Fernandez", Email = "diego.fernandez@example.com", PhoneNumber = "555-0202" },
                new Client { FirstName = "Valentina", LastName = "Ruiz", Email = "valentina.ruiz@example.com", PhoneNumber = "555-0203" },
                new Client { FirstName = "Mateo", LastName = "Gomez", Email = "mateo.gomez@example.com", PhoneNumber = "555-0204" }
            );
            await context.SaveChangesAsync();
        }

        if (await context.Appointments.AnyAsync())
        {
            return;
        }

        var barberIds = await context.Barbers.Select(b => b.Id).ToListAsync();
        var clientIds = await context.Clients.Select(c => c.Id).ToListAsync();
        var services = await context.Services.ToListAsync();

        if (barberIds.Count == 0 || clientIds.Count == 0 || services.Count == 0)
        {
            return;
        }

        var today = DateTime.Today;

        var plan = new (int ClientIndex, int BarberIndex, int ServiceIndex, DateTime StartTime, AppointmentStatus Status)[]
        {
            (0, 0, 0, today.AddHours(9),  AppointmentStatus.Completed),
            (1, 1, 2, today.AddHours(10), AppointmentStatus.Completed),
            (2, 0, 1, today.AddHours(13), AppointmentStatus.Scheduled),
            (3, 1, 0, today.AddHours(15), AppointmentStatus.Scheduled),
            (0, 1, 2, today.AddDays(1).AddHours(11), AppointmentStatus.Scheduled),
            (1, 0, 0, today.AddDays(2).AddHours(9),  AppointmentStatus.Scheduled),
            (2, 1, 1, today.AddDays(-1).AddHours(14), AppointmentStatus.NoShow),
        };

        var appointments = plan
            .Where(p => p.BarberIndex < barberIds.Count && p.ServiceIndex < services.Count)
            .Select(p =>
            {
                var service = services[p.ServiceIndex];
                return new Appointment
                {
                    ClientId = clientIds[p.ClientIndex % clientIds.Count],
                    BarberId = barberIds[p.BarberIndex],
                    ServiceId = service.Id,
                    StartTime = p.StartTime,
                    EndTime = p.StartTime.AddMinutes(service.DurationMinutes),
                    Status = p.Status
                };
            });

        context.Appointments.AddRange(appointments);
        await context.SaveChangesAsync();
    }
}
