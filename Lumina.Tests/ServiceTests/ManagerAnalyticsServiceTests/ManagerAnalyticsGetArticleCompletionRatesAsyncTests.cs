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
    public class ManagerAnalyticsGetArticleCompletionRatesAsyncTests
    {
        private readonly Mock<IManagerAnalyticsRepository> _mockRepository;
        private readonly ManagerAnalyticsService _service;

        public ManagerAnalyticsGetArticleCompletionRatesAsyncTests()
        {
            _mockRepository = new Mock<IManagerAnalyticsRepository>();
            _service = new ManagerAnalyticsService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetArticleCompletionRatesAsync_ShouldCallRepository()
        {
            // Arrange
            int? articleId = 1;
            int? days = 30;
            var rates = new List<ArticleCompletionRateDTO>
            {
                new ArticleCompletionRateDTO { ArticleId = 1, CompletionRate = 75.0 }
            };

            _mockRepository.Setup(r => r.GetArticleCompletionRatesAsync(articleId, It.IsAny<DateTime?>()))
                .ReturnsAsync(rates);

            // Act
            var result = await _service.GetArticleCompletionRatesAsync(articleId, days);

            // Assert
            result.Should().BeEquivalentTo(rates);
            _mockRepository.Verify(r => r.GetArticleCompletionRatesAsync(articleId, It.IsAny<DateTime?>()), Times.Once);
        }
    }
}
