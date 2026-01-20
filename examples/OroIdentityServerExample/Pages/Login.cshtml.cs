using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using OroIdentityServerExample;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace OroIdentityServerExample.Pages;

public class LoginModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public LoginModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Validate against database
        var userEntity = await _context.Users
            .Include(u => u.Claims)
            .FirstOrDefaultAsync(u => u.Username == Username && u.Enabled);

        if (userEntity != null && BCrypt.Net.BCrypt.Verify(Password, userEntity.PasswordHash))
        {
            var claims = userEntity.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();
            
            // Ensure NameIdentifier claim exists
            if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userEntity.Id.ToString()));
            }

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);

            return Redirect(ReturnUrl ?? "/");
        }

        ModelState.AddModelError("", "Invalid username or password");
        return Page();
    }
}