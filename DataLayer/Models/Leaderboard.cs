using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Leaderboard
{
    public int LeaderboardId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public virtual ICollection<UserLeaderboard> UserLeaderboards { get; set; } = new List<UserLeaderboard>();
}
