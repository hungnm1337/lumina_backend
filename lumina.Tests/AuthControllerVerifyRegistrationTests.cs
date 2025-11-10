using DataLayer.DTOs.Auth;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Auth;

namespace Lumina.Tests
{
    public class AuthControllerVerifyRegistrationTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IPasswordResetService> _mockPasswordResetService;
        private readonly AuthController _controller;

        public AuthControllerVerifyRegistrationTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockPasswordResetService = new Mock<IPasswordResetService>();
            _controller = new AuthController(_mockAuthService.Object, _mockPasswordResetService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task VerifyRegistration_WithValidRequest_ShouldReturn201Created()
        {
            // Arrange
            var request = new VerifyRegistrationRequest
            {
                Email = "newuser@example.com",
                OtpCode = "123456",
                Name = "New User",
                Username = "newuser123",
                Password = "Password@123"
            };

            var expectedResponse = new VerifyRegistrationResponse
            {
                Message = "Registration successful",
                Token = "jwt-token",
                ExpiresIn = 3600,
                User = new AuthUserResponse
                {
                    Id = "1",
                    Username = "newuser123",
                    Email = "newuser@example.com",
                    Name = "New User"
                }
            };

            _mockAuthService.Setup(s => s.VerifyRegistrationAsync(It.IsAny<VerifyRegistrationRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.VerifyRegistration(request);

            // Assert
            var createdResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
            
            var actualResponse = Assert.IsType<VerifyRegistrationResponse>(createdResult.Value);
            Assert.Equal(expectedResponse.Message, actualResponse.Message);
            Assert.Equal(expectedResponse.Token, actualResponse.Token);
            Assert.Equal(expectedResponse.User.Username, actualResponse.User.Username);
        }

        [Fact]
        public async Task VerifyRegistration_WithValidRequest_ShouldCallServiceOnce()
        {
            // Arrange
            var request = new VerifyRegistrationRequest
            {
                Email = "newuser@example.com",
                OtpCode = "123456",
                Name = "New User",
                Username = "newuser123",
                Password = "Password@123"
            };

            var expectedResponse = new VerifyRegistrationResponse
            {
                Message = "Registration successful",
                Token = "jwt-token",
                ExpiresIn = 3600,
                User = new AuthUserResponse()
            };

            _mockAuthService.Setup(s => s.VerifyRegistrationAsync(It.IsAny<VerifyRegistrationRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.VerifyRegistration(request);

            // Assert
            _mockAuthService.Verify(s => s.VerifyRegistrationAsync(It.Is<VerifyRegistrationRequest>(
                r => r.Email == request.Email && 
                     r.OtpCode == request.OtpCode && 
                     r.Username == request.Username
            )), Times.Once);
        }

        #endregion

        #region Invalid ModelState Tests

        [Fact]
        public async Task VerifyRegistration_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new VerifyRegistrationRequest
            {
                Email = "newuser@example.com",
                OtpCode = "123456",
                Name = "New User",
                Username = "newuser123",
                Password = "Password@123"
            };

            _controller.ModelState.AddModelError("OtpCode", "Invalid OTP code");

            // Act
            var result = await _controller.VerifyRegistration(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task VerifyRegistration_WithInvalidModelState_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new VerifyRegistrationRequest
            {
                Email = "newuser@example.com",
                OtpCode = "123456",
                Name = "New User",
                Username = "newuser123",
                Password = "Password@123"
            };

            _controller.ModelState.AddModelError("Password", "Password is required");

            // Act
            var result = await _controller.VerifyRegistration(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid verification request", errorResponse.Error);
        }

        [Fact]
        public async Task VerifyRegistration_WithInvalidModelState_ShouldNotCallService()
        {
            // Arrange
            var request = new VerifyRegistrationRequest
            {
                Email = "newuser@example.com",
                OtpCode = "123456",
                Name = "New User",
                Username = "newuser123",
                Password = "Password@123"
            };

            _controller.ModelState.AddModelError("Email", "Invalid email format");

            // Act
            await _controller.VerifyRegistration(request);

            // Assert
            _mockAuthService.Verify(s => s.VerifyRegistrationAsync(It.IsAny<VerifyRegistrationRequest>()), Times.Never);
        }

        #endregion

        #region AuthServiceException Tests

        [Fact]
        public async Task VerifyRegistration_WhenServiceThrowsBadRequestException_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new VerifyRegistrationRequest
            {
                Email = "newuser@example.com",
                OtpCode = "999999",
                Name = "New User",
                Username = "newuser123",
                Password = "Password@123"
            };

            _mockAuthService.Setup(s => s.VerifyRegistrationAsync(It.IsAny<VerifyRegistrationRequest>()))
                .ThrowsAsync(AuthServiceException.BadRequest("Invalid or expired OTP code"));

            // Act
            var result = await _controller.VerifyRegistration(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Invalid or expired OTP code", errorResponse.Error);
        }

        [Fact]
        public async Task VerifyRegistration_WhenServiceThrowsNotFoundException_ShouldReturn404NotFound()
        {
            // Arrange
            var request = new VerifyRegistrationRequest
            {
                Email = "nonexistent@example.com",
                OtpCode = "123456",
                Name = "New User",
                Username = "newuser123",
                Password = "Password@123"
            };

            _mockAuthService.Setup(s => s.VerifyRegistrationAsync(It.IsAny<VerifyRegistrationRequest>()))
                .ThrowsAsync(AuthServiceException.NotFound("No pending registration found for this email"));

            // Act
            var result = await _controller.VerifyRegistration(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("No pending registration found for this email", errorResponse.Error);
        }

        [Fact]
        public async Task VerifyRegistration_WhenServiceThrowsConflictException_ShouldReturn409Conflict()
        {
            // Arrange
            var request = new VerifyRegistrationRequest
            {
                Email = "newuser@example.com",
                OtpCode = "123456",
                Name = "New User",
                Username = "existinguser",
                Password = "Password@123"
            };

            _mockAuthService.Setup(s => s.VerifyRegistrationAsync(It.IsAny<VerifyRegistrationRequest>()))
                .ThrowsAsync(AuthServiceException.Conflict("Username already exists"));

            // Act
            var result = await _controller.VerifyRegistration(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status409Conflict, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Username already exists", errorResponse.Error);
        }

        [Fact]
        public async Task VerifyRegistration_WhenServiceThrowsServerErrorException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var request = new VerifyRegistrationRequest
            {
                Email = "newuser@example.com",
                OtpCode = "123456",
                Name = "New User",
                Username = "newuser123",
                Password = "Password@123"
            };

            _mockAuthService.Setup(s => s.VerifyRegistrationAsync(It.IsAny<VerifyRegistrationRequest>()))
                .ThrowsAsync(AuthServiceException.ServerError("Database connection failed"));

            // Act
            var result = await _controller.VerifyRegistration(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Database connection failed", errorResponse.Error);
        }

        #endregion
    }
}
