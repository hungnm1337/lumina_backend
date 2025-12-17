using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.UserAnswer
{
    public class ExamAttemptRequestDTO
    {
        public int AttemptID { get; set; }

        public int UserID { get; set; }

        public int ExamID { get; set; }

        public int? ExamPartId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public int? Score { get; set; }

        public string Status { get; set; }
        
    }

    public class ExamAttemptResponseDTO
    {
        public int AttemptID { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }

        public string ExamName { get; set; }

        public string ExamPartName { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public int? Score { get; set; }

        public string Status { get; set; }

        public bool IsMocktest { get; set; }
    }

    public class ExamAttemptDetailResponseDTO
    {
        public ExamAttemptResponseDTO? ExamAttemptInfo { get; set; }

        public List<ListeningAnswerResponseDTO>? ListeningAnswers { get; set; }

        public List<SpeakingAnswerResponseDTO>? SpeakingAnswers { get; set; }

        public List<ReadingAnswerResponseDTO>? ReadingAnswers { get; set; }

        public List<WritingAnswerResponseDTO>? WritingAnswers { get; set; }
    }
}
