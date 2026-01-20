using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Headers;

namespace OroIdentityServerExample.Pages;

public class UserInfoModel : PageModel
{
    private readonly HttpClient _httpClient;

    public UserInfoModel(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string? UserInfo { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        var accessToken = TempData["AccessToken"] as string;

        if (string.IsNullOrEmpty(accessToken))
        {
            Error = "No access token found";
            return;
        }

        var userInfoEndpoint = "http://localhost:5160/connect/userinfo";
        var request = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            UserInfo = responseContent;
        }
        else
        {
            Error = responseContent;
        }
    }
}