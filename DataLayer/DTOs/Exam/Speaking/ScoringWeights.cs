namespace DataLayer.DTOs.Exam.Speaking
{
    /// <summary>
    /// Scoring weights aligned with ETS/IIG TOEIC Speaking criteria.
    /// Includes Content Gate mechanism to prevent high scores for irrelevant responses.
    /// </summary>
    public class ScoringWeights
    {
        public float PronunciationWeight { get; set; }
        public float AccuracyWeight { get; set; }
        public float FluencyWeight { get; set; }
        public float GrammarWeight { get; set; }
        public float VocabularyWeight { get; set; }
        public float ContentWeight { get; set; }

        public float ScaleFactor { get; set; } = 1.0f;

        /// <summary>
        /// Minimum content/accuracy score required. If below this, overall score is capped.
        /// For Part 1 (Read Aloud): Uses AccuracyScore (must read the given text).
        /// For Parts 2-5: Uses ContentScore (must be on topic).
        /// </summary>
        public float ContentGateThreshold { get; set; } = 30f;

        /// <summary>
        /// Maximum overall score allowed when Content Gate fails.
        /// Default 33 = equivalent to 1/3 on the 0-3 scale.
        /// </summary>
        public float MaxScoreIfGateFails { get; set; } = 33f;

        /// <summary>
        /// Whether this part uses AccuracyScore for gating (Part 1) vs ContentScore (Parts 2-5).
        /// </summary>
        public bool UseAccuracyForGate { get; set; } = false;

       
        public bool IsValid()
        {
            var sum = PronunciationWeight + AccuracyWeight + FluencyWeight +
                      GrammarWeight + VocabularyWeight + ContentWeight;
            return Math.Abs(sum - 1.0f) < 0.001f;
        }

        
        public float TotalWeight => PronunciationWeight + AccuracyWeight + FluencyWeight +
                                     GrammarWeight + VocabularyWeight + ContentWeight;
    }
}
