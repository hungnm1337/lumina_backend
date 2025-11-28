using DataLayer.DTOs;
using DataLayer.DTOs.Leaderboard;
using RepositoryLayer.Leaderboard;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Leaderboard
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly ILeaderboardRepository _repository;
        private readonly LuminaSystemContext _context;
        private readonly INotificationService _notificationService;

        public LeaderboardService(ILeaderboardRepository repository, LuminaSystemContext context, INotificationService notificationService)
        {
            _repository = repository;
            _context = context;
            _notificationService = notificationService;
        }

        public Task<PaginatedResultDTO<LeaderboardDTO>> GetAllPaginatedAsync(string? keyword = null, int page = 1, int pageSize = 10)
            => _repository.GetAllPaginatedAsync(keyword, page, pageSize);

        public Task<List<LeaderboardDTO>> GetAllAsync(bool? isActive = null)
            => _repository.GetAllAsync(isActive);

        public Task<LeaderboardDTO?> GetByIdAsync(int leaderboardId)
            => _repository.GetByIdAsync(leaderboardId);

        public Task<LeaderboardDTO?> GetCurrentAsync()
            => _repository.GetCurrentAsync();

        public async Task<int> CreateAsync(CreateLeaderboardDTO dto)
        {
            await ValidateAsync(dto.SeasonNumber, dto.StartDate, dto.EndDate, null);
            var now = DateTime.UtcNow;
            var entity = new DataLayer.Models.Leaderboard
            {
                SeasonName = dto.SeasonName,
                SeasonNumber = dto.SeasonNumber,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                CreateAt = now,
                UpdateAt = null
            };
            return await _repository.CreateAsync(entity);
        }

        public async Task<bool> UpdateAsync(int leaderboardId, UpdateLeaderboardDTO dto)
        {
            await ValidateAsync(dto.SeasonNumber, dto.StartDate, dto.EndDate, leaderboardId);
            var entity = new DataLayer.Models.Leaderboard
            {
                LeaderboardId = leaderboardId,
                SeasonName = dto.SeasonName,
                SeasonNumber = dto.SeasonNumber,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                UpdateAt = DateTime.UtcNow
            };
            return await _repository.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int leaderboardId)
            => _repository.DeleteAsync(leaderboardId);

        public Task<bool> SetCurrentAsync(int leaderboardId)
            => _repository.SetCurrentAsync(leaderboardId);

        private async Task ValidateAsync(int seasonNumber, DateTime? start, DateTime? end, int? excludeId)
        {
            if (seasonNumber <= 0) throw new ArgumentException("SeasonNumber must be positive");
            if (start.HasValue && end.HasValue && end < start) throw new ArgumentException("EndDate must be after StartDate");
            var exists = await _repository.ExistsSeasonNumberAsync(seasonNumber, excludeId);
            if (exists) throw new ArgumentException("SeasonNumber already exists");

            // Validate date overlap across all seasons (active or ended). Any intersection is not allowed.
            var overlap = await _repository.ExistsDateOverlapAsync(start, end, excludeId);
            if (overlap) throw new ArgumentException("Date range overlaps with an existing season");
        }

        public Task<List<LeaderboardRankDTO>> GetSeasonRankingAsync(int leaderboardId, int top = 100)
            => _repository.GetSeasonRankingAsync(leaderboardId, top);

        public Task<int> RecalculateSeasonScoresAsync(int leaderboardId)
            => _repository.RecalculateSeasonScoresAsync(leaderboardId);

        public Task<int> ResetSeasonAsync(int leaderboardId, bool archiveScores = true)
            => _repository.ResetSeasonScoresAsync(leaderboardId, archiveScores);

        public async Task<UserSeasonStatsDTO?> GetUserStatsAsync(int userId, int? leaderboardId = null)
        {
            if (!leaderboardId.HasValue)
            {
                var current = await _repository.GetCurrentAsync();
                if (current == null) return null;
                leaderboardId = current.LeaderboardId;
            }

            return await _repository.GetUserSeasonStatsAsync(userId, leaderboardId.Value);
        }

        public async Task<TOEICScoreCalculationDTO?> GetUserTOEICCalculationAsync(int userId, int? leaderboardId = null)
        {
            if (!leaderboardId.HasValue)
            {
                var current = await _repository.GetCurrentAsync();
                if (current == null) return null;
                leaderboardId = current.LeaderboardId;
            }

            return await _repository.GetUserTOEICCalculationAsync(userId, leaderboardId.Value);
        }

        public async Task<int> GetUserRankAsync(int userId, int? leaderboardId = null)
        {
            if (!leaderboardId.HasValue)
            {
                var current = await _repository.GetCurrentAsync();
                if (current == null) return 0;
                leaderboardId = current.LeaderboardId;
            }

            return await _repository.GetUserRankInSeasonAsync(userId, leaderboardId.Value);
        }

        public async Task AutoManageSeasonsAsync()
        {
            // Tự động kích hoạt seasons đã đến ngày bắt đầu
            await _repository.AutoActivateSeasonAsync();
            
            // Tự động kết thúc seasons đã hết hạn
            await _repository.AutoEndSeasonAsync();
        }

        // ============================================
        // CALCULATE SEASON SCORE - TOEIC SCORING SYSTEM
        // ============================================
        
        private static readonly List<TOEICLevelConfig> LevelConfigs = new()
        {
            new() { 
                Level = "Beginner", 
                MinScore = 0, 
                MaxScore = 200, 
                BasePointsPerCorrect = 15,
                TimeBonusPercent = 0.15,
                AccuracyBonusPercent = 0.75
            },
            new() { 
                Level = "Elementary", 
                MinScore = 201, 
                MaxScore = 400, 
                BasePointsPerCorrect = 12,
                TimeBonusPercent = 0.12,
                AccuracyBonusPercent = 0.60
            },
            new() { 
                Level = "Intermediate", 
                MinScore = 401, 
                MaxScore = 600, 
                BasePointsPerCorrect = 8,
                TimeBonusPercent = 0.10,
                AccuracyBonusPercent = 0.40
            },
            new() { 
                Level = "Upper-Intermediate", 
                MinScore = 601, 
                MaxScore = 750, 
                BasePointsPerCorrect = 5,
                TimeBonusPercent = 0.07,
                AccuracyBonusPercent = 0.25
            },
            new() { 
                Level = "Advanced", 
                MinScore = 751, 
                MaxScore = 850, 
                BasePointsPerCorrect = 3,
                TimeBonusPercent = 0.05,
                AccuracyBonusPercent = 0.15
            },
            new() { 
                Level = "Proficient", 
                MinScore = 851, 
                MaxScore = 990, 
                BasePointsPerCorrect = 2,
                TimeBonusPercent = 0.05,
                AccuracyBonusPercent = 0.10
            }
        };

        public async Task<CalculateScoreResponseDTO> CalculateSeasonScoreAsync(int userId, CalculateScoreRequestDTO request)
        {
            // Validate: Chỉ tính điểm cho Listening (1) và Reading (2)
            if (request.ExamPartId != 1 && request.ExamPartId != 2)
            {
                throw new ArgumentException("Chỉ tính điểm cho Listening (ExamPartId=1) và Reading (ExamPartId=2)");
            }

            // ✅ Cho phép CorrectAnswers = 0 để vẫn có thể tạo notification động viên
            // Nếu không có câu nào đúng, vẫn tạo notification nhưng không cộng điểm
            bool hasCorrectAnswers = request.CorrectAnswers > 0;

            // 1. Kiểm tra xem đã làm ĐỀ NÀY chưa (theo ExamId + ExamPartId)
            var examAttempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(ea => ea.AttemptID == request.ExamAttemptId);
            
            if (examAttempt == null)
            {
                throw new ArgumentException("ExamAttempt không tồn tại");
            }

            // Kiểm tra xem đã làm đề này (ExamId + ExamPartId) lần đầu chưa
            var isFirstTimeDoingThisExam = !await _context.ExamAttempts
                .AnyAsync(ea => 
                    ea.UserID == userId
                    && ea.ExamID == examAttempt.ExamID
                    && ea.ExamPartId == examAttempt.ExamPartId
                    && ea.Status == "Completed"
                    && ea.AttemptID < request.ExamAttemptId); // Có attempt nào trước đó đã hoàn thành không

            // 2. Kiểm tra season
            var currentSeason = await _context.Leaderboards
                .FirstOrDefaultAsync(l => l.IsActive);
            
            if (currentSeason == null)
            {
                throw new InvalidOperationException("Không có season nào đang active");
            }

            var userLeaderboard = await _context.UserLeaderboards
                .FirstOrDefaultAsync(ul => 
                    ul.LeaderboardId == currentSeason.LeaderboardId 
                    && ul.UserId == userId);

            bool isFirstAttemptInSeason = userLeaderboard == null || userLeaderboard.FirstAttemptDate == null;

            // 3. LẤY ESTIMATED TOEIC (CHỈ LISTENING + READING)
            var estimatedTOEIC = await GetEstimatedTOEICScore(userId);
            
            // 4. XÁC ĐỊNH LEVEL CONFIG
            var levelConfig = GetLevelConfig(estimatedTOEIC);
            
            // 5. TÍNH BASE POINTS (chỉ khi có câu đúng)
            var basePoints = hasCorrectAnswers ? request.CorrectAnswers * levelConfig.BasePointsPerCorrect : 0;
            
            // 6. TIME BONUS (chỉ khi có câu đúng)
            var timeBonus = hasCorrectAnswers ? CalculateTimeBonus(
                request.TimeSpentSeconds, 
                request.ExpectedTimeSeconds,
                levelConfig.TimeBonusPercent
            ) : 0;
            
            // 7. ACCURACY BONUS (chỉ khi >= 80% và có câu đúng)
            var accuracyRate = request.TotalQuestions > 0 ? (double)request.CorrectAnswers / request.TotalQuestions : 0;
            var accuracyBonus = hasCorrectAnswers && accuracyRate >= 0.8 
                ? CalculateAccuracyBonus(accuracyRate, basePoints, levelConfig.AccuracyBonusPercent)
                : 0;
            
            // 8. TỔNG ĐIỂM TÍCH LŨY CHO LẦN NÀY
            var totalScore = basePoints + timeBonus + accuracyBonus;
            
            // 9. CẬP NHẬT LEADERBOARD (chỉ khi có điểm VÀ làm lần đầu)
            int totalAccumulatedScore = totalScore;

            // ✅ CHỈ CỘNG ĐIỂM KHI LÀM LẦN ĐẦU (không cho spam)
            bool shouldAddPoints = hasCorrectAnswers && isFirstTimeDoingThisExam;

            if (shouldAddPoints)
            {
                if (userLeaderboard == null)
                {
                    // Tạo mới - Lần đầu tiên trong season
                    userLeaderboard = new DataLayer.Models.UserLeaderboard
                    {
                        LeaderboardId = currentSeason.LeaderboardId,
                        UserId = userId,
                        Score = totalScore, // ĐIỂM TÍCH LŨY
                        EstimatedTOEICScore = Math.Min(estimatedTOEIC, 990), // TOEIC lần đầu
                        FirstAttemptDate = DateTime.UtcNow
                    };
                    _context.UserLeaderboards.Add(userLeaderboard);
                    
                    totalAccumulatedScore = totalScore;
                }
                else
                {
                    // Đã có bản ghi - CỘNG DỒN ĐIỂM TÍCH LŨY (chỉ lần đầu)
                    userLeaderboard.Score += totalScore;
                    totalAccumulatedScore = userLeaderboard.Score;

                    // CẬP NHẬT TOEIC (chỉ khi làm đề lần đầu)
                    userLeaderboard.EstimatedTOEICScore = Math.Min(estimatedTOEIC, 990);
                    
                    // Chỉ set FirstAttemptDate lần đầu trong season
                    if (isFirstAttemptInSeason)
                    {
                        userLeaderboard.FirstAttemptDate = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ [LeaderboardService] User {userId} - First attempt. Added {totalScore} points. Total: {totalAccumulatedScore}");
            }
            else
            {
                // Không cộng điểm (làm lại hoặc không có câu đúng)
                if (userLeaderboard != null)
                {
                    totalAccumulatedScore = userLeaderboard.Score;
                }
                
                if (!hasCorrectAnswers)
                {
                    Console.WriteLine($"⚠️ [LeaderboardService] User {userId} has 0 correct answers. No points added, but notification will still be sent.");
                }
                else if (!isFirstTimeDoingThisExam)
                {
                    Console.WriteLine($"⚠️ [LeaderboardService] User {userId} - Not first attempt. No points added (anti-spam). Total remains: {totalAccumulatedScore}");
                }
            }
            
            // Luôn tạo thông báo TOEIC
            var currentTOEICMessage = GetTOEICLevelMessage(levelConfig.Level, estimatedTOEIC);
            
            // Gửi thông báo tự động: Điểm tích lũy
            // ✅ ĐẢM BẢO LUÔN TẠO NOTIFICATION, KỂ CẢ KHI CÓ LỖI
            Console.WriteLine($"📢 [LeaderboardService] ========== START SENDING NOTIFICATION ==========");
            Console.WriteLine($"📢 [LeaderboardService] UserId: {userId}");
            Console.WriteLine($"📢 [LeaderboardService] TotalScore: {totalScore}");
            Console.WriteLine($"📢 [LeaderboardService] TotalAccumulatedScore: {totalAccumulatedScore}");
            Console.WriteLine($"📢 [LeaderboardService] CorrectAnswers: {request.CorrectAnswers}/{request.TotalQuestions}");
            Console.WriteLine($"📢 [LeaderboardService] TimeBonus: {timeBonus}");
            Console.WriteLine($"📢 [LeaderboardService] AccuracyBonus: {accuracyBonus}");
            Console.WriteLine($"📢 [LeaderboardService] IsFirstAttempt: {isFirstTimeDoingThisExam}");
            Console.WriteLine($"📢 [LeaderboardService] ShouldAddPoints: {shouldAddPoints}");
            Console.WriteLine($"📢 [LeaderboardService] NotificationService is null: {_notificationService == null}");
            
            try
            {
                if (_notificationService == null)
                {
                    Console.WriteLine($"❌ [LeaderboardService] CRITICAL: NotificationService is NULL!");
                    throw new InvalidOperationException("NotificationService is not injected!");
                }
                
                Console.WriteLine($"📢 [LeaderboardService] Calling SendPointsNotificationAsync...");
                // ✅ Truyền thêm thông tin về việc có cộng điểm hay không
                var notificationId = await _notificationService.SendPointsNotificationAsync(
                    userId, 
                    shouldAddPoints ? totalScore : 0, // Điểm được cộng (0 nếu làm lại)
                    totalAccumulatedScore,
                    request.CorrectAnswers,
                    request.TotalQuestions,
                    timeBonus,
                    accuracyBonus,
                    isFirstTimeDoingThisExam // Thông tin về lần đầu hay không
                );
                Console.WriteLine($"✅ [LeaderboardService] Points notification {notificationId} sent successfully to user {userId}");
                Console.WriteLine($"📢 [LeaderboardService] ========== NOTIFICATION SENT SUCCESSFULLY ==========");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [LeaderboardService] ========== FAILED TO SEND NOTIFICATION ==========");
                Console.WriteLine($"❌ [LeaderboardService] Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"❌ [LeaderboardService] Message: {ex.Message}");
                Console.WriteLine($"❌ [LeaderboardService] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"❌ [LeaderboardService] InnerException: {ex.InnerException.Message}");
                    Console.WriteLine($"❌ [LeaderboardService] InnerStackTrace: {ex.InnerException.StackTrace}");
                }
                // ✅ KHÔNG THROW LẠI - ĐỂ API VẪN TRẢ VỀ THÀNH CÔNG
                // Nhưng notification sẽ không được tạo nếu có lỗi
            }

            // Gửi thông báo tự động: Kết quả TOEIC (chỉ khi làm đề lần đầu)
            if (isFirstTimeDoingThisExam)
            {
                try
                {
                    Console.WriteLine($"📢 [LeaderboardService] Sending TOEIC notification to user {userId}...");
                    var toeicNotificationId = await _notificationService.SendTOEICNotificationAsync(userId, estimatedTOEIC, levelConfig.Level, currentTOEICMessage);
                    Console.WriteLine($"✅ [LeaderboardService] TOEIC notification {toeicNotificationId} sent successfully to user {userId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [LeaderboardService] Failed to send TOEIC notification: {ex.Message}");
                    Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                }
            }
            
            return new CalculateScoreResponseDTO
            {
                SeasonScore = totalScore,
                EstimatedTOEIC = estimatedTOEIC,
                TOEICLevel = levelConfig.Level,
                BasePoints = basePoints,
                TimeBonus = timeBonus,
                AccuracyBonus = accuracyBonus,
                IsFirstAttempt = isFirstTimeDoingThisExam, // True nếu làm đề này lần đầu
                TOEICMessage = currentTOEICMessage,
                TotalAccumulatedScore = totalAccumulatedScore
            };
        }

        private async Task<int> GetEstimatedTOEICScore(int userId)
        {
            // CHỈ LẤY LISTENING (PartId=1) VÀ READING (PartId=2)
            // CHỈ LẤY LẦN ĐẦU TIÊN của mỗi đề (theo ExamID + ExamPartId)
            var allAttempts = await _context.ExamAttempts
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.ExamParts)
                .Where(ea => ea.UserID == userId 
                    && ea.Status == "Completed"
                    && ea.ExamPartId != null
                    && (ea.ExamPartId == 1 || ea.ExamPartId == 2)) // Listening & Reading only
                .OrderBy(ea => ea.EndTime) // Sắp xếp từ cũ đến mới
                .ToListAsync();

            if (!allAttempts.Any()) return 0;

            // Lấy LẦN ĐẦU TIÊN của mỗi (ExamID, ExamPartId)
            var firstAttempts = allAttempts
                .GroupBy(ea => new { ea.ExamID, ea.ExamPartId })
                .Select(g => g.First()) // Lấy lần đầu tiên (EndTime nhỏ nhất)
                .OrderByDescending(ea => ea.EndTime) // Sắp xếp lại theo mới nhất
                .Take(10) // Lấy 10 đề gần nhất
                .ToList();

            // Tính điểm trung bình Listening
            var listeningAttempts = firstAttempts
                .Where(ea => ea.ExamPartId == 1)
                .ToList();
            var avgListening = listeningAttempts.Any() 
                ? listeningAttempts.Average(ea => ea.Score ?? 0) 
                : 0;

            // Tính điểm trung bình Reading
            var readingAttempts = firstAttempts
                .Where(ea => ea.ExamPartId == 2)
                .ToList();
            var avgReading = readingAttempts.Any() 
                ? readingAttempts.Average(ea => ea.Score ?? 0) 
                : 0;

            // CHUYỂN ĐỔI: Score 0-100 → TOEIC 0-495 (mỗi phần)
            var estimatedListening = (int)(avgListening * 4.95);
            var estimatedReading = (int)(avgReading * 4.95);

            // Tổng TOEIC = Listening + Reading (0-990)
            return estimatedListening + estimatedReading;
        }

        private TOEICLevelConfig GetLevelConfig(int toeicScore)
        {
            return LevelConfigs
                .OrderByDescending(c => c.MinScore)
                .First(c => toeicScore >= c.MinScore);
        }

        private int CalculateTimeBonus(int timeSpentSeconds, int expectedTimeSeconds, double timeBonusPercent)
        {
            if (timeSpentSeconds >= expectedTimeSeconds) return 0;

            var timeSavedPercent = (expectedTimeSeconds - timeSpentSeconds) / (double)expectedTimeSeconds;
            
            return (int)(timeSavedPercent * timeBonusPercent * 100);
        }

        private int CalculateAccuracyBonus(double accuracyRate, int basePoints, double accuracyBonusPercent)
        {
            // Chỉ áp dụng khi accuracy >= 80%
            if (accuracyRate < 0.8) return 0;

            // Bonus tăng dần từ 80% → 100%
            var bonusRatio = (accuracyRate - 0.8) / 0.2; // 0.0 → 1.0
            return (int)(basePoints * accuracyBonusPercent * bonusRatio);
        }

        private async Task UpdateLeaderboardScore(int userId, int seasonScore)
        {
            var currentSeason = await _context.Leaderboards
                .FirstOrDefaultAsync(l => l.IsActive);
            
            if (currentSeason == null) return;

            var ranking = await _context.UserLeaderboards
                .FirstOrDefaultAsync(lr => 
                    lr.LeaderboardId == currentSeason.LeaderboardId 
                    && lr.UserId == userId);

            if (ranking == null)
            {
                // Tạo mới - CHỈ lưu Score
                ranking = new DataLayer.Models.UserLeaderboard
                {
                    LeaderboardId = currentSeason.LeaderboardId,
                    UserId = userId,
                    Score = seasonScore
                };
                _context.UserLeaderboards.Add(ranking);
            }
            else
            {
                // Cộng dồn điểm - CHỈ cập nhật Score
                ranking.Score += seasonScore;
            }

            await _context.SaveChangesAsync();
        }

        private int GetTOEICFromLevel(string level)
        {
            var config = LevelConfigs.FirstOrDefault(c => c.Level == level);
            return config?.MinScore ?? 0;
        }

        private string GetTOEICLevelMessage(string level, int score)
        {
            var messages = new Dictionary<string, string>
            {
                ["Beginner"] = $"🎯 Chúc mừng! Bạn đang ở trình độ Beginner với ước tính {score} điểm TOEIC. Hãy tiếp tục luyện tập để đạt 200+ điểm!",
                ["Elementary"] = $"📚 Tuyệt vời! Bạn đã đạt trình độ Elementary với ước tính {score} điểm TOEIC. Mục tiêu tiếp theo: 400+ điểm!",
                ["Intermediate"] = $"⭐ Xuất sắc! Bạn đang ở trình độ Intermediate với ước tính {score} điểm TOEIC. Tiếp tục phấn đấu để đạt 600+ điểm!",
                ["Upper-Intermediate"] = $"🎓 Thật ấn tượng! Bạn đã đạt Upper-Intermediate với ước tính {score} điểm TOEIC. Chỉ còn một bước nữa đến Advanced!",
                ["Advanced"] = $"🏆 Rất xuất sắc! Bạn đang ở trình độ Advanced với ước tính {score} điểm TOEIC. Hãy hướng tới đỉnh cao 850+ điểm!",
                ["Proficient"] = $"💎 Đỉnh cao! Bạn đã đạt trình độ Proficient với ước tính {score} điểm TOEIC. Bạn đang ở top đầu người học!"
            };

            return messages.TryGetValue(level, out var message) ? message : $"Trình độ TOEIC ước tính của bạn: {score} điểm";
        }
    }
}
