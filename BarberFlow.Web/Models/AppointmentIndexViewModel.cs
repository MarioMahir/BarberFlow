using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using BarberFlow.Web.Models.Entities;

namespace BarberFlow.Web.Models;

public class AppointmentIndexViewModel
{
    public List<Appointment> Appointments { get; set; } = new();

    [Display(Name = "Barber")]
    public int? BarberId { get; set; }

    public AppointmentStatus? Status { get; set; }

    public DateOnly? Date { get; set; }

    public SelectList Barbers { get; set; } = null!;
}
