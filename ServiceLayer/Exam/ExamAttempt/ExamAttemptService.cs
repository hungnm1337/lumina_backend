using DataLayer.DTOs.UserAnswer;
using DataLayer.DTOs.Exam;
using RepositoryLayer.Exam;
using RepositoryLayer.Exam.ExamAttempt;
using RepositoryLayer.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.ExamAttempt
{
    public class ExamAttemptService : IExamAttemptService
    {
        private readonly RepositoryLayer.Exam.ExamAttempt.IExamAttemptRepository _examAttemptRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ExamAttemptService(
            RepositoryLayer.Exam.ExamAttempt.IExamAttemptRepository examAttemptRepository,
            IUnitOfWork unitOfWork)
        {
            _examAttemptRepository = examAttemptRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ExamAttemptRequestDTO> EndAnExam(ExamAttemptRequestDTO model)
        {
            return await _examAttemptRepository.EndAnExam(model);
        }

        public async Task<List<ExamAttemptResponseDTO>> GetAllExamAttempts(int userId)
        {
            return await _examAttemptRepository.GetAllExamAttempts(userId);
        }

        public async Task<ExamAttemptDetailResponseDTO> GetExamAttemptById(int attemptId)
        {
            return await _examAttemptRepository.GetExamAttemptById(attemptId);
        }

        public async Task<ExamAttemptRequestDTO> StartAnExam(ExamAttemptRequestDTO model)
        {
            return await _examAttemptRepository.StartAnExam(model);
        }

        public async Task<ExamAttemptSummaryDTO> FinalizeAttemptAsync(int attemptId)
        {
            var attempt = await _unitOfWork.ExamAttemptsGeneric
                .GetAsync(a => a.AttemptID == attemptId);

            if (attempt == null)
                return new ExamAttemptSummaryDTO { Success = false };

            if (attempt.Status == "Completed")
                return new ExamAttemptSummaryDTO { Success = false };

            // ✅ FIX Bug #13: Get all user answers for this attempt (Reading/Listening/Writing)
            var userAnswers = await _unitOfWork.UserAnswers
                .GetAllAsync(ua => ua.AttemptID == attemptId);

            // ✅ FIX Bug #13: Get Speaking answers separately
            var speakingAnswers = await _unitOfWork.UserAnswersSpeaking
                .GetAllAsync(sa => sa.AttemptID == attemptId);

            // ✅ FIX Bug #13: Calculate scores from both sources
            var regularScore = userAnswers.Sum(ua => ua.Score ?? 0);
            var speakingScore = 0m;

            // ✅ FIX Bug #2: Use OverallScore from DB (calculated by ScoringWeightService)
            // This ensures consistency with SpeakingScoringService scoring logic
            var questionIds = speakingAnswers.Select(sa => sa.QuestionId).ToList();
            var questions = await _unitOfWork.QuestionsGeneric
                .GetAllAsync(q => questionIds.Contains(q.QuestionId));
            var questionDict = questions.ToDictionary(q => q.QuestionId);

            foreach (var sa in speakingAnswers)
            {
                if (questionDict.TryGetValue(sa.QuestionId, out var question) && question.ScoreWeight > 0)
                {
                    // Use OverallScore directly from DB (single source of truth)
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

            // Update attempt
            attempt.EndTime = DateTime.UtcNow;
            attempt.Score = (int)Math.Round(totalScore);
            attempt.Status = "Completed";

            _unitOfWork.ExamAttemptsGeneric.Update(attempt);
            await _unitOfWork.CompleteAsync();

            var duration = attempt.EndTime.Value - attempt.StartTime;

            // Get detailed answers with question info
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

        /// <summary>
        /// DEPRECATED: This method is no longer needed after Bug #2 fix.
        /// OverallScore is now stored in the database and calculated by ScoringWeightService.
        /// Kept for backwards compatibility in case old data exists without OverallScore.
        /// </summary>
        [Obsolete("OverallScore should be read from database. This is a fallback for legacy data.", false)]
        private double CalculateSpeakingOverallScore(DataLayer.Models.UserAnswerSpeaking sa)
        {
            // Fallback: If OverallScore is null (legacy data), use this calculation
            Console.WriteLine($"[DEPRECATED] CalculateSpeakingOverallScore called for QuestionId={sa.QuestionId}. Should use OverallScore from DB.");

            if (sa.OverallScore.HasValue)
            {
                return (double)sa.OverallScore.Value;
            }

            // Legacy calculation - use generic weights as fallback
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
            var attempt = await _unitOfWork.ExamAttemptsGeneric
                .GetAsync(a => a.AttemptID == request.ExamAttemptId);

            if (attempt == null)
                return new DataLayer.DTOs.UserAnswer.SaveProgressResponseDTO 
                { 
                    Success = false, 
                    Message = "Exam attempt not found" 
                };

            // Update status to Paused
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
