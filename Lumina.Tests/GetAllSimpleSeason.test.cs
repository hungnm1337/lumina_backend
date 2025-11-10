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
    public class GetAllSimpleSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockLeaderboardService;
        private readonly LeaderboardController _controller;

        public GetAllSimpleSeasonTests()
        {
            _mockLeaderboardService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockLeaderboardService.Object);
        }

        [Fact]
        public async Task GetAllSimple_NoParameters_ReturnsOkWithAllLeaderboards()
        {
            // Arrange
            var expectedLeaderboards = new List<LeaderboardDTO>
            {
                new LeaderboardDTO { LeaderboardId = 1, SeasonName = "Season 1", IsActive = true },
                new LeaderboardDTO { LeaderboardId = 2, SeasonName = "Season 2", IsActive = false },
                new LeaderboardDTO { LeaderboardId = 3, SeasonName = "Season 3", IsActive = true }
            };

            _mockLeaderboardService
                .Setup(s => s.GetAllAsync(null))
                .ReturnsAsync(expectedLeaderboards);

            // Act
            var result = await _controller.GetAllSimple(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedLeaderboards = Assert.IsAssignableFrom<List<LeaderboardDTO>>(okResult.Value);
            Assert.Equal(3, returnedLeaderboards.Count);
            _mockLeaderboardService.Verify(s => s.GetAllAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetAllSimple_WithIsActiveTrue_ReturnsOkWithActiveLeaderboards()
        {
            // Arrange
            bool? isActive = true;
            var expectedLeaderboards = new List<LeaderboardDTO>
            {
                new LeaderboardDTO { LeaderboardId = 1, SeasonName = "Active Season 1", IsActive = true },
                new LeaderboardDTO { LeaderboardId = 2, SeasonName = "Active Season 2", IsActive = true }
            };

            _mockLeaderboardService
                .Setup(s => s.GetAllAsync(isActive))
                .ReturnsAsync(expectedLeaderboards);

            // Act
            var result = await _controller.GetAllSimple(isActive);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedLeaderboards = Assert.IsAssignableFrom<List<LeaderboardDTO>>(okResult.Value);
            Assert.Equal(2, returnedLeaderboards.Count);
            Assert.All(returnedLeaderboards, lb => Assert.True(lb.IsActive));
            _mockLeaderboardService.Verify(s => s.GetAllAsync(isActive), Times.Once);
        }

        [Fact]
        public async Task GetAllSimple_WithIsActiveFalse_ReturnsOkWithInactiveLeaderboards()
        {
            // Arrange
            bool? isActive = false;
            var expectedLeaderboards = new List<LeaderboardDTO>
            {
                new LeaderboardDTO { LeaderboardId = 1, SeasonName = "Inactive Season", IsActive = false }
            };

            _mockLeaderboardService
                .Setup(s => s.GetAllAsync(isActive))
                .ReturnsAsync(expectedLeaderboards);

            // Act
            var result = await _controller.GetAllSimple(isActive);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedLeaderboards = Assert.IsAssignableFrom<List<LeaderboardDTO>>(okResult.Value);
            Assert.Single(returnedLeaderboards);
            Assert.False(returnedLeaderboards[0].IsActive);
            _mockLeaderboardService.Verify(s => s.GetAllAsync(isActive), Times.Once);
        }

        [Fact]
        public async Task GetAllSimple_NoMatchingLeaderboards_ReturnsOkWithEmptyList()
        {
            // Arrange
            bool? isActive = true;
            var expectedLeaderboards = new List<LeaderboardDTO>();

            _mockLeaderboardService
                .Setup(s => s.GetAllAsync(isActive))
                .ReturnsAsync(expectedLeaderboards);

            // Act
            var result = await _controller.GetAllSimple(isActive);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedLeaderboards = Assert.IsAssignableFrom<List<LeaderboardDTO>>(okResult.Value);
            Assert.Empty(returnedLeaderboards);
            _mockLeaderboardService.Verify(s => s.GetAllAsync(isActive), Times.Once);
        }

        [Fact]
        public async Task GetAllSimple_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            _mockLeaderboardService
                .Setup(s => s.GetAllAsync(It.IsAny<bool?>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.GetAllSimple(null)
            );
            _mockLeaderboardService.Verify(s => s.GetAllAsync(null), Times.Once);
        }
    }
}
