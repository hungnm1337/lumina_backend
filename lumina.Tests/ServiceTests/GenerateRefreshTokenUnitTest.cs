using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ServiceLayer.Auth;
using System.Collections.Generic;

namespace Lumina.Tests.ServiceTests
{
    public class GenerateRefreshTokenUnitTest
    {
        #region Refresh Token Generation Tests

        [Fact]
        public void GenerateRefreshToken_ShouldReturnNonEmptyString()
        {
            // Arrange
            var configuration = CreateMockConfiguration();
            var service = new JwtTokenService(configuration);

            // Act
            var refreshToken = service.GenerateRefreshToken();

            // Assert
            refreshToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GenerateRefreshToken_ShouldReturnBase64String()
        {
            // Arrange
            var configuration = CreateMockConfiguration();
            var service = new JwtTokenService(configuration);

            // Act
            var refreshToken = service.GenerateRefreshToken();

            // Assert
            refreshToken.Should().NotBeNullOrEmpty();

            // Try to decode as Base64 - should not throw
            var act = () => Convert.FromBase64String(refreshToken);
            act.Should().NotThrow();
        }

        [Fact]
        public void GenerateRefreshToken_ShouldGenerate64ByteToken()
        {
            // Arrange
            var configuration = CreateMockConfiguration();
            var service = new JwtTokenService(configuration);

            // Act
            var refreshToken = service.GenerateRefreshToken();
            var decodedBytes = Convert.FromBase64String(refreshToken);

            // Assert
            decodedBytes.Length.Should().Be(64);
        }

        [Fact]
        public void GenerateRefreshToken_CalledMultipleTimes_ShouldGenerateUniqueTokens()
        {
            // Arrange
            var configuration = CreateMockConfiguration();
            var service = new JwtTokenService(configuration);

            // Act
            var token1 = service.GenerateRefreshToken();
            var token2 = service.GenerateRefreshToken();
            var token3 = service.GenerateRefreshToken();

            // Assert
            token1.Should().NotBe(token2);
            token2.Should().NotBe(token3);
            token1.Should().NotBe(token3);
        }

        [Fact]
        public void GenerateRefreshToken_CalledMultipleTimes_ShouldGenerateDifferentValues()
        {
            // Arrange
            var configuration = CreateMockConfiguration();
            var service = new JwtTokenService(configuration);
            var tokens = new HashSet<string>();

            // Act - Generate 10 tokens
            for (int i = 0; i < 10; i++)
            {
                tokens.Add(service.GenerateRefreshToken());
            }

            // Assert - All 10 tokens should be unique
            tokens.Count.Should().Be(10);
        }

        #endregion

        #region Helper Methods

        private IConfiguration CreateMockConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                {"Jwt:SecretKey", "this-is-a-very-secure-secret-key-for-jwt-token-generation-min-32-bytes"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"},
                {"Jwt:AccessTokenExpirationMinutes", "60"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }

        #endregion
    }
}
