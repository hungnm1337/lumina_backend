using DataLayer.DTOs;
using DataLayer.DTOs.Notification;
using DataLayer.Models;
using RepositoryLayer.Notification;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly IUserNotificationRepository _userNotificationRepo;
        private readonly IHubContext<ServiceLayer.Hubs.NotificationHub> _hubContext;

        public NotificationService(
            INotificationRepository notificationRepo,
            IUserNotificationRepository userNotificationRepo,
            IHubContext<ServiceLayer.Hubs.NotificationHub> hubContext)
        {
            _notificationRepo = notificationRepo;
            _userNotificationRepo = userNotificationRepo;
            _hubContext = hubContext;
        }

        public async Task<List<NotificationDTO>> GetAllAsync()
        {
            return await _notificationRepo.GetAllAsync();
        }

        public async Task<PaginatedResultDTO<NotificationDTO>> GetAllPaginatedAsync(int page = 1, int pageSize = 10)
        {
            return await _notificationRepo.GetAllPaginatedAsync(page, pageSize);
        }

        public async Task<NotificationDTO?> GetByIdAsync(int notificationId)
        {
            return await _notificationRepo.GetByIdAsync(notificationId);
        }

        public async Task<int> CreateAsync(CreateNotificationDTO dto, int createdBy)
        {
            var notification = new DataLayer.Models.Notification
            {
                Title = dto.Title,
                Content = dto.Content,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedAt = DateTime.UtcNow
            };

            var notificationId = await _notificationRepo.CreateAsync(notification);

            // Gửi thông báo đến tất cả users
            var userIds = await _notificationRepo.GetAllUserIdsAsync();
            foreach (var userId in userIds)
            {
                var userNotification = new UserNotification
                {
                    UserId = userId,
                    NotificationId = notificationId,
                    IsRead = false,
                    CreateAt = DateTime.UtcNow
                };
                await _userNotificationRepo.CreateAsync(userNotification);
            }

            // ✅ Broadcast realtime notification đến tất cả users qua SignalR
            try
            {
                await _hubContext.Clients.Group("AllUsers").SendAsync("ReceiveNotification", new
                {
                    notificationId = notificationId,
                    title = notification.Title,
                    content = notification.Content,
                    createdAt = notification.CreatedAt
                });
                Console.WriteLine($"✅ Broadcasted notification {notificationId} to all users");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to broadcast notification: {ex.Message}");
            }

            return notificationId;
        }

        public async Task<bool> UpdateAsync(int notificationId, UpdateNotificationDTO dto)
        {
            var existing = await _notificationRepo.GetByIdAsync(notificationId);
            if (existing == null) return false;

            var notification = new DataLayer.Models.Notification
            {
                NotificationId = notificationId,
                Title = dto.Title ?? existing.Title,
                Content = dto.Content ?? existing.Content,
                IsActive = dto.IsActive ?? existing.IsActive,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            return await _notificationRepo.UpdateAsync(notification);
        }

        public async Task<bool> DeleteAsync(int notificationId)
        {
            // Xóa tất cả UserNotifications liên quan
            await _userNotificationRepo.DeleteByNotificationIdAsync(notificationId);
            
            // Xóa Notification
            return await _notificationRepo.DeleteAsync(notificationId);
        }
    }
}
