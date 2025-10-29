using DataLayer.DTOs.Leaderboard;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Leaderboard
{
    public class ScoringRuleService : IScoringRuleService
    {
        private readonly LuminaSystemContext _context;
        private readonly IScoringMilestoneService _milestoneService;

        public ScoringRuleService(LuminaSystemContext context, IScoringMilestoneService milestoneService)
        {
            _context = context;
            _milestoneService = milestoneService;
        }

        public async Task<List<ScoringRuleDTO>> GetAllAsync()
        {
            // Return default scoring rules since we're not using ScoringRule model
            return new List<ScoringRuleDTO>
            {
                new ScoringRuleDTO
                {
                    RuleId = 1,
                    RuleName = "TOEIC Progressive Scoring",
                    Description = "Progressive scoring system: higher total score = lower points per correct answer",
                    BaseScore = 15,
                    DifficultyMultiplier = 1.0f,
                    TimeBonusMultiplier = 0.3f,
                    AccuracyMultiplier = 1.5f,
                    MaxTimeSeconds = 3600,
                    IsActive = true,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = null
                }
            };
        }

        public async Task<ScoringRuleDTO?> GetByIdAsync(int ruleId)
        {
            var rules = await GetAllAsync();
            return rules.FirstOrDefault(r => r.RuleId == ruleId);
        }

        public async Task<int> CreateAsync(CreateScoringRuleDTO dto)
        {
            // Since we're not using ScoringRule model, just return a mock ID
            return 1;
        }

        public async Task<bool> UpdateAsync(int ruleId, UpdateScoringRuleDTO dto)
        {
            // Since we're not using ScoringRule model, just return true
            return true;
        }

        public async Task<bool> DeleteAsync(int ruleId)
        {
            // Since we're not using ScoringRule model, just return true
            return true;
        }

        public async Task<int> CalculateSessionScoreAsync(int userId, int totalQuestions, int correctAnswers, int timeSpentSeconds, string difficulty = "medium")
        {
            var wrongAnswers = totalQuestions - correctAnswers;
            var accuracy = totalQuestions > 0 ? (double)correctAnswers / totalQuestions : 0;

            // Get user's current TOEIC equivalent score (0-990 scale) from UserAnswers
            var userToeicScore = await GetUserToeicEquivalentScoreAsync(userId);

            // Progressive scoring based on TOEIC score levels (0-990)
            var pointsPerCorrect = CalculateProgressivePointsPerCorrect(userToeicScore);
            var baseScore = (int)(pointsPerCorrect * correctAnswers);

            // Difficulty multiplier
            var difficultyMultiplier = GetDifficultyMultiplier(difficulty);
            var difficultyBonus = (int)(baseScore * (difficultyMultiplier - 1));

            // Time bonus (faster completion = higher bonus, but decreases with higher TOEIC scores)
            var timeBonusMultiplier = Math.Max(0.1, 0.3 * (1 - userToeicScore / 990.0)); // Decreases as TOEIC score increases
            var timeBonus = timeSpentSeconds < 1800 ? (int)(baseScore * timeBonusMultiplier) : 0;

            // Accuracy bonus (higher accuracy = more bonus, but decreases with higher TOEIC scores)
            var accuracyBonusMultiplier = Math.Max(0.2, 1.5 * (1 - userToeicScore / 990.0)); // Decreases as TOEIC score increases
            var accuracyBonus = accuracy >= 0.8 ? (int)(baseScore * accuracyBonusMultiplier * (accuracy - 0.8) * 5) : 0;

            var finalScore = baseScore + difficultyBonus + timeBonus + accuracyBonus;

            // Save session score as UserAnswer with calculated score
            var attemptId = await GetOrCreateSessionAttemptAsync(userId, totalQuestions, timeSpentSeconds, accuracy);
            
            // Get or create dummy question for session scoring
            var dummyQuestion = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionType == "SessionScoring");
            if (dummyQuestion == null)
            {
                // Get dummy exam first
                var dummyExam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamType == "SessionScoring");
                if (dummyExam != null)
                {
                    // Create dummy exam part
                    var dummyPart = new ExamPart
                    {
                        ExamId = dummyExam.ExamId,
                        PartCode = "SESSION_SCORING",
                        Title = "Session Scoring Part",
                        OrderIndex = 1,
                        MaxQuestions = 1
                    };
                    _context.ExamParts.Add(dummyPart);
                    await _context.SaveChangesAsync();

                    // Create dummy question
                    dummyQuestion = new Question
                    {
                        PartId = dummyPart.PartId,
                        QuestionType = "SessionScoring",
                        StemText = "Session scoring question",
                        ScoreWeight = 1,
                        Time = 0,
                        QuestionNumber = 1
                    };
                    _context.Questions.Add(dummyQuestion);
                    await _context.SaveChangesAsync();
                }
            }

            var userAnswer = new UserAnswer
            {
                AttemptId = attemptId,
                QuestionId = dummyQuestion?.QuestionId ?? 0, // Use dummy question ID or 0
                AnswerContent = correctAnswers.ToString(), // Store correct answers count
                IsCorrect = true, // Mark as correct for session scoring
                Score = finalScore
            };

            _context.UserAnswers.Add(userAnswer);
            await _context.SaveChangesAsync();

            // Check for milestone notifications based on TOEIC equivalent score
            var newToeicScore = await GetUserToeicEquivalentScoreAsync(userId);
            await _milestoneService.CheckAndCreateMilestoneNotificationsAsync(userId, newToeicScore);

            return finalScore;
        }

        public async Task<List<PracticeSessionScoreDTO>> GetUserSessionScoresAsync(int userId, int page = 1, int pageSize = 10)
        {
            // Get session scores from UserAnswers where QuestionType = "SessionScoring"
            var scores = await _context.UserAnswers
                .Where(ua => ua.Attempt.UserId == userId && ua.Question.QuestionType == "SessionScoring")
                .Include(ua => ua.Attempt)
                .ThenInclude(a => a.User)
                .Include(ua => ua.Question)
                .OrderByDescending(ua => ua.Attempt.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return scores.Select(MapToSessionScoreDto).ToList();
        }

        public async Task InitializeDefaultScoringRulesAsync()
        {
            // Since we're not using ScoringRule model, this method does nothing
            await Task.CompletedTask;
        }

        private async Task<int> GetUserToeicEquivalentScoreAsync(int userId)
        {
            // Calculate TOEIC equivalent score based on recent UserAnswers performance
            var recentAnswers = await _context.UserAnswers
                .Where(ua => ua.Attempt.UserId == userId && 
                           ua.Question.QuestionType != "SessionScoring" && 
                           ua.Question.QuestionType != "NotificationScoring") // Exclude session scores and notifications
                .Include(ua => ua.Attempt)
                .Include(ua => ua.Question)
                .OrderByDescending(ua => ua.Attempt.StartTime)
                .Take(50) // Last 50 answers
                .ToListAsync();

            if (!recentAnswers.Any()) return 0;

            // Calculate average accuracy from recent answers
            var avgAccuracy = recentAnswers.Average(ua => ua.IsCorrect == true ? 1.0 : 0.0);
            
            // Convert accuracy to TOEIC equivalent score (0-990)
            var toeicScore = (int)(avgAccuracy * 990);
            
            return Math.Min(990, Math.Max(0, toeicScore));
        }

        private async Task<int> GetOrCreateSessionAttemptAsync(int userId, int totalQuestions, int timeSpentSeconds, double accuracy)
        {
            // Get or create dummy exam for session scoring
            var dummyExam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamType == "SessionScoring");
            if (dummyExam == null)
            {
                dummyExam = new DataLayer.Models.Exam
                {
                    ExamType = "SessionScoring",
                    Name = "Session Scoring Dummy Exam",
                    Description = "Dummy exam for session-based scoring",
                    IsActive = true,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    ExamSetKey = "SESSION_SCORING"
                };
                _context.Exams.Add(dummyExam);
                await _context.SaveChangesAsync();
            }

            // Create a new exam attempt for session scoring
            var attempt = new ExamAttempt
            {
                UserId = userId,
                ExamId = dummyExam.ExamId,
                StartTime = DateTime.UtcNow.AddSeconds(-timeSpentSeconds),
                EndTime = DateTime.UtcNow,
                Score = 0, // Will be calculated later
                Status = "Completed"
            };

            _context.ExamAttempts.Add(attempt);
            await _context.SaveChangesAsync();
            return attempt.AttemptId;
        }

        private double CalculateProgressivePointsPerCorrect(int userToeicScore)
        {
            // Progressive scoring based on TOEIC score levels (0-990):
            // 0-200: 15 points per correct answer (Beginner)
            // 201-400: 12 points per correct answer (Elementary)
            // 401-600: 8 points per correct answer (Intermediate)
            // 601-750: 5 points per correct answer (Upper-Intermediate)
            // 751-850: 3 points per correct answer (Advanced)
            // 851-990: 2 points per correct answer (Proficient)
            
            if (userToeicScore <= 200)
                return 15.0;
            else if (userToeicScore <= 400)
                return 12.0;
            else if (userToeicScore <= 600)
                return 8.0;
            else if (userToeicScore <= 750)
                return 5.0;
            else if (userToeicScore <= 850)
                return 3.0;
            else
                return 2.0;
        }

        private int CalculateFallbackScore(int totalQuestions, int correctAnswers, int timeSpentSeconds, string difficulty)
        {
            // Fallback uses progressive scoring even without rules
            var pointsPerCorrect = CalculateProgressivePointsPerCorrect(0); // Start with highest points (Beginner level)
            var baseScore = (int)(pointsPerCorrect * correctAnswers);
            var difficultyMultiplier = GetDifficultyMultiplier(difficulty);
            var timeBonus = timeSpentSeconds < 1800 ? (int)(baseScore * 0.2) : 0; // 20% bonus if under 30 minutes
            var accuracyBonus = correctAnswers >= totalQuestions * 0.8 ? (int)(baseScore * 0.3) : 0; // 30% bonus for 80%+ accuracy

            return (int)(baseScore * difficultyMultiplier) + timeBonus + accuracyBonus;
        }

        private double GetDifficultyMultiplier(string difficulty)
        {
            return difficulty.ToLower() switch
            {
                "easy" => 0.8,
                "medium" => 1.0,
                "hard" => 1.3,
                "expert" => 1.6,
                _ => 1.0
            };
        }

        private static PracticeSessionScoreDTO MapToSessionScoreDto(UserAnswer userAnswer)
        {
            var correctAnswers = int.TryParse(userAnswer.AnswerContent, out var correct) ? correct : 0;
            var totalQuestions = correctAnswers; // Simplified for session scoring
            
            return new PracticeSessionScoreDTO
            {
                SessionId = userAnswer.UserAnswerId,
                UserId = userAnswer.Attempt.UserId,
                TotalQuestions = totalQuestions,
                CorrectAnswers = correctAnswers,
                WrongAnswers = 0, // Simplified
                Accuracy = userAnswer.IsCorrect == true ? 1.0 : 0.0,
                TimeSpentSeconds = (int)((userAnswer.Attempt?.EndTime - userAnswer.Attempt?.StartTime)?.TotalSeconds ?? 0),
                BaseScore = (int)(userAnswer.Score ?? 0),
                DifficultyBonus = 0, // Simplified
                TimeBonus = 0, // Simplified
                AccuracyBonus = 0, // Simplified
                FinalScore = (int)(userAnswer.Score ?? 0),
                CompletedAt = userAnswer.Attempt?.EndTime ?? DateTime.UtcNow,
                UserFullName = userAnswer.Attempt?.User?.FullName ?? "Unknown"
            };
        }
    }
}