using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ServiceLayer.Auth;
using DataLayer.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Collections.Generic;

namespace Lumina.Tests.ServiceTests
{
    public class GenerateTokenUnitTest
    {
        #region Null User Validation Tests

        [Fact]
        public void GenerateToken_WithNullUser_ShouldThrowArgumentNullException()
        {
            // Arrange
            var configuration = CreateMockConfiguration(
                secretKey: "this-is-a-very-secure-secret-key-for-jwt-token-generation-min-32-bytes",
                issuer: "TestIssuer",
                audience: "TestAudience",
                expirationMinutes: 60
            );
            var service = new JwtTokenService(configuration);

            // Act
            var act = () => service.GenerateToken(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region Successful Token Generation Tests

        [Fact]
        public void GenerateToken_WithValidUser_ShouldReturnValidJwtToken()
        {
            // Arrange
            var configuration = CreateMockConfiguration(
                secretKey: "this-is-a-very-secure-secret-key-for-jwt-token-generation-min-32-bytes",
                issuer: "TestIssuer",
                audience: "TestAudience",
                expirationMinutes: 60
            );
            var service = new JwtTokenService(configuration);

            var role = new Role { RoleId = 1, RoleName = "Admin" };
            var user = new User
            {
                UserId = 123,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                Role = role
            };

            // Act
            var result = service.GenerateToken(user);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.ExpiresInSeconds.Should().Be(3600); // 60 minutes = 3600 seconds
            result.ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void GenerateToken_WithValidUser_ShouldContainCorrectClaims()
        {
            // Arrange
            var configuration = CreateMockConfiguration(
                secretKey: "this-is-a-very-secure-secret-key-for-jwt-token-generation-min-32-bytes",
                issuer: "TestIssuer",
                audience: "TestAudience",
                expirationMinutes: 60
            );
            var service = new JwtTokenService(configuration);

            var role = new Role { RoleId = 2, RoleName = "User" };
            var user = new User
            {
                UserId = 456,
                Email = "user@example.com",
                FullName = "John Doe",
                RoleId = 2,
                Role = role
            };

            // Act
            var result = service.GenerateToken(user);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(result.Token);

            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "456");
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "user@example.com");
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "John Doe");
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "456");
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "user@example.com");
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "John Doe");
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
            jwtToken.Claims.Should().Contain(c => c.Type == "RoleId" && c.Value == "2");
        }

        [Fact]
        public void GenerateToken_WithValidUser_ShouldHaveCorrectIssuerAndAudience()
        {
            // Arrange
            var configuration = CreateMockConfiguration(
                secretKey: "this-is-a-very-secure-secret-key-for-jwt-token-generation-min-32-bytes",
                issuer: "MyCustomIssuer",
                audience: "MyCustomAudience",
                expirationMinutes: 60
            );
            var service = new JwtTokenService(configuration);

            var role = new Role { RoleId = 1, RoleName = "Admin" };
            var user = new User
            {
                UserId = 789,
                Email = "admin@example.com",
                FullName = "Admin User",
                RoleId = 1,
                Role = role
            };

            // Act
            var result = service.GenerateToken(user);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(result.Token);

            jwtToken.Issuer.Should().Be("MyCustomIssuer");
            jwtToken.Audiences.Should().Contain("MyCustomAudience");
        }

        [Fact]
        public void GenerateToken_WithCustomExpirationTime_ShouldRespectConfiguration()
        {
            // Arrange
            var configuration = CreateMockConfiguration(
                secretKey: "this-is-a-very-secure-secret-key-for-jwt-token-generation-min-32-bytes",
                issuer: "TestIssuer",
                audience: "TestAudience",
                expirationMinutes: 120
            );
            var service = new JwtTokenService(configuration);

            var role = new Role { RoleId = 1, RoleName = "Admin" };
            var user = new User
            {
                UserId = 100,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                Role = role
            };

            // Act
            var result = service.GenerateToken(user);

            // Assert
            result.ExpiresInSeconds.Should().Be(7200); // 120 minutes = 7200 seconds
            result.ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(120), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void GenerateToken_CalledMultipleTimes_ShouldGenerateDifferentTokens()
        {
            // Arrange
            var configuration = CreateMockConfiguration(
                secretKey: "this-is-a-very-secure-secret-key-for-jwt-token-generation-min-32-bytes",
                issuer: "TestIssuer",
                audience: "TestAudience",
                expirationMinutes: 60
            );
            var service = new JwtTokenService(configuration);

            var role = new Role { RoleId = 1, RoleName = "Admin" };
            var user = new User
            {
                UserId = 123,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                Role = role
            };

            // Act
            var result1 = service.GenerateToken(user);
            System.Threading.Thread.Sleep(1000); // Wait 1 second to ensure different timestamps
            var result2 = service.GenerateToken(user);

            // Assert
            result1.Token.Should().NotBe(result2.Token);
        }

        #endregion

        #region Helper Methods

        private IConfiguration CreateMockConfiguration(
            string secretKey,
            string issuer,
            string audience,
            int expirationMinutes)
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                {"Jwt:SecretKey", secretKey},
                {"Jwt:Issuer", issuer},
                {"Jwt:Audience", audience},
                {"Jwt:AccessTokenExpirationMinutes", expirationMinutes.ToString()}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }

        #endregion
    }
}
