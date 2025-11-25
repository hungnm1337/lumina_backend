using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class UserLeaderboard
{
    public int UserLeaderboardId { get; set; }

    public int UserId { get; set; }

    public int LeaderboardId { get; set; }

    public int Score { get; set; } // Điểm tích lũy - tăng mỗi lần làm bài

    public int? EstimatedTOEICScore { get; set; } // Điểm TOEIC ước tính (0-990) - cập nhật liên tục khi làm bài mới

    public DateTime? FirstAttemptDate { get; set; } // Ngày làm bài lần đầu tiên trong season

    public virtual Leaderboard Leaderboard { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
