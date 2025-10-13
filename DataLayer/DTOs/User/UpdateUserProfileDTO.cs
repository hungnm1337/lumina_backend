using System.ComponentModel.DataAnnotations;

namespace DataLayer.DTOs.User;


public sealed class UpdateUserProfileDTO
{
    
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Full name must be between 1 and 50 characters")]
    public string FullName { get; set; } = string.Empty;

    
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? Phone { get; set; }

    
    [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
    public string? Bio { get; set; }

    
    [StringLength(255, ErrorMessage = "Avatar URL cannot exceed 255 characters")]
    public string? AvatarUrl { get; set; }
}
