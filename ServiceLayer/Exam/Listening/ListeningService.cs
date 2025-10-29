﻿using DataLayer.DTOs.UserAnswer;
using DataLayer.Models;
using RepositoryLayer.UnitOfWork;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.Listening
{
    public class ListeningService : IListeningService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ListeningService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SubmitAnswerResponseDTO> SubmitAnswerAsync(SubmitAnswerRequestDTO request)
        {
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

            // 2. Get Question with Options
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

            // 3. Validate Selected Option
            var selectedOption = question.Options
                .FirstOrDefault(o => o.OptionId == request.SelectedOptionId);

            if (selectedOption == null)
                return new SubmitAnswerResponseDTO
                {
                    Success = false,
                    Message = "Invalid option selected"
                };

            // 4. Check if answer already exists
            var existingAnswer = await _unitOfWork.UserAnswers
                .GetAsync(ua =>
                    ua.AttemptID == request.ExamAttemptId &&
                    ua.QuestionId == request.QuestionId
                );

            bool isCorrect = selectedOption.IsCorrect ?? false;
            int score = isCorrect ? question.ScoreWeight : 0;

            if (existingAnswer != null)
            {
                // Update existing answer
                existingAnswer.SelectedOptionId = request.SelectedOptionId;
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.Score = score;

                _unitOfWork.UserAnswers.Update(existingAnswer);
            }
            else
            {
                // Create new answer
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