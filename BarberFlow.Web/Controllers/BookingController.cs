using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BarberFlow.Web.Data;
using BarberFlow.Web.Models;
using BarberFlow.Web.Models.Entities;
using BarberFlow.Web.Services;

namespace BarberFlow.Web.Controllers
{
    [AllowAnonymous]
    public class BookingController : Controller
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const string TimeFormat = "HH:mm";

        private readonly BarberFlowDbContext _context;
        private readonly IAppointmentAvailabilityService _availabilityService;

        public BookingController(BarberFlowDbContext context, IAppointmentAvailabilityService availabilityService)
        {
            _context = context;
            _availabilityService = availabilityService;
        }

        // GET: Booking
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services.OrderBy(s => s.Name).ToListAsync();
            return View(services);
        }

        // GET: Booking/SelectBarber?serviceId=1
        public async Task<IActionResult> SelectBarber(int serviceId)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null)
            {
                return NotFound();
            }

            var barbers = await _context.Barbers
                .Where(b => b.WorkingHours.Any())
                .OrderBy(b => b.FirstName).ThenBy(b => b.LastName)
                .ToListAsync();

            return View(new SelectBarberViewModel { Service = service, Barbers = barbers });
        }

        // GET: Booking/SelectSlot?serviceId=1&barberId=1&date=2026-07-06
        public async Task<IActionResult> SelectSlot(int serviceId, int barberId, string? date)
        {
            var service = await _context.Services.FindAsync(serviceId);
            var barber = await _context.Barbers.FindAsync(barberId);
            if (service == null || barber == null)
            {
                return NotFound();
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var selectedDate = today;
            if (!string.IsNullOrEmpty(date) &&
                DateOnly.TryParseExact(date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                selectedDate = parsedDate;
            }
            if (selectedDate < today)
            {
                selectedDate = today;
            }

            var slots = await _availabilityService.GetAvailableSlotStartTimesAsync(barberId, serviceId, selectedDate);

            return View(new SelectSlotViewModel
            {
                Service = service,
                Barber = barber,
                Date = selectedDate,
                AvailableSlots = slots
            });
        }

        // GET: Booking/Confirm?serviceId=1&barberId=1&date=2026-07-06&time=10:00
        public async Task<IActionResult> Confirm(int serviceId, int barberId, string date, string time)
        {
            var service = await _context.Services.FindAsync(serviceId);
            var barber = await _context.Barbers.FindAsync(barberId);
            if (service == null || barber == null)
            {
                return NotFound();
            }

            if (!DateOnly.TryParseExact(date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
                !TimeOnly.TryParseExact(time, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return BadRequest();
            }

            return View(new ConfirmBookingViewModel
            {
                ServiceId = serviceId,
                BarberId = barberId,
                Date = date,
                Time = time,
                ServiceName = service.Name,
                BarberName = $"{barber.FirstName} {barber.LastName}"
            });
        }

        // POST: Booking/Confirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(ConfirmBookingViewModel model)
        {
            var service = await _context.Services.FindAsync(model.ServiceId);
            var barber = await _context.Barbers.FindAsync(model.BarberId);
            if (service == null || barber == null)
            {
                return NotFound();
            }

            var validDate = DateOnly.TryParseExact(model.Date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);
            var validTime = TimeOnly.TryParseExact(model.Time, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time);
            if (!validDate || !validTime)
            {
                ModelState.AddModelError(string.Empty, "That time slot is no longer valid. Please pick another.");
            }

            if (!ModelState.IsValid)
            {
                model.ServiceName = service.Name;
                model.BarberName = $"{barber.FirstName} {barber.LastName}";
                return View(model);
            }

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == model.Email);
            if (client == null)
            {
                client = new Client
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber
                };
                _context.Clients.Add(client);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Someone registered with this email in the moment between our lookup and insert.
                    client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == model.Email);
                    if (client == null)
                    {
                        throw;
                    }
                }
            }

            var start = date.ToDateTime(time);
            var result = await _availabilityService.TryBookAsync(model.BarberId, client.Id, model.ServiceId, start);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "That time is no longer available.");
                model.ServiceName = service.Name;
                model.BarberName = $"{barber.FirstName} {barber.LastName}";
                return View(model);
            }

            return RedirectToAction(nameof(Confirmed), new { id = result.Appointment!.Id });
        }

        // GET: Booking/Confirmed/5
        public async Task<IActionResult> Confirmed(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Barber)
                .Include(a => a.Service)
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }
    }
}
