using DataLayer.DTOs.Exam.Speaking;

namespace ServiceLayer.Exam.Speaking
{
    
    public interface IScoringWeightService
    {
        
        ScoringWeights GetWeightsForPart(string partCode);
        
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