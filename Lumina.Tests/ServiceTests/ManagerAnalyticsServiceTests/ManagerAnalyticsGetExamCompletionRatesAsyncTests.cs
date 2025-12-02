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
    public class ManagerAnalyticsGetExamCompletionRatesAsyncTests
    {
        private readonly Mock<IManagerAnalyticsRepository> _mockRepository;
        private readonly ManagerAnalyticsService _service;

        public ManagerAnalyticsGetExamCompletionRatesAsyncTests()
        {
            _mockRepository = new Mock<IManagerAnalyticsRepository>();
            _service = new ManagerAnalyticsService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetExamCompletionRatesAsync_ShouldCallRepository()
        {
            // Arrange
            int? examId = 1;
            string? examType = "TOEIC";
            int? days = 30;
            var rates = new List<ExamCompletionRateDTO>
            {
                new ExamCompletionRateDTO { ExamId = 1, CompletionRate = 80.5 }
            };

            _mockRepository.Setup(r => r.GetExamCompletionRatesAsync(examId, examType, It.IsAny<DateTime?>()))
                .ReturnsAsync(rates);

            // Act
            var result = await _service.GetExamCompletionRatesAsync(examId, examType, days);

            // Assert
            result.Should().BeEquivalentTo(rates);
            _mockRepository.Verify(r => r.GetExamCompletionRatesAsync(examId, examType, It.IsAny<DateTime?>()), Times.Once);
        }
    }
}
