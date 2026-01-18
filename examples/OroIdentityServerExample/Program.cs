using OroIdentityServers;
using OroIdentityServers.Core;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddCors();
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
    })
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "http://localhost:5160";
        options.Audience = "api";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:5160",
            ValidateAudience = true,
            ValidAudience = "api",
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes("your-very-long-secret-key-at-least-32-characters-long"))
        };
    });

// Configure OroIdentityServer
builder.Services.AddOroIdentityServer(options =>
{
    options.Issuer = "http://localhost:5160";
    options.Audience = "api";
    options.SecretKey = "your-very-long-secret-key-at-least-32-characters-long";
    options.Clients = new List<Client>
    {
        new Client
        {
            ClientId = "web-client",
            ClientSecret = "web-secret",
            AllowedGrantTypes = new List<string> { "authorization_code", "refresh_token" },
            RedirectUris = new List<string> { "http://localhost:5160/callback" },
            AllowedScopes = new List<string> { "openid", "profile", "api" }
        },
        new Client
        {
            ClientId = "client-credentials-client",
            ClientSecret = "client-secret",
            AllowedGrantTypes = new List<string> { "client_credentials" },
            AllowedScopes = new List<string> { "api" }
        },
        new Client
        {
            ClientId = "password-client",
            ClientSecret = "password-secret",
            AllowedGrantTypes = new List<string> { "password", "refresh_token" },
            AllowedScopes = new List<string> { "openid", "profile", "api" }
        },
        new Client
        {
            ClientId = "implicit-client",
            ClientSecret = "implicit-secret",
            AllowedGrantTypes = new List<string> { "implicit" },
            RedirectUris = new List<string> { "http://localhost:5070/callback" },
            AllowedScopes = new List<string> { "openid", "profile", "api" }
        }
    };
    options.Users = new List<User>
    {
        new User
        {
            Id = "user1",
            Username = "alice",
            PasswordHash = "password",
            Claims = new List<Claim> { new Claim("name", "Alice"), new Claim("email", "alice@example.com") }
        }
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure CORS
app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

app.UseRouting();

app.UseAuthentication();
// Use OroIdentityServer middleware
app.UseOroIdentityServer();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// Add a simple protected API endpoint for testing
app.MapGet("/api/test", (HttpContext context) =>
{
    var user = context.User;
    if (user?.Identity?.IsAuthenticated != true)
    {
        context.Response.StatusCode = 401;
        return "Unauthorized";
    }

    var claims = user.Claims.Select(c => $"{c.Type}: {c.Value}");
    return $"Hello {user.Identity.Name}! Your claims: {string.Join(", ", claims)}";
}).RequireAuthorization(policy => policy.AddAuthenticationSchemes("Bearer").RequireAuthenticatedUser());

app.Run();
