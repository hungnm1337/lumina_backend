using DataLayer.DTOs.Exam.Speaking;

namespace ServiceLayer.Exam.Speaking
{
    /// <summary>
    /// Service for managing scoring weights for different Speaking test parts.
    /// Ensures consistency between scoring and finalization processes.
    /// </summary>
    public interface IScoringWeightService
    {
        /// <summary>
        /// Gets the scoring weights for a specific Speaking part
        /// </summary>
        /// <param name="partCode">The part code (e.g., "SPEAKING_PART_1", "SPEAKING_PART_5")</param>
        /// <returns>ScoringWeights configured for the specified part</returns>
        ScoringWeights GetWeightsForPart(string partCode);

        /// <summary>
        /// Calculates the overall score using the provided weights and component scores
        /// </summary>
        /// <param name="weights">The weights to apply</param>
        /// <param name="pronunciationScore">Pronunciation score (0-100)</param>
        /// <param name="accuracyScore">Accuracy score (0-100)</param>
        /// <param name="fluencyScore">Fluency score (0-100)</param>
        /// <param name="grammarScore">Grammar score (0-100)</param>
        /// <param name="vocabularyScore">Vocabulary score (0-100)</param>
        /// <param name="contentScore">Content score (0-100)</param>
        /// <returns>Overall score (0-100, scaled by ScaleFactor if applicable)</returns>
        float CalculateOverallScore(
            ScoringWeights weights,
            double pronunciationScore,
            double accuracyScore,
            double fluencyScore,
            double grammarScore,
            double vocabularyScore,
            double contentScore);
    }
}
