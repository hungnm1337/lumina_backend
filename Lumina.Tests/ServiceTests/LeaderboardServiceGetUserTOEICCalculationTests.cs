using Xunit;
using Moq;
using ServiceLayer.Leaderboard;
using RepositoryLayer.Leaderboard;
using ServiceLayer.Notification;
using DataLayer.DTOs.Leaderboard;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class LeaderboardServiceGetUserTOEICCalculationTests
    {
        private readonly Mock<ILeaderboardRepository> _mockRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly LuminaSystemContext _context;
        private readonly LeaderboardService _service;

        public LeaderboardServiceGetUserTOEICCalculationTests()
        {
            _mockRepository = new Mock<ILeaderboardRepository>();
            _mockNotificationService = new Mock<INotificationService>();
            _context = InMemoryDbContextHelper.CreateContext();
            _service = new LeaderboardService(_mockRepository.Object, _context, _mockNotificationService.Object);
        }

        [Fact]
        public async Task GetUserTOEICCalculationAsync_WhenLeaderboardIdIsProvided_ShouldReturnCalculation()
        {
            // Arrange
            int userId = 1;
            int leaderboardId = 1;
            var expectedCalc = new TOEICScoreCalculationDTO
            {
                UserId = userId,
                EstimatedTOEICScore = 750,
                ToeicLevel = "Upper-Intermediate",
                BasePointsPerCorrect = 5,
                TimeBonus = 10.5m,
                AccuracyBonus = 15.2m,
                DifficultyMultiplier = 1.0m,
                TotalSeasonScore = 1000
            };

            _mockRepository
                .Setup(repo => repo.GetUserTOEICCalculationAsync(userId, leaderboardId))
                .ReturnsAsync(expectedCalc);

            // Act
            var result = await _service.GetUserTOEICCalculationAsync(userId, leaderboardId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedCalc.UserId, result!.UserId);
            Assert.Equal(expectedCalc.EstimatedTOEICScore, result.EstimatedTOEICScore);

            // Verify repository is called exactly once
            _mockRepository.Verify(
                repo => repo.GetUserTOEICCalculationAsync(userId, leaderboardId),
                Times.Once
            );

            // Verify GetCurrentAsync is never called
            _mockRepository.Verify(
                repo => repo.GetCurrentAsync(),
                Times.Never
            );
        }

        [Fact]
        public async Task GetUserTOEICCalculationAsync_WhenLeaderboardIdIsNullAndCurrentExists_ShouldUseCurrentSeason()
        {
            // Arrange
            int userId = 1;
            var currentSeason = new LeaderboardDTO
            {
                LeaderboardId = 1,
                SeasonNumber = 1,
                IsActive = true
            };

            var expectedCalc = new TOEICScoreCalculationDTO
            {
                UserId = userId,
                EstimatedTOEICScore = 750
            };

            _mockRepository
                .Setup(repo => repo.GetCurrentAsync())
                .ReturnsAsync(currentSeason);

            _mockRepository
                .Setup(repo => repo.GetUserTOEICCalculationAsync(userId, currentSeason.LeaderboardId))
                .ReturnsAsync(expectedCalc);

            // Act
            var result = await _service.GetUserTOEICCalculationAsync(userId, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedCalc.UserId, result!.UserId);

            // Verify GetCurrentAsync is called
            _mockRepository.Verify(
                repo => repo.GetCurrentAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserTOEICCalculationAsync_WhenLeaderboardIdIsNullAndCurrentIsNull_ShouldReturnNull()
        {
            // Arrange
            int userId = 1;

            _mockRepository
                .Setup(repo => repo.GetCurrentAsync())
                .ReturnsAsync((LeaderboardDTO?)null);

            // Act
            var result = await _service.GetUserTOEICCalculationAsync(userId, null);

            // Assert
            Assert.Null(result);

            // Verify GetCurrentAsync is called
            _mockRepository.Verify(
                repo => repo.GetCurrentAsync(),
                Times.Once
            );

            // Verify GetUserTOEICCalculationAsync is never called
            _mockRepository.Verify(
                repo => repo.GetUserTOEICCalculationAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }
    }
}
