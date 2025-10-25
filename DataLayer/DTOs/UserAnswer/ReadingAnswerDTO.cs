using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.UserAnswer
{
    public class ReadingAnswerDTO
    {
        public int AttemptID { get; set; }

        public int QuestionId { get; set; }

        public int? SelectedOptionId { get; set; }

        public int? Score { get; set; }

        public bool IsCorrect { get; set; }
    }
}
