using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.Leaderboard;
using RepositoryLayer.Leaderboard;
using DataLayer.Models;
using ServiceLayer.Notification;
using System.Threading.Tasks;

namespace Lumina.Test.Services.LeaderboardServiceTests
{
    public class LeaderboardServiceManagementTests
    {
        private readonly Mock<ILeaderboardRepository> _mockRepository;
        private readonly Mock<LuminaSystemContext> _mockContext;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly LeaderboardService _service;

        public LeaderboardServiceManagementTests()
        {
            _mockRepository = new Mock<ILeaderboardRepository>();
            _mockContext = new Mock<LuminaSystemContext>();
            _mockNotificationService = new Mock<INotificationService>();
            _service = new LeaderboardService(_mockRepository.Object, _mockContext.Object, _mockNotificationService.Object);
        }

        [Fact]
        public async Task DeleteAsync_ShouldCallRepository()
        {
            // Arrange
            int id = 1;
            _mockRepository.Setup(repo => repo.DeleteAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(repo => repo.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task SetCurrentAsync_ShouldCallRepository()
        {
            // Arrange
            int id = 1;
            _mockRepository.Setup(repo => repo.SetCurrentAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _service.SetCurrentAsync(id);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(repo => repo.SetCurrentAsync(id), Times.Once);
        }

        [Fact]
        public async Task RecalculateSeasonScoresAsync_ShouldCallRepository()
        {
            // Arrange
            int id = 1;
            int expectedCount = 10;
            _mockRepository.Setup(repo => repo.RecalculateSeasonScoresAsync(id)).ReturnsAsync(expectedCount);

            // Act
            var result = await _service.RecalculateSeasonScoresAsync(id);

            // Assert
            result.Should().Be(expectedCount);
            _mockRepository.Verify(repo => repo.RecalculateSeasonScoresAsync(id), Times.Once);
        }

        [Fact]
        public async Task ResetSeasonAsync_ShouldCallRepository()
        {
            // Arrange
            int id = 1;
            bool archive = true;
            int expectedCount = 5;
            _mockRepository.Setup(repo => repo.ResetSeasonScoresAsync(id, archive)).ReturnsAsync(expectedCount);

            // Act
            var result = await _service.ResetSeasonAsync(id, archive);

            // Assert
            result.Should().Be(expectedCount);
            _mockRepository.Verify(repo => repo.ResetSeasonScoresAsync(id, archive), Times.Once);
        }

        [Fact]
        public async Task AutoManageSeasonsAsync_ShouldCallRepositoryMethods()
        {
            // Arrange
            _mockRepository.Setup(repo => repo.AutoActivateSeasonAsync()).Returns(Task.CompletedTask);
            _mockRepository.Setup(repo => repo.AutoEndSeasonAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.AutoManageSeasonsAsync();

            // Assert
            _mockRepository.Verify(repo => repo.AutoActivateSeasonAsync(), Times.Once);
            _mockRepository.Verify(repo => repo.AutoEndSeasonAsync(), Times.Once);
        }
    }
}
