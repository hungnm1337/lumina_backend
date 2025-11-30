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
        
        // Bảng quy đổi TOEIC Listening (100 câu → 5-495 điểm)
        // Nguồn: logic_quy_doi_toeic_61_markdown.md (lines 82-139)
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
        // Nguồn: logic_quy_doi_toeic_61_markdown.md (lines 143-200)
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

        private static readonly List<TOEICLevelConfig> LevelConfigs = new()
        {
            new() { 
                Level = "Beginner", 
                Description = "Chỉ đáp ứng yêu cầu căn bản",
                MinScore = 10, 
                MaxScore = 250, 
                BasePointsPerCorrect = 15,
                TimeBonusPercent = 0.30,
                AccuracyBonusPercent = 1.50
            },
            new() { 
                Level = "Elementary", 
                Description = "Trình độ hạn chế",
                MinScore = 255, 
                MaxScore = 400, 
                BasePointsPerCorrect = 12,
                TimeBonusPercent = 0.28,
                AccuracyBonusPercent = 1.20
            },
            new() { 
                Level = "Intermediate", 
                Description = "Giao tiếp đơn giản theo tình huống quen thuộc",
                MinScore = 405, 
                MaxScore = 600, 
                BasePointsPerCorrect = 8,
                TimeBonusPercent = 0.25,
                AccuracyBonusPercent = 0.90
            },
            new() { 
                Level = "Upper-Intermediate", 
                Description = "Giao tiếp thông thường, hạn chế công việc",
                MinScore = 605, 
                MaxScore = 780, 
                BasePointsPerCorrect = 5,
                TimeBonusPercent = 0.20,
                AccuracyBonusPercent = 0.60
            },
            new() { 
                Level = "Advanced", 
                Description = "Đáp ứng hầu hết yêu cầu công việc",
                MinScore = 785, 
                MaxScore = 900, 
                BasePointsPerCorrect = 3,
                TimeBonusPercent = 0.15,
                AccuracyBonusPercent = 0.40
            },
            new() { 
                Level = "Proficient", 
                Description = "Giao tiếp trôi chảy, tự nhiên mọi hoàn cảnh",
                MinScore = 905, 
                MaxScore = 990, 
                BasePointsPerCorrect = 2,
                TimeBonusPercent = 0.10,
                AccuracyBonusPercent = 0.20
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
            
            // Tạo thông báo chúc mừng hoàn thành part (tiếng Anh)
            var currentTOEICMessage = GetCompletionMessage(totalScore, isFirstTimeDoingThisExam);
            
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
                EstimatedTOEIC = 0, // Ẩn TOEIC info theo yêu cầu user
                TOEICLevel = "", // Ẩn level info
                BasePoints = basePoints,
                TimeBonus = timeBonus,
                AccuracyBonus = accuracyBonus,
                IsFirstAttempt = isFirstTimeDoingThisExam, // True nếu làm đề này lần đầu
                TOEICMessage = currentTOEICMessage, // Message chúc mừng bằng tiếng Anh
                TotalAccumulatedScore = totalAccumulatedScore,
                
                // Metadata for frontend
                ShowTOEICInfo = false, // Không hiển thị TOEIC info
                DisplayLanguage = "en" // Hiển thị toàn bộ bằng tiếng Anh
            };
        }

        /// <summary>
        /// Quy đổi số câu đúng từ hệ thống 61 câu sang 100 câu, sau đó tra bảng TOEIC
        /// Logic: (correctAnswers / totalQuestions) * 100 → làm tròn → tra bảng
        /// </summary>
        /// <param name="correctAnswers">Số câu đúng (0-61)</param>
        /// <param name="totalQuestions">Tổng số câu (61)</param>
        /// <param name="scoreTable">Bảng điểm TOEIC (Listening hoặc Reading)</param>
        /// <returns>Điểm TOEIC (5-495)</returns>
        private int ConvertTo100ScaleAndLookup(double correctAnswers, int totalQuestions, Dictionary<int, int> scoreTable)
        {
            if (correctAnswers <= 0) return scoreTable[0]; // 0 câu đúng → điểm tối thiểu
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

        private async Task<int> GetEstimatedTOEICScore(int userId)
        {
            // LẤY TẤT CẢ CÁC LẦN THI ĐÃ HOÀN THÀNH
            var allAttempts = await _context.ExamAttempts
                .Include(ea => ea.ExamPart) // Load ExamPart để lấy PartCode
                .Where(ea => ea.UserID == userId 
                    && ea.Status == "Completed"
                    && ea.ExamPartId != null
                    && ea.ExamPart != null)
                .OrderBy(ea => ea.EndTime)
                .ToListAsync();

            if (!allAttempts.Any()) return 0;

            // XÁC ĐỊNH SKILL (Listening/Reading) DỰA VÀO PartCode
            var attemptsWithSkill = allAttempts
                .Where(ea => ea.ExamPart.PartCode.StartsWith("LISTENING") || ea.ExamPart.PartCode.StartsWith("READING"))
                .Select(ea => new {
                    ea.ExamID,
                    ea.ExamPartId,
                    ea.Score, // Score = số câu đúng (khi ScoreWeight=1)
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
                    // CỘNG SỐ CÂU ĐÚNG: Score = số câu đúng (khi ScoreWeight=1)
                    // VD: Part1(6) + Part2(3) + Part3(4) + Part4(3) = 16 câu
                    TotalCorrectAnswers = g.Sum(x => (double)(x.Score ?? 0)),
                    CompletedParts = g.Count(),
                    FirstAttemptDate = g.Min(x => x.EndTime)
                })
                .OrderByDescending(x => x.FirstAttemptDate)
                .Take(10) // Lấy 10 exams gần nhất
                .ToList();

            // TÁCH LISTENING VÀ READING
            var listeningExams = groupedByExamAndSkill.Where(x => x.Skill == "Listening").ToList();
            var readingExams = groupedByExamAndSkill.Where(x => x.Skill == "Reading").ToList();

            // TÍNH SỐ CÂU ĐÚNG TRUNG BÌNH CHO MỖI SKILL
            var avgCorrectListening = listeningExams.Any()
                ? listeningExams.Average(x => x.TotalCorrectAnswers)
                : 0;

            var avgCorrectReading = readingExams.Any()
                ? readingExams.Average(x => x.TotalCorrectAnswers)
                : 0;

            // QUY ĐỔI SANG ĐIỂM TOEIC THEO BẢNG CHUẨN (61 câu → 100 câu → TOEIC score)
            var estimatedListening = ConvertTo100ScaleAndLookup(avgCorrectListening, 61, ListeningScoreTable);
            var estimatedReading = ConvertTo100ScaleAndLookup(avgCorrectReading, 61, ReadingScoreTable);

            // TỔNG TOEIC = Listening + Reading (0-990)
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
                ["Beginner"] = $"🎯 Bạn đang ở mức {score} điểm - Chỉ đáp ứng yêu cầu căn bản. Hãy tiếp tục luyện tập!",
                ["Elementary"] = $"📚 Bạn đạt {score} điểm - Trình độ hạn chế. Cố gắng lên để đạt 405+ điểm!",
                ["Intermediate"] = $"⭐ Bạn đạt {score} điểm - Giao tiếp đơn giản theo tình huống quen thuộc. Hướng tới 605+ điểm!",
                ["Upper-Intermediate"] = $"🎓 Bạn đạt {score} điểm - Giao tiếp thông thường, hạn chế công việc. Tiến tới 785+ điểm!",
                ["Advanced"] = $"🏆 Xuất sắc! {score} điểm - Đáp ứng hầu hết yêu cầu công việc. Cố gắng đạt 905+!",
                ["Proficient"] = $"💎 Đỉnh cao! {score} điểm - Giao tiếp trôi chảy, tự nhiên mọi hoàn cảnh!"
            };

            return messages.TryGetValue(level, out var message) ? message : $"Trình độ TOEIC ước tính của bạn: {score} điểm";
        }

        private string GetCompletionMessage(int scoreEarned, bool isFirstAttempt)
        {
            if (!isFirstAttempt)
            {
                return "🎯 Great job completing this part! Keep practicing to improve your skills.";
            }

            if (scoreEarned >= 400)
            {
                return "🌟 Excellent work! You've earned a fantastic score on this part!";
            }
            else if (scoreEarned >= 300)
            {
                return "✨ Well done! Great effort on completing this part!";
            }
            else if (scoreEarned >= 200)
            {
                return "👍 Good job! You're making steady progress!";
            }
            else if (scoreEarned >= 100)
            {
                return "💪 Nice try! Keep practicing and you'll improve!";
            }
            else
            {
                return "🎯 Part completed! Every practice session helps you grow!";
            }
        }
    }
}
