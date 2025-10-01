using DataLayer.DTOs.User;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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

    public async Task<User> GetUserByIdAsync(int userId)
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
}



