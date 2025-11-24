using DataLayer.DTOs.Notification;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.Notification
{
    public class UserNotificationRepository : IUserNotificationRepository
    {
        private readonly LuminaSystemContext _context;

        public UserNotificationRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<List<UserNotificationDTO>> GetByUserIdAsync(int userId)
        {
            var userNotifications = await _context.UserNotifications
                .Include(un => un.Notification)
                .Where(un => un.UserId == userId)
                .OrderByDescending(un => un.CreateAt)
                .ToListAsync();

            return userNotifications.Select(un => new UserNotificationDTO
            {
                UniqueId = un.UniqueId,
                UserId = un.UserId,
                NotificationId = un.NotificationId ?? 0,
                Title = un.Notification?.Title ?? "",
                Content = un.Notification?.Content ?? "",
                IsRead = un.IsRead ?? false,
                CreatedAt = un.CreateAt
            }).ToList();
        }

        public async Task<UserNotificationDTO?> GetByIdAsync(int uniqueId)
        {
            var userNotification = await _context.UserNotifications
                .Include(un => un.Notification)
                .FirstOrDefaultAsync(un => un.UniqueId == uniqueId);

            if (userNotification == null) return null;

            return new UserNotificationDTO
            {
                UniqueId = userNotification.UniqueId,
                UserId = userNotification.UserId,
                NotificationId = userNotification.NotificationId ?? 0,
                Title = userNotification.Notification?.Title ?? "",
                Content = userNotification.Notification?.Content ?? "",
                IsRead = userNotification.IsRead ?? false,
                CreatedAt = userNotification.CreateAt
            };
        }

        public async Task<int> CreateAsync(UserNotification entity)
        {
            _context.UserNotifications.Add(entity);
            await _context.SaveChangesAsync();
            return entity.UniqueId;
        }

        public async Task<bool> MarkAsReadAsync(int uniqueId)
        {
            var userNotification = await _context.UserNotifications.FindAsync(uniqueId);
            if (userNotification == null) return false;

            userNotification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.UserNotifications
                .Where(un => un.UserId == userId && un.IsRead == false)
                .CountAsync();
        }

        public async Task<bool> DeleteByNotificationIdAsync(int notificationId)
        {
            var userNotifications = await _context.UserNotifications
                .Where(un => un.NotificationId == notificationId)
                .ToListAsync();

            if (!userNotifications.Any()) return false;

            _context.UserNotifications.RemoveRange(userNotifications);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
