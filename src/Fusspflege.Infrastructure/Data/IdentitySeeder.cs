using Fusspflege.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fusspflege.Infrastructure.Data;

public static class IdentitySeeder
{
    // Legt die Rolle und, falls konfiguriert, den ersten Admin an. Ein vorhandener Benutzer wird nie verändert.
    public static async Task SeedAdminAsync(IServiceProvider services, string? email, string? password)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        if (!await roles.RoleExistsAsync(AppRoles.Admin))
            await roles.CreateAsync(new IdentityRole(AppRoles.Admin));

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        if (await users.FindByEmailAsync(email) is not null)
            return;

        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            logger.LogError("Admin-Benutzer konnte nicht angelegt werden: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await users.AddToRoleAsync(user, AppRoles.Admin);
        logger.LogInformation("Admin-Benutzer {Email} angelegt", email);
    }
}