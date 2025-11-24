using System;

namespace DataLayer.DTOs.Notification
{
    public class UserNotificationDTO
    {
        public int UniqueId { get; set; }
        public int UserId { get; set; }
        public int NotificationId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
