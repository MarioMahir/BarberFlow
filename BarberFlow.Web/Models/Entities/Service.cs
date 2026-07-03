using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarberFlow.Web.Models.Entities;

public class Service
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10000)]
    public decimal Price { get; set; }

    [Range(1, 480)]
    public int DurationMinutes { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
