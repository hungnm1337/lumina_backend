using DataLayer.DTOs.Auth;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Auth;

namespace Lumina.Tests
{
    public class AuthControllerForgotPasswordTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IPasswordResetService> _mockPasswordResetService;
        private readonly AuthController _controller;

        public AuthControllerForgotPasswordTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockPasswordResetService = new Mock<IPasswordResetService>();
            _controller = new AuthController(_mockAuthService.Object, _mockPasswordResetService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task ForgotPassword_WithValidEmail_ShouldReturn200OK()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "user@example.com"
            };

            var expectedResponse = new ForgotPasswordResponse
            {
                Message = "Password reset code has been sent to your email"
            };

            _mockPasswordResetService.Setup(s => s.SendPasswordResetCodeAsync(It.IsAny<ForgotPasswordRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ForgotPassword(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            
            var actualResponse = Assert.IsType<ForgotPasswordResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Message, actualResponse.Message);
        }

        [Fact]
        public async Task ForgotPassword_WithValidEmail_ShouldCallServiceOnce()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "user@example.com"
            };

            var expectedResponse = new ForgotPasswordResponse
            {
                Message = "Password reset code has been sent to your email"
            };

            _mockPasswordResetService.Setup(s => s.SendPasswordResetCodeAsync(It.IsAny<ForgotPasswordRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.ForgotPassword(request);

            // Assert
            _mockPasswordResetService.Verify(s => s.SendPasswordResetCodeAsync(It.Is<ForgotPasswordRequest>(
                r => r.Email == request.Email
            )), Times.Once);
        }

        #endregion

        #region Invalid ModelState Tests

        [Fact]
        public async Task ForgotPassword_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "user@example.com"
            };

            _controller.ModelState.AddModelError("Email", "Invalid email format");

            // Act
            var result = await _controller.ForgotPassword(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task ForgotPassword_WithInvalidModelState_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "user@example.com"
            };

            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.ForgotPassword(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid forgot password request", errorResponse.Error);
        }

        [Fact]
        public async Task ForgotPassword_WithInvalidModelState_ShouldNotCallService()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "user@example.com"
            };

            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            await _controller.ForgotPassword(request);

            // Assert
            _mockPasswordResetService.Verify(s => s.SendPasswordResetCodeAsync(It.IsAny<ForgotPasswordRequest>()), Times.Never);
        }

        #endregion

        #region AuthServiceException Tests

        [Fact]
        public async Task ForgotPassword_WhenServiceThrowsNotFoundException_ShouldReturn404NotFound()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "nonexistent@example.com"
            };

            _mockPasswordResetService.Setup(s => s.SendPasswordResetCodeAsync(It.IsAny<ForgotPasswordRequest>()))
                .ThrowsAsync(AuthServiceException.NotFound("Email not found"));

            // Act
            var result = await _controller.ForgotPassword(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Email not found", errorResponse.Error);
        }

        [Fact]
        public async Task ForgotPassword_WhenServiceThrowsBadRequestException_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "invalid@example.com"
            };

            _mockPasswordResetService.Setup(s => s.SendPasswordResetCodeAsync(It.IsAny<ForgotPasswordRequest>()))
                .ThrowsAsync(AuthServiceException.BadRequest("Invalid email format"));

            // Act
            var result = await _controller.ForgotPassword(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Invalid email format", errorResponse.Error);
        }

        [Fact]
        public async Task ForgotPassword_WhenServiceThrowsServerErrorException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "user@example.com"
            };

            _mockPasswordResetService.Setup(s => s.SendPasswordResetCodeAsync(It.IsAny<ForgotPasswordRequest>()))
                .ThrowsAsync(AuthServiceException.ServerError("Email service unavailable"));

            // Act
            var result = await _controller.ForgotPassword(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Email service unavailable", errorResponse.Error);
        }

        #endregion
    }
}
