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
    public class VerifyRegistrationAsyncUnitTest
    {
        private readonly LuminaSystemContext _context;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<IGoogleAuthService> _mockGoogleAuthService;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public VerifyRegistrationAsyncUnitTest()
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
        public async Task VerifyRegistrationAsync_WithValidDataAndValidOtp_ShouldReturnVerifyRegistrationResponse()
        {
            // Arrange
            const string email = "user@example.com";
            const string username = "testuser";
            const string name = "Test User";
            const string password = "Password@123";
            const string otpCode = "123456";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var tempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Pending",
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);
            var now = DateTime.UtcNow;
            var registrationToken = new PasswordResetToken
            {
                UserId = tempUser.UserId,
                CodeHash = otpHash,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(10),
                UsedAt = null
            };

            _context.PasswordResetTokens.Add(registrationToken);
            await _context.SaveChangesAsync();

            var request = new VerifyRegistrationRequest
            {
                Email = email,
                Username = username,
                Name = name,
                Password = password,
                OtpCode = otpCode
            };

            var tokenResult = new JwtTokenResult(
                "jwt_token_value",
                3600,
                DateTime.UtcNow.AddSeconds(3600)
            );

            _mockJwtTokenService
                .Setup(s => s.GenerateToken(It.IsAny<User>()))
                .Returns(tokenResult);

            _mockJwtTokenService
                .Setup(s => s.GenerateRefreshToken())
                .Returns("refresh_token_value");

            // Act
            var result = await _authService.VerifyRegistrationAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().NotBeEmpty();
            result.Token.Should().Be("jwt_token_value");
            result.RefreshToken.Should().Be("refresh_token_value");
            result.ExpiresIn.Should().Be(3600);
            result.RefreshExpiresIn.Should().Be(604800); // 7 days in seconds
            result.User.Email.Should().Be(email);
            result.User.Username.Should().Be(username);
            result.User.Name.Should().Be(name);

            // Verify user is activated and name is updated
            var activatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            activatedUser.Should().NotBeNull();
            activatedUser!.IsActive.Should().BeTrue();
            activatedUser.FullName.Should().Be(name);

            // Verify account is created
            var createdAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == username);
            createdAccount.Should().NotBeNull();
            createdAccount!.UserId.Should().Be(tempUser.UserId);
            createdAccount.PasswordHash.Should().NotBeEmpty();

            // Verify token is marked as used
            var usedToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.UserId == tempUser.UserId);
            usedToken!.UsedAt.Should().NotBeNull();

            // Verify refresh token is created
            var createdRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == tempUser.UserId);
            createdRefreshToken.Should().NotBeNull();

            _mockJwtTokenService.Verify(s => s.GenerateToken(It.IsAny<User>()), Times.Once);
            _mockJwtTokenService.Verify(s => s.GenerateRefreshToken(), Times.Once);
        }

        [Fact]
        public async Task VerifyRegistrationAsync_WithEmptyName_ShouldThrowBadRequestException()
        {
            // Arrange
            var request = new VerifyRegistrationRequest
            {
                Email = "user@example.com",
                Username = "testuser",
                Name = "   ", // whitespace only
                Password = "Password@123",
                OtpCode = "123456"
            };

            // Act
            var act = () => _authService.VerifyRegistrationAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 400)
                .WithMessage("Tên không ???c ?? tr?ng");
        }

        [Fact]
        public async Task VerifyRegistrationAsync_WithTempUserNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var request = new VerifyRegistrationRequest
            {
                Email = "nonexistent@example.com",
                Username = "testuser",
                Name = "Test User",
                Password = "Password@123",
                OtpCode = "123456"
            };

            // Act
            var act = () => _authService.VerifyRegistrationAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 404)
                .WithMessage("Không tìm th?y thông tin ??ng ký. Vui lòng yêu c?u mã OTP m?i.");
        }

        [Fact]
        public async Task VerifyRegistrationAsync_WithExpiredOtpToken_ShouldThrowBadRequestException()
        {
            // Arrange
            const string email = "user@example.com";
            const string otpCode = "123456";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var tempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Pending",
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);
            var now = DateTime.UtcNow;
            var expiredToken = new PasswordResetToken
            {
                UserId = tempUser.UserId,
                CodeHash = otpHash,
                CreatedAt = now.AddMinutes(-15),
                ExpiresAt = now.AddMinutes(-5), // Already expired
                UsedAt = null
            };

            _context.PasswordResetTokens.Add(expiredToken);
            await _context.SaveChangesAsync();

            var request = new VerifyRegistrationRequest
            {
                Email = email,
                Username = "testuser",
                Name = "Test User",
                Password = "Password@123",
                OtpCode = otpCode
            };

            // Act
            var act = () => _authService.VerifyRegistrationAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 400)
                .WithMessage("OTP không h?p l? ho?c ?ã h?t h?n");
        }

        [Fact]
        public async Task VerifyRegistrationAsync_WithIncorrectOtpCode_ShouldThrowBadRequestException()
        {
            // Arrange
            const string email = "user@example.com";
            const string correctOtpCode = "123456";
            const string incorrectOtpCode = "654321";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var tempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Pending",
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            var otpHash = BCrypt.Net.BCrypt.HashPassword(correctOtpCode);
            var now = DateTime.UtcNow;
            var registrationToken = new PasswordResetToken
            {
                UserId = tempUser.UserId,
                CodeHash = otpHash,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(10),
                UsedAt = null
            };

            _context.PasswordResetTokens.Add(registrationToken);
            await _context.SaveChangesAsync();

            var request = new VerifyRegistrationRequest
            {
                Email = email,
                Username = "testuser",
                Name = "Test User",
                Password = "Password@123",
                OtpCode = incorrectOtpCode
            };

            // Act
            var act = () => _authService.VerifyRegistrationAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 400)
                .WithMessage("Mã OTP không ?úng");
        }

        [Fact]
        public async Task VerifyRegistrationAsync_WithAlreadyRegisteredEmail_ShouldThrowConflictException()
        {
            // Arrange
            const string email = "user@example.com";
            const string otpCode = "123456";

            var role = new Role { RoleId = 4, RoleName = "User" };
            
            // Existing active user with same email
            var activeUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Existing User",
                RoleId = 4,
                Role = role,
                IsActive = true,
                CurrentStreak = 0
            };

            // Temp user for registration
            var tempUser = new User
            {
                UserId = 2,
                Email = email,
                FullName = "Pending",
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(activeUser);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);
            var now = DateTime.UtcNow;
            var registrationToken = new PasswordResetToken
            {
                UserId = tempUser.UserId,
                CodeHash = otpHash,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(10),
                UsedAt = null
            };

            _context.PasswordResetTokens.Add(registrationToken);
            await _context.SaveChangesAsync();

            var request = new VerifyRegistrationRequest
            {
                Email = email,
                Username = "testuser",
                Name = "Test User",
                Password = "Password@123",
                OtpCode = otpCode
            };

            // Act
            var act = () => _authService.VerifyRegistrationAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 409)
                .WithMessage("Email ?ã ???c ??ng ký");
        }

        [Fact]
        public async Task VerifyRegistrationAsync_WithAlreadyTakenUsername_ShouldThrowConflictException()
        {
            // Arrange
            const string email = "user@example.com";
            const string username = "takenuser";
            const string otpCode = "123456";

            var role = new Role { RoleId = 4, RoleName = "User" };
            
            // Existing user with taken username
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

            var existingAccount = new Account
            {
                AccountId = 1,
                UserId = existingUser.UserId,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123"),
                CreateAt = DateTime.UtcNow
            };

            // Temp user for registration
            var tempUser = new User
            {
                UserId = 2,
                Email = email,
                FullName = "Pending",
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(existingUser);
            _context.Accounts.Add(existingAccount);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);
            var now = DateTime.UtcNow;
            var registrationToken = new PasswordResetToken
            {
                UserId = tempUser.UserId,
                CodeHash = otpHash,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(10),
                UsedAt = null
            };

            _context.PasswordResetTokens.Add(registrationToken);
            await _context.SaveChangesAsync();

            var request = new VerifyRegistrationRequest
            {
                Email = email,
                Username = username,
                Name = "Test User",
                Password = "Password@123",
                OtpCode = otpCode
            };

            // Act
            var act = () => _authService.VerifyRegistrationAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 409)
                .WithMessage("Tên ??ng nh?p ?ã t?n t?i");
        }

        [Fact]
        public async Task VerifyRegistrationAsync_WithLongNameAndUsername_ShouldTruncateAndSucceed()
        {
            // Arrange
            const string email = "user@example.com";
            const string longUsername = "abcdefghijklmnopqrstu"; // 21 chars, max is 20
            const string longName = "This is a very long name that exceeds fifty characters for sure"; // > 50 chars
            const string password = "Password@123";
            const string otpCode = "123456";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var tempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Pending",
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);
            var now = DateTime.UtcNow;
            var registrationToken = new PasswordResetToken
            {
                UserId = tempUser.UserId,
                CodeHash = otpHash,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(10),
                UsedAt = null
            };

            _context.PasswordResetTokens.Add(registrationToken);
            await _context.SaveChangesAsync();

            var request = new VerifyRegistrationRequest
            {
                Email = email,
                Username = longUsername,
                Name = longName,
                Password = password,
                OtpCode = otpCode
            };

            var tokenResult = new JwtTokenResult(
                "jwt_token_value",
                3600,
                DateTime.UtcNow.AddSeconds(3600)
            );

            _mockJwtTokenService
                .Setup(s => s.GenerateToken(It.IsAny<User>()))
                .Returns(tokenResult);

            _mockJwtTokenService
                .Setup(s => s.GenerateRefreshToken())
                .Returns("refresh_token_value");

            // Act
            var result = await _authService.VerifyRegistrationAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.User.Username.Should().HaveLength(20); // Truncated to max length
            result.User.Name.Should().HaveLength(50); // Truncated to max length

            var activatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            activatedUser!.FullName.Should().HaveLength(50);

            var createdAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == tempUser.UserId);
            createdAccount!.Username.Should().HaveLength(20);
        }

        [Fact]
        public async Task VerifyRegistrationAsync_WithNoValidTokenForUser_ShouldThrowBadRequestException()
        {
            // Arrange
            const string email = "user@example.com";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var tempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Pending",
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            // No token created for this user

            var request = new VerifyRegistrationRequest
            {
                Email = email,
                Username = "testuser",
                Name = "Test User",
                Password = "Password@123",
                OtpCode = "123456"
            };

            // Act
            var act = () => _authService.VerifyRegistrationAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 400)
                .WithMessage("OTP không h?p l? ho?c ?ã h?t h?n");
        }

        [Fact]
        public async Task VerifyRegistrationAsync_WithUsedTokenForUser_ShouldThrowBadRequestException()
        {
            // Arrange
            const string email = "user@example.com";
            const string otpCode = "123456";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var tempUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Pending",
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            _context.Roles.Add(role);
            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);
            var now = DateTime.UtcNow;
            var usedToken = new PasswordResetToken
            {
                UserId = tempUser.UserId,
                CodeHash = otpHash,
                CreatedAt = now.AddMinutes(-10),
                ExpiresAt = now.AddMinutes(10),
                UsedAt = now.AddMinutes(-5) // Already used
            };

            _context.PasswordResetTokens.Add(usedToken);
            await _context.SaveChangesAsync();

            var request = new VerifyRegistrationRequest
            {
                Email = email,
                Username = "testuser",
                Name = "Test User",
                Password = "Password@123",
                OtpCode = otpCode
            };

            // Act
            var act = () => _authService.VerifyRegistrationAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 400)
                .WithMessage("OTP không h?p l? ho?c ?ã h?t h?n");
        }
    }
}
