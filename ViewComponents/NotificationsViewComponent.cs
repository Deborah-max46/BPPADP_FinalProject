using ConsumersVoiceSystemPrototype.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsumersVoiceSystemPrototype.Models;

namespace ConsumersVoiceSystemPrototype.ViewComponents;

public class NotificationsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsViewComponent(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return View(new NotificationsVm(null, 0));

        var userId = _userManager.GetUserId(UserClaimsPrincipal);
        if (userId == null)
            return View(new NotificationsVm(null, 0));

        var list = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(8)
            .ToListAsync();

        var unread = await _db.Notifications.AsNoTracking().CountAsync(n => n.UserId == userId && !n.IsRead);

        return View(new NotificationsVm(list, unread));
    }

    public record NotificationsVm(IReadOnlyList<Notification>? Items, int UnreadCount);
}
