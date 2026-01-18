using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

class Program
{
    private static readonly HttpClient _httpClient = new HttpClient();

    static async Task Main(string[] args)
    {
        Console.WriteLine("OAuth 2.0 Password Grant Flow Example");
        Console.WriteLine("=====================================");

        // Client configuration
        const string tokenEndpoint = "http://localhost:5160/connect/token";
        const string clientId = "password-client";
        const string clientSecret = "password-secret";
        const string username = "alice";
        const string password = "password";

        try
        {
            // Paso 1: Obtener token usando password grant
            Console.WriteLine("\n1. Requesting access token using password grant...");

            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["username"] = username,
                ["password"] = password,
                ["scope"] = "api1"
            };

            var tokenResponse = await RequestTokenAsync(tokenEndpoint, tokenRequest);
            Console.WriteLine($"Token Response: {JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions { WriteIndented = true })}");

            if (tokenResponse.ContainsKey("access_token"))
            {
                var accessToken = tokenResponse["access_token"].ToString();

                // Paso 2: Usar el token para acceder a una API protegida
                Console.WriteLine("\n2. Using access token to call protected API...");

                var apiResponse = await CallProtectedApiAsync("http://localhost:5160/api/test", accessToken);
                Console.WriteLine($"API Response: {apiResponse}");

                // Paso 3: Usar refresh token para obtener un nuevo access token
                if (tokenResponse.ContainsKey("refresh_token"))
                {
                    Console.WriteLine("\n3. Refreshing access token...");

                    var refreshRequest = new Dictionary<string, string>
                    {
                        ["grant_type"] = "refresh_token",
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["refresh_token"] = tokenResponse["refresh_token"].ToString()
                    };

                    var refreshResponse = await RequestTokenAsync(tokenEndpoint, refreshRequest);
                    Console.WriteLine($"Refresh Response: {JsonSerializer.Serialize(refreshResponse, new JsonSerializerOptions { WriteIndented = true })}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task<Dictionary<string, object>> RequestTokenAsync(string endpoint, Dictionary<string, string> parameters)
    {
        var content = new FormUrlEncodedContent(parameters);
        var response = await _httpClient.PostAsync(endpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Token request failed: {response.StatusCode} - {responseContent}");
        }

        return JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent)
               ?? throw new Exception("Failed to deserialize token response");
    }

    private static async Task<string> CallProtectedApiAsync(string apiUrl, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}