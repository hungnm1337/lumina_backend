using DataLayer.DTOs.Notification;
using RepositoryLayer.Notification;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Notification
{
    public class UserNotificationService : IUserNotificationService
    {
        private readonly IUserNotificationRepository _userNotificationRepo;

        public UserNotificationService(IUserNotificationRepository userNotificationRepo)
        {
            _userNotificationRepo = userNotificationRepo;
        }

        public async Task<List<UserNotificationDTO>> GetByUserIdAsync(int userId)
        {
            return await _userNotificationRepo.GetByUserIdAsync(userId);
        }

        public async Task<UserNotificationDTO?> GetByIdAsync(int uniqueId)
        {
            return await _userNotificationRepo.GetByIdAsync(uniqueId);
        }

        public async Task<bool> MarkAsReadAsync(int uniqueId, int userId)
        {
            var userNotification = await _userNotificationRepo.GetByIdAsync(uniqueId);
            if (userNotification == null || userNotification.UserId != userId)
            {
                return false;
            }

            return await _userNotificationRepo.MarkAsReadAsync(uniqueId);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _userNotificationRepo.GetUnreadCountAsync(userId);
        }
    }
}
