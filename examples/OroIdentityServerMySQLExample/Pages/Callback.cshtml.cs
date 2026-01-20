using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OroIdentityServerExample.Pages;

public class CallbackModel : PageModel
{
    public string? Code { get; set; }
    public string? State { get; set; }
    public string? Error { get; set; }

    public void OnGet(string code, string state, string error)
    {
        Code = code;
        State = state;
        Error = error;
    }
}