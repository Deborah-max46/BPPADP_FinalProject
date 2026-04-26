using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Complaints;

[Authorize(Roles = RoleNames.Consumer)]
public class MineModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public MineModel(ApplicationDbContext db) => _db = db;

    public IReadOnlyList<ComplaintRow> Rows { get; set; } = Array.Empty<ComplaintRow>();

    public record ComplaintRow(int CaseNumber, string Title, string CategoryName, string? BusinessName, ComplaintStatus Status, DateTime UpdatedAt);

    public async Task OnGetAsync()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (uid == null) return;

        Rows = await _db.Complaints.AsNoTracking()
            .Where(c => c.ConsumerId == uid)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ComplaintRow(
                c.Id,
                c.Title,
                c.Category.Name,
                c.Business != null ? c.Business.Name : null,
                c.Status,
                c.UpdatedAt))
            .ToListAsync();
    }
}
