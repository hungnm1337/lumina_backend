using DataLayer.DTOs.Auth;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Auth;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class AuthControllerResendRegistrationOtpTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IPasswordResetService> _mockPasswordResetService;
        private readonly AuthController _controller;

        public AuthControllerResendRegistrationOtpTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockPasswordResetService = new Mock<IPasswordResetService>();
            _controller = new AuthController(_mockAuthService.Object, _mockPasswordResetService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task ResendRegistrationOtp_WithValidRequest_ShouldReturn200OK()
        {
            // Arrange
            var request = new ResendRegistrationOtpRequest
            {
                Email = "user@example.com"
            };

            var expectedResponse = new ResendOtpResponse
            {
                Message = "OTP has been resent to your email"
            };

            _mockAuthService.Setup(s => s.ResendRegistrationOtpAsync(It.IsAny<ResendRegistrationOtpRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ResendRegistrationOtp(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var actualResponse = Assert.IsType<ResendOtpResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Message, actualResponse.Message);
            _mockAuthService.Verify(s => s.ResendRegistrationOtpAsync(It.IsAny<ResendRegistrationOtpRequest>()), Times.Once);
        }

        [Fact]
        public async Task ResendRegistrationOtp_WithValidRequest_ShouldCallServiceOnce()
        {
            // Arrange
            var request = new ResendRegistrationOtpRequest
            {
                Email = "user@example.com"
            };

            var expectedResponse = new ResendOtpResponse
            {
                Message = "OTP has been resent to your email"
            };

            _mockAuthService.Setup(s => s.ResendRegistrationOtpAsync(It.IsAny<ResendRegistrationOtpRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.ResendRegistrationOtp(request);

            // Assert
            _mockAuthService.Verify(s => s.ResendRegistrationOtpAsync(It.Is<ResendRegistrationOtpRequest>(
                r => r.Email == request.Email
            )), Times.Once);
        }

        #endregion

        #region Invalid ModelState Tests

        [Fact]
        public async Task ResendRegistrationOtp_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new ResendRegistrationOtpRequest
            {
                Email = "user@example.com"
            };

            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.ResendRegistrationOtp(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task ResendRegistrationOtp_WithInvalidModelState_ShouldNotCallService()
        {
            // Arrange
            var request = new ResendRegistrationOtpRequest
            {
                Email = "user@example.com"
            };

            _controller.ModelState.AddModelError("Email", "Invalid email format");

            // Act
            await _controller.ResendRegistrationOtp(request);

            // Assert
            _mockAuthService.Verify(s => s.ResendRegistrationOtpAsync(It.IsAny<ResendRegistrationOtpRequest>()), Times.Never);
        }

        #endregion

        #region AuthServiceException Tests

        [Fact]
        public async Task ResendRegistrationOtp_WhenServiceThrowsNotFoundException_ShouldReturn404NotFound()
        {
            // Arrange
            var request = new ResendRegistrationOtpRequest
            {
                Email = "nonexistent@example.com"
            };

            _mockAuthService.Setup(s => s.ResendRegistrationOtpAsync(It.IsAny<ResendRegistrationOtpRequest>()))
                .ThrowsAsync(AuthServiceException.NotFound("Không tìm thấy thông tin đăng ký"));

            // Act
            var result = await _controller.ResendRegistrationOtp(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Không tìm thấy thông tin đăng ký", errorResponse.Error);
        }

        [Fact]
        public async Task ResendRegistrationOtp_WhenServiceThrowsBadRequestException_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new ResendRegistrationOtpRequest
            {
                Email = "user@example.com"
            };

            _mockAuthService.Setup(s => s.ResendRegistrationOtpAsync(It.IsAny<ResendRegistrationOtpRequest>()))
                .ThrowsAsync(AuthServiceException.BadRequest("User already active"));

            // Act
            var result = await _controller.ResendRegistrationOtp(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("User already active", errorResponse.Error);
        }

        [Fact]
        public async Task ResendRegistrationOtp_WhenServiceThrowsServerErrorException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var request = new ResendRegistrationOtpRequest
            {
                Email = "user@example.com"
            };

            _mockAuthService.Setup(s => s.ResendRegistrationOtpAsync(It.IsAny<ResendRegistrationOtpRequest>()))
                .ThrowsAsync(AuthServiceException.ServerError("Email service unavailable"));

            // Act
            var result = await _controller.ResendRegistrationOtp(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Email service unavailable", errorResponse.Error);
        }

        #endregion
    }
}