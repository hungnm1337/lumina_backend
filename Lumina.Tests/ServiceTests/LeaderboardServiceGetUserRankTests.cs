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
    public class LeaderboardServiceGetUserRankTests
    {
        private readonly Mock<ILeaderboardRepository> _mockRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly LuminaSystemContext _context;
        private readonly LeaderboardService _service;

        public LeaderboardServiceGetUserRankTests()
        {
            _mockRepository = new Mock<ILeaderboardRepository>();
            _mockNotificationService = new Mock<INotificationService>();
            _context = InMemoryDbContextHelper.CreateContext();
            _service = new LeaderboardService(_mockRepository.Object, _context, _mockNotificationService.Object);
        }

        [Fact]
        public async Task GetUserRankAsync_WhenLeaderboardIdIsProvided_ShouldReturnRank()
        {
            // Arrange
            int userId = 1;
            int leaderboardId = 1;
            int expectedRank = 5;

            _mockRepository
                .Setup(repo => repo.GetUserRankInSeasonAsync(userId, leaderboardId))
                .ReturnsAsync(expectedRank);

            // Act
            var result = await _service.GetUserRankAsync(userId, leaderboardId);

            // Assert
            Assert.Equal(expectedRank, result);

            // Verify repository is called exactly once
            _mockRepository.Verify(
                repo => repo.GetUserRankInSeasonAsync(userId, leaderboardId),
                Times.Once
            );

            // Verify GetCurrentAsync is never called
            _mockRepository.Verify(
                repo => repo.GetCurrentAsync(),
                Times.Never
            );
        }

        [Fact]
        public async Task GetUserRankAsync_WhenLeaderboardIdIsNullAndCurrentExists_ShouldUseCurrentSeason()
        {
            // Arrange
            int userId = 1;
            var currentSeason = new LeaderboardDTO
            {
                LeaderboardId = 1,
                SeasonNumber = 1,
                IsActive = true
            };

            int expectedRank = 5;

            _mockRepository
                .Setup(repo => repo.GetCurrentAsync())
                .ReturnsAsync(currentSeason);

            _mockRepository
                .Setup(repo => repo.GetUserRankInSeasonAsync(userId, currentSeason.LeaderboardId))
                .ReturnsAsync(expectedRank);

            // Act
            var result = await _service.GetUserRankAsync(userId, null);

            // Assert
            Assert.Equal(expectedRank, result);

            // Verify GetCurrentAsync is called
            _mockRepository.Verify(
                repo => repo.GetCurrentAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserRankAsync_WhenLeaderboardIdIsNullAndCurrentIsNull_ShouldReturnZero()
        {
            // Arrange
            int userId = 1;

            _mockRepository
                .Setup(repo => repo.GetCurrentAsync())
                .ReturnsAsync((LeaderboardDTO?)null);

            // Act
            var result = await _service.GetUserRankAsync(userId, null);

            // Assert
            Assert.Equal(0, result);

            // Verify GetCurrentAsync is called
            _mockRepository.Verify(
                repo => repo.GetCurrentAsync(),
                Times.Once
            );

            // Verify GetUserRankInSeasonAsync is never called
            _mockRepository.Verify(
                repo => repo.GetUserRankInSeasonAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }
    }
}
