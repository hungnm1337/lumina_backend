using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class UserLeaderboard
{
    public int UserLeaderboardId { get; set; }

    public int UserId { get; set; }

    public int LeaderboardId { get; set; }

    public int Score { get; set; } 

    public int? EstimatedTOEICScore { get; set; } 

    public DateTime? FirstAttemptDate { get; set; } 

    public virtual Leaderboard Leaderboard { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
