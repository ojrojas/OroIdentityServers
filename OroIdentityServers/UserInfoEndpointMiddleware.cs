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
        if (context.Request.Path == "/connect/userinfo" && context.Request.Method == "GET")
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
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        var token = authHeader.Substring("Bearer ".Length);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid token");
            return;
        }

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