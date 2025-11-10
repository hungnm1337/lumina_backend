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
    public class GetByIdSeasonTests
    {
        private readonly Mock<ILeaderboardService> _mockLeaderboardService;
        private readonly LeaderboardController _controller;

        public GetByIdSeasonTests()
        {
            _mockLeaderboardService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockLeaderboardService.Object);
        }

        [Fact]
        public async Task GetById_ExistingLeaderboardId_ReturnsOkWithLeaderboardDTO()
        {
            // Arrange
            int leaderboardId = 123;
            var expectedLeaderboard = new LeaderboardDTO
            {
                LeaderboardId = leaderboardId,
                SeasonName = "Season 2025 Fall",
                SeasonNumber = 10,
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(20),
                IsActive = true,
                Status = "Active",
                TotalParticipants = 150,
                DaysRemaining = 20,
                CreateAt = DateTime.Now.AddDays(-30)
            };

            _mockLeaderboardService
                .Setup(s => s.GetByIdAsync(leaderboardId))
                .ReturnsAsync(expectedLeaderboard);

            // Act
            var result = await _controller.GetById(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedLeaderboard = Assert.IsType<LeaderboardDTO>(okResult.Value);
            Assert.Equal(expectedLeaderboard.LeaderboardId, returnedLeaderboard.LeaderboardId);
            Assert.Equal(expectedLeaderboard.SeasonName, returnedLeaderboard.SeasonName);
            Assert.Equal(expectedLeaderboard.SeasonNumber, returnedLeaderboard.SeasonNumber);
            Assert.True(returnedLeaderboard.IsActive);
            _mockLeaderboardService.Verify(s => s.GetByIdAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetById_NonExistingLeaderboardId_ReturnsNotFound()
        {
            // Arrange
            int leaderboardId = 999;

            _mockLeaderboardService
                .Setup(s => s.GetByIdAsync(leaderboardId))
                .ReturnsAsync((LeaderboardDTO?)null);

            // Act
            var result = await _controller.GetById(leaderboardId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockLeaderboardService.Verify(s => s.GetByIdAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetById_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int leaderboardId = 123;

            _mockLeaderboardService
                .Setup(s => s.GetByIdAsync(leaderboardId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.GetById(leaderboardId)
            );
            _mockLeaderboardService.Verify(s => s.GetByIdAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetById_ZeroLeaderboardId_ReturnsNotFound()
        {
            // Arrange
            int leaderboardId = 0;

            _mockLeaderboardService
                .Setup(s => s.GetByIdAsync(leaderboardId))
                .ReturnsAsync((LeaderboardDTO?)null);

            // Act
            var result = await _controller.GetById(leaderboardId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockLeaderboardService.Verify(s => s.GetByIdAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetById_NegativeLeaderboardId_ReturnsNotFound()
        {
            // Arrange
            int leaderboardId = -1;

            _mockLeaderboardService
                .Setup(s => s.GetByIdAsync(leaderboardId))
                .ReturnsAsync((LeaderboardDTO?)null);

            // Act
            var result = await _controller.GetById(leaderboardId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockLeaderboardService.Verify(s => s.GetByIdAsync(leaderboardId), Times.Once);
        }

        [Fact]
        public async Task GetById_MaxIntLeaderboardId_ReturnsOkWithLeaderboardDTO()
        {
            // Arrange
            int leaderboardId = int.MaxValue;
            var expectedLeaderboard = new LeaderboardDTO
            {
                LeaderboardId = leaderboardId,
                SeasonName = "Max ID Season",
                SeasonNumber = 999,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                IsActive = false,
                Status = "Upcoming"
            };

            _mockLeaderboardService
                .Setup(s => s.GetByIdAsync(leaderboardId))
                .ReturnsAsync(expectedLeaderboard);

            // Act
            var result = await _controller.GetById(leaderboardId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedLeaderboard = Assert.IsType<LeaderboardDTO>(okResult.Value);
            Assert.Equal(leaderboardId, returnedLeaderboard.LeaderboardId);
            _mockLeaderboardService.Verify(s => s.GetByIdAsync(leaderboardId), Times.Once);
        }
    }
}
