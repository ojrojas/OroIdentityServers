using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using OroIdentityServers.Core;
using OroIdentityServers.OAuth;

namespace OroIdentityServers;

public class TokenEndpointMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClientStore _clientStore;
    private readonly IUserStore _userStore;
    private readonly IPersistedGrantStore _grantStore;
    private readonly TokenService _tokenService;

    public TokenEndpointMiddleware(RequestDelegate next, IClientStore clientStore, IUserStore userStore, IPersistedGrantStore grantStore, TokenService tokenService)
    {
        _next = next;
        _clientStore = clientStore;
        _userStore = userStore;
        _grantStore = grantStore;
        _tokenService = tokenService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/connect/token" && context.Request.Method == "POST")
        {
            await HandleTokenRequestAsync(context);
        }
        else
        {
            await _next(context);
        }
    }

    private async Task HandleTokenRequestAsync(HttpContext context)
    {
        var form = await context.Request.ReadFormAsync();
        var grantType = form["grant_type"].ToString();
        var clientId = form["client_id"].ToString();
        var clientSecret = form["client_secret"].ToString();

        var client = await _clientStore.FindClientByIdAsync(clientId);
        if (client == null || client.ClientSecret != clientSecret)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Invalid client");
            return;
        }

        switch (grantType)
        {
            case "client_credentials":
                await HandleClientCredentialsGrantAsync(context, client);
                break;
            case "authorization_code":
                await HandleAuthorizationCodeGrantAsync(context, client, form);
                break;
            case "refresh_token":
                await HandleRefreshTokenGrantAsync(context, client, form);
                break;
            case "password":
                await HandlePasswordGrantAsync(context, client, form);
                break;
            default:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync("Unsupported grant type");
                break;
        }
    }

    private async Task HandleClientCredentialsGrantAsync(HttpContext context, Client client)
    {
        var token = _tokenService.GenerateAccessToken(client, "client", client.AllowedScopes);
        var response = new
        {
            access_token = token,
            token_type = "Bearer",
            expires_in = 3600
        };
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task HandleAuthorizationCodeGrantAsync(HttpContext context, Client client, IFormCollection form)
    {
        var code = form["code"].ToString();
        var redirectUri = form["redirect_uri"].ToString();
        var codeVerifier = form["code_verifier"].ToString();

        var grant = await _grantStore.GetAuthorizationCodeAsync(code);
        if (grant == null || grant.ClientId != client.ClientId || grant.RedirectUri != redirectUri)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync("Invalid authorization code");
            return;
        }

        // Validate PKCE if code_challenge was provided
        if (!string.IsNullOrEmpty(grant.CodeChallenge))
        {
            if (string.IsNullOrEmpty(codeVerifier))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync("code_verifier required");
                return;
            }

            string expectedChallenge;
            if (grant.CodeChallengeMethod == "S256")
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(codeVerifier));
                expectedChallenge = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
            else // plain
            {
                expectedChallenge = codeVerifier;
            }

            if (expectedChallenge != grant.CodeChallenge)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync("Invalid code_verifier");
                return;
            }
        }

        await _grantStore.RemoveAuthorizationCodeAsync(code);

        var user = await _userStore.FindUserByIdAsync(grant.UserId);
        if (user == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync("Invalid user");
            return;
        }

        var accessToken = _tokenService.GenerateAccessToken(client, user.Id, grant.Scopes);
        var idToken = _tokenService.GenerateIdToken(user, client.ClientId, ""); // simplified nonce
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _grantStore.StoreRefreshTokenAsync(refreshToken, client.ClientId, user.Id, grant.Scopes);

        var response = new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600,
            id_token = idToken,
            refresh_token = refreshToken
        };
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task HandleRefreshTokenGrantAsync(HttpContext context, Client client, IFormCollection form)
    {
        var refreshToken = form["refresh_token"].ToString();

        var grant = await _grantStore.GetRefreshTokenAsync(refreshToken);
        if (grant == null || grant.ClientId != client.ClientId)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync("Invalid refresh token");
            return;
        }

        var user = await _userStore.FindUserByIdAsync(grant.UserId);
        if (user == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync("Invalid user");
            return;
        }

        var accessToken = _tokenService.GenerateAccessToken(client, user.Id, grant.Scopes);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        await _grantStore.RemoveRefreshTokenAsync(refreshToken);
        await _grantStore.StoreRefreshTokenAsync(newRefreshToken, client.ClientId, user.Id, grant.Scopes);

        var response = new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600,
            refresh_token = newRefreshToken
        };
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task HandlePasswordGrantAsync(HttpContext context, Client client, IFormCollection form)
    {
        var username = form["username"].ToString();
        var password = form["password"].ToString();

        var user = await _userStore.FindUserByUsernameAsync(username);
        if (user == null || !user.ValidatePassword(password))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Invalid credentials");
            return;
        }

        var accessToken = _tokenService.GenerateAccessToken(client, user.Id, client.AllowedScopes);
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _grantStore.StoreRefreshTokenAsync(refreshToken, client.ClientId, user.Id, client.AllowedScopes);

        var response = new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600,
            refresh_token = refreshToken
        };
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}