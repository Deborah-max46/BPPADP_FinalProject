using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Admin.Categories;

[Authorize(Roles = RoleNames.Admin)]
public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public DeleteModel(ApplicationDbContext db) => _db = db;

    public string? Name { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var c = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();

        var usage = await _db.Complaints.CountAsync(x => x.CategoryId == id);
        if (usage > 0)
        {
            TempData["ErrorMessage"] = "Cannot delete a category that is in use.";
            return RedirectToPage("Index");
        }

        Name = c.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();

        if (await _db.Complaints.AnyAsync(x => x.CategoryId == id))
        {
            TempData["ErrorMessage"] = "Cannot delete a category that is in use.";
            return RedirectToPage("Index");
        }

        _db.Categories.Remove(c);
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Category deleted.";
        return RedirectToPage("Index");
    }
}
