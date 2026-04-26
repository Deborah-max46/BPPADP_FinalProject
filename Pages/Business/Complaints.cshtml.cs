using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Business;

[Authorize(Roles = RoleNames.Business)]
public class ComplaintsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ComplaintsModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public IReadOnlyList<Row> Rows { get; set; } = Array.Empty<Row>();

    public record Row(int CaseNumber, string Title, string CategoryName, string ConsumerName, ComplaintStatus Status, DateTime UpdatedAt);

    public async Task OnGetAsync()
    {
        var uid = _users.GetUserId(User);
        if (uid == null) return;

        var biz = await _db.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.OwnerUserId == uid);
        if (biz == null) return;

        Rows = await _db.Complaints.AsNoTracking()
            .Where(c => c.BusinessId == biz.Id)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new Row(
                c.Id,
                c.Title,
                c.Category.Name,
                c.Consumer.FullName,
                c.Status,
                c.UpdatedAt))
            .ToListAsync();
    }
}
