using ConsumersVoiceSystemPrototype.Models;
using ConsumersVoiceSystemPrototype.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConsumersVoiceSystemPrototype.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IndexModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public IActionResult OnGet()
    {
        if (!_signInManager.IsSignedIn(User))
            return Page();
        var path = UserDashboardRedirect.GetDashboardPathFromPrincipal(User);
        // Signed in but no known role: show landing (matches /Dashboard/Index fallback to /Index).
        if (path == "~/Dashboard/Index")
            return Page();
        return LocalRedirect(Url.Content(path));
    }
}
