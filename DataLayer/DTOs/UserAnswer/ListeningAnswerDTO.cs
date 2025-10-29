using DataLayer.DTOs.Exam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.UserAnswer
{
    public class ListeningAnswerRequestDTO
    {
        public int ExamAttemptId { get; set; }

        public int QuestionId { get; set; }

        public int SelectedOptionId { get; set; }
    }

    public class ListeningAnswerResponseDTO
    {
        public int AttemptID { get; set; }

        public QuestionDTO Question { get; set; }

        public OptionDTO SelectedOption { get; set; }

        public int? Score { get; set; }

        public bool IsCorrect { get; set; }
    }

    public class SubmitAnswerRequestDTO
    {
        public int ExamAttemptId { get; set; }

        public int QuestionId { get; set; }

        public int SelectedOptionId { get; set; }
    }

    public class SubmitAnswerResponseDTO
    {
        public bool Success { get; set; }

        public bool IsCorrect { get; set; }

        public decimal Score { get; set; }

        public string Message { get; set; }
    }

    public class UserAnswerDetailDTO
    {
        public int QuestionId { get; set; }

        public int? SelectedOptionId { get; set; }

        public bool IsCorrect { get; set; }

        public decimal Score { get; set; }

        public string QuestionText { get; set; }

        public string SelectedOptionText { get; set; }

        public string CorrectOptionText { get; set; }

        public string Explanation { get; set; }
    }

    public class SaveProgressResponseDTO
    {
        public bool Success { get; set; }

        public string Message { get; set; }
    }
}
