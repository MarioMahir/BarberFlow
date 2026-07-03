using BarberFlow.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BarberFlow.Web.Data;

public static class IdentitySeeder
{
    public const string AdminRole = "Admin";
    public const string BarberRole = "Barber";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AdminRole, BarberRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.Users.AnyAsync())
        {
            return;
        }

        var password = configuration["IdentitySeed:Password"];
        if (string.IsNullOrEmpty(password))
        {
            if (!environment.IsDevelopment())
            {
                return;
            }

            Console.WriteLine(
                "No IdentitySeed:Password configured - skipping login seeding. " +
                "Set one with: dotnet user-secrets set \"IdentitySeed:Password\" \"<password>\"");
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = "admin@barberflow.dev",
            Email = "admin@barberflow.dev",
            EmailConfirmed = true
        };
        var adminResult = await userManager.CreateAsync(admin, password);
        if (adminResult.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, AdminRole);
        }

        var context = services.GetRequiredService<BarberFlowDbContext>();
        var barbers = await context.Barbers.ToListAsync();
        foreach (var barber in barbers)
        {
            var barberUser = new ApplicationUser
            {
                UserName = barber.Email,
                Email = barber.Email,
                EmailConfirmed = true,
                BarberId = barber.Id
            };
            var result = await userManager.CreateAsync(barberUser, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(barberUser, BarberRole);
            }
        }
    }
}
