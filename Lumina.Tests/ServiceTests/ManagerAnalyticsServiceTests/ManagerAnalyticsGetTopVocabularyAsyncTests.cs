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
    public class ManagerAnalyticsGetTopVocabularyAsyncTests
    {
        private readonly Mock<IManagerAnalyticsRepository> _mockRepository;
        private readonly ManagerAnalyticsService _service;

        public ManagerAnalyticsGetTopVocabularyAsyncTests()
        {
            _mockRepository = new Mock<IManagerAnalyticsRepository>();
            _service = new ManagerAnalyticsService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetTopVocabularyAsync_ShouldCallRepository()
        {
            // Arrange
            int topN = 10;
            int days = 7;
            var vocabList = new List<TopVocabularyDTO>
            {
                new TopVocabularyDTO { VocabularyListId = 1, ListName = "Vocab 1", LearnerCount = 20 }
            };

            _mockRepository.Setup(r => r.GetTopVocabularyByLearnersAsync(topN, It.IsAny<DateTime?>()))
                .ReturnsAsync(vocabList);

            // Act
            var result = await _service.GetTopVocabularyAsync(topN, days);

            // Assert
            result.Should().BeEquivalentTo(vocabList);
            _mockRepository.Verify(r => r.GetTopVocabularyByLearnersAsync(topN, It.IsAny<DateTime?>()), Times.Once);
        }
    }
}
