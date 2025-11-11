namespace DataLayer.DTOs.Exam.Speaking
{
    /// <summary>
    /// Represents the scoring weights for different aspects of speaking assessment.
    /// Weights are normalized to sum to 1.0 for each Part.
    /// </summary>
    public class ScoringWeights
    {
        public float PronunciationWeight { get; set; }
        public float AccuracyWeight { get; set; }
        public float FluencyWeight { get; set; }
        public float GrammarWeight { get; set; }
        public float VocabularyWeight { get; set; }
        public float ContentWeight { get; set; }

        /// <summary>
        /// Scale factor for Part 5 (0-5 scale instead of 0-3)
        /// </summary>
        public float ScaleFactor { get; set; } = 1.0f;

        /// <summary>
        /// Validates that weights sum to approximately 1.0
        /// </summary>
        public bool IsValid()
        {
            var sum = PronunciationWeight + AccuracyWeight + FluencyWeight +
                      GrammarWeight + VocabularyWeight + ContentWeight;
            return Math.Abs(sum - 1.0f) < 0.001f;
        }

        /// <summary>
        /// Gets the total weight sum
        /// </summary>
        public float TotalWeight => PronunciationWeight + AccuracyWeight + FluencyWeight +
                                     GrammarWeight + VocabularyWeight + ContentWeight;
    }
}
