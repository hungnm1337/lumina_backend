using System;

namespace DataLayer.DTOs.Notification
{
    public class NotificationDTO
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
