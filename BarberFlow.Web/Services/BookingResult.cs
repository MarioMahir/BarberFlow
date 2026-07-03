using BarberFlow.Web.Models.Entities;

namespace BarberFlow.Web.Services;

public record BookingResult(bool Success, Appointment? Appointment, string? Error);
