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

            // Get all user answers for this attempt
            var userAnswers = await _unitOfWork.UserAnswers
                .GetAllAsync(ua => ua.AttemptID == attemptId);

            var totalScore = userAnswers.Sum(ua => ua.Score ?? 0);
            var correctCount = userAnswers.Count(ua => ua.IsCorrect);
            var totalQuestions = userAnswers.Count();

            // Update attempt
            attempt.EndTime = DateTime.UtcNow;
            attempt.Score = totalScore;
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
