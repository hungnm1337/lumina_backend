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

        public List<int>? RoleIds { get; set; }

        public List<int>? UserIds { get; set; }

    }
}
