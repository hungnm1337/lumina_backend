using DataLayer.DTOs;
using DataLayer.DTOs.Notification;
using DataLayer.Models;
using RepositoryLayer.Notification;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly IUserNotificationRepository _userNotificationRepo;
        private readonly IHubContext<ServiceLayer.Hubs.NotificationHub> _hubContext;
        private readonly LuminaSystemContext _context;

        public NotificationService(
            INotificationRepository notificationRepo,
            IUserNotificationRepository userNotificationRepo,
            IHubContext<ServiceLayer.Hubs.NotificationHub> hubContext,
            LuminaSystemContext context)
        {
            _notificationRepo = notificationRepo;
            _userNotificationRepo = userNotificationRepo;
            _hubContext = hubContext;
            _context = context;
        }

        
        private async Task<int> GetSystemUserIdAsync()
        {
            var adminUser = await _context.Users
                .Where(u => u.IsActive == true && u.RoleId == 1)
                .OrderBy(u => u.UserId)
                .FirstOrDefaultAsync();

            if (adminUser != null)
            {
                return adminUser.UserId;
            }

            var firstUser = await _context.Users
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.UserId)
                .FirstOrDefaultAsync();

            if (firstUser != null)
            {
                return firstUser.UserId;
            }

            Console.WriteLine($" [NotificationService] No active users found, using fallback UserID = 1");
            return 1;
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

            // Xác định danh sách userIds cần gửi thông báo
            List<int> userIds = new List<int>();

            // Ưu tiên: Nếu có UserIds cụ thể, dùng UserIds
            if (dto.UserIds != null && dto.UserIds.Count > 0)
            {
                userIds = await _notificationRepo.GetUserIdsByUserIdsAsync(dto.UserIds);
            }
            // Nếu có RoleIds, lấy users theo role
            else if (dto.RoleIds != null && dto.RoleIds.Count > 0)
            {
                userIds = await _notificationRepo.GetUserIdsByRoleIdsAsync(dto.RoleIds);
            }
            // Nếu không có cả hai, gửi cho tất cả users (backward compatibility)
            else
            {
                userIds = await _notificationRepo.GetAllUserIdsAsync();
            }

            // Tạo UserNotification cho từng user
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

            try
            {
                var notificationData = new
                {
                    notificationId = notificationId,
                    title = notification.Title,
                    content = notification.Content,
                    createdAt = notification.CreatedAt
                };

                // Nếu gửi cho tất cả users, dùng group
                if ((dto.UserIds == null || dto.UserIds.Count == 0) && 
                    (dto.RoleIds == null || dto.RoleIds.Count == 0))
                {
                    await _hubContext.Clients.Group("AllUsers").SendAsync("ReceiveNotification", notificationData);
                    Console.WriteLine($" Broadcasted notification {notificationId} to all users");
                }
                else
                {
                    // Gửi cho từng user cụ thể
                    foreach (var userId in userIds)
                    {
                        var connectionId = ServiceLayer.Hubs.NotificationHub.GetConnectionId(userId);
                        if (!string.IsNullOrEmpty(connectionId))
                        {
                            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", notificationData);
                        }
                    }
                    Console.WriteLine($" Broadcasted notification {notificationId} to {userIds.Count} specific users");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Failed to broadcast notification: {ex.Message}");
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

        // Thông báo tự động: Điểm tích lũy
        public async Task<int> SendPointsNotificationAsync(int userId, int pointsEarned, int totalAccumulatedScore,
            int correctAnswers, int totalQuestions, int timeBonus, int accuracyBonus, bool isFirstAttempt = true)
        {
            int notificationId = 0;
            try
            {
                Console.WriteLine($" [NotificationService] Starting SendPointsNotificationAsync for user {userId}");
                Console.WriteLine($"   - PointsEarned: {pointsEarned}");
                Console.WriteLine($"   - TotalAccumulatedScore: {totalAccumulatedScore}");
                Console.WriteLine($"   - CorrectAnswers: {correctAnswers}/{totalQuestions}");
                
                // Tính accuracy rate
                double accuracyRate = totalQuestions > 0 ? (double)correctAnswers / totalQuestions : 0;
                int accuracyPercent = (int)(accuracyRate * 100);
                
                string encouragementMessage = GetEncouragementMessage(accuracyRate, timeBonus, accuracyBonus, pointsEarned);
                
                string title = "🎯 Điểm tích lũy mới!";
                string content;
                
                if (correctAnswers == 0)
                {
                    // Trường hợp đặc biệt: Không có câu nào đúng
                    content = $"Bạn đã hoàn thành bài làm với {correctAnswers}/{totalQuestions} câu đúng ({accuracyPercent}%). " +
                             $"Lần này bạn chưa nhận được điểm tích lũy. " +
                             $"Tổng điểm tích lũy hiện tại: {totalAccumulatedScore} điểm. " +
                             $"Đừng nản lòng! Mỗi lần làm bài là một cơ hội học hỏi. Hãy xem lại những câu sai và cố gắng lần sau nhé! 💪";
                }
                else if (!isFirstAttempt)
                {
                    // Trường hợp: Làm lại (không phải lần đầu) - Không cộng điểm
                    content = $"Bạn đã hoàn thành bài làm với {correctAnswers}/{totalQuestions} câu đúng ({accuracyPercent}%). " +
                             $"Đây không phải lần đầu làm phần thi này, nên không cộng điểm tích lũy. " +
                             $"Tổng điểm tích lũy hiện tại: {totalAccumulatedScore} điểm. " +
                             $"Hãy thử làm các phần thi mới để nhận thêm điểm tích lũy nhé! 🎯";
                }
                else
                {
                    // Trường hợp bình thường: Có câu đúng và làm lần đầu
                    content = $"Bạn đã hoàn thành bài làm với {correctAnswers}/{totalQuestions} câu đúng ({accuracyPercent}%). " +
                             $"Bạn nhận được {pointsEarned} điểm tích lũy. " +
                             (timeBonus > 0 ? $"Bonus tốc độ: +{timeBonus} điểm. " : "") +
                             (accuracyBonus > 0 ? $"Bonus độ chính xác: +{accuracyBonus} điểm. " : "") +
                             $"Tổng điểm tích lũy: {totalAccumulatedScore} điểm. " +
                             $"{encouragementMessage}";
                }

                // Lấy system user ID để dùng cho CreatedBy
                var systemUserId = await GetSystemUserIdAsync();
                Console.WriteLine($"📢 [NotificationService] Using system UserID: {systemUserId} for CreatedBy");

                var notification = new DataLayer.Models.Notification
                {
                    Title = title,
                    Content = content,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = systemUserId, // System user
                    UpdatedAt = DateTime.UtcNow
                };

                notificationId = await _notificationRepo.CreateAsync(notification);
                Console.WriteLine($" [NotificationService] Points Notification {notificationId} created in database. Title: {title}");

                // Gửi cho user cụ thể
                var userNotification = new UserNotification
                {
                    UserId = userId,
                    NotificationId = notificationId,
                    IsRead = false,
                    CreateAt = DateTime.UtcNow
                };
                var userNotificationId = await _userNotificationRepo.CreateAsync(userNotification);
                Console.WriteLine($" [NotificationService] Points UserNotification {userNotificationId} created for user {userId}. NotificationId: {notificationId}");

                // Broadcast realtime
                try
                {
                    var connectionId = ServiceLayer.Hubs.NotificationHub.GetConnectionId(userId);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", new
                        {
                            notificationId = notificationId,
                            title = title,
                            content = content,
                            createdAt = notification.CreatedAt
                        });
                        Console.WriteLine($" [NotificationService] Broadcasted points notification {notificationId} to user {userId} via SignalR (ConnectionId: {connectionId})");
                    }
                    else
                    {
                        Console.WriteLine($" [NotificationService] User {userId} is not connected to SignalR. Notification {notificationId} saved to database and will be shown on next page load.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" [NotificationService] Failed to broadcast points notification: {ex.Message}");
                    Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                }
                
                Console.WriteLine($" [NotificationService] Points notification {notificationId} completed for user {userId}");
                return notificationId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" [NotificationService] CRITICAL ERROR in SendPointsNotificationAsync for user {userId}:");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   InnerException: {ex.InnerException.Message}");
                }
                // Re-throw để LeaderboardService biết có lỗi
                throw;
            }
        }

        // Thông báo tự động: Kết quả TOEIC
        public async Task<int> SendTOEICNotificationAsync(int userId, int estimatedTOEIC, string toeicLevel, string message)
        {
            int notificationId = 0;
            try
            {
                Console.WriteLine($" [NotificationService] Starting SendTOEICNotificationAsync for user {userId}");
                Console.WriteLine($"   - EstimatedTOEIC: {estimatedTOEIC}");
                Console.WriteLine($"   - TOEICLevel: {toeicLevel}");
                
                string title = $"📊 Kết quả TOEIC: {toeicLevel}";
                string content = message;

                // Lấy system user ID để dùng cho CreatedBy
                var systemUserId = await GetSystemUserIdAsync();
                Console.WriteLine($" [NotificationService] Using system UserID: {systemUserId} for CreatedBy (TOEIC)");

                var notification = new DataLayer.Models.Notification
                {
                    Title = title,
                    Content = content,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = systemUserId, // System user
                    UpdatedAt = DateTime.UtcNow
                };

                notificationId = await _notificationRepo.CreateAsync(notification);
                Console.WriteLine($" [NotificationService] TOEIC Notification {notificationId} created in database. Title: {title}");

                // Gửi cho user cụ thể
                var userNotification = new UserNotification
                {
                    UserId = userId,
                    NotificationId = notificationId,
                    IsRead = false,
                    CreateAt = DateTime.UtcNow
                };
                var userNotificationId = await _userNotificationRepo.CreateAsync(userNotification);
                Console.WriteLine($" [NotificationService] TOEIC UserNotification {userNotificationId} created for user {userId}. NotificationId: {notificationId}");

                // Broadcast realtime
                try
                {
                    var connectionId = ServiceLayer.Hubs.NotificationHub.GetConnectionId(userId);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", new
                        {
                            notificationId = notificationId,
                            title = title,
                            content = content,
                            createdAt = notification.CreatedAt
                        });
                        Console.WriteLine($" [NotificationService] Broadcasted TOEIC notification {notificationId} to user {userId} via SignalR (ConnectionId: {connectionId})");
                    }
                    else
                    {
                        Console.WriteLine($" [NotificationService] User {userId} is not connected to SignalR. Notification {notificationId} saved to database and will be shown on next page load.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" [NotificationService] Failed to broadcast TOEIC notification: {ex.Message}");
                    Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                }
                
                Console.WriteLine($" [NotificationService] TOEIC notification {notificationId} completed for user {userId}");
                return notificationId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" [NotificationService] CRITICAL ERROR in SendTOEICNotificationAsync for user {userId}:");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   InnerException: {ex.InnerException.Message}");
                }
                // Re-throw để LeaderboardService biết có lỗi
                throw;
            }

            return notificationId;
        }

        private string GetEncouragementMessage(double accuracyRate, int timeBonus, int accuracyBonus, int totalPoints)
        {
            int accuracyPercent = (int)(accuracyRate * 100);
            
            // Khen ngợi khi đạt độ chính xác cao
            if (accuracyRate >= 0.95)
            {
                return " Xuất sắc! Bạn đã làm rất tốt! Hãy tiếp tục phát huy!";
            }
            else if (accuracyRate >= 0.90)
            {
                return " Tuyệt vời! Kết quả rất ấn tượng! Cố gắng duy trì nhé!";
            }
            else if (accuracyRate >= 0.80)
            {
                if (timeBonus > 0 && accuracyBonus > 0)
                {
                    return " Tốt lắm! Bạn vừa nhanh vừa chính xác! Tiếp tục như vậy nhé!";
                }
                else if (timeBonus > 0)
                {
                    return " Tốt! Bạn làm bài rất nhanh! Hãy cố gắng tăng độ chính xác lên nhé!";
                }
                else if (accuracyBonus > 0)
                {
                    return " Tốt! Độ chính xác của bạn rất cao! Hãy cố gắng làm nhanh hơn một chút!";
                }
                return " Tốt! Bạn đã làm khá tốt! Hãy tiếp tục luyện tập để cải thiện hơn nữa!";
            }
            else if (accuracyRate >= 0.70)
            {
                return " Không tệ! Bạn đang tiến bộ. Hãy ôn lại những câu sai và cố gắng lần sau nhé!";
            }
            else if (accuracyRate >= 0.60)
            {
                return " Cần cố gắng thêm! Hãy xem lại bài học và luyện tập nhiều hơn. Bạn sẽ làm tốt hơn!";
            }
            else if (accuracyRate >= 0.50)
            {
                return " Đừng nản lòng! Mỗi lần làm bài là một cơ hội học hỏi. Hãy xem lại và cố gắng lần sau!";
            }
            else
            {
                return " Mọi hành trình đều bắt đầu từ bước đầu tiên! Hãy kiên trì luyện tập, bạn sẽ tiến bộ!";
            }
        }
    }
}
