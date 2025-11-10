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
    public class GetCurrentSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockLeaderboardService;
        private readonly LeaderboardController _controller;

        public GetCurrentSeasonTests()
        {
            _mockLeaderboardService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockLeaderboardService.Object);
        }

        [Fact]
        public async Task GetCurrent_ExistingCurrentSeason_ReturnsOkWithLeaderboardDTO()
        {
            // Arrange
            var expectedLeaderboard = new LeaderboardDTO
            {
                LeaderboardId = 1,
                SeasonName = "Season 2025 Fall",
                SeasonNumber = 10,
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(20),
                IsActive = true,
                Status = "Active",
                TotalParticipants = 150,
                DaysRemaining = 20
            };

            _mockLeaderboardService
                .Setup(s => s.GetCurrentAsync())
                .ReturnsAsync(expectedLeaderboard);

            // Act
            var result = await _controller.GetCurrent();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedLeaderboard = Assert.IsType<LeaderboardDTO>(okResult.Value);
            Assert.Equal(expectedLeaderboard.LeaderboardId, returnedLeaderboard.LeaderboardId);
            Assert.Equal(expectedLeaderboard.SeasonName, returnedLeaderboard.SeasonName);
            Assert.True(returnedLeaderboard.IsActive);
            Assert.Equal("Active", returnedLeaderboard.Status);
            _mockLeaderboardService.Verify(s => s.GetCurrentAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCurrent_NoCurrentSeason_ReturnsNotFoundWithMessage()
        {
            // Arrange
            _mockLeaderboardService
                .Setup(s => s.GetCurrentAsync())
                .ReturnsAsync((LeaderboardDTO?)null);

            // Act
            var result = await _controller.GetCurrent();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var responseValue = notFoundResult.Value;
            
            // Verify the anonymous object has the message property
            var messageProperty = responseValue?.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty?.GetValue(responseValue)?.ToString();
            Assert.Equal("Không có mùa giải nào đang diễn ra", message);
            
            _mockLeaderboardService.Verify(s => s.GetCurrentAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCurrent_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            _mockLeaderboardService
                .Setup(s => s.GetCurrentAsync())
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.GetCurrent()
            );
            _mockLeaderboardService.Verify(s => s.GetCurrentAsync(), Times.Once);
        }
    }
}
