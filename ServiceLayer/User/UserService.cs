using DataLayer.DTOs.Role;
using DataLayer.DTOs.User;
using DataLayer.Models;
using RepositoryLayer.User;

namespace ServiceLayer.User;


public sealed class UserService : IUserService
{
    private const int NameMaxLength = 50;
    private const int MinPasswordLength = 8;

    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

   
    public async Task<(IEnumerable<UserDto> users, int totalPages)> GetFilteredUsersPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        string? roleName)
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

    
    public async Task<UserDto?> UpdateCurrentUserProfileAsync(int currentUserId, UpdateUserProfileDTO request)
    {
        // Validate FullName (đã được validate bởi Data Annotations, nhưng thêm business validation)
        var fullName = (request.FullName ?? string.Empty).Trim();

        // Guard clause: FullName không được rỗng
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required");
        }

        // Truncate nếu quá dài
        if (fullName.Length > NameMaxLength)
        {
            fullName = fullName[..NameMaxLength];
        }

        // Gọi repository để update
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
        var newPassword = (request.NewPassword ?? string.Empty).Trim();
        var currentPassword = (request.CurrentPassword ?? string.Empty).Trim();

        // Guard clause: Password mới phải đủ độ dài
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < MinPasswordLength)
        {
            throw new ArgumentException($"New password must be at least {MinPasswordLength} characters");
        }

        // Guard clause: Password mới phải khác password hiện tại
        if (newPassword == currentPassword)
        {
            throw new ArgumentException("New password must be different from current password");
        }

        // Gọi repository để thay đổi password
        var success = await _userRepository.ChangePasswordAsync(currentUserId, currentPassword, newPassword);

        if (!success)
        {
            throw new ArgumentException("Current password is incorrect or account type does not support password change");
        }

        return true;
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, int roleId)
    {
        // Có thể kiểm tra roleId hợp lệ nếu muốn (ví dụ có tồn tại trong bảng Role)
        return await _userRepository.UpdateUserRoleAsync(userId, roleId);
    }

}
