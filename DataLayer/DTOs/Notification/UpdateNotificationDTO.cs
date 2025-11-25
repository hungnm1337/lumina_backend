using System.ComponentModel.DataAnnotations;

namespace DataLayer.DTOs.Notification
{
    public class UpdateNotificationDTO
    {
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        [StringLength(2000, ErrorMessage = "Content cannot exceed 2000 characters")]
        public string? Content { get; set; }

        public bool? IsActive { get; set; }
    }
}
