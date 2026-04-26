using Microsoft.AspNetCore.Identity;

namespace ConsumersVoiceSystemPrototype.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
