using DataLayer.DTOs.Leaderboard;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Leaderboard
{
    public class ScoringMilestoneService : IScoringMilestoneService
    {
        private readonly LuminaSystemContext _context;

        public ScoringMilestoneService(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<List<ScoringMilestoneDTO>> GetAllAsync()
        {
            // Use Leaderboard as milestones with specific criteria
            var milestones = await _context.Leaderboards
                .Where(l => l.IsActive && l.SeasonName != null && l.SeasonName.Contains("Milestone"))
                .OrderBy(l => l.SeasonNumber)
                .ToListAsync();

            return milestones.Select<DataLayer.Models.Leaderboard, ScoringMilestoneDTO>(MapToDto).ToList();
        }

        public async Task<ScoringMilestoneDTO?> GetByIdAsync(int milestoneId)
        {
            var milestone = await _context.Leaderboards
                .FirstOrDefaultAsync(l => l.LeaderboardId == milestoneId && l.SeasonName != null && l.SeasonName.Contains("Milestone"));

            return milestone == null ? null : MapToDto(milestone);
        }

        public async Task<int> CreateAsync(CreateScoringMilestoneDTO dto)
        {
            await ValidateMilestoneAsync(dto.MinScore, dto.MaxScore, null);

            var entity = new DataLayer.Models.Leaderboard
            {
                SeasonName = $"Milestone: {dto.Name}",
                SeasonNumber = dto.MinScore, // Use MinScore as SeasonNumber for sorting
                StartDate = DateTime.UtcNow.AddDays(-dto.MinScore), // Use MinScore for date calculation
                EndDate = DateTime.UtcNow.AddDays(-dto.MaxScore), // Use MaxScore for date calculation
                IsActive = dto.IsActive,
                CreateAt = DateTime.UtcNow,
                UpdateAt = null
            };

            _context.Leaderboards.Add(entity);
            await _context.SaveChangesAsync();
            return entity.LeaderboardId;
        }

        public async Task<bool> UpdateAsync(int milestoneId, UpdateScoringMilestoneDTO dto)
        {
            await ValidateMilestoneAsync(dto.MinScore, dto.MaxScore, milestoneId);

            var existing = await _context.Leaderboards
                .FirstOrDefaultAsync(l => l.LeaderboardId == milestoneId);

            if (existing == null) return false;

            existing.SeasonName = $"Milestone: {dto.Name}";
            existing.SeasonNumber = dto.MinScore;
            existing.StartDate = DateTime.UtcNow.AddDays(-dto.MinScore);
            existing.EndDate = DateTime.UtcNow.AddDays(-dto.MaxScore);
            existing.IsActive = dto.IsActive;
            existing.UpdateAt = DateTime.UtcNow;

            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int milestoneId)
        {
            var existing = await _context.Leaderboards
                .FirstOrDefaultAsync(l => l.LeaderboardId == milestoneId);

            if (existing == null) return false;

            _context.Leaderboards.Remove(existing);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<List<UserMilestoneNotificationDTO>> GetUserNotificationsAsync(int userId)
        {
            // Use UserAnswers with QuestionType = "NotificationScoring" to store notifications
            var notifications = await _context.UserAnswers
                .Where(ua => ua.Attempt.UserId == userId && ua.Question.QuestionType == "NotificationScoring") // NotificationScoring = notification
                .Include(ua => ua.Attempt)
                .ThenInclude(a => a.User)
                .Include(ua => ua.Question)
                .OrderByDescending(ua => ua.Attempt.StartTime)
                .ToListAsync();

            return notifications.Select(MapToNotificationDto).ToList();
        }

        public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
        {
            // Use UserAnswer with QuestionType = "NotificationScoring" and UserAnswerId = notificationId
            var notification = await _context.UserAnswers
                .Include(ua => ua.Question)
                .FirstOrDefaultAsync(ua => ua.UserAnswerId == notificationId && ua.Question.QuestionType == "NotificationScoring");

            if (notification == null) return false;

            // Mark as read by setting IsCorrect = true (read), false = unread
            notification.IsCorrect = true;
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task CheckAndCreateMilestoneNotificationsAsync(int userId, int currentScore)
        {
            var milestones = await _context.Leaderboards
                .Where(l => l.IsActive && l.SeasonName != null && l.SeasonName.Contains("Milestone"))
                .ToListAsync();

            foreach (var milestone in milestones)
            {
                // Extract score range from milestone (stored in SeasonNumber and calculated from dates)
                var minScore = milestone.SeasonNumber;
                var maxScore = Math.Abs((int)(milestone.EndDate?.DayOfYear ?? 0));
                
                if (currentScore >= minScore && currentScore <= maxScore)
                {
                    // Check if user already has notification for this milestone recently (within 7 days)
                    var recentNotification = await _context.UserAnswers
                        .Where(ua => ua.Attempt.UserId == userId && ua.Question.QuestionType == "NotificationScoring") // NotificationScoring = notification
                        .Where(ua => ua.AnswerContent == milestone.LeaderboardId.ToString()) // Store milestone ID in AnswerContent
                        .Where(ua => ua.Attempt.StartTime >= DateTime.UtcNow.AddDays(-7))
                        .Include(ua => ua.Attempt)
                        .Include(ua => ua.Question)
                        .FirstOrDefaultAsync();

                    if (recentNotification == null)
                    {
                        // Get or create dummy exam for notifications
                        var dummyExam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamType == "NotificationScoring");
                        if (dummyExam == null)
                        {
                            dummyExam = new DataLayer.Models.Exam
                            {
                                ExamType = "NotificationScoring",
                                Name = "Notification Dummy Exam",
                                Description = "Dummy exam for milestone notifications",
                                IsActive = true,
                                CreatedBy = userId,
                                CreatedAt = DateTime.UtcNow,
                                ExamSetKey = "NOTIFICATION_SCORING"
                            };
                            _context.Exams.Add(dummyExam);
                            await _context.SaveChangesAsync();
                        }

                        // Create notification as UserAnswer
                        var attempt = new ExamAttempt
                        {
                            UserId = userId,
                            ExamId = dummyExam.ExamId,
                            StartTime = DateTime.UtcNow,
                            EndTime = DateTime.UtcNow,
                            Score = currentScore,
                            Status = "Completed"
                        };

                        _context.ExamAttempts.Add(attempt);
                        await _context.SaveChangesAsync();

                        // Get or create dummy question for notifications
                        var dummyQuestion = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionType == "NotificationScoring");
                        if (dummyQuestion == null)
                        {
                            // Get dummy exam first
                            var dummyExamForQuestion = await _context.Exams.FirstOrDefaultAsync(e => e.ExamType == "NotificationScoring");
                            if (dummyExamForQuestion != null)
                            {
                                // Create dummy exam part
                                var dummyPart = new ExamPart
                                {
                                    ExamId = dummyExamForQuestion.ExamId,
                                    PartCode = "NOTIFICATION_SCORING",
                                    Title = "Notification Scoring Part",
                                    OrderIndex = 1,
                                    MaxQuestions = 1
                                };
                                _context.ExamParts.Add(dummyPart);
                                await _context.SaveChangesAsync();

                                // Create dummy question
                                dummyQuestion = new Question
                                {
                                    PartId = dummyPart.PartId,
                                    QuestionType = "NotificationScoring",
                                    StemText = "Notification scoring question",
                                    ScoreWeight = 1,
                                    Time = 0,
                                    QuestionNumber = 1
                                };
                                _context.Questions.Add(dummyQuestion);
                                await _context.SaveChangesAsync();
                            }
                        }

                        var notification = new UserAnswer
                        {
                            AttemptId = attempt.AttemptId,
                            QuestionId = dummyQuestion?.QuestionId ?? -1, // Use dummy question ID or -1
                            AnswerContent = milestone.LeaderboardId.ToString(), // Store milestone ID
                            IsCorrect = false, // false = unread, true = read
                            Score = currentScore
                        };

                        _context.UserAnswers.Add(notification);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task InitializeDefaultMilestonesAsync()
        {
            var existingMilestones = await _context.Leaderboards
                .AnyAsync(l => l.SeasonName != null && l.SeasonName.Contains("Milestone"));
            
            if (existingMilestones) return;

            var defaultMilestones = new List<(string name, int minScore, int maxScore, string message, string type)>
            {
                ("Bắt đầu hành trình", 0, 200, "Chào mừng bạn đến với hành trình luyện thi TOEIC! Hãy kiên trì và bạn sẽ đạt được mục tiêu.", "encouragement"),
                ("Đang tiến bộ", 201, 400, "Tuyệt vời! Bạn đang tiến bộ rất tốt. Hãy tiếp tục luyện tập để đạt điểm cao hơn.", "encouragement"),
                ("Trung bình", 401, 600, "Bạn đã đạt mức trung bình! Hãy tập trung vào những phần còn yếu để cải thiện điểm số.", "encouragement"),
                ("Khá tốt", 601, 750, "Xuất sắc! Bạn đã đạt mức khá tốt. Chỉ cần thêm một chút nỗ lực nữa là có thể đi thi rồi!", "achievement"),
                ("Sẵn sàng thi", 751, 850, "Tuyệt vời! Bạn đã sẵn sàng để đi thi TOEIC rồi. Hãy đăng ký thi ngay để đạt được mục tiêu!", "achievement"),
                ("Xuất sắc", 851, 990, "Chúc mừng! Bạn đã đạt điểm xuất sắc. Bạn hoàn toàn có thể đạt điểm cao trong kỳ thi thật!", "achievement")
            };

            foreach (var (name, minScore, maxScore, message, type) in defaultMilestones)
            {
                var milestone = new DataLayer.Models.Leaderboard
                {
                    SeasonName = $"Milestone: {name}",
                    SeasonNumber = minScore,
                    StartDate = DateTime.UtcNow.AddDays(-minScore),
                    EndDate = DateTime.UtcNow.AddDays(-maxScore),
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = null
                };

                _context.Leaderboards.Add(milestone);
            }

            await _context.SaveChangesAsync();
        }

        private async Task ValidateMilestoneAsync(int minScore, int maxScore, int? excludeId)
        {
            if (minScore < 0 || maxScore > 990) 
                throw new ArgumentException("Điểm số phải trong khoảng 0-990");
            
            if (minScore >= maxScore) 
                throw new ArgumentException("Điểm tối thiểu phải nhỏ hơn điểm tối đa");

            // Check for overlapping ranges
            var overlapping = await _context.Leaderboards
                .Where(l => l.IsActive && l.SeasonName != null && l.SeasonName.Contains("Milestone"))
                .Where(l => (!excludeId.HasValue || l.LeaderboardId != excludeId.Value))
                .ToListAsync();

            var hasOverlap = overlapping.Any(l => 
            {
                var lEndDate = l.EndDate?.DayOfYear ?? 0;
                var lMaxScore = Math.Abs((int)lEndDate);
                
                return (minScore >= l.SeasonNumber && minScore <= lMaxScore) ||
                       (maxScore >= l.SeasonNumber && maxScore <= lMaxScore) ||
                       (minScore <= l.SeasonNumber && maxScore >= lMaxScore);
            });

            if (hasOverlap)
                throw new ArgumentException("Khoảng điểm này đã trùng với mốc điểm khác");
        }

        private static string GetMilestoneMessage(int minScore, int maxScore)
        {
            return (minScore, maxScore) switch
            {
                (0, 200) => "Chào mừng bạn đến với hành trình luyện thi TOEIC! Hãy kiên trì và bạn sẽ đạt được mục tiêu.",
                (201, 400) => "Tuyệt vời! Bạn đang tiến bộ rất tốt. Hãy tiếp tục luyện tập để đạt điểm cao hơn.",
                (401, 600) => "Bạn đã đạt mức trung bình! Hãy tập trung vào những phần còn yếu để cải thiện điểm số.",
                (601, 750) => "Xuất sắc! Bạn đã đạt mức khá tốt. Chỉ cần thêm một chút nỗ lực nữa là có thể đi thi rồi!",
                (751, 850) => "Tuyệt vời! Bạn đã sẵn sàng để đi thi TOEIC rồi. Hãy đăng ký thi ngay để đạt được mục tiêu!",
                (851, 990) => "Chúc mừng! Bạn đã đạt điểm xuất sắc. Bạn hoàn toàn có thể đạt điểm cao trong kỳ thi thật!",
                _ => "Chúc mừng bạn đã đạt được mốc điểm mới!"
            };
        }

        private static ScoringMilestoneDTO MapToDto(DataLayer.Models.Leaderboard milestone)
        {
            var minScore = milestone.SeasonNumber;
            var maxScore = Math.Abs((int)(milestone.EndDate?.DayOfYear ?? 0));
            
            return new ScoringMilestoneDTO
            {
                MilestoneId = milestone.LeaderboardId,
                Name = milestone.SeasonName?.Replace("Milestone: ", "") ?? "Unknown",
                MinScore = minScore,
                MaxScore = maxScore,
                Message = GetMilestoneMessage(minScore, maxScore),
                NotificationType = milestone.IsActive ? "achievement" : "encouragement",
                IsActive = milestone.IsActive,
                CreateAt = milestone.CreateAt,
                UpdateAt = milestone.UpdateAt
            };
        }

        private static UserMilestoneNotificationDTO MapToNotificationDto(UserAnswer notification)
        {
            var milestoneId = int.TryParse(notification.AnswerContent, out var id) ? id : 0;
            var milestoneName = GetMilestoneNameById(milestoneId);
            
            return new UserMilestoneNotificationDTO
            {
                NotificationId = notification.UserAnswerId,
                UserId = notification.Attempt.UserId,
                MilestoneId = milestoneId,
                CurrentScore = (int)(notification.Score ?? 0),
                Message = GetMilestoneMessageFromId(milestoneId),
                IsRead = notification.IsCorrect == true, // true = read, false = unread
                CreatedAt = notification.Attempt?.StartTime ?? DateTime.UtcNow,
                UserFullName = notification.Attempt?.User?.FullName ?? "Unknown",
                MilestoneName = milestoneName
            };
        }

        private static string GetMilestoneNameById(int milestoneId)
        {
            return milestoneId switch
            {
                1 => "Bắt đầu hành trình",
                2 => "Đang tiến bộ", 
                3 => "Trung bình",
                4 => "Khá tốt",
                5 => "Sẵn sàng thi",
                6 => "Xuất sắc",
                _ => "Unknown"
            };
        }

        private static string GetMilestoneMessageFromId(int milestoneId)
        {
            return milestoneId switch
            {
                1 => "Chào mừng bạn đến với hành trình luyện thi TOEIC! Hãy kiên trì và bạn sẽ đạt được mục tiêu.",
                2 => "Tuyệt vời! Bạn đang tiến bộ rất tốt. Hãy tiếp tục luyện tập để đạt điểm cao hơn.",
                3 => "Bạn đã đạt mức trung bình! Hãy tập trung vào những phần còn yếu để cải thiện điểm số.",
                4 => "Xuất sắc! Bạn đã đạt mức khá tốt. Chỉ cần thêm một chút nỗ lực nữa là có thể đi thi rồi!",
                5 => "Tuyệt vời! Bạn đã sẵn sàng để đi thi TOEIC rồi. Hãy đăng ký thi ngay để đạt được mục tiêu!",
                6 => "Chúc mừng! Bạn đã đạt điểm xuất sắc. Bạn hoàn toàn có thể đạt điểm cao trong kỳ thi thật!",
                _ => "Chúc mừng bạn đã đạt được mốc điểm mới!"
            };
        }

    }
}