using DataLayer.DTOs.ManagerAnalytics;
using RepositoryLayer.ManagerAnalytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.ManagerAnalytics;

public class ManagerAnalyticsService : IManagerAnalyticsService
{
    private readonly IManagerAnalyticsRepository _repository;

    public ManagerAnalyticsService(IManagerAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<ActiveUsersDTO> GetActiveUsersAsync(DateTime? fromDate = null)
    {
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddMonths(-1);

        var activeUsers = await _repository.GetActiveUsersCountAsync(fromDate);
        var totalUsers = await _repository.GetTotalUsersCountAsync();
        var newUsersThisWeek = await _repository.GetNewUsersCountAsync(weekAgo);
        var newUsersThisMonth = await _repository.GetNewUsersCountAsync(monthAgo);

        var lastMonthStart = monthAgo;
        var newUsersLastMonth = await _repository.GetNewUsersCountAsync(lastMonthStart);
        var growthRate = newUsersLastMonth > 0
            ? ((double)(newUsersThisMonth - newUsersLastMonth) / newUsersLastMonth) * 100
            : 0;

        return new ActiveUsersDTO
        {
            ActiveUsersNow = activeUsers,
            TotalUsers = totalUsers,
            NewUsersThisWeek = newUsersThisWeek,
            NewUsersThisMonth = newUsersThisMonth,
            GrowthRate = Math.Round(growthRate, 2)
        };
    }

    public async Task<List<TopArticleDTO>> GetTopArticlesAsync(int topN = 10, int? days = null)
    {
        DateTime? fromDate = null;
        if (days.HasValue)
        {
            fromDate = DateTime.UtcNow.AddDays(-days.Value);
        }

        var articles = await _repository.GetTopArticlesByViewsAsync(topN, fromDate);

        // Calculate average reading time for each article - run sequentially to avoid DbContext concurrency
        // Note: This could be optimized by batching, but for now sequential is safer
        for (int i = 0; i < articles.Count; i++)
        {
            articles[i].AverageReadingTime = await _repository.GetAverageReadingTimeAsync(articles[i].ArticleId);
        }

        return articles;
    }

    public async Task<List<TopVocabularyDTO>> GetTopVocabularyAsync(int topN = 10, int? days = null)
    {
        DateTime? fromDate = null;
        if (days.HasValue)
        {
            fromDate = DateTime.UtcNow.AddDays(-days.Value);
        }

        return await _repository.GetTopVocabularyByLearnersAsync(topN, fromDate);
    }

    public async Task<List<TopEventDTO>> GetTopEventsAsync(int topN = 10)
    {
        return await _repository.GetTopEventsByParticipantsAsync(topN);
    }

    public async Task<List<TopSlideDTO>> GetTopSlidesAsync(int topN = 10, int? days = null)
    {
        DateTime? fromDate = null;
        if (days.HasValue)
        {
            fromDate = DateTime.UtcNow.AddDays(-days.Value);
        }

        return await _repository.GetTopSlidesAsync(topN, fromDate);
    }

    public async Task<List<TopExamDTO>> GetTopExamsAsync(int topN = 10, int? days = null)
    {
        DateTime? fromDate = null;
        if (days.HasValue)
        {
            fromDate = DateTime.UtcNow.AddDays(-days.Value);
        }

        return await _repository.GetTopExamsByAttemptsAsync(topN, fromDate);
    }

    public async Task<List<ExamCompletionRateDTO>> GetExamCompletionRatesAsync(int? examId = null, string? examType = null, int? days = null)
    {
        DateTime? fromDate = null;
        if (days.HasValue)
        {
            fromDate = DateTime.UtcNow.AddDays(-days.Value);
        }

        return await _repository.GetExamCompletionRatesAsync(examId, examType, fromDate);
    }

    public async Task<List<ArticleCompletionRateDTO>> GetArticleCompletionRatesAsync(int? articleId = null, int? days = null)
    {
        DateTime? fromDate = null;
        if (days.HasValue)
        {
            fromDate = DateTime.UtcNow.AddDays(-days.Value);
        }

        return await _repository.GetArticleCompletionRatesAsync(articleId, fromDate);
    }

    public async Task<List<VocabularyCompletionRateDTO>> GetVocabularyCompletionRatesAsync(int? vocabularyListId = null, int? days = null)
    {
        DateTime? fromDate = null;
        if (days.HasValue)
        {
            fromDate = DateTime.UtcNow.AddDays(-days.Value);
        }

        return await _repository.GetVocabularyCompletionRatesAsync(vocabularyListId, fromDate);
    }

    public async Task<List<EventParticipationRateDTO>> GetEventParticipationRatesAsync(int? eventId = null)
    {
        return await _repository.GetEventParticipationRatesAsync(eventId);
    }

    public async Task<ManagerAnalyticsOverviewDTO> GetOverviewAsync(int topN = 10, int? days = null)
    {
        var overview = new ManagerAnalyticsOverviewDTO();

        // Load data sequentially to avoid DbContext concurrency issues
        // Even with AsNoTracking(), running too many parallel operations can cause issues
        overview.ActiveUsers = await GetActiveUsersAsync();
        overview.TopArticles = await GetTopArticlesAsync(topN, days);
        overview.TopVocabulary = await GetTopVocabularyAsync(topN, days);
        overview.TopEvents = await GetTopEventsAsync(topN);
        overview.TopSlides = await GetTopSlidesAsync(topN, days);
        overview.TopExams = await GetTopExamsAsync(topN, days);
        overview.ExamCompletionRates = await GetExamCompletionRatesAsync(null, null, days);
        overview.ArticleCompletionRates = await GetArticleCompletionRatesAsync(null, days);
        overview.VocabularyCompletionRates = await GetVocabularyCompletionRatesAsync(null, days);
        overview.EventParticipationRates = await GetEventParticipationRatesAsync(null);

        return overview;
    }
}

