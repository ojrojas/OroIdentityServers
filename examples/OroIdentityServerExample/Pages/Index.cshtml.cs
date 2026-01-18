using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OroIdentityServerExample.Pages;

public class IndexModel : PageModel
{
    public void OnGet()
    {

    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync("Cookies");
        return RedirectToPage();
    }
}
