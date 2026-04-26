using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Notifications;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public IReadOnlyList<NotificationRow> Items { get; set; } = Array.Empty<NotificationRow>();

    public record NotificationRow(int Id, string Message, bool IsRead, DateTime CreatedAt, int? ComplaintId);

    public async Task OnGetAsync()
    {
        var uid = _users.GetUserId(User);
        if (uid == null) return;

        Items = await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == uid)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationRow(n.Id, n.Message, n.IsRead, n.CreatedAt, n.ComplaintId))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostMarkOneAsync(int id)
    {
        var uid = _users.GetUserId(User);
        if (uid == null) return Unauthorized();

        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
        if (n == null) return NotFound();

        n.IsRead = true;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllAsync()
    {
        var uid = _users.GetUserId(User);
        if (uid == null) return Unauthorized();

        await _db.Notifications
            .Where(n => n.UserId == uid && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return RedirectToPage();
    }
}
