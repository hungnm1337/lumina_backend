using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.UserAnswer
{
    public class WritingAnswerDTO
    {
        public int UserAnswerWritingId { get; set; }

        public int AttemptID { get; set; }

        public int QuestionId { get; set; }

        public string UserAnswerContent { get; set; }

        public string FeedbackFromAI { get; set; }
    }
}
