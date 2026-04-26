using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConsumersVoiceSystemPrototype.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.IsInRole(RoleNames.Consumer))
            return RedirectToPage("/Dashboard/Consumer");
        if (User.IsInRole(RoleNames.Advocate))
            return RedirectToPage("/Dashboard/Advocate");
        if (User.IsInRole(RoleNames.Business))
            return RedirectToPage("/Dashboard/Business");
        if (User.IsInRole(RoleNames.Admin))
            return RedirectToPage("/Dashboard/Admin");
        return RedirectToPage("/Index");
    }
}
