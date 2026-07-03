using System.ComponentModel.DataAnnotations;
using BarberFlow.Web.Models.Entities;

namespace BarberFlow.Web.Models;

public class SelectBarberViewModel
{
    public Service Service { get; set; } = null!;
    public List<Barber> Barbers { get; set; } = new();
}

public class SelectSlotViewModel
{
    public Service Service { get; set; } = null!;
    public Barber Barber { get; set; } = null!;
    public DateOnly Date { get; set; }
    public List<TimeOnly> AvailableSlots { get; set; } = new();
}

// Deliberately separate from Appointment/Client so a public, anonymous POST
// can never bind Status, BarberId overrides, or any other field beyond these.
// Date/Time are carried as invariant "yyyy-MM-dd"/"HH:mm" strings (not DateOnly/TimeOnly)
// so parsing never depends on the server's current culture.
public class ConfirmBookingViewModel
{
    public int ServiceId { get; set; }
    public int BarberId { get; set; }

    [Required]
    public string Date { get; set; } = string.Empty;

    [Required]
    public string Time { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;
    public string BarberName { get; set; } = string.Empty;
}
