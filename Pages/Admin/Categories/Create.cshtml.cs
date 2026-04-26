using System.ComponentModel.DataAnnotations;
using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConsumersVoiceSystemPrototype.Pages.Admin.Categories;

[Authorize(Roles = RoleNames.Admin)]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public CreateModel(ApplicationDbContext db) => _db = db;

    [BindProperty]
    public Input InputModel { get; set; } = new();

    public class Input
    {
        [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        _db.Categories.Add(new Category { Name = InputModel.Name.Trim() });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Category created.";
        return RedirectToPage("Index");
    }
}
