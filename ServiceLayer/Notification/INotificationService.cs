using DataLayer.DTOs;
using DataLayer.DTOs.Notification;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Notification
{
    public interface INotificationService
    {
        Task<List<NotificationDTO>> GetAllAsync();
        Task<PaginatedResultDTO<NotificationDTO>> GetAllPaginatedAsync(int page = 1, int pageSize = 10);
        Task<NotificationDTO?> GetByIdAsync(int notificationId);
        Task<int> CreateAsync(CreateNotificationDTO dto, int createdBy);
        Task<bool> UpdateAsync(int notificationId, UpdateNotificationDTO dto);
        Task<bool> DeleteAsync(int notificationId);
    }
}
