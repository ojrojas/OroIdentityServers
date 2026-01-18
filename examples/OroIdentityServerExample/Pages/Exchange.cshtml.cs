using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text.Json;

namespace OroIdentityServerExample.Pages;

public class ExchangeModel : PageModel
{
    private readonly HttpClient _httpClient;

    public ExchangeModel(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Error { get; set; }
    public bool RequiresCodeVerifier { get; set; }
    public string? Code { get; set; }
    public string? State { get; set; }

    public async Task OnGetAsync(string code, string state)
    {
        Code = code;
        State = state;
        
        await ExchangeCodeForTokensAsync(code, state, null);
    }

    public async Task OnPostAsync(string code, string state, string codeVerifier)
    {
        Code = code;
        State = state;
        
        await ExchangeCodeForTokensAsync(code, state, codeVerifier);
    }

    private async Task ExchangeCodeForTokensAsync(string code, string state, string? codeVerifier)
    {
        var tokenEndpoint = "http://localhost:5160/connect/token";
        
        var formData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", "web-client"),
            new KeyValuePair<string, string>("client_secret", "web-secret"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:5160/callback")
        };

        if (!string.IsNullOrEmpty(codeVerifier))
        {
            formData.Add(new KeyValuePair<string, string>("code_verifier", codeVerifier));
        }

        var content = new FormUrlEncodedContent(formData);

        var response = await _httpClient.PostAsync(tokenEndpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
            AccessToken = tokenResponse?.access_token;
            IdToken = tokenResponse?.id_token;
            RefreshToken = tokenResponse?.refresh_token;

            // Store access token in TempData for next page
            TempData["AccessToken"] = AccessToken;
        }
        else
        {
            Error = responseContent;
            // Check if the error is about missing code_verifier
            if (responseContent.Contains("code_verifier required"))
            {
                RequiresCodeVerifier = true;
            }
        }
    }

    private class TokenResponse
    {
        public string? access_token { get; set; }
        public string? id_token { get; set; }
        public string? refresh_token { get; set; }
    }
}