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
    public class SendPasswordResetCodeAsyncUnitTest : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<PasswordResetService>> _mockLogger;
        private readonly IConfiguration _configuration;
        private readonly PasswordResetService _service;

        public SendPasswordResetCodeAsyncUnitTest()
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
        public async Task SendPasswordResetCodeAsync_WhenEmailNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "nonexistent@example.com"
            };

            // Act
            var act = async () => await _service.SendPasswordResetCodeAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("Email not found");
            exception.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task SendPasswordResetCodeAsync_WhenEmailHasDifferentCase_ShouldNormalizeAndFind()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                Accounts = new List<Account>
                {
                    new Account
                    {
                        AccountId = 1,
                        UserId = 1,
                        Username = "testuser",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123"),
                        AuthProvider = null,
                        CreateAt = DateTime.UtcNow
                    }
                }
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = "TEST@EXAMPLE.COM" // Different case
            };

            _mockEmailSender.Setup(x => x.SendPasswordResetCodeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.SendPasswordResetCodeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("An OTP has been sent to your email");
        }

        #endregion

        #region Test Cases for Account Validation

        [Fact]
        public async Task SendPasswordResetCodeAsync_WhenAccountHasNoPasswordHash_ShouldThrowBadRequestException()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                Accounts = new List<Account>
                {
                    new Account
                    {
                        AccountId = 1,
                        UserId = 1,
                        Username = "testuser",
                        PasswordHash = null, // No password set
                        AuthProvider = null,
                        CreateAt = DateTime.UtcNow
                    }
                }
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = "test@example.com"
            };

            // Act
            var act = async () => await _service.SendPasswordResetCodeAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("This account does not have a password set");
            exception.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task SendPasswordResetCodeAsync_WhenOnlyOAuthAccountExists_ShouldThrowBadRequestException()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                Accounts = new List<Account>
                {
                    new Account
                    {
                        AccountId = 1,
                        UserId = 1,
                        Username = "testuser",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                        AuthProvider = "Google", // OAuth account
                        CreateAt = DateTime.UtcNow
                    }
                }
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = "test@example.com"
            };

            // Act
            var act = async () => await _service.SendPasswordResetCodeAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("This account does not have a password set");
            exception.Which.StatusCode.Should().Be(400);
        }

        #endregion

        #region Test Cases for Existing Tokens

        [Fact]
        public async Task SendPasswordResetCodeAsync_WhenExistingUnusedTokensExist_ShouldRemoveThemAndCreateNew()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                Accounts = new List<Account>
                {
                    new Account
                    {
                        AccountId = 1,
                        UserId = 1,
                        Username = "testuser",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                        AuthProvider = null,
                        CreateAt = DateTime.UtcNow
                    }
                }
            };

            var existingToken1 = new PasswordResetToken
            {
                PasswordResetTokenId = 1,
                UserId = 1,
                CodeHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                UsedAt = null
            };

            var existingToken2 = new PasswordResetToken
            {
                PasswordResetTokenId = 2,
                UserId = 1,
                CodeHash = BCrypt.Net.BCrypt.HashPassword("654321"),
                CreatedAt = DateTime.UtcNow.AddMinutes(-3),
                ExpiresAt = DateTime.UtcNow.AddMinutes(7),
                UsedAt = null
            };

            await _context.Users.AddAsync(user);
            await _context.PasswordResetTokens.AddRangeAsync(existingToken1, existingToken2);
            await _context.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = "test@example.com"
            };

            _mockEmailSender.Setup(x => x.SendPasswordResetCodeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.SendPasswordResetCodeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("An OTP has been sent to your email");

            var remainingTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == 1 && t.UsedAt == null)
                .ToListAsync();

            remainingTokens.Should().HaveCount(1);
            remainingTokens[0].PasswordResetTokenId.Should().NotBe(existingToken1.PasswordResetTokenId);
            remainingTokens[0].PasswordResetTokenId.Should().NotBe(existingToken2.PasswordResetTokenId);
        }

        #endregion

        #region Test Cases for Successful Flow

        [Fact]
        public async Task SendPasswordResetCodeAsync_WhenValidRequest_ShouldCreateTokenAndSendEmail()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                Accounts = new List<Account>
                {
                    new Account
                    {
                        AccountId = 1,
                        UserId = 1,
                        Username = "testuser",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                        AuthProvider = null,
                        CreateAt = DateTime.UtcNow
                    }
                }
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = "test@example.com"
            };

            string capturedOtp = null!;
            _mockEmailSender.Setup(x => x.SendPasswordResetCodeAsync(
                It.Is<string>(email => email == "test@example.com"),
                It.Is<string>(name => name == "Test User"),
                It.IsAny<string>()))
                .Callback<string, string, string>((email, name, otp) => capturedOtp = otp)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.SendPasswordResetCodeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be("An OTP has been sent to your email");

            _mockEmailSender.Verify(x => x.SendPasswordResetCodeAsync(
                "test@example.com",
                "Test User",
                It.IsAny<string>()), Times.Once);

            capturedOtp.Should().NotBeNullOrEmpty();
            capturedOtp.Should().HaveLength(6);
            capturedOtp.Should().MatchRegex("^[0-9]{6}$");

            var savedToken = await _context.PasswordResetTokens
                .Where(t => t.UserId == 1)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            savedToken.Should().NotBeNull();
            savedToken!.UserId.Should().Be(1);
            savedToken.UsedAt.Should().BeNull();
            BCrypt.Net.BCrypt.Verify(capturedOtp, savedToken.CodeHash).Should().BeTrue();
            savedToken.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(5));
        }

        #endregion

        #region Test Cases for Email Sending Failure

        [Fact]
        public async Task SendPasswordResetCodeAsync_WhenEmailSendingFails_ShouldMarkTokenAsUsedAndThrowServerError()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                Accounts = new List<Account>
                {
                    new Account
                    {
                        AccountId = 1,
                        UserId = 1,
                        Username = "testuser",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                        AuthProvider = null,
                        CreateAt = DateTime.UtcNow
                    }
                }
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = "test@example.com"
            };

            _mockEmailSender.Setup(x => x.SendPasswordResetCodeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP connection failed"));

            // Act
            var act = async () => await _service.SendPasswordResetCodeAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<AuthServiceException>();
            exception.Which.Message.Should().Be("Failed to send OTP email");
            exception.Which.StatusCode.Should().Be(500);

            var savedToken = await _context.PasswordResetTokens
                .Where(t => t.UserId == 1)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            savedToken.Should().NotBeNull();
            savedToken!.UsedAt.Should().NotBeNull();
            savedToken.UsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        #endregion

        #region Test Cases for OTP Code Generation

        [Fact]
        public async Task SendPasswordResetCodeAsync_WhenCalledMultipleTimes_ShouldGenerateDifferentOtpCodes()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                RoleId = 1,
                Accounts = new List<Account>
                {
                    new Account
                    {
                        AccountId = 1,
                        UserId = 1,
                        Username = "testuser",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                        AuthProvider = null,
                        CreateAt = DateTime.UtcNow
                    }
                }
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = "test@example.com"
            };

            var capturedOtps = new List<string>();
            _mockEmailSender.Setup(x => x.SendPasswordResetCodeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Callback<string, string, string>((email, name, otp) => capturedOtps.Add(otp))
                .Returns(Task.CompletedTask);

            // Act
            await _service.SendPasswordResetCodeAsync(request);
            await _service.SendPasswordResetCodeAsync(request);
            await _service.SendPasswordResetCodeAsync(request);

            // Assert
            capturedOtps.Should().HaveCount(3);
            capturedOtps.Should().OnlyHaveUniqueItems();
            capturedOtps.Should().AllSatisfy(otp =>
            {
                otp.Should().HaveLength(6);
                otp.Should().MatchRegex("^[0-9]{6}$");
            });
        }

        #endregion
    }
}
