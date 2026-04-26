using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var context = provider.GetRequiredService<ApplicationDbContext>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = [RoleNames.Consumer, RoleNames.Advocate, RoleNames.Admin, RoleNames.Business];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Name = "Product quality" },
                new Category { Name = "Pricing / billing" },
                new Category { Name = "Service delivery" },
                new Category { Name = "Safety" },
                new Category { Name = "Other" });
            await context.SaveChangesAsync();
        }

        await EnsureUserAsync(userManager, context, "consumer@test.com", "Consumer User", "Password123!", RoleNames.Consumer);
        await EnsureUserAsync(userManager, context, "advocate@test.com", "Advocate User", "Password123!", RoleNames.Advocate);
        await EnsureUserAsync(userManager, context, "admin@test.com", "Admin User", "Password123!", RoleNames.Admin);
        var bizUser = await EnsureUserAsync(userManager, context, "business@test.com", "Shop Owner", "Password123!", RoleNames.Business);

        if (bizUser != null && !await context.Businesses.AnyAsync(b => b.OwnerUserId == bizUser.Id))
        {
            context.Businesses.Add(new Business
            {
                Name = "Demo Retail Ltd",
                ContactEmail = "contact@demoretail.test",
                Phone = "+250 788 000 000",
                OwnerUserId = bizUser.Id
            });
            await context.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        string email,
        string fullName,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
            return user;

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return null;

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
