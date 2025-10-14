using System;
using System.Collections.Generic;

namespace lumina.Models;

public partial class UserLeaderboard
{
    public int UserLeaderboardId { get; set; }

    public int UserId { get; set; }

    public int LeaderboardId { get; set; }

    public int Score { get; set; }

    public virtual Leaderboard Leaderboard { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
