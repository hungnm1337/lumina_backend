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
    public class AuthControllerVerifyForgotPasswordOtpTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IPasswordResetService> _mockPasswordResetService;
        private readonly AuthController _controller;

        public AuthControllerVerifyForgotPasswordOtpTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockPasswordResetService = new Mock<IPasswordResetService>();
            _controller = new AuthController(_mockAuthService.Object, _mockPasswordResetService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task VerifyForgotPasswordOtp_WithValidRequest_ShouldReturn200OK()
        {
            // Arrange
            var request = new VerifyResetCodeRequest
            {
                Email = "user@example.com",
                OtpCode = "123456"
            };

            var expectedResponse = new VerifyResetCodeResponse
            {
                Message = "OTP verified successfully"
            };

            _mockPasswordResetService.Setup(s => s.VerifyResetCodeAsync(It.IsAny<VerifyResetCodeRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.VerifyForgotPasswordOtp(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var actualResponse = Assert.IsType<VerifyResetCodeResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Message, actualResponse.Message);
            _mockPasswordResetService.Verify(s => s.VerifyResetCodeAsync(It.IsAny<VerifyResetCodeRequest>()), Times.Once);
        }

        [Fact]
        public async Task VerifyForgotPasswordOtp_WithValidRequest_ShouldCallServiceOnce()
        {
            // Arrange
            var request = new VerifyResetCodeRequest
            {
                Email = "user@example.com",
                OtpCode = "123456"
            };

            var expectedResponse = new VerifyResetCodeResponse
            {
                Message = "OTP verified successfully"
            };

            _mockPasswordResetService.Setup(s => s.VerifyResetCodeAsync(It.IsAny<VerifyResetCodeRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.VerifyForgotPasswordOtp(request);

            // Assert
            _mockPasswordResetService.Verify(s => s.VerifyResetCodeAsync(It.Is<VerifyResetCodeRequest>(
                r => r.Email == request.Email && r.OtpCode == request.OtpCode
            )), Times.Once);
        }

        #endregion

        #region Invalid ModelState Tests

        [Fact]
        public async Task VerifyForgotPasswordOtp_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new VerifyResetCodeRequest
            {
                Email = "user@example.com",
                OtpCode = "123456"
            };

            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.VerifyForgotPasswordOtp(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task VerifyForgotPasswordOtp_WithInvalidModelState_ShouldNotCallService()
        {
            // Arrange
            var request = new VerifyResetCodeRequest
            {
                Email = "user@example.com",
                OtpCode = "123456"
            };

            _controller.ModelState.AddModelError("OtpCode", "OTP code is required");

            // Act
            await _controller.VerifyForgotPasswordOtp(request);

            // Assert
            _mockPasswordResetService.Verify(s => s.VerifyResetCodeAsync(It.IsAny<VerifyResetCodeRequest>()), Times.Never);
        }

        #endregion

        #region AuthServiceException Tests

        [Fact]
        public async Task VerifyForgotPasswordOtp_WhenServiceThrowsBadRequestException_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new VerifyResetCodeRequest
            {
                Email = "user@example.com",
                OtpCode = "999999"
            };

            _mockPasswordResetService.Setup(s => s.VerifyResetCodeAsync(It.IsAny<VerifyResetCodeRequest>()))
                .ThrowsAsync(AuthServiceException.BadRequest("Invalid or expired OTP code"));

            // Act
            var result = await _controller.VerifyForgotPasswordOtp(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Invalid or expired OTP code", errorResponse.Error);
        }

        [Fact]
        public async Task VerifyForgotPasswordOtp_WhenServiceThrowsNotFoundException_ShouldReturn404NotFound()
        {
            // Arrange
            var request = new VerifyResetCodeRequest
            {
                Email = "nonexistent@example.com",
                OtpCode = "123456"
            };

            _mockPasswordResetService.Setup(s => s.VerifyResetCodeAsync(It.IsAny<VerifyResetCodeRequest>()))
                .ThrowsAsync(AuthServiceException.NotFound("Email not found"));

            // Act
            var result = await _controller.VerifyForgotPasswordOtp(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Email not found", errorResponse.Error);
        }

        [Fact]
        public async Task VerifyForgotPasswordOtp_WhenServiceThrowsServerErrorException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var request = new VerifyResetCodeRequest
            {
                Email = "user@example.com",
                OtpCode = "123456"
            };

            _mockPasswordResetService.Setup(s => s.VerifyResetCodeAsync(It.IsAny<VerifyResetCodeRequest>()))
                .ThrowsAsync(AuthServiceException.ServerError("Database connection failed"));

            // Act
            var result = await _controller.VerifyForgotPasswordOtp(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Database connection failed", errorResponse.Error);
        }

        #endregion
    }
}