using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Dashboard;

[Authorize(Roles = RoleNames.Admin)]
public class AdminModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public AdminModel(ApplicationDbContext db) => _db = db;

    public int TotalComplaints { get; set; }
    public int Categories { get; set; }
    public int Businesses { get; set; }

    public async Task OnGetAsync()
    {
        TotalComplaints = await _db.Complaints.AsNoTracking().CountAsync();
        Categories = await _db.Categories.AsNoTracking().CountAsync();
        Businesses = await _db.Businesses.AsNoTracking().CountAsync();
    }
}
