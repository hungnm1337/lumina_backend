using DataLayer.DTOs.Auth;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Auth;

namespace Lumina.Tests
{
    public class AuthControllerResetPasswordTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IPasswordResetService> _mockPasswordResetService;
        private readonly AuthController _controller;

        public AuthControllerResetPasswordTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockPasswordResetService = new Mock<IPasswordResetService>();
            _controller = new AuthController(_mockAuthService.Object, _mockPasswordResetService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task ResetPassword_WithValidRequest_ShouldReturn200OK()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Email = "user@example.com",
                OtpCode = "123456",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            var expectedResponse = new ResetPasswordResponse
            {
                Message = "Password has been reset successfully"
            };

            _mockPasswordResetService.Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ResetPassword(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            
            var actualResponse = Assert.IsType<ResetPasswordResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Message, actualResponse.Message);
        }

        [Fact]
        public async Task ResetPassword_WithValidRequest_ShouldCallServiceOnce()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Email = "user@example.com",
                OtpCode = "123456",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            var expectedResponse = new ResetPasswordResponse
            {
                Message = "Password has been reset successfully"
            };

            _mockPasswordResetService.Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.ResetPassword(request);

            // Assert
            _mockPasswordResetService.Verify(s => s.ResetPasswordAsync(It.Is<ResetPasswordRequest>(
                r => r.Email == request.Email && 
                     r.OtpCode == request.OtpCode && 
                     r.NewPassword == request.NewPassword
            )), Times.Once);
        }

        #endregion

        #region Invalid ModelState Tests

        [Fact]
        public async Task ResetPassword_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Email = "user@example.com",
                OtpCode = "123456",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            _controller.ModelState.AddModelError("OtpCode", "Invalid OTP code");

            // Act
            var result = await _controller.ResetPassword(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithInvalidModelState_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Email = "user@example.com",
                OtpCode = "123456",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            _controller.ModelState.AddModelError("NewPassword", "Password is required");

            // Act
            var result = await _controller.ResetPassword(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid password reset request", errorResponse.Error);
        }

        [Fact]
        public async Task ResetPassword_WithInvalidModelState_ShouldNotCallService()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Email = "user@example.com",
                OtpCode = "123456",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            _controller.ModelState.AddModelError("Email", "Invalid email format");

            // Act
            await _controller.ResetPassword(request);

            // Assert
            _mockPasswordResetService.Verify(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()), Times.Never);
        }

        #endregion

        #region AuthServiceException Tests

        [Fact]
        public async Task ResetPassword_WhenServiceThrowsBadRequestException_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Email = "user@example.com",
                OtpCode = "999999",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            _mockPasswordResetService.Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()))
                .ThrowsAsync(AuthServiceException.BadRequest("Invalid or expired OTP code"));

            // Act
            var result = await _controller.ResetPassword(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Invalid or expired OTP code", errorResponse.Error);
        }

        [Fact]
        public async Task ResetPassword_WhenServiceThrowsNotFoundException_ShouldReturn404NotFound()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Email = "nonexistent@example.com",
                OtpCode = "123456",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            _mockPasswordResetService.Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()))
                .ThrowsAsync(AuthServiceException.NotFound("No pending password reset found for this email"));

            // Act
            var result = await _controller.ResetPassword(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("No pending password reset found for this email", errorResponse.Error);
        }

        [Fact]
        public async Task ResetPassword_WhenServiceThrowsServerErrorException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Email = "user@example.com",
                OtpCode = "123456",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            _mockPasswordResetService.Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()))
                .ThrowsAsync(AuthServiceException.ServerError("Database connection failed"));

            // Act
            var result = await _controller.ResetPassword(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Database connection failed", errorResponse.Error);
        }

        #endregion
    }
}
