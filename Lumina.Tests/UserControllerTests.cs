using DataLayer.DTOs.User;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.User;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _controller = new UserController(_mockUserService.Object);
        }

        #region GetFilteredUsersPaged Tests (6 test cases)

        [Fact]
        public async Task GetFilteredUsersPaged_ValidParametersWithoutFilters_ReturnsOkWithData()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { UserId = 1, FullName = "User 1", Email = "user1@test.com", RoleId = 2, RoleName = "User" },
                new UserDto { UserId = 2, FullName = "User 2", Email = "user2@test.com", RoleId = 2, RoleName = "User" }
            };
            _mockUserService.Setup(s => s.GetFilteredUsersPagedAsync(1, 6, null, null))
                .ReturnsAsync((users, 1));

            // Act
            var result = await _controller.GetFilteredUsersPaged(1, null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var dataProperty = response.GetType().GetProperty("data");
            var totalPagesProperty = response.GetType().GetProperty("totalPages");
            
            Assert.NotNull(dataProperty);
            Assert.NotNull(totalPagesProperty);
            Assert.Equal(1, totalPagesProperty.GetValue(response));
        }

        [Fact]
        public async Task GetFilteredUsersPaged_WithSearchTerm_ReturnsFilteredData()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { UserId = 1, FullName = "John Doe", Email = "john@test.com", RoleId = 2, RoleName = "User" }
            };
            _mockUserService.Setup(s => s.GetFilteredUsersPagedAsync(1, 6, "John", null))
                .ReturnsAsync((users, 1));

            // Act
            var result = await _controller.GetFilteredUsersPaged(1, "John", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetFilteredUsersPaged_WithRoleName_ReturnsFilteredData()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { UserId = 2, FullName = "User 2", Email = "user2@test.com", RoleId = 2, RoleName = "User" }
            };
            _mockUserService.Setup(s => s.GetFilteredUsersPagedAsync(1, 6, null, "User"))
                .ReturnsAsync((users, 1));

            // Act
            var result = await _controller.GetFilteredUsersPaged(1, null, "User");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetFilteredUsersPaged_WithBothFilters_ReturnsFilteredData()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { UserId = 1, FullName = "John User", Email = "john@test.com", RoleId = 2, RoleName = "User" }
            };
            _mockUserService.Setup(s => s.GetFilteredUsersPagedAsync(1, 6, "John", "User"))
                .ReturnsAsync((users, 1));

            // Act
            var result = await _controller.GetFilteredUsersPaged(1, "John", "User");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetFilteredUsersPaged_PageNumberZero_ReturnsOkWithData()
        {
            // Arrange
            var users = new List<UserDto>();
            _mockUserService.Setup(s => s.GetFilteredUsersPagedAsync(0, 6, null, null))
                .ReturnsAsync((users, 0));

            // Act
            var result = await _controller.GetFilteredUsersPaged(0, null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetFilteredUsersPaged_ServiceThrowsException_ThrowsException()
        {
            // Arrange
            _mockUserService.Setup(s => s.GetFilteredUsersPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.GetFilteredUsersPaged(1, null, null));
        }

        #endregion

        #region GetUserById Tests (5 test cases)

        [Fact]
        public async Task GetUserById_ValidId_ReturnsOkWithUser()
        {
            // Arrange
            var user = new UserDto { UserId = 1, FullName = "Test User", Email = "test@test.com", RoleId = 2, RoleName = "User" };
            _mockUserService.Setup(s => s.GetUserByIdAsync(1))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.GetUserById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(1, returnedUser.UserId);
        }

        [Fact]
        public async Task GetUserById_InvalidId_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(s => s.GetUserByIdAsync(999))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetUserById(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("User not found", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task GetUserById_IdZero_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(s => s.GetUserByIdAsync(0))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetUserById(0);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetUserById_NegativeId_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(s => s.GetUserByIdAsync(-1))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetUserById(-1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetUserById_ServiceThrowsException_ThrowsException()
        {
            // Arrange
            _mockUserService.Setup(s => s.GetUserByIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.GetUserById(1));
        }

        #endregion

        #region ToggleUserStatus Tests (5 test cases)

        [Fact]
        public async Task ToggleUserStatus_ValidId_ReturnsNoContent()
        {
            // Arrange
            _mockUserService.Setup(s => s.ToggleUserStatusAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ToggleUserStatus(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task ToggleUserStatus_InvalidId_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(s => s.ToggleUserStatusAsync(999))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ToggleUserStatus(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("User not found", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task ToggleUserStatus_IdZero_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(s => s.ToggleUserStatusAsync(0))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ToggleUserStatus(0);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ToggleUserStatus_NegativeId_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(s => s.ToggleUserStatusAsync(-1))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ToggleUserStatus(-1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ToggleUserStatus_ServiceThrowsException_ThrowsException()
        {
            // Arrange
            _mockUserService.Setup(s => s.ToggleUserStatusAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.ToggleUserStatus(1));
        }

        #endregion

        #region GetProfile Tests (5 test cases)

        [Fact]
        public async Task GetProfile_ValidToken_ReturnsOkWithProfile()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var profile = new UserDto { UserId = 1, FullName = "Test User", Email = "test@test.com", RoleId = 2, RoleName = "User" };
            _mockUserService.Setup(s => s.GetCurrentUserProfileAsync(1))
                .ReturnsAsync(profile);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedProfile = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(1, returnedProfile.UserId);
        }

        [Fact]
        public async Task GetProfile_InvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = unauthorizedResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Invalid token", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task GetProfile_InvalidUserIdFormat_ReturnsUnauthorized()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "invalid") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.GetProfile();

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_ProfileNotFound_ReturnsNotFound()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserService.Setup(s => s.GetCurrentUserProfileAsync(1))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Profile not found", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task GetProfile_ServiceThrowsException_ThrowsException()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _mockUserService.Setup(s => s.GetCurrentUserProfileAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.GetProfile());
        }

        #endregion

        #region UpdateUserRole Tests (7 test cases)

        [Fact]
        public async Task UpdateUserRole_ValidParameters_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            _mockUserService.Setup(s => s.UpdateUserRoleAsync(1, 2))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateUserRole(1, 2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Cập nhật quyền thành công!", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task UpdateUserRole_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(s => s.UpdateUserRoleAsync(999, 2))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateUserRole(999, 2);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Không tìm thấy user hoặc cập nhật thất bại!", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task UpdateUserRole_UserIdZero_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(s => s.UpdateUserRoleAsync(0, 2))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateUserRole(0, 2);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUserRole_NegativeUserId_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(s => s.UpdateUserRoleAsync(-1, 2))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateUserRole(-1, 2);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUserRole_RoleIdZero_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(s => s.UpdateUserRoleAsync(1, 0))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateUserRole(1, 0);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUserRole_NegativeRoleId_ReturnsNotFound()
        {
            // Arrange
            _mockUserService.Setup(s => s.UpdateUserRoleAsync(1, -1))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateUserRole(1, -1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUserRole_ServiceThrowsException_ThrowsException()
        {
            // Arrange
            _mockUserService.Setup(s => s.UpdateUserRoleAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.UpdateUserRole(1, 2));
        }

        #endregion
    }
}