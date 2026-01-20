using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OroIdentityServers.Core;
using OroIdentityServers.OAuth;
using System.Text.Json;

namespace OroIdentityServers;

/// <summary>
/// Handler for OAuth 2.1 Token endpoint
/// </summary>
public class TokenEndpointHandler : IOAuthEndpointHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenEndpointHandler> _logger;

    public TokenEndpointHandler(IServiceProvider serviceProvider, ILogger<TokenEndpointHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task HandleAsync(object context)
    {
        var httpContext = (HttpContext)context;
        await HandleTokenRequestAsync(httpContext);
    }

    private async Task HandleTokenRequestAsync(HttpContext context)
    {
        using var scope = _serviceProvider.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var clientStore = scope.ServiceProvider.GetRequiredService<IClientStore>();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();
        var authService = scope.ServiceProvider.GetRequiredService<IUserAuthenticationService>();

        try
        {
            // Only accept POST requests
            if (!HttpMethods.IsPost(context.Request.Method))
            {
                await WriteErrorResponseAsync(context, "invalid_request", "Method not allowed", 405);
                return;
            }

            // Parse the token request
            var tokenRequest = await ParseTokenRequestAsync(context);
            if (tokenRequest == null)
            {
                await WriteErrorResponseAsync(context, "invalid_request", "Invalid request format");
                return;
            }

            // Validate client
            var client = await clientStore.FindClientByIdAsync(tokenRequest.ClientId);
            if (client == null)
            {
                await WriteErrorResponseAsync(context, "invalid_client", "Invalid client");
                return;
            }

            // Validate client secret if provided
            if (!string.IsNullOrEmpty(tokenRequest.ClientSecret) &&
                !await authService.ValidateClientSecretAsync(client, tokenRequest.ClientSecret))
            {
                await WriteErrorResponseAsync(context, "invalid_client", "Invalid client credentials");
                return;
            }

            // Handle different grant types
            switch (tokenRequest.GrantType?.ToLower())
            {
                case "client_credentials":
                    await HandleClientCredentialsGrantAsync(context, tokenService, client);
                    break;

                case "password":
                    await HandlePasswordGrantAsync(context, tokenService, userStore, authService, client, tokenRequest);
                    break;

                case "refresh_token":
                    await HandleRefreshTokenGrantAsync(context, tokenService, client, tokenRequest);
                    break;

                default:
                    await WriteErrorResponseAsync(context, "unsupported_grant_type", "Unsupported grant type");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing token request");
            await WriteErrorResponseAsync(context, "server_error", "Internal server error");
        }
    }

    private async Task<TokenRequest?> ParseTokenRequestAsync(HttpContext context)
    {
        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync();
            return new TokenRequest
            {
                GrantType = form["grant_type"].ToString(),
                ClientId = form["client_id"].ToString(),
                ClientSecret = form["client_secret"].ToString(),
                Username = form["username"].ToString(),
                Password = form["password"].ToString(),
                RefreshToken = form["refresh_token"].ToString(),
                Scope = form["scope"].ToString()
            };
        }
        else if (context.Request.ContentType?.Contains("application/json") == true)
        {
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            return JsonSerializer.Deserialize<TokenRequest>(requestBody);
        }

        return null;
    }

    private async Task HandleClientCredentialsGrantAsync(HttpContext context, ITokenService tokenService, Client client)
    {
        var tokenResponse = await tokenService.CreateAccessTokenAsync(client, null, "client_credentials");
        await WriteTokenResponseAsync(context, tokenResponse);
    }

    private async Task HandlePasswordGrantAsync(HttpContext context, ITokenService tokenService,
        IUserStore userStore, IUserAuthenticationService authService, Client client, TokenRequest tokenRequest)
    {
        if (string.IsNullOrEmpty(tokenRequest.Username) || string.IsNullOrEmpty(tokenRequest.Password))
        {
            await WriteErrorResponseAsync(context, "invalid_request", "Username and password required");
            return;
        }

        var user = await userStore.FindUserByUsernameAsync(tokenRequest.Username);
        if (user == null || !await authService.ValidatePasswordAsync(user, tokenRequest.Password))
        {
            await WriteErrorResponseAsync(context, "invalid_grant", "Invalid username or password");
            return;
        }

        var tokenResponse = await tokenService.CreateAccessTokenAsync(client, user, "password");
        await WriteTokenResponseAsync(context, tokenResponse);
    }

    private async Task HandleRefreshTokenGrantAsync(HttpContext context, ITokenService tokenService,
        Client client, TokenRequest tokenRequest)
    {
        if (string.IsNullOrEmpty(tokenRequest.RefreshToken))
        {
            await WriteErrorResponseAsync(context, "invalid_request", "Refresh token required");
            return;
        }

        var tokenResponse = await tokenService.RefreshAccessTokenAsync(tokenRequest.RefreshToken, client);
        if (tokenResponse == null)
        {
            await WriteErrorResponseAsync(context, "invalid_grant", "Invalid refresh token");
            return;
        }

        await WriteTokenResponseAsync(context, tokenResponse);
    }

    private async Task WriteTokenResponseAsync(HttpContext context, object tokenResponse)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync(JsonSerializer.Serialize(tokenResponse));
    }

    private async Task WriteErrorResponseAsync(HttpContext context, string error, string description, int statusCode = 400)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        var errorResponse = new
        {
            error,
            error_description = description
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}