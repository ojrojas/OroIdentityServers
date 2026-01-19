using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using OroIdentityServers.Core;
using OroIdentityServers.OAuth;
using Microsoft.Extensions.DependencyInjection;
using OroIdentityServers.EntityFramework.Events;

namespace OroIdentityServers;

public class TokenEndpointMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public TokenEndpointMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
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
        using var scope = _serviceProvider.CreateScope();
        var clientStore = scope.ServiceProvider.GetRequiredService<IClientStore>();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();
        var grantStore = scope.ServiceProvider.GetRequiredService<IPersistedGrantStore>();
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
        var form = await context.Request.ReadFormAsync();
        var grantType = form["grant_type"].ToString();
        var clientId = form["client_id"].ToString();
        var clientSecret = form["client_secret"].ToString();

        var client = await clientStore.FindClientByIdAsync(clientId);
        if (client == null || client.ClientSecret != clientSecret)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Invalid client");
            return;
        }

        switch (grantType)
        {
            case "client_credentials":
                await HandleClientCredentialsGrantAsync(context, client, _serviceProvider);
                break;
            case "authorization_code":
                await HandleAuthorizationCodeGrantAsync(context, client, form, _serviceProvider);
                break;
            case "refresh_token":
                await HandleRefreshTokenGrantAsync(context, client, form, _serviceProvider);
                break;
            case "password":
                await HandlePasswordGrantAsync(context, client, form, _serviceProvider);
                break;
            default:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync("Unsupported grant type");
                break;
        }
    }

    private async Task HandleClientCredentialsGrantAsync(HttpContext context, Client client, IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
        var eventPublisher = scope.ServiceProvider.GetService<IEventPublisher>();
        var token = tokenService.GenerateAccessToken(client, "client", client.AllowedScopes);
        var response = new
        {
            access_token = token,
            token_type = "Bearer",
            expires_in = 3600
        };
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));

        // Publish access token issued event
        if (eventPublisher != null)
        {
            var tokenEvent = new AccessTokenIssuedEvent
            {
                ClientId = client.ClientId,
                UserId = "client",
                Scopes = client.AllowedScopes.ToList(),
                TokenId = token,
                GrantType = "client_credentials",
                Timestamp = DateTime.UtcNow
            };
            await eventPublisher.PublishToExternalServicesAsync(tokenEvent);
        }
    }

    private async Task HandleAuthorizationCodeGrantAsync(HttpContext context, Client client, IFormCollection form, IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var grantStore = scope.ServiceProvider.GetRequiredService<IPersistedGrantStore>();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
        var code = form["code"].ToString();
        var redirectUri = form["redirect_uri"].ToString();
        var codeVerifier = form["code_verifier"].ToString();

        var grant = await grantStore.GetAuthorizationCodeAsync(code);
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

        await grantStore.RemoveAuthorizationCodeAsync(code);

        var user = await userStore.FindUserByIdAsync(grant.UserId);
        if (user == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync("Invalid user");
            return;
        }

        var accessToken = tokenService.GenerateAccessToken(client, user.Id, grant.Scopes);
        var idToken = tokenService.GenerateIdToken(user, client.ClientId, ""); // simplified nonce
        var refreshToken = tokenService.GenerateRefreshToken();

        await grantStore.StoreRefreshTokenAsync(refreshToken, client.ClientId, user.Id, grant.Scopes);

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

        // Publish token events
        var eventPublisher = scope.ServiceProvider.GetService<IEventPublisher>();
        if (eventPublisher != null)
        {
            // Access token issued event
            var accessTokenEvent = new AccessTokenIssuedEvent
            {
                ClientId = client.ClientId,
                UserId = user.Id,
                Scopes = grant.Scopes.ToList(),
                TokenId = accessToken,
                GrantType = "authorization_code",
                Timestamp = DateTime.UtcNow
            };
            await eventPublisher.PublishToExternalServicesAsync(accessTokenEvent);

            // Refresh token issued event
            var refreshTokenEvent = new RefreshTokenIssuedEvent
            {
                ClientId = client.ClientId,
                UserId = user.Id,
                Scopes = grant.Scopes.ToList(),
                TokenId = refreshToken,
                Timestamp = DateTime.UtcNow
            };
            await eventPublisher.PublishToExternalServicesAsync(refreshTokenEvent);
        }
    }

    private async Task HandleRefreshTokenGrantAsync(HttpContext context, Client client, IFormCollection form, IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var grantStore = scope.ServiceProvider.GetRequiredService<IPersistedGrantStore>();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
        var refreshToken = form["refresh_token"].ToString();

        var grant = await grantStore.GetRefreshTokenAsync(refreshToken);
        if (grant == null || grant.ClientId != client.ClientId)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync("Invalid refresh token");
            return;
        }

        var user = await userStore.FindUserByIdAsync(grant.UserId);
        if (user == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync("Invalid user");
            return;
        }

        var accessToken = tokenService.GenerateAccessToken(client, user.Id, grant.Scopes);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        await grantStore.RemoveRefreshTokenAsync(refreshToken);
        await grantStore.StoreRefreshTokenAsync(newRefreshToken, client.ClientId, user.Id, grant.Scopes);

        var response = new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600,
            refresh_token = newRefreshToken
        };
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));

        // Publish token events
        var eventPublisher = scope.ServiceProvider.GetService<IEventPublisher>();
        if (eventPublisher != null)
        {
            // Access token issued event
            var accessTokenEvent = new AccessTokenIssuedEvent
            {
                ClientId = client.ClientId,
                UserId = user.Id,
                Scopes = grant.Scopes.ToList(),
                TokenId = accessToken,
                GrantType = "refresh_token",
                Timestamp = DateTime.UtcNow
            };
            await eventPublisher.PublishToExternalServicesAsync(accessTokenEvent);

            // Refresh token issued event
            var refreshTokenEvent = new RefreshTokenIssuedEvent
            {
                ClientId = client.ClientId,
                UserId = user.Id,
                Scopes = grant.Scopes.ToList(),
                TokenId = newRefreshToken,
                Timestamp = DateTime.UtcNow
            };
            await eventPublisher.PublishToExternalServicesAsync(refreshTokenEvent);

            // Token revoked event for old refresh token
            var tokenRevokedEvent = new TokenRevokedEvent
            {
                ClientId = client.ClientId,
                UserId = user.Id,
                TokenId = refreshToken,
                TokenType = "refresh_token",
                Reason = "refresh_token_used",
                Timestamp = DateTime.UtcNow
            };
            await eventPublisher.PublishToExternalServicesAsync(tokenRevokedEvent);
        }
    }

    private async Task HandlePasswordGrantAsync(HttpContext context, Client client, IFormCollection form, IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
        var grantStore = scope.ServiceProvider.GetRequiredService<IPersistedGrantStore>();
        var username = form["username"].ToString();
        var password = form["password"].ToString();

        var user = await userStore.FindUserByUsernameAsync(username);
        if (user == null || !user.ValidatePassword(password))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Invalid credentials");
            return;
        }

        var accessToken = tokenService.GenerateAccessToken(client, user.Id, client.AllowedScopes);
        var refreshToken = tokenService.GenerateRefreshToken();

        await grantStore.StoreRefreshTokenAsync(refreshToken, client.ClientId, user.Id, client.AllowedScopes);

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