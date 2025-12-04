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
    public class ManagerAnalyticsGetEventParticipationRatesAsyncTests
    {
        private readonly Mock<IManagerAnalyticsRepository> _mockRepository;
        private readonly ManagerAnalyticsService _service;

        public ManagerAnalyticsGetEventParticipationRatesAsyncTests()
        {
            _mockRepository = new Mock<IManagerAnalyticsRepository>();
            _service = new ManagerAnalyticsService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetEventParticipationRatesAsync_ShouldCallRepository()
        {
            // Arrange
            int? eventId = 1;
            var rates = new List<EventParticipationRateDTO>
            {
                new EventParticipationRateDTO { EventId = 1, ParticipationRate = 45.0 }
            };

            _mockRepository.Setup(r => r.GetEventParticipationRatesAsync(eventId))
                .ReturnsAsync(rates);

            // Act
            var result = await _service.GetEventParticipationRatesAsync(eventId);

            // Assert
            result.Should().BeEquivalentTo(rates);
            _mockRepository.Verify(r => r.GetEventParticipationRatesAsync(eventId), Times.Once);
        }
    }
}
