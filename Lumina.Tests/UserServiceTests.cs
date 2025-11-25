using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLayer.DTOs.User;
using DataLayer.Models;
using Moq;
using RepositoryLayer.User;
using ServiceLayer.User;
using Xunit;

namespace Lumina.Tests;

public sealed class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        _service = new UserService(_userRepositoryMock.Object);
    }

    #region GetFilteredUsersPagedAsync

    [Fact]
    public async Task GetFilteredUsersPagedAsync_ValidInput_ReturnsResultFromRepository()
    {
        // Arrange
        var expectedUsers = new List<UserDto>
        {
            new UserDto { UserId = 1, Email = "u1@test.com", FullName = "User 1", RoleId = 1, RoleName = "User" },
            new UserDto { UserId = 2, Email = "u2@test.com", FullName = "User 2", RoleId = 2, RoleName = "Admin" }
        };
        const int expectedTotalPages = 3;

        _userRepositoryMock
            .Setup(r => r.GetFilteredUsersPagedAsync(2, 20, "abc", "Admin"))
            .ReturnsAsync((expectedUsers, expectedTotalPages));

        // Act
        var (users, totalPages) = await _service.GetFilteredUsersPagedAsync(2, 20, "abc", "Admin");

        // Assert
        Assert.Equal(expectedTotalPages, totalPages);
        Assert.Same(expectedUsers, users);
        _userRepositoryMock.Verify(r => r.GetFilteredUsersPagedAsync(2, 20, "abc", "Admin"), Times.Once);
    }

    [Fact]
    public async Task GetFilteredUsersPagedAsync_NullFilters_StillPassesToRepository()
    {
        // Arrange
        var expectedUsers = Array.Empty<UserDto>();

        _userRepositoryMock
            .Setup(r => r.GetFilteredUsersPagedAsync(1, 10, null, null))
            .ReturnsAsync((expectedUsers, 0));

        // Act
        var (users, totalPages) = await _service.GetFilteredUsersPagedAsync(1, 10, null, null);

        // Assert
        Assert.Empty(users);
        Assert.Equal(0, totalPages);
        _userRepositoryMock.Verify(r => r.GetFilteredUsersPagedAsync(1, 10, null, null), Times.Once);
    }

    #endregion

    #region GetUserByIdAsync

    [Fact]
    public async Task GetUserByIdAsync_UserExistsWithRoleAndAccount_MapsToUserDto()
    {
        // Arrange
        var createdDate = new DateTime(2025, 1, 2);
        var user = new DataLayer.Models.User
        {
            UserId = 10,
            Email = "user@test.com",
            FullName = "Test User",
            RoleId = 5,
            Role = new Role { RoleId = 5, RoleName = "Admin" },
            AvatarUrl = "avatar.png",
            Bio = "Bio",
            Phone = "0123",
            IsActive = true,
            Accounts = new List<Account>
            {
                new Account
                {
                    Username = "username",
                    CreateAt = createdDate
                }
            }
        };

        _userRepositoryMock
            .Setup(r => r.GetUserByIdAsync(user.UserId))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserByIdAsync(user.UserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result!.UserId);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.FullName, result.FullName);
        Assert.Equal(user.RoleId, result.RoleId);
        Assert.Equal("Admin", result.RoleName);
        Assert.Equal("username", result.UserName);
        Assert.Equal(user.AvatarUrl, result.AvatarUrl);
        Assert.Equal(user.Bio, result.Bio);
        Assert.Equal(user.Phone, result.Phone);
        Assert.Equal(user.IsActive, result.IsActive);
        Assert.Equal(createdDate.ToString("yyyy-MM-dd"), result.JoinDate);

        _userRepositoryMock.Verify(r => r.GetUserByIdAsync(user.UserId), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserExistsWithoutRoleOrAccounts_MapsNullablesCorrectly()
    {
        // Arrange
        var user = new DataLayer.Models.User
        {
            UserId = 20,
            Email = "noacc@test.com",
            FullName = "No Account",
            RoleId = 0,
            Role = null!,
            AvatarUrl = null,
            Bio = null,
            Phone = null,
            IsActive = null,
            Accounts = new List<Account>() // empty
        };

        _userRepositoryMock
            .Setup(r => r.GetUserByIdAsync(user.UserId))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserByIdAsync(user.UserId);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result!.RoleName);
        Assert.Null(result.UserName);
        Assert.Null(result.JoinDate);

        _userRepositoryMock.Verify(r => r.GetUserByIdAsync(user.UserId), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetUserByIdAsync(999))
            .ReturnsAsync((DataLayer.Models.User?)null);

        // Act
        var result = await _service.GetUserByIdAsync(999);

        // Assert
        Assert.Null(result);
        _userRepositoryMock.Verify(r => r.GetUserByIdAsync(999), Times.Once);
    }

    #endregion

    #region ToggleUserStatusAsync

    [Fact]
    public async Task ToggleUserStatusAsync_UserFound_TogglesAndReturnsTrue()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.ToggleUserStatusAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ToggleUserStatusAsync(1);

        // Assert
        Assert.True(result);
        _userRepositoryMock.Verify(r => r.ToggleUserStatusAsync(1), Times.Once);
    }

    [Fact]
    public async Task ToggleUserStatusAsync_UserNotFound_ReturnsFalse()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.ToggleUserStatusAsync(2))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ToggleUserStatusAsync(2);

        // Assert
        Assert.False(result);
        _userRepositoryMock.Verify(r => r.ToggleUserStatusAsync(2), Times.Once);
    }

    #endregion

    #region GetCurrentUserProfileAsync

    [Fact]
    public async Task GetCurrentUserProfileAsync_ProfileExists_ReturnsDto()
    {
        // Arrange
        var dto = new UserDto { UserId = 5, Email = "p@test.com", FullName = "Profile", RoleId = 1, RoleName = "User" };

        _userRepositoryMock
            .Setup(r => r.GetCurrentUserProfileAsync(5))
            .ReturnsAsync(dto);

        // Act
        var result = await _service.GetCurrentUserProfileAsync(5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.UserId, result!.UserId);
        _userRepositoryMock.Verify(r => r.GetCurrentUserProfileAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserProfileAsync_ProfileNotFound_ReturnsNull()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetCurrentUserProfileAsync(7))
            .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _service.GetCurrentUserProfileAsync(7);

        // Assert
        Assert.Null(result);
        _userRepositoryMock.Verify(r => r.GetCurrentUserProfileAsync(7), Times.Once);
    }

    #endregion

    #region UpdateCurrentUserProfileAsync

    [Fact]
    public async Task UpdateCurrentUserProfileAsync_ValidRequest_UpdatesAndReturnsDto()
    {
        // Arrange
        var request = new UpdateUserProfileDTO
        {
            FullName = "  New Name  ",
            Phone = "0123",
            Bio = "Bio",
            AvatarUrl = "avatar.png"
        };

        var expectedDto = new UserDto { UserId = 1, FullName = "New Name" };

        _userRepositoryMock
            .Setup(r => r.UpdateUserProfileAsync(1, "New Name", request.Phone, request.Bio, request.AvatarUrl))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateCurrentUserProfileAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result!.FullName);

        _userRepositoryMock.Verify(r => r.UpdateUserProfileAsync(
            1,
            It.Is<string>(n => n == "New Name"),
            request.Phone,
            request.Bio,
            request.AvatarUrl),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateCurrentUserProfileAsync_EmptyOrWhitespaceName_ThrowsArgumentException(string? fullName)
    {
        // Arrange
        var request = new UpdateUserProfileDTO
        {
            FullName = fullName ?? string.Empty,
            Phone = null,
            Bio = null,
            AvatarUrl = null
        };

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdateCurrentUserProfileAsync(1, request));

        // Assert
        Assert.Equal("Full name is required", ex.Message);
        _userRepositoryMock.Verify(r => r.UpdateUserProfileAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateCurrentUserProfileAsync_NameExceedsMaxLength_IsTruncatedBeforeRepositoryCall()
    {
        // Arrange
        var longName = new string('A', 60);
        string? capturedName = null;

        var request = new UpdateUserProfileDTO
        {
            FullName = longName,
            Phone = "1",
            Bio = "2",
            AvatarUrl = "3"
        };

        _userRepositoryMock
            .Setup(r => r.UpdateUserProfileAsync(
                1,
                It.IsAny<string>(),
                request.Phone,
                request.Bio,
                request.AvatarUrl))
            .Callback<int, string, string?, string?, string?>((_, name, _, _, _) => capturedName = name)
            .ReturnsAsync(new UserDto { UserId = 1, FullName = longName[..50] });

        // Act
        var result = await _service.UpdateCurrentUserProfileAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, capturedName!.Length);
        Assert.Equal(longName[..50], capturedName);

        _userRepositoryMock.Verify(r => r.UpdateUserProfileAsync(1, It.IsAny<string>(), request.Phone, request.Bio, request.AvatarUrl), Times.Once);
    }

    #endregion

    #region ChangePasswordAsync

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_CallsRepositoryAndReturnsTrue()
    {
        // Arrange
        var request = new ChangePasswordDTO
        {
            CurrentPassword = "  old-pass  ",
            NewPassword = "  new-pass-123  "
        };

        _userRepositoryMock
            .Setup(r => r.ChangePasswordAsync(1, "old-pass", "new-pass-123"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ChangePasswordAsync(1, request);

        // Assert
        Assert.True(result);

        _userRepositoryMock.Verify(r => r.ChangePasswordAsync(
                1,
                It.Is<string>(p => p == "old-pass"),
                It.Is<string>(p => p == "new-pass-123")),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    public async Task ChangePasswordAsync_NewPasswordTooShort_ThrowsArgumentException(string? newPassword)
    {
        // Arrange
        var request = new ChangePasswordDTO
        {
            CurrentPassword = "current-pass",
            NewPassword = newPassword ?? string.Empty
        };

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ChangePasswordAsync(1, request));

        // Assert
        Assert.Equal("New password must be at least 8 characters", ex.Message);
        _userRepositoryMock.Verify(r => r.ChangePasswordAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_NewPasswordSameAsCurrent_ThrowsArgumentException()
    {
        // Arrange
        var request = new ChangePasswordDTO
        {
            CurrentPassword = "  same-pass  ",
            NewPassword = "same-pass"
        };

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ChangePasswordAsync(1, request));

        // Assert
        Assert.Equal("New password must be different from current password", ex.Message);
        _userRepositoryMock.Verify(r => r.ChangePasswordAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_RepositoryReturnsFalse_ThrowsArgumentException()
    {
        // Arrange
        var request = new ChangePasswordDTO
        {
            CurrentPassword = "current-pass",
            NewPassword = "new-pass-123"
        };

        _userRepositoryMock
            .Setup(r => r.ChangePasswordAsync(1, "current-pass", "new-pass-123"))
            .ReturnsAsync(false);

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ChangePasswordAsync(1, request));

        // Assert
        Assert.Equal("Current password is incorrect or account type does not support password change", ex.Message);
        _userRepositoryMock.Verify(r => r.ChangePasswordAsync(1, "current-pass", "new-pass-123"), Times.Once);
    }

    #endregion

    #region UpdateUserRoleAsync

    [Fact]
    public async Task UpdateUserRoleAsync_ValidInput_ReturnsTrue()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.UpdateUserRoleAsync(1, 2))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateUserRoleAsync(1, 2);

        // Assert
        Assert.True(result);
        _userRepositoryMock.Verify(r => r.UpdateUserRoleAsync(1, 2), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_RepositoryReturnsFalse_ReturnsFalse()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.UpdateUserRoleAsync(3, 99))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateUserRoleAsync(3, 99);

        // Assert
        Assert.False(result);
        _userRepositoryMock.Verify(r => r.UpdateUserRoleAsync(3, 99), Times.Once);
    }

    #endregion
}


