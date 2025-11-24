using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Streak
{
    public class StreakRepository : IStreakRepository
    {
        private readonly LuminaSystemContext _dbContext;

        public StreakRepository(LuminaSystemContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<List<StreakUserDTO>> GetTopStreakUsersAsync(int topN)
        {
            var now = DateTime.UtcNow;

            return await _dbContext.Users
                .Where(u => u.IsActive == true && u.CurrentStreak > 0)
                .OrderByDescending(u => u.CurrentStreak)
                .ThenBy(u => u.LastPracticeDate) // Người giữ streak lâu hơn lên trên
                .ThenBy(u => u.UserId)           // Nếu vẫn bằng nhau thì UserId nhỏ hơn lên trên
                .Take(topN)
                .Select(u => new StreakUserDTO
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    CurrentStreak = u.CurrentStreak ?? 0,
                    AvatarUrl = u.AvatarUrl,
                    IsPro = _dbContext.Subscriptions.Any(s =>
                        s.UserId == u.UserId &&
                        s.Status != null &&
                        s.Status == "Active" &&
                        s.StartTime <= now &&
                        s.EndTime >= now
                    )
                })
                .ToListAsync();
        }
    }
}
