using System.ComponentModel.DataAnnotations;
using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Admin.Categories;

[Authorize(Roles = RoleNames.Admin)]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public EditModel(ApplicationDbContext db) => _db = db;

    [BindProperty]
    public Input InputModel { get; set; } = new();

    public class Input
    {
        [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var c = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();

        InputModel.Name = c.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
            return Page();

        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();

        c.Name = InputModel.Name.Trim();
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Category updated.";
        return RedirectToPage("Index");
    }
}
