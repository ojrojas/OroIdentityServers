using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OroIdentityServerClientCredentialsExample.Services;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;

    public AuthenticationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

    public string? AccessToken => _accessToken;

    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            var tokenEndpoint = "http://localhost:5160/connect/token";
            Console.WriteLine($"Requesting token from: {tokenEndpoint}");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", "client-credentials-client"),
                new KeyValuePair<string, string>("client_secret", "client-secret"),
                new KeyValuePair<string, string>("scope", "api")
            });

            Console.WriteLine("Sending POST request to token endpoint...");
            var response = await _httpClient.PostAsync(tokenEndpoint, content);
            Console.WriteLine($"Response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response content: {responseContent}");
                
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
                Console.WriteLine($"Deserialized token: {tokenResponse?.AccessToken}");
                
                _accessToken = tokenResponse?.AccessToken;
                Console.WriteLine($"Token received: {!string.IsNullOrEmpty(_accessToken)}");
                
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Token is null or empty despite successful response");
                    return false;
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error response: {errorContent}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in AuthenticateAsync: {ex.Message}");
            return false;
        }
    }

    public void Logout()
    {
        _accessToken = null;
    }

    public async Task<string?> CallProtectedApiAsync(string endpoint)
    {
        if (!IsAuthenticated)
            return null;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:5160{endpoint}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return $"Error: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"Exception: {ex.Message}";
        }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}