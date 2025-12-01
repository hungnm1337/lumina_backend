using Xunit;
using Moq;
using FluentAssertions;
using ServiceLayer.Leaderboard;
using RepositoryLayer.Leaderboard;
using DataLayer.DTOs.Leaderboard;
using DataLayer.DTOs;
using DataLayer.Models;
using ServiceLayer.Notification;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services.LeaderboardServiceTests
{
    public class LeaderboardServiceGetTests
    {
        private readonly Mock<ILeaderboardRepository> _mockRepository;
        private readonly Mock<LuminaSystemContext> _mockContext;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly LeaderboardService _service;

        public LeaderboardServiceGetTests()
        {
            _mockRepository = new Mock<ILeaderboardRepository>();
            _mockContext = new Mock<LuminaSystemContext>(); // Context might not be needed for these simple pass-throughs but keeping for consistency
            _mockNotificationService = new Mock<INotificationService>();
            _service = new LeaderboardService(_mockRepository.Object, _mockContext.Object, _mockNotificationService.Object);
        }

        [Fact]
        public async Task GetAllPaginatedAsync_ShouldCallRepository()
        {
            // Arrange
            string keyword = "test";
            int page = 1;
            int pageSize = 10;
            var expectedResult = new PaginatedResultDTO<LeaderboardDTO>
            {
                Items = new List<LeaderboardDTO>(),
                Total = 0
            };

            _mockRepository.Setup(repo => repo.GetAllPaginatedAsync(keyword, page, pageSize))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllPaginatedAsync(keyword, page, pageSize);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockRepository.Verify(repo => repo.GetAllPaginatedAsync(keyword, page, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldCallRepository()
        {
            // Arrange
            bool isActive = true;
            var expectedResult = new List<LeaderboardDTO>();

            _mockRepository.Setup(repo => repo.GetAllAsync(isActive))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllAsync(isActive);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockRepository.Verify(repo => repo.GetAllAsync(isActive), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldCallRepository()
        {
            // Arrange
            int id = 1;
            var expectedResult = new LeaderboardDTO { LeaderboardId = id };

            _mockRepository.Setup(repo => repo.GetByIdAsync(id))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockRepository.Verify(repo => repo.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetCurrentAsync_ShouldCallRepository()
        {
            // Arrange
            var expectedResult = new LeaderboardDTO { LeaderboardId = 1, IsActive = true };

            _mockRepository.Setup(repo => repo.GetCurrentAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetCurrentAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockRepository.Verify(repo => repo.GetCurrentAsync(), Times.Once);
        }
        
        [Fact]
        public async Task GetSeasonRankingAsync_ShouldCallRepository()
        {
            // Arrange
            int leaderboardId = 1;
            int top = 50;
            var expectedResult = new List<LeaderboardRankDTO>();

            _mockRepository.Setup(repo => repo.GetSeasonRankingAsync(leaderboardId, top))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetSeasonRankingAsync(leaderboardId, top);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockRepository.Verify(repo => repo.GetSeasonRankingAsync(leaderboardId, top), Times.Once);
        }
    }
}
