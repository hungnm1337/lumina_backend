using DataLayer.DTOs.Auth;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Auth;

namespace Lumina.Tests
{
    public class AuthControllerSendRegistrationOtpTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IPasswordResetService> _mockPasswordResetService;
        private readonly AuthController _controller;

        public AuthControllerSendRegistrationOtpTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockPasswordResetService = new Mock<IPasswordResetService>();
            _controller = new AuthController(_mockAuthService.Object, _mockPasswordResetService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task SendRegistrationOtp_WithValidRequest_ShouldReturn200OK()
        {
            // Arrange
            var request = new SendRegistrationOtpRequest
            {
                Email = "newuser@example.com",
                Username = "newuser123"
            };

            var expectedResponse = new SendRegistrationOtpResponse
            {
                Message = "OTP has been sent to your email"
            };

            _mockAuthService.Setup(s => s.SendRegistrationOtpAsync(It.IsAny<SendRegistrationOtpRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SendRegistrationOtp(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            
            var actualResponse = Assert.IsType<SendRegistrationOtpResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Message, actualResponse.Message);
        }

        [Fact]
        public async Task SendRegistrationOtp_WithValidRequest_ShouldCallServiceOnce()
        {
            // Arrange
            var request = new SendRegistrationOtpRequest
            {
                Email = "newuser@example.com",
                Username = "newuser123"
            };

            var expectedResponse = new SendRegistrationOtpResponse
            {
                Message = "OTP has been sent to your email"
            };

            _mockAuthService.Setup(s => s.SendRegistrationOtpAsync(It.IsAny<SendRegistrationOtpRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.SendRegistrationOtp(request);

            // Assert
            _mockAuthService.Verify(s => s.SendRegistrationOtpAsync(It.Is<SendRegistrationOtpRequest>(
                r => r.Email == request.Email && r.Username == request.Username
            )), Times.Once);
        }

        #endregion

        #region Invalid ModelState Tests

        [Fact]
        public async Task SendRegistrationOtp_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new SendRegistrationOtpRequest
            {
                Email = "newuser@example.com",
                Username = "newuser123"
            };

            _controller.ModelState.AddModelError("Email", "Invalid email format");

            // Act
            var result = await _controller.SendRegistrationOtp(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SendRegistrationOtp_WithInvalidModelState_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new SendRegistrationOtpRequest
            {
                Email = "newuser@example.com",
                Username = "newuser123"
            };

            _controller.ModelState.AddModelError("Username", "Username is required");

            // Act
            var result = await _controller.SendRegistrationOtp(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid request", errorResponse.Error);
        }

        [Fact]
        public async Task SendRegistrationOtp_WithInvalidModelState_ShouldNotCallService()
        {
            // Arrange
            var request = new SendRegistrationOtpRequest
            {
                Email = "newuser@example.com",
                Username = "newuser123"
            };

            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            await _controller.SendRegistrationOtp(request);

            // Assert
            _mockAuthService.Verify(s => s.SendRegistrationOtpAsync(It.IsAny<SendRegistrationOtpRequest>()), Times.Never);
        }

        #endregion

        #region AuthServiceException Tests

        [Fact]
        public async Task SendRegistrationOtp_WhenServiceThrowsConflictException_ShouldReturn409Conflict()
        {
            // Arrange
            var request = new SendRegistrationOtpRequest
            {
                Email = "existing@example.com",
                Username = "existinguser"
            };

            _mockAuthService.Setup(s => s.SendRegistrationOtpAsync(It.IsAny<SendRegistrationOtpRequest>()))
                .ThrowsAsync(AuthServiceException.Conflict("Email already exists"));

            // Act
            var result = await _controller.SendRegistrationOtp(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status409Conflict, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Email already exists", errorResponse.Error);
        }

        [Fact]
        public async Task SendRegistrationOtp_WhenServiceThrowsBadRequestException_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new SendRegistrationOtpRequest
            {
                Email = "invalid@example.com",
                Username = "ab"
            };

            _mockAuthService.Setup(s => s.SendRegistrationOtpAsync(It.IsAny<SendRegistrationOtpRequest>()))
                .ThrowsAsync(AuthServiceException.BadRequest("Username too short"));

            // Act
            var result = await _controller.SendRegistrationOtp(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Username too short", errorResponse.Error);
        }

        [Fact]
        public async Task SendRegistrationOtp_WhenServiceThrowsServerErrorException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var request = new SendRegistrationOtpRequest
            {
                Email = "newuser@example.com",
                Username = "newuser123"
            };

            _mockAuthService.Setup(s => s.SendRegistrationOtpAsync(It.IsAny<SendRegistrationOtpRequest>()))
                .ThrowsAsync(AuthServiceException.ServerError("Email service unavailable"));

            // Act
            var result = await _controller.SendRegistrationOtp(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Email service unavailable", errorResponse.Error);
        }

        #endregion
    }
}
