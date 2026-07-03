using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BarberFlow.Web.Data;
using BarberFlow.Web.Models.Entities;

namespace BarberFlow.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BarberWorkingHoursController : Controller
    {
        private readonly BarberFlowDbContext _context;

        public BarberWorkingHoursController(BarberFlowDbContext context)
        {
            _context = context;
        }

        // GET: BarberWorkingHours?barberId=5
        public async Task<IActionResult> Index(int barberId)
        {
            var barber = await _context.Barbers.FindAsync(barberId);
            if (barber == null)
            {
                return NotFound();
            }

            ViewData["Barber"] = barber;

            var hours = await _context.BarberWorkingHours
                .Where(w => w.BarberId == barberId)
                .OrderBy(w => w.DayOfWeek)
                .ThenBy(w => w.StartTime)
                .ToListAsync();

            return View(hours);
        }

        // GET: BarberWorkingHours/Create?barberId=5
        public async Task<IActionResult> Create(int barberId)
        {
            var barber = await _context.Barbers.FindAsync(barberId);
            if (barber == null)
            {
                return NotFound();
            }

            ViewData["Barber"] = barber;
            return View(new BarberWorkingHour { BarberId = barberId });
        }

        // POST: BarberWorkingHours/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BarberId,DayOfWeek,StartTime,EndTime")] BarberWorkingHour workingHour)
        {
            if (workingHour.EndTime <= workingHour.StartTime)
            {
                ModelState.AddModelError(nameof(BarberWorkingHour.EndTime), "End time must be after start time.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(workingHour);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { barberId = workingHour.BarberId });
            }

            var barber = await _context.Barbers.FindAsync(workingHour.BarberId);
            if (barber == null)
            {
                return NotFound();
            }
            ViewData["Barber"] = barber;
            return View(workingHour);
        }

        // GET: BarberWorkingHours/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workingHour = await _context.BarberWorkingHours
                .Include(w => w.Barber)
                .FirstOrDefaultAsync(w => w.Id == id);
            if (workingHour == null)
            {
                return NotFound();
            }

            return View(workingHour);
        }

        // POST: BarberWorkingHours/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var workingHour = await _context.BarberWorkingHours.FindAsync(id);
            if (workingHour == null)
            {
                return NotFound();
            }

            var barberId = workingHour.BarberId;
            _context.BarberWorkingHours.Remove(workingHour);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { barberId });
        }
    }
}
