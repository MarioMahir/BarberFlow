namespace BarberFlow.Web.Services;

public interface IAppointmentAvailabilityService
{
    Task<bool> IsWithinWorkingHoursAsync(int barberId, DateTime start, DateTime end);

    Task<bool> HasOverlapAsync(int barberId, DateTime start, DateTime end, int? excludeAppointmentId = null);

    // Returns the start times a service of the given duration could begin at for
    // the given barber/date, honoring working hours, existing bookings, and the current time.
    Task<List<TimeOnly>> GetAvailableSlotStartTimesAsync(int barberId, int serviceId, DateOnly date);

    // Re-validates and books a slot atomically (serialized per-barber on SQL Server) so two
    // concurrent public bookers can't both win the same slot.
    Task<BookingResult> TryBookAsync(int barberId, int clientId, int serviceId, DateTime start);
}
