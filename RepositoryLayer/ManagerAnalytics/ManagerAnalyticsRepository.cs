using DataLayer.DTOs.ManagerAnalytics;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.ManagerAnalytics;

public class ManagerAnalyticsRepository : IManagerAnalyticsRepository
{
    private readonly LuminaSystemContext _context;

    public ManagerAnalyticsRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<int> GetActiveUsersCountAsync(DateTime? fromDate = null)
    {
        var query = _context.Users.Where(u => u.RoleId == 4 && u.IsActive == true); // RoleId 4 = User

        if (fromDate.HasValue)
        {
            query = query.Where(u => u.LastPracticeDate.HasValue && u.LastPracticeDate >= fromDate.Value);
        }
        else
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            query = query.Where(u => u.LastPracticeDate.HasValue && u.LastPracticeDate >= last24Hours);
        }

        return await query.CountAsync();
    }

    public async Task<int> GetTotalUsersCountAsync()
    {
        return await _context.Users.CountAsync(u => u.RoleId == 4);
    }

    public async Task<int> GetNewUsersCountAsync(DateTime? fromDate)
    {
        if (!fromDate.HasValue)
            return 0;

        return await _context.Users
            .Where(u => u.RoleId == 4 && u.Accounts.Any(a => a.CreateAt >= fromDate.Value))
            .CountAsync();
    }

    public async Task<List<TopArticleDTO>> GetTopArticlesByViewsAsync(int topN, DateTime? fromDate = null)
    {
        var articlesQuery = _context.Articles
            .Where(a => a.IsPublished == true)
            .AsNoTracking()
            .AsQueryable();

        if (fromDate.HasValue)
        {
            articlesQuery = articlesQuery.Where(a => a.UserArticleProgresses.Any(p => p.LastAccessedAt >= fromDate.Value));
        }

        var results = await articlesQuery
            .Select(a => new TopArticleDTO
            {
                ArticleId = a.ArticleId,
                Title = a.Title,
                ViewCount = fromDate.HasValue
                    ? a.UserArticleProgresses.Count(p => p.LastAccessedAt >= fromDate.Value)
                    : a.UserArticleProgresses.Count,
                ReaderCount = fromDate.HasValue
                    ? a.UserArticleProgresses.Where(p => p.LastAccessedAt >= fromDate.Value).Select(p => p.UserId).Distinct().Count()
                    : a.UserArticleProgresses.Select(p => p.UserId).Distinct().Count(),
                AverageReadingTime = 0, // Will calculate separately
                CompletionRate = a.UserArticleProgresses.Count > 0
                    ? (double)a.UserArticleProgresses.Count(p => p.CompletedAt != null) / a.UserArticleProgresses.Count * 100
                    : 0,
                CreatedAt = a.CreatedAt
            })
            .OrderByDescending(a => a.ViewCount)
            .Take(topN)
            .ToListAsync();

        return results;
    }

    public async Task<int> GetArticleViewCountAsync(int articleId, DateTime? fromDate = null)
    {
        var query = _context.UserArticleProgresses
            .AsNoTracking()
            .Where(p => p.ArticleId == articleId);

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.LastAccessedAt >= fromDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<int> GetArticleReadersCountAsync(int articleId, DateTime? fromDate = null)
    {
        IQueryable<int> query;
        
        if (fromDate.HasValue)
        {
            query = _context.UserArticleProgresses
                .Where(p => p.ArticleId == articleId && p.LastAccessedAt >= fromDate.Value)
                .Select(p => p.UserId)
                .Distinct();
        }
        else
        {
            query = _context.UserArticleProgresses
                .Where(p => p.ArticleId == articleId)
                .Select(p => p.UserId)
                .Distinct();
        }

        return await query.CountAsync();
    }

    public async Task<double> GetAverageReadingTimeAsync(int articleId)
    {
        var completedArticles = await _context.UserArticleProgresses
            .AsNoTracking()
            .Where(p => p.ArticleId == articleId && p.CompletedAt != null)
            .ToListAsync();

        if (!completedArticles.Any())
            return 0;

        var times = completedArticles
            .Where(p => p.CompletedAt.HasValue)
            .Select(p => (p.CompletedAt!.Value - p.LastAccessedAt).TotalMinutes)
            .Where(t => t > 0 && t < 120) // Filter out unrealistic values (0-120 minutes)
            .ToList();

        return times.Any() ? times.Average() : 0;
    }

    public async Task<List<TopVocabularyDTO>> GetTopVocabularyByLearnersAsync(int topN, DateTime? fromDate = null)
    {
        var listsQuery = _context.VocabularyLists
            .Where(v => v.IsDeleted != true && v.IsPublic == true)
            .AsNoTracking()
            .AsQueryable();

        if (fromDate.HasValue)
        {
            listsQuery = listsQuery.Where(v => v.UserSpacedRepetitions.Any(usr => usr.LastReviewedAt >= fromDate.Value));
        }

        var results = await listsQuery
            .Select(v => new TopVocabularyDTO
            {
                VocabularyListId = v.VocabularyListId,
                ListName = v.Name,
                LearnerCount = fromDate.HasValue
                    ? v.UserSpacedRepetitions.Where(usr => usr.LastReviewedAt >= fromDate.Value).Select(usr => usr.UserId).Distinct().Count()
                    : v.UserSpacedRepetitions.Select(usr => usr.UserId).Distinct().Count(),
                AverageWordsLearned = 0, // Will calculate separately (complex calculation)
                CompletionRate = v.UserSpacedRepetitions.Count > 0
                    ? (double)v.UserSpacedRepetitions.Count(usr => usr.Status == "Mastered" || usr.Status == "Completed") / v.UserSpacedRepetitions.Count * 100
                    : 0,
                TotalWords = v.Vocabularies.Count,
                CreatedAt = v.CreateAt
            })
            .OrderByDescending(v => v.LearnerCount)
            .Take(topN)
            .ToListAsync();

        for (int i = 0; i < results.Count; i++)
        {
            results[i].AverageWordsLearned = await GetAverageWordsLearnedAsync(results[i].VocabularyListId);
        }

        return results;
    }

    public async Task<int> GetVocabularyLearnersCountAsync(int vocabularyListId, DateTime? fromDate = null)
    {
        IQueryable<int> query;
        
        if (fromDate.HasValue)
        {
            query = _context.UserSpacedRepetitions
                .Where(usr => usr.VocabularyListId == vocabularyListId && usr.LastReviewedAt >= fromDate.Value)
                .Select(usr => usr.UserId)
                .Distinct();
        }
        else
        {
            query = _context.UserSpacedRepetitions
                .Where(usr => usr.VocabularyListId == vocabularyListId)
                .Select(usr => usr.UserId)
                .Distinct();
        }

        return await query.CountAsync();
    }

    public async Task<double> GetAverageWordsLearnedAsync(int vocabularyListId)
    {
        var totalWords = await GetVocabularyListTotalWordsAsync(vocabularyListId);
        if (totalWords == 0) return 0;

        var learners = await _context.UserSpacedRepetitions
            .AsNoTracking()
            .Where(usr => usr.VocabularyListId == vocabularyListId && usr.VocabularyId != null)
            .Select(usr => usr.UserId)
            .Distinct()
            .CountAsync();

        if (learners == 0) return 0;

        var totalLearned = await _context.UserSpacedRepetitions
            .AsNoTracking()
            .Where(usr => usr.VocabularyListId == vocabularyListId && usr.VocabularyId != null)
            .CountAsync();

        return (double)totalLearned / learners;
    }

    public async Task<List<TopEventDTO>> GetTopEventsByParticipantsAsync(int topN)
    {
        var events = await _context.Events
            .AsNoTracking()
            .OrderByDescending(e => e.CreateAt)
            .Take(topN)
            .ToListAsync();

        var totalUsers = await GetTotalUsersCountAsync();
        var results = new List<TopEventDTO>();

        foreach (var evt in events)
        {
            var participants = await GetEventParticipantsCountAsync(evt.EventId);
            var participationRate = totalUsers > 0 ? (double)participants / totalUsers * 100 : 0;

            results.Add(new TopEventDTO
            {
                EventId = evt.EventId,
                EventName = evt.EventName,
                ParticipantCount = participants,
                ParticipationRate = Math.Round(participationRate, 2),
                StartDate = evt.StartDate,
                EndDate = evt.EndDate,
                CreatedAt = evt.CreateAt
            });
        }

        return results.OrderByDescending(e => e.ParticipantCount).ToList();
    }

    public async Task<int> GetEventParticipantsCountAsync(int eventId)
    {
        var evt = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (evt == null) return 0;

        var participants = await _context.Users
            .AsNoTracking()
            .Where(u => u.RoleId == 4 && // RoleId 4 = User
                       u.IsActive == true &&
                       u.LastPracticeDate.HasValue &&
                       u.LastPracticeDate >= evt.StartDate &&
                       u.LastPracticeDate <= evt.EndDate)
            .CountAsync();

        return participants;
    }

    public async Task<List<TopSlideDTO>> GetTopSlidesAsync(int topN, DateTime? fromDate = null)
    {
        var query = _context.Slides
            .AsNoTracking()
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.CreateAt >= fromDate.Value);
        }

        return await query
            .OrderByDescending(s => s.CreateAt)
            .Take(topN)
            .Select(s => new TopSlideDTO
            {
                SlideId = s.SlideId,
                SlideName = s.SlideName,
                IsActive = s.IsActive ?? false,
                CreatedAt = s.CreateAt
            })
            .ToListAsync();
    }

    public async Task<List<TopExamDTO>> GetTopExamsByAttemptsAsync(int topN, DateTime? fromDate = null)
    {
        var examsQuery = _context.Exams
            .Where(e => e.IsActive == true)
            .AsNoTracking()
            .AsQueryable();

        var results = await examsQuery
            .Select(e => new TopExamDTO
            {
                ExamId = e.ExamId,
                ExamName = e.Name,
                ExamType = e.ExamType,
                AttemptCount = fromDate.HasValue
                    ? e.ExamAttempts.Count(a => a.StartTime >= fromDate.Value)
                    : e.ExamAttempts.Count,
                CompletedCount = fromDate.HasValue
                    ? e.ExamAttempts.Count(a => a.Status == "Completed" && a.StartTime >= fromDate.Value)
                    : e.ExamAttempts.Count(a => a.Status == "Completed"),
                AverageScore = fromDate.HasValue
                    ? e.ExamAttempts.Where(a => a.Score != null && a.StartTime >= fromDate.Value).Any()
                        ? e.ExamAttempts.Where(a => a.Score != null && a.StartTime >= fromDate.Value).Average(a => a.Score!.Value)
                        : 0
                    : e.ExamAttempts.Where(a => a.Score != null).Any()
                        ? e.ExamAttempts.Where(a => a.Score != null).Average(a => a.Score!.Value)
                        : 0,
                CompletionRate = fromDate.HasValue
                    ? e.ExamAttempts.Count(a => a.StartTime >= fromDate.Value) > 0
                        ? (double)e.ExamAttempts.Count(a => a.Status == "Completed" && a.StartTime >= fromDate.Value) / 
                          e.ExamAttempts.Count(a => a.StartTime >= fromDate.Value) * 100
                        : 0
                    : e.ExamAttempts.Count > 0
                        ? (double)e.ExamAttempts.Count(a => a.Status == "Completed") / e.ExamAttempts.Count * 100
                        : 0,
                CreatedAt = e.CreatedAt
            })
            .OrderByDescending(e => e.AttemptCount)
            .Take(topN)
            .ToListAsync();

        return results;
    }

    public async Task<int> GetExamAttemptsCountAsync(int examId, DateTime? fromDate = null)
    {
        var query = _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamID == examId);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.StartTime >= fromDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<int> GetExamCompletedCountAsync(int examId, DateTime? fromDate = null)
    {
        var query = _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamID == examId && a.Status == "Completed");

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.StartTime >= fromDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<double> GetAverageExamScoreAsync(int examId, DateTime? fromDate = null)
    {
        var query = _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamID == examId && a.Score != null);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.StartTime >= fromDate.Value);
        }

        var scores = await query.Select(a => a.Score!.Value).ToListAsync();
        return scores.Any() ? scores.Average() : 0;
    }

    public async Task<double> GetAverageExamCompletionTimeAsync(int examId, DateTime? fromDate = null)
    {
        var query = _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamID == examId && a.Status == "Completed" && a.EndTime != null);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.StartTime >= fromDate.Value);
        }

        var attempts = await query.ToListAsync();
        if (!attempts.Any()) return 0;

        var times = attempts
            .Where(a => a.EndTime.HasValue)
            .Select(a => (a.EndTime!.Value - a.StartTime).TotalMinutes)
            .Where(t => t > 0 && t < 300) // Filter unrealistic values (0-300 minutes)
            .ToList();

        return times.Any() ? times.Average() : 0;
    }

    public async Task<int> GetArticleProgressCountAsync(int articleId, DateTime? fromDate = null)
    {
        var query = _context.UserArticleProgresses
            .AsNoTracking()
            .Where(p => p.ArticleId == articleId);

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.LastAccessedAt >= fromDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<int> GetArticleCompletedCountAsync(int articleId, DateTime? fromDate = null)
    {
        var query = _context.UserArticleProgresses
            .AsNoTracking()
            .Where(p => p.ArticleId == articleId && p.CompletedAt != null);

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.CompletedAt >= fromDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<int> GetVocabularyProgressCountAsync(int vocabularyListId, DateTime? fromDate = null)
    {
        var query = _context.UserSpacedRepetitions
            .AsNoTracking()
            .Where(usr => usr.VocabularyListId == vocabularyListId);

        if (fromDate.HasValue)
        {
            query = query.Where(usr => usr.LastReviewedAt >= fromDate.Value);
        }

        return await query.Select(usr => usr.UserId).Distinct().CountAsync();
    }

    public async Task<int> GetVocabularyCompletedCountAsync(int vocabularyListId, DateTime? fromDate = null)
    {
        var query = _context.UserSpacedRepetitions
            .AsNoTracking()
            .Where(usr => usr.VocabularyListId == vocabularyListId && 
                         (usr.Status == "Mastered" || usr.Status == "Completed"));

        if (fromDate.HasValue)
        {
            query = query.Where(usr => usr.LastReviewedAt >= fromDate.Value);
        }

        return await query.Select(usr => usr.UserId).Distinct().CountAsync();
    }

    public async Task<int> GetVocabularyListTotalWordsAsync(int vocabularyListId)
    {
        return await _context.Vocabularies
            .AsNoTracking()
            .CountAsync(v => v.VocabularyListId == vocabularyListId);
    }

    public async Task<List<ExamCompletionRateDTO>> GetExamCompletionRatesAsync(int? examId = null, string? examType = null, DateTime? fromDate = null)
    {
        var query = _context.Exams
            .AsNoTracking()
            .AsQueryable();

        if (examId.HasValue)
        {
            query = query.Where(e => e.ExamId == examId.Value);
        }

        if (!string.IsNullOrEmpty(examType))
        {
            query = query.Where(e => e.ExamType == examType);
        }

        var exams = await query.ToListAsync();

        var results = new List<ExamCompletionRateDTO>();

        foreach (var exam in exams)
        {
            var attemptsQuery = _context.ExamAttempts
                .AsNoTracking()
                .Where(a => a.ExamID == exam.ExamId);

            if (fromDate.HasValue)
            {
                attemptsQuery = attemptsQuery.Where(a => a.StartTime >= fromDate.Value);
            }

            var totalAttempts = await attemptsQuery.CountAsync();
            
            var completedQuery = _context.ExamAttempts
                .AsNoTracking()
                .Where(a => a.ExamID == exam.ExamId && a.Status == "Completed");
            if (fromDate.HasValue)
            {
                completedQuery = completedQuery.Where(a => a.StartTime >= fromDate.Value);
            }
            var completedAttempts = await completedQuery.CountAsync();
            
            var avgTime = await GetAverageExamCompletionTimeAsync(exam.ExamId, fromDate);

            results.Add(new ExamCompletionRateDTO
            {
                ExamId = exam.ExamId,
                ExamName = exam.Name,
                ExamType = exam.ExamType,
                TotalAttempts = totalAttempts,
                CompletedAttempts = completedAttempts,
                CompletionRate = totalAttempts > 0 ? (double)completedAttempts / totalAttempts * 100 : 0,
                AverageCompletionTime = avgTime
            });
        }

        return results;
    }

    public async Task<List<ArticleCompletionRateDTO>> GetArticleCompletionRatesAsync(int? articleId = null, DateTime? fromDate = null)
    {
        var query = _context.Articles
            .Where(a => a.IsPublished == true)
            .AsNoTracking()
            .AsQueryable();

        if (articleId.HasValue)
        {
            query = query.Where(a => a.ArticleId == articleId.Value);
        }

        var results = await query
            .Select(a => new ArticleCompletionRateDTO
            {
                ArticleId = a.ArticleId,
                Title = a.Title,
                TotalReaders = fromDate.HasValue
                    ? a.UserArticleProgresses.Count(p => p.LastAccessedAt >= fromDate.Value)
                    : a.UserArticleProgresses.Count,
                CompletedReaders = fromDate.HasValue
                    ? a.UserArticleProgresses.Count(p => p.CompletedAt != null && p.CompletedAt >= fromDate.Value)
                    : a.UserArticleProgresses.Count(p => p.CompletedAt != null),
                CompletionRate = fromDate.HasValue
                    ? a.UserArticleProgresses.Count(p => p.LastAccessedAt >= fromDate.Value) > 0
                        ? (double)a.UserArticleProgresses.Count(p => p.CompletedAt != null && p.CompletedAt >= fromDate.Value) / 
                          a.UserArticleProgresses.Count(p => p.LastAccessedAt >= fromDate.Value) * 100
                        : 0
                    : a.UserArticleProgresses.Count > 0
                        ? (double)a.UserArticleProgresses.Count(p => p.CompletedAt != null) / a.UserArticleProgresses.Count * 100
                        : 0,
                AverageReadingTime = 0 // Will calculate separately (complex calculation)
            })
            .ToListAsync();

        for (int i = 0; i < results.Count; i++)
        {
            results[i].AverageReadingTime = await GetAverageReadingTimeAsync(results[i].ArticleId);
        }

        return results;
    }

    public async Task<List<VocabularyCompletionRateDTO>> GetVocabularyCompletionRatesAsync(int? vocabularyListId = null, DateTime? fromDate = null)
    {
        var query = _context.VocabularyLists
            .Where(v => v.IsDeleted != true)
            .AsNoTracking()
            .AsQueryable();

        if (vocabularyListId.HasValue)
        {
            query = query.Where(v => v.VocabularyListId == vocabularyListId.Value);
        }

        var results = await query
            .Select(v => new VocabularyCompletionRateDTO
            {
                VocabularyListId = v.VocabularyListId,
                ListName = v.Name,
                TotalLearners = fromDate.HasValue
                    ? v.UserSpacedRepetitions.Where(usr => usr.LastReviewedAt >= fromDate.Value).Select(usr => usr.UserId).Distinct().Count()
                    : v.UserSpacedRepetitions.Select(usr => usr.UserId).Distinct().Count(),
                CompletedLearners = fromDate.HasValue
                    ? v.UserSpacedRepetitions.Where(usr => (usr.Status == "Mastered" || usr.Status == "Completed") && usr.LastReviewedAt >= fromDate.Value).Select(usr => usr.UserId).Distinct().Count()
                    : v.UserSpacedRepetitions.Where(usr => usr.Status == "Mastered" || usr.Status == "Completed").Select(usr => usr.UserId).Distinct().Count(),
                CompletionRate = fromDate.HasValue
                    ? v.UserSpacedRepetitions.Where(usr => usr.LastReviewedAt >= fromDate.Value).Select(usr => usr.UserId).Distinct().Count() > 0
                        ? (double)v.UserSpacedRepetitions.Where(usr => (usr.Status == "Mastered" || usr.Status == "Completed") && usr.LastReviewedAt >= fromDate.Value).Select(usr => usr.UserId).Distinct().Count() / 
                          v.UserSpacedRepetitions.Where(usr => usr.LastReviewedAt >= fromDate.Value).Select(usr => usr.UserId).Distinct().Count() * 100
                        : 0
                    : v.UserSpacedRepetitions.Select(usr => usr.UserId).Distinct().Count() > 0
                        ? (double)v.UserSpacedRepetitions.Where(usr => usr.Status == "Mastered" || usr.Status == "Completed").Select(usr => usr.UserId).Distinct().Count() / 
                          v.UserSpacedRepetitions.Select(usr => usr.UserId).Distinct().Count() * 100
                        : 0,
                AverageWordsLearned = 0, // Will calculate separately (complex calculation)
                TotalWords = v.Vocabularies.Count
            })
            .ToListAsync();

        for (int i = 0; i < results.Count; i++)
        {
            results[i].AverageWordsLearned = await GetAverageWordsLearnedAsync(results[i].VocabularyListId);
        }

        return results;
    }

    public async Task<List<EventParticipationRateDTO>> GetEventParticipationRatesAsync(int? eventId = null)
    {
        var query = _context.Events
            .AsNoTracking()
            .AsQueryable();

        if (eventId.HasValue)
        {
            query = query.Where(e => e.EventId == eventId.Value);
        }

        var events = await query.ToListAsync();
        var totalUsers = await GetTotalUsersCountAsync();
        var results = new List<EventParticipationRateDTO>();

        foreach (var evt in events)
        {
            var participants = await GetEventParticipantsCountAsync(evt.EventId);

            results.Add(new EventParticipationRateDTO
            {
                EventId = evt.EventId,
                EventName = evt.EventName,
                TotalUsers = totalUsers,
                Participants = participants,
                ParticipationRate = totalUsers > 0 ? (double)participants / totalUsers * 100 : 0,
                StartDate = evt.StartDate,
                EndDate = evt.EndDate
            });
        }

        return results;
    }
}

