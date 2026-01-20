using OroIdentityServers;
using OroIdentityServers.EntityFramework.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore.Sqlite;
using OroIdentityServerExample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddHttpClient();
        builder.Services.AddCors();

        // Configure database
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=OroIdentityServerExample.db";

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        // Configure OroIdentityServer with Entity Framework
        builder.Services.AddOroIdentityServerDbContext<ApplicationDbContext>();
        builder.Services.AddEntityFrameworkStores();
        builder.Services.AddConfigurationEvents();
        builder.Services.AddAutomaticMigrations<ApplicationDbContext>();
        builder.Services.AddTokenCleanupService();

        // Configure event-driven architecture
        builder.Services.AddInMemoryEventBus();
        builder.Services.AddEventStore();

        // Configure encryption service for client secrets (optional but recommended)
        builder.Services.AddEncryptionService("your-secure-encryption-key-change-this-in-production");

        // Configure IdentityServer options
        builder.Services.AddSingleton(new IdentityServerOptions
        {
            Issuer = "http://localhost:5160",
            Audience = "api",
            SecretKey = "your-very-long-secret-key-at-least-32-characters-long"
        });

        // Register TokenService
        builder.Services.AddSingleton<TokenService>(sp =>
        {
            var options = sp.GetRequiredService<IdentityServerOptions>();
            return new TokenService(options.Issuer, options.Audience, options.SecretKey);
        });

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

        // Configurar políticas de autorización
        builder.Services.AddAuthorization(options =>
        {
            // Política por defecto - usa Cookies para aplicaciones web
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Cookies")
                .RequireAuthenticatedUser()
                .Build();

            // Política para APIs - usa Bearer tokens
            options.AddPolicy("ApiPolicy", policy =>
                policy.AddAuthenticationSchemes("Bearer")
                      .RequireAuthenticatedUser());

            // Política flexible - permite tanto Cookies como Bearer
            options.AddPolicy("FlexiblePolicy", policy =>
                policy.AddAuthenticationSchemes("Cookies", "Bearer")
                      .RequireAuthenticatedUser());
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
        }).RequireAuthorization("FlexiblePolicy");

        app.Run();
    }
}

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Seed identity resources
        if (!context.IdentityResources.Any())
        {
            context.IdentityResources.AddRange(
                new OroIdentityServers.EntityFramework.Entities.IdentityResourceEntity
                {
                    TenantId = "default",
                    Name = "openid",
                    DisplayName = "OpenID",
                    Description = "OpenID Connect",
                    Enabled = true,
                    ShowInDiscoveryDocument = true,
                    Created = DateTime.UtcNow
                },
                new OroIdentityServers.EntityFramework.Entities.IdentityResourceEntity
                {
                    TenantId = "default",
                    Name = "profile",
                    DisplayName = "Profile",
                    Description = "User profile information",
                    Enabled = true,
                    ShowInDiscoveryDocument = true,
                    Created = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
        }

        // Seed API scopes
        if (!context.ApiScopes.Any())
        {
            context.ApiScopes.Add(new OroIdentityServers.EntityFramework.Entities.ApiScopeEntity
            {
                Name = "api",
                DisplayName = "API Access",
                Description = "Access to API",
                ShowInDiscoveryDocument = true,
                Created = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // Seed clients
        if (!context.Clients.Any())
        {
            var webClient = new OroIdentityServers.EntityFramework.Entities.ClientEntity
            {
                TenantId = "default",
                ClientId = "web-client",
                ClientSecret = "web-secret",
                ClientName = "Web Client",
                Enabled = true,
                Created = DateTime.UtcNow
            };
            context.Clients.Add(webClient);
            await context.SaveChangesAsync();

            // Add grant types and scopes
            context.ClientGrantTypes.AddRange(
                new OroIdentityServers.EntityFramework.Entities.ClientGrantTypeEntity { ClientId = webClient.Id, GrantType = "authorization_code" },
                new OroIdentityServers.EntityFramework.Entities.ClientGrantTypeEntity { ClientId = webClient.Id, GrantType = "refresh_token" }
            );
            context.ClientScopes.AddRange(
                new OroIdentityServers.EntityFramework.Entities.ClientScopeEntity { ClientId = webClient.Id, Scope = "openid" },
                new OroIdentityServers.EntityFramework.Entities.ClientScopeEntity { ClientId = webClient.Id, Scope = "profile" },
                new OroIdentityServers.EntityFramework.Entities.ClientScopeEntity { ClientId = webClient.Id, Scope = "api" }
            );
            context.ClientRedirectUris.Add(
                new OroIdentityServers.EntityFramework.Entities.ClientRedirectUriEntity { ClientId = webClient.Id, RedirectUri = "http://localhost:5160/callback" }
            );

            var apiClient = new OroIdentityServers.EntityFramework.Entities.ClientEntity
            {
                TenantId = "default",
                ClientId = "client-credentials-client",
                ClientSecret = "client-secret",
                ClientName = "Client Credentials Client",
                Enabled = true,
                Created = DateTime.UtcNow
            };
            context.Clients.Add(apiClient);
            await context.SaveChangesAsync();

            context.ClientGrantTypes.Add(
                new OroIdentityServers.EntityFramework.Entities.ClientGrantTypeEntity { ClientId = apiClient.Id, GrantType = "client_credentials" }
            );
            context.ClientScopes.Add(
                new OroIdentityServers.EntityFramework.Entities.ClientScopeEntity { ClientId = apiClient.Id, Scope = "api" }
            );

            var passwordClient = new OroIdentityServers.EntityFramework.Entities.ClientEntity
            {
                TenantId = "default",
                ClientId = "password-client",
                ClientSecret = "password-secret",
                ClientName = "Password Client",
                Enabled = true,
                Created = DateTime.UtcNow
            };
            context.Clients.Add(passwordClient);
            await context.SaveChangesAsync();

            context.ClientGrantTypes.AddRange(
                new OroIdentityServers.EntityFramework.Entities.ClientGrantTypeEntity { ClientId = passwordClient.Id, GrantType = "password" },
                new OroIdentityServers.EntityFramework.Entities.ClientGrantTypeEntity { ClientId = passwordClient.Id, GrantType = "refresh_token" }
            );
            context.ClientScopes.AddRange(
                new OroIdentityServers.EntityFramework.Entities.ClientScopeEntity { ClientId = passwordClient.Id, Scope = "openid" },
                new OroIdentityServers.EntityFramework.Entities.ClientScopeEntity { ClientId = passwordClient.Id, Scope = "profile" },
                new OroIdentityServers.EntityFramework.Entities.ClientScopeEntity { ClientId = passwordClient.Id, Scope = "api" }
            );

            await context.SaveChangesAsync();
        }

        // Seed users
        if (!context.Users.Any())
        {
            var user = new OroIdentityServers.EntityFramework.Entities.UserEntity
            {
                TenantId = "default",
                Username = "alice",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                Email = "alice@example.com",
                EmailConfirmed = true,
                Enabled = true,
                Created = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Add user claims
            context.UserClaims.AddRange(
                new OroIdentityServers.EntityFramework.Entities.UserClaimEntity
                {
                    UserId = user.Id,
                    Type = "name",
                    Value = "Alice"
                },
                new OroIdentityServers.EntityFramework.Entities.UserClaimEntity
                {
                    UserId = user.Id,
                    Type = "email",
                    Value = "alice@example.com"
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
