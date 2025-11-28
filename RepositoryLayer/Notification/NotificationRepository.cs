using DataLayer.DTOs;
using DataLayer.DTOs.Notification;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.Notification
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly LuminaSystemContext _context;

        public NotificationRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<List<NotificationDTO>> GetAllAsync()
        {
            var notifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return notifications.Select(n => new NotificationDTO
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Content = n.Content,
                IsActive = n.IsActive,
                CreatedAt = n.CreatedAt,
                CreatedBy = n.CreatedBy,
                UpdatedAt = n.UpdatedAt
            }).ToList();
        }

        public async Task<PaginatedResultDTO<NotificationDTO>> GetAllPaginatedAsync(int page = 1, int pageSize = 10)
        {
            var query = _context.Notifications.AsQueryable();

            var total = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = notifications.Select(n => new NotificationDTO
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Content = n.Content,
                IsActive = n.IsActive,
                CreatedAt = n.CreatedAt,
                CreatedBy = n.CreatedBy,
                UpdatedAt = n.UpdatedAt
            }).ToList();

            return new PaginatedResultDTO<NotificationDTO>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                HasNext = page < (int)Math.Ceiling(total / (double)pageSize),
                HasPrevious = page > 1
            };
        }

        public async Task<NotificationDTO?> GetByIdAsync(int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId);

            if (notification == null) return null;

            return new NotificationDTO
            {
                NotificationId = notification.NotificationId,
                Title = notification.Title,
                Content = notification.Content,
                IsActive = notification.IsActive,
                CreatedAt = notification.CreatedAt,
                CreatedBy = notification.CreatedBy,
                UpdatedAt = notification.UpdatedAt
            };
        }

        public async Task<int> CreateAsync(DataLayer.Models.Notification entity)
        {
            _context.Notifications.Add(entity);
            await _context.SaveChangesAsync();
            return entity.NotificationId;
        }

        public async Task<bool> UpdateAsync(DataLayer.Models.Notification entity)
        {
            var existing = await _context.Notifications.FindAsync(entity.NotificationId);
            if (existing == null) return false;

            existing.Title = entity.Title;
            existing.Content = entity.Content;
            existing.IsActive = entity.IsActive;
            existing.UpdatedAt = entity.UpdatedAt;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null) return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<int>> GetAllUserIdsAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive == true)
                .Select(u => u.UserId)
                .ToListAsync();
        }

        public async Task<List<int>> GetUserIdsByRoleIdsAsync(List<int> roleIds)
        {
            if (roleIds == null || roleIds.Count == 0)
                return new List<int>();

            return await _context.Users
                .Where(u => u.IsActive == true && roleIds.Contains(u.RoleId))
                .Select(u => u.UserId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<int>> GetUserIdsByUserIdsAsync(List<int> userIds)
        {
            if (userIds == null || userIds.Count == 0)
                return new List<int>();

            return await _context.Users
                .Where(u => u.IsActive == true && userIds.Contains(u.UserId))
                .Select(u => u.UserId)
                .Distinct()
                .ToListAsync();
        }
    }
}
