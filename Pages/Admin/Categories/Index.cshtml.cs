using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Admin.Categories;

[Authorize(Roles = RoleNames.Admin)]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db) => _db = db;

    public IReadOnlyList<CategoryRow> Rows { get; set; } = Array.Empty<CategoryRow>();

    public record CategoryRow(int Id, string Name, int UsageCount);

    public async Task OnGetAsync()
    {
        Rows = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryRow(c.Id, c.Name, c.Complaints.Count()))
            .ToListAsync();
    }
}
