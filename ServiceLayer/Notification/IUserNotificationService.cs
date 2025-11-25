using DataLayer.DTOs.Notification;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Notification
{
    public interface IUserNotificationService
    {
        Task<List<UserNotificationDTO>> GetByUserIdAsync(int userId);
        Task<UserNotificationDTO?> GetByIdAsync(int uniqueId);
        Task<bool> MarkAsReadAsync(int uniqueId, int userId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}
