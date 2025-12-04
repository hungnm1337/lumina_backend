using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.ManagerAnalytics;
using RepositoryLayer.ManagerAnalytics;
using DataLayer.DTOs.ManagerAnalytics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services.ManagerAnalyticsServiceTests
{
    public class ManagerAnalyticsGetTopEventsAsyncTests
    {
        private readonly Mock<IManagerAnalyticsRepository> _mockRepository;
        private readonly ManagerAnalyticsService _service;

        public ManagerAnalyticsGetTopEventsAsyncTests()
        {
            _mockRepository = new Mock<IManagerAnalyticsRepository>();
            _service = new ManagerAnalyticsService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetTopEventsAsync_ShouldCallRepository()
        {
            // Arrange
            int topN = 5;
            var events = new List<TopEventDTO>
            {
                new TopEventDTO { EventId = 1, EventName = "Event 1", ParticipantCount = 50 }
            };

            _mockRepository.Setup(r => r.GetTopEventsByParticipantsAsync(topN))
                .ReturnsAsync(events);

            // Act
            var result = await _service.GetTopEventsAsync(topN);

            // Assert
            result.Should().BeEquivalentTo(events);
            _mockRepository.Verify(r => r.GetTopEventsByParticipantsAsync(topN), Times.Once);
        }
    }
}
