using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLayer.DTOs.Leaderboard;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Leaderboard;
using Xunit;

namespace Lumina.Tests
{
    public class GetRankingSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockLeaderboardService;
        private readonly LeaderboardController _controller;

        public GetRankingSeasonTests()
        {
            _mockLeaderboardService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockLeaderboardService.Object);
        }

        [Fact]
        public async Task GetRanking_WithDefaultTop_ReturnsOkWithRankingList()
        {
            // Arrange
            int leaderboardId = 123;
            int top = 100; // default value
            var expectedRanking = new List<LeaderboardRankDTO>
            {
                new LeaderboardRankDTO { UserId = 1, FullName = "User One", Score = 1000, Rank = 1 },
                new LeaderboardRankDTO { UserId = 2, FullName = "User Two", Score = 950, Rank = 2 },
                new LeaderboardRankDTO { UserId = 3, FullName = "User Three", Score = 900, Rank = 3 }
            };

            _mockLeaderboardService
                .Setup(s => s.GetSeasonRankingAsync(leaderboardId, top))
                .ReturnsAsync(expectedRanking);

            // Act
            var result = await _controller.GetRanking(leaderboardId, top);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedRanking = Assert.IsAssignableFrom<List<LeaderboardRankDTO>>(okResult.Value);
            Assert.Equal(3, returnedRanking.Count);
            Assert.Equal(1, returnedRanking[0].Rank);
            _mockLeaderboardService.Verify(s => s.GetSeasonRankingAsync(leaderboardId, top), Times.Once);
        }

        [Fact]
        public async Task GetRanking_WithCustomTop_ReturnsOkWithLimitedRanking()
        {
            // Arrange
            int leaderboardId = 123;
            int top = 10;
            var expectedRanking = new List<LeaderboardRankDTO>
            {
                new LeaderboardRankDTO { UserId = 1, FullName = "Top User", Score = 2000, Rank = 1 }
            };

            _mockLeaderboardService
                .Setup(s => s.GetSeasonRankingAsync(leaderboardId, top))
                .ReturnsAsync(expectedRanking);

            // Act
            var result = await _controller.GetRanking(leaderboardId, top);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedRanking = Assert.IsAssignableFrom<List<LeaderboardRankDTO>>(okResult.Value);
            Assert.Single(returnedRanking);
            _mockLeaderboardService.Verify(s => s.GetSeasonRankingAsync(leaderboardId, top), Times.Once);
        }

        [Fact]
        public async Task GetRanking_EmptyRanking_ReturnsOkWithEmptyList()
        {
            // Arrange
            int leaderboardId = 999;
            int top = 100;
            var expectedRanking = new List<LeaderboardRankDTO>();

            _mockLeaderboardService
                .Setup(s => s.GetSeasonRankingAsync(leaderboardId, top))
                .ReturnsAsync(expectedRanking);

            // Act
            var result = await _controller.GetRanking(leaderboardId, top);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedRanking = Assert.IsAssignableFrom<List<LeaderboardRankDTO>>(okResult.Value);
            Assert.Empty(returnedRanking);
            _mockLeaderboardService.Verify(s => s.GetSeasonRankingAsync(leaderboardId, top), Times.Once);
        }

        [Fact]
        public async Task GetRanking_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int leaderboardId = 123;
            int top = 100;

            _mockLeaderboardService
                .Setup(s => s.GetSeasonRankingAsync(leaderboardId, top))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.GetRanking(leaderboardId, top)
            );
            _mockLeaderboardService.Verify(s => s.GetSeasonRankingAsync(leaderboardId, top), Times.Once);
        }

        [Fact]
        public async Task GetRanking_ZeroLeaderboardId_ReturnsOkWithRanking()
        {
            // Arrange
            int leaderboardId = 0;
            int top = 50;
            var expectedRanking = new List<LeaderboardRankDTO>
            {
                new LeaderboardRankDTO { UserId = 1, FullName = "User", Score = 500, Rank = 1 }
            };

            _mockLeaderboardService
                .Setup(s => s.GetSeasonRankingAsync(leaderboardId, top))
                .ReturnsAsync(expectedRanking);

            // Act
            var result = await _controller.GetRanking(leaderboardId, top);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedRanking = Assert.IsAssignableFrom<List<LeaderboardRankDTO>>(okResult.Value);
            Assert.Single(returnedRanking);
            _mockLeaderboardService.Verify(s => s.GetSeasonRankingAsync(leaderboardId, top), Times.Once);
        }

        [Fact]
        public async Task GetRanking_TopEqualOne_ReturnsOkWithSingleRank()
        {
            // Arrange
            int leaderboardId = 123;
            int top = 1;
            var expectedRanking = new List<LeaderboardRankDTO>
            {
                new LeaderboardRankDTO { UserId = 1, FullName = "Champion", Score = 3000, Rank = 1 }
            };

            _mockLeaderboardService
                .Setup(s => s.GetSeasonRankingAsync(leaderboardId, top))
                .ReturnsAsync(expectedRanking);

            // Act
            var result = await _controller.GetRanking(leaderboardId, top);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedRanking = Assert.IsAssignableFrom<List<LeaderboardRankDTO>>(okResult.Value);
            Assert.Single(returnedRanking);
            Assert.Equal("Champion", returnedRanking[0].FullName);
            _mockLeaderboardService.Verify(s => s.GetSeasonRankingAsync(leaderboardId, top), Times.Once);
        }

        [Fact]
        public async Task GetRanking_NegativeTop_CallsServiceWithNegativeTop()
        {
            // Arrange
            int leaderboardId = 123;
            int top = -10;
            var expectedRanking = new List<LeaderboardRankDTO>();

            _mockLeaderboardService
                .Setup(s => s.GetSeasonRankingAsync(leaderboardId, top))
                .ReturnsAsync(expectedRanking);

            // Act
            var result = await _controller.GetRanking(leaderboardId, top);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedRanking = Assert.IsAssignableFrom<List<LeaderboardRankDTO>>(okResult.Value);
            Assert.Empty(returnedRanking);
            _mockLeaderboardService.Verify(s => s.GetSeasonRankingAsync(leaderboardId, top), Times.Once);
        }
    }
}
