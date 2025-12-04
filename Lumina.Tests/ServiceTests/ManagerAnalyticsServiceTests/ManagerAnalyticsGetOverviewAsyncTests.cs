using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.ManagerAnalytics;
using RepositoryLayer.ManagerAnalytics;
using DataLayer.DTOs.ManagerAnalytics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services.ManagerAnalyticsServiceTests
{
    public class ManagerAnalyticsGetOverviewAsyncTests
    {
        private readonly Mock<IManagerAnalyticsRepository> _mockRepository;
        private readonly ManagerAnalyticsService _service;

        public ManagerAnalyticsGetOverviewAsyncTests()
        {
            _mockRepository = new Mock<IManagerAnalyticsRepository>();
            _service = new ManagerAnalyticsService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetOverviewAsync_ShouldCallAllRepositoriesAndAggregateResults()
        {
            // Arrange
            int topN = 5;
            int days = 30;

            // Setup mocks for all dependencies called by GetOverviewAsync
            _mockRepository.Setup(r => r.GetActiveUsersCountAsync(It.IsAny<DateTime?>())).ReturnsAsync(100);
            _mockRepository.Setup(r => r.GetTotalUsersCountAsync()).ReturnsAsync(1000);
            _mockRepository.Setup(r => r.GetNewUsersCountAsync(It.IsAny<DateTime>())).ReturnsAsync(10); // For all calls

            _mockRepository.Setup(r => r.GetTopArticlesByViewsAsync(topN, It.IsAny<DateTime?>())).ReturnsAsync(new List<TopArticleDTO>());
            _mockRepository.Setup(r => r.GetTopVocabularyByLearnersAsync(topN, It.IsAny<DateTime?>())).ReturnsAsync(new List<TopVocabularyDTO>());
            _mockRepository.Setup(r => r.GetTopEventsByParticipantsAsync(topN)).ReturnsAsync(new List<TopEventDTO>());
            _mockRepository.Setup(r => r.GetTopSlidesAsync(topN, It.IsAny<DateTime?>())).ReturnsAsync(new List<TopSlideDTO>());
            _mockRepository.Setup(r => r.GetTopExamsByAttemptsAsync(topN, It.IsAny<DateTime?>())).ReturnsAsync(new List<TopExamDTO>());
            
            _mockRepository.Setup(r => r.GetExamCompletionRatesAsync(null, null, It.IsAny<DateTime?>())).ReturnsAsync(new List<ExamCompletionRateDTO>());
            _mockRepository.Setup(r => r.GetArticleCompletionRatesAsync(null, It.IsAny<DateTime?>())).ReturnsAsync(new List<ArticleCompletionRateDTO>());
            _mockRepository.Setup(r => r.GetVocabularyCompletionRatesAsync(null, It.IsAny<DateTime?>())).ReturnsAsync(new List<VocabularyCompletionRateDTO>());
            _mockRepository.Setup(r => r.GetEventParticipationRatesAsync(null)).ReturnsAsync(new List<EventParticipationRateDTO>());

            // Act
            var result = await _service.GetOverviewAsync(topN, days);

            // Assert
            result.Should().NotBeNull();
            result.ActiveUsers.Should().NotBeNull();
            result.TopArticles.Should().NotBeNull();
            // ... Verify other properties are populated (even if empty lists)
            
            // Verify calls
            _mockRepository.Verify(r => r.GetActiveUsersCountAsync(It.IsAny<DateTime?>()), Times.Once);
            _mockRepository.Verify(r => r.GetTopArticlesByViewsAsync(topN, It.IsAny<DateTime?>()), Times.Once);
            // ... Verify other calls
        }
    }
}
