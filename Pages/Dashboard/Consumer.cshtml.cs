using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Dashboard;

[Authorize(Roles = RoleNames.Consumer)]
public class ConsumerModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public ConsumerModel(ApplicationDbContext db) => _db = db;

    public int OpenCount { get; set; }
    public int ResolvedCount { get; set; }

    public async Task OnGetAsync()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (uid == null) return;

        OpenCount = await _db.Complaints.AsNoTracking().CountAsync(c =>
            c.ConsumerId == uid && c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Closed);

        ResolvedCount = await _db.Complaints.AsNoTracking().CountAsync(c =>
            c.ConsumerId == uid && (c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed));
    }
}
