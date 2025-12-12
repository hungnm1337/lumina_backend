using Moq;
using FluentAssertions;
using ServiceLayer.Auth;
using ServiceLayer.Email;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Lumina.Tests.Helpers;
using RepositoryLayer.UnitOfWork;

namespace Lumina.Test.Services
{
    public class RefreshTokenAsyncUnitTest : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<IGoogleAuthService> _mockGoogleAuthService;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _service;

        public RefreshTokenAsyncUnitTest()
        {
            (_unitOfWork, _context) = InMemoryDbContextHelper.CreateUnitOfWork();
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _mockGoogleAuthService = new Mock<IGoogleAuthService>();
            _mockEmailSender = new Mock<IEmailSender>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            _service = new AuthService(
                _unitOfWork,
                _mockJwtTokenService.Object,
                _mockGoogleAuthService.Object,
                _mockEmailSender.Object,
                _mockLogger.Object
            );

            _mockJwtTokenService.Setup(x => x.GenerateToken(It.IsAny<User>()))
                .Returns(new JwtTokenResult("test-token", 3600, DateTime.UtcNow.AddHours(1)));
            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns("new-refresh-token");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenTokenNotFoundOrInvalid_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "non-existent-token"
            };

            // Act
            var act = async () => await _service.RefreshTokenAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenTokenRevokedOrExpired_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var role = new Role { RoleId = 1, RoleName = "User" };
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                IsActive = true,
                Role = role
            };

            var account = new Account
            {
                AccountId = 1,
                UserId = 1,
                Username = "testuser",
                CreateAt = DateTime.UtcNow
            };

            var revokedToken = new RefreshToken
            {
                Id = 1,
                UserId = 1,
                Token = "revoked-token",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ExpiresAt = DateTime.UtcNow.AddDays(6),
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow.AddHours(-1),
                RevokedReason = "Manual revoke",
                User = user
            };

            await _context.Roles.AddAsync(role);
            await _context.Users.AddAsync(user);
            await _context.Accounts.AddAsync(account);
            await _context.RefreshTokens.AddAsync(revokedToken);
            await _context.SaveChangesAsync();

            var request = new RefreshTokenRequest
            {
                RefreshToken = "revoked-token"
            };

            // Act
            var act = async () => await _service.RefreshTokenAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("Refresh token has been revoked");
            exception.Which.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenValidToken_ShouldReturnNewTokens()
        {
            // Arrange
            var role = new Role { RoleId = 1, RoleName = "User" };
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                IsActive = true,
                Role = role
            };

            var account = new Account
            {
                AccountId = 1,
                UserId = 1,
                Username = "testuser",
                CreateAt = DateTime.UtcNow
            };

            var validToken = new RefreshToken
            {
                Id = 1,
                UserId = 1,
                Token = "valid-refresh-token",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                User = user
            };

            await _context.Roles.AddAsync(role);
            await _context.Users.AddAsync(user);
            await _context.Accounts.AddAsync(account);
            await _context.RefreshTokens.AddAsync(validToken);
            await _context.SaveChangesAsync();

            var request = new RefreshTokenRequest
            {
                RefreshToken = "valid-refresh-token"
            };

            // Act
            var result = await _service.RefreshTokenAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be("test-token");
            result.RefreshToken.Should().Be("new-refresh-token");
            result.ExpiresIn.Should().Be(3600);
            result.User.Should().NotBeNull();
            result.User.Username.Should().Be("testuser");
            result.User.Email.Should().Be("test@example.com");

            var oldToken = await _context.RefreshTokens.FirstAsync(rt => rt.Id == validToken.Id);
            oldToken.IsRevoked.Should().BeTrue();
            oldToken.RevokedReason.Should().Be("Replaced by new token");

            var newToken = await _context.RefreshTokens
                .Where(rt => rt.Token == "new-refresh-token")
                .FirstOrDefaultAsync();
            newToken.Should().NotBeNull();
            newToken!.UserId.Should().Be(1);
            newToken.IsRevoked.Should().BeFalse();
        }
    }
}
