using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Admin.Businesses;

[Authorize(Roles = RoleNames.Admin)]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db) => _db = db;

    public IReadOnlyList<Row> Rows { get; set; } = Array.Empty<Row>();

    public record Row(int Id, string Name, string ContactEmail, string OwnerDisplayName, int ComplaintCount);

    public async Task OnGetAsync()
    {
        Rows = await _db.Businesses.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new Row(
                b.Id,
                b.Name,
                b.ContactEmail,
                b.Owner.FullName,
                b.Complaints.Count()))
            .ToListAsync();
    }
}
