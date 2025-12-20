using DataLayer.DTOs.Exam.Speaking;
using DataLayer.DTOs.UserAnswer;
using DataLayer.Models;
using RepositoryLayer.UnitOfWork;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.Reading
{
    

    public class ReadingService : IReadingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReadingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

       
        public async Task<AttemptValidationResult> ValidateAttemptAsync(int attemptId, int userId)
        {
            var attempt = await _unitOfWork.ExamAttemptsGeneric
                .GetAsync(a => a.AttemptID == attemptId);

            if (attempt == null)
            {
                return new AttemptValidationResult
                {
                    IsValid = false,
                    ErrorType = AttemptErrorType.NotFound,
                    ErrorMessage = $"ExamAttempt {attemptId} not found."
                };
            }

            if (attempt.UserID != userId)
            {
                return new AttemptValidationResult
                {
                    IsValid = false,
                    ErrorType = AttemptErrorType.Forbidden,
                    ErrorMessage = "User does not own this attempt."
                };
            }

            return new AttemptValidationResult
            {
                IsValid = true,
                AttemptId = attemptId,
                ErrorType = AttemptErrorType.None
            };
        }

        public async Task<SubmitAnswerResponseDTO> SubmitAnswerAsync(ReadingAnswerRequestDTO request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            if (request.ExamAttemptId <= 0)
                throw new ArgumentException("Exam Attempt ID must be greater than zero.", nameof(request.ExamAttemptId));

            if (request.QuestionId <= 0)
                throw new ArgumentException("Question ID must be greater than zero.", nameof(request.QuestionId));

            if (request.SelectedOptionId <= 0)
                throw new ArgumentException("Selected Option ID must be greater than zero.", nameof(request.SelectedOptionId));

            // 1. Validate ExamAttempt
            var attempt = await _unitOfWork.ExamAttemptsGeneric
                .GetAsync(a => a.AttemptID == request.ExamAttemptId);

            if (attempt == null)
                return new SubmitAnswerResponseDTO
                {
                    Success = false,
                    Message = "Exam attempt not found"
                };

            if (attempt.Status == "Completed")
                return new SubmitAnswerResponseDTO
                {
                    Success = false,
                    Message = "Exam already completed"
                };

            var question = await _unitOfWork.QuestionsGeneric
                .GetAsync(
                    q => q.QuestionId == request.QuestionId,
                    includeProperties: "Options"
                );

            if (question == null)
                return new SubmitAnswerResponseDTO
                {
                    Success = false,
                    Message = "Question not found"
                };

            var selectedOption = question.Options
                .FirstOrDefault(o => o.OptionId == request.SelectedOptionId);

            if (selectedOption == null)
                return new SubmitAnswerResponseDTO
                {
                    Success = false,
                    Message = "Invalid option selected"
                };

            var existingAnswer = await _unitOfWork.UserAnswers
                .GetAsync(ua =>
                    ua.AttemptID == request.ExamAttemptId &&
                    ua.QuestionId == request.QuestionId
                );

            bool isCorrect = selectedOption.IsCorrect ?? false;
            int score = isCorrect ? question.ScoreWeight : 0;

            if (existingAnswer != null)
            {
                existingAnswer.SelectedOptionId = request.SelectedOptionId;
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.Score = score;

                _unitOfWork.UserAnswers.Update(existingAnswer);
            }
            else
            {
                var userAnswer = new UserAnswerMultipleChoice
                {
                    AttemptID = request.ExamAttemptId,
                    QuestionId = request.QuestionId,
                    SelectedOptionId = request.SelectedOptionId,
                    IsCorrect = isCorrect,
                    Score = score
                };

                await _unitOfWork.UserAnswers.AddAsync(userAnswer);
            }

            await _unitOfWork.CompleteAsync();

            return new SubmitAnswerResponseDTO
            {
                Success = true,
                IsCorrect = isCorrect,
                Score = score,
                Message = "Answer submitted successfully"
            };
        }
    }
}
