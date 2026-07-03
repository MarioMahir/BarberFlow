using System.Security.Claims;
using BarberFlow.Web.Data;

namespace BarberFlow.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetBarberId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("BarberId");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(IdentitySeeder.AdminRole);
}
