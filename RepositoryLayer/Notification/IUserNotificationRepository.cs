using DataLayer.DTOs.Notification;
using DataLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositoryLayer.Notification
{
    public interface IUserNotificationRepository
    {
        Task<List<UserNotificationDTO>> GetByUserIdAsync(int userId);
        Task<UserNotificationDTO?> GetByIdAsync(int uniqueId);
        Task<int> CreateAsync(UserNotification entity);
        Task<bool> MarkAsReadAsync(int uniqueId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<bool> DeleteByNotificationIdAsync(int notificationId);
    }
}
