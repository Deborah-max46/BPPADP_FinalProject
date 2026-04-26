using System.ComponentModel.DataAnnotations;
using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Admin.Businesses;

[Authorize(Roles = RoleNames.Admin)]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [BindProperty]
    public Input InputModel { get; set; } = new();

    public class Input
    {
        [Required, StringLength(200)] public string Name { get; set; } = string.Empty;

        [Required, EmailAddress] public string ContactEmail { get; set; } = string.Empty;

        public string? Phone { get; set; }

        [Required, EmailAddress, Display(Name = "Owner account email")]
        public string OwnerEmail { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var owner = await _users.FindByEmailAsync(InputModel.OwnerEmail);
        if (owner == null)
        {
            ModelState.AddModelError(nameof(Input.OwnerEmail), "No user with that email.");
            return Page();
        }

        if (!await _users.IsInRoleAsync(owner, RoleNames.Business))
        {
            ModelState.AddModelError(nameof(Input.OwnerEmail), "User must have Business role.");
            return Page();
        }

        if (await _db.Businesses.AnyAsync(b => b.OwnerUserId == owner.Id))
        {
            ModelState.AddModelError(nameof(Input.OwnerEmail), "That user already owns a business.");
            return Page();
        }

        _db.Businesses.Add(new Models.Business
        {
            Name = InputModel.Name.Trim(),
            ContactEmail = InputModel.ContactEmail.Trim(),
            Phone = string.IsNullOrWhiteSpace(InputModel.Phone) ? null : InputModel.Phone.Trim(),
            OwnerUserId = owner.Id
        });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Business created.";
        return RedirectToPage("Index");
    }
}
