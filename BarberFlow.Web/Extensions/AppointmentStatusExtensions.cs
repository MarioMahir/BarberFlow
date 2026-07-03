using BarberFlow.Web.Models.Entities;

namespace BarberFlow.Web.Extensions;

public static class AppointmentStatusExtensions
{
    public static string BadgeClass(this AppointmentStatus status) => status switch
    {
        AppointmentStatus.Completed => "bg-success",
        AppointmentStatus.Cancelled => "bg-danger",
        AppointmentStatus.NoShow => "bg-warning text-dark",
        _ => "bg-primary"
    };
}
