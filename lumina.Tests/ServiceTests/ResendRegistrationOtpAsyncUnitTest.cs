using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Auth;
using ServiceLayer.Email;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lumina.Tests.ServiceTests
{
    public class ResendRegistrationOtpAsyncUnitTest
    {
        private readonly LuminaSystemContext _context;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<IGoogleAuthService> _mockGoogleAuthService;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public ResendRegistrationOtpAsyncUnitTest()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _mockGoogleAuthService = new Mock<IGoogleAuthService>();
            _mockEmailSender = new Mock<IEmailSender>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _context,
                _mockJwtTokenService.Object,
                _mockGoogleAuthService.Object,
                _mockEmailSender.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ResendRegistrationOtpAsync_WithValidDataAndExistingTokens_ShouldSucceed()
        {
            // Arrange
            const string email = "user@example.com";
            const string fullName = "Test User";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var tempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = fullName,
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            // Add existing unused token
            var existingOtpCode = "111111";
            var existingOtpHash = BCrypt.Net.BCrypt.HashPassword(existingOtpCode);
            var now = DateTime.UtcNow;
            var existingToken = new PasswordResetToken
            {
                UserId = tempUser.UserId,
                CodeHash = existingOtpHash,
                CreatedAt = now.AddMinutes(-5),
                ExpiresAt = now.AddMinutes(5),
                UsedAt = null
            };

            _context.PasswordResetTokens.Add(existingToken);
            await _context.SaveChangesAsync();

            var request = new ResendRegistrationOtpRequest { Email = email };

            _mockEmailSender
                .Setup(s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.ResendRegistrationOtpAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().NotBeNullOrEmpty();

            // Verify old token is removed
            var oldToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.CodeHash == existingOtpHash);
            oldToken.Should().BeNull();

            // Verify new token is created
            var newToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == tempUser.UserId && t.UsedAt == null);
            newToken.Should().NotBeNull();
            newToken!.CodeHash.Should().NotBe(existingOtpHash);

            _mockEmailSender.Verify(
                s => s.SendRegistrationOtpAsync(email, fullName, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task ResendRegistrationOtpAsync_WithValidDataAndNoExistingTokens_ShouldSucceed()
        {
            // Arrange
            const string email = "user@example.com";
            const string fullName = "Test User";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var tempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = fullName,
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            var request = new ResendRegistrationOtpRequest { Email = email };

            _mockEmailSender
                .Setup(s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.ResendRegistrationOtpAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().NotBeNullOrEmpty();

            // Verify new token is created
            var newToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == tempUser.UserId && t.UsedAt == null);
            newToken.Should().NotBeNull();

            _mockEmailSender.Verify(
                s => s.SendRegistrationOtpAsync(email, fullName, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task ResendRegistrationOtpAsync_WithInactiveTempUserNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            const string email = "nonexistent@example.com";
            var request = new ResendRegistrationOtpRequest { Email = email };

            // Act
            var act = () => _authService.ResendRegistrationOtpAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 404);

            _mockEmailSender.Verify(
                s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ResendRegistrationOtpAsync_WithEmailSendFailure_ShouldThrowServerErrorAndRemoveToken()
        {
            // Arrange
            const string email = "user@example.com";
            const string fullName = "Test User";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var tempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = fullName,
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            var request = new ResendRegistrationOtpRequest { Email = email };

            _mockEmailSender
                .Setup(s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service failed"));

            // Act
            var act = () => _authService.ResendRegistrationOtpAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 500);

            // Verify token is removed
            var tokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == tempUser.UserId && t.UsedAt == null)
                .ToListAsync();
            tokens.Should().BeEmpty();
        }

        [Fact]
        public async Task ResendRegistrationOtpAsync_WithMultipleExistingUnusedTokens_ShouldRemoveAllAndCreateNew()
        {
            // Arrange
            const string email = "user@example.com";
            const string fullName = "Test User";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var tempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = fullName,
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            // Add multiple existing unused tokens
            var now = DateTime.UtcNow;
            var token1 = new PasswordResetToken
            {
                UserId = tempUser.UserId,
                CodeHash = BCrypt.Net.BCrypt.HashPassword("111111"),
                CreatedAt = now.AddMinutes(-10),
                ExpiresAt = now.AddMinutes(5),
                UsedAt = null
            };

            var token2 = new PasswordResetToken
            {
                UserId = tempUser.UserId,
                CodeHash = BCrypt.Net.BCrypt.HashPassword("222222"),
                CreatedAt = now.AddMinutes(-5),
                ExpiresAt = now.AddMinutes(10),
                UsedAt = null
            };

            _context.PasswordResetTokens.Add(token1);
            _context.PasswordResetTokens.Add(token2);
            await _context.SaveChangesAsync();

            var request = new ResendRegistrationOtpRequest { Email = email };

            _mockEmailSender
                .Setup(s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.ResendRegistrationOtpAsync(request);

            // Assert
            result.Should().NotBeNull();

            // Verify all old tokens are removed
            var unusedTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == tempUser.UserId && t.UsedAt == null)
                .ToListAsync();
            unusedTokens.Should().HaveCount(1); // Only the newly created token

            _mockEmailSender.Verify(
                s => s.SendRegistrationOtpAsync(email, fullName, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task ResendRegistrationOtpAsync_WithUsedTokensOnly_ShouldNotRemoveThemAndCreateNew()
        {
            // Arrange
            const string email = "user@example.com";
            const string fullName = "Test User";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var tempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = fullName,
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            // Add used token (should not be removed)
            var usedTokenOtpCode = "111111";
            var usedTokenOtpHash = BCrypt.Net.BCrypt.HashPassword(usedTokenOtpCode);
            var now = DateTime.UtcNow;
            var usedToken = new PasswordResetToken
            {
                UserId = tempUser.UserId,
                CodeHash = usedTokenOtpHash,
                CreatedAt = now.AddMinutes(-20),
                ExpiresAt = now.AddMinutes(-5),
                UsedAt = now.AddMinutes(-10)
            };

            _context.PasswordResetTokens.Add(usedToken);
            await _context.SaveChangesAsync();

            var request = new ResendRegistrationOtpRequest { Email = email };

            _mockEmailSender
                .Setup(s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.ResendRegistrationOtpAsync(request);

            // Assert
            result.Should().NotBeNull();

            // Verify used token still exists
            var usedTokenInDb = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.CodeHash == usedTokenOtpHash);
            usedTokenInDb.Should().NotBeNull();
            usedTokenInDb!.UsedAt.Should().NotBeNull();

            // Verify new unused token is created
            var unusedTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == tempUser.UserId && t.UsedAt == null)
                .ToListAsync();
            unusedTokens.Should().HaveCount(1);

            _mockEmailSender.Verify(
                s => s.SendRegistrationOtpAsync(email, fullName, It.IsAny<string>()),
                Times.Once);
        }
    }
}
