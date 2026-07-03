using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BarberFlow.Web.Data;
using BarberFlow.Web.Extensions;
using BarberFlow.Web.Models;
using BarberFlow.Web.Models.Entities;

namespace BarberFlow.Web.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private const int SlotMinutes = 30;
        private const int MinutesPerDay = 24 * 60;
        private static readonly TimeOnly DefaultDayStart = new(9, 0);
        private static readonly TimeOnly DefaultDayEnd = new(18, 0);

        private readonly BarberFlowDbContext _context;

        public CalendarController(BarberFlowDbContext context)
        {
            _context = context;
        }

        // GET: Calendar?weekStart=2026-07-06&barberId=1
        public async Task<IActionResult> Index(DateOnly? weekStart, int? barberId)
        {
            var isAdmin = User.IsAdmin();
            var currentBarberId = User.GetBarberId();

            if (!isAdmin && currentBarberId == null)
            {
                return Forbid();
            }

            var barbers = isAdmin
                ? await _context.Barbers.OrderBy(b => b.FirstName).ThenBy(b => b.LastName).ToListAsync()
                : await _context.Barbers.Where(b => b.Id == currentBarberId).ToListAsync();
            var monday = ToMonday(weekStart ?? DateOnly.FromDateTime(DateTime.Today));

            if (barbers.Count == 0)
            {
                return View(new CalendarViewModel { WeekStart = monday });
            }

            var selectedBarberId = isAdmin
                ? (barbers.Any(b => b.Id == barberId) ? barberId!.Value : barbers[0].Id)
                : currentBarberId!.Value;

            var workingHours = await _context.BarberWorkingHours
                .Where(w => w.BarberId == selectedBarberId)
                .ToListAsync();

            var dayStart = workingHours.Count > 0 ? workingHours.Min(w => w.StartTime) : DefaultDayStart;
            var dayEnd = workingHours.Count > 0 ? workingHours.Max(w => w.EndTime) : DefaultDayEnd;

            var dayStartMinutes = dayStart.Hour * 60 + dayStart.Minute;
            var dayEndMinutes = Math.Min(dayEnd.Hour * 60 + dayEnd.Minute, MinutesPerDay);
            if (dayEndMinutes <= dayStartMinutes)
            {
                dayEndMinutes = Math.Min(dayStartMinutes + 60, MinutesPerDay);
            }

            var timeSlots = new List<TimeOnly>();
            for (var m = dayStartMinutes; m < dayEndMinutes; m += SlotMinutes)
            {
                timeSlots.Add(MinutesToTimeOnly(m));
            }

            var weekStartDt = monday.ToDateTime(TimeOnly.MinValue);
            var weekEndDt = monday.AddDays(7).ToDateTime(TimeOnly.MinValue);

            var appointments = await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Service)
                .Where(a => a.BarberId == selectedBarberId && a.StartTime >= weekStartDt && a.StartTime < weekEndDt)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            var days = Enumerable.Range(0, 7).Select(offset =>
            {
                var date = monday.AddDays(offset);
                var day = new CalendarDay { Date = date };

                foreach (var appt in appointments.Where(a => DateOnly.FromDateTime(a.StartTime) == date))
                {
                    var slot = SnapToSlot(TimeOnly.FromDateTime(appt.StartTime), dayStartMinutes, dayEndMinutes);
                    if (!day.AppointmentsBySlot.TryGetValue(slot, out var list))
                    {
                        list = new List<Appointment>();
                        day.AppointmentsBySlot[slot] = list;
                    }
                    list.Add(appt);
                }

                return day;
            }).ToList();

            return View(new CalendarViewModel
            {
                WeekStart = monday,
                BarberId = selectedBarberId,
                Barbers = barbers,
                TimeSlots = timeSlots,
                Days = days
            });
        }

        // Buckets a time into the slot it falls in, clamping anything outside
        // [dayStartMinutes, dayEndMinutes) into the nearest edge slot so no
        // appointment (e.g. legacy data booked before working-hours validation existed) is dropped from the view.
        private static TimeOnly SnapToSlot(TimeOnly time, int dayStartMinutes, int dayEndMinutesExclusive)
        {
            var timeMinutes = time.Hour * 60 + time.Minute;
            var lastSlotStart = dayEndMinutesExclusive - SlotMinutes;

            if (timeMinutes <= dayStartMinutes)
            {
                return MinutesToTimeOnly(dayStartMinutes);
            }
            if (timeMinutes >= dayEndMinutesExclusive)
            {
                return MinutesToTimeOnly(lastSlotStart);
            }

            var snapped = dayStartMinutes + (timeMinutes - dayStartMinutes) / SlotMinutes * SlotMinutes;
            return MinutesToTimeOnly(Math.Min(snapped, lastSlotStart));
        }

        private static TimeOnly MinutesToTimeOnly(int minutesSinceMidnight)
        {
            return new TimeOnly(0, 0).Add(TimeSpan.FromMinutes(minutesSinceMidnight));
        }

        private static DateOnly ToMonday(DateOnly date)
        {
            var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.AddDays(-diff);
        }
    }
}
