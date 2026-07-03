using BarberFlow.Web.Models.Entities;

namespace BarberFlow.Web.Models;

public class CalendarViewModel
{
    public DateOnly WeekStart { get; set; }
    public int? BarberId { get; set; }
    public List<Barber> Barbers { get; set; } = new();
    public List<TimeOnly> TimeSlots { get; set; } = new();
    public List<CalendarDay> Days { get; set; } = new();
}

public class CalendarDay
{
    public DateOnly Date { get; set; }
    public Dictionary<TimeOnly, List<Appointment>> AppointmentsBySlot { get; set; } = new();
}
