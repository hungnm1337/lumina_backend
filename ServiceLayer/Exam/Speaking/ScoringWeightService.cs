using DataLayer.DTOs.Exam.Speaking;
using Microsoft.Extensions.Logging;

namespace ServiceLayer.Exam.Speaking
{
    /// <summary>
    /// Implementation of scoring weight service based on TOEIC Speaking rubrics.
    ///
    /// Scoring Architecture:
    /// - Part 1 (Q1-2): Read Aloud - Focus on Pronunciation + Intonation (Rubric 3.1)
    /// - Part 2 (Q3-4): Describe Picture - Add Grammar + Vocabulary + Cohesion (Rubric 3.2)
    /// - Part 3 (Q5-7): Respond to Questions - Add Relevance + Completeness (Rubric 3.3)
    /// - Part 4 (Q8-10): Info + Paraphrasing - Synthesize information (Rubric 3.4)
    /// - Part 5 (Q11): Express Opinion - All criteria + Argumentation (Rubric 3.5, 0-5 scale)
    /// </summary>
    public class ScoringWeightService : IScoringWeightService
    {
        private readonly ILogger<ScoringWeightService> _logger;

        public ScoringWeightService(ILogger<ScoringWeightService> logger)
        {
            _logger = logger;
        }

        public ScoringWeights GetWeightsForPart(string partCode)
        {
            var weights = partCode?.ToUpper() switch
            {
                // Part 1: Read Aloud - ONLY Pronunciation + Intonation (Rubric 3.1)
                "SPEAKING_PART_1" => new ScoringWeights
                {
                    PronunciationWeight = 0.50f,  // Pronunciation is PRIMARY (AccuracyScore)
                    FluencyWeight = 0.25f,        // Fluency is second priority
                    AccuracyWeight = 0.15f,       // CompletenessScore (read all words)
                    GrammarWeight = 0.05f,        // Grammar NOT important (reading script)
                    VocabularyWeight = 0.05f,     // Vocabulary NOT important
                    ContentWeight = 0.00f,        // Content NOT applicable
                    ScaleFactor = 1.0f
                },

                // Part 2: Describe Picture - Add Grammar + Vocab + Cohesion (Rubric 3.2)
                "SPEAKING_PART_2" => new ScoringWeights
                {
                    GrammarWeight = 0.25f,        // Grammar becomes important
                    VocabularyWeight = 0.25f,     // Vocabulary for accurate description
                    ContentWeight = 0.20f,        // Cohesion + Relevance (describe correctly)
                    FluencyWeight = 0.15f,        // Fluency still important
                    PronunciationWeight = 0.10f,  // Pronunciation decreases
                    AccuracyWeight = 0.05f,       // Accuracy less important
                    ScaleFactor = 1.0f
                },

                // Part 3: Respond to Questions - Add Relevance + Completeness (Rubric 3.3)
                "SPEAKING_PART_3" => new ScoringWeights
                {
                    ContentWeight = 0.30f,        // Relevance + Completeness highest
                    FluencyWeight = 0.25f,        // Spontaneous reaction needs fluency
                    GrammarWeight = 0.20f,        // Grammar still important
                    VocabularyWeight = 0.15f,     // Vocabulary supports
                    PronunciationWeight = 0.10f,  // Pronunciation decreases
                    AccuracyWeight = 0.00f,       // Accuracy not applicable
                    ScaleFactor = 1.0f
                },

                // Part 4: Info + Paraphrasing - Synthesize information (Rubric 3.4)
                "SPEAKING_PART_4" => new ScoringWeights
                {
                    ContentWeight = 0.30f,        // Paraphrasing + Accuracy of info
                    GrammarWeight = 0.25f,        // Grammar high for paraphrasing
                    VocabularyWeight = 0.20f,     // Vocabulary to paraphrase
                    FluencyWeight = 0.15f,        // Fluency
                    PronunciationWeight = 0.10f,  // Pronunciation
                    AccuracyWeight = 0.00f,       // Accuracy not applicable
                    ScaleFactor = 1.0f
                },

                // Part 5: Express Opinion - ALL criteria (Rubric 3.5, 0-5 scale)
                "SPEAKING_PART_5" => new ScoringWeights
                {
                    GrammarWeight = 0.30f,        // "Sustained discourse" needs good grammar
                    VocabularyWeight = 0.25f,     // "Accurate and precise vocabulary"
                    ContentWeight = 0.20f,        // Argumentation depth + Coherence
                    FluencyWeight = 0.15f,        // Connected speech
                    PronunciationWeight = 0.10f,  // Intelligibility
                    AccuracyWeight = 0.00f,       // Not applicable
                    ScaleFactor = 1.67f           // Scale to 0-5 instead of 0-3 (67% higher)
                },

                // Default: Balanced weights
                _ => new ScoringWeights
                {
                    GrammarWeight = 0.25f,
                    VocabularyWeight = 0.20f,
                    ContentWeight = 0.20f,
                    FluencyWeight = 0.20f,
                    PronunciationWeight = 0.10f,
                    AccuracyWeight = 0.05f,
                    ScaleFactor = 1.0f
                }
            };

            // Validate weights
            if (!weights.IsValid())
            {
                _logger.LogWarning(
                    "Scoring weights for {PartCode} do not sum to 1.0 (sum={Sum}). Using anyway.",
                    partCode, weights.TotalWeight);
            }

            _logger.LogDebug(
                "Loaded weights for {PartCode}: P={P:P0}, A={A:P0}, F={F:P0}, G={G:P0}, V={V:P0}, C={C:P0}, Scale={Scale:F2}",
                partCode,
                weights.PronunciationWeight,
                weights.AccuracyWeight,
                weights.FluencyWeight,
                weights.GrammarWeight,
                weights.VocabularyWeight,
                weights.ContentWeight,
                weights.ScaleFactor);

            return weights;
        }

        public float CalculateOverallScore(
            ScoringWeights weights,
            double pronunciationScore,
            double accuracyScore,
            double fluencyScore,
            double grammarScore,
            double vocabularyScore,
            double contentScore)
        {
            // Round component scores to 1 decimal place to avoid floating-point errors
            double totalScore =
                Math.Round(pronunciationScore, 1) * weights.PronunciationWeight +
                Math.Round(accuracyScore, 1) * weights.AccuracyWeight +
                Math.Round(fluencyScore, 1) * weights.FluencyWeight +
                Math.Round(grammarScore, 1) * weights.GrammarWeight +
                Math.Round(vocabularyScore, 1) * weights.VocabularyWeight +
                Math.Round(contentScore, 1) * weights.ContentWeight;

            // Normalize by total weight (should be 1.0, but handle edge cases)
            double totalWeight = weights.TotalWeight;
            if (totalWeight > 0)
            {
                totalScore /= totalWeight;
            }

            // Apply scale factor (for Part 5: 1.67x to scale 0-3 → 0-5)
            if (weights.ScaleFactor != 1.0f)
            {
                double originalScore = totalScore;
                totalScore *= weights.ScaleFactor;
                totalScore = Math.Min(100, totalScore); // Cap at 100

                _logger.LogDebug(
                    "Applied scale factor {ScaleFactor:F2}: {Original:F1} → {Scaled:F1}",
                    weights.ScaleFactor, originalScore, totalScore);
            }

            // Round final result to 1 decimal place
            return (float)Math.Round(totalScore, 1);
        }
    }
}
