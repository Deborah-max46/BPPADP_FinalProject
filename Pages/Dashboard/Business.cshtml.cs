using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Dashboard;

[Authorize(Roles = RoleNames.Business)]
public class BusinessModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public BusinessModel(ApplicationDbContext db) => _db = db;

    public string BusinessName { get; set; } = "";
    public int OpenAgainstBusiness { get; set; }

    public async Task OnGetAsync()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (uid == null) return;

        var biz = await _db.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.OwnerUserId == uid);
        if (biz == null) return;

        BusinessName = biz.Name;
        OpenAgainstBusiness = await _db.Complaints.AsNoTracking().CountAsync(c =>
            c.BusinessId == biz.Id && c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Closed);
    }
}
