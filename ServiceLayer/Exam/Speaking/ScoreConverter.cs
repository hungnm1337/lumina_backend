namespace ServiceLayer.Exam.Speaking
{
    /// <summary>
    /// Utility class to convert 100-point scores to TOEIC Speaking raw scores (0-3 or 0-5).
    /// These conversions align with ETS/IIG TOEIC Speaking scoring criteria.
    /// </summary>
    public static class ScoreConverter
    {
        /// <summary>
        /// Convert 100-point score to 0-3 scale (for Parts 1-4, Questions 1-10).
        /// </summary>
        /// <remarks>
        /// ETS Criteria:
        /// - Score 0: No response, not English, or completely unrelated
        /// - Score 1: Limited success, may impede comprehension
        /// - Score 2: Generally successful but with some weaknesses
        /// - Score 3: Full marks - highly intelligible, appropriate response
        /// </remarks>
        public static int ConvertTo0to3Scale(double score100)
        {
            return score100 switch
            {
                <= 16.67 => 0,   // No response / completely unrelated
                <= 50.0 => 1,   // Limited success
                <= 83.0 => 2,   // Generally successful
                _ => 3          // Full marks
            };
        }

        /// <summary>
        /// Convert 100-point score to 0-5 scale (for Part 5, Question 11).
        /// </summary>
        /// <remarks>
        /// ETS Criteria (Express Opinion):
        /// - Score 0: No response, not English, or completely unrelated
        /// - Score 1: Minimal opinion, isolated words/phrases
        /// - Score 2: Limited development, choppy delivery
        /// - Score 3: Adequate response, basic structures
        /// - Score 4: Good response, fairly effective vocabulary
        /// - Score 5: Excellent - sustained, coherent, well-paced
        /// </remarks>
        public static int ConvertTo0to5Scale(double score100)
        {
            return score100 switch
            {
                <= 10.0 => 0,   // No response / completely unrelated
                <= 30.0 => 1,   // Minimal opinion
                <= 50.0 => 2,   // Limited development
                <= 70.0 => 3,   // Adequate response
                <= 90.0 => 4,   // Good response
                _ => 5          // Excellent
            };
        }

        /// <summary>
        /// Get the raw score for a specific part based on 100-point score.
        /// </summary>
        /// <param name="score100">Score on 100-point scale</param>
        /// <param name="partCode">Speaking part code (e.g., "SPEAKING_PART_1")</param>
        /// <returns>Raw score (0-3 for Parts 1-4, 0-5 for Part 5)</returns>
        public static int GetRawScoreForPart(double score100, string partCode)
        {
            // Part 5 (Express Opinion) uses 0-5 scale
            if (partCode?.ToUpper() == "SPEAKING_PART_5")
            {
                return ConvertTo0to5Scale(score100);
            }
            
            // Parts 1-4 use 0-3 scale
            return ConvertTo0to3Scale(score100);
        }

        /// <summary>
        /// Calculate the earned score based on raw score and question weight.
        /// </summary>
        /// <param name="score100">Score on 100-point scale</param>
        /// <param name="partCode">Speaking part code</param>
        /// <param name="questionWeight">Weight/max score for this question (typically 3 or 5)</param>
        /// <returns>Earned score for this question</returns>
        public static decimal CalculateEarnedScore(double score100, string partCode, int questionWeight)
        {
            int rawScore = GetRawScoreForPart(score100, partCode);
            int maxRawScore = partCode?.ToUpper() == "SPEAKING_PART_5" ? 5 : 3;
            
            // Earned = (rawScore / maxRawScore) * questionWeight
            decimal ratio = (decimal)rawScore / maxRawScore;
            return Math.Round(ratio * questionWeight, 2);
        }
    }
}
