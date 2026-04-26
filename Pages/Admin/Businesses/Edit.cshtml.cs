using System.ComponentModel.DataAnnotations;
using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Admin.Businesses;

[Authorize(Roles = RoleNames.Admin)]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public EditModel(ApplicationDbContext db) => _db = db;

    [BindProperty]
    public Input InputModel { get; set; } = new();

    public string OwnerDisplay { get; set; } = "";

    public class Input
    {
        [Required, StringLength(200)] public string Name { get; set; } = string.Empty;

        [Required, EmailAddress] public string ContactEmail { get; set; } = string.Empty;

        public string? Phone { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var b = await _db.Businesses.Include(x => x.Owner).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();

        InputModel.Name = b.Name;
        InputModel.ContactEmail = b.ContactEmail;
        InputModel.Phone = b.Phone;
        OwnerDisplay = $"{b.Owner.FullName} ({b.Owner.Email})";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
            return Page();

        var b = await _db.Businesses.Include(x => x.Owner).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();

        OwnerDisplay = $"{b.Owner.FullName} ({b.Owner.Email})";
        b.Name = InputModel.Name.Trim();
        b.ContactEmail = InputModel.ContactEmail.Trim();
        b.Phone = string.IsNullOrWhiteSpace(InputModel.Phone) ? null : InputModel.Phone.Trim();
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Business updated.";
        return RedirectToPage("Index");
    }
}
