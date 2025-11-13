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

        #region UpdateProfile Tests (9 test cases)

        [Fact]
        public async Task UpdateProfile_ValidRequestWithAllFields_ReturnsOkWithUpdatedProfile()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new UpdateUserProfileDTO
            {
                FullName = "Updated User",
                Phone = "0123456789",
                Bio = "Updated bio",
                AvatarUrl = "https://avatar.jpg"
            };

            var updatedProfile = new UserDto
            {
                UserId = 1,
                FullName = "Updated User",
                Email = "user@test.com",
                Phone = "0123456789",
                Bio = "Updated bio",
                AvatarUrl = "https://avatar.jpg",
                RoleId = 2,
                RoleName = "User"
            };

            _mockUserService.Setup(s => s.UpdateCurrentUserProfileAsync(1, request))
                .ReturnsAsync(updatedProfile);

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedProfile = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(1, returnedProfile.UserId);
            Assert.Equal("Updated User", returnedProfile.FullName);
            Assert.Equal("0123456789", returnedProfile.Phone);
            Assert.Equal("Updated bio", returnedProfile.Bio);
            Assert.Equal("https://avatar.jpg", returnedProfile.AvatarUrl);
        }

        [Fact]
        public async Task UpdateProfile_ValidRequestMinimalFields_ReturnsOkWithUpdatedProfile()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new UpdateUserProfileDTO
            {
                FullName = "New Name",
                Phone = null,
                Bio = null,
                AvatarUrl = null
            };

            var updatedProfile = new UserDto
            {
                UserId = 1,
                FullName = "New Name",
                Email = "user@test.com",
                RoleId = 2,
                RoleName = "User"
            };

            _mockUserService.Setup(s => s.UpdateCurrentUserProfileAsync(1, request))
                .ReturnsAsync(updatedProfile);

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedProfile = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal("New Name", returnedProfile.FullName);
        }

        [Fact]
        public async Task UpdateProfile_InvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var request = new UpdateUserProfileDTO
            {
                FullName = "Updated User",
                Phone = "0123456789",
                Bio = "Updated bio",
                AvatarUrl = "https://avatar.jpg"
            };

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = unauthorizedResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Invalid token", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task UpdateProfile_InvalidUserIdFormatInToken_ReturnsUnauthorized()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "invalid_id") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new UpdateUserProfileDTO
            {
                FullName = "Updated User",
                Phone = "0123456789",
                Bio = "Updated bio",
                AvatarUrl = "https://avatar.jpg"
            };

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_ProfileNotFound_ReturnsNotFound()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new UpdateUserProfileDTO
            {
                FullName = "Updated User",
                Phone = "0123456789",
                Bio = "Updated bio",
                AvatarUrl = "https://avatar.jpg"
            };

            _mockUserService.Setup(s => s.UpdateCurrentUserProfileAsync(1, request))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Profile not found", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task UpdateProfile_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new UpdateUserProfileDTO
            {
                FullName = "Updated User",
                Phone = "0123456789",
                Bio = "Updated bio",
                AvatarUrl = "https://avatar.jpg"
            };

            _mockUserService.Setup(s => s.UpdateCurrentUserProfileAsync(1, request))
                .ThrowsAsync(new ArgumentException("Phone number format is invalid"));

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Phone number format is invalid", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task UpdateProfile_FullNameExceedsMaxLength_ReturnsBadRequest()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new UpdateUserProfileDTO
            {
                FullName = new string('A', 51), // Exceeds max length of 50
                Phone = "0123456789",
                Bio = "Updated bio",
                AvatarUrl = "https://avatar.jpg"
            };

            _controller.ModelState.AddModelError("FullName", "Full name must be between 1 and 50 characters");

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateProfile_RequestBodyNull_InvalidOperationCaught()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.UpdateProfile(null!);

            // Assert
            // The controller should have already been called, even with null
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateProfile_ServiceThrowsUnexpectedException_ThrowsException()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new UpdateUserProfileDTO
            {
                FullName = "Updated User",
                Phone = "0123456789",
                Bio = "Updated bio",
                AvatarUrl = "https://avatar.jpg"
            };

            _mockUserService.Setup(s => s.UpdateCurrentUserProfileAsync(1, request))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.UpdateProfile(request));
        }

        #endregion

        #region ChangePassword Tests (9 test cases)

        [Fact]
        public async Task ChangePassword_ValidCredentials_ReturnsNoContent()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new ChangePasswordDTO
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123"
            };

            _mockUserService.Setup(s => s.ChangePasswordAsync(1, request))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockUserService.Verify(s => s.ChangePasswordAsync(1, request), Times.Once);
        }

        [Fact]
        public async Task ChangePassword_InvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var request = new ChangePasswordDTO
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123"
            };

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = unauthorizedResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Invalid token", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task ChangePassword_InvalidUserIdFormatInToken_ReturnsUnauthorized()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "not_a_number") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new ChangePasswordDTO
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123"
            };

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_IncorrectCurrentPassword_ReturnsBadRequest()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new ChangePasswordDTO
            {
                CurrentPassword = "WrongPassword",
                NewPassword = "NewPassword123"
            };

            _mockUserService.Setup(s => s.ChangePasswordAsync(1, request))
                .ThrowsAsync(new ArgumentException("Current password is incorrect"));

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Current password is incorrect", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task ChangePassword_NewPasswordSameAsCurrentPassword_ReturnsBadRequest()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new ChangePasswordDTO
            {
                CurrentPassword = "SamePassword123",
                NewPassword = "SamePassword123"
            };

            _mockUserService.Setup(s => s.ChangePasswordAsync(1, request))
                .ThrowsAsync(new ArgumentException("New password cannot be the same as current password"));

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("New password cannot be the same as current password", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task ChangePassword_NewPasswordTooWeak_ReturnsBadRequest()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new ChangePasswordDTO
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "weak"
            };

            _mockUserService.Setup(s => s.ChangePasswordAsync(1, request))
                .ThrowsAsync(new ArgumentException("Password must be at least 8 characters long"));

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.Equal("Password must be at least 8 characters long", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task ChangePassword_CurrentPasswordEmpty_ReturnsBadRequestFromModelValidation()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new ChangePasswordDTO
            {
                CurrentPassword = string.Empty,
                NewPassword = "NewPassword123"
            };

            _controller.ModelState.AddModelError("CurrentPassword", "Current password is required");

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task ChangePassword_NewPasswordEmpty_ReturnsBadRequestFromModelValidation()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new ChangePasswordDTO
            {
                CurrentPassword = "OldPassword123",
                NewPassword = string.Empty
            };

            _controller.ModelState.AddModelError("NewPassword", "New password is required");

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task ChangePassword_ServiceThrowsUnexpectedException_ThrowsException()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var request = new ChangePasswordDTO
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123"
            };

            _mockUserService.Setup(s => s.ChangePasswordAsync(1, request))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.ChangePassword(request));
        }

        #endregion
    }
}