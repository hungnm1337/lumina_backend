using DataLayer.DTOs;
using DataLayer.DTOs.User;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IUserService
{
    Task<(IEnumerable<UserDto> users, int totalPages)> GetFilteredUsersPagedAsync(int pageNumber, int pageSize, string? searchTerm, string? roleName);

    Task<UserDto?> GetUserByIdAsync(int userId);    
    Task<bool> ToggleUserStatusAsync(int userId);    
    Task<UserDto?> GetCurrentUserProfileAsync(int currentUserId);
    Task<UserDto?> UpdateCurrentUserProfileAsync(int currentUserId, UpdateUserDTO request);

    Task<bool> ChangePasswordAsync(int currentUserId, ChangePasswordDTO request);
}



