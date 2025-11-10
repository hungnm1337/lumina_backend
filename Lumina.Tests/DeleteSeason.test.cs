using System;
using System.Threading.Tasks;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Leaderboard;
using Xunit;

namespace Lumina.Tests
{
    public class DeleteSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockLeaderboardService;
        private readonly LeaderboardController _controller;

        public DeleteSeasonTests()
        {
            _mockLeaderboardService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockLeaderboardService.Object);
        }

        [Fact]
        public async Task Delete_ExistingLeaderboardId_ReturnsNoContent()
        {
            // Arrange
            int leaderboardId = 123;

            _mockLeaderboardService
                .Setup(s => s.DeleteAsync(leaderboardId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(leaderboardId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLeaderboardService.Verify(s => s.DeleteAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task Delete_NonExistingLeaderboardId_ReturnsNotFound()
        {
            // Arrange
            int leaderboardId = 999;

            _mockLeaderboardService
                .Setup(s => s.DeleteAsync(leaderboardId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(leaderboardId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockLeaderboardService.Verify(s => s.DeleteAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task Delete_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int leaderboardId = 123;

            _mockLeaderboardService
                .Setup(s => s.DeleteAsync(leaderboardId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.Delete(leaderboardId)
            );
            _mockLeaderboardService.Verify(s => s.DeleteAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task Delete_ZeroLeaderboardId_CallsServiceWithZeroId()
        {
            // Arrange
            int leaderboardId = 0;

            _mockLeaderboardService
                .Setup(s => s.DeleteAsync(leaderboardId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(leaderboardId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockLeaderboardService.Verify(s => s.DeleteAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task Delete_NegativeLeaderboardId_CallsServiceWithNegativeId()
        {
            // Arrange
            int leaderboardId = -1;

            _mockLeaderboardService
                .Setup(s => s.DeleteAsync(leaderboardId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(leaderboardId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLeaderboardService.Verify(s => s.DeleteAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task Delete_MaxIntLeaderboardId_CallsServiceSuccessfully()
        {
            // Arrange
            int leaderboardId = int.MaxValue;

            _mockLeaderboardService
                .Setup(s => s.DeleteAsync(leaderboardId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(leaderboardId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLeaderboardService.Verify(s => s.DeleteAsync(leaderboardId), Times.Once);
        }
    }
}
