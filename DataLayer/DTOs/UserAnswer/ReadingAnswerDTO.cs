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
    public class ReadingAnswerRequestDTO
    {
        public int AttemptID { get; set; }

        public int QuestionId { get; set; }

        public int? SelectedOptionId { get; set; }
    }

    public class ReadingAnswerResponseDTO
    {
        public int AttemptID { get; set; }

        public QuestionDTO Question { get; set; }

        public OptionDTO SelectedOption { get; set; }

        public int? Score { get; set; }

        public bool IsCorrect { get; set; }
    }
}
