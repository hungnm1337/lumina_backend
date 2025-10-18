using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Leaderboard
{
    public int LeaderboardId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? SeasonName { get; set; }

    public int SeasonNumber { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<UserLeaderboard> UserLeaderboards { get; set; } = new List<UserLeaderboard>();
}
