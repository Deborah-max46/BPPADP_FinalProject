using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Admin.Businesses;

[Authorize(Roles = RoleNames.Admin)]
public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public DeleteModel(ApplicationDbContext db) => _db = db;

    public string? BusinessName { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var b = await _db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();

        if (await _db.Complaints.AnyAsync(c => c.BusinessId == id))
        {
            TempData["ErrorMessage"] = "Cannot delete a business referenced by complaints.";
            return RedirectToPage("Index");
        }

        BusinessName = b.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var b = await _db.Businesses.FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();

        if (await _db.Complaints.AnyAsync(c => c.BusinessId == id))
        {
            TempData["ErrorMessage"] = "Cannot delete a business referenced by complaints.";
            return RedirectToPage("Index");
        }

        _db.Businesses.Remove(b);
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Business deleted.";
        return RedirectToPage("Index");
    }
}
