using DataLayer.DTOs;
using DataLayer.DTOs.Notification;
using DataLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositoryLayer.Notification
{
    public interface INotificationRepository
    {
        Task<List<NotificationDTO>> GetAllAsync();
        Task<PaginatedResultDTO<NotificationDTO>> GetAllPaginatedAsync(int page = 1, int pageSize = 10);
        Task<NotificationDTO?> GetByIdAsync(int notificationId);
        Task<int> CreateAsync(DataLayer.Models.Notification entity);
        Task<bool> UpdateAsync(DataLayer.Models.Notification entity);
        Task<bool> DeleteAsync(int notificationId);
        Task<List<int>> GetAllUserIdsAsync();
        Task<List<int>> GetUserIdsByRoleIdsAsync(List<int> roleIds);
        Task<List<int>> GetUserIdsByUserIdsAsync(List<int> userIds);
    }
}
