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
    public class ManagerAnalyticsGetTopSlidesAsyncTests
    {
        private readonly Mock<IManagerAnalyticsRepository> _mockRepository;
        private readonly ManagerAnalyticsService _service;

        public ManagerAnalyticsGetTopSlidesAsyncTests()
        {
            _mockRepository = new Mock<IManagerAnalyticsRepository>();
            _service = new ManagerAnalyticsService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetTopSlidesAsync_ShouldCallRepository()
        {
            // Arrange
            int topN = 5;
            int days = 10;
            var slides = new List<TopSlideDTO>
            {
                new TopSlideDTO { SlideId = 1, SlideName = "Slide 1" }
            };

            _mockRepository.Setup(r => r.GetTopSlidesAsync(topN, It.IsAny<DateTime?>()))
                .ReturnsAsync(slides);

            // Act
            var result = await _service.GetTopSlidesAsync(topN, days);

            // Assert
            result.Should().BeEquivalentTo(slides);
            _mockRepository.Verify(r => r.GetTopSlidesAsync(topN, It.IsAny<DateTime?>()), Times.Once);
        }
    }
}
