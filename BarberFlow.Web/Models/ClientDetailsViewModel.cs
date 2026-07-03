using BarberFlow.Web.Models.Entities;

namespace BarberFlow.Web.Models;

public class ClientDetailsViewModel
{
    public Client Client { get; set; } = null!;
    public List<Appointment> UpcomingAppointments { get; set; } = new();
    public List<Appointment> PastAppointments { get; set; } = new();
}
