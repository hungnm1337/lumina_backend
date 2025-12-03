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
    public class ManagerAnalyticsGetVocabularyCompletionRatesAsyncTests
    {
        private readonly Mock<IManagerAnalyticsRepository> _mockRepository;
        private readonly ManagerAnalyticsService _service;

        public ManagerAnalyticsGetVocabularyCompletionRatesAsyncTests()
        {
            _mockRepository = new Mock<IManagerAnalyticsRepository>();
            _service = new ManagerAnalyticsService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetVocabularyCompletionRatesAsync_ShouldCallRepository()
        {
            // Arrange
            int? vocabListId = 1;
            int? days = 30;
            var rates = new List<VocabularyCompletionRateDTO>
            {
                new VocabularyCompletionRateDTO { VocabularyListId = 1, CompletionRate = 60.0 }
            };

            _mockRepository.Setup(r => r.GetVocabularyCompletionRatesAsync(vocabListId, It.IsAny<DateTime?>()))
                .ReturnsAsync(rates);

            // Act
            var result = await _service.GetVocabularyCompletionRatesAsync(vocabListId, days);

            // Assert
            result.Should().BeEquivalentTo(rates);
            _mockRepository.Verify(r => r.GetVocabularyCompletionRatesAsync(vocabListId, It.IsAny<DateTime?>()), Times.Once);
        }
    }
}
