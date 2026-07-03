using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BarberFlow.Web.Models.Entities;

public class Appointment
{
    public int Id { get; set; }

    public int ClientId { get; set; }
    [ValidateNever]
    public Client Client { get; set; } = null!;

    public int BarberId { get; set; }
    [ValidateNever]
    public Barber Barber { get; set; } = null!;

    public int ServiceId { get; set; }
    [ValidateNever]
    public Service Service { get; set; } = null!;

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
}
