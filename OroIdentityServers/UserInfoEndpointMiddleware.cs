using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace OroIdentityServers;

public class UserInfoEndpointMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public UserInfoEndpointMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/connect/userinfo" && (context.Request.Method == "GET" || context.Request.Method == "POST"))
        {
            await HandleUserInfoRequestAsync(context);
        }
        else
        {
            await _next(context);
        }
    }

    private async Task HandleUserInfoRequestAsync(HttpContext context)
    {
        using var scope = _serviceProvider.CreateScope();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();
        var options = scope.ServiceProvider.GetRequiredService<IdentityServerOptions>();

        // First, try to authenticate via session/cookies (for web applications)
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = ExtractUserIdFromClaims(context.User);
            if (!string.IsNullOrEmpty(userId))
            {
                await ReturnUserInfoAsync(context, userStore, userId);
                return;
            }
        }

        // If not authenticated via session, try Bearer token authentication
        var token = ExtractAccessToken(context);
        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        // Validate the JWT token
        var userIdFromToken = await ValidateTokenAndExtractUserIdAsync(token, options, userStore);
        if (string.IsNullOrEmpty(userIdFromToken))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await ReturnUserInfoAsync(context, userStore, userIdFromToken);
    }

    private string ExtractUserIdFromClaims(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value
               ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    }

    private string ExtractAccessToken(HttpContext context)
    {
        // Check Authorization header first
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        // Check query parameter
        var token = context.Request.Query["access_token"].ToString();
        if (!string.IsNullOrEmpty(token))
        {
            return token;
        }

        // Check form body (for POST requests)
        if (context.Request.HasFormContentType)
        {
            var form = context.Request.ReadFormAsync().Result;
            token = form["access_token"].ToString();
            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }
        }

        return null;
    }

    private async Task<string> ValidateTokenAndExtractUserIdAsync(string token, IdentityServerOptions options, IUserStore userStore)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(options.SecretKey.PadRight(32, '0')))
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userId = ExtractUserIdFromClaims(principal);

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            // Verify user exists
            var user = await userStore.FindUserByIdAsync(userId);
            return user != null ? userId : null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    private async Task ReturnUserInfoAsync(HttpContext context, IUserStore userStore, string userId)
    {
        var user = await userStore.FindUserByIdAsync(userId);
        if (user == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("User not found");
            return;
        }

        var userInfo = new
        {
            sub = user.Id,
            name = user.Username,
            claims = user.Claims
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(userInfo));
    }
}