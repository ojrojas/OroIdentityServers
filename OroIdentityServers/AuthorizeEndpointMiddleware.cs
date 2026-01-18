using Microsoft.AspNetCore.Http;
using OroIdentityServers.Core;

namespace OroIdentityServers;

public class AuthorizeEndpointMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClientStore _clientStore;
    private readonly IUserStore _userStore;
    private readonly IPersistedGrantStore _grantStore;
    private readonly TokenService _tokenService;

    public AuthorizeEndpointMiddleware(RequestDelegate next, IClientStore clientStore, IUserStore userStore, IPersistedGrantStore grantStore, TokenService tokenService)
    {
        _next = next;
        _clientStore = clientStore;
        _userStore = userStore;
        _grantStore = grantStore;
        _tokenService = tokenService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/connect/authorize" && context.Request.Method == "GET")
        {
            await HandleAuthorizeRequestAsync(context);
        }
        else
        {
            await _next(context);
        }
    }

    private async Task HandleAuthorizeRequestAsync(HttpContext context)
    {
        var query = context.Request.Query;
        var clientId = query["client_id"].ToString();
        var responseType = query["response_type"].ToString();
        var redirectUri = query["redirect_uri"].ToString();
        var scope = query["scope"].ToString();
        var state = query["state"].ToString();
        var codeChallenge = query["code_challenge"].ToString();
        var codeChallengeMethod = query["code_challenge_method"].ToString();

        var client = await _clientStore.FindClientByIdAsync(clientId);
        if (client == null || !client.RedirectUris.Contains(redirectUri) || responseType != "code")
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid request");
            return;
        }

        // Validate PKCE parameters if present
        if (!string.IsNullOrEmpty(codeChallenge))
        {
            if (string.IsNullOrEmpty(codeChallengeMethod))
            {
                codeChallengeMethod = "plain"; // default
            }
            else if (codeChallengeMethod != "S256" && codeChallengeMethod != "plain")
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid code_challenge_method");
                return;
            }
        }

        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return;
        }

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("User not authenticated");
            return;
        }

        var user = await _userStore.FindUserByIdAsync(userId);
        if (user == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("User not found");
            return;
        }

        var scopes = scope.Split(' ');
        var code = _tokenService.GenerateAuthorizationCode();
        await _grantStore.StoreAuthorizationCodeAsync(code, clientId, userId, redirectUri, scopes, codeChallenge, codeChallengeMethod);

        var redirectUrl = $"{redirectUri}?code={code}&state={state}";
        context.Response.Redirect(redirectUrl);
    }
}