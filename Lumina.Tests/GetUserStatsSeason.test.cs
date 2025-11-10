using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer.Leaderboard;
using DataLayer.DTOs.Leaderboard;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class GetUserStatsSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockService;
        private readonly LeaderboardController _controller;

        public GetUserStatsSeasonTests()
        {
            _mockService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockService.Object);
        }

        [Fact]
        public async Task GetUserStats_ValidUserIdWithLeaderboardId_ReturnsOkWithStats()
        {
            // Arrange
            int userId = 1;
            int leaderboardId = 5;
            var expectedStats = new UserSeasonStatsDTO
            {
                UserId = userId,
                CurrentRank = 10,
                CurrentScore = 850,
                EstimatedTOEICScore = 650,
                ToeicLevel = "B1",
                TotalAttempts = 20,
                CorrectAnswers = 150,
                AccuracyRate = 0.75m,
                IsReadyForTOEIC = true
            };
            
            _mockService.Setup(s => s.GetUserStatsAsync(userId, leaderboardId))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetUserStats(userId, leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<UserSeasonStatsDTO>(okResult.Value);
            Assert.Equal(userId, stats.UserId);
            Assert.Equal(10, stats.CurrentRank);
            Assert.Equal(850, stats.CurrentScore);
            Assert.Equal(650, stats.EstimatedTOEICScore);
            Assert.Equal("B1", stats.ToeicLevel);
            Assert.True(stats.IsReadyForTOEIC);
            _mockService.Verify(s => s.GetUserStatsAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetUserStats_ValidUserIdWithoutLeaderboardId_ReturnsOkWithStats()
        {
            // Arrange
            int userId = 2;
            var expectedStats = new UserSeasonStatsDTO
            {
                UserId = userId,
                CurrentRank = 5,
                CurrentScore = 1200,
                EstimatedTOEICScore = 750,
                ToeicLevel = "B2",
                TotalAttempts = 50,
                CorrectAnswers = 400,
                AccuracyRate = 0.80m,
                IsReadyForTOEIC = true
            };
            
            _mockService.Setup(s => s.GetUserStatsAsync(userId, null))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetUserStats(userId, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<UserSeasonStatsDTO>(okResult.Value);
            Assert.Equal(userId, stats.UserId);
            Assert.Equal(5, stats.CurrentRank);
            _mockService.Verify(s => s.GetUserStatsAsync(userId, null), Times.Once);
        }

        [Fact]
        public async Task GetUserStats_StatsNotFound_ReturnsNotFound()
        {
            // Arrange
            int userId = 3;
            int leaderboardId = 10;
            _mockService.Setup(s => s.GetUserStatsAsync(userId, leaderboardId))
                .ReturnsAsync((UserSeasonStatsDTO?)null);

            // Act
            var result = await _controller.GetUserStats(userId, leaderboardId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockService.Verify(s => s.GetUserStatsAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetUserStats_ZeroUserId_CallsServiceWithZero()
        {
            // Arrange
            int userId = 0;
            int leaderboardId = 1;
            var expectedStats = new UserSeasonStatsDTO
            {
                UserId = userId,
                CurrentRank = 100,
                CurrentScore = 0,
                EstimatedTOEICScore = 0,
                ToeicLevel = "A1",
                TotalAttempts = 0,
                CorrectAnswers = 0,
                AccuracyRate = 0m,
                IsReadyForTOEIC = false
            };
            
            _mockService.Setup(s => s.GetUserStatsAsync(userId, leaderboardId))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetUserStats(userId, leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockService.Verify(s => s.GetUserStatsAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetUserStats_NegativeUserId_CallsService()
        {
            // Arrange
            int userId = -1;
            int leaderboardId = 1;
            _mockService.Setup(s => s.GetUserStatsAsync(userId, leaderboardId))
                .ReturnsAsync((UserSeasonStatsDTO?)null);

            // Act
            var result = await _controller.GetUserStats(userId, leaderboardId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockService.Verify(s => s.GetUserStatsAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetUserStats_ZeroLeaderboardId_CallsServiceWithZero()
        {
            // Arrange
            int userId = 4;
            int leaderboardId = 0;
            var expectedStats = new UserSeasonStatsDTO
            {
                UserId = userId,
                CurrentRank = 1,
                CurrentScore = 100,
                EstimatedTOEICScore = 400,
                ToeicLevel = "A2",
                TotalAttempts = 5,
                CorrectAnswers = 30,
                AccuracyRate = 0.60m,
                IsReadyForTOEIC = false
            };
            
            _mockService.Setup(s => s.GetUserStatsAsync(userId, leaderboardId))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetUserStats(userId, leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockService.Verify(s => s.GetUserStatsAsync(userId, leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetUserStats_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int userId = 5;
            int leaderboardId = 1;
            _mockService.Setup(s => s.GetUserStatsAsync(userId, leaderboardId))
                .ThrowsAsync(new System.Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => 
                _controller.GetUserStats(userId, leaderboardId));
            _mockService.Verify(s => s.GetUserStatsAsync(userId, leaderboardId), Times.Once);
        }
    }
}
