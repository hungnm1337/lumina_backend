using DataLayer.DTOs;
using DataLayer.DTOs.Leaderboard;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.Leaderboard
{
    public class LeaderboardRepository : ILeaderboardRepository
    {
        private readonly LuminaSystemContext _context;

        public LeaderboardRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResultDTO<LeaderboardDTO>> GetAllPaginatedAsync(string? keyword = null, int page = 1, int pageSize = 10)
        {
            var query = _context.Leaderboards.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var key = keyword.Trim();
                query = query.Where(x => (x.SeasonName != null && x.SeasonName.Contains(key)) || x.SeasonNumber.ToString().Contains(key));
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            var items = await query
                .OrderByDescending(x => x.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = new List<LeaderboardDTO>();
            foreach (var item in items)
            {
                dtos.Add(await MapToDtoAsync(item));
            }

            return new PaginatedResultDTO<LeaderboardDTO>
            {
                Items = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNext = page < totalPages,
                HasPrevious = page > 1
            };
        }

        public async Task<List<LeaderboardDTO>> GetAllAsync(bool? isActive = null)
        {
            var query = _context.Leaderboards.AsQueryable();
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }
            var items = await query.OrderByDescending(x => x.StartDate).ToListAsync();
            
            var dtos = new List<LeaderboardDTO>();
            foreach (var item in items)
            {
                dtos.Add(await MapToDtoAsync(item));
            }
            return dtos;
        }

        public async Task<LeaderboardDTO?> GetByIdAsync(int leaderboardId)
        {
            var entity = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            return entity == null ? null : await MapToDtoAsync(entity);
        }

        public async Task<LeaderboardDTO?> GetCurrentAsync()
        {
            var now = DateTime.UtcNow;
            var entity = await _context.Leaderboards
                .Where(x => x.IsActive && (!x.StartDate.HasValue || x.StartDate <= now) && (!x.EndDate.HasValue || x.EndDate >= now))
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefaultAsync();
            return entity == null ? null : await MapToDtoAsync(entity);
        }

        public async Task<int> CreateAsync(DataLayer.Models.Leaderboard entity)
        {
            _context.Leaderboards.Add(entity);
            await _context.SaveChangesAsync();
            return entity.LeaderboardId;
        }

        public async Task<bool> UpdateAsync(DataLayer.Models.Leaderboard entity)
        {
            var existing = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == entity.LeaderboardId);
            if (existing == null) return false;

            existing.SeasonName = entity.SeasonName;
            existing.SeasonNumber = entity.SeasonNumber;
            existing.StartDate = entity.StartDate;
            existing.EndDate = entity.EndDate;
            existing.IsActive = entity.IsActive;
            existing.UpdateAt = entity.UpdateAt;

            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int leaderboardId)
        {
            var existing = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (existing == null) return false;
            _context.Leaderboards.Remove(existing);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> SetCurrentAsync(int leaderboardId)
        {
            var target = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (target == null) return false;

            var all = await _context.Leaderboards.ToListAsync();
            foreach (var l in all)
            {
                l.IsActive = l.LeaderboardId == leaderboardId;
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<bool> ExistsSeasonNumberAsync(int seasonNumber, int? excludeId = null)
        {
            return _context.Leaderboards.AnyAsync(x => x.SeasonNumber == seasonNumber && (!excludeId.HasValue || x.LeaderboardId != excludeId.Value));
        }

        public Task<bool> ExistsDateOverlapAsync(DateTime? start, DateTime? end, int? excludeId = null)
        {
            return _context.Leaderboards.AnyAsync(x =>
                (!excludeId.HasValue || x.LeaderboardId != excludeId.Value)
                && (
                    (
                        (!x.StartDate.HasValue || !end.HasValue || x.StartDate <= end)
                        && (!start.HasValue || !x.EndDate.HasValue || start <= x.EndDate)
                    )
                )
            );
        }

        private async Task<LeaderboardDTO> MapToDtoAsync(DataLayer.Models.Leaderboard e)
        {
            var now = DateTime.UtcNow;
            var status = "Upcoming";
            var daysRemaining = 0;

            if (e.StartDate.HasValue && e.EndDate.HasValue)
            {
                if (now < e.StartDate.Value)
                {
                    status = "Upcoming";
                    daysRemaining = (e.StartDate.Value - now).Days;
                }
                else if (now >= e.StartDate.Value && now <= e.EndDate.Value)
                {
                    status = "Active";
                    daysRemaining = (e.EndDate.Value - now).Days;
                }
                else
                {
                    status = "Ended";
                    daysRemaining = 0;
                }
            }

            var totalParticipants = await _context.UserLeaderboards
                .Where(ul => ul.LeaderboardId == e.LeaderboardId)
                .Select(ul => ul.UserId)
                .Distinct()
                .CountAsync();

            return new LeaderboardDTO
            {
                LeaderboardId = e.LeaderboardId,
                SeasonName = e.SeasonName,
                SeasonNumber = e.SeasonNumber,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                IsActive = e.IsActive,
                CreateAt = e.CreateAt,
                UpdateAt = e.UpdateAt,
                TotalParticipants = totalParticipants,
                Status = status,
                DaysRemaining = daysRemaining
            };
        }

        public async Task<List<LeaderboardRankDTO>> GetSeasonRankingAsync(int leaderboardId, int top = 100)
        {
            var season = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (season == null) return new List<LeaderboardRankDTO>();

            // Lấy từ UserLeaderboards (điểm đã được tính sẵn)
            var persisted = await _context.UserLeaderboards
                .Where(ul => ul.LeaderboardId == leaderboardId)
                .OrderByDescending(ul => ul.Score)
                .Take(top)
                .Select(ul => new { ul.UserId, ul.Score })
                .ToListAsync();

            if (persisted.Count == 0)
            {
                return new List<LeaderboardRankDTO>();
            }

            var userIds = persisted.Select(x => x.UserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => new { u.FullName, u.AvatarUrl });

            int rank = 1;
            var result = new List<LeaderboardRankDTO>();

            foreach (var item in persisted)
            {
                var estimatedToeic = await CalculateEstimatedTOEICScore(item.UserId, leaderboardId);
                var toeicLevel = GetTOEICLevel(estimatedToeic);

                result.Add(new LeaderboardRankDTO
                {
                    UserId = item.UserId,
                    FullName = users.TryGetValue(item.UserId, out var user) ? user.FullName : $"User {item.UserId}",
                    Score = item.Score,
                    Rank = rank++,
                    EstimatedTOEICScore = estimatedToeic,
                    ToeicLevel = toeicLevel,
                    AvatarUrl = users.TryGetValue(item.UserId, out var u) ? u.AvatarUrl : null
                });
            }

            return result;
        }

        public async Task<int> RecalculateSeasonScoresAsync(int leaderboardId)
        {
            var season = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (season == null) return 0;

            // Xóa điểm cũ
            var existing = _context.UserLeaderboards.Where(ul => ul.LeaderboardId == leaderboardId);
            _context.UserLeaderboards.RemoveRange(existing);
            await _context.SaveChangesAsync();

            // Tính lại điểm từ UserAnswerMultipleChoice (và các bảng khác nếu có)
            var attempts = await _context.ExamAttempts
                .Where(ea => ea.Status == "Completed" 
                    && (!season.StartDate.HasValue || ea.EndTime >= season.StartDate)
                    && (!season.EndDate.HasValue || ea.EndTime <= season.EndDate))
                .Select(ea => new { ea.AttemptID, ea.UserID, ea.EndTime, ea.StartTime })
                .ToListAsync();

            var userScores = new Dictionary<int, int>();

            foreach (var attempt in attempts)
            {
                var answers = await _context.UserAnswerMultipleChoices
                    .Where(ua => ua.AttemptID == attempt.AttemptID)
                    .ToListAsync();

                if (answers.Count == 0) continue;

                var correctCount = answers.Count(a => a.IsCorrect);
                var totalQuestions = answers.Count;
                var accuracyRate = totalQuestions > 0 ? (decimal)correctCount / totalQuestions : 0;
                
                // Lấy độ khó trung bình (giả sử có field DifficultyLevel trong Question)
                var avgDifficulty = 1.0m; // Default Medium

                // Tính thời gian làm bài
                var timeSpent = attempt.EndTime.HasValue 
                    ? (attempt.EndTime.Value - attempt.StartTime).TotalMinutes 
                    : 0;

                // Tính điểm theo công thức TOEIC
                var estimatedToeic = await CalculateEstimatedTOEICScore(attempt.UserID, leaderboardId);
                var scoreConfig = GetScoreConfigByTOEIC(estimatedToeic);
                
                var baseScore = correctCount * scoreConfig.BasePoints;
                var timeBonus = timeSpent < 30 ? baseScore * scoreConfig.TimeBonus : 0; // Bonus nếu làm nhanh
                var accuracyBonus = accuracyRate >= 0.8m ? baseScore * scoreConfig.AccuracyBonus : 0;
                var difficultyBonus = baseScore * (avgDifficulty - 1);

                var totalScore = (int)(baseScore + timeBonus + accuracyBonus + difficultyBonus);

                if (!userScores.ContainsKey(attempt.UserID))
                    userScores[attempt.UserID] = 0;
                
                userScores[attempt.UserID] += totalScore;
            }

            // Lưu vào UserLeaderboards
            foreach (var kvp in userScores)
            {
                _context.UserLeaderboards.Add(new UserLeaderboard
                {
                    UserId = kvp.Key,
                    LeaderboardId = leaderboardId,
                    Score = kvp.Value
                });
            }

            return await _context.SaveChangesAsync();
        }

        public async Task<int> ResetSeasonScoresAsync(int leaderboardId, bool archiveScores = true)
        {
            var season = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (season == null) return 0;

            if (archiveScores)
            {
                // TODO: Implement archiving logic (lưu vào bảng LeaderboardArchive)
                // Có thể tạo bảng mới để lưu lịch sử điểm của các season đã kết thúc
            }

            // Reset điểm về 0
            var userLeaderboards = await _context.UserLeaderboards
                .Where(ul => ul.LeaderboardId == leaderboardId)
                .ToListAsync();

            _context.UserLeaderboards.RemoveRange(userLeaderboards);
            return await _context.SaveChangesAsync();
        }

        public async Task<UserSeasonStatsDTO?> GetUserSeasonStatsAsync(int userId, int leaderboardId)
        {
            var season = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (season == null) return null;

            var userLeaderboard = await _context.UserLeaderboards
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LeaderboardId == leaderboardId);

            if (userLeaderboard == null)
            {
                return new UserSeasonStatsDTO
                {
                    UserId = userId,
                    CurrentRank = 0,
                    CurrentScore = 0,
                    EstimatedTOEICScore = 0,
                    ToeicLevel = "Beginner",
                    TotalAttempts = 0,
                    CorrectAnswers = 0,
                    AccuracyRate = 0,
                    IsReadyForTOEIC = false
                };
            }

            var rank = await GetUserRankInSeasonAsync(userId, leaderboardId);
            var estimatedToeic = await CalculateEstimatedTOEICScore(userId, leaderboardId);
            
            // Tính stats từ attempts
            var attempts = await _context.ExamAttempts
                .Where(ea => ea.UserID == userId 
                    && ea.Status == "Completed"
                    && (!season.StartDate.HasValue || ea.EndTime >= season.StartDate)
                    && (!season.EndDate.HasValue || ea.EndTime <= season.EndDate))
                .ToListAsync();

            var attemptIds = attempts.Select(a => a.AttemptID).ToList();
            var answers = await _context.UserAnswerMultipleChoices
                .Where(ua => attemptIds.Contains(ua.AttemptID))
                .ToListAsync();

            var totalAttempts = attempts.Count;
            var correctAnswers = answers.Count(a => a.IsCorrect);
            var totalAnswers = answers.Count;
            var accuracyRate = totalAnswers > 0 ? (decimal)correctAnswers / totalAnswers : 0;

            return new UserSeasonStatsDTO
            {
                UserId = userId,
                CurrentRank = rank,
                CurrentScore = userLeaderboard.Score,
                EstimatedTOEICScore = estimatedToeic,
                ToeicLevel = GetTOEICLevel(estimatedToeic),
                TotalAttempts = totalAttempts,
                CorrectAnswers = correctAnswers,
                AccuracyRate = accuracyRate,
                IsReadyForTOEIC = estimatedToeic >= 600
            };
        }

        public async Task<TOEICScoreCalculationDTO?> GetUserTOEICCalculationAsync(int userId, int leaderboardId)
        {
            var season = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (season == null) return null;

            var userLeaderboard = await _context.UserLeaderboards
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LeaderboardId == leaderboardId);

            var estimatedToeic = await CalculateEstimatedTOEICScore(userId, leaderboardId);
            var config = GetScoreConfigByTOEIC(estimatedToeic);

            return new TOEICScoreCalculationDTO
            {
                UserId = userId,
                EstimatedTOEICScore = estimatedToeic,
                ToeicLevel = GetTOEICLevel(estimatedToeic),
                BasePointsPerCorrect = config.BasePoints,
                TimeBonus = config.TimeBonus,
                AccuracyBonus = config.AccuracyBonus,
                DifficultyMultiplier = 1.0m, // Default
                TotalSeasonScore = userLeaderboard?.Score ?? 0
            };
        }

        public async Task<int> GetUserRankInSeasonAsync(int userId, int leaderboardId)
        {
            var userScore = await _context.UserLeaderboards
                .Where(ul => ul.UserId == userId && ul.LeaderboardId == leaderboardId)
                .Select(ul => ul.Score)
                .FirstOrDefaultAsync();

            if (userScore == 0) return 0;

            var rank = await _context.UserLeaderboards
                .Where(ul => ul.LeaderboardId == leaderboardId && ul.Score > userScore)
                .CountAsync();

            return rank + 1;
        }

        public async Task<bool> IsSeasonActiveAsync(int leaderboardId)
        {
            var now = DateTime.UtcNow;
            return await _context.Leaderboards
                .AnyAsync(x => x.LeaderboardId == leaderboardId 
                    && x.IsActive 
                    && (!x.StartDate.HasValue || x.StartDate <= now) 
                    && (!x.EndDate.HasValue || x.EndDate >= now));
        }

        public async Task AutoActivateSeasonAsync()
        {
            var now = DateTime.UtcNow;
            var seasonsToActivate = await _context.Leaderboards
                .Where(x => !x.IsActive 
                    && x.StartDate.HasValue 
                    && x.StartDate <= now 
                    && (!x.EndDate.HasValue || x.EndDate >= now))
                .ToListAsync();

            foreach (var season in seasonsToActivate)
            {
                season.IsActive = true;
                season.UpdateAt = now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task AutoEndSeasonAsync()
        {
            var now = DateTime.UtcNow;
            var seasonsToEnd = await _context.Leaderboards
                .Where(x => x.IsActive 
                    && x.EndDate.HasValue 
                    && x.EndDate < now)
                .ToListAsync();

            foreach (var season in seasonsToEnd)
            {
                season.IsActive = false;
                season.UpdateAt = now;
            }

            await _context.SaveChangesAsync();
        }

        // Helper methods
        private async Task<int> CalculateEstimatedTOEICScore(int userId, int leaderboardId)
        {
            var season = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (season == null) return 0;

            // Lấy 10 bài gần nhất
            var recentAttempts = await _context.ExamAttempts
                .Where(ea => ea.UserID == userId 
                    && ea.Status == "Completed"
                    && (!season.StartDate.HasValue || ea.EndTime >= season.StartDate)
                    && (!season.EndDate.HasValue || ea.EndTime <= season.EndDate))
                .OrderByDescending(ea => ea.EndTime)
                .Take(10)
                .ToListAsync();

            if (recentAttempts.Count == 0) return 0;

            var attemptIds = recentAttempts.Select(a => a.AttemptID).ToList();
            var answers = await _context.UserAnswerMultipleChoices
                .Where(ua => attemptIds.Contains(ua.AttemptID))
                .ToListAsync();

            var totalQuestions = answers.Count;
            var correctAnswers = answers.Count(a => a.IsCorrect);

            if (totalQuestions == 0) return 0;

            // Công thức ước tính: TOEIC = (correct/total) * 990
            var accuracyRate = (decimal)correctAnswers / totalQuestions;
            var estimatedScore = (int)(accuracyRate * 990);

            return estimatedScore;
        }

        private string GetTOEICLevel(int toeicScore)
        {
            return toeicScore switch
            {
                >= 851 => "Proficient", // Xuất sắc
                >= 751 => "Advanced", // Sẵn sàng thi
                >= 601 => "Upper-Intermediate", // Khá tốt
                >= 401 => "Intermediate", // Trung bình
                >= 201 => "Elementary", // Đang tiến bộ
                _ => "Beginner" // Bắt đầu hành trình
            };
        }

        private (int BasePoints, decimal TimeBonus, decimal AccuracyBonus) GetScoreConfigByTOEIC(int toeicScore)
        {
            return toeicScore switch
            {
                >= 851 => (2, 0.10m, 0.20m),   // Proficient
                >= 751 => (3, 0.15m, 0.40m),   // Advanced
                >= 601 => (5, 0.20m, 0.60m),   // Upper-Intermediate
                >= 401 => (8, 0.25m, 0.90m),   // Intermediate
                >= 201 => (12, 0.28m, 1.20m),  // Elementary
                _ => (15, 0.30m, 1.50m)        // Beginner
            };
        }
    }
}
