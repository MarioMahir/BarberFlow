using BarberFlow.Web.Data;
using BarberFlow.Web.Models.Entities;
using BarberFlow.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace BarberFlow.Web.Tests;

public class AppointmentAvailabilityServiceTests
{
    private static BarberFlowDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BarberFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BarberFlowDbContext(options);
    }

    private static async Task<BarberFlowDbContext> SeedBarberWithMondayHoursAsync(int barberId)
    {
        var context = CreateContext();
        context.Barbers.Add(new Barber { Id = barberId, FirstName = "Test", LastName = "Barber", Email = $"barber{barberId}@test.dev", PhoneNumber = "555-0000" });
        context.BarberWorkingHours.Add(new BarberWorkingHour
        {
            BarberId = barberId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        });
        await context.SaveChangesAsync();
        return context;
    }

    // A fixed Monday, so tests aren't sensitive to when they run.
    private static readonly DateTime Monday = new(2026, 7, 6, 0, 0, 0);

    [Fact]
    public async Task IsWithinWorkingHoursAsync_ReturnsTrue_WhenFullyInsideWindow()
    {
        await using var context = await SeedBarberWithMondayHoursAsync(barberId: 1);
        var service = new AppointmentAvailabilityService(context);

        var result = await service.IsWithinWorkingHoursAsync(1, Monday.AddHours(10), Monday.AddHours(10).AddMinutes(30));

        Assert.True(result);
    }

    [Fact]
    public async Task IsWithinWorkingHoursAsync_ReturnsFalse_WhenBarberHasNoHoursThatDay()
    {
        await using var context = await SeedBarberWithMondayHoursAsync(barberId: 1);
        var service = new AppointmentAvailabilityService(context);

        var tuesday = Monday.AddDays(1);
        var result = await service.IsWithinWorkingHoursAsync(1, tuesday.AddHours(10), tuesday.AddHours(10).AddMinutes(30));

        Assert.False(result);
    }

    [Fact]
    public async Task IsWithinWorkingHoursAsync_ReturnsFalse_WhenStartsBeforeWindow()
    {
        await using var context = await SeedBarberWithMondayHoursAsync(barberId: 1);
        var service = new AppointmentAvailabilityService(context);

        var result = await service.IsWithinWorkingHoursAsync(1, Monday.AddHours(8).AddMinutes(30), Monday.AddHours(9).AddMinutes(30));

        Assert.False(result);
    }

    [Fact]
    public async Task IsWithinWorkingHoursAsync_ReturnsFalse_WhenEndsAfterWindow()
    {
        await using var context = await SeedBarberWithMondayHoursAsync(barberId: 1);
        var service = new AppointmentAvailabilityService(context);

        var result = await service.IsWithinWorkingHoursAsync(1, Monday.AddHours(16).AddMinutes(30), Monday.AddHours(17).AddMinutes(30));

        Assert.False(result);
    }

    [Fact]
    public async Task IsWithinWorkingHoursAsync_ReturnsFalse_WhenAppointmentCrossesMidnight()
    {
        await using var context = await SeedBarberWithMondayHoursAsync(barberId: 1);
        var service = new AppointmentAvailabilityService(context);

        var result = await service.IsWithinWorkingHoursAsync(1, Monday.AddHours(23), Monday.AddDays(1).AddHours(1));

        Assert.False(result);
    }

    [Fact]
    public async Task HasOverlapAsync_ReturnsTrue_WhenAnotherAppointmentOverlaps()
    {
        await using var context = CreateContext();
        context.Appointments.Add(new Appointment
        {
            BarberId = 1,
            ClientId = 1,
            ServiceId = 1,
            StartTime = Monday.AddHours(10),
            EndTime = Monday.AddHours(10).AddMinutes(30),
            Status = AppointmentStatus.Scheduled
        });
        await context.SaveChangesAsync();
        var service = new AppointmentAvailabilityService(context);

        var result = await service.HasOverlapAsync(1, Monday.AddHours(10).AddMinutes(15), Monday.AddHours(10).AddMinutes(45));

        Assert.True(result);
    }

    [Fact]
    public async Task HasOverlapAsync_ReturnsFalse_WhenAppointmentsAreBackToBack()
    {
        await using var context = CreateContext();
        context.Appointments.Add(new Appointment
        {
            BarberId = 1,
            ClientId = 1,
            ServiceId = 1,
            StartTime = Monday.AddHours(10),
            EndTime = Monday.AddHours(10).AddMinutes(30),
            Status = AppointmentStatus.Scheduled
        });
        await context.SaveChangesAsync();
        var service = new AppointmentAvailabilityService(context);

        var result = await service.HasOverlapAsync(1, Monday.AddHours(10).AddMinutes(30), Monday.AddHours(11));

        Assert.False(result);
    }

    [Fact]
    public async Task HasOverlapAsync_ReturnsFalse_WhenTheOverlappingAppointmentIsCancelled()
    {
        await using var context = CreateContext();
        context.Appointments.Add(new Appointment
        {
            BarberId = 1,
            ClientId = 1,
            ServiceId = 1,
            StartTime = Monday.AddHours(10),
            EndTime = Monday.AddHours(10).AddMinutes(30),
            Status = AppointmentStatus.Cancelled
        });
        await context.SaveChangesAsync();
        var service = new AppointmentAvailabilityService(context);

        var result = await service.HasOverlapAsync(1, Monday.AddHours(10), Monday.AddHours(10).AddMinutes(30));

        Assert.False(result);
    }

    [Fact]
    public async Task HasOverlapAsync_ExcludesTheAppointmentBeingEdited()
    {
        await using var context = CreateContext();
        context.Appointments.Add(new Appointment
        {
            Id = 42,
            BarberId = 1,
            ClientId = 1,
            ServiceId = 1,
            StartTime = Monday.AddHours(10),
            EndTime = Monday.AddHours(10).AddMinutes(30),
            Status = AppointmentStatus.Scheduled
        });
        await context.SaveChangesAsync();
        var service = new AppointmentAvailabilityService(context);

        var result = await service.HasOverlapAsync(1, Monday.AddHours(10), Monday.AddHours(10).AddMinutes(30), excludeAppointmentId: 42);

        Assert.False(result);
    }

    // Always a week out, so slot-availability tests (which filter out past times
    // relative to DateTime.Now) never go flaky depending on when the suite runs.
    private static readonly DateOnly FutureTestDate = DateOnly.FromDateTime(DateTime.Today).AddDays(7);

    private static async Task<(BarberFlowDbContext Context, int BarberId, int ServiceId)> SeedForSlotTestsAsync(
        TimeOnly windowStart, TimeOnly windowEnd, int durationMinutes)
    {
        var context = CreateContext();
        context.Barbers.Add(new Barber { Id = 1, FirstName = "Test", LastName = "Barber", Email = "slot.barber@test.dev", PhoneNumber = "555-0000" });
        context.Services.Add(new Service { Id = 1, Name = "Test Service", Price = 10m, DurationMinutes = durationMinutes });
        context.BarberWorkingHours.Add(new BarberWorkingHour
        {
            BarberId = 1,
            DayOfWeek = FutureTestDate.DayOfWeek,
            StartTime = windowStart,
            EndTime = windowEnd
        });
        await context.SaveChangesAsync();
        return (context, 1, 1);
    }

    [Fact]
    public async Task GetAvailableSlotStartTimesAsync_ReturnsSlotsAcrossTheWholeWindow_WhenNothingBooked()
    {
        var (context, barberId, serviceId) = await SeedForSlotTestsAsync(new TimeOnly(9, 0), new TimeOnly(10, 0), durationMinutes: 30);
        await using var _ = context;
        var service = new AppointmentAvailabilityService(context);

        var slots = await service.GetAvailableSlotStartTimesAsync(barberId, serviceId, FutureTestDate);

        // 9:00-10:00 window, 30-min service, 15-min step -> 9:00, 9:15, 9:30 all fit (9:45+30=10:15 doesn't)
        Assert.Equal(new[] { new TimeOnly(9, 0), new TimeOnly(9, 15), new TimeOnly(9, 30) }, slots);
    }

    [Fact]
    public async Task GetAvailableSlotStartTimesAsync_ExcludesSlotsThatOverlapAnExistingAppointment()
    {
        var (context, barberId, serviceId) = await SeedForSlotTestsAsync(new TimeOnly(9, 0), new TimeOnly(10, 0), durationMinutes: 30);
        await using var _ = context;
        context.Clients.Add(new Client { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", PhoneNumber = "555-1111" });
        context.Appointments.Add(new Appointment
        {
            BarberId = barberId,
            ClientId = 1,
            ServiceId = serviceId,
            StartTime = FutureTestDate.ToDateTime(new TimeOnly(9, 15)),
            EndTime = FutureTestDate.ToDateTime(new TimeOnly(9, 45)),
            Status = AppointmentStatus.Scheduled
        });
        await context.SaveChangesAsync();
        var service = new AppointmentAvailabilityService(context);

        var slots = await service.GetAvailableSlotStartTimesAsync(barberId, serviceId, FutureTestDate);

        // 9:00 (would end 9:30, overlaps 9:15-9:45) and 9:15/9:30 (inside the booking) are all excluded.
        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetAvailableSlotStartTimesAsync_IgnoresCancelledAppointments()
    {
        var (context, barberId, serviceId) = await SeedForSlotTestsAsync(new TimeOnly(9, 0), new TimeOnly(10, 0), durationMinutes: 30);
        await using var _ = context;
        context.Clients.Add(new Client { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", PhoneNumber = "555-1111" });
        context.Appointments.Add(new Appointment
        {
            BarberId = barberId,
            ClientId = 1,
            ServiceId = serviceId,
            StartTime = FutureTestDate.ToDateTime(new TimeOnly(9, 0)),
            EndTime = FutureTestDate.ToDateTime(new TimeOnly(9, 30)),
            Status = AppointmentStatus.Cancelled
        });
        await context.SaveChangesAsync();
        var service = new AppointmentAvailabilityService(context);

        var slots = await service.GetAvailableSlotStartTimesAsync(barberId, serviceId, FutureTestDate);

        Assert.Contains(new TimeOnly(9, 0), slots);
    }

    [Fact]
    public async Task GetAvailableSlotStartTimesAsync_ReturnsEmpty_WhenBarberHasNoHoursThatDay()
    {
        var context = CreateContext();
        await using var _ = context;
        context.Barbers.Add(new Barber { Id = 1, FirstName = "Test", LastName = "Barber", Email = "no.hours@test.dev", PhoneNumber = "555-0000" });
        context.Services.Add(new Service { Id = 1, Name = "Test Service", Price = 10m, DurationMinutes = 30 });
        await context.SaveChangesAsync();
        var service = new AppointmentAvailabilityService(context);

        var slots = await service.GetAvailableSlotStartTimesAsync(1, 1, FutureTestDate);

        Assert.Empty(slots);
    }

    [Fact]
    public async Task TryBookAsync_Succeeds_WhenSlotIsAvailable()
    {
        var (context, barberId, serviceId) = await SeedForSlotTestsAsync(new TimeOnly(9, 0), new TimeOnly(10, 0), durationMinutes: 30);
        await using var _ = context;
        context.Clients.Add(new Client { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", PhoneNumber = "555-1111" });
        await context.SaveChangesAsync();
        var service = new AppointmentAvailabilityService(context);

        var result = await service.TryBookAsync(barberId, 1, serviceId, FutureTestDate.ToDateTime(new TimeOnly(9, 0)));

        Assert.True(result.Success);
        Assert.NotNull(result.Appointment);
        Assert.Equal(AppointmentStatus.Scheduled, result.Appointment!.Status);
    }

    [Fact]
    public async Task TryBookAsync_Fails_WhenSlotIsAlreadyTaken()
    {
        var (context, barberId, serviceId) = await SeedForSlotTestsAsync(new TimeOnly(9, 0), new TimeOnly(10, 0), durationMinutes: 30);
        await using var _ = context;
        context.Clients.Add(new Client { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", PhoneNumber = "555-1111" });
        context.Appointments.Add(new Appointment
        {
            BarberId = barberId,
            ClientId = 1,
            ServiceId = serviceId,
            StartTime = FutureTestDate.ToDateTime(new TimeOnly(9, 0)),
            EndTime = FutureTestDate.ToDateTime(new TimeOnly(9, 30)),
            Status = AppointmentStatus.Scheduled
        });
        await context.SaveChangesAsync();
        var service = new AppointmentAvailabilityService(context);

        var result = await service.TryBookAsync(barberId, 1, serviceId, FutureTestDate.ToDateTime(new TimeOnly(9, 0)));

        Assert.False(result.Success);
        Assert.Null(result.Appointment);
    }
}
