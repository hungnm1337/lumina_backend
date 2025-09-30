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
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Phone = user.Phone,
            IsActive = user.IsActive
            /*JoinDate = user.Accounts.Any() ? user.Accounts.First().CreateAt.ToString("yyyy-MM-dd") : null*/
        };
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        return await _userRepository.ToggleUserStatusAsync(userId);
    }
}


