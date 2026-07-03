using System.Diagnostics;
using BarberFlow.Web.Data;
using BarberFlow.Web.Extensions;
using BarberFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberFlow.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BarberFlowDbContext _context;

        public HomeController(ILogger<HomeController> logger, BarberFlowDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return View("Landing");
            }

            var isAdmin = User.IsAdmin();
            var currentBarberId = User.GetBarberId();
            if (!isAdmin && currentBarberId == null)
            {
                return Forbid();
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var weekEnd = today.AddDays(7);

            var appointments = _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Barber)
                .Include(a => a.Service)
                .AsQueryable();

            if (!isAdmin)
            {
                appointments = appointments.Where(a => a.BarberId == currentBarberId);
            }

            var weekCountQuery = _context.Appointments.Where(a => a.StartTime >= today && a.StartTime < weekEnd);
            if (!isAdmin)
            {
                weekCountQuery = weekCountQuery.Where(a => a.BarberId == currentBarberId);
            }

            var viewModel = new HomeDashboardViewModel
            {
                TodayAppointments = await appointments
                    .Where(a => a.StartTime >= today && a.StartTime < tomorrow)
                    .OrderBy(a => a.StartTime)
                    .ToListAsync(),

                UpcomingAppointments = await appointments
                    .Where(a => a.StartTime >= tomorrow && a.StartTime < weekEnd)
                    .OrderBy(a => a.StartTime)
                    .Take(5)
                    .ToListAsync(),

                WeekCount = await weekCountQuery.CountAsync(),
                ActiveClientCount = await _context.Clients.CountAsync(),
                BarberCount = await _context.Barbers.CountAsync()
            };
            viewModel.TodayCount = viewModel.TodayAppointments.Count;

            return View(viewModel);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
