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
                "SPEAKING_PART_1" => new ScoringWeights
                {
                    PronunciationWeight = 0.50f,
                    FluencyWeight = 0.25f,
                    AccuracyWeight = 0.15f,
                    GrammarWeight = 0.05f,
                    VocabularyWeight = 0.05f,
                    ContentWeight = 0.00f,
                    ScaleFactor = 1.0f
                },

                "SPEAKING_PART_2" => new ScoringWeights
                {
                    GrammarWeight = 0.25f,
                    VocabularyWeight = 0.25f,
                    ContentWeight = 0.20f,
                    FluencyWeight = 0.15f,
                    PronunciationWeight = 0.10f,
                    AccuracyWeight = 0.05f,
                    ScaleFactor = 1.0f
                },

                "SPEAKING_PART_3" => new ScoringWeights
                {
                    ContentWeight = 0.30f,
                    FluencyWeight = 0.25f,
                    GrammarWeight = 0.20f,
                    VocabularyWeight = 0.15f,
                    PronunciationWeight = 0.10f,
                    AccuracyWeight = 0.00f,
                    ScaleFactor = 1.0f
                },

                "SPEAKING_PART_4" => new ScoringWeights
                {
                    ContentWeight = 0.30f,
                    GrammarWeight = 0.25f,
                    VocabularyWeight = 0.20f,
                    FluencyWeight = 0.15f,
                    PronunciationWeight = 0.10f,
                    AccuracyWeight = 0.00f,
                    ScaleFactor = 1.0f
                },

                "SPEAKING_PART_5" => new ScoringWeights
                {
                    GrammarWeight = 0.30f,
                    VocabularyWeight = 0.25f,
                    ContentWeight = 0.20f,
                    FluencyWeight = 0.15f,
                    PronunciationWeight = 0.10f,
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