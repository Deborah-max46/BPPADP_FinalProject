using System.ComponentModel.DataAnnotations;
using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConsumersVoiceSystemPrototype.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, Display(Name = "Full name"), StringLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password), Display(Name = "Confirm password")]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required, Display(Name = "Role")]
        public string Role { get; set; } = RoleNames.Consumer;

        [Display(Name = "Business name (if Business)")]
        public string? BusinessName { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var allowed = new[] { RoleNames.Consumer, RoleNames.Advocate, RoleNames.Admin, RoleNames.Business };
        if (!allowed.Contains(Input.Role))
        {
            ModelState.AddModelError(string.Empty, "Invalid role selected.");
            return Page();
        }

        if (Input.Role == RoleNames.Business && string.IsNullOrWhiteSpace(Input.BusinessName))
        {
            ModelState.AddModelError(nameof(Input.BusinessName), "Business name is required for business accounts.");
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            FullName = Input.FullName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return Page();
        }

        await _userManager.AddToRoleAsync(user, Input.Role);

        if (Input.Role == RoleNames.Business)
        {
            _db.Businesses.Add(new Business
            {
                Name = Input.BusinessName!.Trim(),
                ContactEmail = Input.Email,
                OwnerUserId = user.Id
            });
            await _db.SaveChangesAsync();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        return Input.Role switch
        {
            RoleNames.Consumer => RedirectToPage("/Dashboard/Consumer"),
            RoleNames.Advocate => RedirectToPage("/Dashboard/Advocate"),
            RoleNames.Business => RedirectToPage("/Dashboard/Business"),
            RoleNames.Admin => RedirectToPage("/Dashboard/Admin"),
            _ => RedirectToPage("/Index")
        };
    }
}
