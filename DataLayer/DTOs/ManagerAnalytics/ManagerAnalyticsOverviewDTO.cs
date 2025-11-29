namespace DataLayer.DTOs.ManagerAnalytics;

public class ManagerAnalyticsOverviewDTO
{
    public ActiveUsersDTO ActiveUsers { get; set; } = new();
    public List<TopArticleDTO> TopArticles { get; set; } = new();
    public List<TopVocabularyDTO> TopVocabulary { get; set; } = new();
    public List<TopEventDTO> TopEvents { get; set; } = new();
    public List<TopSlideDTO> TopSlides { get; set; } = new();
    public List<TopExamDTO> TopExams { get; set; } = new();
    public List<ExamCompletionRateDTO> ExamCompletionRates { get; set; } = new();
    public List<ArticleCompletionRateDTO> ArticleCompletionRates { get; set; } = new();
    public List<VocabularyCompletionRateDTO> VocabularyCompletionRates { get; set; } = new();
    public List<EventParticipationRateDTO> EventParticipationRates { get; set; } = new();
}












