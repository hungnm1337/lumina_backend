namespace DataLayer.DTOs.Exam.Speaking
{
    /// <summary>
    /// Result DTO for speaking answer submission - wraps the scoring result with status info
    /// </summary>
    public class SpeakingSubmitResultDTO
    {
        /// <summary>
        /// Whether the submission was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Whether this was a duplicate submission (already submitted before)
        /// </summary>
        public bool IsDuplicate { get; set; }

        /// <summary>
        /// The scoring result if successful
        /// </summary>
        public SpeakingScoringResultDTO? Result { get; set; }

        /// <summary>
        /// Error message if submission failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The attempt ID (useful when auto-created)
        /// </summary>
        public int AttemptId { get; set; }
    }

    /// <summary>
    /// Result of attempt validation
    /// </summary>
    public class AttemptValidationResult
    {
        public bool IsValid { get; set; }
        public int AttemptId { get; set; }
        public string? ErrorMessage { get; set; }
        public AttemptErrorType ErrorType { get; set; }
    }

    /// <summary>
    /// Error types for attempt validation
    /// </summary>
    public enum AttemptErrorType
    {
        None,
        NotFound,
        Forbidden,
        InvalidUser
    }
}
