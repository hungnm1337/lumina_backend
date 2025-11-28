using System.ComponentModel.DataAnnotations;

namespace DataLayer.DTOs.Notification
{
    public class CreateNotificationDTO
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Content is required")]
        [StringLength(2000, ErrorMessage = "Content cannot exceed 2000 characters")]
        public string Content { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        // Gửi thông báo theo role (nếu có)
        public List<int>? RoleIds { get; set; }

        // Gửi thông báo cho các user cụ thể (nếu có)
        public List<int>? UserIds { get; set; }

        // Nếu cả RoleIds và UserIds đều null/empty, gửi cho tất cả users
    }
}
