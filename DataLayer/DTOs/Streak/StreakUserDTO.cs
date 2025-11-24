public class StreakUserDTO
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public int CurrentStreak { get; set; }
    public string? AvatarUrl { get; set; }

    public bool IsPro { get; set; }
}