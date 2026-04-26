using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Dashboard;

[Authorize(Roles = RoleNames.Advocate)]
public class AdvocateModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public AdvocateModel(ApplicationDbContext db) => _db = db;

    public int AssignedToMe { get; set; }
    public int AwaitingReview { get; set; }

    public async Task OnGetAsync()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (uid == null) return;

        AssignedToMe = await _db.Complaints.AsNoTracking().CountAsync(c => c.AssignedAdvocateId == uid);
        AwaitingReview = await _db.Complaints.AsNoTracking().CountAsync(c =>
            c.Status == ComplaintStatus.Submitted || c.Status == ComplaintStatus.UnderReview);
    }
}
