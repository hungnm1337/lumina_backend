using System;

namespace DataLayer.DTOs.Leaderboard
{
    public class LeaderboardDTO
    {
        public int LeaderboardId { get; set; }
        public string? SeasonName { get; set; }
        public int SeasonNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }

    public class CreateLeaderboardDTO
    {
        public string? SeasonName { get; set; }
        public int SeasonNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateLeaderboardDTO
    {
        public string? SeasonName { get; set; }
        public int SeasonNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class LeaderboardRankDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int Rank { get; set; }
    }
}
