using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.ManagerAnalytics;
using RepositoryLayer.ManagerAnalytics;
using DataLayer.DTOs.ManagerAnalytics;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services.ManagerAnalyticsServiceTests
{
    public class ManagerAnalyticsGetActiveUsersAsyncTests
    {
        private readonly Mock<IManagerAnalyticsRepository> _mockRepository;
        private readonly ManagerAnalyticsService _service;

        public ManagerAnalyticsGetActiveUsersAsyncTests()
        {
            _mockRepository = new Mock<IManagerAnalyticsRepository>();
            _service = new ManagerAnalyticsService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetActiveUsersAsync_ShouldReturnActiveUsersData()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddDays(-30);
            int activeUsers = 100;
            int totalUsers = 1000;

            _mockRepository.Setup(r => r.GetActiveUsersCountAsync(fromDate)).ReturnsAsync(activeUsers);
            _mockRepository.Setup(r => r.GetTotalUsersCountAsync()).ReturnsAsync(totalUsers);
            _mockRepository.SetupSequence(r => r.GetNewUsersCountAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(10)  // newUsersThisWeek
                .ReturnsAsync(50)  // newUsersThisMonth
                .ReturnsAsync(40); // newUsersLastMonth

            // Act
            var result = await _service.GetActiveUsersAsync(fromDate);

            // Assert
            result.Should().NotBeNull();
            result.ActiveUsersNow.Should().Be(activeUsers);
            result.TotalUsers.Should().Be(totalUsers);
            result.NewUsersThisWeek.Should().Be(10);
            result.NewUsersThisMonth.Should().Be(50);
            result.GrowthRate.Should().Be(25.0); // ((50 - 40) / 40) * 100 = 25%
            
            _mockRepository.Verify(r => r.GetActiveUsersCountAsync(fromDate), Times.Once);
            _mockRepository.Verify(r => r.GetTotalUsersCountAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetNewUsersCountAsync(It.IsAny<DateTime>()), Times.Exactly(3));
        }

        [Fact]
        public async Task GetActiveUsersAsync_ShouldReturnZeroGrowthRate_WhenLastMonthIsZero()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddDays(-30);
            int activeUsers = 100;
            int totalUsers = 1000;

            _mockRepository.Setup(r => r.GetActiveUsersCountAsync(fromDate)).ReturnsAsync(activeUsers);
            _mockRepository.Setup(r => r.GetTotalUsersCountAsync()).ReturnsAsync(totalUsers);
            _mockRepository.SetupSequence(r => r.GetNewUsersCountAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(10)  // newUsersThisWeek
                .ReturnsAsync(50)  // newUsersThisMonth
                .ReturnsAsync(0);  // newUsersLastMonth = 0

            // Act
            var result = await _service.GetActiveUsersAsync(fromDate);

            // Assert
            result.Should().NotBeNull();
            result.GrowthRate.Should().Be(0.0); // When lastMonth is 0, growth rate should be 0
            
            _mockRepository.Verify(r => r.GetNewUsersCountAsync(It.IsAny<DateTime>()), Times.Exactly(3));
        }

        [Fact]
        public async Task GetActiveUsersAsync_ShouldCallRepositoryWithCorrectParameters()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddDays(-7);

            _mockRepository.Setup(r => r.GetActiveUsersCountAsync(fromDate)).ReturnsAsync(50);
            _mockRepository.Setup(r => r.GetTotalUsersCountAsync()).ReturnsAsync(500);
            _mockRepository.Setup(r => r.GetNewUsersCountAsync(It.IsAny<DateTime>())).ReturnsAsync(5);

            // Act
            var result = await _service.GetActiveUsersAsync(fromDate);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.GetActiveUsersCountAsync(fromDate), Times.Once);
            _mockRepository.Verify(r => r.GetTotalUsersCountAsync(), Times.Once);
        }
    }
}
