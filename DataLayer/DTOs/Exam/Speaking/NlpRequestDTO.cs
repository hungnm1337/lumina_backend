namespace DataLayer.DTOs.Exam.Speaking
{
    public class NlpRequestDTO
    {
        public string Transcript { get; set; }
        public string Sample_answer { get; set; }
        
        /// <summary>
        /// Speaking part code (e.g., "SPEAKING_PART_1" for Read Aloud).
        /// Used to apply part-specific scoring logic in NLP service.
        /// </summary>
        public string Part_code { get; set; }
        public string Question { get; set; }  // NEW: For question-answer relevance detection
        public string Image_url { get; set; }  // NEW: For Part 2 caption integration
    }
}