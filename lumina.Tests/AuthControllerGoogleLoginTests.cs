using DataLayer.DTOs.Auth;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Auth;

namespace Lumina.Tests
{
    public class AuthControllerGoogleLoginTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IPasswordResetService> _mockPasswordResetService;
        private readonly AuthController _controller;

        public AuthControllerGoogleLoginTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockPasswordResetService = new Mock<IPasswordResetService>();
            _controller = new AuthController(_mockAuthService.Object, _mockPasswordResetService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task GoogleLogin_WithValidToken_ShouldReturn200OK()
        {
            // Arrange
            var request = new GoogleLoginRequest
            {
                Token = "valid.google.token.with.minimum.length"
            };

            var expectedResponse = new LoginResponse
            {
                Token = "jwt-token",
                ExpiresIn = 3600,
                User = new AuthUserResponse
                {
                    Id = "1",
                    Username = "googleuser",
                    Email = "user@gmail.com",
                    Name = "Google User"
                }
            };

            _mockAuthService.Setup(s => s.GoogleLoginAsync(It.IsAny<GoogleLoginRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GoogleLogin(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            
            var actualResponse = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Token, actualResponse.Token);
            Assert.Equal(expectedResponse.ExpiresIn, actualResponse.ExpiresIn);
            Assert.Equal(expectedResponse.User.Email, actualResponse.User.Email);
        }

        [Fact]
        public async Task GoogleLogin_WithValidToken_ShouldCallServiceOnce()
        {
            // Arrange
            var request = new GoogleLoginRequest
            {
                Token = "valid.google.token.with.minimum.length"
            };

            var expectedResponse = new LoginResponse
            {
                Token = "jwt-token",
                ExpiresIn = 3600,
                User = new AuthUserResponse()
            };

            _mockAuthService.Setup(s => s.GoogleLoginAsync(It.IsAny<GoogleLoginRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.GoogleLogin(request);

            // Assert
            _mockAuthService.Verify(s => s.GoogleLoginAsync(It.Is<GoogleLoginRequest>(
                r => r.Token == request.Token
            )), Times.Once);
        }

        #endregion

        #region Invalid ModelState Tests

        [Fact]
        public async Task GoogleLogin_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new GoogleLoginRequest
            {
                Token = "valid.google.token.with.minimum.length"
            };

            _controller.ModelState.AddModelError("Token", "Token is required");

            // Act
            var result = await _controller.GoogleLogin(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GoogleLogin_WithInvalidModelState_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new GoogleLoginRequest
            {
                Token = "valid.google.token.with.minimum.length"
            };

            _controller.ModelState.AddModelError("Token", "Token is required");

            // Act
            var result = await _controller.GoogleLogin(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid Google login request", errorResponse.Error);
        }

        [Fact]
        public async Task GoogleLogin_WithInvalidModelState_ShouldNotCallService()
        {
            // Arrange
            var request = new GoogleLoginRequest
            {
                Token = "valid.google.token.with.minimum.length"
            };

            _controller.ModelState.AddModelError("Token", "Token is required");

            // Act
            await _controller.GoogleLogin(request);

            // Assert
            _mockAuthService.Verify(s => s.GoogleLoginAsync(It.IsAny<GoogleLoginRequest>()), Times.Never);
        }

        #endregion

        #region AuthServiceException Tests

        [Fact]
        public async Task GoogleLogin_WhenServiceThrowsUnauthorizedException_ShouldReturn401Unauthorized()
        {
            // Arrange
            var request = new GoogleLoginRequest
            {
                Token = "invalid.google.token"
            };

            _mockAuthService.Setup(s => s.GoogleLoginAsync(It.IsAny<GoogleLoginRequest>()))
                .ThrowsAsync(AuthServiceException.Unauthorized("Invalid Google token"));

            // Act
            var result = await _controller.GoogleLogin(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Invalid Google token", errorResponse.Error);
        }

        [Fact]
        public async Task GoogleLogin_WhenServiceThrowsBadRequestException_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new GoogleLoginRequest
            {
                Token = "malformed.token"
            };

            _mockAuthService.Setup(s => s.GoogleLoginAsync(It.IsAny<GoogleLoginRequest>()))
                .ThrowsAsync(AuthServiceException.BadRequest("Malformed token"));

            // Act
            var result = await _controller.GoogleLogin(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Malformed token", errorResponse.Error);
        }

        [Fact]
        public async Task GoogleLogin_WhenServiceThrowsServerErrorException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var request = new GoogleLoginRequest
            {
                Token = "valid.google.token.with.minimum.length"
            };

            _mockAuthService.Setup(s => s.GoogleLoginAsync(It.IsAny<GoogleLoginRequest>()))
                .ThrowsAsync(AuthServiceException.ServerError("Google API connection failed"));

            // Act
            var result = await _controller.GoogleLogin(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Google API connection failed", errorResponse.Error);
        }

        #endregion
    }
}
