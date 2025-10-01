using DataLayer.DTOs.Role;
using DataLayer.DTOs.User;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<(IEnumerable<UserDto> users, int totalPages)> GetFilteredUsersPagedAsync(int pageNumber, int pageSize, string? searchTerm, string? roleName)
    {
        return await _userRepository.GetFilteredUsersPagedAsync(pageNumber, pageSize, searchTerm, roleName);
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            RoleId = user.RoleId,
            RoleName = user.Role?.RoleName,
            UserName = user.Accounts != null && user.Accounts.Any() ? user.Accounts.First().Username : null,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Phone = user.Phone,
            IsActive = user.IsActive,
            JoinDate = user.Accounts.Any() ? user.Accounts.First().CreateAt.ToString("yyyy-MM-dd") : null
        };
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        return await _userRepository.ToggleUserStatusAsync(userId);
    }

public async Task<UserDto?> GetCurrentUserProfileAsync(int currentUserId)
{
    return await _userRepository.GetCurrentUserProfileAsync(currentUserId);
}

public async Task<UserDto?> UpdateCurrentUserProfileAsync(int currentUserId, UpdateUserDTO request)
{
    var fullName = (request.FullName ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(fullName))
    {
        throw new ArgumentException("FullName is required.");
    }
    if (fullName.Length > 50)
    {
        fullName = fullName[..50];
    }

    return await _userRepository.UpdateUserProfileAsync(
        currentUserId,
        fullName,
        request.Phone,
        request.Bio,
        request.AvatarUrl
    );
}

public async Task<bool> ChangePasswordAsync(int currentUserId, ChangePasswordDTO request)
{
    var newPw = (request.NewPassword ?? string.Empty).Trim();
    var curPw = (request.CurrentPassword ?? string.Empty).Trim();

    if (string.IsNullOrEmpty(newPw) || newPw.Length < 8)
        throw new ArgumentException("New password must be at least 8 characters.");

    if (newPw == curPw)
        throw new ArgumentException("New password must be different from current password.");


    var success = await _userRepository.ChangePasswordAsync(currentUserId, curPw, newPw);
    if (!success)
        throw new ArgumentException("Current password is incorrect or account type does not support password change.");

    return true;
}
}


