using Moq;
using FluentAssertions;
using ServiceLayer.Auth;
using ServiceLayer.Email;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Lumina.Tests.Helpers;
using RepositoryLayer.UnitOfWork;

namespace Lumina.Test.Services
{
    public class ResetPasswordAsyncUnitTest : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<PasswordResetService>> _mockLogger;
        private readonly IConfiguration _configuration;
        private readonly PasswordResetService _service;

        public ResetPasswordAsyncUnitTest()
        {
            (_unitOfWork, _context) = InMemoryDbContextHelper.CreateUnitOfWork();
            _mockEmailSender = new Mock<IEmailSender>();
            _mockLogger = new Mock<ILogger<PasswordResetService>>();

            var configurationData = new Dictionary<string, string?>
            {
                { "PasswordReset:CodeLength", "6" },
                { "PasswordReset:CodeExpiryMinutes", "10" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            _service = new PasswordResetService(
                _unitOfWork,
                _mockEmailSender.Object,
                _mockLogger.Object,
                _configuration
            );
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenUserNotFoundOrNoPasswordAccount_ShouldThrowException()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Email = "nonexistent@example.com",
                OtpCode = "123456",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            // Act
            var act = async () => await _service.ResetPasswordAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>();
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenInvalidToken_ShouldThrowBadRequestException()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
            };

            var account = new Account
            {
                AccountId = 1,
                UserId = 1,
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword@123"),
                AuthProvider = null,
                CreateAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();

            var request = new ResetPasswordRequest
            {
                Email = "test@example.com",
                OtpCode = "123456",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            // Act
            var act = async () => await _service.ResetPasswordAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("Invalid or expired OTP code");
            exception.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenValidRequest_ShouldResetPasswordSuccessfully()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
            };

            var oldPasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword@123");
            var account = new Account
            {
                AccountId = 1,
                UserId = 1,
                Username = "testuser",
                PasswordHash = oldPasswordHash,
                AuthProvider = null,
                CreateAt = DateTime.UtcNow.AddDays(-1),
                UpdateAt = null
            };

            var validOtpCode = "123456";
            var resetToken = new PasswordResetToken
            {
                PasswordResetTokenId = 1,
                UserId = 1,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(validOtpCode),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                UsedAt = null
            };

            await _context.Users.AddAsync(user);
            await _context.Accounts.AddAsync(account);
            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            var request = new ResetPasswordRequest
            {
                Email = "test@example.com",
                OtpCode = validOtpCode,
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            // Act
            var result = await _service.ResetPasswordAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("Password has been reset successfully");

            var updatedAccount = await _context.Accounts.FirstAsync(a => a.AccountId == account.AccountId);
            updatedAccount.PasswordHash.Should().NotBe(oldPasswordHash);
            BCrypt.Net.BCrypt.Verify("NewPassword@123", updatedAccount.PasswordHash).Should().BeTrue();
            updatedAccount.UpdateAt.Should().NotBeNull();

            var updatedToken = await _context.PasswordResetTokens.FirstAsync(t => t.PasswordResetTokenId == resetToken.PasswordResetTokenId);
            updatedToken.UsedAt.Should().NotBeNull();
        }
    }
}
