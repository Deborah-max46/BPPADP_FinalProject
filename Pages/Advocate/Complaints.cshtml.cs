using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Advocate;

[Authorize(Roles = RoleNames.Advocate + "," + RoleNames.Admin)]
public class ComplaintsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public ComplaintsModel(ApplicationDbContext db) => _db = db;

    public string? Filter { get; set; }

    public IReadOnlyList<Row> Rows { get; set; } = Array.Empty<Row>();

    public record Row(
        int CaseNumber,
        string Title,
        string CategoryName,
        string? BusinessName,
        string ConsumerName,
        string? AdvocateName,
        bool Unassigned,
        ComplaintStatus Status,
        DateTime UpdatedAt);

    public async Task OnGetAsync(string? filter)
    {
        Filter = filter;
        var q = _db.Complaints.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var f = filter.Trim();
            q = q.Where(c =>
                c.Title.Contains(f) || c.Consumer.FullName.Contains(f) || c.Consumer.Email!.Contains(f) || c.Category.Name.Contains(f));
        }

        Rows = await q
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new Row(
                c.Id,
                c.Title,
                c.Category.Name,
                c.Business != null ? c.Business.Name : null,
                c.Consumer.FullName,
                c.AssignedAdvocate != null ? c.AssignedAdvocate.FullName : null,
                c.AssignedAdvocateId == null,
                c.Status,
                c.UpdatedAt))
            .ToListAsync();
    }
}
