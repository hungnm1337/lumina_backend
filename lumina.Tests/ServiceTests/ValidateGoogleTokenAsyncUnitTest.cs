using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Auth;

namespace Lumina.Tests.ServiceTests
{
    public class ValidateGoogleTokenAsyncUnitTest
    {
        private readonly Mock<ILogger<GoogleAuthService>> _mockLogger;

        public ValidateGoogleTokenAsyncUnitTest()
        {
            _mockLogger = new Mock<ILogger<GoogleAuthService>>();
        }

        #region Configuration Validation Tests

        [Fact]
        public async Task ValidateGoogleTokenAsync_WithMissingClientId_ShouldThrowConfigurationError()
        {
            // Arrange
            var configuration = CreateMockConfiguration(clientId: null);
            var service = new GoogleAuthService(_mockLogger.Object, configuration.Object);
            const string validToken = "valid_token";

            // Act
            var act = () => service.ValidateGoogleTokenAsync(validToken);

            // Assert
            await act.Should().ThrowAsync<GoogleAuthException>()
                .WithMessage("Google login is not configured");
        }

        [Fact]
        public async Task ValidateGoogleTokenAsync_WithEmptyClientId_ShouldThrowConfigurationError()
        {
            // Arrange
            var configuration = CreateMockConfiguration(clientId: "");
            var service = new GoogleAuthService(_mockLogger.Object, configuration.Object);
            const string validToken = "valid_token";

            // Act
            var act = () => service.ValidateGoogleTokenAsync(validToken);

            // Assert
            await act.Should().ThrowAsync<GoogleAuthException>()
                .WithMessage("Google login is not configured");
        }

        [Fact]
        public async Task ValidateGoogleTokenAsync_WithWhitespaceClientId_ShouldThrowConfigurationError()
        {
            // Arrange
            var configuration = CreateMockConfiguration(clientId: "   ");
            var service = new GoogleAuthService(_mockLogger.Object, configuration.Object);
            const string validToken = "valid_token";

            // Act
            var act = () => service.ValidateGoogleTokenAsync(validToken);

            // Assert
            await act.Should().ThrowAsync<GoogleAuthException>()
                .WithMessage("Google login is not configured");
        }

        #endregion

        #region Token Validation Tests

        [Fact]
        public async Task ValidateGoogleTokenAsync_WithNullToken_ShouldThrowInvalidTokenException()
        {
            // Arrange
            var configuration = CreateMockConfiguration(clientId: "valid-client-id");
            var service = new GoogleAuthService(_mockLogger.Object, configuration.Object);

            // Act
            var act = () => service.ValidateGoogleTokenAsync(null!);

            // Assert
            await act.Should().ThrowAsync<GoogleAuthException>()
                .WithMessage("Google token is required");
        }

        [Fact]
        public async Task ValidateGoogleTokenAsync_WithEmptyToken_ShouldThrowInvalidTokenException()
        {
            // Arrange
            var configuration = CreateMockConfiguration(clientId: "valid-client-id");
            var service = new GoogleAuthService(_mockLogger.Object, configuration.Object);

            // Act
            var act = () => service.ValidateGoogleTokenAsync("");

            // Assert
            await act.Should().ThrowAsync<GoogleAuthException>()
                .WithMessage("Google token is required");
        }

        [Fact]
        public async Task ValidateGoogleTokenAsync_WithWhitespaceToken_ShouldThrowInvalidTokenException()
        {
            // Arrange
            var configuration = CreateMockConfiguration(clientId: "valid-client-id");
            var service = new GoogleAuthService(_mockLogger.Object, configuration.Object);

            // Act
            var act = () => service.ValidateGoogleTokenAsync("   ");

            // Assert
            await act.Should().ThrowAsync<GoogleAuthException>()
                .WithMessage("Google token is required");
        }

        #endregion

        #region Google API Validation Tests

        [Fact]
        public async Task ValidateGoogleTokenAsync_WithInvalidJwtFormat_ShouldThrowInvalidTokenException()
        {
            // Arrange
            var configuration = CreateMockConfiguration(clientId: "valid-client-id.apps.googleusercontent.com");
            var service = new GoogleAuthService(_mockLogger.Object, configuration.Object);
            const string invalidToken = "invalid-jwt-format";

            // Act
            var act = () => service.ValidateGoogleTokenAsync(invalidToken);

            // Assert
            await act.Should().ThrowAsync<GoogleAuthException>()
                .WithMessage("Invalid Google token");
        }

        [Fact]
        public async Task ValidateGoogleTokenAsync_WithMalformedJwt_ShouldThrowInvalidTokenException()
        {
            // Arrange
            var configuration = CreateMockConfiguration(clientId: "valid-client-id.apps.googleusercontent.com");
            var service = new GoogleAuthService(_mockLogger.Object, configuration.Object);
            const string malformedJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.malformed";

            // Act
            var act = () => service.ValidateGoogleTokenAsync(malformedJwt);

            // Assert
            await act.Should().ThrowAsync<GoogleAuthException>()
                .WithMessage("Invalid Google token");
        }

        [Fact]
        public async Task ValidateGoogleTokenAsync_WithExpiredToken_ShouldThrowInvalidTokenException()
        {
            // Arrange
            var configuration = CreateMockConfiguration(clientId: "valid-client-id.apps.googleusercontent.com");
            var service = new GoogleAuthService(_mockLogger.Object, configuration.Object);
            // This is a sample expired Google JWT token structure (will fail validation)
            const string expiredToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InRlc3QifQ.eyJpc3MiOiJodHRwczovL2FjY291bnRzLmdvb2dsZS5jb20iLCJhenAiOiJ0ZXN0LWNsaWVudC1pZCIsImF1ZCI6InRlc3QtY2xpZW50LWlkIiwic3ViIjoiMTIzNDU2Nzg5MCIsImVtYWlsIjoidGVzdEBleGFtcGxlLmNvbSIsImVtYWlsX3ZlcmlmaWVkIjp0cnVlLCJpYXQiOjE1MTYyMzkwMjIsImV4cCI6MTUxNjIzOTAyMn0.test";

            // Act
            var act = () => service.ValidateGoogleTokenAsync(expiredToken);

            // Assert
            await act.Should().ThrowAsync<GoogleAuthException>()
                .WithMessage("Invalid Google token");
        }

        #endregion

        #region Helper Methods

        private Mock<IConfiguration> CreateMockConfiguration(string? clientId)
        {
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(c => c["Google:ClientId"]).Returns(clientId);
            return configuration;
        }

        #endregion
    }
}
