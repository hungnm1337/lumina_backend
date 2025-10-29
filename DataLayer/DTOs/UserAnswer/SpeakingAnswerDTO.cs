using DataLayer.DTOs.Exam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.UserAnswer
{
    /// <summary>
    /// Request DTO for submitting a speaking answer
    /// </summary>
    public class SpeakingAnswerRequestDTO
    {
        public int ExamAttemptId { get; set; }

        public int QuestionId { get; set; }

        public string AudioUrl { get; set; }

        public string Transcript { get; set; }
    }

    /// <summary>
    /// Response DTO for speaking answer with detailed scores
    /// </summary>
    public class SpeakingAnswerResponseDTO
    {
        public int UserAnswerSpeakingId { get; set; }

        public int AttemptID { get; set; }

        public QuestionDTO Question { get; set; }

        public string Transcript { get; set; }

        public string AudioUrl { get; set; }

        public decimal? PronunciationScore { get; set; }

        public decimal? AccuracyScore { get; set; }

        public decimal? FluencyScore { get; set; }

        public decimal? CompletenessScore { get; set; }

        public decimal? GrammarScore { get; set; }

        public decimal? VocabularyScore { get; set; }

        public decimal? ContentScore { get; set; }

        public decimal? OverallScore { get; set; }
    }
}
