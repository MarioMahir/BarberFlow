using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BarberFlow.Web.Models.Entities;

public class BarberWorkingHour
{
    public int Id { get; set; }

    public int BarberId { get; set; }
    [ValidateNever]
    public Barber Barber { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
