using OroIdentityServers;
using OroIdentityServers.Core;

namespace OroIdentityServers.Tests;

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
}
