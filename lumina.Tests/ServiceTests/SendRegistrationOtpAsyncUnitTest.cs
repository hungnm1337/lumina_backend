using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Auth;
using ServiceLayer.Email;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;
using RepositoryLayer.UnitOfWork;

namespace Lumina.Tests.ServiceTests
{
    public class SendRegistrationOtpAsyncUnitTest
    {
        private readonly LuminaSystemContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<IGoogleAuthService> _mockGoogleAuthService;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public SendRegistrationOtpAsyncUnitTest()
        {
            (_unitOfWork, _context) = InMemoryDbContextHelper.CreateUnitOfWork();
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _mockGoogleAuthService = new Mock<IGoogleAuthService>();
            _mockEmailSender = new Mock<IEmailSender>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _unitOfWork,
                _mockJwtTokenService.Object,
                _mockGoogleAuthService.Object,
                _mockEmailSender.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task SendRegistrationOtpAsync_WithValidNewEmail_ShouldSendOtpSuccessfully()
        {
            // Arrange
            const string email = "newuser@example.com";
            const string username = "newuser";

            var request = new SendRegistrationOtpRequest
            {
                Email = email,
                Username = username
            };

            _mockEmailSender
                .Setup(s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.SendRegistrationOtpAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().NotBeNullOrEmpty();

            // Verify temp user was created
            var tempUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
            tempUser.Should().NotBeNull();
            tempUser!.IsActive.Should().BeFalse();
            tempUser.FullName.Should().Be("Pending");

            // Verify OTP token was created
            var otpToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == tempUser.UserId);
            otpToken.Should().NotBeNull();
            otpToken!.UsedAt.Should().BeNull();
            otpToken.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

            // Verify email was sent
            _mockEmailSender.Verify(
                s => s.SendRegistrationOtpAsync(
                    email.ToLowerInvariant(),
                    email.ToLowerInvariant(),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task SendRegistrationOtpAsync_WithRegisteredActiveEmail_ShouldThrowConflict()
        {
            // Arrange
            const string email = "existing@example.com";
            const string username = "newusername";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var existingUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Existing User",
                RoleId = 4,
                Role = role,
                IsActive = true,
                CurrentStreak = 0
            };

            var account = new Account
            {
                AccountId = 1,
                UserId = 1,
                Username = "existinguser",
                CreateAt = DateTime.UtcNow,
                User = existingUser
            };

            _context.Roles.Add(role);
            _context.Users.Add(existingUser);
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var request = new SendRegistrationOtpRequest
            {
                Email = email,
                Username = username
            };

            // Act
            var act = () => _authService.SendRegistrationOtpAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 409)
                .WithMessage("Email đã được đăng ký");

            // Verify no temp user was created
            var tempUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive == false);
            tempUser.Should().BeNull();

            _mockEmailSender.Verify(
                s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task SendRegistrationOtpAsync_WithExistingUsername_ShouldThrowConflict()
        {
            // Arrange
            const string email = "newuser@example.com";
            const string username = "existingusername";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var existingUser = new User
            {
                UserId = 1,
                Email = "existing@example.com",
                FullName = "Existing User",
                RoleId = 4,
                Role = role,
                IsActive = true,
                CurrentStreak = 0
            };

            var account = new Account
            {
                AccountId = 1,
                UserId = 1,
                Username = username,
                CreateAt = DateTime.UtcNow,
                User = existingUser
            };

            _context.Roles.Add(role);
            _context.Users.Add(existingUser);
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var request = new SendRegistrationOtpRequest
            {
                Email = email,
                Username = username
            };

            // Act
            var act = () => _authService.SendRegistrationOtpAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 409)
                .WithMessage("Tên đăng nhập đã tồn tại");

            // Verify no temp user was created
            var tempUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
            tempUser.Should().BeNull();

            _mockEmailSender.Verify(
                s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task SendRegistrationOtpAsync_WithOldTempUser_ShouldCleanupAndSendNewOtp()
        {
            // Arrange
            const string email = "tempuser@example.com";
            const string username = "tempusername";

            // Create old temp user with expired token
            var oldTempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Pending",
                RoleId = 4,
                IsActive = false,
                CurrentStreak = 0
            };

            var oldToken = new PasswordResetToken
            {
                PasswordResetTokenId = 1,
                UserId = 1,
                CodeHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired
            };

            _context.Users.Add(oldTempUser);
            await _context.SaveChangesAsync();

            _context.PasswordResetTokens.Add(oldToken);
            await _context.SaveChangesAsync();

            var request = new SendRegistrationOtpRequest
            {
                Email = email,
                Username = username
            };

            _mockEmailSender
                .Setup(s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.SendRegistrationOtpAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().NotBeNullOrEmpty();

            // Verify old token was removed
            var oldTokenCount = await _context.PasswordResetTokens
                .CountAsync(t => t.PasswordResetTokenId == 1);
            oldTokenCount.Should().Be(0);

            // Verify new temp user was created
            var newTempUser = await _context.Users
                .Include(u => u.PasswordResetTokens)
                .FirstOrDefaultAsync(u => u.Email == email);
            newTempUser.Should().NotBeNull();
            newTempUser!.IsActive.Should().BeFalse();

            // Verify new token was created
            var newToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == newTempUser.UserId);
            newToken.Should().NotBeNull();

            _mockEmailSender.Verify(
                s => s.SendRegistrationOtpAsync(
                    email.ToLowerInvariant(),
                    email.ToLowerInvariant(),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task SendRegistrationOtpAsync_WithEmailSendingFailure_ShouldThrowServerError()
        {
            // Arrange
            const string email = "failuser@example.com";
            const string username = "failusername";

            var request = new SendRegistrationOtpRequest
            {
                Email = email,
                Username = username
            };

            _mockEmailSender
                .Setup(s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service unavailable"));

            // Act
            var act = () => _authService.SendRegistrationOtpAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 500)
                .WithMessage("Không thể gửi mã OTP. Vui lòng thử lại sau.");

            // Verify temp user and token were rolled back
            var tempUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
            tempUser.Should().BeNull();

            var tokens = await _context.PasswordResetTokens.CountAsync();
            tokens.Should().Be(0);
        }

        [Fact]
        public async Task SendRegistrationOtpAsync_WithWhitespaceEmail_ShouldNormalizeAndProcess()
        {
            // Arrange
            const string email = "  testuser@example.com  ";
            const string username = "  testusername  ";

            var request = new SendRegistrationOtpRequest
            {
                Email = email,
                Username = username
            };

            _mockEmailSender
                .Setup(s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.SendRegistrationOtpAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().NotBeNullOrEmpty();

            // Verify normalized email was used
            var tempUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "testuser@example.com");
            tempUser.Should().NotBeNull();

            _mockEmailSender.Verify(
                s => s.SendRegistrationOtpAsync(
                    "testuser@example.com",
                    "testuser@example.com",
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task SendRegistrationOtpAsync_WithOldTempUserNoTokens_ShouldCleanupWithoutTokenRemoval()
        {
            // Arrange
            const string email = "oldtempnotoken@example.com";
            const string username = "oldtempnotoken";

            // Create old temp user WITHOUT tokens (empty collection or null)
            var oldTempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Pending",
                RoleId = 4,
                IsActive = false,
                CurrentStreak = 0,
                PasswordResetTokens = new List<PasswordResetToken>() // Empty collection
            };

            _context.Users.Add(oldTempUser);
            await _context.SaveChangesAsync();

            var request = new SendRegistrationOtpRequest
            {
                Email = email,
                Username = username
            };

            _mockEmailSender
                .Setup(s => s.SendRegistrationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.SendRegistrationOtpAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().NotBeNullOrEmpty();

            // Verify new temp user was created (old one cleaned up)
            var userCount = await _context.Users.CountAsync(u => u.Email == email);
            userCount.Should().Be(1);

            // Verify new token was created
            var newToken = await _context.PasswordResetTokens.FirstOrDefaultAsync();
            newToken.Should().NotBeNull();

            _mockEmailSender.Verify(
                s => s.SendRegistrationOtpAsync(
                    email,
                    email,
                    It.IsAny<string>()),
                Times.Once);
        }
    }
}
