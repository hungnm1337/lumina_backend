namespace DataLayer.DTOs.Exam.Speaking
{
    public class SpeakingScoringResultDTO
    {
        public int QuestionId { get; set; }
        public string Transcript { get; set; }
        public string SavedAudioUrl { get; set; }
        public string AudioUrl { get; set; }
        public double? OverallScore { get; set; } // Sẽ tính ở Task 5

        // Điểm từ Azure
        public double? PronunciationScore { get; set; }
        public double? AccuracyScore { get; set; }
        public double? FluencyScore { get; set; }
        public double? CompletenessScore { get; set; }

        // Điểm từ Python
        public double? GrammarScore { get; set; }
        public double? VocabularyScore { get; set; }
        public double? ContentScore { get; set; }

        public DateTime SubmittedAt { get; set; }
    }
}