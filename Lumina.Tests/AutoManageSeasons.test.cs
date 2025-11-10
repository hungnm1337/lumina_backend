using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer.Leaderboard;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class AutoManageSeasonsTests
    {
        private readonly Mock<ILeaderboardService> _mockService;
        private readonly LeaderboardController _controller;

        public AutoManageSeasonsTests()
        {
            _mockService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockService.Object);
        }

        [Fact]
        public async Task AutoManageSeasons_ServiceExecutesSuccessfully_ReturnsOkWithMessage()
        {
            // Arrange
            _mockService.Setup(s => s.AutoManageSeasonsAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AutoManageSeasons();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var messageProp = value.GetType().GetProperty("message");
            Assert.NotNull(messageProp);
            Assert.Equal("Đã tự động quản lý mùa giải", messageProp.GetValue(value));
            _mockService.Verify(s => s.AutoManageSeasonsAsync(), Times.Once);
        }

        [Fact]
        public async Task AutoManageSeasons_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            _mockService.Setup(s => s.AutoManageSeasonsAsync())
                .ThrowsAsync(new System.Exception("Auto-manage failed"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => _controller.AutoManageSeasons());
            _mockService.Verify(s => s.AutoManageSeasonsAsync(), Times.Once);
        }

        [Fact]
        public async Task AutoManageSeasons_ServiceThrowsInvalidOperationException_PropagatesException()
        {
            // Arrange
            _mockService.Setup(s => s.AutoManageSeasonsAsync())
                .ThrowsAsync(new System.InvalidOperationException("Invalid season state"));

            // Act & Assert
            await Assert.ThrowsAsync<System.InvalidOperationException>(() => 
                _controller.AutoManageSeasons());
            _mockService.Verify(s => s.AutoManageSeasonsAsync(), Times.Once);
        }

        [Fact]
        public async Task AutoManageSeasons_CalledMultipleTimes_EachCallInvokesService()
        {
            // Arrange
            _mockService.Setup(s => s.AutoManageSeasonsAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _controller.AutoManageSeasons();
            await _controller.AutoManageSeasons();
            await _controller.AutoManageSeasons();

            // Assert
            _mockService.Verify(s => s.AutoManageSeasonsAsync(), Times.Exactly(3));
        }
    }
}
