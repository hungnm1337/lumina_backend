namespace DataLayer.DTOs.User
{
    public class UpdateUserDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
    }
}