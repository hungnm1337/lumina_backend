using DataLayer.DTOs;
using DataLayer.DTOs.User;
using DataLayer.Models;

namespace ServiceLayer.User;


public interface IUserService
{
    // Admin functions
    Task<(IEnumerable<UserDto> users, int totalPages)> GetFilteredUsersPagedAsync(int pageNumber, int pageSize, string? searchTerm, string? roleName);
    Task<UserDto?> GetUserByIdAsync(int userId);    
    Task<bool> ToggleUserStatusAsync(int userId);    

    // User profile functions
    Task<UserDto?> GetCurrentUserProfileAsync(int currentUserId);
    Task<UserDto?> UpdateCurrentUserProfileAsync(int currentUserId, UpdateUserProfileDTO request);

    // Password management
    Task<bool> ChangePasswordAsync(int currentUserId, ChangePasswordDTO request);


    Task<bool> UpdateUserRoleAsync(int userId, int roleId);
}



