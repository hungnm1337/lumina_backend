using DataLayer.DTOs.ManagerAnalytics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositoryLayer.ManagerAnalytics;

public interface IManagerAnalyticsRepository
{
    // Active Users
    Task<int> GetActiveUsersCountAsync(DateTime? fromDate = null);
    Task<int> GetTotalUsersCountAsync();
    Task<int> GetNewUsersCountAsync(DateTime? fromDate);

    // Top Articles
    Task<List<TopArticleDTO>> GetTopArticlesByViewsAsync(int topN, DateTime? fromDate = null);
    Task<int> GetArticleViewCountAsync(int articleId, DateTime? fromDate = null);
    Task<int> GetArticleReadersCountAsync(int articleId, DateTime? fromDate = null);
    Task<double> GetAverageReadingTimeAsync(int articleId);

    // Top Vocabulary
    Task<List<TopVocabularyDTO>> GetTopVocabularyByLearnersAsync(int topN, DateTime? fromDate = null);
    Task<int> GetVocabularyLearnersCountAsync(int vocabularyListId, DateTime? fromDate = null);
    Task<double> GetAverageWordsLearnedAsync(int vocabularyListId);

    // Top Events
    Task<List<TopEventDTO>> GetTopEventsByParticipantsAsync(int topN);
    Task<int> GetEventParticipantsCountAsync(int eventId);

    // Top Slides
    Task<List<TopSlideDTO>> GetTopSlidesAsync(int topN, DateTime? fromDate = null);

    // Top Exams
    Task<List<TopExamDTO>> GetTopExamsByAttemptsAsync(int topN, DateTime? fromDate = null);
    Task<int> GetExamAttemptsCountAsync(int examId, DateTime? fromDate = null);
    Task<int> GetExamCompletedCountAsync(int examId, DateTime? fromDate = null);
    Task<double> GetAverageExamScoreAsync(int examId, DateTime? fromDate = null);
    Task<double> GetAverageExamCompletionTimeAsync(int examId, DateTime? fromDate = null);

    // Completion Rates - Articles
    Task<int> GetArticleProgressCountAsync(int articleId, DateTime? fromDate = null);
    Task<int> GetArticleCompletedCountAsync(int articleId, DateTime? fromDate = null);

    // Completion Rates - Vocabulary
    Task<int> GetVocabularyProgressCountAsync(int vocabularyListId, DateTime? fromDate = null);
    Task<int> GetVocabularyCompletedCountAsync(int vocabularyListId, DateTime? fromDate = null);
    Task<int> GetVocabularyListTotalWordsAsync(int vocabularyListId);

    // Completion Rates - Exams
    Task<List<ExamCompletionRateDTO>> GetExamCompletionRatesAsync(int? examId = null, string? examType = null, DateTime? fromDate = null);

    // Completion Rates - Articles
    Task<List<ArticleCompletionRateDTO>> GetArticleCompletionRatesAsync(int? articleId = null, DateTime? fromDate = null);

    // Completion Rates - Vocabulary
    Task<List<VocabularyCompletionRateDTO>> GetVocabularyCompletionRatesAsync(int? vocabularyListId = null, DateTime? fromDate = null);

    // Completion Rates - Events
    Task<List<EventParticipationRateDTO>> GetEventParticipationRatesAsync(int? eventId = null);
}
























