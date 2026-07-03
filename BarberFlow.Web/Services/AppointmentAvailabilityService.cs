using BarberFlow.Web.Data;
using BarberFlow.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BarberFlow.Web.Services;

public class AppointmentAvailabilityService : IAppointmentAvailabilityService
{
    private const int SlotStepMinutes = 15;

    private readonly BarberFlowDbContext _context;

    public AppointmentAvailabilityService(BarberFlowDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsWithinWorkingHoursAsync(int barberId, DateTime start, DateTime end)
    {
        if (start.Date != end.Date)
        {
            return false;
        }

        var windows = await _context.BarberWorkingHours
            .Where(w => w.BarberId == barberId && w.DayOfWeek == start.DayOfWeek)
            .ToListAsync();

        var startTime = TimeOnly.FromDateTime(start);
        var endTime = TimeOnly.FromDateTime(end);

        return windows.Any(w => startTime >= w.StartTime && endTime <= w.EndTime);
    }

    public async Task<bool> HasOverlapAsync(int barberId, DateTime start, DateTime end, int? excludeAppointmentId = null)
    {
        return await _context.Appointments.AnyAsync(a =>
            a.Id != (excludeAppointmentId ?? 0) &&
            a.BarberId == barberId &&
            a.Status != AppointmentStatus.Cancelled &&
            start < a.EndTime &&
            end > a.StartTime);
    }

    public async Task<List<TimeOnly>> GetAvailableSlotStartTimesAsync(int barberId, int serviceId, DateOnly date)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null)
        {
            return new List<TimeOnly>();
        }

        var windows = await _context.BarberWorkingHours
            .Where(w => w.BarberId == barberId && w.DayOfWeek == date.DayOfWeek)
            .ToListAsync();
        if (windows.Count == 0)
        {
            return new List<TimeOnly>();
        }

        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var existing = await _context.Appointments
            .Where(a => a.BarberId == barberId && a.Status != AppointmentStatus.Cancelled
                && a.StartTime < dayEnd && a.EndTime > dayStart)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        var durationMinutes = service.DurationMinutes;
        var now = DateTime.Now;
        var slots = new List<TimeOnly>();

        foreach (var window in windows)
        {
            var windowStartMinutes = window.StartTime.Hour * 60 + window.StartTime.Minute;
            var windowEndMinutes = window.EndTime.Hour * 60 + window.EndTime.Minute;

            for (var m = windowStartMinutes; m + durationMinutes <= windowEndMinutes; m += SlotStepMinutes)
            {
                var candidateStart = dayStart.AddMinutes(m);
                if (candidateStart < now)
                {
                    continue;
                }

                var candidateEnd = candidateStart.AddMinutes(durationMinutes);
                var overlaps = existing.Any(e => candidateStart < e.EndTime && candidateEnd > e.StartTime);
                if (!overlaps)
                {
                    slots.Add(TimeOnly.FromDateTime(candidateStart));
                }
            }
        }

        return slots.Distinct().OrderBy(t => t).ToList();
    }

    public async Task<BookingResult> TryBookAsync(int barberId, int clientId, int serviceId, DateTime start)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null)
        {
            return new BookingResult(false, null, "Selected service was not found.");
        }

        var end = start.AddMinutes(service.DurationMinutes);
        var useDbLock = _context.Database.IsSqlServer();

        var transaction = useDbLock ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            if (transaction != null)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_getapplock @Resource = {"barber:" + barberId}, @LockMode = 'Exclusive', @LockOwner = 'Transaction'");
            }

            if (!await IsWithinWorkingHoursAsync(barberId, start, end) || await HasOverlapAsync(barberId, start, end))
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                return new BookingResult(false, null, "That time is no longer available. Please pick another slot.");
            }

            var appointment = new Appointment
            {
                BarberId = barberId,
                ClientId = clientId,
                ServiceId = serviceId,
                StartTime = start,
                EndTime = end,
                Status = AppointmentStatus.Scheduled
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return new BookingResult(true, appointment, null);
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
