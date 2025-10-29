using DataLayer.DTOs.Leaderboard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Leaderboard
{
    public interface IScoringRuleService
    {
        Task<List<ScoringRuleDTO>> GetAllAsync();
        Task<ScoringRuleDTO?> GetByIdAsync(int ruleId);
        Task<int> CreateAsync(CreateScoringRuleDTO dto);
        Task<bool> UpdateAsync(int ruleId, UpdateScoringRuleDTO dto);
        Task<bool> DeleteAsync(int ruleId);
        Task<int> CalculateSessionScoreAsync(int userId, int totalQuestions, int correctAnswers, int timeSpentSeconds, string difficulty = "medium");
        Task<List<PracticeSessionScoreDTO>> GetUserSessionScoresAsync(int userId, int page = 1, int pageSize = 10);
        Task InitializeDefaultScoringRulesAsync();
    }
}

