using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BarberFlow.Web.Data;
using BarberFlow.Web.Extensions;
using BarberFlow.Web.Models;
using BarberFlow.Web.Models.Entities;
using BarberFlow.Web.Services;

namespace BarberFlow.Web.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly BarberFlowDbContext _context;
        private readonly IAppointmentAvailabilityService _availabilityService;

        public AppointmentsController(BarberFlowDbContext context, IAppointmentAvailabilityService availabilityService)
        {
            _context = context;
            _availabilityService = availabilityService;
        }

        private bool IsAdmin => User.IsAdmin();
        private int? CurrentBarberId => User.GetBarberId();

        // Displays full names instead of emails; restricted to the caller's own barber for non-admins.
        private SelectList BuildBarberSelectList(int? selectedId)
        {
            var barbers = IsAdmin ? _context.Barbers.AsQueryable() : _context.Barbers.Where(b => b.Id == CurrentBarberId);

            return new SelectList(
                barbers.OrderBy(b => b.FirstName).ThenBy(b => b.LastName)
                    .Select(b => new { b.Id, Name = b.FirstName + " " + b.LastName }),
                "Id", "Name", selectedId);
        }

        private SelectList BuildClientSelectList(int? selectedId)
        {
            return new SelectList(
                _context.Clients.OrderBy(c => c.FirstName).ThenBy(c => c.LastName)
                    .Select(c => new { c.Id, Name = c.FirstName + " " + c.LastName }),
                "Id", "Name", selectedId);
        }

        // GET: Appointments
        public async Task<IActionResult> Index(int? barberId, AppointmentStatus? status, DateOnly? date)
        {
            if (!IsAdmin && CurrentBarberId == null)
            {
                return Forbid();
            }

            var effectiveBarberId = IsAdmin ? barberId : CurrentBarberId;

            var query = _context.Appointments
                .Include(a => a.Barber)
                .Include(a => a.Client)
                .Include(a => a.Service)
                .AsQueryable();

            if (effectiveBarberId.HasValue)
            {
                query = query.Where(a => a.BarberId == effectiveBarberId);
            }
            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status);
            }
            if (date.HasValue)
            {
                var dayStart = date.Value.ToDateTime(TimeOnly.MinValue);
                var dayEnd = dayStart.AddDays(1);
                query = query.Where(a => a.StartTime >= dayStart && a.StartTime < dayEnd);
            }

            var barbers = IsAdmin
                ? await _context.Barbers.OrderBy(b => b.FirstName).ThenBy(b => b.LastName).ToListAsync()
                : await _context.Barbers.Where(b => b.Id == effectiveBarberId).ToListAsync();

            var viewModel = new AppointmentIndexViewModel
            {
                Appointments = await query.OrderBy(a => a.StartTime).ToListAsync(),
                BarberId = effectiveBarberId,
                Status = status,
                Date = date,
                Barbers = new SelectList(
                    barbers.Select(b => new { b.Id, Name = $"{b.FirstName} {b.LastName}" }),
                    "Id", "Name", effectiveBarberId)
            };

            return View(viewModel);
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Barber)
                .Include(a => a.Client)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            if (!IsAdmin && appointment.BarberId != CurrentBarberId)
            {
                return Forbid();
            }

            return View(appointment);
        }

        // GET: Appointments/Create
        public IActionResult Create()
        {
            if (!IsAdmin && CurrentBarberId == null)
            {
                return Forbid();
            }

            ViewData["BarberId"] = BuildBarberSelectList(CurrentBarberId);
            ViewData["ClientId"] = BuildClientSelectList(null);
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name");
            return View();
        }

        // POST: Appointments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ClientId,BarberId,ServiceId,StartTime,Status")] Appointment appointment)
        {
            if (!IsAdmin && CurrentBarberId == null)
            {
                return Forbid();
            }

            if (!IsAdmin)
            {
                // Never trust a client-submitted BarberId for a non-admin: always book under the caller's own barber.
                appointment.BarberId = CurrentBarberId!.Value;
            }

            ModelState.Remove(nameof(Appointment.EndTime));

            if (ModelState.IsValid && await TrySetEndTimeAndValidateOverlap(appointment))
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BarberId"] = BuildBarberSelectList(appointment.BarberId);
            ViewData["ClientId"] = BuildClientSelectList(appointment.ClientId);
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", appointment.ServiceId);
            return View(appointment);
        }

        // Computes EndTime from the selected service's duration and checks for
        // overlapping appointments with the same barber; adds a ModelState error on conflict.
        private async Task<bool> TrySetEndTimeAndValidateOverlap(Appointment appointment)
        {
            var service = await _context.Services.FindAsync(appointment.ServiceId);
            if (service == null)
            {
                ModelState.AddModelError(nameof(Appointment.ServiceId), "Selected service was not found.");
                return false;
            }

            appointment.EndTime = appointment.StartTime.AddMinutes(service.DurationMinutes);

            if (!await _availabilityService.IsWithinWorkingHoursAsync(appointment.BarberId, appointment.StartTime, appointment.EndTime))
            {
                var worksThatDay = await _context.BarberWorkingHours.AnyAsync(w =>
                    w.BarberId == appointment.BarberId && w.DayOfWeek == appointment.StartTime.DayOfWeek);

                ModelState.AddModelError(nameof(Appointment.StartTime), worksThatDay
                    ? "The selected time is outside the barber's working hours for that day."
                    : $"The barber does not work on {appointment.StartTime.DayOfWeek}s.");
                return false;
            }

            var excludeId = appointment.Id == 0 ? (int?)null : appointment.Id;
            if (await _availabilityService.HasOverlapAsync(appointment.BarberId, appointment.StartTime, appointment.EndTime, excludeId))
            {
                ModelState.AddModelError(string.Empty, "This barber already has an appointment that overlaps with the selected time.");
                return false;
            }

            return true;
        }

        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!IsAdmin && CurrentBarberId == null)
            {
                return Forbid();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            if (!IsAdmin && appointment.BarberId != CurrentBarberId)
            {
                return Forbid();
            }

            ViewData["BarberId"] = BuildBarberSelectList(appointment.BarberId);
            ViewData["ClientId"] = BuildClientSelectList(appointment.ClientId);
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", appointment.ServiceId);
            return View(appointment);
        }

        // POST: Appointments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientId,BarberId,ServiceId,StartTime,Status")] Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }

            if (!IsAdmin && CurrentBarberId == null)
            {
                return Forbid();
            }

            if (!IsAdmin)
            {
                var existingBarberId = await _context.Appointments
                    .Where(a => a.Id == id)
                    .Select(a => (int?)a.BarberId)
                    .FirstOrDefaultAsync();
                if (existingBarberId == null)
                {
                    return NotFound();
                }
                if (existingBarberId != CurrentBarberId)
                {
                    return Forbid();
                }

                // Never trust a client-submitted BarberId for a non-admin: it stays with the caller's own barber.
                appointment.BarberId = CurrentBarberId!.Value;
            }

            ModelState.Remove(nameof(Appointment.EndTime));

            if (ModelState.IsValid && await TrySetEndTimeAndValidateOverlap(appointment))
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BarberId"] = BuildBarberSelectList(appointment.BarberId);
            ViewData["ClientId"] = BuildClientSelectList(appointment.ClientId);
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", appointment.ServiceId);
            return View(appointment);
        }

        // GET: Appointments/Cancel/5
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Barber)
                .Include(a => a.Client)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            if (!IsAdmin && appointment.BarberId != CurrentBarberId)
            {
                return Forbid();
            }

            return View(appointment);
        }

        // POST: Appointments/Cancel/5
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            if (!IsAdmin && appointment.BarberId != CurrentBarberId)
            {
                return Forbid();
            }

            if (appointment.Status == AppointmentStatus.Scheduled)
            {
                appointment.Status = AppointmentStatus.Cancelled;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Barber)
                .Include(a => a.Client)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            if (appointment.Status == AppointmentStatus.Scheduled)
            {
                TempData["Error"] = "Scheduled appointments can't be deleted directly. Cancel it first.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            if (appointment.Status == AppointmentStatus.Scheduled)
            {
                TempData["Error"] = "Scheduled appointments can't be deleted directly. Cancel it first.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }
    }
}
