using Microsoft.AspNetCore.Identity;

namespace UOM.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public bool MustChangePassword { get; set; }
}