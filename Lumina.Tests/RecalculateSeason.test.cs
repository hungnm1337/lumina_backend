using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer.Leaderboard;
using System.Threading.Tasks;
using System.Reflection;

namespace Lumina.Tests
{
    public class RecalculateSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockService;
        private readonly LeaderboardController _controller;

        public RecalculateSeasonTests()
        {
            _mockService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockService.Object);
        }

        [Fact]
        public async Task Recalculate_ValidLeaderboardId_ReturnsOkWithAffectedCount()
        {
            // Arrange
            int leaderboardId = 1;
            int expectedAffected = 50;
            _mockService.Setup(s => s.RecalculateSeasonScoresAsync(leaderboardId))
                .ReturnsAsync(expectedAffected);

            // Act
            var result = await _controller.Recalculate(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = okResult.Value;
            var affectedProp = value?.GetType().GetProperty("affected");
            var messageProp = value?.GetType().GetProperty("message");
            Assert.NotNull(affectedProp);
            Assert.NotNull(messageProp);
            Assert.Equal(expectedAffected, affectedProp.GetValue(value));
            Assert.Equal($"Đã tính lại điểm cho {expectedAffected} người dùng", messageProp.GetValue(value));
            _mockService.Verify(s => s.RecalculateSeasonScoresAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task Recalculate_NoUsersAffected_ReturnsOkWithZero()
        {
            // Arrange
            int leaderboardId = 2;
            int expectedAffected = 0;
            _mockService.Setup(s => s.RecalculateSeasonScoresAsync(leaderboardId))
                .ReturnsAsync(expectedAffected);

            // Act
            var result = await _controller.Recalculate(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = okResult.Value;
            var affectedProp = value?.GetType().GetProperty("affected");
            var messageProp = value?.GetType().GetProperty("message");
            Assert.NotNull(affectedProp);
            Assert.NotNull(messageProp);
            Assert.Equal(0, affectedProp.GetValue(value));
            Assert.Equal("Đã tính lại điểm cho 0 người dùng", messageProp.GetValue(value));
            _mockService.Verify(s => s.RecalculateSeasonScoresAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task Recalculate_LargeNumberOfUsers_ReturnsOkWithHighCount()
        {
            // Arrange
            int leaderboardId = 3;
            int expectedAffected = 10000;
            _mockService.Setup(s => s.RecalculateSeasonScoresAsync(leaderboardId))
                .ReturnsAsync(expectedAffected);

            // Act
            var result = await _controller.Recalculate(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = okResult.Value;
            var affectedProp = value?.GetType().GetProperty("affected");
            var messageProp = value?.GetType().GetProperty("message");
            Assert.NotNull(affectedProp);
            Assert.NotNull(messageProp);
            Assert.Equal(expectedAffected, affectedProp.GetValue(value));
            var message = messageProp.GetValue(value)?.ToString();
            Assert.Contains("10000 người dùng", message);
            _mockService.Verify(s => s.RecalculateSeasonScoresAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task Recalculate_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int leaderboardId = 4;
            _mockService.Setup(s => s.RecalculateSeasonScoresAsync(leaderboardId))
                .ThrowsAsync(new System.Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => _controller.Recalculate(leaderboardId));
            _mockService.Verify(s => s.RecalculateSeasonScoresAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task Recalculate_InvalidLeaderboardId_ReturnsOkWithZero()
        {
            // Arrange
            int leaderboardId = 999;
            _mockService.Setup(s => s.RecalculateSeasonScoresAsync(leaderboardId))
                .ReturnsAsync(0);

            // Act
            var result = await _controller.Recalculate(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = okResult.Value;
            var affectedProp = value?.GetType().GetProperty("affected");
            Assert.NotNull(affectedProp);
            Assert.Equal(0, affectedProp.GetValue(value));
            _mockService.Verify(s => s.RecalculateSeasonScoresAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task Recalculate_NegativeLeaderboardId_CallsService()
        {
            // Arrange
            int leaderboardId = -1;
            _mockService.Setup(s => s.RecalculateSeasonScoresAsync(leaderboardId))
                .ReturnsAsync(0);

            // Act
            var result = await _controller.Recalculate(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockService.Verify(s => s.RecalculateSeasonScoresAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task Recalculate_ZeroLeaderboardId_CallsService()
        {
            // Arrange
            int leaderboardId = 0;
            _mockService.Setup(s => s.RecalculateSeasonScoresAsync(leaderboardId))
                .ReturnsAsync(0);

            // Act
            var result = await _controller.Recalculate(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = okResult.Value;
            var affectedProp = value?.GetType().GetProperty("affected");
            Assert.NotNull(affectedProp);
            Assert.Equal(0, affectedProp.GetValue(value));
            _mockService.Verify(s => s.RecalculateSeasonScoresAsync(leaderboardId), Times.Once);
        }
    }
}
