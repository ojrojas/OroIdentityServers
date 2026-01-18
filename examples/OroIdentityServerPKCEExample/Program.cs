using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

class Program
{
    private static readonly HttpClient _httpClient = new HttpClient();

    static async Task Main(string[] args)
    {
        Console.WriteLine("OAuth 2.1 Authorization Code Flow with PKCE Example");
        Console.WriteLine("=================================================");

        // Client configuration
        const string authorizeEndpoint = "http://localhost:5160/connect/authorize";
        const string tokenEndpoint = "http://localhost:5160/connect/token";
        const string clientId = "web-client";
        const string redirectUri = "http://localhost:5160/callback";

        try
        {
            // Step 1: Generate PKCE parameters
            Console.WriteLine("\n1. Generating PKCE parameters...");
            var (codeVerifier, codeChallenge) = GeneratePKCE();
            Console.WriteLine($"Code Verifier: {codeVerifier}");
            Console.WriteLine($"Code Challenge: {codeChallenge}");

            // Step 2: Build authorization URL (simulated - in real app would open browser)
            Console.WriteLine("\n2. Authorization URL (would open in browser):");
            var authUrl = $"{authorizeEndpoint}?response_type=code&client_id={clientId}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&scope=openid%20profile%20api&state=xyz&code_challenge={codeChallenge}&code_challenge_method=S256";
            Console.WriteLine(authUrl);

            // Step 3: Simulate user authentication and authorization code reception
            // In real app, this would come from browser callback
            Console.WriteLine("\n3. Simulating user authentication and authorization code reception...");
            Console.WriteLine("   (In real app: user logs in, grants consent, browser redirects to callback with code)");
            Console.WriteLine("   ");
            Console.WriteLine("   RECOMMENDED: Use the web interface!");
            Console.WriteLine("   1. Copy the authorization URL above");
            Console.WriteLine("   2. Open it in your browser and complete login");
            Console.WriteLine("   3. On the callback page, click 'Exchange Code for Tokens'");
            Console.WriteLine("   4. When PKCE form appears, paste this code_verifier:");
            Console.WriteLine($"      {codeVerifier}");
            Console.WriteLine("   5. You'll get the tokens!");
            Console.WriteLine("   ");
            Console.WriteLine("   ALTERNATIVE: Manual code entry");
            Console.Write("   Enter the authorization code manually: ");
            var authorizationCode = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(authorizationCode))
            {
                Console.WriteLine("No authorization code provided. Exiting.");
                return;
            }

            // Step 4: Exchange code for tokens using PKCE
            Console.WriteLine("\n4. Exchanging authorization code for tokens with PKCE...");

            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId,
                ["code"] = authorizationCode,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = codeVerifier
            };

            var tokenResponse = await RequestTokenAsync(tokenEndpoint, tokenRequest);
            Console.WriteLine($"Token Response: {JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions { WriteIndented = true })}");

            if (tokenResponse.ContainsKey("access_token"))
            {
                var accessToken = tokenResponse["access_token"].ToString();

                // Step 5: Use access token to call protected API
                Console.WriteLine("\n5. Using access token to call protected API...");

                var apiResponse = await CallProtectedApiAsync("http://localhost:5160/api/test", accessToken);
                Console.WriteLine($"API Response: {apiResponse}");

                // Step 6: Use refresh token to get new access token
                if (tokenResponse.ContainsKey("refresh_token"))
                {
                    Console.WriteLine("\n6. Refreshing access token...");

                    var refreshRequest = new Dictionary<string, string>
                    {
                        ["grant_type"] = "refresh_token",
                        ["client_id"] = clientId,
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

    private static (string codeVerifier, string codeChallenge) GeneratePKCE()
    {
        // Generate code_verifier: random string of 43-128 characters
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        var codeVerifier = Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        // Generate code_challenge: SHA256 hash of code_verifier, base64url encoded
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        var codeChallenge = Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return (codeVerifier, codeChallenge);
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