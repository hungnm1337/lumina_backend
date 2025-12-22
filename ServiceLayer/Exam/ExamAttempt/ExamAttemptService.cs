using DataLayer.DTOs.UserAnswer;
using DataLayer.DTOs.Exam;
using DataLayer.Models;
using RepositoryLayer.Exam;
using RepositoryLayer.Exam.ExamAttempt;
using RepositoryLayer.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceLayer.Streak;
using Microsoft.Extensions.Logging;

namespace ServiceLayer.Exam.ExamAttempt
{
    public class ExamAttemptService : IExamAttemptService
    {
        private readonly IExamAttemptRepository _examAttemptRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStreakService _streakService; 
        private readonly ILogger<ExamAttemptService> _logger; 

        public ExamAttemptService(
            IExamAttemptRepository examAttemptRepository,
            IUnitOfWork unitOfWork,
            IStreakService streakService, 
            ILogger<ExamAttemptService> logger) 
        {
            _examAttemptRepository = examAttemptRepository;
            _unitOfWork = unitOfWork;
            _streakService = streakService; 
            _logger = logger; 
        }

        public async Task<ExamAttemptRequestDTO> EndAnExam(ExamAttemptRequestDTO model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model), "Request DTO cannot be null.");

            if (model.AttemptID < 0)
                throw new ArgumentException("AttemptID must be non-negative.", nameof(model.AttemptID));

            if (model.AttemptID == 0)
                return null;

            if (string.IsNullOrWhiteSpace(model.Status))
                throw new ArgumentException("Status cannot be empty.", nameof(model.Status));

            return await _examAttemptRepository.EndAnExam(model);
        }

        public async Task<List<ExamAttemptResponseDTO>> GetAllExamAttempts(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("User ID must be greater than zero.", nameof(userId));

            return await _examAttemptRepository.GetAllExamAttempts(userId);
        }

        public async Task<ExamAttemptDetailResponseDTO> GetExamAttemptById(int attemptId)
        {
            if (attemptId <= 0)
                throw new ArgumentException("Attempt ID must be greater than zero.", nameof(attemptId));

            return await _examAttemptRepository.GetExamAttemptById(attemptId);
        }

        public async Task<ExamAttemptRequestDTO> StartAnExam(ExamAttemptRequestDTO model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model), "Request DTO cannot be null.");

            if (model.UserID <= 0)
                throw new ArgumentException("User ID must be greater than zero.", nameof(model.UserID));

            if (model.ExamID <= 0)
                throw new ArgumentException("Exam ID must be greater than zero.", nameof(model.ExamID));

            if (string.IsNullOrWhiteSpace(model.Status))
                throw new ArgumentException("Status cannot be empty.", nameof(model.Status));

            return await _examAttemptRepository.StartAnExam(model);
        }

        public async Task<ExamAttemptSummaryDTO> FinalizeAttemptAsync(int attemptId)
        {
            if (attemptId <= 0)
                throw new ArgumentException("Attempt ID must be greater than zero.", nameof(attemptId));

            var attempt = await _unitOfWork.ExamAttemptsGeneric
                .GetAsync(a => a.AttemptID == attemptId);

            if (attempt == null)
                return new ExamAttemptSummaryDTO { Success = false };

            var userAnswers = await _unitOfWork.UserAnswers
                .GetAllAsync(ua => ua.AttemptID == attemptId);

            var speakingAnswers = await _unitOfWork.UserAnswersSpeaking
                .GetAllAsync(sa => sa.AttemptID == attemptId);

            var regularScore = userAnswers.Sum(ua => ua.Score ?? 0);
            var speakingScore = 0m;

            var questionIds = speakingAnswers.Select(sa => sa.QuestionId).ToList();
            List<Question> questions = new List<Question>();
            if (questionIds.Any())
            {
                var questionsResult = await _unitOfWork.QuestionsGeneric
                    .GetAllAsync(q => questionIds.Contains(q.QuestionId));
                if (questionsResult != null)
                {
                    questions = questionsResult;
                }
            }
            var questionDict = questions.ToDictionary(q => q.QuestionId);

            foreach (var sa in speakingAnswers)
            {
                if (questionDict.TryGetValue(sa.QuestionId, out var question) && question.ScoreWeight > 0)
                {
                    var overallScore = sa.OverallScore ?? 0m;
                    var scoreRatio = overallScore / 100m;
                    var earnedScore = question.ScoreWeight * scoreRatio;
                    speakingScore += Math.Round(earnedScore, 2);

                    Console.WriteLine($"[ExamAttempt] QuestionId={sa.QuestionId}, OverallScore={overallScore:F1}, Weight={question.ScoreWeight}, Earned={earnedScore:F2}");
                }
            }

            var totalScore = (decimal)regularScore + speakingScore;
            var correctCount = userAnswers.Count(ua => ua.IsCorrect);
            var totalQuestions = userAnswers.Count() + speakingAnswers.Count();

            Console.WriteLine($"[ExamAttempt] Finalize attemptId={attemptId}: regularScore={regularScore}, speakingScore={speakingScore}, total={totalScore}");

            attempt.EndTime = DateTime.UtcNow;
            attempt.Score = (int)Math.Round(totalScore);
            attempt.Status = "Completed";

            _unitOfWork.ExamAttemptsGeneric.Update(attempt);
            await _unitOfWork.CompleteAsync();

            try
            {
                var userId = attempt.UserID;
                var todayLocal = _streakService.GetTodayGMT7();
                
                var streakResult = await _streakService.UpdateOnValidPracticeAsync(userId, todayLocal);
                
                if (streakResult.Success)
                {
                    _logger.LogInformation(
                        "Streak updated for user {UserId} after exam {AttemptId}. Event: {EventType}, Streak: {Streak}",
                        userId, attemptId, streakResult.EventType, streakResult.Summary?.CurrentStreak ?? 0);

                    if (streakResult.MilestoneReached)
                    {
                        _logger.LogInformation(
                            " User {UserId} reached milestone {Milestone}!",
                            userId, streakResult.MilestoneValue);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to update streak for user {UserId}: {Message}",
                        userId, streakResult.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error updating streak for user {UserId} after exam {AttemptId}",
                    attempt.UserID, attemptId);
            }

            var duration = attempt.EndTime.Value - attempt.StartTime;

            var answerDetails = new List<UserAnswerDetailDTO>();
            foreach (var ua in userAnswers)
            {
                var question = await _unitOfWork.QuestionsGeneric.GetAsync(
                    q => q.QuestionId == ua.QuestionId,
                    includeProperties: "Options"
                );

                if (question != null)
                {
                    var selectedOption = question.Options.FirstOrDefault(o => o.OptionId == ua.SelectedOptionId);
                    var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect == true);

                    answerDetails.Add(new UserAnswerDetailDTO
                    {
                        QuestionId = ua.QuestionId,
                        SelectedOptionId = ua.SelectedOptionId,
                        IsCorrect = ua.IsCorrect,
                        Score = ua.Score ?? 0,
                        QuestionText = question.StemText,
                        SelectedOptionText = selectedOption?.Content ?? "",
                        CorrectOptionText = correctOption?.Content ?? "",
                        Explanation = question.QuestionExplain ?? ""
                    });
                }
            }

            return new ExamAttemptSummaryDTO
            {
                Success = true,
                ExamAttemptId = attemptId,
                TotalScore = totalScore,
                TotalQuestions = totalQuestions,
                CorrectAnswers = correctCount,
                IncorrectAnswers = totalQuestions - correctCount,
                PercentCorrect = totalQuestions > 0 ? (double)correctCount / totalQuestions * 100 : 0,
                StartTime = attempt.StartTime,
                EndTime = attempt.EndTime.Value,
                Duration = duration,
                Answers = answerDetails
            };
        }

        
        [Obsolete("OverallScore should be read from database. This is a fallback for legacy data.", false)]
        private double CalculateSpeakingOverallScore(DataLayer.Models.UserAnswerSpeaking sa)
        {
            Console.WriteLine($"[DEPRECATED] CalculateSpeakingOverallScore called for QuestionId={sa.QuestionId}. Should use OverallScore from DB.");

            if (sa.OverallScore.HasValue)
            {
                return (double)sa.OverallScore.Value;
            }

            var weights = new Dictionary<string, double>
            {
                ["pronunciation"] = 0.20,
                ["accuracy"] = 0.15,
                ["fluency"] = 0.15,
                ["grammar"] = 0.25,
                ["vocabulary"] = 0.15,
                ["content"] = 0.10
            };

            double total = 0;
            double totalWeight = 0;

            if (sa.PronunciationScore.HasValue) { total += (double)sa.PronunciationScore.Value * weights["pronunciation"]; totalWeight += weights["pronunciation"]; }
            if (sa.AccuracyScore.HasValue) { total += (double)sa.AccuracyScore.Value * weights["accuracy"]; totalWeight += weights["accuracy"]; }
            if (sa.FluencyScore.HasValue) { total += (double)sa.FluencyScore.Value * weights["fluency"]; totalWeight += weights["fluency"]; }
            if (sa.GrammarScore.HasValue) { total += (double)sa.GrammarScore.Value * weights["grammar"]; totalWeight += weights["grammar"]; }
            if (sa.VocabularyScore.HasValue) { total += (double)sa.VocabularyScore.Value * weights["vocabulary"]; totalWeight += weights["vocabulary"]; }
            if (sa.ContentScore.HasValue) { total += (double)sa.ContentScore.Value * weights["content"]; totalWeight += weights["content"]; }

            return totalWeight > 0 ? total / totalWeight : 0;
        }

        public async Task<DataLayer.DTOs.UserAnswer.SaveProgressResponseDTO> SaveProgressAsync(SaveProgressRequestDTO request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            if (request.ExamAttemptId <= 0)
                throw new ArgumentException("Exam Attempt ID must be greater than zero.", nameof(request.ExamAttemptId));

            var attempt = await _unitOfWork.ExamAttemptsGeneric
                .GetAsync(a => a.AttemptID == request.ExamAttemptId);

            if (attempt == null)
                return new DataLayer.DTOs.UserAnswer.SaveProgressResponseDTO 
                { 
                    Success = false, 
                    Message = "Exam attempt not found" 
                };

            attempt.Status = "Paused";

            _unitOfWork.ExamAttemptsGeneric.Update(attempt);
            await _unitOfWork.CompleteAsync();

            return new DataLayer.DTOs.UserAnswer.SaveProgressResponseDTO
            {
                Success = true,
                Message = "Progress saved successfully"
            };
        }
    }
}
