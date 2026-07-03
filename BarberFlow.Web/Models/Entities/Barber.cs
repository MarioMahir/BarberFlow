using System.ComponentModel.DataAnnotations;

namespace BarberFlow.Web.Models.Entities;

public class Barber
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public ICollection<BarberWorkingHour> WorkingHours { get; set; } = new List<BarberWorkingHour>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
