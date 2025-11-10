using System;
using System.Threading.Tasks;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Leaderboard;
using Xunit;

namespace Lumina.Tests
{
    public class SetCurrentSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockLeaderboardService;
        private readonly LeaderboardController _controller;

        public SetCurrentSeasonTests()
        {
            _mockLeaderboardService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockLeaderboardService.Object);
        }

        [Fact]
        public async Task SetCurrent_ExistingLeaderboardId_ReturnsNoContent()
        {
            // Arrange
            int leaderboardId = 123;

            _mockLeaderboardService
                .Setup(s => s.SetCurrentAsync(leaderboardId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.SetCurrent(leaderboardId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLeaderboardService.Verify(s => s.SetCurrentAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task SetCurrent_NonExistingLeaderboardId_ReturnsNotFound()
        {
            // Arrange
            int leaderboardId = 999;

            _mockLeaderboardService
                .Setup(s => s.SetCurrentAsync(leaderboardId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.SetCurrent(leaderboardId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockLeaderboardService.Verify(s => s.SetCurrentAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task SetCurrent_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int leaderboardId = 123;

            _mockLeaderboardService
                .Setup(s => s.SetCurrentAsync(leaderboardId))
                .ThrowsAsync(new InvalidOperationException("Cannot set current season"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.SetCurrent(leaderboardId)
            );
            _mockLeaderboardService.Verify(s => s.SetCurrentAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task SetCurrent_ZeroLeaderboardId_CallsServiceWithZeroId()
        {
            // Arrange
            int leaderboardId = 0;

            _mockLeaderboardService
                .Setup(s => s.SetCurrentAsync(leaderboardId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.SetCurrent(leaderboardId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockLeaderboardService.Verify(s => s.SetCurrentAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task SetCurrent_NegativeLeaderboardId_CallsServiceWithNegativeId()
        {
            // Arrange
            int leaderboardId = -1;

            _mockLeaderboardService
                .Setup(s => s.SetCurrentAsync(leaderboardId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.SetCurrent(leaderboardId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLeaderboardService.Verify(s => s.SetCurrentAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task SetCurrent_MaxIntLeaderboardId_CallsServiceSuccessfully()
        {
            // Arrange
            int leaderboardId = int.MaxValue;

            _mockLeaderboardService
                .Setup(s => s.SetCurrentAsync(leaderboardId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.SetCurrent(leaderboardId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLeaderboardService.Verify(s => s.SetCurrentAsync(leaderboardId), Times.Once);
        }
    }
}
