using OroIdentityServers;
using OroIdentityServers.Core;
using Moq;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace OroIdentityServers.Tests
{

public class UnitTest1
{
    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        // Arrange
        var tokenService = new TokenService("https://issuer", "api", "secretkey");
        var client = new Client { ClientId = "client1", ClientSecret = "secret" };

        // Act
        var token = tokenService.GenerateAccessToken(client, "user1", new[] { "api" });

        // Assert
        Assert.NotNull(token);
        Assert.True(token.Length > 0);
    }

    [Fact]
    public void GenerateAuthorizationCode_ShouldReturnUniqueCode()
    {
        // Arrange
        var tokenService = new TokenService("https://issuer", "api", "secretkey");

        // Act
        var code1 = tokenService.GenerateAuthorizationCode();
        var code2 = tokenService.GenerateAuthorizationCode();

        // Assert
        Assert.NotNull(code1);
        Assert.NotNull(code2);
        Assert.NotEqual(code1, code2);
    }

    [Fact]
    public async Task UserInfoEndpoint_WithValidBearerToken_ShouldReturnUserInfo()
    {
        // Arrange
        var userId = "user123";
        var username = "testuser";
        var userClaims = new List<Claim> { new Claim("email", "test@example.com") };
        var user = new User { Id = userId, Username = username, Claims = userClaims, PasswordHash = "hashedpassword" };

        var options = new IdentityServerOptions
        {
            Issuer = "https://testissuer",
            Audience = "testapi",
            SecretKey = "testsecretkey12345678901234567890"
        };

        // Generate a valid JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(options.SecretKey.PadRight(32, '0'));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, userId) }),
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);

        // Create HttpContext with the token
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/connect/userinfo";
        httpContext.Request.Method = "GET";
        httpContext.Request.Headers["Authorization"] = $"Bearer {jwtToken}";
        httpContext.Response.Body = new MemoryStream();

        // Create a simple test service provider
        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton<IUserStore>(new TestUserStore(user));
        services.AddSingleton(new TokenService(options.Issuer, options.Audience, options.SecretKey));
        var serviceProvider = services.BuildServiceProvider();

        // Create middleware
        RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;
        var middleware = new UserInfoEndpointMiddleware(next, serviceProvider);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal("application/json", httpContext.Response.ContentType);

        // Read response body
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = new StreamReader(httpContext.Response.Body).ReadToEnd();
        
        // Verify response contains expected values
        Assert.Contains($"\"sub\":\"{userId}\"", responseBody);
        Assert.Contains($"\"name\":\"{username}\"", responseBody);
        Assert.Contains("\"claims\":", responseBody);
    }

    [Fact]
    public async Task UserInfoEndpoint_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        var options = new IdentityServerOptions
        {
            Issuer = "https://testissuer",
            Audience = "testapi",
            SecretKey = "testsecretkey12345678901234567890"
        };

        // Create HttpContext with invalid token
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/connect/userinfo";
        httpContext.Request.Method = "GET";
        httpContext.Request.Headers["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyMTIzIiwibmJmIjoxNzY4ODc2NTMwLCJleHAiOjE3Njg4ODAxMzAsImlhdCI6MTc2ODg3NjUzMCwiaXNzIjoiaHR0cHM6Ly90ZXN0aXNzdWVyIiwiYXVkIjoidGVzdGFwaSJ9.invalid_signature";
        httpContext.Response.Body = new MemoryStream();

        // Create a simple test service provider
        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton<IUserStore>(new TestUserStore(null!)); // Not used in this test
        services.AddSingleton(new TokenService(options.Issuer, options.Audience, options.SecretKey));
        var serviceProvider = services.BuildServiceProvider();

        // Create middleware
        RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;
        var middleware = new UserInfoEndpointMiddleware(next, serviceProvider);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.Equal(401, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task UserInfoEndpoint_WithoutAuthorizationHeader_ShouldReturn401()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/connect/userinfo";
        httpContext.Request.Method = "GET";
        httpContext.Response.Body = new MemoryStream();
        // No Authorization header

        // Create a simple test service provider
        var services = new ServiceCollection();
        services.AddSingleton(new IdentityServerOptions { Issuer = "https://testissuer", Audience = "testapi", SecretKey = "testsecretkey12345678901234567890" });
        services.AddSingleton(new IdentityServerOptions { Issuer = "https://testissuer", Audience = "testapi", SecretKey = "testsecretkey12345678901234567890" });
        services.AddSingleton<IUserStore>(new TestUserStore(null!)); // Not used in this test
        services.AddSingleton(new TokenService("https://testissuer", "testapi", "testsecretkey12345678901234567890"));
        var serviceProvider = services.BuildServiceProvider();

        // Create middleware
        RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;
        var middleware = new UserInfoEndpointMiddleware(next, serviceProvider);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.Equal(401, httpContext.Response.StatusCode);
    }
}
}

public class TestUserStore : IUserStore
{
    private readonly User _user;

    public TestUserStore(User user)
    {
        _user = user;
    }

    public Task<IUser?> FindUserByIdAsync(string userId)
    {
        return Task.FromResult<IUser?>(_user.Id == userId ? _user : null);
    }

    public Task<IUser?> FindUserByUsernameAsync(string username)
    {
        return Task.FromResult<IUser?>(_user.Username == username ? _user : null);
    }
}

public class UserInfoResponse
{
    public string? sub { get; set; }
    public string? name { get; set; }
    public List<Claim>? claims { get; set; }
}
