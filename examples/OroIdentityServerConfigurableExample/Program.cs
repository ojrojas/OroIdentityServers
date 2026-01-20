using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OroIdentityServers;
using OroIdentityServers.Core;
using OroIdentityServers.EntityFramework.Extensions;
using OroIdentityServers.EntityFramework.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;

namespace OroIdentityServerConfigurableExample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers();

        // Configure database (using SQLite for simplicity)
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite("Data Source=configurable-example.db"));

        // Configure OroIdentityServer with Entity Framework
        builder.Services.AddOroIdentityServerDbContext<ApplicationDbContext>();
        builder.Services.AddEntityFrameworkStores();
        builder.Services.AddConfigurationEvents();
        builder.Services.AddAutomaticMigrations<ApplicationDbContext>();

        // Configure IdentityServer options
        builder.Services.AddSingleton(new IdentityServerOptions
        {
            Issuer = "http://localhost:5000",
            Audience = "api",
            SecretKey = "your-very-long-secret-key-at-least-32-characters-long-for-demo"
        });

        // Register TokenService
        builder.Services.AddSingleton<ITokenService, TokenService>(sp =>
        {
            var options = sp.GetRequiredService<IdentityServerOptions>();
            return new TokenService(options.Issuer, options.Audience, options.SecretKey);
        });

        // Configure the new configurable OAuth endpoints
        builder.Services.AddDefaultOAuthEndpoints();

        // Configure authentication
        builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                var identityOptions = builder.Services.BuildServiceProvider().GetRequiredService<IdentityServerOptions>();
                options.Authority = identityOptions.Issuer;
                options.Audience = identityOptions.Audience;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = identityOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = identityOptions.Audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(identityOptions.SecretKey.PadRight(32, '0')))
                };
            });

        builder.Services.AddAuthorization();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        // Use the new configurable OAuth endpoints
        app.UseOAuthEndpoints();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Add a simple test endpoint
        app.MapGet("/api/test", (HttpContext context) =>
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = 401;
                return Results.Unauthorized();
            }

            var claims = user.Claims.Select(c => new { c.Type, c.Value });
            return Results.Ok(new
            {
                message = $"Hello {user.Identity.Name}!",
                claims
            });
        }).RequireAuthorization();

        // Seed the database
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            // Seed some test data
            if (!dbContext.Clients.Any())
            {
                dbContext.Clients.Add(new ClientEntity
                {
                    TenantId = "default",
                    ClientId = "test-client",
                    ClientSecret = "test-secret", // In production, this should be hashed
                    ClientName = "Test Client",
                    Description = "A test client for demonstration",
                    Enabled = true,
                    Created = DateTime.UtcNow
                });
            }

            if (!dbContext.Users.Any())
            {
                var userEntity = new UserEntity
                {
                    TenantId = "default",
                    Username = "testuser",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                    Email = "test@example.com",
                    EmailConfirmed = true,
                    Enabled = true,
                    Created = DateTime.UtcNow
                };

                dbContext.Users.Add(userEntity);
                dbContext.SaveChanges(); // Save to get the UserId

                // Add claims for the user
                dbContext.UserClaims.Add(new UserClaimEntity
                {
                    UserId = userEntity.Id,
                    Type = "name",
                    Value = "Test User"
                });

                dbContext.UserClaims.Add(new UserClaimEntity
                {
                    UserId = userEntity.Id,
                    Type = "email",
                    Value = "test@example.com"
                });
            }

            await dbContext.SaveChangesAsync();
        }

        await app.RunAsync();
    }
}