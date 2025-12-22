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
                // =================================================================
                // Part 1: Read Aloud (Q1-2)
                // ETS criteria: Only Pronunciation & Intonation/Stress
                // Uses AccuracyScore from Azure Speech for gating (measures text match)
                // =================================================================
                "SPEAKING_PART_1" => new ScoringWeights
                {
                    PronunciationWeight = 0.50f,  // 50% - Pronunciation clarity
                    FluencyWeight = 0.30f,        // 30% - Intonation & Stress
                    AccuracyWeight = 0.20f,       // 20% - Match to reference text
                    GrammarWeight = 0.00f,        // Not evaluated
                    VocabularyWeight = 0.00f,     // Not evaluated
                    ContentWeight = 0.00f,        // Not evaluated
                    ScaleFactor = 1.0f,
                    // Content Gate: If AccuracyScore < 20, cap at 17 (Score 0 on 0-3)
                    // This catches truly unrelated responses while allowing partial readings
                    UseAccuracyForGate = true,
                    ContentGateThreshold = 20f,   // Lower threshold for partial readings
                    MaxScoreIfGateFails = 17f     // Score 0 on 0-3 scale
                },

                // =================================================================
                // Part 2: Describe a Picture (Q3-4)
                // ETS criteria: Delivery 35%, Language Use 35%, Content 30%
                // STRICTER than other parts - must describe picture coherently
                // =================================================================
                "SPEAKING_PART_2" => new ScoringWeights
                {
                    PronunciationWeight = 0.15f,  // 15% - Delivery
                    FluencyWeight = 0.20f,        // 20% - Intonation/Stress
                    AccuracyWeight = 0.00f,       // Not used
                    GrammarWeight = 0.20f,        // 20% - Language Use
                    VocabularyWeight = 0.15f,     // 15% - Language Use + Cohesion
                    ContentWeight = 0.30f,        // 30% - Describes main features
                    ScaleFactor = 1.0f,
                    // Content Gate: If ContentScore < 40, cap at 25 (Score 0-1)
                    // Higher threshold than other parts - fragmented descriptions
                    // with low semantic coherence should score low
                    UseAccuracyForGate = false,
                    ContentGateThreshold = 40f,   // Raised from 20
                    MaxScoreIfGateFails = 25f     // Stricter: 25 instead of 33
                },

                // =================================================================
                // Part 3: Respond to Questions (Q5-7)
                // ETS criteria: Heavy on Content (Relevance + Completeness)
                // =================================================================
                "SPEAKING_PART_3" => new ScoringWeights
                {
                    PronunciationWeight = 0.10f,  // 10% - Delivery
                    FluencyWeight = 0.10f,        // 10% - Delivery
                    AccuracyWeight = 0.00f,       // Not used
                    GrammarWeight = 0.25f,        // 25% - Language Use
                    VocabularyWeight = 0.15f,     // 15% - Language Use
                    ContentWeight = 0.40f,        // 40% - Relevance + Completeness
                    ScaleFactor = 1.0f,
                    // Content Gate: If ContentScore < 50, cap at 25 (Score 0-1)
                    // VERY HIGH THRESHOLD - even parroting the question prompt should fail
                    // Must provide specific, relevant answer to the actual question
                    UseAccuracyForGate = false,
                    ContentGateThreshold = 50f,   // Raised from 45
                    MaxScoreIfGateFails = 25f     // Stricter cap
                },

                // =================================================================
                // Part 4: Respond Using Information (Q8-10)
                // ETS criteria: Must use provided info accurately - highest content weight
                // =================================================================
                "SPEAKING_PART_4" => new ScoringWeights
                {
                    PronunciationWeight = 0.10f,  // 10% - Delivery
                    FluencyWeight = 0.10f,        // 10% - Delivery
                    AccuracyWeight = 0.00f,       // Not used
                    GrammarWeight = 0.20f,        // 20% - Language Use
                    VocabularyWeight = 0.10f,     // 10% - Language Use
                    ContentWeight = 0.50f,        // 50% - Must use provided info
                    ScaleFactor = 1.0f,
                    // Content Gate: If ContentScore < 40, cap at 25 (Score 0-1)
                    // CRITICAL: Must use provided info ACCURATELY
                    // Wrong/missing information should fail immediately
                    UseAccuracyForGate = false,
                    ContentGateThreshold = 40f,   // Raised from 20 - match Part 2 strictness
                    MaxScoreIfGateFails = 25f     // Score 0-1 for wrong info
                },

                // =================================================================
                // Part 5: Express an Opinion (Q11)
                // ETS criteria: Balanced but content-heavy (Opinion + Reasons + Examples)
                // Scale 0-5 instead of 0-3
                // =================================================================
                "SPEAKING_PART_5" => new ScoringWeights
                {
                    PronunciationWeight = 0.08f,  // 8% - Delivery
                    FluencyWeight = 0.07f,        // 7% - Delivery
                    AccuracyWeight = 0.00f,       // Not used
                    GrammarWeight = 0.25f,        // 25% - Language Use
                    VocabularyWeight = 0.15f,     // 15% - Language Use
                    ContentWeight = 0.45f,        // 45% - Opinion + Reasons + Examples
                    ScaleFactor = 1.0f,           // No scaling needed now
                    // Content Gate: If ContentScore < 35, cap at 20 (Score 1 on 0-5 scale)
                    // Must provide opinion + reasoning/examples
                    // Opinion only (no support) should score low
                    UseAccuracyForGate = false,
                    ContentGateThreshold = 35f,   // Raised from 15 - moderate strictness
                    MaxScoreIfGateFails = 20f     // 20% = Score 1 on 0-5 scale
                },

                // Default weights for unknown parts
                _ => new ScoringWeights
                {
                    GrammarWeight = 0.25f,
                    VocabularyWeight = 0.20f,
                    ContentWeight = 0.20f,
                    FluencyWeight = 0.20f,
                    PronunciationWeight = 0.10f,
                    AccuracyWeight = 0.05f,
                    ScaleFactor = 1.0f,
                    // Content Gate: If ContentScore < 20, cap at 25 (Score 0-1)
                    UseAccuracyForGate = false,
                    ContentGateThreshold = 20f,
                    MaxScoreIfGateFails = 25f     // Stricter cap
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
            double contentScore,
            double completenessScore = 100)
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

            // =================================================================
            // Content Gate: Cap score if content/accuracy is below threshold
            // This prevents high scores for irrelevant responses
            // =================================================================
            double gatingScore = weights.UseAccuracyForGate ? accuracyScore : contentScore;
            
            if (gatingScore < weights.ContentGateThreshold)
            {
                double originalScore = totalScore;
                totalScore = Math.Min(totalScore, weights.MaxScoreIfGateFails);
                
                _logger.LogInformation(
                    "Content Gate applied: {GateType}={GateScore:F1} < {Threshold:F1}, capping score from {Original:F1} to {Capped:F1}",
                    weights.UseAccuracyForGate ? "AccuracyScore" : "ContentScore",
                    gatingScore,
                    weights.ContentGateThreshold,
                    originalScore,
                    totalScore);
            }
            
            // =================================================================
            // CRITICAL FIX: Completeness Gate for Part 1 Read Aloud
            // Azure Speech CompletenessScore measures how much of the reference
            // text was actually read. If user only reads 20% of text, score
            // should be low regardless of pronunciation quality.
            // =================================================================
            if (weights.UseAccuracyForGate && pronunciationScore > 0)
            {
                // For Part 1 (Read Aloud): Apply strict completeness check
                // CompletenessScore from Azure Speech shows % of text read
                
                // Tier 1: If CompletenessScore < 30, user read very little → Score 0 (0-17)
                if (completenessScore < 30)
                {
                    double capScore = 17;  // Score 0 on 0-3 scale
                    if (totalScore > capScore)
                    {
                        _logger.LogWarning(
                            "Part 1 Completeness Gate (Severe): CompletenessScore={Completeness:F1} < 30%, capping from {Original:F1} to {Cap:F1} (Score 0)",
                            completenessScore,
                            totalScore,
                            capScore);
                        totalScore = capScore;
                    }
                }
                // Tier 2: If CompletenessScore < 60, user read less than 60% → Score 1-2 max (17-83)
                else if (completenessScore < 60)
                {
                    double capScore = 50;  // Score 1-2 on 0-3 scale
                    if (totalScore > capScore)
                    {
                        _logger.LogWarning(
                            "Part 1 Completeness Gate (Moderate): CompletenessScore={Completeness:F1} < 60%, capping from {Original:F1} to {Cap:F1} (Score 1-2)",
                            completenessScore,
                            totalScore,
                            capScore);
                        totalScore = capScore;
                    }
                }
            }

            return (float)Math.Round(totalScore, 1);
        }
    }
}