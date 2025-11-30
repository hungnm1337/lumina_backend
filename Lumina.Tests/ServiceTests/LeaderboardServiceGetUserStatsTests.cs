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
    public class LeaderboardServiceGetUserStatsTests
    {
        private readonly Mock<ILeaderboardRepository> _mockRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly LuminaSystemContext _context;
        private readonly LeaderboardService _service;

        public LeaderboardServiceGetUserStatsTests()
        {
            _mockRepository = new Mock<ILeaderboardRepository>();
            _mockNotificationService = new Mock<INotificationService>();
            _context = InMemoryDbContextHelper.CreateContext();
            _service = new LeaderboardService(_mockRepository.Object, _context, _mockNotificationService.Object);
        }

        [Fact]
        public async Task GetUserStatsAsync_WhenLeaderboardIdIsProvided_ShouldReturnUserStats()
        {
            // Arrange
            int userId = 1;
            int leaderboardId = 1;
            var expectedStats = new UserSeasonStatsDTO
            {
                UserId = userId,
                CurrentRank = 5,
                CurrentScore = 1000,
                EstimatedTOEICScore = 750,
                ToeicLevel = "Upper-Intermediate",
                TotalAttempts = 10,
                CorrectAnswers = 80,
                AccuracyRate = 0.8m,
                IsReadyForTOEIC = true
            };

            _mockRepository
                .Setup(repo => repo.GetUserSeasonStatsAsync(userId, leaderboardId))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _service.GetUserStatsAsync(userId, leaderboardId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedStats.UserId, result!.UserId);
            Assert.Equal(expectedStats.CurrentRank, result.CurrentRank);

            // Verify repository is called exactly once
            _mockRepository.Verify(
                repo => repo.GetUserSeasonStatsAsync(userId, leaderboardId),
                Times.Once
            );

            // Verify GetCurrentAsync is never called
            _mockRepository.Verify(
                repo => repo.GetCurrentAsync(),
                Times.Never
            );
        }

        [Fact]
        public async Task GetUserStatsAsync_WhenLeaderboardIdIsNullAndCurrentExists_ShouldUseCurrentSeason()
        {
            // Arrange
            int userId = 1;
            var currentSeason = new LeaderboardDTO
            {
                LeaderboardId = 1,
                SeasonNumber = 1,
                IsActive = true
            };

            var expectedStats = new UserSeasonStatsDTO
            {
                UserId = userId,
                CurrentRank = 5,
                CurrentScore = 1000
            };

            _mockRepository
                .Setup(repo => repo.GetCurrentAsync())
                .ReturnsAsync(currentSeason);

            _mockRepository
                .Setup(repo => repo.GetUserSeasonStatsAsync(userId, currentSeason.LeaderboardId))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _service.GetUserStatsAsync(userId, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedStats.UserId, result!.UserId);

            // Verify GetCurrentAsync is called
            _mockRepository.Verify(
                repo => repo.GetCurrentAsync(),
                Times.Once
            );

            // Verify GetUserSeasonStatsAsync is called with current season ID
            _mockRepository.Verify(
                repo => repo.GetUserSeasonStatsAsync(userId, currentSeason.LeaderboardId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserStatsAsync_WhenLeaderboardIdIsNullAndCurrentIsNull_ShouldReturnNull()
        {
            // Arrange
            int userId = 1;

            _mockRepository
                .Setup(repo => repo.GetCurrentAsync())
                .ReturnsAsync((LeaderboardDTO?)null);

            // Act
            var result = await _service.GetUserStatsAsync(userId, null);

            // Assert
            Assert.Null(result);

            // Verify GetCurrentAsync is called
            _mockRepository.Verify(
                repo => repo.GetCurrentAsync(),
                Times.Once
            );

            // Verify GetUserSeasonStatsAsync is never called
            _mockRepository.Verify(
                repo => repo.GetUserSeasonStatsAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }
    }
}
