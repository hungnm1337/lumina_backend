using DataLayer.DTOs.User;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.User
{
    public class UserRepository : IUserRepository
    {
    private readonly LuminaSystemContext _context;

    public UserRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<UserDto> users, int totalPages)> GetFilteredUsersPagedAsync(int pageNumber, int pageSize, string? searchTerm, string? roleName)
    {
        var query = _context.Users
                            .Include(u => u.Role)
                             .Where(u => u.RoleId != 1)
                            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(lowerSearchTerm)
                                  || u.Email.ToLower().Contains(lowerSearchTerm));
        }

        if (!string.IsNullOrEmpty(roleName) && roleName != "Tất cả vai trò")
        {
            query = query.Where(u => u.Role.RoleName.ToLower() == roleName.ToLower());

        }

        var totalUsers = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Email = u.Email,
                FullName = u.FullName,
                RoleId = u.Role.RoleId,
                RoleName = u.Role.RoleName,
                AvatarUrl = u.AvatarUrl,
                Phone = u.Phone,
                IsActive = u.IsActive,
                JoinDate = u.Accounts.Any() ? u.Accounts.First().CreateAt.ToString("yyyy-MM-dd") : null,
            })
            .ToListAsync();

        return (users, totalPages);
    }

    public async Task<DataLayer.Models.User> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        // Đảo ngược trạng thái IsActive
        user.IsActive = !user.IsActive;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> GetCurrentUserProfileAsync(int userId)
{
    var u = await _context.Users
        .Include(x => x.Role)
        .FirstOrDefaultAsync(x => x.UserId == userId);

    if (u == null) return null;

    return new UserDto
    {
        UserId = u.UserId,
        Email = u.Email,
        FullName = u.FullName,
        RoleId = u.RoleId,
        RoleName = u.Role?.RoleName,
        AvatarUrl = u.AvatarUrl,
        Bio = u.Bio,
        Phone = u.Phone,
        IsActive = u.IsActive,
        JoinDate = u.Accounts.Any() ? u.Accounts.First().CreateAt.ToString("yyyy-MM-dd") : null
    };
}

public async Task<UserDto?> UpdateUserProfileAsync(int userId, string fullName, string? phone, string? bio, string? avatarUrl)
{
    var u = await _context.Users.Include(x => x.Role).Include(x => x.Accounts).FirstOrDefaultAsync(x => x.UserId == userId);
    if (u == null) return null;

    // Update fields - fullName đã được validated trong service layer
    u.FullName = fullName;
    u.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    u.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
    u.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();

    _context.Users.Update(u);
    await _context.SaveChangesAsync();

    return new UserDto
    {
        UserId = u.UserId,
        Email = u.Email,
        FullName = u.FullName,
        RoleId = u.RoleId,
        RoleName = u.Role?.RoleName,
        AvatarUrl = u.AvatarUrl,
        Bio = u.Bio,
        Phone = u.Phone,
        IsActive = u.IsActive,
        JoinDate = u.Accounts.Any() ? u.Accounts.First().CreateAt.ToString("yyyy-MM-dd") : null
    };
}

public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
{
    var account = await _context.Accounts
        .FirstOrDefaultAsync(a => a.UserId == userId && string.IsNullOrEmpty(a.AuthProvider) && a.PasswordHash != null);

    if (account == null)
    {
        return false; // Không phải tài khoản local hoặc không có mật khẩu để đổi
    }

    var ok = BCrypt.Net.BCrypt.Verify(currentPassword, account.PasswordHash);
    if (!ok)
    {
        return false;
    }

    account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
    account.UpdateAt = DateTime.UtcNow;

    _context.Accounts.Update(account);
    await _context.SaveChangesAsync();
    return true;
}
    }
}



