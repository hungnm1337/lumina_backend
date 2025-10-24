using DataLayer.DTOs.User;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.User
{
    public interface IUserRepository
    {
        Task<(IEnumerable<UserDto> users, int totalPages)> GetFilteredUsersPagedAsync(int pageNumber, int pageSize, string? searchTerm, string? roleName);
        Task<UserDto?> GetCurrentUserProfileAsync(int userId);

        Task<DataLayer.Models.User> GetUserByIdAsync(int userId);
        Task<bool> ToggleUserStatusAsync(int userId);

        Task<UserDto?> UpdateUserProfileAsync(int userId, string fullName, string? phone, string? bio, string? avatarUrl);

        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}


