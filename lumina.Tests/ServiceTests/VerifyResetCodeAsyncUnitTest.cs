using Moq;
using FluentAssertions;
using ServiceLayer.Auth;
using ServiceLayer.Email;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lumina.Test.Services
{
    public class VerifyResetCodeAsyncUnitTest : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<PasswordResetService>> _mockLogger;
        private readonly IConfiguration _configuration;
        private readonly PasswordResetService _service;

        public VerifyResetCodeAsyncUnitTest()
        {
            var options = new DbContextOptionsBuilder<LuminaSystemContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new LuminaSystemContext(options);
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
                _context,
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

        #region Test Cases for User Not Found

        [Fact]
        public async Task VerifyResetCodeAsync_WhenEmailNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var request = new VerifyResetCodeRequest
            {
                Email = "nonexistent@example.com",
                OtpCode = "123456"
            };

            // Act
            var act = async () => await _service.VerifyResetCodeAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("Email not found");
            exception.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task VerifyResetCodeAsync_WhenEmailHasDifferentCase_ShouldNormalizeAndFind()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
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
            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            var request = new VerifyResetCodeRequest
            {
                Email = "TEST@EXAMPLE.COM", // Different case
                OtpCode = validOtpCode
            };

            // Act
            var result = await _service.VerifyResetCodeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("OTP verified successfully");
        }

        #endregion

        #region Test Cases for Invalid or Expired Token

        [Fact]
        public async Task VerifyResetCodeAsync_WhenNoActiveTokenExists_ShouldThrowBadRequestException()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var request = new VerifyResetCodeRequest
            {
                Email = "test@example.com",
                OtpCode = "123456"
            };

            // Act
            var act = async () => await _service.VerifyResetCodeAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("Invalid or expired OTP code");
            exception.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task VerifyResetCodeAsync_WhenTokenIsExpired_ShouldThrowBadRequestException()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
            };

            var validOtpCode = "123456";
            var resetToken = new PasswordResetToken
            {
                PasswordResetTokenId = 1,
                UserId = 1,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(validOtpCode),
                CreatedAt = DateTime.UtcNow.AddMinutes(-15),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // Expired 5 minutes ago
                UsedAt = null
            };

            await _context.Users.AddAsync(user);
            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            var request = new VerifyResetCodeRequest
            {
                Email = "test@example.com",
                OtpCode = validOtpCode
            };

            // Act
            var act = async () => await _service.VerifyResetCodeAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("Invalid or expired OTP code");
            exception.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task VerifyResetCodeAsync_WhenTokenIsAlreadyUsed_ShouldThrowBadRequestException()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
            };

            var validOtpCode = "123456";
            var resetToken = new PasswordResetToken
            {
                PasswordResetTokenId = 1,
                UserId = 1,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(validOtpCode),
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                UsedAt = DateTime.UtcNow.AddMinutes(-1) // Already used
            };

            await _context.Users.AddAsync(user);
            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            var request = new VerifyResetCodeRequest
            {
                Email = "test@example.com",
                OtpCode = validOtpCode
            };

            // Act
            var act = async () => await _service.VerifyResetCodeAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("Invalid or expired OTP code");
            exception.Which.StatusCode.Should().Be(400);
        }

        #endregion

        #region Test Cases for Invalid OTP Code

        [Fact]
        public async Task VerifyResetCodeAsync_WhenOtpCodeIsIncorrect_ShouldThrowBadRequestException()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
            };

            var correctOtpCode = "123456";
            var resetToken = new PasswordResetToken
            {
                PasswordResetTokenId = 1,
                UserId = 1,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(correctOtpCode),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                UsedAt = null
            };

            await _context.Users.AddAsync(user);
            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            var request = new VerifyResetCodeRequest
            {
                Email = "test@example.com",
                OtpCode = "654321" // Wrong OTP code
            };

            // Act
            var act = async () => await _service.VerifyResetCodeAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("Invalid or expired OTP code");
            exception.Which.StatusCode.Should().Be(400);
        }

        #endregion

        #region Test Cases for Successful Verification

        [Fact]
        public async Task VerifyResetCodeAsync_WhenValidOtpCode_ShouldReturnSuccessResponse()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
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
            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            var request = new VerifyResetCodeRequest
            {
                Email = "test@example.com",
                OtpCode = validOtpCode
            };

            // Act
            var result = await _service.VerifyResetCodeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("OTP verified successfully");
        }

        [Fact]
        public async Task VerifyResetCodeAsync_WhenMultipleTokensExist_ShouldUseLatestActiveToken()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
            };

            var oldOtpCode = "111111";
            var newOtpCode = "222222";

            var oldToken = new PasswordResetToken
            {
                PasswordResetTokenId = 1,
                UserId = 1,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(oldOtpCode),
                CreatedAt = DateTime.UtcNow.AddMinutes(-8),
                ExpiresAt = DateTime.UtcNow.AddMinutes(2),
                UsedAt = null
            };

            var newToken = new PasswordResetToken
            {
                PasswordResetTokenId = 2,
                UserId = 1,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(newOtpCode),
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                ExpiresAt = DateTime.UtcNow.AddMinutes(8),
                UsedAt = null
            };

            await _context.Users.AddAsync(user);
            await _context.PasswordResetTokens.AddRangeAsync(oldToken, newToken);
            await _context.SaveChangesAsync();

            var request = new VerifyResetCodeRequest
            {
                Email = "test@example.com",
                OtpCode = newOtpCode // Using the latest OTP
            };

            // Act
            var result = await _service.VerifyResetCodeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("OTP verified successfully");
        }

        [Fact]
        public async Task VerifyResetCodeAsync_WhenMultipleTokensExist_ShouldNotVerifyOldToken()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
            };

            var oldOtpCode = "111111";
            var newOtpCode = "222222";

            var oldToken = new PasswordResetToken
            {
                PasswordResetTokenId = 1,
                UserId = 1,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(oldOtpCode),
                CreatedAt = DateTime.UtcNow.AddMinutes(-8),
                ExpiresAt = DateTime.UtcNow.AddMinutes(2),
                UsedAt = null
            };

            var newToken = new PasswordResetToken
            {
                PasswordResetTokenId = 2,
                UserId = 1,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(newOtpCode),
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                ExpiresAt = DateTime.UtcNow.AddMinutes(8),
                UsedAt = null
            };

            await _context.Users.AddAsync(user);
            await _context.PasswordResetTokens.AddRangeAsync(oldToken, newToken);
            await _context.SaveChangesAsync();

            var request = new VerifyResetCodeRequest
            {
                Email = "test@example.com",
                OtpCode = oldOtpCode // Trying to use old OTP
            };

            // Act
            var act = async () => await _service.VerifyResetCodeAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("Invalid or expired OTP code");
            exception.Which.StatusCode.Should().Be(400);
        }

        #endregion

        #region Test Cases for AsNoTracking Behavior

        [Fact]
        public async Task VerifyResetCodeAsync_ShouldNotTrackUserEntity()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
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
            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            var request = new VerifyResetCodeRequest
            {
                Email = "test@example.com",
                OtpCode = validOtpCode
            };

            // Act
            var result = await _service.VerifyResetCodeAsync(request);

            // Assert
            result.Should().NotBeNull();
            var trackedEntities = _context.ChangeTracker.Entries<User>().ToList();
            trackedEntities.Should().BeEmpty();
        }

        [Fact]
        public async Task VerifyResetCodeAsync_ShouldNotTrackPasswordResetTokenEntity()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1
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
            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            var request = new VerifyResetCodeRequest
            {
                Email = "test@example.com",
                OtpCode = validOtpCode
            };

            // Act
            var result = await _service.VerifyResetCodeAsync(request);

            // Assert
            result.Should().NotBeNull();
            var trackedTokens = _context.ChangeTracker.Entries<PasswordResetToken>().ToList();
            trackedTokens.Should().BeEmpty();
        }

        #endregion
    }
}
