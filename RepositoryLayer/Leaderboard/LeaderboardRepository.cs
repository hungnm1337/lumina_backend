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

        // Bảng quy đổi TOEIC Listening (100 câu → 5-495 điểm)
        private static readonly Dictionary<int, int> ListeningScoreTable = new()
        {
            {0, 5}, {1, 15}, {2, 20}, {3, 25}, {4, 30}, {5, 35}, {6, 40}, {7, 45}, {8, 50}, {9, 55},
            {10, 60}, {11, 65}, {12, 70}, {13, 75}, {14, 80}, {15, 85}, {16, 90}, {17, 95}, {18, 100}, {19, 105},
            {20, 110}, {21, 115}, {22, 120}, {23, 125}, {24, 130}, {25, 135}, {26, 140}, {27, 145}, {28, 150}, {29, 155},
            {30, 160}, {31, 165}, {32, 170}, {33, 175}, {34, 180}, {35, 185}, {36, 190}, {37, 195}, {38, 200}, {39, 205},
            {40, 210}, {41, 215}, {42, 220}, {43, 225}, {44, 230}, {45, 235}, {46, 240}, {47, 245}, {48, 250}, {49, 255},
            {50, 260}, {51, 265}, {52, 270}, {53, 275}, {54, 280}, {55, 285}, {56, 290}, {57, 295}, {58, 300}, {59, 305},
            {60, 310}, {61, 315}, {62, 320}, {63, 325}, {64, 330}, {65, 335}, {66, 340}, {67, 345}, {68, 350}, {69, 355},
            {70, 360}, {71, 365}, {72, 370}, {73, 375}, {74, 380}, {75, 385}, {76, 395}, {77, 400}, {78, 405}, {79, 410},
            {80, 415}, {81, 420}, {82, 425}, {83, 430}, {84, 435}, {85, 440}, {86, 445}, {87, 450}, {88, 455}, {89, 460},
            {90, 465}, {91, 470}, {92, 475}, {93, 480}, {94, 485}, {95, 490}, {96, 495}, {97, 495}, {98, 495}, {99, 495},
            {100, 495}
        };

        // Bảng quy đổi TOEIC Reading (100 câu → 5-495 điểm)
        private static readonly Dictionary<int, int> ReadingScoreTable = new()
        {
            {0, 5}, {1, 5}, {2, 5}, {3, 10}, {4, 15}, {5, 20}, {6, 25}, {7, 30}, {8, 35}, {9, 40},
            {10, 45}, {11, 50}, {12, 55}, {13, 60}, {14, 65}, {15, 70}, {16, 75}, {17, 80}, {18, 85}, {19, 90},
            {20, 95}, {21, 100}, {22, 105}, {23, 110}, {24, 115}, {25, 120}, {26, 125}, {27, 130}, {28, 135}, {29, 140},
            {30, 145}, {31, 150}, {32, 155}, {33, 160}, {34, 165}, {35, 170}, {36, 175}, {37, 180}, {38, 185}, {39, 190},
            {40, 195}, {41, 200}, {42, 205}, {43, 210}, {44, 215}, {45, 220}, {46, 225}, {47, 230}, {48, 235}, {49, 240},
            {50, 245}, {51, 250}, {52, 255}, {53, 260}, {54, 265}, {55, 270}, {56, 275}, {57, 280}, {58, 285}, {59, 290},
            {60, 295}, {61, 300}, {62, 305}, {63, 310}, {64, 315}, {65, 320}, {66, 325}, {67, 330}, {68, 335}, {69, 340},
            {70, 345}, {71, 350}, {72, 355}, {73, 360}, {74, 365}, {75, 370}, {76, 375}, {77, 380}, {78, 385}, {79, 390},
            {80, 395}, {81, 400}, {82, 405}, {83, 410}, {84, 415}, {85, 420}, {86, 425}, {87, 430}, {88, 435}, {89, 440},
            {90, 445}, {91, 450}, {92, 455}, {93, 460}, {94, 465}, {95, 470}, {96, 475}, {97, 480}, {98, 485}, {99, 490},
            {100, 495}
        };

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
        /// <summary>
        /// Quy đổi số câu đúng từ hệ thống 61 câu sang 100 câu, sau đó tra bảng TOEIC
        /// </summary>
        private int ConvertTo100ScaleAndLookup(double correctAnswers, int totalQuestions, Dictionary<int, int> scoreTable)
        {
            if (correctAnswers <= 0) return scoreTable[0];
            if (totalQuestions <= 0) return scoreTable[0];
            
            // Bước 1: Quy đổi về thang 100 câu
            double scaledScore = (correctAnswers / totalQuestions) * 100.0;
            
            // Bước 2: Làm tròn (round half up)
            int roundedScore = (int)Math.Round(scaledScore, MidpointRounding.AwayFromZero);
            
            // Bảo đảm trong khoảng [0, 100]
            roundedScore = Math.Clamp(roundedScore, 0, 100);
            
            // Bước 3: Tra bảng TOEIC
            return scoreTable[roundedScore];
        }

        private async Task<int> CalculateEstimatedTOEICScore(int userId, int leaderboardId)
        {
            var season = await _context.Leaderboards.FirstOrDefaultAsync(x => x.LeaderboardId == leaderboardId);
            if (season == null) return 0;

            // LẤY TẤT CẢ CÁC LẦN THI ĐÃ HOÀN THÀNH TRONG SEASON
            var allAttempts = await _context.ExamAttempts
                .Include(ea => ea.ExamPart) // Load ExamPart để lấy PartCode
                .Where(ea => ea.UserID == userId 
                    && ea.Status == "Completed"
                    && ea.ExamPartId != null
                    && ea.ExamPart != null
                    && (!season.StartDate.HasValue || ea.EndTime >= season.StartDate)
                    && (!season.EndDate.HasValue || ea.EndTime <= season.EndDate))
                .OrderBy(ea => ea.EndTime)
                .ToListAsync();

            if (!allAttempts.Any()) return 0;

            // XÁC ĐỊNH SKILL (Listening/Reading) DỰA VÀO PartCode
            var attemptsWithSkill = allAttempts
                .Where(ea => ea.ExamPart.PartCode.StartsWith("LISTENING") || ea.ExamPart.PartCode.StartsWith("READING"))
                .Select(ea => new {
                    ea.ExamID,
                    ea.ExamPartId,
                    ea.Score, // Score = số câu đúng
                    ea.EndTime,
                    Skill = ea.ExamPart.PartCode.StartsWith("LISTENING") ? "Listening" : "Reading"
                })
                .ToList();

            if (!attemptsWithSkill.Any()) return 0;

            // LẤY LẦN ĐẦU TIÊN của mỗi (ExamID, ExamPartId)
            var firstAttempts = attemptsWithSkill
                .GroupBy(x => new { x.ExamID, x.ExamPartId })
                .Select(g => g.OrderBy(x => x.EndTime).First())
                .ToList();

            // NHÓM THEO ExamID + Skill VÀ CỘNG TẤT CẢ CÁC PARTS
            var groupedByExamAndSkill = firstAttempts
                .GroupBy(x => new { x.ExamID, x.Skill })
                .Select(g => new {
                    ExamID = g.Key.ExamID,
                    Skill = g.Key.Skill,
                    // CỘNG SỐ CÂU ĐÚNG: Score = số câu đúng
                    TotalCorrectAnswers = g.Sum(x => (double)(x.Score ?? 0)),
                    CompletedParts = g.Count(),
                    FirstAttemptDate = g.Min(x => x.EndTime)
                })
                .OrderByDescending(x => x.FirstAttemptDate)
                .Take(10)
                .ToList();

            // TÁCH LISTENING VÀ READING
            var listeningExams = groupedByExamAndSkill.Where(x => x.Skill == "Listening").ToList();
            var readingExams = groupedByExamAndSkill.Where(x => x.Skill == "Reading").ToList();

            // TÍNH SỐ CÂU ĐÚNG TRUNG BÌNH
            var avgCorrectListening = listeningExams.Any()
                ? listeningExams.Average(x => x.TotalCorrectAnswers)
                : 0;

            var avgCorrectReading = readingExams.Any()
                ? readingExams.Average(x => x.TotalCorrectAnswers)
                : 0;

            // QUY ĐỔI SANG ĐIỂM TOEIC
            var estimatedListening = ConvertTo100ScaleAndLookup(avgCorrectListening, 61, ListeningScoreTable);
            var estimatedReading = ConvertTo100ScaleAndLookup(avgCorrectReading, 61, ReadingScoreTable);

            return estimatedListening + estimatedReading;
        }

        private string GetTOEICLevel(int toeicScore)
        {
            return toeicScore switch
            {
                >= 905 => "Proficient", // Giao tiếp trôi chảy, tự nhiên mọi hoàn cảnh
                >= 785 => "Advanced", // Đáp ứng hầu hết yêu cầu công việc
                >= 605 => "Upper-Intermediate", // Giao tiếp thông thường, hạn chế công việc
                >= 405 => "Intermediate", // Giao tiếp đơn giản theo tình huống quen thuộc
                >= 255 => "Elementary", // Trình độ hạn chế
                >= 10 => "Beginner", // Chỉ đáp ứng yêu cầu căn bản
                _ => "Beginner" // Dưới 10 điểm
            };
        }

        private (int BasePoints, decimal TimeBonus, decimal AccuracyBonus) GetScoreConfigByTOEIC(int toeicScore)
        {
            return toeicScore switch
            {
                >= 905 => (2, 0.10m, 0.20m),   // Proficient - Giao tiếp trôi chảy
                >= 785 => (3, 0.15m, 0.40m),   // Advanced - Đáp ứng công việc
                >= 605 => (5, 0.20m, 0.60m),   // Upper-Intermediate - Giao tiếp thông thường
                >= 405 => (8, 0.25m, 0.90m),   // Intermediate - Giao tiếp đơn giản
                >= 255 => (12, 0.28m, 1.20m),  // Elementary - Trình độ hạn chế
                _ => (15, 0.30m, 1.50m)        // Beginner - Căn bản
            };
        }
    }
}
