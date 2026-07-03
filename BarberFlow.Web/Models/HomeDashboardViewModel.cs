using BarberFlow.Web.Models.Entities;

namespace BarberFlow.Web.Models;

public class HomeDashboardViewModel
{
    public List<Appointment> TodayAppointments { get; set; } = new();
    public List<Appointment> UpcomingAppointments { get; set; } = new();

    public int TodayCount { get; set; }
    public int WeekCount { get; set; }
    public int ActiveClientCount { get; set; }
    public int BarberCount { get; set; }
}
