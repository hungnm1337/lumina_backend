using DataLayer.DTOs.Auth;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Auth;

namespace Lumina.Tests
{
    public class AuthControllerLoginTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IPasswordResetService> _mockPasswordResetService;
        private readonly AuthController _controller;

        public AuthControllerLoginTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockPasswordResetService = new Mock<IPasswordResetService>();
            _controller = new AuthController(_mockAuthService.Object, _mockPasswordResetService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturn200OK()
        {
            // Arrange
            var request = new LoginRequestDTO
            {
                Username = "hoangthuy123",
                Password = "@HoangThuy123"
            };

            var expectedResponse = new LoginResponse
            {
                Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI0IiwiZW1haWwiOiJ0aGFuZ3ZjaGUxNzkwMDNAZnB0LmVkdS52biIsInVuaXF1ZV9uYW1lIjoiVHJ1bmcgVsSDbiBUdXnhur9uIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZWlkZW50aWZpZXIiOiI0IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvZW1haWxhZGRyZXNzIjoidGhhbmd2Y2hlMTc5MDAzQGZwdC5lZHUudm4iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiVHJ1bmcgVsSDbiBUdXnhur9uIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiVXNlciIsIm5iZiI6MTc2MjgxODIzMiwiZXhwIjoxNzYyODIxODMyLCJpc3MiOiJMdW1pbmEiLCJhdWQiOiJMdW1pbmFGcm9udGVuZCJ9.IsN1aRYt8hiV_9dhwvseGCfabjMueatuUjibOqjDvdQ",
                ExpiresIn = 3600,
                User = new AuthUserResponse
                {
                    Id = "1",
                    Username = "hoangthuy123",
                    Email = "hoangthuy123@gmail.com",
                    Name = "Hoang Thuy"
                }
            };

            _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            
            var actualResponse = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Token, actualResponse.Token);
            Assert.Equal(expectedResponse.ExpiresIn, actualResponse.ExpiresIn);
            Assert.Equal(expectedResponse.User.Username, actualResponse.User.Username);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldCallServiceOnce()
        {
            // Arrange
            var request = new LoginRequestDTO
            {
                Username = "hoangthuy123",
                Password = "@HoangThuy123"
            };

            var expectedResponse = new LoginResponse
            {
                Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI0IiwiZW1haWwiOiJ0aGFuZ3ZjaGUxNzkwMDNAZnB0LmVkdS52biIsInVuaXF1ZV9uYW1lIjoiVHJ1bmcgVsSDbiBUdXnhur9uIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZWlkZW50aWZpZXIiOiI0IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvZW1haWxhZGRyZXNzIjoidGhhbmd2Y2hlMTc5MDAzQGZwdC5lZHUudm4iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiVHJ1bmcgVsSDbiBUdXnhur9uIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiVXNlciIsIm5iZiI6MTc2MjgxODIzMiwiZXhwIjoxNzYyODIxODMyLCJpc3MiOiJMdW1pbmEiLCJhdWQiOiJMdW1pbmFGcm9udGVuZCJ9.IsN1aRYt8hiV_9dhwvseGCfabjMueatuUjibOqjDvdQ",
                ExpiresIn = 3600,
                User = new AuthUserResponse()
            };

            _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.Login(request);

            // Assert
            _mockAuthService.Verify(s => s.LoginAsync(It.Is<LoginRequestDTO>(
                r => r.Username == request.Username && r.Password == request.Password
            )), Times.Once);
        }

        #endregion

        #region Invalid ModelState Tests

        [Fact]
        public async Task Login_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new LoginRequestDTO
            {
                Username = "",
                Password = "@HoangThuy456"
            };

            _controller.ModelState.AddModelError("Username", "Username is required");

            // Act
            var result = await _controller.Login(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task Login_WithInvalidModelState_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new LoginRequestDTO
            {
                Username = "testuser",
                Password = "password123"
            };

            _controller.ModelState.AddModelError("Username", "Username is required");

            // Act
            var result = await _controller.Login(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid login request", errorResponse.Error);
        }

        [Fact]
        public async Task Login_WithInvalidModelState_ShouldNotCallService()
        {
            // Arrange
            var request = new LoginRequestDTO
            {
                Username = "testuser",
                Password = "password123"
            };

            _controller.ModelState.AddModelError("Username", "Username is required");

            // Act
            await _controller.Login(request);

            // Assert
            _mockAuthService.Verify(s => s.LoginAsync(It.IsAny<LoginRequestDTO>()), Times.Never);
        }

        #endregion

        #region AuthServiceException Tests

        [Fact]
        public async Task Login_WhenServiceThrowsUnauthorizedException_ShouldReturn401Unauthorized()
        {
            // Arrange
            var request = new LoginRequestDTO
            {
                Username = "testuser",
                Password = "wrongpassword"
            };

            _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDTO>()))
                .ThrowsAsync(AuthServiceException.Unauthorized("Invalid username or password"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Invalid username or password", errorResponse.Error);
        }

        [Fact]
        public async Task Login_WhenServiceThrowsBadRequestException_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new LoginRequestDTO
            {
                Username = "testuser",
                Password = "password123"
            };

            _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDTO>()))
                .ThrowsAsync(AuthServiceException.BadRequest("Invalid request data"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Invalid request data", errorResponse.Error);
        }

        [Fact]
        public async Task Login_WhenServiceThrowsNotFoundException_ShouldReturn404NotFound()
        {
            // Arrange
            var request = new LoginRequestDTO
            {
                Username = "nonexistentuser",
                Password = "password123"
            };

            _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDTO>()))
                .ThrowsAsync(AuthServiceException.NotFound("User not found"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("User not found", errorResponse.Error);
        }

        [Fact]
        public async Task Login_WhenServiceThrowsServerErrorException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var request = new LoginRequestDTO
            {
                Username = "testuser",
                Password = "password123"
            };

            _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDTO>()))
                .ThrowsAsync(AuthServiceException.ServerError("Internal server error"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Internal server error", errorResponse.Error);
        }

        #endregion
    }
}
