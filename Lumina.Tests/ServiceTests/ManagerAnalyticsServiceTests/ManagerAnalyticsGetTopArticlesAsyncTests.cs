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
    public class ManagerAnalyticsGetTopArticlesAsyncTests
    {
        private readonly Mock<IManagerAnalyticsRepository> _mockRepository;
        private readonly ManagerAnalyticsService _service;

        public ManagerAnalyticsGetTopArticlesAsyncTests()
        {
            _mockRepository = new Mock<IManagerAnalyticsRepository>();
            _service = new ManagerAnalyticsService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetTopArticlesAsync_ShouldReturnArticlesWithReadingTime()
        {
            // Arrange
            int topN = 5;
            int days = 30;
            var articles = new List<TopArticleDTO>
            {
                new TopArticleDTO { ArticleId = 1, Title = "Article 1", ViewCount = 100 },
                new TopArticleDTO { ArticleId = 2, Title = "Article 2", ViewCount = 50 }
            };

            _mockRepository.Setup(r => r.GetTopArticlesByViewsAsync(topN, It.IsAny<DateTime?>()))
                .ReturnsAsync(articles);

            _mockRepository.Setup(r => r.GetAverageReadingTimeAsync(1)).ReturnsAsync(5.5);
            _mockRepository.Setup(r => r.GetAverageReadingTimeAsync(2)).ReturnsAsync(3.2);

            // Act
            var result = await _service.GetTopArticlesAsync(topN, days);

            // Assert
            result.Should().HaveCount(2);
            result[0].AverageReadingTime.Should().Be(5.5);
            result[1].AverageReadingTime.Should().Be(3.2);
            
            _mockRepository.Verify(r => r.GetTopArticlesByViewsAsync(topN, It.IsAny<DateTime?>()), Times.Once);
            _mockRepository.Verify(r => r.GetAverageReadingTimeAsync(1), Times.Once);
            _mockRepository.Verify(r => r.GetAverageReadingTimeAsync(2), Times.Once);
        }
    }
}
