using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OroIdentityServerExample.Pages;

[Authorize]
public class ProfileModel : PageModel
{
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public Dictionary<string, string> Claims { get; set; } = new();

    public void OnGet()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Username = User.Identity.Name;

            foreach (var claim in User.Claims)
            {
                Claims[claim.Type] = claim.Value;
            }
        }
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync("Cookies");
        return RedirectToPage("/Index");
    }
}