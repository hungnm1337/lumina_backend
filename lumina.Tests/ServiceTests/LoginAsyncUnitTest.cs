using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Auth;
using ServiceLayer.Email;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Lumina.Tests.Helpers;

namespace Lumina.Tests.ServiceTests
{
    public class LoginAsyncUnitTest
    {
        private readonly LuminaSystemContext _context;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<IGoogleAuthService> _mockGoogleAuthService;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public LoginAsyncUnitTest()
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
        public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponse()
        {
            // Arrange
            const string username = "testuser";
            const string email = "test@example.com";
            const string password = "password123";
            
            var role = new Role { RoleId = 4, RoleName = "User" };
            var user = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Test User",
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
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                AuthProvider = null,
                CreateAt = DateTime.UtcNow,
                User = user
            };

            _context.Roles.Add(role);
            _context.Users.Add(user);
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var request = new LoginRequestDTO
            {
                Username = username,
                Password = password
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
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be("jwt_token_value");
            result.ExpiresIn.Should().Be(3600);
            result.RefreshToken.Should().Be("refresh_token_value");
            result.User.Should().NotBeNull();
            result.User.Username.Should().Be(username);
            result.User.Email.Should().Be(email);
        }

        [Fact]
        public async Task LoginAsync_WhenAccountNotFound_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var request = new LoginRequestDTO
            {
                Username = "nonexistent",
                Password = "password123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthServiceException>(
                async () => await _authService.LoginAsync(request)
            );

            exception.StatusCode.Should().Be(401);
            exception.Message.Should().Be("Invalid username or password");
        }

        [Fact]
        public async Task LoginAsync_WhenPasswordIsInvalid_ShouldThrowUnauthorizedException()
        {
            // Arrange
            const string username = "testuser";
            const string email = "test@example.com";
            const string correctPassword = "password123";
            const string wrongPassword = "wrongpassword";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var user = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Test User",
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
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword),
                AuthProvider = null,
                CreateAt = DateTime.UtcNow,
                User = user
            };

            _context.Roles.Add(role);
            _context.Users.Add(user);
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var request = new LoginRequestDTO
            {
                Username = username,
                Password = wrongPassword
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthServiceException>(
                async () => await _authService.LoginAsync(request)
            );

            exception.StatusCode.Should().Be(401);
            exception.Message.Should().Be("Invalid username or password");
        }

        [Fact]
        public async Task LoginAsync_WhenUserIsInactive_ShouldThrowUnauthorizedException()
        {
            // Arrange
            const string username = "testuser";
            const string email = "test@example.com";
            const string password = "password123";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var user = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Test User",
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            var account = new Account
            {
                AccountId = 1,
                UserId = 1,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                AuthProvider = null,
                CreateAt = DateTime.UtcNow,
                User = user
            };

            _context.Roles.Add(role);
            _context.Users.Add(user);
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var request = new LoginRequestDTO
            {
                Username = username,
                Password = password
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthServiceException>(
                async () => await _authService.LoginAsync(request)
            );

            exception.StatusCode.Should().Be(401);
            exception.Message.Should().Be("Account is inactive");
        }

        [Fact]
        public async Task LoginAsync_WhenPasswordHashIsNullOrEmpty_ShouldThrowUnauthorizedException()
        {
            // Arrange
            const string username = "testuser";
            const string email = "test@example.com";
            const string password = "password123";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var user = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Test User",
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
                PasswordHash = null,
                AuthProvider = null,
                CreateAt = DateTime.UtcNow,
                User = user
            };

            _context.Roles.Add(role);
            _context.Users.Add(user);
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var request = new LoginRequestDTO
            {
                Username = username,
                Password = password
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthServiceException>(
                async () => await _authService.LoginAsync(request)
            );

            exception.StatusCode.Should().Be(401);
            exception.Message.Should().Be("Invalid username or password");
        }

        [Fact]
        public async Task LoginAsync_WhenFindingByEmail_ShouldReturnLoginResponse()
        {
            // Arrange
            const string username = "testuser";
            const string email = "test@example.com";
            const string password = "password123";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var user = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Test User",
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
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                AuthProvider = null,
                CreateAt = DateTime.UtcNow,
                User = user
            };

            _context.Roles.Add(role);
            _context.Users.Add(user);
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var request = new LoginRequestDTO
            {
                Username = email,
                Password = password
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
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.User.Email.Should().Be(email);
        }
    }
}
