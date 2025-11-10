using System;
using System.Threading.Tasks;
using DataLayer.DTOs.Leaderboard;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Leaderboard;
using Xunit;

namespace Lumina.Tests
{
    public class UpdateSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockLeaderboardService;
        private readonly LeaderboardController _controller;

        public UpdateSeasonTests()
        {
            _mockLeaderboardService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockLeaderboardService.Object);
        }

        [Fact]
        public async Task Update_ValidUpdateLeaderboardDTO_ReturnsNoContent()
        {
            // Arrange
            int leaderboardId = 123;
            var updateDto = new UpdateLeaderboardDTO
            {
                SeasonName = "Season 2025 Winter Updated",
                SeasonNumber = 11,
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(31),
                IsActive = true
            };

            _mockLeaderboardService
                .Setup(s => s.UpdateAsync(leaderboardId, It.IsAny<UpdateLeaderboardDTO>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(leaderboardId, updateDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLeaderboardService.Verify(s => s.UpdateAsync(leaderboardId, updateDto), Times.Once);
        }

        [Fact]
        public async Task Update_ServiceReturnsFalse_ReturnsNotFound()
        {
            // Arrange
            int leaderboardId = 999;
            var updateDto = new UpdateLeaderboardDTO
            {
                SeasonName = "Non-existent Season",
                SeasonNumber = 5,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                IsActive = false
            };

            _mockLeaderboardService
                .Setup(s => s.UpdateAsync(leaderboardId, It.IsAny<UpdateLeaderboardDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Update(leaderboardId, updateDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockLeaderboardService.Verify(s => s.UpdateAsync(leaderboardId, updateDto), Times.Once);
        }

        [Fact]
        public async Task Update_ServiceThrowsArgumentException_ReturnsBadRequestWithMessage()
        {
            // Arrange
            int leaderboardId = 123;
            var updateDto = new UpdateLeaderboardDTO
            {
                SeasonName = "Invalid Season",
                SeasonNumber = -1,
                StartDate = DateTime.Now.AddDays(10),
                EndDate = DateTime.Now.AddDays(5), // EndDate before StartDate
                IsActive = true
            };
            string errorMessage = "EndDate must be after StartDate";

            _mockLeaderboardService
                .Setup(s => s.UpdateAsync(leaderboardId, It.IsAny<UpdateLeaderboardDTO>()))
                .ThrowsAsync(new ArgumentException(errorMessage));

            // Act
            var result = await _controller.Update(leaderboardId, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var responseValue = badRequestResult.Value;
            
            var messageProperty = responseValue?.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty?.GetValue(responseValue)?.ToString();
            Assert.Equal(errorMessage, message);
            
            _mockLeaderboardService.Verify(s => s.UpdateAsync(leaderboardId, updateDto), Times.Once);
        }

        [Fact]
        public async Task Update_ServiceThrowsInvalidOperationException_PropagatesException()
        {
            // Arrange
            int leaderboardId = 123;
            var updateDto = new UpdateLeaderboardDTO
            {
                SeasonName = "Test Season",
                SeasonNumber = 5,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                IsActive = false
            };

            _mockLeaderboardService
                .Setup(s => s.UpdateAsync(leaderboardId, It.IsAny<UpdateLeaderboardDTO>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.Update(leaderboardId, updateDto)
            );
            _mockLeaderboardService.Verify(s => s.UpdateAsync(leaderboardId, updateDto), Times.Once);
        }

        [Fact]
        public async Task Update_ZeroLeaderboardId_ReturnsNotFound()
        {
            // Arrange
            int leaderboardId = 0;
            var updateDto = new UpdateLeaderboardDTO
            {
                SeasonName = "Test Season",
                SeasonNumber = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                IsActive = false
            };

            _mockLeaderboardService
                .Setup(s => s.UpdateAsync(leaderboardId, It.IsAny<UpdateLeaderboardDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Update(leaderboardId, updateDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockLeaderboardService.Verify(s => s.UpdateAsync(leaderboardId, updateDto), Times.Once);
        }

        [Fact]
        public async Task Update_NegativeLeaderboardId_ReturnsNoContent()
        {
            // Arrange
            int leaderboardId = -1;
            var updateDto = new UpdateLeaderboardDTO
            {
                SeasonName = "Boundary Test",
                SeasonNumber = 100,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(90),
                IsActive = true
            };

            _mockLeaderboardService
                .Setup(s => s.UpdateAsync(leaderboardId, It.IsAny<UpdateLeaderboardDTO>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(leaderboardId, updateDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLeaderboardService.Verify(s => s.UpdateAsync(leaderboardId, updateDto), Times.Once);
        }

        [Fact]
        public async Task Update_MinimalDTO_ReturnsNoContent()
        {
            // Arrange
            int leaderboardId = 50;
            var updateDto = new UpdateLeaderboardDTO
            {
                SeasonName = null,
                SeasonNumber = 0,
                StartDate = null,
                EndDate = null,
                IsActive = false
            };

            _mockLeaderboardService
                .Setup(s => s.UpdateAsync(leaderboardId, It.IsAny<UpdateLeaderboardDTO>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(leaderboardId, updateDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLeaderboardService.Verify(s => s.UpdateAsync(leaderboardId, updateDto), Times.Once);
        }
    }
}
