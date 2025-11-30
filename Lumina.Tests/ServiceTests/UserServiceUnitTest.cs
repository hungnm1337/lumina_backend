using Xunit;
using Moq;
using DataLayer.DTOs.User;
using DataLayer.Models;
using RepositoryLayer.User;
using ServiceLayer.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class UserServiceUnitTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly UserService _service;

        public UserServiceUnitTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _service = new UserService(_mockUserRepository.Object);
        }

        #region GetFilteredUsersPagedAsync Tests

        /// <summary>
        /// Test GetFilteredUsersPagedAsync với các tham số khác nhau
        /// Coverage: Line 28 (repository call and return)
        /// </summary>
        [Theory]
        [InlineData(1, 10, null, null)]           // No filters
        [InlineData(1, 20, "john", null)]         // Search term only
        [InlineData(2, 15, null, "Admin")]        // Role filter only
        [InlineData(1, 10, "test", "Student")]    // Both filters
        public async Task GetFilteredUsersPagedAsync_WithVariousParameters_ShouldReturnCorrectResult(
            int pageNumber, int pageSize, string? searchTerm, string? roleName)
        {
            // Arrange
            var expectedUsers = new List<UserDto>
            {
                new UserDto { UserId = 1, Email = "test@example.com", FullName = "Test User" }
            };
            var expectedTotalPages = 5;

            _mockUserRepository
                .Setup(r => r.GetFilteredUsersPagedAsync(pageNumber, pageSize, searchTerm, roleName))
                .ReturnsAsync((expectedUsers, expectedTotalPages));

            // Act
            var (users, totalPages) = await _service.GetFilteredUsersPagedAsync(pageNumber, pageSize, searchTerm, roleName);

            // Assert
            Assert.NotNull(users);
            Assert.Equal(expectedUsers.Count, users.Count());
            Assert.Equal(expectedTotalPages, totalPages);
            _mockUserRepository.Verify(r => r.GetFilteredUsersPagedAsync(pageNumber, pageSize, searchTerm, roleName), Times.Once);
        }

        #endregion

        #region GetUserByIdAsync Tests

        /// <summary>
        /// Test GetUserByIdAsync khi user không tồn tại
        /// Coverage: Line 34-37 (null check và return null)
        /// </summary>
        [Fact]
        public async Task GetUserByIdAsync_WhenUserNotFound_ShouldReturnNull()
        {
            // Arrange
            int userId = 999;
            _mockUserRepository
                .Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _service.GetUserByIdAsync(userId);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(r => r.GetUserByIdAsync(userId), Times.Once);
        }

        /// <summary>
        /// Test GetUserByIdAsync khi user tồn tại với Account
        /// Coverage: Line 34, 40-53 (mapping UserDto with Account)
        /// </summary>
        [Fact]
        public async Task GetUserByIdAsync_WhenUserFoundWithAccount_ShouldReturnUserDto()
        {
            // Arrange
            int userId = 1;
            var user = new User
            {
                UserId = userId,
                Email = "test@example.com",
                FullName = "John Doe",
                RoleId = 2,
                AvatarUrl = "https://example.com/avatar.jpg",
                Bio = "Test bio",
                Phone = "1234567890",
                IsActive = true,
                Role = new Role { RoleId = 2, RoleName = "Student" },
                Accounts = new List<Account>
                {
                    new Account
                    {
                        AccountId = 1,
                        Username = "johndoe",
                        CreateAt = new DateTime(2024, 1, 1)
                    }
                }
            };

            _mockUserRepository
                .Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _service.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal("John Doe", result.FullName);
            Assert.Equal(2, result.RoleId);
            Assert.Equal("Student", result.RoleName);
            Assert.Equal("johndoe", result.UserName);
            Assert.Equal("https://example.com/avatar.jpg", result.AvatarUrl);
            Assert.Equal("Test bio", result.Bio);
            Assert.Equal("1234567890", result.Phone);
            Assert.True(result.IsActive);
            Assert.Equal("2024-01-01", result.JoinDate);
            _mockUserRepository.Verify(r => r.GetUserByIdAsync(userId), Times.Once);
        }

        /// <summary>
        /// Test GetUserByIdAsync khi user không có Account
        /// Coverage: Line 47, 52 (null check for Accounts)
        /// </summary>
        [Fact]
        public async Task GetUserByIdAsync_WhenUserFoundWithoutAccount_ShouldReturnUserDtoWithNullUsername()
        {
            // Arrange
            int userId = 2;
            var user = new User
            {
                UserId = userId,
                Email = "noaccounts@example.com",
                FullName = "No Accounts User",
                RoleId = 1,
                IsActive = false,
                Role = new Role { RoleId = 1, RoleName = "Admin" },
                Accounts = new List<Account>() // Empty accounts
            };

            _mockUserRepository
                .Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _service.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.UserName);
            Assert.Null(result.JoinDate);
            Assert.False(result.IsActive);
            _mockUserRepository.Verify(r => r.GetUserByIdAsync(userId), Times.Once);
        }

        /// <summary>
        /// Test GetUserByIdAsync khi user không có Role
        /// Coverage: Line 46 (null check for Role)
        /// </summary>
        [Fact]
        public async Task GetUserByIdAsync_WhenUserFoundWithoutRole_ShouldReturnUserDtoWithNullRoleName()
        {
            // Arrange
            int userId = 3;
            var user = new User
            {
                UserId = userId,
                Email = "norole@example.com",
                FullName = "No Role User",
                RoleId = 1,
                IsActive = true,
                Role = null, // No role
                Accounts = new List<Account>
                {
                    new Account
                    {
                        AccountId = 2,
                        Username = "noroleuser",
                        CreateAt = new DateTime(2024, 2, 1)
                    }
                }
            };

            _mockUserRepository
                .Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _service.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.RoleName);
            Assert.Equal("noroleuser", result.UserName);
            _mockUserRepository.Verify(r => r.GetUserByIdAsync(userId), Times.Once);
        }

        #endregion

        #region ToggleUserStatusAsync Tests

        /// <summary>
        /// Test ToggleUserStatusAsync với kết quả success và failure
        /// Coverage: Line 59 (repository call and return)
        /// </summary>
        [Theory]
        [InlineData(1, true)]   // Success case
        [InlineData(2, false)]  // Failure case
        public async Task ToggleUserStatusAsync_WithValidUserId_ShouldReturnRepositoryResult(int userId, bool expectedResult)
        {
            // Arrange
            _mockUserRepository
                .Setup(r => r.ToggleUserStatusAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.ToggleUserStatusAsync(userId);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockUserRepository.Verify(r => r.ToggleUserStatusAsync(userId), Times.Once);
        }

        #endregion

        #region UpdateUserRoleAsync Tests

        /// <summary>
        /// Test UpdateUserRoleAsync với các roleId và userId khác nhau
        /// Coverage: Line 131 (repository call and return)
        /// </summary>
        [Theory]
        [InlineData(1, 1, true)]   // User 1, Role 1, Success
        [InlineData(2, 2, true)]   // User 2, Role 2, Success
        [InlineData(3, 1, false)]  // User 3, Role 1, Failure
        public async Task UpdateUserRoleAsync_WithValidParameters_ShouldReturnRepositoryResult(
            int userId, int roleId, bool expectedResult)
        {
            // Arrange
            _mockUserRepository
                .Setup(r => r.UpdateUserRoleAsync(userId, roleId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.UpdateUserRoleAsync(userId, roleId);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockUserRepository.Verify(r => r.UpdateUserRoleAsync(userId, roleId), Times.Once);
        }

        #endregion
    }
}
