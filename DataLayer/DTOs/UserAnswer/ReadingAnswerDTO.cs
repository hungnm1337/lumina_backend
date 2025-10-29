using DataLayer.DTOs.Exam;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.UserAnswer
{
    /// <summary>
    /// Request DTO for submitting a reading answer
    /// Should only contain user input, not calculated values
    /// </summary>
    public class ReadingAnswerRequestDTO
    {
        public int ExamAttemptId { get; set; } // Changed from AttemptID for consistency

        public int QuestionId { get; set; }

        public int SelectedOptionId { get; set; } // Changed from nullable for consistency
    }

    /// <summary>
    /// Response DTO for reading answer with detailed information
    /// </summary>
    public class ReadingAnswerResponseDTO
    {
        public int AttemptID { get; set; }

        public QuestionDTO Question { get; set; }

        public OptionDTO SelectedOption { get; set; }

        public int? Score { get; set; }

        public bool IsCorrect { get; set; }
    }
}
