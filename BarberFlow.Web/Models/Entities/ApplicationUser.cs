using Microsoft.AspNetCore.Identity;

namespace BarberFlow.Web.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public int? BarberId { get; set; }
    public Barber? Barber { get; set; }
}
