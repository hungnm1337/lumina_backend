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
    public class ManagerAnalyticsGetTopExamsAsyncTests
    {
        private readonly Mock<IManagerAnalyticsRepository> _mockRepository;
        private readonly ManagerAnalyticsService _service;

        public ManagerAnalyticsGetTopExamsAsyncTests()
        {
            _mockRepository = new Mock<IManagerAnalyticsRepository>();
            _service = new ManagerAnalyticsService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetTopExamsAsync_ShouldCallRepository()
        {
            // Arrange
            int topN = 5;
            int days = 10;
            var exams = new List<TopExamDTO>
            {
                new TopExamDTO { ExamId = 1, ExamName = "Exam 1", AttemptCount = 20 }
            };

            _mockRepository.Setup(r => r.GetTopExamsByAttemptsAsync(topN, It.IsAny<DateTime?>()))
                .ReturnsAsync(exams);

            // Act
            var result = await _service.GetTopExamsAsync(topN, days);

            // Assert
            result.Should().BeEquivalentTo(exams);
            _mockRepository.Verify(r => r.GetTopExamsByAttemptsAsync(topN, It.IsAny<DateTime?>()), Times.Once);
        }
    }
}
