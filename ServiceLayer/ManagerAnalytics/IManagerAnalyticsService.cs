using DataLayer.DTOs.ManagerAnalytics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.ManagerAnalytics;

public interface IManagerAnalyticsService
{
    Task<ActiveUsersDTO> GetActiveUsersAsync(DateTime? fromDate = null);
    Task<List<TopArticleDTO>> GetTopArticlesAsync(int topN = 10, int? days = null);
    Task<List<TopVocabularyDTO>> GetTopVocabularyAsync(int topN = 10, int? days = null);
    Task<List<TopEventDTO>> GetTopEventsAsync(int topN = 10);
    Task<List<TopSlideDTO>> GetTopSlidesAsync(int topN = 10, int? days = null);
    Task<List<TopExamDTO>> GetTopExamsAsync(int topN = 10, int? days = null);
    Task<List<ExamCompletionRateDTO>> GetExamCompletionRatesAsync(int? examId = null, string? examType = null, int? days = null);
    Task<List<ArticleCompletionRateDTO>> GetArticleCompletionRatesAsync(int? articleId = null, int? days = null);
    Task<List<VocabularyCompletionRateDTO>> GetVocabularyCompletionRatesAsync(int? vocabularyListId = null, int? days = null);
    Task<List<EventParticipationRateDTO>> GetEventParticipationRatesAsync(int? eventId = null);
    Task<ManagerAnalyticsOverviewDTO> GetOverviewAsync(int topN = 10, int? days = null);
}


