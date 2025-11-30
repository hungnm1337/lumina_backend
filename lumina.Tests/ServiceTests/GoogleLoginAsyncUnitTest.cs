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
    public class GoogleLoginAsyncUnitTest
    {
        private readonly LuminaSystemContext _context;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<IGoogleAuthService> _mockGoogleAuthService;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public GoogleLoginAsyncUnitTest()
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
        public async Task GoogleLoginAsync_WithValidTokenAndExistingAccount_ShouldReturnLoginResponse()
        {
            // Arrange
            const string googleSubject = "google123";
            const string email = "user@example.com";
            const string userName = "googleuser";
            const string googleToken = "valid_google_token";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var user = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Google User",
                RoleId = 4,
                Role = role,
                IsActive = true,
                CurrentStreak = 0
            };

            var account = new Account
            {
                AccountId = 1,
                UserId = 1,
                Username = userName,
                AuthProvider = "Google",
                ProviderUserId = googleSubject,
                AccessToken = "old_token",
                CreateAt = DateTime.UtcNow,
                User = user
            };

            _context.Roles.Add(role);
            _context.Users.Add(user);
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var googleUserInfo = new GoogleUserInfo
            {
                Subject = googleSubject,
                Email = email,
                Name = "Google User",
                ExpirationTimeSeconds = null
            };

            var request = new GoogleLoginRequest { Token = googleToken };

            var tokenResult = new JwtTokenResult(
                "jwt_token_value",
                3600,
                DateTime.UtcNow.AddSeconds(3600)
            );

            _mockGoogleAuthService
                .Setup(s => s.ValidateGoogleTokenAsync(googleToken))
                .ReturnsAsync(googleUserInfo);

            _mockJwtTokenService
                .Setup(s => s.GenerateToken(It.IsAny<User>()))
                .Returns(tokenResult);

            _mockJwtTokenService
                .Setup(s => s.GenerateRefreshToken())
                .Returns("refresh_token_value");

            // Act
            var result = await _authService.GoogleLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be("jwt_token_value");
            result.RefreshToken.Should().Be("refresh_token_value");
            result.ExpiresIn.Should().Be(3600);
            result.User.Email.Should().Be(email);
            result.User.Username.Should().Be(userName);

            _mockGoogleAuthService.Verify(s => s.ValidateGoogleTokenAsync(googleToken), Times.Once);
            _mockJwtTokenService.Verify(s => s.GenerateToken(It.IsAny<User>()), Times.Once);
            _mockJwtTokenService.Verify(s => s.GenerateRefreshToken(), Times.Once);
        }

        [Fact]
        public async Task GoogleLoginAsync_WithValidTokenAndNewAccount_ShouldCreateAndReturnLoginResponse()
        {
            // Arrange
            const string googleSubject = "new_google_user_123";
            const string email = "newuser@gmail.com";
            const string googleToken = "valid_google_token";

            var role = new Role { RoleId = 4, RoleName = "User" };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            var googleUserInfo = new GoogleUserInfo
            {
                Subject = googleSubject,
                Email = email,
                Name = "New Google User",
                ExpirationTimeSeconds = 1234567890
            };

            var request = new GoogleLoginRequest { Token = googleToken };

            var tokenResult = new JwtTokenResult(
                "jwt_token_value",
                3600,
                DateTime.UtcNow.AddSeconds(3600)
            );

            _mockGoogleAuthService
                .Setup(s => s.ValidateGoogleTokenAsync(googleToken))
                .ReturnsAsync(googleUserInfo);

            _mockJwtTokenService
                .Setup(s => s.GenerateToken(It.IsAny<User>()))
                .Returns(tokenResult);

            _mockJwtTokenService
                .Setup(s => s.GenerateRefreshToken())
                .Returns("refresh_token_value");

            // Act
            var result = await _authService.GoogleLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be("jwt_token_value");
            result.RefreshToken.Should().Be("refresh_token_value");
            result.ExpiresIn.Should().Be(3600);
            result.User.Email.Should().Be(email);
            result.User.Name.Should().Be("New Google User");

            var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            createdUser.Should().NotBeNull();
            createdUser!.IsActive.Should().BeTrue();
            createdUser.RoleId.Should().Be(4);

            var createdAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.ProviderUserId == googleSubject);
            createdAccount.Should().NotBeNull();
            createdAccount!.AuthProvider.Should().Be("Google");
            createdAccount.AccessToken.Should().Be(googleToken);
        }

        [Fact]
        public async Task GoogleLoginAsync_WithInvalidGoogleToken_ShouldThrowUnauthorizedException()
        {
            // Arrange
            const string invalidToken = "invalid_google_token";
            var request = new GoogleLoginRequest { Token = invalidToken };

            var googleAuthException = GoogleAuthException.InvalidToken("Invalid Google token");

            _mockGoogleAuthService
                .Setup(s => s.ValidateGoogleTokenAsync(invalidToken))
                .ThrowsAsync(googleAuthException);

            // Act
            var act = () => _authService.GoogleLoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 401)
                .WithMessage("Invalid Google token");

            _mockGoogleAuthService.Verify(s => s.ValidateGoogleTokenAsync(invalidToken), Times.Once);
        }

        [Fact]
        public async Task GoogleLoginAsync_WithInactiveAccount_ShouldThrowUnauthorizedException()
        {
            // Arrange
            const string googleSubject = "google_inactive_user";
            const string email = "inactive@example.com";
            const string userName = "inactiveuser";
            const string googleToken = "valid_token";

            var role = new Role { RoleId = 4, RoleName = "User" };
            var inactiveUser = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Inactive User",
                RoleId = 4,
                Role = role,
                IsActive = false,
                CurrentStreak = 0
            };

            var account = new Account
            {
                AccountId = 1,
                UserId = 1,
                Username = userName,
                AuthProvider = "Google",
                ProviderUserId = googleSubject,
                AccessToken = "token",
                CreateAt = DateTime.UtcNow,
                User = inactiveUser
            };

            _context.Roles.Add(role);
            _context.Users.Add(inactiveUser);
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var googleUserInfo = new GoogleUserInfo
            {
                Subject = googleSubject,
                Email = email,
                Name = "Inactive User",
                ExpirationTimeSeconds = null
            };

            var request = new GoogleLoginRequest { Token = googleToken };

            _mockGoogleAuthService
                .Setup(s => s.ValidateGoogleTokenAsync(googleToken))
                .ReturnsAsync(googleUserInfo);

            // Act
            var act = () => _authService.GoogleLoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 401)
                .WithMessage("Account is inactive");

            _mockGoogleAuthService.Verify(s => s.ValidateGoogleTokenAsync(googleToken), Times.Once);
        }

        [Fact]
        public async Task GoogleLoginAsync_WithMissingRole_ShouldThrowInvalidOperationException()
        {
            // Arrange
            const string googleSubject = "google_no_role_user";
            const string email = "norole@example.com";
            const string googleToken = "valid_token";

            // Note: Don't add default role (RoleId = 4) to database so it won't be found
            var googleUserInfo = new GoogleUserInfo
            {
                Subject = googleSubject,
                Email = email,
                Name = "No Role User",
                ExpirationTimeSeconds = null
            };

            var request = new GoogleLoginRequest { Token = googleToken };

            _mockGoogleAuthService
                .Setup(s => s.ValidateGoogleTokenAsync(googleToken))
                .ReturnsAsync(googleUserInfo);

            // Act
            var act = () => _authService.GoogleLoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Role 4 not found*");

            _mockGoogleAuthService.Verify(s => s.ValidateGoogleTokenAsync(googleToken), Times.Once);
        }

        [Fact]
        public async Task GoogleLoginAsync_WithGoogleAuthenticationException_ShouldThrowUnauthorizedException()
        {
            // Arrange
            const string googleToken = "problematic_token";
            var request = new GoogleLoginRequest { Token = googleToken };

            var googleAuthException = GoogleAuthException.ValidationFailed("Failed to verify Google token");

            _mockGoogleAuthService
                .Setup(s => s.ValidateGoogleTokenAsync(googleToken))
                .ThrowsAsync(googleAuthException);

            // Act
            var act = () => _authService.GoogleLoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<AuthServiceException>()
                .Where(e => e.StatusCode == 401)
                .WithMessage("Failed to verify Google token");

            _mockGoogleAuthService.Verify(s => s.ValidateGoogleTokenAsync(googleToken), Times.Once);
        }
    }
}
