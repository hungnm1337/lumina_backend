using DataLayer.DTOs.Exam.Speaking;
using Microsoft.Extensions.Logging;

namespace ServiceLayer.Exam.Speaking
{
    
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
                // Part 1: Read Aloud - Focus on DELIVERY (70%)
                "SPEAKING_PART_1" => new ScoringWeights
                {
                    PronunciationWeight = 0.40f,  // Delivery
                    FluencyWeight = 0.20f,        // Delivery
                    AccuracyWeight = 0.10f,       // Delivery
                    GrammarWeight = 0.15f,        // Language Use
                    VocabularyWeight = 0.05f,     // Language Use
                    ContentWeight = 0.10f,        // Task Appropriateness
                    ScaleFactor = 1.0f
                },

                // Part 2: Describe Picture - Balanced (Delivery 35%, Language 35%, Task 30%)
                "SPEAKING_PART_2" => new ScoringWeights
                {
                    PronunciationWeight = 0.15f,  // Delivery
                    FluencyWeight = 0.15f,        // Delivery
                    AccuracyWeight = 0.05f,       // Delivery
                    GrammarWeight = 0.20f,        // Language Use
                    VocabularyWeight = 0.15f,     // Language Use
                    ContentWeight = 0.30f,        // Task Appropriateness
                    ScaleFactor = 1.0f
                },

                // Part 3: Respond to Questions - Focus on TASK (40%) + Language (35%)
                "SPEAKING_PART_3" => new ScoringWeights
                {
                    ContentWeight = 0.40f,        // Task Appropriateness
                    GrammarWeight = 0.20f,        // Language Use
                    VocabularyWeight = 0.15f,     // Language Use
                    FluencyWeight = 0.15f,        // Delivery
                    PronunciationWeight = 0.10f,  // Delivery
                    AccuracyWeight = 0.00f,
                    ScaleFactor = 1.0f
                },

                // Part 4: Respond Using Information - Focus on TASK (45%) + Language (35%)
                "SPEAKING_PART_4" => new ScoringWeights
                {
                    ContentWeight = 0.45f,        // Task Appropriateness
                    GrammarWeight = 0.20f,        // Language Use
                    VocabularyWeight = 0.15f,     // Language Use
                    FluencyWeight = 0.10f,        // Delivery
                    PronunciationWeight = 0.10f,  // Delivery
                    AccuracyWeight = 0.00f,
                    ScaleFactor = 1.0f
                },

                // Part 5: Express Opinion - Balanced (Language 40%, Task 35%, Delivery 25%)
                "SPEAKING_PART_5" => new ScoringWeights
                {
                    GrammarWeight = 0.25f,        // Language Use
                    VocabularyWeight = 0.15f,     // Language Use
                    ContentWeight = 0.35f,        // Task Appropriateness
                    FluencyWeight = 0.15f,        // Delivery
                    PronunciationWeight = 0.10f,  // Delivery
                    AccuracyWeight = 0.00f,
                    ScaleFactor = 1.67f
                },

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
            double totalScore =
                Math.Round(pronunciationScore, 1) * weights.PronunciationWeight +
                Math.Round(accuracyScore, 1) * weights.AccuracyWeight +
                Math.Round(fluencyScore, 1) * weights.FluencyWeight +
                Math.Round(grammarScore, 1) * weights.GrammarWeight +
                Math.Round(vocabularyScore, 1) * weights.VocabularyWeight +
                Math.Round(contentScore, 1) * weights.ContentWeight;

            double totalWeight = weights.TotalWeight;
            if (totalWeight > 0)
            {
                totalScore /= totalWeight;
            }

            if (weights.ScaleFactor != 1.0f)
            {
                double originalScore = totalScore;
                totalScore *= weights.ScaleFactor;
                totalScore = Math.Min(100, totalScore);

                _logger.LogDebug(
                    "Applied scale factor {ScaleFactor:F2}: {Original:F1} → {Scaled:F1}",
                    weights.ScaleFactor, originalScore, totalScore);
            }

            return (float)Math.Round(totalScore, 1);
        }
    }
}